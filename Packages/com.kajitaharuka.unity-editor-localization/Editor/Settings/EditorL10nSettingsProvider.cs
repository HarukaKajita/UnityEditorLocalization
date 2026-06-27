#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kajitaharuka.EditorLocalization
{
    /// <summary>
    /// Preferences > UnityEditorLocalization の設定画面。UI Toolkit で構築し、2 ゾーンヘッダー
    /// （左=アイデンティティ＋概況、右=ドキュメント等の汎用 chrome）、表示言語（グローバル）、
    /// scope 個別設定、開発者向け（段階的開示）に再編する。画面自身の文言も自身の翻訳カタログ
    /// （scope=UiScope）から引くことで多言語表示・言語切替に追従させる（ドッグフーディング）。
    /// </summary>
    internal static class EditorL10nSettingsProvider
    {
        // この設定画面自身の UI 文言を引く scope（Editor/Localization の manifest と一致させる）。
        private const string UiScope = "com.kajitaharuka.unity-editor-localization";

        // Preferences のこの設定ページのパス。CreateProvider とメニューから開く導線で共有する。
        private const string SettingsPath = "Preferences/UnityEditorLocalization";

        // Tools から Preferences を開き、この項目を選択状態にする導線。
        [MenuItem("Tools/UnityEditorLocalization/Settings", priority = 0)]
        private static void OpenSettings() => SettingsService.OpenUserPreferences(SettingsPath);

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider(SettingsPath, SettingsScope.User)
            {
                label = "UnityEditorLocalization",
                // Preferences 検索での発見性（多言語）。
                keywords = new HashSet<string>(new[]
                {
                    "localization", "locale", "language", "l10n", "editor", "translation",
                    "言語", "ロケール", "多言語", "翻訳",
                }),
                activateHandler = (_, root) => new Pane().Build(root),
            };
        }

        private static string Tr(string key, params object[] args) => EditorL10n.Tr(UiScope, key, args);

        // ラベル/ツールチップを翻訳キーへバインドし、言語変更へ追従させる（管理元を一本化）。
        private static void BindLabel(Label label, string key)
        {
            if (label == null) return;
            EditorL10nUi.BindText(label, UiScope, key);
        }

        private static void BindTooltip(VisualElement element, string key)
        {
            if (element == null) return;
            void Apply() => element.tooltip = Tr(key);
            Apply();
            EditorL10nUi.RegisterLocaleCallback(element, Apply);
        }

        /// <summary>1 回のアクティベーションぶんの画面状態。動的部の参照を保持し、in-place 更新する。</summary>
        private sealed class Pane
        {
            private Label _overviewBadge;
            private VisualElement _scopeList;
            private Label _countLabel;
            private HelpBox _scopeEmpty;
            // 絞り込み文字列はここにキャッシュせず、ApplyFilter で常にこのフィールドの現在値を直接読む
            // （キャッシュすると表示と内部状態が desync し、フィールドが空なのに全カードが消える不具合が起きうる）。
            private TextField _search;
            private string[] _builtScopes = Array.Empty<string>();
            private readonly List<ScopeCard> _cards = new();
            // カタログ検証の結果表示。要約行（_catalogsResult）と scope 別分類（_validationGroups）の参照を保持し、
            // 言語変更時に保持したスナップショット（_lastValidation）で再描画して画面言語へ追従させる。
            private Label _catalogsResult;
            private VisualElement _validationGroups;
            private EditorL10nValidationResult _lastValidation;

            public void Build(VisualElement root)
            {
                root.Clear();
                EditorL10nUiKit.ApplyTheme(root);
                // 親（Settings ペイン）が縦に狭いと flex 子が圧縮されて要素が重なり、レイアウトが崩れる。
                // 内容を ScrollView に収め、狭いときは潰さずスクロールで全体に到達できるようにする。
                root.style.flexGrow = 1;

                var scroll = new ScrollView(ScrollViewMode.Vertical);
                scroll.style.flexGrow = 1;
                scroll.style.flexShrink = 1;
                scroll.style.minHeight = 0; // 親が狭いとき scroll 自身が縮めるようにして内容をスクロール領域へ追い込む
                root.Add(scroll);

                var header = BuildHeader();
                scroll.Add(header);
                scroll.Add(EditorL10nUiKit.Note(Tr("note")).Also(label => BindLabel(label, "note")));
                // 主要操作（表示言語）を上部に保ち、カタログの保守/検証はその下に置く（作業順・主要操作の明確化）。
                scroll.Add(BuildGlobalSection());
                scroll.Add(BuildScopeSection());
                scroll.Add(BuildCatalogs());
                scroll.Add(BuildSkillsSection());
                scroll.Add(BuildDeveloperSection());

                _builtScopes = EditorL10n.GetScopes().ToArray();
                RebuildScopeList(_builtScopes);

                // 言語変更/カタログ変更へ追従。再構築サブツリーに含まれる header へ紐付けることで、
                // 再アクティベーション時の root.Clear() で古い購読が確実に解除され、リークを防ぐ。
                EditorL10nUi.RegisterLocaleCallback(header, OnLocaleChanged);
            }

            // ===== ヘッダー（2 ゾーン）=====
            private VisualElement BuildHeader()
            {
                _overviewBadge = EditorL10nUiKit.StatusBadge();
                var doc = EditorL10nUiKit.DocButton(EditorL10nDocs.DocumentationUrl, Tr("doc.tooltip"));
                BindTooltip(doc, "doc.tooltip");

                var header = EditorL10nUiKit.Header("UnityEditorLocalization", Tr("header.subtitle"), _overviewBadge, doc);
                BindLabel(header.Q<Label>("eui-header-subtitle"), "header.subtitle");
                UpdateOverviewBadge();
                return header;
            }

            // ===== カタログ（Reload / Validate / 検証結果の scope 別分類）=====
            // 「再読み込み・検証」を独立した大項目（タイトル付き Section）として捉え直し、その中に
            // 検証結果を scope ごとに分類して表示する。どの scope 由来の警告/エラーかを一目で追える。
            private VisualElement BuildCatalogs()
            {
                var card = EditorL10nUiKit.Section(Tr("catalogs.title"), out var content);
                BindLabel(card.Q<Label>(className: "eui-section__title"), "catalogs.title");

                var row = new VisualElement();
                row.AddToClassList("l10n-catalogs");

                var result = new Label { name = "l10n-catalogs-result" };
                result.AddToClassList("l10n-catalogs__result");
                result.style.display = DisplayStyle.None;
                _catalogsResult = result;

                // 検証結果を scope ごとに分類して並べる領域（Validate 実行時に作り直す）。
                var groups = new VisualElement();
                groups.AddToClassList("l10n-validation-groups");
                groups.style.display = DisplayStyle.None;
                _validationGroups = groups;

                var reload = EditorL10nUiKit.ActionButton(Tr("catalogs.reload"), () =>
                {
                    EditorL10n.Reload();
                    SetResult(result, Tr("catalogs.reloaded"), EditorL10nBadgeKind.Neutral);
                    // Reload でカタログが入れ替わると前回の検証結果は古くなるため分類表示を畳む。
                    _lastValidation = null;
                    RenderValidationGroups(null);
                }, Tr("catalogs.reload.tooltip"));
                BindButtonText(reload, "catalogs.reload", "catalogs.reload.tooltip");

                var validate = EditorL10nUiKit.ActionButton(Tr("catalogs.validate"), () =>
                {
                    var validation = EditorL10nValidator.ValidateAndLog();
                    _lastValidation = validation;
                    UpdateCatalogsResultLine();
                    RenderValidationGroups(validation);
                }, Tr("catalogs.validate.tooltip"));
                BindButtonText(validate, "catalogs.validate", "catalogs.validate.tooltip");

                // 両操作の意味を説明する HelpBox（既定は非表示）。下の ⓘ ボタンで開閉する。
                var help = EditorL10nUiKit.InfoBox(Tr("catalogs.help.tooltip"));
                help.style.display = DisplayStyle.None;
                EditorL10nUi.RegisterLocaleCallback(help, () => help.text = Tr("catalogs.help.tooltip"));

                // 説明トグル（ⓘ）。クリック/キーボードで上の HelpBox を開閉でき（キーボード到達可）、
                // ホバーの tooltip でも要約を確認できる（マウス）。これで説明性を入力手段に依らず確保する。
                var helpToggle = EditorL10nUiKit.IconLinkButton("console.infoicon", Tr("catalogs.help.tooltip"), () =>
                {
                    help.style.display = help.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
                });
                BindTooltip(helpToggle, "catalogs.help.tooltip");

                row.Add(reload);
                row.Add(validate);
                row.Add(helpToggle);
                row.Add(result);

                content.Add(row);
                content.Add(help);
                content.Add(groups);
                return card;
            }

            // 検証結果の要約行を現在のスナップショット（_lastValidation）から（再）描画する。言語変更にも追従させる。
            private void UpdateCatalogsResultLine()
            {
                if (_catalogsResult == null || _lastValidation == null)
                    return;
                if (_lastValidation.IsValid)
                    SetResult(_catalogsResult, Tr("catalogs.result.ok", _lastValidation.WarningCount), EditorL10nBadgeKind.Ok);
                else
                    SetResult(_catalogsResult, Tr("catalogs.result.issues", _lastValidation.ErrorCount, _lastValidation.WarningCount), EditorL10nBadgeKind.Warning);
            }

            // 検証結果を scope ごとに分類して描画する（point-in-time のスナップショット）。
            // null を渡すと領域を畳む。言語変更時は保持した _lastValidation で呼び直し、画面言語へ追従させる。
            private void RenderValidationGroups(EditorL10nValidationResult validation)
            {
                if (_validationGroups == null)
                    return;

                _validationGroups.Clear();

                if (validation == null)
                {
                    _validationGroups.style.display = DisplayStyle.None;
                    return;
                }

                // 追加順を保ちつつ scope ごとに issue をまとめる。
                var byScope = new Dictionary<string, List<EditorL10nValidationIssue>>();
                var order = new List<string>();
                foreach (var issue in validation.Issues)
                {
                    if (!byScope.TryGetValue(issue.Scope, out var list))
                    {
                        list = new List<EditorL10nValidationIssue>();
                        byScope[issue.Scope] = list;
                        order.Add(issue.Scope);
                    }
                    list.Add(issue);
                }

                if (order.Count == 0)
                {
                    _validationGroups.style.display = DisplayStyle.None;
                    return;
                }

                _validationGroups.style.display = DisplayStyle.Flex;
                foreach (var scope in order)
                    _validationGroups.Add(BuildValidationGroup(scope, byScope[scope]));

                // 問題の無かった scope 数を控えめに示す（検査済みである安心材料・全体像の把握）。
                var cleanScopes = EditorL10n.GetScopes().Count - order.Count;
                if (cleanScopes > 0)
                    _validationGroups.Add(EditorL10nUiKit.HintRow(Tr("catalogs.groups.clean", cleanScopes)));
            }

            // 1 scope ぶんの分類グループ（折りたたみ可能）。ヘッダーに件数ピル、本文に issue 行を並べる。
            private VisualElement BuildValidationGroup(string scope, List<EditorL10nValidationIssue> issues)
            {
                // 1 深刻度あたりの表示上限。巨大な scope（数十 key 欠落など）で UI が縦に伸び切るのを防ぐ。
                // 全件は常に Console（ValidateAndLog）へ出ているので、超過分は件数だけ示して Console へ誘導する。
                const int maxRowsPerSeverity = 30;

                var errors = issues.Where(issue => issue.Severity == EditorL10nValidationSeverity.Error).ToList();
                var warnings = issues.Where(issue => issue.Severity == EditorL10nValidationSeverity.Warning).ToList();

                var group = new VisualElement();
                group.AddToClassList("l10n-vgroup");

                var head = new VisualElement();
                head.AddToClassList("l10n-vgroup__head");

                var chevron = new Button { text = "▾" };
                chevron.AddToClassList("l10n-chevron");

                var body = new VisualElement();
                body.AddToClassList("l10n-vgroup__body");

                // エラーを含む scope は既定で展開（注意が要る対象を最初から見せる）。警告だけなら畳む。
                var expanded = errors.Count > 0;
                void SetExpanded(bool value)
                {
                    expanded = value;
                    body.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
                    chevron.text = value ? "▾" : "▸";
                }
                chevron.clicked += () => SetExpanded(!expanded);

                var name = new Label(EditorL10nUiKit.InsertWrapOpportunities(scope));
                name.AddToClassList("l10n-vgroup__name");
                name.tooltip = scope;

                // 件数ピル（畳んだ状態でも深刻度の概況が分かる）。数を含むので未翻訳化されず多言語でも一意。
                var pills = new VisualElement();
                pills.AddToClassList("l10n-scope-card__pills");
                if (errors.Count > 0)
                    pills.Add(EditorL10nUiKit.Pill(Tr("catalogs.count.errors", errors.Count), EditorL10nBadgeKind.Error));
                if (warnings.Count > 0)
                    pills.Add(EditorL10nUiKit.Pill(Tr("catalogs.count.warnings", warnings.Count), EditorL10nBadgeKind.Warning));

                head.Add(chevron);
                head.Add(name);
                head.Add(pills);
                // head 行のどこをクリックしても開閉できる（ヒット領域拡大）。チェブロンは自前で処理するため除外。
                head.RegisterCallback<ClickEvent>(evt =>
                {
                    if (evt.target == chevron)
                        return;
                    SetExpanded(!expanded);
                });

                // 本文: エラー → 警告 の順に行を並べる（深刻なものを上に）。上限超過分は件数を示す。
                foreach (var issue in errors.Take(maxRowsPerSeverity))
                    body.Add(BuildValidationIssueRow(issue));
                if (errors.Count > maxRowsPerSeverity)
                    body.Add(EditorL10nUiKit.HintRow(Tr("catalogs.more", errors.Count - maxRowsPerSeverity)));
                foreach (var issue in warnings.Take(maxRowsPerSeverity))
                    body.Add(BuildValidationIssueRow(issue));
                if (warnings.Count > maxRowsPerSeverity)
                    body.Add(EditorL10nUiKit.HintRow(Tr("catalogs.more", warnings.Count - maxRowsPerSeverity)));

                SetExpanded(expanded);

                group.Add(head);
                group.Add(body);
                return group;
            }

            // issue 1 件の行: 深刻度マーカー（色＋形）／locale チップ／詳細メッセージ。長文は折り返す。
            private VisualElement BuildValidationIssueRow(EditorL10nValidationIssue issue)
            {
                var row = new VisualElement();
                row.AddToClassList("l10n-vissue");

                // 不足キーはその場で追加できるクイックfix（"+"。defaultLocale の値をコピーして種にする）。
                Button addButton = null;
                if (issue.Kind == EditorL10nValidationMessageKind.MissingKey
                    && !string.IsNullOrEmpty(issue.Locale) && issue.Args.Count > 0)
                {
                    var missingKey = issue.Args[0];
                    addButton = new Button(() => QuickAddMissingKey(issue.Scope, issue.Locale, missingKey)) { text = "+" };
                    addButton.AddToClassList("l10n-vissue__add");
                    addButton.tooltip = Tr("quickfix.addKey.tooltip");
                    row.Add(addButton);
                }

                var isError = issue.Severity == EditorL10nValidationSeverity.Error;
                // 色だけに頼らず形（× / !）でも深刻度が伝わるマーカー（色覚配慮）。文言ではないので未翻訳化しない。
                var mark = EditorL10nUiKit.Pill(isError ? "×" : "!",
                    isError ? EditorL10nBadgeKind.Error : EditorL10nBadgeKind.Warning);
                mark.AddToClassList("l10n-vissue__mark");
                row.Add(mark);

                // どの locale 由来かを示すチップ（scope 全体の問題など locale が無いときは省略）。
                if (!string.IsNullOrEmpty(issue.Locale))
                {
                    var locale = EditorL10nUiKit.Pill(issue.Locale, EditorL10nBadgeKind.Neutral);
                    locale.AddToClassList("l10n-vissue__locale");
                    row.Add(locale);
                }

                var message = new Label(issue.Message);
                message.AddToClassList("l10n-vissue__msg");
                row.Add(message);

                // クリックで由来アセット（locale テーブル、無ければ manifest）を選択+Ping し、原因箇所へ素早く辿れるようにする。
                if (TryResolveIssueAsset(issue, out var assetPath))
                {
                    row.AddToClassList("l10n-vissue--clickable");
                    row.tooltip = Tr("catalogs.issue.openAsset.tooltip");
                    var quickAdd = addButton;
                    row.RegisterCallback<ClickEvent>(evt =>
                    {
                        // 追加ボタンのクリックはクイックfixが処理するので、行のジャンプは発火させない。
                        if (quickAdd != null && evt.target == quickAdd)
                            return;
                        PingAsset(assetPath);
                    });
                }

                return row;
            }

            // 不足キーをその locale テーブルへ追加する（値は defaultLocale からコピー）。
            // 正準ライターでファイルを書き戻し、再 import → 再検証して結果表示を更新する。
            private void QuickAddMissingKey(string scope, string locale, string key)
            {
                try
                {
                    if (!EditorL10n.TryGetLocaleTablePath(scope, locale, out var tablePath))
                        throw new Exception($"locale テーブルのパスが見つかりません: {scope}/{locale}");

                    var entries = LoadTableEntries(tablePath);
                    if (entries.All(entry => entry.Key != key))
                    {
                        entries.Add(new KeyValuePair<string, string>(key, GetDefaultLocaleValue(scope, key)));
                        File.WriteAllText(FileUtil.GetPhysicalPath(tablePath), EditorL10nCatalogWriter.WriteTable(locale, entries));
                        AssetDatabase.ImportAsset(tablePath);
                    }

                    EditorL10n.Reload();
                    // 再検証して結果表示を更新する。RenderValidationGroups はこの行ボタン自身も作り直すが、
                    // 上の書き込み〜Reload は同期完了しており、このコールバックは既に return 済みなので安全
                    // （いずれかの工程を非同期化する場合は detach 済み要素の使用に注意）。
                    _lastValidation = EditorL10nValidator.ValidateAll();
                    UpdateCatalogsResultLine();
                    RenderValidationGroups(_lastValidation);
                    Debug.Log($"EditorLocalization: {scope}/{locale} に key を追加しました: {key}");
                }
                catch (Exception exception)
                {
                    Debug.LogError($"EditorLocalization: key の追加に失敗しました: {exception}");
                    if (_catalogsResult != null)
                        SetResult(_catalogsResult, Tr("quickfix.failed"), EditorL10nBadgeKind.Error);
                }
            }

            // locale テーブルの全エントリを出現順で読み出す（追加時に既存順を保つため）。
            private static List<KeyValuePair<string, string>> LoadTableEntries(string tablePath)
            {
                var result = new List<KeyValuePair<string, string>>();
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(tablePath);
                if (asset == null)
                    return result;

                var document = JsonUtility.FromJson<EditorL10nTableDocument>(asset.text);
                if (document?.entries == null)
                    return result;

                foreach (var entry in document.entries)
                    if (entry != null && !string.IsNullOrEmpty(entry.key))
                        result.Add(new KeyValuePair<string, string>(entry.key, entry.value ?? ""));
                return result;
            }

            // 種にする defaultLocale の値を取得する（無ければ空）。
            private static string GetDefaultLocaleValue(string scope, string key)
            {
                if (!EditorL10n.TryGetScopeInfo(scope, out var info) || string.IsNullOrEmpty(info.DefaultLocale))
                    return "";
                if (!EditorL10n.TryGetLocaleTablePath(scope, info.DefaultLocale, out var path))
                    return "";
                foreach (var entry in LoadTableEntries(path))
                    if (entry.Key == key)
                        return entry.Value;
                return "";
            }

            // issue の由来アセットを解決する。locale 由来はその locale テーブル、scope 由来（locale 空）は manifest。
            private static bool TryResolveIssueAsset(EditorL10nValidationIssue issue, out string assetPath)
            {
                if (!string.IsNullOrEmpty(issue.Locale)
                    && EditorL10n.TryGetLocaleTablePath(issue.Scope, issue.Locale, out assetPath)
                    && !string.IsNullOrEmpty(assetPath))
                    return true;

                if (EditorL10n.TryGetScopeInfo(issue.Scope, out var info) && !string.IsNullOrEmpty(info.ManifestPath))
                {
                    assetPath = info.ManifestPath;
                    return true;
                }

                assetPath = "";
                return false;
            }

            private static void SetResult(Label result, string text, EditorL10nBadgeKind kind)
            {
                result.text = text;
                result.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
                result.style.color = ResolveColor(kind);
            }

            // ===== 表示言語（グローバル）=====
            private VisualElement BuildGlobalSection()
            {
                var card = EditorL10nUiKit.Section(Tr("global.title"), out var content);
                BindLabel(card.Q<Label>(className: "eui-section__title"), "global.title");

                var dropdown = BuildGlobalDropdown();
                content.Add(dropdown);

                // 検出したシステム（OS）言語の表示。実機で何が返るかの確認も兼ねる。
                var systemLine = EditorL10nUiKit.HintRow("");
                void ApplySystemLine()
                {
                    var systemLocale = EditorL10n.GetSystemLocale();
                    systemLine.text = string.IsNullOrEmpty(systemLocale)
                        ? Tr("system.detected.none")
                        : Tr("system.detected", systemLocale);
                }
                ApplySystemLine();
                EditorL10nUi.RegisterLocaleCallback(systemLine, ApplySystemLine);
                content.Add(systemLine);

                // システム言語フォールバックの有効/無効トグル（グローバル未設定時の挙動を切り替える）。
                var fallbackToggle = new Toggle(Tr("system.fallback.label")) { value = EditorL10n.GetSystemLocaleFallbackEnabled() };
                EditorL10nUiKit.AlignField(fallbackToggle);
                BindTooltip(fallbackToggle, "system.fallback.tooltip");
                EditorL10nUi.RegisterLocaleCallback(fallbackToggle, () => fallbackToggle.label = Tr("system.fallback.label"));
                fallbackToggle.RegisterValueChangedCallback(evt => EditorL10n.SetSystemLocaleFallbackEnabled(evt.newValue));
                content.Add(fallbackToggle);

                // 未設定時に実際どう解決されるかを示す動的ヒント（トグルとシステム言語の検出状態に追従）。
                var resolveHint = EditorL10nUiKit.HintRow("");
                void ApplyResolveHint()
                {
                    if (!EditorL10n.GetSystemLocaleFallbackEnabled())
                    {
                        resolveHint.text = Tr("global.resolve.default");
                        return;
                    }

                    var systemLocale = EditorL10n.GetSystemLocale();
                    resolveHint.text = string.IsNullOrEmpty(systemLocale)
                        ? Tr("global.resolve.systemNone")
                        : Tr("global.resolve.system", systemLocale);
                }
                ApplyResolveHint();
                EditorL10nUi.RegisterLocaleCallback(resolveHint, ApplyResolveHint);
                content.Add(resolveHint);

                return card;
            }

            private DropdownField BuildGlobalDropdown()
            {
                var tags = new List<string>();
                var dropdown = new DropdownField(Tr("global.label"), new List<string> { Tr("global.unset") }, 0);
                EditorL10nUiKit.AlignField(dropdown);
                BindTooltip(dropdown, "global.tooltip");

                // Apply() による choices/value の更新が value-changed を巻き戻し発火させ、古い値で
                // SetGlobalLocale を再実行する（カタログ/言語変化時に 1 段ズレる）のを防ぐガード。
                var applying = false;
                void Apply()
                {
                    applying = true;
                    tags.Clear();
                    var labels = new List<string>();
                    tags.Add("");
                    labels.Add(Tr("global.unset"));
                    foreach (var locale in GetGlobalLocaleOptions())
                    {
                        tags.Add(locale.Tag);
                        labels.Add(locale.DisplayName);
                    }

                    var active = EditorL10n.GetGlobalLocale();
                    var index = tags.IndexOf(active);
                    if (index < 0 && !string.IsNullOrEmpty(active))
                    {
                        tags.Add(active);
                        labels.Add(Tr("outOfCatalog", active));
                        index = tags.Count - 1;
                    }
                    if (index < 0) index = 0;

                    dropdown.label = Tr("global.label");
                    dropdown.choices = labels;
                    dropdown.SetValueWithoutNotify(labels[index]);
                    applying = false;
                }

                dropdown.RegisterValueChangedCallback(_ =>
                {
                    if (applying) return;
                    var index = dropdown.index;
                    if (index >= 0 && index < tags.Count)
                        EditorL10n.SetGlobalLocale(tags[index]);
                });

                Apply();
                EditorL10nUi.RegisterLocaleCallback(dropdown, Apply);
                return dropdown;
            }

            // ===== scope 個別設定 =====
            private VisualElement BuildScopeSection()
            {
                var card = EditorL10nUiKit.Section(Tr("scope.title"), out var content);
                BindLabel(card.Q<Label>(className: "eui-section__title"), "scope.title");

                _search = new TextField(Tr("scope.search.label"));
                EditorL10nUiKit.AlignField(_search);
                BindTooltip(_search, "scope.search.tooltip");
                EditorL10nUi.RegisterLocaleCallback(_search, () => _search.label = Tr("scope.search.label"));
                // 値はキャッシュせず、変更時は再フィルタするだけ（ApplyFilter がフィールドの現在値を読む）。
                _search.RegisterValueChangedCallback(_ => ApplyFilter());
                content.Add(_search);

                _countLabel = new Label();
                _countLabel.AddToClassList("eui-hint");
                content.Add(_countLabel);

                _scopeEmpty = EditorL10nUiKit.InfoBox(Tr("scope.empty.noManifest"));
                _scopeEmpty.style.display = DisplayStyle.None;
                content.Add(_scopeEmpty);

                _scopeList = new VisualElement();
                content.Add(_scopeList);
                return card;
            }

            private void RebuildScopeList(string[] scopes)
            {
                _scopeList.Clear();
                _cards.Clear();

                foreach (var scope in scopes)
                {
                    var entry = new ScopeCard(scope);
                    _cards.Add(entry);
                    _scopeList.Add(entry.Root);
                }

                ApplyFilter();
            }

            // 検索でカードを表示/非表示（再構築せずドロップダウン/状態を保持）。件数と空状態を更新。
            // 絞り込み文字列はフィールドから直接読む（キャッシュしないことで desync を排除）。
            private void ApplyFilter()
            {
                var searchText = _search != null ? _search.value : "";
                var total = _cards.Count;
                var shown = 0;
                foreach (var card in _cards)
                {
                    var match = ScopeMatchesSearch(card.Scope, searchText);
                    card.Root.style.display = match ? DisplayStyle.Flex : DisplayStyle.None;
                    if (match) shown++;
                }

                _countLabel.text = Tr("scope.count", shown, total);
                _countLabel.style.display = total == 0 ? DisplayStyle.None : DisplayStyle.Flex;

                if (total == 0)
                    ShowScopeEmpty("scope.empty.noManifest", HelpBoxMessageType.Info);
                else if (shown == 0)
                    ShowScopeEmpty("scope.empty.noMatch", HelpBoxMessageType.Info);
                else
                    _scopeEmpty.style.display = DisplayStyle.None;
            }

            private void ShowScopeEmpty(string key, HelpBoxMessageType type)
            {
                _scopeEmpty.text = Tr(key);
                _scopeEmpty.messageType = type;
                _scopeEmpty.style.display = DisplayStyle.Flex;
            }

            // ===== AIエージェント連携スキル（同梱スキルの登録）=====
            private VisualElement BuildSkillsSection()
            {
                var card = EditorL10nUiKit.Section(Tr("skills.title"), out var content);
                BindLabel(card.Q<Label>(className: "eui-section__title"), "skills.title");
                content.Add(EditorL10nUiKit.Note(Tr("skills.note")).Also(label => BindLabel(label, "skills.note")));

                content.Add(BuildSkillRow("skills.translation.name", "skills.translation.desc"));
                content.Add(BuildSkillRow("skills.optional.name", "skills.optional.desc"));

                // 操作結果のインライン表示（登録/コピーの両方からここへ出す）。
                var result = new Label { name = "l10n-skills-result" };
                result.AddToClassList("l10n-catalogs__result");
                // l10n-catalogs__result は横並び行（l10n-catalogs）内で全幅にするための flex-basis:100% を持つ。
                // この節では result を列フローに直接置くため、flex-basis:100% だと全高を占めて他要素を潰す。auto へ戻す。
                result.style.flexBasis = StyleKeyword.Auto;
                result.style.display = DisplayStyle.None;

                // 登録ボタン行（ボタン文言でどこへ登録するかを明示）。
                var installRow = new VisualElement();
                installRow.AddToClassList("l10n-catalogs");

                var installUser = EditorL10nUiKit.ActionButton(Tr("skills.install.user"), () =>
                {
                    Debug.Log(EditorL10nSkillInstaller.InstallToUser());
                    SetResult(result, Tr("skills.result.installed"), EditorL10nBadgeKind.Ok);
                }, Tr("skills.install.user.tooltip"));
                BindButtonText(installUser, "skills.install.user", "skills.install.user.tooltip");

                var installProject = EditorL10nUiKit.ActionButton(Tr("skills.install.project"), () =>
                {
                    Debug.Log(EditorL10nSkillInstaller.InstallToProject());
                    SetResult(result, Tr("skills.result.installed"), EditorL10nBadgeKind.Ok);
                }, Tr("skills.install.project.tooltip"));
                BindButtonText(installProject, "skills.install.project", "skills.install.project.tooltip");

                installRow.Add(installUser);
                installRow.Add(installProject);
                content.Add(installRow);

                // CLI で追加したい場合の案内＋コマンド明示＋横のコピーボタン。
                content.Add(EditorL10nUiKit.Note(Tr("skills.cli.note")).Also(label => BindLabel(label, "skills.cli.note")));

                var cliRow = new VisualElement();
                cliRow.style.flexDirection = FlexDirection.Row;
                cliRow.style.alignItems = Align.FlexStart;

                // 読み取り専用の複数行フィールドにコマンドを表示（選択もできる）。値変更時は再表示するだけ。
                var cliField = new TextField { multiline = true, isReadOnly = true };
                cliField.value = EditorL10nSkillInstaller.CliSnippetForUser() + "\n" + EditorL10nSkillInstaller.CliSnippetForProject();
                cliField.style.flexGrow = 1;
                cliField.style.flexShrink = 1;
                cliField.style.fontSize = 10;
                cliField.style.marginRight = 4;

                var copyCli = EditorL10nUiKit.ActionButton(Tr("skills.cli.copy"), () =>
                {
                    EditorGUIUtility.systemCopyBuffer = cliField.value;
                    SetResult(result, Tr("skills.result.copied"), EditorL10nBadgeKind.Neutral);
                }, Tr("skills.cli.copy.tooltip"));
                copyCli.style.flexShrink = 0;
                BindButtonText(copyCli, "skills.cli.copy", "skills.cli.copy.tooltip");

                cliRow.Add(cliField);
                cliRow.Add(copyCli);
                content.Add(cliRow);

                content.Add(result);
                return card;
            }

            // 同梱スキル 1 件の表示（名前＋説明）。言語変更に追従させる。
            private VisualElement BuildSkillRow(string nameKey, string descKey)
            {
                var box = new VisualElement();
                box.style.marginBottom = 4;

                var name = new Label(Tr(nameKey));
                name.style.unityFontStyleAndWeight = FontStyle.Bold;
                name.style.fontSize = 11;
                BindLabel(name, nameKey);

                box.Add(name);
                box.Add(EditorL10nUiKit.HintRow(Tr(descKey)).Also(label => BindLabel(label, descKey)));
                return box;
            }

            // ===== 開発者向け（段階的開示）=====
            private VisualElement BuildDeveloperSection()
            {
                var card = EditorL10nUiKit.Card();
                var foldout = new Foldout { text = Tr("dev.title"), value = false };
                EditorL10nUi.RegisterLocaleCallback(foldout, () => foldout.text = Tr("dev.title"));

                var toggle = new Toggle(Tr("diagnostics.label")) { value = EditorL10nPreferences.DiagnosticsEnabled };
                EditorL10nUiKit.AlignField(toggle);
                BindTooltip(toggle, "diagnostics.tooltip");
                EditorL10nUi.RegisterLocaleCallback(toggle, () => toggle.label = Tr("diagnostics.label"));
                toggle.RegisterValueChangedCallback(evt => EditorL10nPreferences.DiagnosticsEnabled = evt.newValue);
                foldout.Add(toggle);

                // トグルの意味が一目で分かるよう、何を・いつ・どう振る舞うかを永続のノートで添える
                // （tooltip だけだと開発者でも用途が分からない、という指摘への対応）。
                foldout.Add(EditorL10nUiKit.HintRow(Tr("diagnostics.note")).Also(label => BindLabel(label, "diagnostics.note")));

                card.Add(foldout);
                return card;
            }

            // ===== 言語/カタログ変更への追従 =====
            private void OnLocaleChanged()
            {
                var scopes = EditorL10n.GetScopes().ToArray();
                if (!scopes.SequenceEqual(_builtScopes))
                {
                    // カタログが変化した（manifest 追加/削除/Reload）。一覧を作り直す。
                    _builtScopes = scopes;
                    RebuildScopeList(scopes);

                    // カタログ構成が変わったので前回の検証結果は古い。要約行と分類表示を畳む。
                    _lastValidation = null;
                    RenderValidationGroups(null);
                    if (_catalogsResult != null)
                        _catalogsResult.style.display = DisplayStyle.None;
                }
                else
                {
                    // 言語のみの変更。編集中のドロップダウンを壊さないよう、派生表示だけ in-place 更新。
                    foreach (var card in _cards)
                        card.UpdateState();

                    // in-place 更新だけだと scope 一覧が再レイアウトされず、カードが消えて見えることがある
                    // （フィルタを触ると復帰する症状）。フィルタ操作と同じ ApplyFilter を呼んで各カードの
                    // display を書き直して表示を再適用し、併せて件数ラベルを新しい言語へ再翻訳する。
                    // 検索文字列は不変なので在不在の判定（どのカードを出すか）は変わらない。
                    ApplyFilter();

                    // 検証結果（要約行・scope 別分類）も保持スナップショットから新しい画面言語で再描画する。
                    UpdateCatalogsResultLine();
                    RenderValidationGroups(_lastValidation);
                }

                UpdateOverviewBadge();
            }

            private void UpdateOverviewBadge()
            {
                var scopes = EditorL10n.GetScopes();
                if (scopes.Count == 0)
                {
                    EditorL10nUiKit.SetBadge(_overviewBadge, Tr("header.overview.empty"), EditorL10nBadgeKind.Neutral);
                    return;
                }

                var locales = new HashSet<string>();
                var hasIssue = false;
                foreach (var scope in scopes)
                {
                    foreach (var locale in EditorL10n.GetLocales(scope))
                    {
                        if (locale != null && !string.IsNullOrEmpty(locale.Tag))
                            locales.Add(locale.Tag);
                    }
                    if (ScopeHasUnavailableRequest(scope))
                        hasIssue = true;
                }

                EditorL10nUiKit.SetBadge(
                    _overviewBadge,
                    Tr("header.overview", scopes.Count, locales.Count),
                    hasIssue ? EditorL10nBadgeKind.Warning : EditorL10nBadgeKind.Neutral);
            }

            private void BindButtonText(Button button, string textKey, string tooltipKey)
            {
                void Apply()
                {
                    button.text = Tr(textKey);
                    button.tooltip = Tr(tooltipKey);
                }
                Apply();
                EditorL10nUi.RegisterLocaleCallback(button, Apply);
            }
        }

        /// <summary>scope 1 件ぶんのカード。状態（pill/meta）は常時表示、言語ドロップダウンは折りたたみ。</summary>
        private sealed class ScopeCard
        {
            public string Scope { get; }
            public VisualElement Root { get; }

            private readonly Label _overridePill;
            private readonly Label _fallbackPill;
            private readonly Label _meta;
            private readonly Label _fallbackNote;
            // fallback 連鎖の可視化行。locale を持つ scope のみ生成する（無い場合は null）。
            private readonly VisualElement _chainRow;

            public ScopeCard(string scope)
            {
                Scope = scope;
                Root = new VisualElement();
                Root.AddToClassList("l10n-scope-card");

                var locales = EditorL10n.GetLocales(scope).ToArray();

                var head = new VisualElement();
                head.AddToClassList("l10n-scope-card__head");

                // 既定は展開（主要操作＝言語ドロップダウンを最初から見せる）。body は display 未指定で可視。
                var body = new VisualElement();
                body.AddToClassList("l10n-scope-body");

                var chevron = new Button { text = "▾" };
                chevron.AddToClassList("l10n-chevron");

                // 展開/折りたたみの切替。チェブロン（キーボード操作可）と head 行全体のクリック（広いヒット領域）の
                // 双方から呼べるよう単一メソッドにまとめる。
                var expanded = true;
                void SetExpanded(bool value)
                {
                    expanded = value;
                    body.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
                    chevron.text = value ? "▾" : "▸";
                }
                chevron.clicked += () => SetExpanded(!expanded);

                var name = new Label(EditorL10nUiKit.InsertWrapOpportunities(scope));
                name.AddToClassList("l10n-scope-card__name");
                name.tooltip = scope;

                var pills = new VisualElement();
                pills.AddToClassList("l10n-scope-card__pills");
                _overridePill = EditorL10nUiKit.Pill("", EditorL10nBadgeKind.Accent);
                _fallbackPill = EditorL10nUiKit.Pill("", EditorL10nBadgeKind.Warning);
                pills.Add(_overridePill);
                pills.Add(_fallbackPill);

                head.Add(chevron);
                head.Add(name);
                head.Add(pills);
                // head 行のどこをクリックしても開閉できる（ヒット領域拡大・発見性向上）。
                // チェブロン自身のクリックは chevron.clicked が処理するため、二重トグルを避けて除外する。
                head.RegisterCallback<ClickEvent>(evt =>
                {
                    if (evt.target == chevron)
                        return;
                    SetExpanded(!expanded);
                });

                _meta = new Label();
                _meta.AddToClassList("l10n-scope-meta");

                _fallbackNote = new Label();
                _fallbackNote.AddToClassList("l10n-scope-meta");
                _fallbackNote.AddToClassList("l10n-scope-meta--warn");
                _fallbackNote.style.display = DisplayStyle.None;

                Root.Add(head);
                Root.Add(_meta);
                Root.Add(_fallbackNote);

                // manifest を選択+Ping できる行（認識>想起・利便）。
                if (EditorL10n.TryGetScopeInfo(scope, out var info) && !string.IsNullOrEmpty(info.ManifestPath))
                {
                    var manifestPath = info.ManifestPath;
                    var manifestRow = EditorL10nUiKit.AssetRow(manifestPath, Tr("scope.manifest.tooltip"), () => PingAsset(manifestPath));
                    BindTooltip(manifestRow, "scope.manifest.tooltip");
                    Root.Add(manifestRow);
                }

                if (locales.Length == 0)
                {
                    var warn = EditorL10nUiKit.WarningBox(Tr("scope.noLocales"));
                    EditorL10nUi.RegisterLocaleCallback(warn, () => warn.text = Tr("scope.noLocales"));
                    body.Add(warn);
                }
                else
                {
                    body.Add(BuildScopeDropdown(scope, locales));

                    // 実際に効いている fallback 連鎖を可視化（要求 → 親 → defaultLocale、使用段を強調）。
                    _chainRow = new VisualElement();
                    _chainRow.AddToClassList("l10n-scope-chain");
                    body.Add(_chainRow);

                    // 解決順の下に、この scope が対応する言語コードの一覧を表示する（固定。見出しのみ言語追従）。
                    var localeTags = string.Join(" · ", locales.Select(locale => locale.Tag));
                    var localesRow = EditorL10nUiKit.HintRow("");
                    void ApplyLocalesRow() => localesRow.text = Tr("scope.locales.label") + ": " + localeTags;
                    ApplyLocalesRow();
                    EditorL10nUi.RegisterLocaleCallback(localesRow, ApplyLocalesRow);
                    body.Add(localesRow);
                }

                Root.Add(body);
                UpdateState();
            }

            private DropdownField BuildScopeDropdown(string scope, EditorL10nLocaleInfo[] locales)
            {
                var tags = new List<string>();
                var dropdown = new DropdownField(Tr("scope.locale.label"), new List<string> { Tr("scope.followGlobal") }, 0);
                EditorL10nUiKit.AlignField(dropdown);
                BindTooltip(dropdown, "scope.locale.tooltip");

                // Apply() による choices/value の更新が value-changed を巻き戻し発火させ、古い値で
                // SetActiveLocale を再実行する（カタログ/言語変化時のズレ）のを防ぐガード。
                var applying = false;
                void Apply()
                {
                    applying = true;
                    tags.Clear();
                    var labels = new List<string>();
                    tags.Add("");
                    labels.Add(Tr("scope.followGlobal"));

                    var currentLocales = EditorL10n.GetLocales(scope);
                    foreach (var locale in currentLocales)
                    {
                        tags.Add(locale.Tag);
                        labels.Add(locale.DisplayName);
                    }

                    var explicitLocale = EditorL10n.NormalizeLocaleTag(EditorL10nPreferences.GetScopeLocale(scope));
                    var index = tags.IndexOf(explicitLocale);
                    if (index < 0 && !string.IsNullOrEmpty(explicitLocale))
                    {
                        tags.Add(explicitLocale);
                        labels.Add(Tr("outOfCatalog", explicitLocale));
                        index = tags.Count - 1;
                    }
                    if (index < 0) index = 0;

                    dropdown.label = Tr("scope.locale.label");
                    dropdown.choices = labels;
                    dropdown.SetValueWithoutNotify(labels[index]);
                    applying = false;
                }

                dropdown.RegisterValueChangedCallback(_ =>
                {
                    if (applying) return;
                    var index = dropdown.index;
                    if (index >= 0 && index < tags.Count)
                        EditorL10n.SetActiveLocale(scope, tags[index]);
                });

                Apply();
                EditorL10nUi.RegisterLocaleCallback(dropdown, Apply);
                return dropdown;
            }

            /// <summary>pill と meta を現在の解決状態から再計算する（言語変更時に in-place 更新）。</summary>
            public void UpdateState()
            {
                if (!EditorL10n.TryGetScopeInfo(Scope, out var info))
                    info = new EditorL10nScopeInfo(Scope, "", "");

                var locales = EditorL10n.GetLocales(Scope).ToArray();
                var explicitLocale = EditorL10n.NormalizeLocaleTag(EditorL10nPreferences.GetScopeLocale(Scope));
                var requested = EditorL10n.GetActiveLocale(Scope, out var source);
                var resolved = ResolveAvailableLocale(requested, info.DefaultLocale, locales);

                // 由来（source）は解決順を集約した EditorL10n.GetActiveLocale から受け取る（二重持ちを避ける）。
                _meta.text = Tr("scope.meta", FormatLocaleTag(resolved), Tr(SourceKey(source)), FormatLocaleTag(info.DefaultLocale));

                EditorL10nUiKit.SetBadge(_overridePill,
                    string.IsNullOrEmpty(explicitLocale) ? "" : Tr("pill.override"),
                    EditorL10nBadgeKind.Accent);

                var requestUnavailable = !string.IsNullOrEmpty(requested) && requested != resolved;
                EditorL10nUiKit.SetBadge(_fallbackPill,
                    requestUnavailable ? Tr("pill.fallback") : "",
                    EditorL10nBadgeKind.Warning);

                if (requestUnavailable)
                {
                    var noteKey = string.IsNullOrEmpty(resolved) ? "scope.outOfCatalogNote" : "scope.fallbackNote";
                    _fallbackNote.text = Tr(noteKey, requested);
                    _fallbackNote.style.display = DisplayStyle.Flex;
                }
                else
                {
                    _fallbackNote.style.display = DisplayStyle.None;
                }

                UpdateChain(requested, info.DefaultLocale, locales);
            }

            // fallback 連鎖（要求 → 親 → defaultLocale）をチップ列で描画し、実際に翻訳が当たった段を強調する。
            // locale を持たない scope では _chainRow を生成しないため、その場合は何もしない。
            private void UpdateChain(string requested, string defaultLocale, EditorL10nLocaleInfo[] locales)
            {
                if (_chainRow == null)
                    return;

                _chainRow.Clear();

                var label = new Label(Tr("chain.label"));
                label.AddToClassList("l10n-scope-chain__label");
                label.tooltip = Tr("chain.tooltip");
                _chainRow.Add(label);

                var available = new HashSet<string>(
                    locales.Where(locale => locale != null && !string.IsNullOrEmpty(locale.Tag)).Select(locale => locale.Tag));

                var chain = EditorL10n.BuildFallbackChain(requested, defaultLocale).ToArray();
                var hitIndex = Array.FindIndex(chain, tag => available.Contains(tag));

                for (var i = 0; i < chain.Length; i++)
                {
                    if (i > 0)
                    {
                        var separator = new Label("›");
                        separator.AddToClassList("l10n-chain-sep");
                        _chainRow.Add(separator);
                    }

                    var step = new Label(chain[i]);
                    step.AddToClassList("l10n-chain-step");
                    if (hitIndex >= 0 && i == hitIndex)
                        step.AddToClassList("l10n-chain-step--used");   // 実際に表示へ使われた段
                    else if (hitIndex >= 0 && i > hitIndex)
                        step.AddToClassList("l10n-chain-step--skipped"); // 使用段より後＝探索に到達しない段
                    _chainRow.Add(step);
                }
            }
        }

        // ===== 共有ヘルパー =====
        // 表示ロケールの由来 enum を翻訳キーへ対応付ける（meta 行の source 表示用）。
        private static string SourceKey(EditorL10nLocaleSource source) => source switch
        {
            EditorL10nLocaleSource.ScopeOverride => "source.scopeOverride",
            EditorL10nLocaleSource.Global => "source.global",
            EditorL10nLocaleSource.System => "source.system",
            _ => "source.default",
        };

        private static bool ScopeMatchesSearch(string scope, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return true;
            return (scope ?? "").IndexOf(searchText.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ScopeHasUnavailableRequest(string scope)
        {
            if (!EditorL10n.TryGetScopeInfo(scope, out var info))
                return false;
            var locales = EditorL10n.GetLocales(scope).ToArray();
            var requested = EditorL10n.GetActiveLocale(scope);
            var resolved = ResolveAvailableLocale(requested, info.DefaultLocale, locales);
            return !string.IsNullOrEmpty(requested) && requested != resolved;
        }

        private static string ResolveAvailableLocale(string requestedLocale, string defaultLocale, EditorL10nLocaleInfo[] locales)
        {
            var available = new HashSet<string>(
                locales.Where(locale => locale != null && !string.IsNullOrEmpty(locale.Tag)).Select(locale => locale.Tag));

            foreach (var candidate in EditorL10n.BuildFallbackChain(requestedLocale, defaultLocale))
            {
                if (available.Contains(candidate))
                    return candidate;
            }
            return "";
        }

        private static IEnumerable<EditorL10nLocaleInfo> GetGlobalLocaleOptions()
        {
            var byTag = new Dictionary<string, EditorL10nLocaleInfo>();
            foreach (var scope in EditorL10n.GetScopes())
            {
                foreach (var locale in EditorL10n.GetLocales(scope))
                {
                    if (locale == null || string.IsNullOrEmpty(locale.Tag) || byTag.ContainsKey(locale.Tag))
                        continue;
                    byTag.Add(locale.Tag, locale);
                }
            }
            return byTag.Values.OrderBy(locale => locale.Tag);
        }

        private static string FormatLocaleTag(string locale)
        {
            return string.IsNullOrEmpty(locale) ? Tr("locale.unset") : locale;
        }

        private static void PingAsset(string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }

        private static StyleColor ResolveColor(EditorL10nBadgeKind kind)
        {
            // 検証結果テキストの色。USS トークンに合わせた近似値（両スキンで可読）。
            var pro = EditorGUIUtility.isProSkin;
            return kind switch
            {
                EditorL10nBadgeKind.Ok => new StyleColor(pro ? new Color(0.475f, 0.816f, 0.541f) : new Color(0.145f, 0.416f, 0.173f)),
                EditorL10nBadgeKind.Warning => new StyleColor(pro ? new Color(0.902f, 0.757f, 0.361f) : new Color(0.541f, 0.427f, 0.063f)),
                EditorL10nBadgeKind.Error => new StyleColor(pro ? new Color(0.941f, 0.549f, 0.510f) : new Color(0.698f, 0.188f, 0.149f)),
                _ => new StyleColor(pro ? new Color(0.604f, 0.604f, 0.604f) : new Color(0.353f, 0.353f, 0.353f)),
            };
        }
    }

    internal static class VisualElementExtensions
    {
        /// <summary>生成した要素にその場で副作用（バインド等）を適用しつつ要素を返す小さなヘルパー。</summary>
        public static T Also<T>(this T element, Action<T> action) where T : VisualElement
        {
            action?.Invoke(element);
            return element;
        }
    }
}
#endif
