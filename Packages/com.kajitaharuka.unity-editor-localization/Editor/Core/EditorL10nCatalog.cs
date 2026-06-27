#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Kajitaharuka.EditorLocalization
{
    internal sealed class EditorL10nCatalog
    {
        private readonly Dictionary<string, EditorL10nScopeCatalog> _scopes = new();

        internal IReadOnlyCollection<EditorL10nScopeCatalog> Scopes => _scopes.Values;

        internal static EditorL10nCatalog Load()
        {
            var catalog = new EditorL10nCatalog();
            foreach (var manifestPath in FindManifestPaths())
                catalog.TryAddManifest(manifestPath);
            return catalog;
        }

        internal static bool IsManifestPath(string path)
        {
            return !string.IsNullOrEmpty(path)
                && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                && Path.GetFileName(path).Contains("l10n-manifest");
        }

        internal bool TryGetScope(string scope, out EditorL10nScopeCatalog scopeCatalog)
        {
            return _scopes.TryGetValue(scope ?? "", out scopeCatalog);
        }

        private static string[] FindManifestPaths()
        {
            return AssetDatabase.FindAssets("l10n-manifest")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(IsManifestPath)
                .Distinct()
                .OrderBy(path => path)
                .ToArray();
        }

        private void TryAddManifest(string manifestPath)
        {
            var manifestAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(manifestPath);
            if (manifestAsset == null)
                return;

            var document = JsonUtility.FromJson<EditorL10nManifestDocument>(manifestAsset.text);
            if (document == null || string.IsNullOrEmpty(document.scope))
                return;

            var scope = (document.scope ?? "").Trim();
            if (_scopes.ContainsKey(scope))
            {
                Debug.LogWarning($"EditorLocalization: scope が重複しているため後続のmanifestを無視しました: {scope} ({manifestPath})");
                return;
            }

            var manifestDirectory = Path.GetDirectoryName(manifestPath)?.Replace("\\", "/") ?? "";
            var scopeCatalog = new EditorL10nScopeCatalog(scope, EditorL10n.NormalizeLocaleTag(document.defaultLocale), manifestPath, document.fixedTerms);
            foreach (var locale in document.locales ?? Array.Empty<EditorL10nManifestLocale>())
            {
                if (locale == null || string.IsNullOrEmpty(locale.tag) || string.IsNullOrEmpty(locale.tablePath))
                    continue;

                var tag = EditorL10n.NormalizeLocaleTag(locale.tag);
                var tablePath = NormalizeAssetPath(Path.Combine(manifestDirectory, locale.tablePath));
                var tableAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(tablePath);
                var entries = LoadEntries(tableAsset, tag);
                scopeCatalog.AddLocale(new EditorL10nLocaleInfo(tag, locale.nativeName, locale.englishName), entries, tablePath);
            }

            _scopes.Add(scope, scopeCatalog);
        }

        private static Dictionary<string, string> LoadEntries(TextAsset tableAsset, string expectedLocale)
        {
            var entries = new Dictionary<string, string>();
            if (tableAsset == null)
                return entries;

            var document = JsonUtility.FromJson<EditorL10nTableDocument>(tableAsset.text);
            if (document == null)
                return entries;

            var actualLocale = EditorL10n.NormalizeLocaleTag(document.locale);
            if (!string.IsNullOrEmpty(actualLocale) && actualLocale != expectedLocale)
                Debug.LogWarning($"EditorLocalization: tableのlocaleとmanifestのtagが一致しません: {actualLocale} / {expectedLocale} ({AssetDatabase.GetAssetPath(tableAsset)})");

            foreach (var entry in document.entries ?? Array.Empty<EditorL10nEntry>())
            {
                if (entry == null || string.IsNullOrEmpty(entry.key))
                    continue;
                entries[entry.key] = entry.value ?? "";
            }

            return entries;
        }

        private static string NormalizeAssetPath(string path)
        {
            return path.Replace("\\", "/");
        }
    }

    internal sealed class EditorL10nScopeCatalog
    {
        private readonly List<EditorL10nLocaleInfo> _locales = new();
        private readonly Dictionary<string, Dictionary<string, string>> _tablesByLocale = new();
        private readonly HashSet<string> _tablePaths = new();
        // locale タグ -> その locale のテーブルアセットパス（検証結果から該当ファイルへジャンプする用途）。
        private readonly Dictionary<string, string> _tablePathByLocale = new();
        // 全ロケールで defaultLocale と同値でも「未翻訳の疑い」警告を出さない固定語 key（manifest の fixedTerms）。
        private readonly HashSet<string> _fixedTerms;

        internal string Scope { get; }
        internal string DefaultLocale { get; }
        internal string ManifestPath { get; }
        internal IReadOnlyList<EditorL10nLocaleInfo> Locales => _locales;
        internal IReadOnlyDictionary<string, Dictionary<string, string>> TablesByLocale => _tablesByLocale;
        internal IReadOnlyCollection<string> TablePaths => _tablePaths;
        internal IReadOnlyCollection<string> FixedTerms => _fixedTerms;

        internal EditorL10nScopeCatalog(string scope, string defaultLocale, string manifestPath, IEnumerable<string> fixedTerms = null)
        {
            Scope = scope;
            DefaultLocale = defaultLocale;
            ManifestPath = manifestPath;
            _fixedTerms = new HashSet<string>(fixedTerms ?? Array.Empty<string>());
        }

        /// <summary>その key が固定語（同値でも未翻訳警告を出さない）として宣言されているか。</summary>
        internal bool IsFixedTerm(string key) => key != null && _fixedTerms.Contains(key);

        internal void AddLocale(EditorL10nLocaleInfo locale, Dictionary<string, string> entries, string tablePath)
        {
            if (locale == null || string.IsNullOrEmpty(locale.Tag))
                return;

            if (_tablesByLocale.ContainsKey(locale.Tag))
                return;

            _locales.Add(locale);
            _tablesByLocale.Add(locale.Tag, entries ?? new Dictionary<string, string>());
            if (!string.IsNullOrEmpty(tablePath))
            {
                _tablePaths.Add(tablePath);
                _tablePathByLocale[locale.Tag] = tablePath;
            }
        }

        internal bool HasLocale(string locale)
        {
            return _tablesByLocale.ContainsKey(locale);
        }

        /// <summary>その locale のテーブルアセットパスを取得する（未登録/空なら false）。</summary>
        internal bool TryGetTablePath(string locale, out string tablePath)
        {
            tablePath = "";
            if (_tablePathByLocale.TryGetValue(locale ?? "", out var path) && !string.IsNullOrEmpty(path))
            {
                tablePath = path;
                return true;
            }

            return false;
        }

        internal bool TryGetText(string locale, string key, out string text)
        {
            text = "";
            return _tablesByLocale.TryGetValue(locale, out var table) && table.TryGetValue(key, out text);
        }
    }
}
#endif
