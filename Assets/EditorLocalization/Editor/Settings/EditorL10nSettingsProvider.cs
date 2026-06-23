#if UNITY_EDITOR
using System.Linq;
using UnityEditor;

namespace Kajitaharuka.EditorLocalization
{
    internal static class EditorL10nSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Preferences/Editor Localization", SettingsScope.User)
            {
                label = "Editor Localization",
                guiHandler = _ =>
                {
                    EditorGUILayout.LabelField("Editor Localization", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("表示言語はEditorPrefsへ保存され、プロジェクト資産には書き込まれません。", MessageType.Info);

                    foreach (var scope in EditorL10n.GetScopes())
                        DrawScopeLocale(scope);
                },
            };
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
    }
}
#endif
