#if UNITY_EDITOR
namespace Kajitaharuka.EditorLocalization
{
    /// <summary>
    /// オンラインドキュメント等のURLを一元管理する。
    /// 製品ページのベースを一箇所に集約し、ページ未公開でもここの差し替えだけで遷移先を運用できる。
    /// package.json の documentationUrl と一致させること。
    /// </summary>
    internal static class EditorL10nDocs
    {
        /// <summary>製品ページ（オンラインドキュメント）のURL。ヘッダーのドキュメントボタンが開く。</summary>
        internal const string DocumentationUrl = "https://kajitaharuka.com/products/unity-editor-localization/";
    }
}
#endif
