#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kajitaharuka.EditorLocalization
{
    /// <summary>
    /// UI Toolkitで作ったEditor UIをロケール変更に追従させるための小さなバインド集。
    /// </summary>
    public static class EditorL10nUi
    {
        // 部品自身の文言（カタログ外表記・無効理由など）を引く scope。パッケージ同梱カタログは
        // 常に存在するため、利用側 scope のカタログ状態に依存せず表示言語へ追従できる。
        private const string PackageUiScope = "com.kajitaharuka.unity-editor-localization";

        /// <summary>
        /// コンパクト言語選択メニューへ付与する既定のUSS class名。
        /// </summary>
        public const string CompactLocaleMenuUssClassName = "editor-l10n-compact-locale-menu";

        /// <summary>
        /// コンパクト言語選択メニューの先頭に表示する既定の視覚記号。
        /// </summary>
        public const string CompactLocaleMenuDefaultMarker = "A/文";

        /// <summary>「Powered by」クレジットの固定文言。ブランド名なので言語追従しない（全言語共通）。</summary>
        public const string AttributionText = "Powered by UnityEditorLocalization";

        /// <summary>
        /// 「Powered by UnityEditorLocalization」のクレジット要素（任意・歓迎）。クリックで製品ページを開く。
        /// 利用側ツールの About 行やフッターに置ける。文言は固定なので言語追従の購読は持たない。
        /// </summary>
        public static Button CreateAttribution()
        {
            var button = new Button(() => Application.OpenURL(EditorL10nDocs.DocumentationUrl))
            {
                text = AttributionText,
                tooltip = EditorL10nDocs.DocumentationUrl,
            };
            button.AddToClassList("eui-attribution");
            return button;
        }

        public static void BindText(Label label, string scope, string key, params object[] args)
        {
            if (label == null)
                return;

            void Apply()
            {
                label.text = EditorL10n.Tr(scope, key, args);
            }

            Apply();
            RegisterLocaleCallback(label, Apply);
        }

        public static void BindButton(Button button, string scope, string textKey, string tooltipKey = null, params object[] args)
        {
            if (button == null)
                return;

            void Apply()
            {
                button.text = EditorL10n.Tr(scope, textKey, args);
                if (!string.IsNullOrEmpty(tooltipKey))
                    button.tooltip = EditorL10n.Tr(scope, tooltipKey);
            }

            Apply();
            RegisterLocaleCallback(button, Apply);
        }

        public static void BindPropertyField(PropertyField field, string scope, string labelKey, string tooltipKey = null)
        {
            if (field == null)
                return;

            void Apply()
            {
                var label = EditorL10n.Tr(scope, labelKey);
                // 配列/リストはFoldoutで描画され、タイトルはBaseFieldのラベル(labelUssClassName)ではない。
                // そのためFoldout時はFoldout.textを更新する。スカラー時のみBaseFieldのラベルを更新し、
                // 配列の要素ラベル(Element 0等)を誤って書き換えないよう分岐する。
                var foldout = field.Q<Foldout>();
                if (foldout != null)
                {
                    foldout.text = label;
                }
                else
                {
                    var labelElement = field.Q<Label>(className: BaseField<string>.labelUssClassName);
                    if (labelElement != null)
                        labelElement.text = label;
                }
                if (!string.IsNullOrEmpty(tooltipKey))
                    field.tooltip = EditorL10n.Tr(scope, tooltipKey);
            }

            field.RegisterCallback<GeometryChangedEvent>(_ => Apply());
            Apply();
            RegisterLocaleCallback(field, Apply);
        }

        public static DropdownField CreateLocaleDropdown(string scope, string label)
        {
            static int FindLocaleIndex(EditorL10nLocaleInfo[] locales, string localeTag)
            {
                foreach (var candidate in EditorL10n.EnumerateLocaleAndParents(localeTag))
                {
                    var index = Array.FindIndex(locales, locale => locale.Tag == candidate);
                    if (index >= 0)
                        return index;
                }

                return -1;
            }

            var locales = EditorL10n.GetLocales(scope).ToArray();
            var choices = locales.Select(locale => locale.DisplayName).ToList();
            var activeLocale = EditorL10n.GetActiveLocale(scope);
            var activeIndex = FindLocaleIndex(locales, activeLocale);
            if (activeIndex < 0)
                activeIndex = 0;

            var dropdown = new DropdownField(label, choices, activeIndex);

            // Apply() による choices/value の再代入は value-changed を同期発火しうる
            // （SetValueWithoutNotify は choices 再代入をカバーしない）。ガード無しだと
            // カタログ再読込・言語変更のたびに過渡値で SetActiveLocale を書き戻す恐れがある。
            var applying = false;

            dropdown.RegisterValueChangedCallback(_ =>
            {
                if (applying) return;
                var currentLocales = EditorL10n.GetLocales(scope);
                var index = dropdown.index;
                if (index < 0 || index >= currentLocales.Count)
                    return;
                EditorL10n.SetActiveLocale(scope, currentLocales[index].Tag);
            });

            void Apply()
            {
                // 公開 API のため、choices 再代入の同期通知中に利用側コールバックが例外を投げても
                // ガードが立ちっぱなしにならないよう finally で必ず解除する。
                applying = true;
                try
                {
                    locales = EditorL10n.GetLocales(scope).ToArray();
                    choices = locales.Select(locale => locale.DisplayName).ToList();
                    dropdown.choices = choices;

                    var currentLocale = EditorL10n.GetActiveLocale(scope);
                    var index = FindLocaleIndex(locales, currentLocale);
                    // 候補に無い（登録済みカタログ外の）ロケールは空欄にせず、その旨を表示する
                    // （空白は「未設定」と誤読される。表記は Preferences と共通のキーを使う）。
                    dropdown.SetValueWithoutNotify(index >= 0
                        ? choices[index]
                        : string.IsNullOrEmpty(currentLocale)
                            ? ""
                            : EditorL10n.Tr(PackageUiScope, "outOfCatalog", currentLocale));
                }
                finally
                {
                    applying = false;
                }
            }

            Apply();
            RegisterLocaleCallback(dropdown, Apply);
            return dropdown;
        }

        /// <summary>
        /// Inspectorヘッダーやツールバーに置きやすい、短い表示の言語選択メニューを生成する。
        /// </summary>
        public static Button CreateCompactLocaleMenu(string scope, string tooltipLabel = null, string marker = CompactLocaleMenuDefaultMarker, bool showAttribution = true)
        {
            return CreateCompactLocaleMenu(scope, () => tooltipLabel, marker, showAttribution);
        }

        /// <summary>
        /// tooltipラベルを翻訳キーから取得する、短い表示の言語選択メニューを生成する。
        /// </summary>
        /// <param name="showAttribution">開いたメニューの末尾に「Powered by UnityEditorLocalization」を出すか（既定: 出す。任意で false にできる）。</param>
        public static Button CreateLocalizedCompactLocaleMenu(string scope, string tooltipLabelKey, string marker = CompactLocaleMenuDefaultMarker, bool showAttribution = true)
        {
            return CreateCompactLocaleMenu(scope, () => EditorL10n.Tr(scope, tooltipLabelKey), marker, showAttribution);
        }

        public static DropdownField CreateLocalizedLocaleDropdown(string scope, string labelKey)
        {
            var dropdown = CreateLocaleDropdown(scope, EditorL10n.Tr(scope, labelKey));

            void Apply()
            {
                dropdown.label = EditorL10n.Tr(scope, labelKey);
            }

            Apply();
            RegisterLocaleCallback(dropdown, Apply);
            return dropdown;
        }

        private static Button CreateCompactLocaleMenu(string scope, Func<string> tooltipLabelProvider, string marker, bool showAttribution)
        {
            var normalizedScope = scope ?? "";
            var button = new Button();
            button.AddToClassList(CompactLocaleMenuUssClassName);

            void Apply()
            {
                var locales = EditorL10n.GetLocales(normalizedScope);
                var locale = ActiveLocaleInfo(normalizedScope, locales);
                var label = CompactLocaleLabel(locale);
                var displayName = locale?.DisplayName ?? EditorL10n.GetActiveLocale(normalizedScope);
                button.text = BuildCompactLocaleMenuText(marker, label);
                // 無効時（カタログ未登録）は通常の説明でなく「なぜ押せないか」を出す。
                // tooltip の管理元はこの Apply に一本化されているため上書き合戦は起きない。
                button.tooltip = locales.Count > 0
                    ? BuildLocaleTooltip(tooltipLabelProvider?.Invoke(), displayName)
                    : EditorL10n.Tr(PackageUiScope, "menu.noCatalog.tooltip");
                button.SetEnabled(locales.Count > 0);
            }

            button.clicked += () =>
            {
                var locales = EditorL10n.GetLocales(normalizedScope);
                if (locales.Count == 0)
                    return;

                var menu = new GenericMenu();
                var activeLocale = EditorL10n.GetActiveLocale(normalizedScope);
                foreach (var locale in locales)
                {
                    var selectedLocale = locale;
                    menu.AddItem(
                        new GUIContent(locale.DisplayName),
                        selectedLocale.Tag == activeLocale,
                        () => EditorL10n.SetActiveLocale(normalizedScope, selectedLocale.Tag));
                }

                // 開いたメニューの末尾に控えめなクレジット（任意・既定オン）。クリックで製品ページを開く。
                if (showAttribution)
                {
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent(AttributionText), false, () => Application.OpenURL(EditorL10nDocs.DocumentationUrl));
                }

                menu.ShowAsContext();
            };

            Apply();
            RegisterLocaleCallback(button, Apply);
            return button;
        }

        private static EditorL10nLocaleInfo ActiveLocaleInfo(string scope, IReadOnlyList<EditorL10nLocaleInfo> locales)
        {
            var activeLocale = EditorL10n.GetActiveLocale(scope);
            foreach (var locale in locales)
            {
                if (locale.Tag == activeLocale)
                    return locale;
            }

            return null;
        }

        private static string CompactLocaleLabel(EditorL10nLocaleInfo locale)
        {
            if (locale == null)
                return "";
            if (!string.IsNullOrEmpty(locale.NativeName))
                return locale.NativeName;
            if (!string.IsNullOrEmpty(locale.EnglishName))
                return locale.EnglishName;
            return locale.Tag;
        }

        private static string BuildCompactLocaleMenuText(string marker, string localeLabel)
        {
            var label = string.IsNullOrEmpty(localeLabel) ? "Lang" : localeLabel;
            return string.IsNullOrEmpty(marker)
                ? label + " ▾"
                : marker + " " + label + " ▾";
        }

        private static string BuildLocaleTooltip(string tooltipLabel, string localeDisplayName)
        {
            if (string.IsNullOrEmpty(localeDisplayName))
                return tooltipLabel ?? "";
            return string.IsNullOrEmpty(tooltipLabel)
                ? localeDisplayName
                : tooltipLabel + ": " + localeDisplayName;
        }

        public static void RegisterLocaleCallback(VisualElement element, Action callback)
        {
            if (element == null || callback == null)
                return;

            var subscribed = false;

            void Subscribe()
            {
                if (subscribed)
                    return;

                EditorL10n.LocaleChanged += callback;
                subscribed = true;
            }

            void Unsubscribe()
            {
                if (!subscribed)
                    return;

                EditorL10n.LocaleChanged -= callback;
                subscribed = false;
            }

            element.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                Subscribe();
                callback();
            });
            element.RegisterCallback<DetachFromPanelEvent>(_ => Unsubscribe());

            if (element.panel != null)
                Subscribe();
        }
    }
}
#endif
