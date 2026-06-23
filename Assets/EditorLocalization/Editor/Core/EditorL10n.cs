#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Kajitaharuka.EditorLocalization
{
    /// <summary>
    /// Unity Editor拡張向けの軽量ローカライズAPI。
    /// scopeごとのmanifestをAssetDatabaseから読み込み、選択ロケールとfallbackに従って文言を返す。
    /// </summary>
    public static class EditorL10n
    {
        private static EditorL10nCatalog _catalog;
        private static readonly HashSet<string> ReportedDiagnostics = new();

        public static event Action LocaleChanged;

        internal static EditorL10nCatalog Catalog => _catalog ??= EditorL10nCatalog.Load();

        /// <summary>
        /// 全 scope 共通で使う表示ロケールを返す。未設定の場合は空文字を返す。
        /// </summary>
        public static string GetGlobalLocale()
        {
            return NormalizeLocaleTag(EditorL10nPreferences.GlobalLocale);
        }

        /// <summary>
        /// 全 scope 共通で使う表示ロケールを設定する。空文字を指定すると未設定へ戻す。
        /// </summary>
        public static void SetGlobalLocale(string locale)
        {
            var normalizedLocale = NormalizeLocaleTag(locale);
            EditorL10nPreferences.GlobalLocale = normalizedLocale;
            LocaleChanged?.Invoke();
        }

        public static string GetActiveLocale(string scope)
        {
            var normalizedScope = scope ?? "";
            var scopeLocale = NormalizeLocaleTag(EditorL10nPreferences.GetScopeLocale(normalizedScope));
            if (!string.IsNullOrEmpty(scopeLocale))
                return scopeLocale;

            var globalLocale = NormalizeLocaleTag(EditorL10nPreferences.GlobalLocale);
            if (!string.IsNullOrEmpty(globalLocale))
                return globalLocale;

            return Catalog.TryGetScope(normalizedScope, out var scopeCatalog)
                ? scopeCatalog.DefaultLocale
                : "";
        }

        public static void SetActiveLocale(string scope, string locale)
        {
            var normalizedLocale = NormalizeLocaleTag(locale);
            EditorL10nPreferences.SetScopeLocale(scope, normalizedLocale);
            LocaleChanged?.Invoke();
        }

        public static IReadOnlyList<EditorL10nLocaleInfo> GetLocales(string scope)
        {
            return Catalog.TryGetScope(scope ?? "", out var scopeCatalog)
                ? scopeCatalog.Locales
                : Array.Empty<EditorL10nLocaleInfo>();
        }

        /// <summary>
        /// scopeのdefaultLocaleやmanifestパスなど、表示補助用のメタ情報を取得する。
        /// </summary>
        public static bool TryGetScopeInfo(string scope, out EditorL10nScopeInfo info)
        {
            if (Catalog.TryGetScope(scope ?? "", out var scopeCatalog))
            {
                info = new EditorL10nScopeInfo(
                    scopeCatalog.Scope,
                    scopeCatalog.DefaultLocale,
                    scopeCatalog.ManifestPath);
                return true;
            }

            info = null;
            return false;
        }

        public static IReadOnlyList<string> GetScopes()
        {
            return Catalog.Scopes.Select(scope => scope.Scope).OrderBy(scope => scope).ToArray();
        }

        public static string Tr(string scope, string key, params object[] args)
        {
            if (TryTranslate(scope, key, out var text))
            {
                if (args == null || args.Length == 0)
                    return text;
                try
                {
                    return string.Format(text, args);
                }
                catch (FormatException e)
                {
                    Debug.LogError($"EditorLocalization: formatに失敗しました: {scope}/{key}: {e.Message}");
                    return text;
                }
            }

            return key ?? "";
        }

        public static bool TryTranslate(string scope, string key, out string text)
        {
            text = "";
            if (string.IsNullOrEmpty(scope) || string.IsNullOrEmpty(key))
                return false;

            if (!Catalog.TryGetScope(scope, out var scopeCatalog))
            {
                LogMissingScopeOnce(scope, key);
                return false;
            }

            foreach (var locale in BuildFallbackChain(GetActiveLocale(scope), scopeCatalog.DefaultLocale))
            {
                if (scopeCatalog.TryGetText(locale, key, out text))
                    return true;
            }

            LogMissingKeyOnce(scope, key);
            return false;
        }

        public static void Reload()
        {
            _catalog = EditorL10nCatalog.Load();
            ReportedDiagnostics.Clear();
            LocaleChanged?.Invoke();
        }

        private static void LogMissingScopeOnce(string scope, string key)
        {
            LogDiagnosticOnce(
                "scope",
                scope,
                key,
                $"EditorLocalization: 未知の scope です: {scope} (key={key})");
        }

        private static void LogMissingKeyOnce(string scope, string key)
        {
            LogDiagnosticOnce(
                "key",
                scope,
                key,
                $"EditorLocalization: 未解決の key です: {scope}/{key}");
        }

        private static void LogDiagnosticOnce(string category, string scope, string key, string message)
        {
            if (!EditorL10nPreferences.DiagnosticsEnabled)
                return;

            var diagnosticKey = $"{category}\u001f{scope}\u001f{key}";
            if (ReportedDiagnostics.Add(diagnosticKey))
                Debug.LogWarning(message);
        }

        internal static IEnumerable<string> BuildFallbackChain(string locale, string defaultLocale)
        {
            var yielded = new HashSet<string>();
            foreach (var candidate in EnumerateLocaleAndParents(NormalizeLocaleTag(locale)))
            {
                if (!string.IsNullOrEmpty(candidate) && yielded.Add(candidate))
                    yield return candidate;
            }

            foreach (var candidate in EnumerateLocaleAndParents(NormalizeLocaleTag(defaultLocale)))
            {
                if (!string.IsNullOrEmpty(candidate) && yielded.Add(candidate))
                    yield return candidate;
            }
        }

        internal static IEnumerable<string> EnumerateLocaleAndParents(string locale)
        {
            var current = NormalizeLocaleTag(locale);
            while (!string.IsNullOrEmpty(current))
            {
                yield return current;
                var index = current.LastIndexOf('-');
                current = index <= 0 ? "" : current.Substring(0, index);
            }
        }

        public static string NormalizeLocaleTag(string locale)
        {
            if (string.IsNullOrWhiteSpace(locale))
                return "";

            var parts = locale.Trim().Replace('_', '-').Split('-');
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (string.IsNullOrEmpty(part))
                    continue;

                if (i == 0)
                    parts[i] = part.ToLowerInvariant();
                else if (part.Length == 4 && part.All(char.IsLetter))
                    parts[i] = char.ToUpperInvariant(part[0]) + part.Substring(1).ToLowerInvariant();
                else if (part.Length == 2 && part.All(char.IsLetter))
                    parts[i] = part.ToUpperInvariant();
                else
                    parts[i] = part.ToLowerInvariant();
            }

            return string.Join("-", parts.Where(part => !string.IsNullOrEmpty(part)));
        }
    }
}
#endif
