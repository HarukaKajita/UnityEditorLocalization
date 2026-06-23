#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Kajitaharuka.EditorLocalization
{
    internal static class EditorL10nSettingsProvider
    {
        private const string UnsetGlobalLocaleLabel = "未設定（各 scope の既定言語）";
        private const string FollowGlobalLocaleLabel = "グローバル設定に従う";
        private const float LocaleLabelWidth = 120f;

        private static readonly Dictionary<string, bool> ScopeFoldouts = new();
        private static readonly Dictionary<string, bool> SearchScopeFoldouts = new();
        private static string _scopeSearchText = "";
        private static string _previousScopeSearchText = "";

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Preferences/Editor Localization", SettingsScope.User)
            {
                label = "Editor Localization",
                guiHandler = _ =>
                {
                    EditorGUILayout.LabelField("Editor Localization", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("表示言語はEditorPrefsへ保存され、プロジェクト資産には書き込まれません。scope 個別設定はグローバル設定より優先され、「グローバル設定に従う」で解除できます。", MessageType.Info);

                    GUILayout.Space(8f);
                    DrawGlobalLocale();
                    GUILayout.Space(8f);
                    DrawDiagnostics();
                    GUILayout.Space(8f);
                    DrawScopeLocales();
                },
            };
        }

        private static void DrawDiagnostics()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("開発時診断", EditorStyles.boldLabel);
                using (new LabelWidthScope(LocaleLabelWidth))
                {
                    EditorGUI.BeginChangeCheck();
                    var enabled = EditorGUILayout.Toggle(
                        new GUIContent("未解決警告", "未知 scope や未解決 key を検出したときに、同一 scope/key につき一度だけ Console へ警告します。"),
                        EditorL10nPreferences.DiagnosticsEnabled);
                    if (EditorGUI.EndChangeCheck())
                        EditorL10nPreferences.DiagnosticsEnabled = enabled;
                }
            }
        }

        private static void DrawGlobalLocale()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("グローバル設定", EditorStyles.boldLabel);
                DrawGlobalLocalePopup();
            }
        }

        private static void DrawGlobalLocalePopup()
        {
            var locales = GetGlobalLocaleOptions().ToArray();
            var optionTags = new List<string> { "" };
            var labels = new List<string> { UnsetGlobalLocaleLabel };

            foreach (var locale in locales)
            {
                optionTags.Add(locale.Tag);
                labels.Add(locale.DisplayName);
            }

            var activeLocale = EditorL10n.GetGlobalLocale();
            var activeIndex = optionTags.IndexOf(activeLocale);
            if (activeIndex < 0 && !string.IsNullOrEmpty(activeLocale))
            {
                optionTags.Add(activeLocale);
                labels.Add($"{activeLocale}（登録済みカタログ外）");
                activeIndex = optionTags.Count - 1;
            }

            if (activeIndex < 0)
                activeIndex = 0;

            using (new LabelWidthScope(LocaleLabelWidth))
            {
                EditorGUI.BeginChangeCheck();
                var selectedIndex = EditorGUILayout.Popup(
                    new GUIContent("すべての scope", "scope 個別設定がない場合に使う表示言語です。"),
                    activeIndex,
                    labels.ToArray());
                if (EditorGUI.EndChangeCheck() && selectedIndex >= 0 && selectedIndex < optionTags.Count)
                    EditorL10n.SetGlobalLocale(optionTags[selectedIndex]);
            }
        }

        private static void DrawScopeLocales()
        {
            var scopes = EditorL10n.GetScopes().ToArray();
            EditorGUILayout.LabelField("scope 個別設定", EditorStyles.boldLabel);

            using (new LabelWidthScope(LocaleLabelWidth))
            {
                _scopeSearchText = EditorGUILayout.TextField(
                    new GUIContent("検索", "scope 文字列で絞り込みます。"),
                    _scopeSearchText ?? "");
            }
            ResetSearchFoldoutsWhenSearchTextChanged();

            if (scopes.Length == 0)
            {
                EditorGUILayout.HelpBox("l10n manifest が見つかりません。manifest を追加すると scope ごとの表示言語を設定できます。", MessageType.Info);
                return;
            }

            var filteredScopes = scopes
                .Where(scope => ScopeMatchesSearch(scope, _scopeSearchText))
                .ToArray();
            EditorGUILayout.LabelField($"{filteredScopes.Length} / {scopes.Length} scope", EditorStyles.miniLabel);

            if (filteredScopes.Length == 0)
            {
                EditorGUILayout.HelpBox("検索条件に一致する scope はありません。", MessageType.Info);
                return;
            }

            foreach (var scope in filteredScopes)
                DrawScopeLocale(scope);
        }

        private static void DrawScopeLocale(string scope)
        {
            if (!EditorL10n.TryGetScopeInfo(scope, out var scopeInfo))
                scopeInfo = new EditorL10nScopeInfo(scope, "", "");

            var locales = EditorL10n.GetLocales(scope).ToArray();
            var explicitLocale = EditorL10n.NormalizeLocaleTag(EditorL10nPreferences.GetScopeLocale(scope));
            var localeState = CreateScopeLocaleState(scopeInfo, locales, explicitLocale);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var foldout = DrawScopeFoldout(
                    scope,
                    new GUIContent(scope, scopeInfo.ManifestPath));

                DrawScopeSummary(scopeInfo, localeState);

                if (!foldout)
                    return;

                GUILayout.Space(4f);

                if (locales.Length == 0)
                {
                    EditorGUILayout.HelpBox("この scope には利用可能な locale がありません。manifest の locales を確認してください。", MessageType.Warning);
                    return;
                }

                DrawScopeLocalePopup(scope, locales, explicitLocale);
            }
        }

        private static void DrawScopeLocalePopup(string scope, EditorL10nLocaleInfo[] locales, string explicitLocale)
        {
            var optionTags = new List<string> { "" };
            var labels = new List<string> { FollowGlobalLocaleLabel };

            foreach (var locale in locales)
            {
                optionTags.Add(locale.Tag);
                labels.Add(locale.DisplayName);
            }

            var activeIndex = optionTags.IndexOf(explicitLocale);
            if (activeIndex < 0 && !string.IsNullOrEmpty(explicitLocale))
            {
                optionTags.Add(explicitLocale);
                labels.Add($"{explicitLocale}（登録済みカタログ外）");
                activeIndex = optionTags.Count - 1;
            }

            if (activeIndex < 0)
                activeIndex = 0;

            using (new LabelWidthScope(LocaleLabelWidth))
            {
                EditorGUI.BeginChangeCheck();
                var selectedIndex = EditorGUILayout.Popup(
                    new GUIContent("表示言語", "この scope だけに適用する表示言語です。"),
                    activeIndex,
                    labels.ToArray());
                if (EditorGUI.EndChangeCheck() && selectedIndex >= 0 && selectedIndex < optionTags.Count)
                    EditorL10n.SetActiveLocale(scope, optionTags[selectedIndex]);
            }
        }

        private static void DrawScopeSummary(EditorL10nScopeInfo scopeInfo, ScopeLocaleState localeState)
        {
            EditorGUILayout.LabelField(
                $"現在: {FormatLocaleTag(localeState.ResolvedLocale)}（{localeState.SourceDescription}） / defaultLocale: {FormatLocaleTag(scopeInfo.DefaultLocale)}",
                WordWrapMiniLabel);

            EditorGUILayout.LabelField(
                $"manifest: {FormatPath(scopeInfo.ManifestPath)}",
                WordWrapMiniLabel);
        }

        private static bool ScopeMatchesSearch(string scope, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return true;

            return (scope ?? "").IndexOf(searchText.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void ResetSearchFoldoutsWhenSearchTextChanged()
        {
            var currentSearchText = (_scopeSearchText ?? "").Trim();
            if (currentSearchText == _previousScopeSearchText)
                return;

            SearchScopeFoldouts.Clear();
            _previousScopeSearchText = currentSearchText;
        }

        private static bool DrawScopeFoldout(string scope, GUIContent content)
        {
            if (!string.IsNullOrWhiteSpace(_scopeSearchText))
            {
                if (!SearchScopeFoldouts.TryGetValue(scope, out var searchFoldout))
                    SearchScopeFoldouts[scope] = searchFoldout = true;

                SearchScopeFoldouts[scope] = EditorGUILayout.Foldout(searchFoldout, content, true);
                return SearchScopeFoldouts[scope];
            }

            if (!ScopeFoldouts.TryGetValue(scope, out var foldout))
                ScopeFoldouts[scope] = foldout = false;

            ScopeFoldouts[scope] = EditorGUILayout.Foldout(foldout, content, true);
            return ScopeFoldouts[scope];
        }

        private static string GetActiveLocaleSource(string explicitLocale)
        {
            if (!string.IsNullOrEmpty(explicitLocale))
                return "scope 個別設定";

            return string.IsNullOrEmpty(EditorL10n.GetGlobalLocale())
                ? "defaultLocale"
                : "グローバル設定";
        }

        private static ScopeLocaleState CreateScopeLocaleState(EditorL10nScopeInfo scopeInfo, EditorL10nLocaleInfo[] locales, string explicitLocale)
        {
            var requestedLocale = EditorL10n.GetActiveLocale(scopeInfo.Scope);
            var resolvedLocale = ResolveAvailableLocale(requestedLocale, scopeInfo.DefaultLocale, locales);
            var sourceDescription = GetLocaleSourceDescription(GetActiveLocaleSource(explicitLocale), requestedLocale, resolvedLocale);
            return new ScopeLocaleState(resolvedLocale, sourceDescription);
        }

        private static string ResolveAvailableLocale(string requestedLocale, string defaultLocale, EditorL10nLocaleInfo[] locales)
        {
            var availableLocales = new HashSet<string>(
                locales
                    .Where(locale => locale != null && !string.IsNullOrEmpty(locale.Tag))
                    .Select(locale => locale.Tag));

            foreach (var candidate in EditorL10n.BuildFallbackChain(requestedLocale, defaultLocale))
            {
                if (availableLocales.Contains(candidate))
                    return candidate;
            }

            return "";
        }

        private static string GetLocaleSourceDescription(string source, string requestedLocale, string resolvedLocale)
        {
            if (string.IsNullOrEmpty(requestedLocale) || requestedLocale == resolvedLocale)
                return source;

            if (string.IsNullOrEmpty(resolvedLocale))
                return $"{source}: {requestedLocale} は登録済み locale 外";

            return $"{source}: {requestedLocale} から fallback";
        }

        private static string FormatLocaleTag(string locale)
        {
            return string.IsNullOrEmpty(locale) ? "未設定" : locale;
        }

        private static string FormatPath(string path)
        {
            return string.IsNullOrEmpty(path) ? "不明" : path;
        }

        private static IEnumerable<EditorL10nLocaleInfo> GetGlobalLocaleOptions()
        {
            var localesByTag = new Dictionary<string, EditorL10nLocaleInfo>();
            foreach (var scope in EditorL10n.GetScopes())
            {
                foreach (var locale in EditorL10n.GetLocales(scope))
                {
                    if (locale == null || string.IsNullOrEmpty(locale.Tag) || localesByTag.ContainsKey(locale.Tag))
                        continue;
                    localesByTag.Add(locale.Tag, locale);
                }
            }

            return localesByTag.Values.OrderBy(locale => locale.Tag);
        }

        private static GUIStyle WordWrapMiniLabel => new(EditorStyles.miniLabel)
        {
            wordWrap = true,
        };

        private sealed class LabelWidthScope : IDisposable
        {
            private readonly float _previousLabelWidth;

            public LabelWidthScope(float labelWidth)
            {
                _previousLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = labelWidth;
            }

            public void Dispose()
            {
                EditorGUIUtility.labelWidth = _previousLabelWidth;
            }
        }

        private sealed class ScopeLocaleState
        {
            public string ResolvedLocale { get; }
            public string SourceDescription { get; }

            public ScopeLocaleState(string resolvedLocale, string sourceDescription)
            {
                ResolvedLocale = resolvedLocale ?? "";
                SourceDescription = sourceDescription ?? "";
            }
        }
    }
}
#endif
