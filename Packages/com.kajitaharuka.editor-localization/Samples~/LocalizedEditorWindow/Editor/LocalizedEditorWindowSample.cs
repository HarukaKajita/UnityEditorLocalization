#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Kajitaharuka.EditorLocalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kajitaharuka.EditorLocalization.Samples.LocalizedEditorWindow
{
    public sealed class LocalizedEditorWindowSample : EditorWindow
    {
        private const string Scope = "com.kajitaharuka.editor-localization.samples.localized-window";

        private static readonly string[] DemoModeKeys =
        {
            "demo.mode.design",
            "demo.mode.docs",
            "demo.mode.validation"
        };

        private static readonly string[] DemoSummaryKeys =
        {
            "demo.mode.design.summary",
            "demo.mode.docs.summary",
            "demo.mode.validation.summary"
        };

        private Label _statusLabel;
        private Label _demoSummaryLabel;
        private DropdownField _demoDropdown;
        private TextField _sampleTextField;
        private Label _placeholderFallbackLabel;
        private string _statusKey = "status.initial";
        private object[] _statusArgs = Array.Empty<object>();
        private int _selectedDemoModeIndex;

        [MenuItem("Tools/UnityEditorLocalization/Samples/Localized Window")]
        private static void Open()
        {
            var window = GetWindow<LocalizedEditorWindowSample>();
            window.titleContent = new GUIContent(EditorL10n.Tr(Scope, "window.title"));
            window.minSize = new Vector2(360f, 440f);
            window.Show();
        }

        private void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexGrow = 1f;
            rootVisualElement.style.paddingLeft = 12f;
            rootVisualElement.style.paddingRight = 12f;
            rootVisualElement.style.paddingTop = 12f;
            rootVisualElement.style.paddingBottom = 12f;

            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1f;
            scrollView.style.flexShrink = 1f;
            rootVisualElement.Add(scrollView);

            scrollView.Add(CreateHeader());
            scrollView.Add(CreateOverviewSection());
            scrollView.Add(CreateDemoSection());
            scrollView.Add(CreateCatalogSection());
            scrollView.Add(CreateValidationSection());
            scrollView.Add(CreateDetailsSection());

            EditorL10nUi.RegisterLocaleCallback(rootVisualElement, ApplyWindowTitle);
            ApplyWindowTitle();
        }

        private VisualElement CreateHeader()
        {
            var container = new VisualElement();
            container.style.marginBottom = 10f;

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 4f;
            container.Add(row);

            var title = CreateWrappedLabel();
            title.style.flexGrow = 1f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 15f;
            EditorL10nUi.BindText(title, Scope, "header.title");
            row.Add(title);

            var compactMenu = EditorL10nUi.CreateLocalizedCompactLocaleMenu(Scope, "locale.compact.tooltip");
            compactMenu.style.flexShrink = 0f;
            compactMenu.style.marginLeft = 8f;
            row.Add(compactMenu);

            var subtitle = CreateWrappedLabel();
            subtitle.style.opacity = 0.78f;
            EditorL10nUi.BindText(subtitle, Scope, "header.subtitle");
            container.Add(subtitle);

            var localeDropdown = EditorL10nUi.CreateLocalizedLocaleDropdown(Scope, "locale.dropdown.label");
            localeDropdown.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            localeDropdown.style.marginTop = 8f;
            EditorL10nUi.RegisterLocaleCallback(localeDropdown, () =>
            {
                localeDropdown.tooltip = EditorL10n.Tr(Scope, "locale.dropdown.tooltip");
            });
            container.Add(localeDropdown);

            return container;
        }

        private VisualElement CreateOverviewSection()
        {
            var section = CreateSection("overview.section.title");
            var helpBox = new HelpBox("", HelpBoxMessageType.Info);
            helpBox.style.whiteSpace = WhiteSpace.Normal;
            helpBox.style.flexShrink = 1f;
            EditorL10nUi.RegisterLocaleCallback(helpBox, () =>
            {
                helpBox.text = EditorL10n.Tr(Scope, "overview.help");
                helpBox.tooltip = EditorL10n.Tr(Scope, "overview.tooltip");
            });
            section.Add(helpBox);
            return section;
        }

        private VisualElement CreateDemoSection()
        {
            var section = CreateSection("demo.section.title");

            var intro = CreateWrappedLabel();
            EditorL10nUi.BindText(intro, Scope, "demo.intro");
            section.Add(intro);

            _demoDropdown = new DropdownField();
            _demoDropdown.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            _demoDropdown.RegisterValueChangedCallback(_ =>
            {
                _selectedDemoModeIndex = Mathf.Clamp(_demoDropdown.index, 0, DemoModeKeys.Length - 1);
                ApplyDemoSummary();
            });
            EditorL10nUi.RegisterLocaleCallback(_demoDropdown, ApplyDemoDropdown);
            section.Add(_demoDropdown);

            _demoSummaryLabel = CreateWrappedLabel();
            _demoSummaryLabel.style.marginTop = 2f;
            _demoSummaryLabel.style.marginBottom = 8f;
            EditorL10nUi.RegisterLocaleCallback(_demoSummaryLabel, ApplyDemoSummary);
            section.Add(_demoSummaryLabel);

            _sampleTextField = new TextField();
            _sampleTextField.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            _sampleTextField.isDelayed = true;
            _sampleTextField.style.marginTop = 4f;
            section.Add(_sampleTextField);

            _placeholderFallbackLabel = CreateWrappedLabel();
            _placeholderFallbackLabel.style.opacity = 0.72f;
            _placeholderFallbackLabel.style.marginTop = 0f;
            _placeholderFallbackLabel.style.marginBottom = 8f;
            section.Add(_placeholderFallbackLabel);

            EditorL10nUi.RegisterLocaleCallback(_sampleTextField, ApplyInputCopy);

            var progressBar = new ProgressBar
            {
                lowValue = 0f,
                highValue = 100f,
                value = 68f
            };
            progressBar.style.marginTop = 4f;
            progressBar.style.marginBottom = 8f;
            EditorL10nUi.RegisterLocaleCallback(progressBar, () =>
            {
                progressBar.title = EditorL10n.Tr(Scope, "demo.progress.title", (int)progressBar.value);
                progressBar.tooltip = EditorL10n.Tr(Scope, "demo.progress.tooltip");
            });
            section.Add(progressBar);

            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.flexWrap = Wrap.Wrap;
            buttonRow.style.marginTop = 4f;
            section.Add(buttonRow);

            var reloadButton = new Button(() =>
            {
                EditorL10n.Reload();
                SetStatus("status.reloaded", DateTime.Now.ToString("HH:mm:ss"));
            });
            reloadButton.style.marginRight = 6f;
            reloadButton.style.marginBottom = 6f;
            EditorL10nUi.BindButton(reloadButton, Scope, "demo.button.reload.text", "demo.button.reload.tooltip");
            buttonRow.Add(reloadButton);

            var validateButton = new Button(() =>
            {
                var result = EditorL10nValidator.ValidateAll();
                SetStatus("status.validated", result.Errors.Count, result.Warnings.Count);
            });
            validateButton.style.marginRight = 6f;
            validateButton.style.marginBottom = 6f;
            EditorL10nUi.BindButton(validateButton, Scope, "demo.button.validate.text", "demo.button.validate.tooltip");
            buttonRow.Add(validateButton);

            _statusLabel = CreateWrappedLabel();
            _statusLabel.style.marginTop = 2f;
            _statusLabel.style.opacity = 0.82f;
            EditorL10nUi.RegisterLocaleCallback(_statusLabel, ApplyStatus);
            section.Add(_statusLabel);

            ApplyDemoDropdown();
            ApplyDemoSummary();
            ApplyInputCopy();
            ApplyStatus();

            return section;
        }

        private VisualElement CreateCatalogSection()
        {
            var section = CreateSection("catalog.section.title");
            section.Add(CreateInfoBlock("catalog.scope.label", Scope, "catalog.scope.tooltip"));
            section.Add(CreateInfoBlock("catalog.keys.label", () => EditorL10n.Tr(Scope, "catalog.keys.value")));
            section.Add(CreateInfoBlock("catalog.defaultLocale.label", () =>
            {
                return EditorL10n.TryGetScopeInfo(Scope, out var info) ? info.DefaultLocale : "ja";
            }));
            section.Add(CreateInfoBlock("catalog.manifest.label", () =>
            {
                return EditorL10n.TryGetScopeInfo(Scope, out var info)
                    ? info.ManifestPath
                    : EditorL10n.Tr(Scope, "catalog.manifest.notFound");
            }));
            section.Add(CreateInfoBlock("catalog.localeJson.label", "Editor/Localization/Locales/ja.json / en.json"));
            section.Add(CreateInfoBlock("catalog.fallback.label", () => EditorL10n.Tr(Scope, "catalog.fallback.value")));
            return section;
        }

        private VisualElement CreateValidationSection()
        {
            var section = CreateSection("validation.section.title");
            var helpBox = new HelpBox("", HelpBoxMessageType.None);
            helpBox.style.whiteSpace = WhiteSpace.Normal;
            helpBox.style.flexShrink = 1f;
            EditorL10nUi.RegisterLocaleCallback(helpBox, () =>
            {
                helpBox.text = EditorL10n.Tr(Scope, "validation.help");
            });
            section.Add(helpBox);
            section.Add(CreateInfoBlock("quality.script.label", "Packages/com.kajitaharuka.editor-localization/skills/editor-localization-translation-quality/scripts/validate_locale_quality.py"));
            return section;
        }

        private VisualElement CreateDetailsSection()
        {
            var section = CreateSection();
            var foldout = new Foldout
            {
                value = true
            };
            foldout.style.whiteSpace = WhiteSpace.Normal;
            EditorL10nUi.RegisterLocaleCallback(foldout, () =>
            {
                foldout.text = EditorL10n.Tr(Scope, "details.foldout.title");
                foldout.tooltip = EditorL10n.Tr(Scope, "details.foldout.tooltip");
            });

            foreach (var key in new[]
            {
                "details.manifest",
                "details.localeJson",
                "details.scopeKeys",
                "details.fallback",
                "details.quality"
            })
            {
                var label = CreateWrappedLabel();
                label.style.marginTop = 4f;
                EditorL10nUi.BindText(label, Scope, key);
                foldout.Add(label);
            }

            section.Add(foldout);
            return section;
        }

        private VisualElement CreateSection(string titleKey = null)
        {
            var section = new VisualElement();
            section.style.backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.22f, 0.22f, 1f)
                : new Color(0.88f, 0.88f, 0.88f, 1f);
            section.style.borderTopColor = BorderColor();
            section.style.borderRightColor = BorderColor();
            section.style.borderBottomColor = BorderColor();
            section.style.borderLeftColor = BorderColor();
            section.style.borderTopWidth = 1f;
            section.style.borderRightWidth = 1f;
            section.style.borderBottomWidth = 1f;
            section.style.borderLeftWidth = 1f;
            section.style.borderTopLeftRadius = 4f;
            section.style.borderTopRightRadius = 4f;
            section.style.borderBottomLeftRadius = 4f;
            section.style.borderBottomRightRadius = 4f;
            section.style.paddingLeft = 10f;
            section.style.paddingRight = 10f;
            section.style.paddingTop = 10f;
            section.style.paddingBottom = 10f;
            section.style.marginBottom = 10f;

            if (!string.IsNullOrEmpty(titleKey))
            {
                var title = CreateWrappedLabel();
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                title.style.marginBottom = 6f;
                EditorL10nUi.BindText(title, Scope, titleKey);
                section.Add(title);
            }

            return section;
        }

        private VisualElement CreateInfoBlock(string labelKey, string value, string tooltipKey = null)
        {
            return CreateInfoBlock(labelKey, () => value, tooltipKey);
        }

        private VisualElement CreateInfoBlock(string labelKey, Func<string> valueProvider, string tooltipKey = null)
        {
            var block = new VisualElement();
            block.style.marginBottom = 8f;

            var label = CreateWrappedLabel();
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 1f;
            block.Add(label);

            var valueLabel = CreateWrappedLabel();
            valueLabel.style.opacity = 0.82f;
            block.Add(valueLabel);

            EditorL10nUi.RegisterLocaleCallback(block, () =>
            {
                label.text = EditorL10n.Tr(Scope, labelKey);
                valueLabel.text = InsertWrapOpportunities(valueProvider?.Invoke() ?? "");
                if (!string.IsNullOrEmpty(tooltipKey))
                    block.tooltip = EditorL10n.Tr(Scope, tooltipKey);
            });

            return block;
        }

        private void ApplyWindowTitle()
        {
            titleContent = new GUIContent(EditorL10n.Tr(Scope, "window.title"));
        }

        private void ApplyDemoDropdown()
        {
            if (_demoDropdown == null)
                return;

            var choices = new List<string>(DemoModeKeys.Length);
            foreach (var key in DemoModeKeys)
                choices.Add(EditorL10n.Tr(Scope, key));

            _demoDropdown.label = EditorL10n.Tr(Scope, "demo.dropdown.label");
            _demoDropdown.tooltip = EditorL10n.Tr(Scope, "demo.dropdown.tooltip");
            _demoDropdown.choices = choices;
            _selectedDemoModeIndex = Mathf.Clamp(_selectedDemoModeIndex, 0, choices.Count - 1);
            _demoDropdown.index = _selectedDemoModeIndex;
            _demoDropdown.SetValueWithoutNotify(choices[_selectedDemoModeIndex]);
        }

        private void ApplyDemoSummary()
        {
            if (_demoSummaryLabel == null)
                return;

            _selectedDemoModeIndex = Mathf.Clamp(_selectedDemoModeIndex, 0, DemoSummaryKeys.Length - 1);
            _demoSummaryLabel.text = EditorL10n.Tr(Scope, DemoSummaryKeys[_selectedDemoModeIndex]);
        }

        private void ApplyInputCopy()
        {
            if (_sampleTextField == null || _placeholderFallbackLabel == null)
                return;

            _sampleTextField.label = EditorL10n.Tr(Scope, "demo.input.label");
            _sampleTextField.tooltip = EditorL10n.Tr(Scope, "demo.input.tooltip");

            var placeholder = EditorL10n.Tr(Scope, "demo.input.placeholder");
            if (!TrySetTextFieldPlaceholder(_sampleTextField, placeholder))
                _placeholderFallbackLabel.text = EditorL10n.Tr(Scope, "demo.input.placeholder.caption", placeholder);
            else
                _placeholderFallbackLabel.text = "";
        }

        private void SetStatus(string key, params object[] args)
        {
            _statusKey = key;
            _statusArgs = args ?? Array.Empty<object>();
            ApplyStatus();
        }

        private void ApplyStatus()
        {
            if (_statusLabel != null)
                _statusLabel.text = EditorL10n.Tr(Scope, _statusKey, _statusArgs);
        }

        private static bool TrySetTextFieldPlaceholder(TextField textField, string placeholder)
        {
            // Unityのバージョン差を避けるため、placeholder APIは反射で存在確認してから使う。
            var textEditionProperty = typeof(TextField).GetProperty("textEdition", BindingFlags.Instance | BindingFlags.Public);
            var textEdition = textEditionProperty?.GetValue(textField);
            if (textEdition == null)
                return false;

            var placeholderProperty = textEdition.GetType().GetProperty("placeholder", BindingFlags.Instance | BindingFlags.Public);
            if (placeholderProperty == null || !placeholderProperty.CanWrite)
                return false;

            placeholderProperty.SetValue(textEdition, placeholder);
            return true;
        }

        private static Label CreateWrappedLabel(string text = "")
        {
            var label = new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.flexShrink = 1f;
            return label;
        }

        private static Color BorderColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.30f, 0.30f, 0.30f, 1f)
                : new Color(0.72f, 0.72f, 0.72f, 1f);
        }

        private static string InsertWrapOpportunities(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value
                .Replace("/", "/\u200B")
                .Replace("\\", "\\\u200B")
                .Replace(".", ".\u200B")
                .Replace("_", "_\u200B")
                .Replace("-", "-\u200B");
        }
    }
}
#endif
