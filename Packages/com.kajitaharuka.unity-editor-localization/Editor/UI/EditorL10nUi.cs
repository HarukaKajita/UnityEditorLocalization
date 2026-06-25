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
        /// <summary>
        /// コンパクト言語選択メニューへ付与する既定のUSS class名。
        /// </summary>
        public const string CompactLocaleMenuUssClassName = "editor-l10n-compact-locale-menu";

        /// <summary>
        /// コンパクト言語選択メニューの先頭に表示する既定の視覚記号。
        /// </summary>
        public const string CompactLocaleMenuDefaultMarker = "A/文";

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
            dropdown.RegisterValueChangedCallback(_ =>
            {
                var currentLocales = EditorL10n.GetLocales(scope);
                var index = dropdown.index;
                if (index < 0 || index >= currentLocales.Count)
                    return;
                EditorL10n.SetActiveLocale(scope, currentLocales[index].Tag);
            });

            void Apply()
            {
                locales = EditorL10n.GetLocales(scope).ToArray();
                choices = locales.Select(locale => locale.DisplayName).ToList();
                dropdown.choices = choices;

                var currentLocale = EditorL10n.GetActiveLocale(scope);
                var index = FindLocaleIndex(locales, currentLocale);
                dropdown.SetValueWithoutNotify(index >= 0 ? choices[index] : "");
            }

            Apply();
            RegisterLocaleCallback(dropdown, Apply);
            return dropdown;
        }

        /// <summary>
        /// Inspectorヘッダーやツールバーに置きやすい、短い表示の言語選択メニューを生成する。
        /// </summary>
        public static Button CreateCompactLocaleMenu(string scope, string tooltipLabel = null, string marker = CompactLocaleMenuDefaultMarker)
        {
            return CreateCompactLocaleMenu(scope, () => tooltipLabel, marker);
        }

        /// <summary>
        /// tooltipラベルを翻訳キーから取得する、短い表示の言語選択メニューを生成する。
        /// </summary>
        public static Button CreateLocalizedCompactLocaleMenu(string scope, string tooltipLabelKey, string marker = CompactLocaleMenuDefaultMarker)
        {
            return CreateCompactLocaleMenu(scope, () => EditorL10n.Tr(scope, tooltipLabelKey), marker);
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

        private static Button CreateCompactLocaleMenu(string scope, Func<string> tooltipLabelProvider, string marker)
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
                button.tooltip = BuildLocaleTooltip(tooltipLabelProvider?.Invoke(), displayName);
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
