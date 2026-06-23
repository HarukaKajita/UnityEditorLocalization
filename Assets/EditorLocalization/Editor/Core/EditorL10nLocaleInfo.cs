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
}
#endif
