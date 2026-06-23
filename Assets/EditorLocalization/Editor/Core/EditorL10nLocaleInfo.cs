#if UNITY_EDITOR
using System;

namespace Kajitaharuka.EditorLocalization
{
    /// <summary>
    /// 翻訳カタログに登録されているロケールの表示情報。
    /// ロケール追加時にC#のenumを更新しなくてよいよう、タグは文字列で保持する。
    /// </summary>
    [Serializable]
    public sealed class EditorL10nLocaleInfo
    {
        public string Tag { get; }
        public string NativeName { get; }
        public string EnglishName { get; }

        public string DisplayName
        {
            get
            {
                var displayName = !string.IsNullOrEmpty(NativeName)
                    ? NativeName
                    : !string.IsNullOrEmpty(EnglishName)
                        ? EnglishName
                        : Tag;
                return $"{displayName} ({Tag})";
            }
        }

        public EditorL10nLocaleInfo(string tag, string nativeName, string englishName)
        {
            Tag = tag;
            NativeName = nativeName;
            EnglishName = englishName;
        }
    }

    /// <summary>
    /// 翻訳カタログに登録されているscopeの表示補助情報。
    /// PreferencesなどのUIで、scopeの出所と既定ロケールを示すために使う。
    /// </summary>
    [Serializable]
    public sealed class EditorL10nScopeInfo
    {
        public string Scope { get; }
        public string DefaultLocale { get; }
        public string ManifestPath { get; }

        public EditorL10nScopeInfo(string scope, string defaultLocale, string manifestPath)
        {
            Scope = scope ?? "";
            DefaultLocale = defaultLocale ?? "";
            ManifestPath = manifestPath ?? "";
        }
    }
}
#endif
