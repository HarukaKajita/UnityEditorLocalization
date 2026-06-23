#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Kajitaharuka.EditorLocalization
{
    internal static class EditorL10nSettingsProvider
    {
        private const string UnsetGlobalLocaleLabel = "未設定（各 scope の既定言語）";

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Preferences/Editor Localization", SettingsScope.User)
            {
                label = "Editor Localization",
                guiHandler = _ =>
                {
                    EditorGUILayout.LabelField("Editor Localization", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("表示言語はEditorPrefsへ保存され、プロジェクト資産には書き込まれません。scope 個別設定はグローバル設定より優先されます。", MessageType.Info);

                    DrawGlobalLocale();
                    EditorGUILayout.Space();

                    foreach (var scope in EditorL10n.GetScopes())
                        DrawScopeLocale(scope);
                },
            };
        }

        private static void DrawGlobalLocale()
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

            EditorGUI.BeginChangeCheck();
            var selectedIndex = EditorGUILayout.Popup("すべての scope", activeIndex, labels.ToArray());
            if (EditorGUI.EndChangeCheck() && selectedIndex >= 0 && selectedIndex < optionTags.Count)
                EditorL10n.SetGlobalLocale(optionTags[selectedIndex]);
        }

        private static void DrawScopeLocale(string scope)
        {
            var locales = EditorL10n.GetLocales(scope).ToArray();
            if (locales.Length == 0)
                return;

            var labels = locales.Select(locale => locale.DisplayName).ToArray();
            var activeLocale = EditorL10n.GetActiveLocale(scope);
            var activeIndex = System.Array.FindIndex(locales, locale => locale.Tag == activeLocale);
            if (activeIndex < 0)
                activeIndex = 0;

            EditorGUI.BeginChangeCheck();
            var selectedIndex = EditorGUILayout.Popup(scope, activeIndex, labels);
            if (EditorGUI.EndChangeCheck() && selectedIndex >= 0 && selectedIndex < locales.Length)
                EditorL10n.SetActiveLocale(scope, locales[selectedIndex].Tag);
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
    }
}
#endif
