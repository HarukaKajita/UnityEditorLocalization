#if UNITY_EDITOR
using UnityEditor;

namespace Kajitaharuka.EditorLocalization
{
    /// <summary>
    /// 表示言語の選択と開発時診断フラグをユーザーごとのEditorPrefsに保存する。
    /// プロジェクト資産へ書き込まないため、チーム内で設定の好みが衝突しない。
    /// </summary>
    internal static class EditorL10nPreferences
    {
        private const string DiagnosticsEnabledKey = "Kajitaharuka.EditorLocalization.DiagnosticsEnabled";
        private const string GlobalLocaleKey = "Kajitaharuka.EditorLocalization.GlobalLocale";
        private const string ScopeLocaleKeyPrefix = "Kajitaharuka.EditorLocalization.ScopeLocale.";
        private const string SystemLocaleFallbackEnabledKey = "Kajitaharuka.EditorLocalization.SystemLocaleFallbackEnabled";

        internal static bool DiagnosticsEnabled
        {
            get => EditorPrefs.GetBool(DiagnosticsEnabledKey, false);
            set => SetOrDelete(DiagnosticsEnabledKey, value);
        }

        /// <summary>
        /// グローバル設定が未設定のとき、OS の言語へフォールバックするかどうか。既定は有効（true）。
        /// 既定値と同じ「有効」のときはキーを削除し、無効化したときだけ false を保存する。
        /// </summary>
        internal static bool SystemLocaleFallbackEnabled
        {
            get => EditorPrefs.GetBool(SystemLocaleFallbackEnabledKey, true);
            set
            {
                if (value)
                    EditorPrefs.DeleteKey(SystemLocaleFallbackEnabledKey);
                else
                    EditorPrefs.SetBool(SystemLocaleFallbackEnabledKey, false);
            }
        }

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

        private static void SetOrDelete(string key, bool value)
        {
            if (value)
                EditorPrefs.SetBool(key, true);
            else
                EditorPrefs.DeleteKey(key);
        }
    }
}
#endif
