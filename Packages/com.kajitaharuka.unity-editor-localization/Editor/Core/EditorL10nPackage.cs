#if UNITY_EDITOR
namespace Kajitaharuka.EditorLocalization
{
    /// <summary>
    /// パッケージ自身の識別子の単一定義。パッケージ名は「自身の翻訳カタログの scope 名」
    /// 「Package Manager 上のパッケージ名」を兼ねるため、リテラルを各ファイルへ重複させず
    /// ここだけで持つ（リネーム・fork 時の変更漏れを防ぐ）。
    /// </summary>
    internal static class EditorL10nPackage
    {
        /// <summary>パッケージ名（= 自身の翻訳カタログの scope。manifest の scope と一致させること）。</summary>
        internal const string Name = "com.kajitaharuka.unity-editor-localization";
    }
}
#endif
