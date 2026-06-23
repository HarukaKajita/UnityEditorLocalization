#if UNITY_EDITOR
using UnityEditor;

namespace Kajitaharuka.EditorLocalization
{
    /// <summary>
    /// 表示言語の選択をユーザーごとのEditorPrefsに保存する。
    /// プロジェクト資産へ書き込まないため、チーム内で表示言語の好みが衝突しない。
    /// </summary>
    internal static class EditorL10nPreferences
    {
        private const string GlobalLocaleKey = "Kajitaharuka.EditorLocalization.GlobalLocale";
        private const string ScopeLocaleKeyPrefix = "Kajitaharuka.EditorLocalization.ScopeLocale.";

        internal static string GlobalLocale
        {
            get => EditorPrefs.GetString(GlobalLocaleKey, "");
            set => SetOrDelete(GlobalLocaleKey, value);
        }

        internal static string GetScopeLocale(string scope)
        {
            if (string.IsNullOrEmpty(scope))
                return "";
            return EditorPrefs.GetString(ScopeLocaleKeyPrefix + scope, "");
        }

        internal static void SetScopeLocale(string scope, string locale)
        {
            if (string.IsNullOrEmpty(scope))
                return;
            SetOrDelete(ScopeLocaleKeyPrefix + scope, locale);
        }

        private static void SetOrDelete(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
                EditorPrefs.DeleteKey(key);
            else
                EditorPrefs.SetString(key, value);
        }
    }
}
#endif
