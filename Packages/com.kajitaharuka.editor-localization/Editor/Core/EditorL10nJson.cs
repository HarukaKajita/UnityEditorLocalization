#if UNITY_EDITOR
using System;

namespace Kajitaharuka.EditorLocalization
{
    [Serializable]
    internal sealed class EditorL10nManifestDocument
    {
        public string scope;
        public string defaultLocale;
        public EditorL10nManifestLocale[] locales = Array.Empty<EditorL10nManifestLocale>();
    }

    [Serializable]
    internal sealed class EditorL10nManifestLocale
    {
        public string tag;
        public string nativeName;
        public string englishName;
        public string tablePath;
    }

    [Serializable]
    internal sealed class EditorL10nTableDocument
    {
        public string locale;
        public EditorL10nEntry[] entries = Array.Empty<EditorL10nEntry>();
    }

    [Serializable]
    internal sealed class EditorL10nEntry
    {
        public string key;
        public string value;
    }
}
#endif
