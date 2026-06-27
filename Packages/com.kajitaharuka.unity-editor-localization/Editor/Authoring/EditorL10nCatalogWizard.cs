#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kajitaharuka.EditorLocalization
{
    /// <summary>
    /// 利用側拡張の l10n カタログ（manifest ＋空の翻訳テーブル）の雛形を生成するウィザード。
    /// 手書き JSON の手間を無くし、最小構成をすぐ用意できるようにする。文言はパッケージ自身の
    /// 翻訳カタログから引き（ドッグフーディング）、表示言語に追従する。
    /// </summary>
    internal sealed class EditorL10nCatalogWizard : EditorWindow
    {
        private const string UiScope = "com.kajitaharuka.unity-editor-localization";
        private static string Tr(string key, params object[] args) => EditorL10n.Tr(UiScope, key, args);

        // 既知タグの表示名（manifest の nativeName/englishName に使う）。未知タグはタグ自身を名前にする。
        private static readonly Dictionary<string, (string native, string english)> KnownLocaleNames = new()
        {
            ["en"] = ("English", "English"),
            ["ja"] = ("日本語", "Japanese"),
            ["zh-Hans"] = ("简体中文", "Simplified Chinese"),
            ["zh-Hant"] = ("繁體中文", "Traditional Chinese"),
            ["ko"] = ("한국어", "Korean"),
            ["fr"] = ("Français", "French"),
            ["de"] = ("Deutsch", "German"),
            ["it"] = ("Italiano", "Italian"),
            ["es-ES"] = ("Español (España)", "Spanish (Spain)"),
            ["es-419"] = ("Español (Latinoamérica)", "Spanish (Latin America)"),
            ["pt-BR"] = ("Português (Brasil)", "Portuguese (Brazil)"),
            ["pt-PT"] = ("Português (Portugal)", "Portuguese (Portugal)"),
            ["ru"] = ("Русский", "Russian"),
            ["pl"] = ("Polski", "Polish"),
            ["tr"] = ("Türkçe", "Turkish"),
            ["th"] = ("ไทย", "Thai"),
            ["vi"] = ("Tiếng Việt", "Vietnamese"),
            ["uk"] = ("Українська", "Ukrainian"),
            ["id"] = ("Bahasa Indonesia", "Indonesian"),
        };

        private TextField _scope;
        private ObjectField _folder;
        private TextField _defaultLocale;
        private TextField _locales;
        private Label _sectionTitle;
        private Label _subtitle;
        private Label _localesHint;
        private Button _create;
        private HelpBox _message;

        [MenuItem("Tools/UnityEditorLocalization/Create Catalog", priority = 1)]
        private static void Open()
        {
            var window = GetWindow<EditorL10nCatalogWizard>(true);
            window.titleContent = new GUIContent(Tr("wizard.title"));
            window.minSize = new Vector2(440, 320);
            window.Show();
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            EditorL10nUiKit.ApplyTheme(root);

            _subtitle = null;
            var header = EditorL10nUiKit.Header("UnityEditorLocalization", Tr("wizard.subtitle"));
            _subtitle = header.Q<Label>("eui-header-subtitle");
            root.Add(header);

            var card = EditorL10nUiKit.Section(Tr("wizard.title"), out var content);
            _sectionTitle = card.Q<Label>(className: "eui-section__title");

            _scope = new TextField(Tr("wizard.scope.label"));
            EditorL10nUiKit.AlignField(_scope);
            content.Add(_scope);

            _folder = new ObjectField(Tr("wizard.folder.label")) { objectType = typeof(DefaultAsset), allowSceneObjects = false };
            EditorL10nUiKit.AlignField(_folder);
            // Project で選択中のフォルダがあれば初期値にする。
            var selectedPath = Selection.activeObject != null ? AssetDatabase.GetAssetPath(Selection.activeObject) : "";
            if (!string.IsNullOrEmpty(selectedPath) && AssetDatabase.IsValidFolder(selectedPath))
                _folder.value = Selection.activeObject;
            content.Add(_folder);

            _defaultLocale = new TextField(Tr("wizard.defaultLocale.label")) { value = "en" };
            EditorL10nUiKit.AlignField(_defaultLocale);
            content.Add(_defaultLocale);

            _locales = new TextField(Tr("wizard.locales.label")) { value = "en ja" };
            EditorL10nUiKit.AlignField(_locales);
            content.Add(_locales);

            _localesHint = EditorL10nUiKit.HintRow(Tr("wizard.locales.hint"));
            content.Add(_localesHint);

            _create = EditorL10nUiKit.ActionButton(Tr("wizard.create"), OnCreate);
            content.Add(_create);

            _message = new HelpBox("", HelpBoxMessageType.Info);
            _message.style.display = DisplayStyle.None;
            content.Add(_message);

            root.Add(card);

            ApplyTexts();
            // 表示言語の変更に追従（静的ラベルの再翻訳）。root へ紐付けて閉じる際に購読解除される。
            EditorL10nUi.RegisterLocaleCallback(root, ApplyTexts);
        }

        // Tr 由来の静的文言を（再）適用する。言語変更時に呼ばれる。
        private void ApplyTexts()
        {
            titleContent = new GUIContent(Tr("wizard.title"));
            if (_subtitle != null) _subtitle.text = Tr("wizard.subtitle");
            if (_sectionTitle != null) _sectionTitle.text = Tr("wizard.title");
            if (_scope != null) _scope.label = Tr("wizard.scope.label");
            if (_folder != null) _folder.label = Tr("wizard.folder.label");
            if (_defaultLocale != null) _defaultLocale.label = Tr("wizard.defaultLocale.label");
            if (_locales != null) _locales.label = Tr("wizard.locales.label");
            if (_localesHint != null) _localesHint.text = Tr("wizard.locales.hint");
            if (_create != null) _create.text = Tr("wizard.create");
        }

        private void OnCreate()
        {
            var scope = (_scope.value ?? "").Trim();
            if (string.IsNullOrEmpty(scope)) { ShowMessage("wizard.error.scopeEmpty", HelpBoxMessageType.Error); return; }
            if (EditorL10n.GetScopes().Contains(scope)) { ShowMessage("wizard.error.scopeDuplicate", HelpBoxMessageType.Error); return; }

            var folderPath = _folder.value != null ? AssetDatabase.GetAssetPath(_folder.value) : "";
            if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
            { ShowMessage("wizard.error.folderInvalid", HelpBoxMessageType.Error); return; }

            var defaultLocale = EditorL10n.NormalizeLocaleTag((_defaultLocale.value ?? "").Trim());
            if (string.IsNullOrEmpty(defaultLocale)) { ShowMessage("wizard.error.defaultLocaleEmpty", HelpBoxMessageType.Error); return; }

            var tags = ParseLocales(_locales.value, defaultLocale);

            var manifestName = SanitizeFileName(scope.Split('.').LastOrDefault());
            if (string.IsNullOrEmpty(manifestName)) manifestName = "catalog";
            var assetManifestPath = folderPath + "/" + manifestName + ".l10n-manifest.json";

            // ローカル/組み込みどのパッケージでも正しく書けるよう物理パスで扱う。
            var physicalFolder = FileUtil.GetPhysicalPath(folderPath);
            var physicalLocales = physicalFolder + "/Locales";
            var physicalManifest = physicalFolder + "/" + manifestName + ".l10n-manifest.json";

            // manifest だけでなく各テーブルの出力先も先に存在チェックし、既存ファイルを絶対に上書きしない。
            var targets = new List<string> { physicalManifest };
            foreach (var tag in tags)
                targets.Add(physicalLocales + "/" + tag + ".json");
            if (targets.Any(File.Exists))
            {
                ShowMessage("wizard.error.alreadyExists", HelpBoxMessageType.Error);
                return;
            }

            var document = new EditorL10nManifestDocument
            {
                scope = scope,
                defaultLocale = defaultLocale,
                fixedTerms = Array.Empty<string>(),
                locales = tags.Select(tag =>
                {
                    var names = NameFor(tag);
                    return new EditorL10nManifestLocale
                    {
                        tag = tag,
                        nativeName = names.native,
                        englishName = names.english,
                        tablePath = "Locales/" + tag + ".json",
                    };
                }).ToArray(),
            };

            var written = new List<string>();
            try
            {
                Directory.CreateDirectory(physicalLocales);
                File.WriteAllText(physicalManifest, EditorL10nCatalogWriter.WriteManifest(document));
                written.Add(physicalManifest);
                foreach (var tag in tags)
                {
                    var path = physicalLocales + "/" + tag + ".json";
                    File.WriteAllText(path, EditorL10nCatalogWriter.WriteTable(tag, Array.Empty<KeyValuePair<string, string>>()));
                    written.Add(path);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"EditorLocalization: カタログ生成に失敗しました: {exception}");
                // 途中失敗で半端なカタログを残さないよう、書いたファイルを巻き戻す。
                foreach (var path in written)
                    try { File.Delete(path); } catch { /* 巻き戻し失敗は握りつぶす */ }
                ShowMessage("wizard.error.writeFailed", HelpBoxMessageType.Error);
                return;
            }

            AssetDatabase.Refresh();
            EditorL10n.Reload();

            var manifestAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetManifestPath);
            if (manifestAsset != null)
            {
                Selection.activeObject = manifestAsset;
                EditorGUIUtility.PingObject(manifestAsset);
            }

            _message.text = Tr("wizard.result.created", assetManifestPath);
            _message.messageType = HelpBoxMessageType.Info;
            _message.style.display = DisplayStyle.Flex;
        }

        // 入力タグ列（空白/カンマ区切り）を正規化・重複排除し、defaultLocale を先頭に必ず含める。
        private static List<string> ParseLocales(string raw, string defaultLocale)
        {
            var result = new List<string> { defaultLocale };
            foreach (var token in (raw ?? "").Split(new[] { ' ', ',', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var tag = EditorL10n.NormalizeLocaleTag(token.Trim());
                if (!string.IsNullOrEmpty(tag) && !result.Contains(tag))
                    result.Add(tag);
            }

            return result;
        }

        private static (string native, string english) NameFor(string tag)
        {
            return KnownLocaleNames.TryGetValue(tag, out var names) ? names : (tag, tag);
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            foreach (var invalid in Path.GetInvalidFileNameChars())
                name = name.Replace(invalid, '-');
            return name;
        }

        private void ShowMessage(string key, HelpBoxMessageType type)
        {
            _message.text = Tr(key);
            _message.messageType = type;
            _message.style.display = DisplayStyle.Flex;
        }
    }
}
#endif
