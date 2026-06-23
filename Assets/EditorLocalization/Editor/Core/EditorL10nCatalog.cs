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

        internal bool TryGetScope(string scope, out EditorL10nScopeCatalog scopeCatalog)
        {
            return _scopes.TryGetValue(scope ?? "", out scopeCatalog);
        }

        private static string[] FindManifestPaths()
        {
            return AssetDatabase.FindAssets("l10n-manifest")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                .Where(path => Path.GetFileName(path).Contains("l10n-manifest"))
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
            var scopeCatalog = new EditorL10nScopeCatalog(scope, EditorL10n.NormalizeLocaleTag(document.defaultLocale), manifestPath);
            foreach (var locale in document.locales ?? Array.Empty<EditorL10nManifestLocale>())
            {
                if (locale == null || string.IsNullOrEmpty(locale.tag) || string.IsNullOrEmpty(locale.tablePath))
                    continue;

                var tag = EditorL10n.NormalizeLocaleTag(locale.tag);
                var tablePath = NormalizeAssetPath(Path.Combine(manifestDirectory, locale.tablePath));
                var tableAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(tablePath);
                var entries = LoadEntries(tableAsset, tag);
                scopeCatalog.AddLocale(new EditorL10nLocaleInfo(tag, locale.nativeName, locale.englishName), entries);
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

        internal string Scope { get; }
        internal string DefaultLocale { get; }
        internal string ManifestPath { get; }
        internal IReadOnlyList<EditorL10nLocaleInfo> Locales => _locales;
        internal IReadOnlyDictionary<string, Dictionary<string, string>> TablesByLocale => _tablesByLocale;

        internal EditorL10nScopeCatalog(string scope, string defaultLocale, string manifestPath)
        {
            Scope = scope;
            DefaultLocale = defaultLocale;
            ManifestPath = manifestPath;
        }

        internal void AddLocale(EditorL10nLocaleInfo locale, Dictionary<string, string> entries)
        {
            if (locale == null || string.IsNullOrEmpty(locale.Tag))
                return;

            if (_tablesByLocale.ContainsKey(locale.Tag))
                return;

            _locales.Add(locale);
            _tablesByLocale.Add(locale.Tag, entries ?? new Dictionary<string, string>());
        }

        internal bool HasLocale(string locale)
        {
            return _tablesByLocale.ContainsKey(locale);
        }

        internal bool TryGetText(string locale, string key, out string text)
        {
            text = "";
            return _tablesByLocale.TryGetValue(locale, out var table) && table.TryGetValue(key, out text);
        }
    }
}
#endif
