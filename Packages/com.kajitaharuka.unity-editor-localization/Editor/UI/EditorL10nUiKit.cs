#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kajitaharuka.EditorLocalization
{
    /// <summary>状態バッジ/ピルの種別。色は状態にだけ持たせる。</summary>
    internal enum EditorL10nBadgeKind
    {
        Ok,
        Warning,
        Error,
        Neutral,
        Accent,
    }

    /// <summary>
    /// UnityEditorLocalization の Editor UI を組み立てる再利用部品のファクトリ。
    /// 各画面はこれらを「合成」することで、同じ概念が常に同じ見た目になり（一貫性）、
    /// 画面側のコードは薄く保たれる。EditorDesignTokens.uss と組で使う（ApplyTheme が名前で解決する）。
    /// 色はトークン経由でスキンごとに切り替わるため、両スキンで機能する。
    /// </summary>
    internal static class EditorL10nUiKit
    {
        private const string RootClass = "eui-root";
        private const string DarkClass = "eui-dark";
        private const string LightClass = "eui-light";
        private const string StyleSheetName = "EditorDesignTokens";

        private static StyleSheet _cachedStyleSheet;
        private static bool _searchedStyleSheet;

        /// <summary>共有スタイルシートとスキン別の配色クラスをルート要素へ適用する。</summary>
        internal static void ApplyTheme(VisualElement root)
        {
            if (root == null)
                return;

            root.AddToClassList(RootClass);
            root.EnableInClassList(DarkClass, EditorGUIUtility.isProSkin);
            root.EnableInClassList(LightClass, !EditorGUIUtility.isProSkin);

            var sheet = LoadStyleSheet();
            if (sheet != null && !root.styleSheets.Contains(sheet))
                root.styleSheets.Add(sheet);
        }

        private static StyleSheet LoadStyleSheet()
        {
            if (_searchedStyleSheet)
                return _cachedStyleSheet;

            _searchedStyleSheet = true;
            // パス/GUID をハードコードせず名前で解決する（アセットが移動しても堅牢）。
            foreach (var guid in AssetDatabase.FindAssets($"t:StyleSheet {StyleSheetName}"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) != StyleSheetName)
                    continue;
                var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (sheet != null)
                {
                    _cachedStyleSheet = sheet;
                    break;
                }
            }

            return _cachedStyleSheet;
        }

        /// <summary>
        /// 2 ゾーン構成のヘッダー。左（アイデンティティ）にタイトル＋サブタイトルと、この対象に
        /// 固有の状態（<paramref name="status"/>）を置き、右（汎用 chrome）にツール横断のメタ操作
        /// （<paramref name="chrome"/>）を渡した順に並べる。色は左の状態だけに持たせる。
        /// </summary>
        internal static VisualElement Header(string title, string subtitle, VisualElement status = null,
            params VisualElement[] chrome)
        {
            var header = new VisualElement();
            header.AddToClassList("eui-header");

            var texts = new VisualElement();
            texts.AddToClassList("eui-header__texts");

            var titleRow = new VisualElement();
            titleRow.AddToClassList("eui-header__title-row");
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("eui-header__title");
            titleRow.Add(titleLabel);
            if (status != null)
                titleRow.Add(status);
            texts.Add(titleRow);

            if (!string.IsNullOrEmpty(subtitle))
            {
                var subtitleLabel = new Label(subtitle);
                subtitleLabel.AddToClassList("eui-header__subtitle");
                subtitleLabel.name = "eui-header-subtitle";
                texts.Add(subtitleLabel);
            }

            var actions = new VisualElement();
            actions.AddToClassList("eui-header__actions");
            if (chrome != null)
            {
                foreach (var tool in chrome)
                {
                    if (tool != null)
                        actions.Add(tool);
                }
            }

            header.Add(texts);
            header.Add(actions);
            return header;
        }

        /// <summary>ヘッダーのアイデンティティ・ゾーンに置く状態バッジ（空・Neutral）。SetBadge で更新する。</summary>
        internal static Label StatusBadge()
        {
            var badge = new Label();
            badge.AddToClassList("eui-badge");
            SetBadge(badge, "", EditorL10nBadgeKind.Neutral);
            return badge;
        }

        /// <summary>バッジ/ピルの文言と種別を更新する。文言が空のときは非表示。</summary>
        internal static void SetBadge(Label badge, string text, EditorL10nBadgeKind kind)
        {
            if (badge == null)
                return;
            badge.text = text;
            badge.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
            badge.EnableInClassList("eui-badge--ok", kind == EditorL10nBadgeKind.Ok);
            badge.EnableInClassList("eui-badge--warn", kind == EditorL10nBadgeKind.Warning);
            badge.EnableInClassList("eui-badge--err", kind == EditorL10nBadgeKind.Error);
            badge.EnableInClassList("eui-badge--neutral", kind == EditorL10nBadgeKind.Neutral);
            badge.EnableInClassList("eui-badge--accent", kind == EditorL10nBadgeKind.Accent);
        }

        /// <summary>状態バッジ/ピル（初期文言・種別を指定）。リスト内の override/fallback 等に使う。</summary>
        internal static Label Pill(string text, EditorL10nBadgeKind kind)
        {
            var badge = new Label();
            badge.AddToClassList("eui-badge");
            SetBadge(badge, text, kind);
            return badge;
        }

        /// <summary>
        /// 汎用 chrome に置く、組み込みアイコンのみのメタ操作ボタン。アイコンのみなので用途は
        /// 必ず tooltip で補う。組み込みアイコンはスキン別画像が返るため tint しない。
        /// </summary>
        internal static Button IconLinkButton(string iconName, string tooltip, Action onClick)
        {
            var button = new Button(() => onClick?.Invoke()) { tooltip = tooltip };
            button.AddToClassList("eui-icon-link");

            var icon = new Image
            {
                image = EditorGUIUtility.IconContent(iconName).image,
                scaleMode = ScaleMode.ScaleToFit,
            };
            icon.AddToClassList("eui-icon-link__icon");
            icon.pickingMode = PickingMode.Ignore;
            button.Add(icon);
            return button;
        }

        /// <summary>オンラインドキュメントを開くアイコンボタン（汎用 chrome の代表例。`_Help` アイコン）。</summary>
        internal static Button DocButton(string url, string tooltip)
        {
            return IconLinkButton("_Help", tooltip, () => Application.OpenURL(url));
        }

        /// <summary>タイトル付きセクションカード。内容の追加先を out で返す。</summary>
        internal static VisualElement Section(string title, out VisualElement content)
        {
            var card = new VisualElement();
            card.AddToClassList("eui-section");

            var titleLabel = new Label(title);
            titleLabel.AddToClassList("eui-section__title");
            titleLabel.name = "eui-section-title";
            card.Add(titleLabel);

            content = new VisualElement();
            card.Add(content);
            return card;
        }

        /// <summary>タイトル無しのセクションカード。Foldout 等それ自体が見出しを持つ要素を囲うのに使う。</summary>
        internal static VisualElement Card()
        {
            var card = new VisualElement();
            card.AddToClassList("eui-section");
            return card;
        }

        /// <summary>永続表示の補足ノート（説明文。認識>想起・説明性）。</summary>
        internal static Label Note(string text)
        {
            var note = new Label(text);
            note.AddToClassList("eui-note");
            return note;
        }

        /// <summary>入力欄補足の常時ヒント行（プレースホルダ凡例など）。</summary>
        internal static Label HintRow(string text)
        {
            var hint = new Label(text);
            hint.AddToClassList("eui-hint");
            return hint;
        }

        /// <summary>主要操作と副次操作を分ける水平区切り線。</summary>
        internal static VisualElement Separator()
        {
            var separator = new VisualElement();
            separator.AddToClassList("eui-separator");
            return separator;
        }

        /// <summary>中立のアクションボタン（任意で tooltip）。ボタンは着色しないこと。</summary>
        internal static Button ActionButton(string text, Action onClick, string tooltip = null)
        {
            var button = new Button(() => onClick?.Invoke()) { text = text };
            if (!string.IsNullOrEmpty(tooltip))
                button.tooltip = tooltip;
            return button;
        }

        /// <summary>クリックできるアセット/パス行（例: 選択＋Ping）。長いパスは折り返す。</summary>
        internal static Button AssetRow(string label, string tooltip, Action onClick)
        {
            var button = new Button(() => onClick?.Invoke()) { text = InsertWrapOpportunities(label) };
            button.AddToClassList("eui-asset-row");
            if (!string.IsNullOrEmpty(tooltip))
                button.tooltip = tooltip;
            return button;
        }

        internal static HelpBox InfoBox(string text) => new HelpBox(text, HelpBoxMessageType.Info);
        internal static HelpBox WarningBox(string text) => new HelpBox(text, HelpBoxMessageType.Warning);
        internal static HelpBox ErrorBox(string text) => new HelpBox(text, HelpBoxMessageType.Error);

        /// <summary>
        /// 自作フィールドを Inspector のラベル幅整列システムに合わせ、周囲の DropdownField 等と揃える。
        /// </summary>
        internal static void AlignField<TValue>(BaseField<TValue> field)
        {
            field.AddToClassList(BaseField<TValue>.alignedFieldUssClassName);
        }

        /// <summary>
        /// パス区切りの直後にゼロ幅スペースを挿入し、スペースの無い長い文字列（パス/識別子）が
        /// 枠からはみ出さず折り返せるようにする。挿入文字は不可視なので表示文字列は変わらない。
        /// </summary>
        internal static string InsertWrapOpportunities(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var builder = new StringBuilder(text.Length * 2);
            foreach (var character in text)
            {
                builder.Append(character);
                if (character is '/' or '\\' or '_' or '-' or '.')
                    builder.Append('\u200B');
            }

            return builder.ToString();
        }
    }
}
#endif
