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

        public static event Action LocaleChanged;

        internal static EditorL10nCatalog Catalog => _catalog ??= EditorL10nCatalog.Load();

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
                return false;

            foreach (var locale in BuildFallbackChain(GetActiveLocale(scope), scopeCatalog.DefaultLocale))
            {
                if (scopeCatalog.TryGetText(locale, key, out text))
                    return true;
            }

            return false;
        }

        public static void Reload()
        {
            _catalog = EditorL10nCatalog.Load();
            LocaleChanged?.Invoke();
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
