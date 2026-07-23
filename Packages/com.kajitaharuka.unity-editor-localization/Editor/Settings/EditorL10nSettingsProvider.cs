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
    /// （左=アイデンティティ＋概況、右=ドキュメント等の汎用 chrome）の下に、折りたたみ可能な
    /// 5 つの大項目（表示言語 / scope 個別設定 / カタログ / AIエージェント連携スキル / 開発者向け）を
    /// 並べる。開閉状態は EditorPrefs（ユーザーごと）に永続化し、畳んだ見出しにも要約ピルで概況を出す。
    /// カタログの大項目には scope ごとのファイル一覧（manifest / 各 locale テーブル）と検証結果を
    /// 由来 scope 単位で統合表示する。画面自身の文言も自身の翻訳カタログ（scope=UiScope）から引くことで
    /// 多言語表示・言語切替に追従させる（ドッグフーディング）。
    /// </summary>
    internal static class EditorL10nSettingsProvider
    {
        // この設定画面自身の UI 文言を引く scope（Editor/Localization の manifest と一致させる）。
        private const string UiScope = EditorL10nPackage.Name;

        // Preferences のこの設定ページのパス。CreateProvider とメニューから開く導線（スキルインストーラの
        // 間接方式メニュー含む）で共有する。同一 assembly の EditorL10nSkillInstaller からも参照するため internal。
        internal const string SettingsPath = "Preferences/UnityEditorLocalization";

        // 大項目の開閉状態を保存する EditorPrefs キーの前置詞（ユーザーごと。プロジェクト資産へ書かない方針と一貫）。
        private const string SectionPrefsPrefix = "Kajitaharuka.EditorLocalization.Section.";

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
            // 絞り込みの適用先は scope 個別設定（設定カード）のみ。カタログ節の scope グループは
            // 折りたたみで密度を保つ方針とし、別節の表示を書き換える暗黙の副作用を持たせない。
            private TextField _search;
            private string[] _builtScopes = Array.Empty<string>();
            private readonly List<ScopeCard> _cards = new();

            // 折りたたみセクションの見出し要約ピル（畳んだままでも概況が読めるようにする）。
            private Label _globalSummary;
            private Label _scopeSummary;
            private Label _catalogsSummaryErrors;
            private Label _catalogsSummaryWarnings;

            // カタログ節の表示部。要約行（_catalogsResult）・検証時刻（_validatedAtHint）・scope 別グループ
            // （_catalogGroups: ファイル一覧＋検証結果）の参照を保持し、言語変更時に保持したスナップショット
            // （_lastValidation）で再描画して画面言語へ追従させる。
            private Label _catalogsResult;
            private Label _validatedAtHint;
            private VisualElement _catalogGroups;
            private HelpBox _catalogsEmpty;
            private EditorL10nValidationResult _lastValidation;
            private DateTime _lastValidatedAt;
            // scope グループの開閉状態（ユーザー操作を記憶し、検証/言語変更の再描画でも保持する）。
            private readonly Dictionary<string, bool> _groupExpanded = new();

            // AIエージェント連携スキルの登録状態ピル（登録先ごと）。
            private Label _userSkillStatePill;
            private Label _projectSkillStatePill;

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
                // 主要操作（表示言語）を上部に保ち、カタログの保守/検証はその下に置く（作業順・主要操作の明確化）。
                scroll.Add(BuildGlobalSection());
                scroll.Add(BuildScopeSection());
                scroll.Add(BuildCatalogs());
                scroll.Add(BuildSkillsSection());
                scroll.Add(BuildDeveloperSection());

                _builtScopes = EditorL10n.GetScopes().ToArray();
                RebuildScopeList(_builtScopes);
                RenderCatalogGroups();
                UpdateScopeSummary();
                UpdateOverviewBadge();

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
                return header;
            }

            // ===== 折りたたみセクションの共通生成 =====
            // タイトルは翻訳キーへバインドし、開閉状態は EditorPrefs（ユーザーごと）に永続化する。
            private VisualElement BuildSection(string titleKey, string prefsSuffix, bool defaultExpanded,
                out VisualElement content, out VisualElement summary)
            {
                var card = EditorL10nUiKit.CollapsibleSection(
                    Tr(titleKey), SectionPrefsPrefix + prefsSuffix, defaultExpanded, out content, out summary);
                BindLabel(card.Q<Label>(className: "eui-section__title"), titleKey);
                return card;
            }

            // ===== 表示言語（グローバル）=====
            private VisualElement BuildGlobalSection()
            {
                var card = BuildSection("global.title", "global", true, out var content, out var summary);

                // 畳んだままでも「現在どの言語が・どの由来で」効いているかが分かる要約ピル。
                _globalSummary = EditorL10nUiKit.Pill("", EditorL10nBadgeKind.Neutral);
                summary.Add(_globalSummary);
                UpdateGlobalSummary();

                // 保存先と解決順の説明。表示言語の話なのでこの節に属させる（ヘッダー直下から移設）。
                content.Add(EditorL10nUiKit.Note(Tr("note")).Also(label => BindLabel(label, "note")));

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

            // グローバル節の要約ピル: 未設定時はシステム言語/既定への解決先を出し、規則の暗記に頼らせない。
            private void UpdateGlobalSummary()
            {
                if (_globalSummary == null)
                    return;

                var globalLocale = EditorL10n.GetGlobalLocale();
                string text;
                if (!string.IsNullOrEmpty(globalLocale))
                {
                    text = Tr("summary.localeWithSource", globalLocale, Tr("source.global"));
                }
                else
                {
                    var systemLocale = EditorL10n.GetSystemLocaleFallbackEnabled() ? EditorL10n.GetSystemLocale() : "";
                    text = string.IsNullOrEmpty(systemLocale)
                        ? Tr("summary.localeWithSource", Tr("locale.unset"), Tr("source.default"))
                        : Tr("summary.localeWithSource", systemLocale, Tr("source.system"));
                }

                EditorL10nUiKit.SetBadge(_globalSummary, text, EditorL10nBadgeKind.Neutral);
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
                var card = BuildSection("scope.title", "scopes", true, out var content, out var summary);

                // 畳んだままでも「いくつの scope があり、いくつ個別設定中か」が分かる要約ピル。
                _scopeSummary = EditorL10nUiKit.Pill("", EditorL10nBadgeKind.Neutral);
                summary.Add(_scopeSummary);

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

            // scope 節の要約ピル: 総数と個別設定中の数。
            private void UpdateScopeSummary()
            {
                if (_scopeSummary == null)
                    return;

                var total = _builtScopes.Length;
                var overrides = _builtScopes.Count(scope => !string.IsNullOrEmpty(EditorL10nPreferences.GetScopeLocale(scope)));
                EditorL10nUiKit.SetBadge(_scopeSummary,
                    total == 0 ? "" : Tr("scope.summary", total, overrides),
                    EditorL10nBadgeKind.Neutral);
            }

            // ===== カタログ（作成 / 検証 / 再読み込み / scope 別のファイル一覧＋検証結果）=====
            // scope ごとのグループに「カタログのファイル一覧（常時）」と「検証結果（スナップショット）」を
            // 同居させ、どの scope のどのファイル・どの警告かを 1 箇所で追えるようにする。
            private VisualElement BuildCatalogs()
            {
                var card = BuildSection("catalogs.title", "catalogs", true, out var content, out var summary);

                // 畳んだ状態でも最終検証の概況が分かる件数ピル（未検証の間は非表示）。
                _catalogsSummaryErrors = EditorL10nUiKit.Pill("", EditorL10nBadgeKind.Error);
                _catalogsSummaryWarnings = EditorL10nUiKit.Pill("", EditorL10nBadgeKind.Warning);
                summary.Add(_catalogsSummaryErrors);
                summary.Add(_catalogsSummaryWarnings);
                UpdateCatalogsSummaryPills();

                var row = new VisualElement();
                row.AddToClassList("l10n-catalogs");

                var result = new Label { name = "l10n-catalogs-result" };
                result.AddToClassList("l10n-catalogs__result");
                result.style.display = DisplayStyle.None;
                _catalogsResult = result;

                // 検証結果の鮮度を示す時刻行（スナップショットがいつ時点かを正直に出す）。
                _validatedAtHint = EditorL10nUiKit.HintRow("");
                _validatedAtHint.style.display = DisplayStyle.None;
                EditorL10nUi.RegisterLocaleCallback(_validatedAtHint, UpdateValidatedAtHint);

                // scope 別グループ（ファイル一覧＋検証結果）の描画先。
                var groups = new VisualElement();
                groups.AddToClassList("l10n-validation-groups");
                _catalogGroups = groups;

                // カタログが 1 つも無いときの空状態（次の一手＝作成ウィザードへの導線を添える）。
                _catalogsEmpty = EditorL10nUiKit.InfoBox(Tr("catalogs.empty"));
                _catalogsEmpty.style.display = DisplayStyle.None;
                EditorL10nUi.RegisterLocaleCallback(_catalogsEmpty, () => _catalogsEmpty.text = Tr("catalogs.empty"));

                // この節の主操作は「検証」。先頭に置き、保守（再読み込み）と作成をその後ろに並べる。
                var validate = EditorL10nUiKit.ActionButton(Tr("catalogs.validate"), RunValidation, Tr("catalogs.validate.tooltip"));
                BindButtonText(validate, "catalogs.validate", "catalogs.validate.tooltip");

                var reload = EditorL10nUiKit.ActionButton(Tr("catalogs.reload"), () =>
                {
                    // Reload でカタログが入れ替わると前回の検証結果は古くなる。先に破棄してから再読込することで、
                    // Reload が同期発火する LocaleChanged（→OnLocaleChanged）が古い検証結果を描き直さないようにする。
                    _lastValidation = null;
                    EditorL10n.Reload();
                    EditorL10nUiKit.SetInlineResult(result, Tr("catalogs.reloaded"), EditorL10nBadgeKind.Neutral);
                }, Tr("catalogs.reload.tooltip"));
                BindButtonText(reload, "catalogs.reload", "catalogs.reload.tooltip");

                // カタログ作成ウィザードへの導線（初回導線。空状態の「次の一手」でもある）。
                // ボタン文言はウィザードのタイトルを共有し、ダイアログを開く操作の慣習として「…」を添える。
                var create = EditorL10nUiKit.ActionButton(Tr("wizard.title") + "…", EditorL10nCatalogWizard.Open, Tr("catalogs.create.tooltip"));
                void ApplyCreateText()
                {
                    create.text = Tr("wizard.title") + "…";
                    create.tooltip = Tr("catalogs.create.tooltip");
                }
                EditorL10nUi.RegisterLocaleCallback(create, ApplyCreateText);

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

                row.Add(validate);
                row.Add(reload);
                row.Add(create);
                row.Add(helpToggle);
                row.Add(result);

                content.Add(row);
                content.Add(_validatedAtHint);
                content.Add(help);
                content.Add(_catalogsEmpty);
                content.Add(groups);
                return card;
            }

            // 検証を実行し、結果表示（要約行・時刻・scope 別グループ・要約ピル・ヘッダーバッジ）を更新する。
            private void RunValidation()
            {
                // ValidateAndLog 内の Reload が LocaleChanged を同期発火し OnLocaleChanged が先に走るため、
                // 古い結果を先に破棄しておく（過渡状態でも古い結果を新鮮な顔で描かない）。
                _lastValidation = null;
                var validation = EditorL10nValidator.ValidateAndLog();
                _lastValidation = validation;
                _lastValidatedAt = DateTime.Now;
                UpdateCatalogsResultLine();
                UpdateValidatedAtHint();
                RenderCatalogGroups();
                UpdateCatalogsSummaryPills();
                UpdateOverviewBadge();
            }

            // 検証結果の要約行を現在のスナップショット（_lastValidation）から（再）描画する。言語変更にも追従させる。
            // 色は内容と矛盾させない: エラーあり=エラー色 / 警告のみ=警告色 / 問題なし=OK 色。
            private void UpdateCatalogsResultLine()
            {
                if (_catalogsResult == null || _lastValidation == null)
                    return;
                if (_lastValidation.ErrorCount > 0)
                    EditorL10nUiKit.SetInlineResult(_catalogsResult, Tr("catalogs.result.issues", _lastValidation.ErrorCount, _lastValidation.WarningCount), EditorL10nBadgeKind.Error);
                else if (_lastValidation.WarningCount > 0)
                    EditorL10nUiKit.SetInlineResult(_catalogsResult, Tr("catalogs.result.warnings", _lastValidation.WarningCount), EditorL10nBadgeKind.Warning);
                else
                    EditorL10nUiKit.SetInlineResult(_catalogsResult, Tr("catalogs.result.ok"), EditorL10nBadgeKind.Ok);
            }

            // 検証時刻の表示（スナップショットが無ければ隠す）。
            private void UpdateValidatedAtHint()
            {
                if (_validatedAtHint == null)
                    return;
                if (_lastValidation == null)
                {
                    _validatedAtHint.style.display = DisplayStyle.None;
                    return;
                }

                _validatedAtHint.text = Tr("catalogs.validatedAt", _lastValidatedAt.ToString("HH:mm:ss"));
                _validatedAtHint.style.display = DisplayStyle.Flex;
            }

            // カタログ節の見出し要約ピル（エラー/警告件数）。未検証なら両方隠す。
            private void UpdateCatalogsSummaryPills()
            {
                if (_catalogsSummaryErrors == null || _catalogsSummaryWarnings == null)
                    return;

                var errors = _lastValidation?.ErrorCount ?? 0;
                var warnings = _lastValidation?.WarningCount ?? 0;
                EditorL10nUiKit.SetBadge(_catalogsSummaryErrors,
                    errors > 0 ? Tr("catalogs.count.errors", errors) : "", EditorL10nBadgeKind.Error);
                EditorL10nUiKit.SetBadge(_catalogsSummaryWarnings,
                    warnings > 0 ? Tr("catalogs.count.warnings", warnings) : "", EditorL10nBadgeKind.Warning);
            }

            // scope 別グループ（ファイル一覧＋検証結果）を現在のカタログ構成から描き直す。
            // 検証結果は point-in-time のスナップショット（_lastValidation。null なら未検証＝ファイル一覧のみ）。
            // 言語変更時にも呼び直して行の文言・tooltip を新しい表示言語で作り直す（行ごとの言語バインドは
            // 張らない＝購読リークを避ける）。
            private void RenderCatalogGroups()
            {
                if (_catalogGroups == null)
                    return;

                _catalogGroups.Clear();

                var scopes = EditorL10n.GetScopes();
                if (scopes.Count == 0)
                {
                    _catalogGroups.style.display = DisplayStyle.None;
                    if (_catalogsEmpty != null)
                        _catalogsEmpty.style.display = DisplayStyle.Flex;
                    return;
                }

                if (_catalogsEmpty != null)
                    _catalogsEmpty.style.display = DisplayStyle.None;
                _catalogGroups.style.display = DisplayStyle.Flex;

                // scope -> issue 群（scope 内の追加順は維持）。
                var byScope = new Dictionary<string, List<EditorL10nValidationIssue>>();
                if (_lastValidation != null)
                {
                    foreach (var issue in _lastValidation.Issues)
                    {
                        if (!byScope.TryGetValue(issue.Scope, out var list))
                        {
                            list = new List<EditorL10nValidationIssue>();
                            byScope[issue.Scope] = list;
                        }
                        list.Add(issue);
                    }
                }

                foreach (var scope in scopes)
                {
                    byScope.TryGetValue(scope, out var issues);
                    _catalogGroups.Add(BuildCatalogGroup(scope, issues));
                }
            }

            // 1 scope ぶんのグループ（折りたたみ可能）。ヘッダーに件数ピル（検証済みで問題なしは ✓）、
            // 本文にファイル一覧（manifest → 各 locale テーブル）と検証 issue 行を並べる。
            private VisualElement BuildCatalogGroup(string scope, List<EditorL10nValidationIssue> issues)
            {
                // 1 深刻度あたりの表示上限。巨大な scope（数十 key 欠落など）で UI が縦に伸び切るのを防ぐ。
                // 全件は常に Console（ValidateAndLog）へ出ているので、超過分は件数だけ示して Console へ誘導する。
                const int maxRowsPerSeverity = 30;

                var errors = issues?.Where(issue => issue.Severity == EditorL10nValidationSeverity.Error).ToList()
                             ?? new List<EditorL10nValidationIssue>();
                var warnings = issues?.Where(issue => issue.Severity == EditorL10nValidationSeverity.Warning).ToList()
                               ?? new List<EditorL10nValidationIssue>();

                var group = new VisualElement();
                group.AddToClassList("l10n-vgroup");

                var head = new VisualElement();
                head.AddToClassList("l10n-vgroup__head");

                var chevron = EditorL10nUiKit.Chevron();

                var body = new VisualElement();
                body.AddToClassList("l10n-vgroup__body");

                // 開閉はユーザー操作を scope 単位で記憶し、再描画（検証/言語変更）でも保持する。
                // 触られていない scope の既定は「エラーを含むなら展開、それ以外は畳む」（ファイル一覧は二次的詳細）。
                var expanded = _groupExpanded.TryGetValue(scope, out var stored) ? stored : errors.Count > 0;
                void SetExpanded(bool value)
                {
                    expanded = value;
                    _groupExpanded[scope] = value;
                    body.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
                    chevron.text = value ? "▾" : "▸";
                }
                chevron.clicked += () => SetExpanded(!expanded);

                var name = new Label(EditorL10nUiKit.InsertWrapOpportunities(scope));
                name.AddToClassList("l10n-vgroup__name");
                name.tooltip = scope;

                // 件数ピル（畳んだ状態でも深刻度の概況が分かる）。数を含むので未翻訳化されず多言語でも一意。
                // 検証済みで問題の無い scope は言語非依存の ✓（記号なので翻訳キーにしない）で「検査済み・問題なし」を示す。
                var pills = new VisualElement();
                pills.AddToClassList("l10n-scope-card__pills");
                if (errors.Count > 0)
                    pills.Add(EditorL10nUiKit.Pill(Tr("catalogs.count.errors", errors.Count), EditorL10nBadgeKind.Error));
                if (warnings.Count > 0)
                    pills.Add(EditorL10nUiKit.Pill(Tr("catalogs.count.warnings", warnings.Count), EditorL10nBadgeKind.Warning));
                if (_lastValidation != null && errors.Count == 0 && warnings.Count == 0)
                    pills.Add(EditorL10nUiKit.Pill("✓", EditorL10nBadgeKind.Ok));

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

                // 本文前半: カタログのファイル一覧（manifest 行 → 各 locale テーブル行）。クリックで選択+Ping。
                if (EditorL10n.TryGetScopeInfo(scope, out var info) && !string.IsNullOrEmpty(info.ManifestPath))
                {
                    var manifestPath = info.ManifestPath;
                    body.Add(EditorL10nUiKit.AssetRow(manifestPath, Tr("scope.manifest.tooltip"), () => PingAsset(manifestPath)));
                }
                foreach (var locale in EditorL10n.GetLocales(scope))
                    body.Add(BuildTableFileRow(scope, info, locale));

                // 本文後半: 検証結果（エラー → 警告 の順に深刻なものを上に）。ファイル一覧と区切って示す。
                if (errors.Count > 0 || warnings.Count > 0)
                {
                    body.Add(EditorL10nUiKit.Separator());
                    foreach (var issue in errors.Take(maxRowsPerSeverity))
                        body.Add(BuildValidationIssueRow(issue));
                    if (errors.Count > maxRowsPerSeverity)
                        body.Add(EditorL10nUiKit.HintRow(Tr("catalogs.more", errors.Count - maxRowsPerSeverity)));
                    foreach (var issue in warnings.Take(maxRowsPerSeverity))
                        body.Add(BuildValidationIssueRow(issue));
                    if (warnings.Count > maxRowsPerSeverity)
                        body.Add(EditorL10nUiKit.HintRow(Tr("catalogs.more", warnings.Count - maxRowsPerSeverity)));
                }

                SetExpanded(expanded);

                group.Add(head);
                group.Add(body);
                return group;
            }

            // locale テーブル 1 行: [locale タグ][既定ピル][パス行（クリックで選択）][key 数]。
            // manifest が宣言しているのにファイルが無いテーブルは、パスを淡色表示し欠落マーカー（×）を添える
            // （検証を走らせる前から異常が見える）。
            private VisualElement BuildTableFileRow(string scope, EditorL10nScopeInfo info, EditorL10nLocaleInfo locale)
            {
                var row = new VisualElement();
                row.AddToClassList("l10n-file-row");

                var tagPill = EditorL10nUiKit.Pill(locale.Tag, EditorL10nBadgeKind.Neutral);
                tagPill.tooltip = locale.DisplayName;
                row.Add(tagPill);

                if (info != null && locale.Tag == info.DefaultLocale)
                    row.Add(EditorL10nUiKit.Pill(Tr("catalogs.pill.default"), EditorL10nBadgeKind.Accent));

                EditorL10n.TryGetLocaleTablePath(scope, locale.Tag, out var tablePath);
                var exists = !string.IsNullOrEmpty(tablePath) && AssetDatabase.LoadAssetAtPath<TextAsset>(tablePath) != null;
                if (exists)
                {
                    var pathToPing = tablePath;
                    row.Add(EditorL10nUiKit.AssetRow(tablePath, Tr("catalogs.table.tooltip"), () => PingAsset(pathToPing)));

                    // key 数（翻訳の進み具合の一目把握）。数値のみの表示なので翻訳キーにしない（tooltip で意味を回復）。
                    if (EditorL10n.TryGetEntryCount(scope, locale.Tag, out var count))
                    {
                        var countPill = EditorL10nUiKit.Pill(count.ToString(), EditorL10nBadgeKind.Neutral);
                        countPill.tooltip = Tr("catalogs.entries.tooltip");
                        row.Add(countPill);
                    }
                }
                else
                {
                    var missing = new Label(EditorL10nUiKit.InsertWrapOpportunities(
                        string.IsNullOrEmpty(tablePath) ? locale.Tag : tablePath));
                    missing.AddToClassList("l10n-file-row__missing");
                    missing.tooltip = Tr("catalogs.table.missing.tooltip");
                    row.Add(missing);

                    var mark = EditorL10nUiKit.Pill("×", EditorL10nBadgeKind.Error);
                    mark.tooltip = Tr("catalogs.table.missing.tooltip");
                    row.Add(mark);
                }

                return row;
            }

            // issue 1 件の行: 深刻度マーカー（色＋形）／locale チップ／詳細メッセージ／行末のクイックfix。
            // マーカーを行頭に固定し（縦の走査線を崩さない）、操作ボタンは行末に置く（IDE の慣習）。長文は折り返す。
            private VisualElement BuildValidationIssueRow(EditorL10nValidationIssue issue)
            {
                var row = new VisualElement();
                row.AddToClassList("l10n-vissue");

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
                    // 再検証して結果表示を更新する。RenderCatalogGroups はこの行ボタン自身も作り直すが、
                    // 上の書き込み〜Reload は同期完了しており、このコールバックは既に return 済みなので安全
                    // （いずれかの工程を非同期化する場合は detach 済み要素の使用に注意）。
                    _lastValidation = EditorL10nValidator.ValidateAll();
                    _lastValidatedAt = DateTime.Now;
                    UpdateCatalogsResultLine();
                    UpdateValidatedAtHint();
                    RenderCatalogGroups();
                    UpdateCatalogsSummaryPills();
                    UpdateOverviewBadge();
                    Debug.Log($"EditorLocalization: {scope}/{locale} に key を追加しました: {key}");
                }
                catch (Exception exception)
                {
                    Debug.LogError($"EditorLocalization: key の追加に失敗しました: {exception}");
                    if (_catalogsResult != null)
                        EditorL10nUiKit.SetInlineResult(_catalogsResult, Tr("quickfix.failed"), EditorL10nBadgeKind.Error);
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

            // ===== AIエージェント連携スキル（同梱スキルの登録）=====
            private VisualElement BuildSkillsSection()
            {
                var card = BuildSection("skills.title", "skills", true, out var content, out _);
                content.Add(EditorL10nUiKit.Note(Tr("skills.note")).Also(label => BindLabel(label, "skills.note")));

                content.Add(BuildSkillRow("editor-localization-translation-quality",
                    "skills.translation.name", "skills.translation.desc", "skills.translation.samplePrompt"));
                content.Add(BuildSkillRow("editor-localization-optional-integration",
                    "skills.optional.name", "skills.optional.desc", "skills.optional.samplePrompt"));

                // 操作結果のインライン表示（登録/コピーの両方からここへ出す）。列フローに直接置くため
                // 行専用（flex-basis:100%）の class ではなく列用の class を使う（軸反転で他要素を潰さない）。
                var result = new Label { name = "l10n-skills-result" };
                result.AddToClassList("eui-inline-result");
                result.style.display = DisplayStyle.None;

                // 登録先ごとに「登録ボタン＋登録状態ピル」を 1 行で並べ、押す前から状態が見えるようにする。
                _userSkillStatePill = EditorL10nUiKit.Pill("", EditorL10nBadgeKind.Neutral);
                _projectSkillStatePill = EditorL10nUiKit.Pill("", EditorL10nBadgeKind.Neutral);

                var installUser = EditorL10nUiKit.ActionButton(Tr("skills.install.user"), () =>
                {
                    Debug.Log(EditorL10nSkillInstaller.InstallToUser());
                    EditorL10nUiKit.SetInlineResult(result, Tr("skills.result.installed"), EditorL10nBadgeKind.Ok);
                    UpdateSkillStatePills();
                }, Tr("skills.install.user.tooltip"));
                BindButtonText(installUser, "skills.install.user", "skills.install.user.tooltip");

                var installProject = EditorL10nUiKit.ActionButton(Tr("skills.install.project"), () =>
                {
                    Debug.Log(EditorL10nSkillInstaller.InstallToProject());
                    EditorL10nUiKit.SetInlineResult(result, Tr("skills.result.installed"), EditorL10nBadgeKind.Ok);
                    UpdateSkillStatePills();
                }, Tr("skills.install.project.tooltip"));
                BindButtonText(installProject, "skills.install.project", "skills.install.project.tooltip");

                var userRow = new VisualElement();
                userRow.AddToClassList("l10n-install-row");
                userRow.Add(installUser);
                userRow.Add(_userSkillStatePill);
                content.Add(userRow);

                var projectRow = new VisualElement();
                projectRow.AddToClassList("l10n-install-row");
                projectRow.Add(installProject);
                projectRow.Add(_projectSkillStatePill);
                content.Add(projectRow);

                // 状態ピルの文言は言語変更にも追従させる（文言・状態とも UpdateSkillStatePills に一本化）。
                EditorL10nUi.RegisterLocaleCallback(_userSkillStatePill, UpdateSkillStatePills);
                UpdateSkillStatePills();

                // CLI で追加したい場合の案内＋コマンド明示＋横のコピーボタン。
                content.Add(EditorL10nUiKit.Note(Tr("skills.cli.note")).Also(label => BindLabel(label, "skills.cli.note")));

                var cliRow = new VisualElement();
                cliRow.AddToClassList("l10n-cli-row");

                // 読み取り専用の複数行フィールドにコマンドを表示（選択もできる）。値変更時は再表示するだけ。
                var cliField = new TextField { multiline = true, isReadOnly = true };
                cliField.AddToClassList("l10n-cli-field");
                cliField.value = EditorL10nSkillInstaller.CliSnippetForUser() + "\n" + EditorL10nSkillInstaller.CliSnippetForProject();

                var copyCli = EditorL10nUiKit.ActionButton(Tr("skills.cli.copy"), () =>
                {
                    EditorGUIUtility.systemCopyBuffer = cliField.value;
                    EditorL10nUiKit.SetInlineResult(result, Tr("skills.result.copied"), EditorL10nBadgeKind.Neutral);
                }, Tr("skills.cli.copy.tooltip"));
                copyCli.style.flexShrink = 0;
                BindButtonText(copyCli, "skills.cli.copy", "skills.cli.copy.tooltip");

                cliRow.Add(cliField);
                cliRow.Add(copyCli);
                content.Add(cliRow);

                content.Add(result);
                return card;
            }

            // 登録先 2 スコープの登録状態を確認してピルへ反映する（登録操作後・言語変更時に呼ぶ）。
            private void UpdateSkillStatePills()
            {
                ApplySkillStatePill(_userSkillStatePill, EditorL10nSkillInstaller.GetUserInstallState());
                ApplySkillStatePill(_projectSkillStatePill, EditorL10nSkillInstaller.GetProjectInstallState());
            }

            private static void ApplySkillStatePill(Label pill, EditorL10nSkillInstallState state)
            {
                if (pill == null)
                    return;

                switch (state)
                {
                    case EditorL10nSkillInstallState.Installed:
                        EditorL10nUiKit.SetBadge(pill, Tr("skills.status.installed"), EditorL10nBadgeKind.Ok);
                        break;
                    case EditorL10nSkillInstallState.NeedsReinstall:
                        EditorL10nUiKit.SetBadge(pill, Tr("skills.status.reinstall"), EditorL10nBadgeKind.Warning);
                        break;
                    default:
                        EditorL10nUiKit.SetBadge(pill, Tr("skills.status.notInstalled"), EditorL10nBadgeKind.Neutral);
                        break;
                }

                pill.tooltip = Tr("skills.status.tooltip");
            }

            // 同梱スキル 1 件の表示（名前＋説明＋プロンプト例＋正本フォルダへの導線）。言語変更に追従させる。
            private VisualElement BuildSkillRow(string skillFolder, string nameKey, string descKey, string promptKey)
            {
                var box = new VisualElement();
                box.AddToClassList("l10n-skill");

                var name = new Label(Tr(nameKey));
                name.AddToClassList("l10n-skill__name");
                BindLabel(name, nameKey);

                box.Add(name);
                box.Add(EditorL10nUiKit.HintRow(Tr(descKey)).Also(label => BindLabel(label, descKey)));

                // 導入者が「エージェントへ何を頼めるスキルか」を具体例 1 行で掴めるようにする（認識 > 想起）。
                var prompt = EditorL10nUiKit.HintRow("");
                void ApplyPrompt() => prompt.text = Tr("skills.samplePrompt.label") + " " + Tr(promptKey);
                ApplyPrompt();
                EditorL10nUi.RegisterLocaleCallback(prompt, ApplyPrompt);
                box.Add(prompt);

                // 登録されるスキルの正本フォルダへの導線。クリックで Project ビューに選択表示し、
                // 導入者が登録前に SKILL.md を含む全文を確認できるようにする。
                var folderPath = "Packages/" + EditorL10nPackage.Name + "/skills/" + skillFolder;
                var folderRow = EditorL10nUiKit.AssetRow(folderPath, Tr("skills.folder.tooltip"),
                    () => SelectSkillFolder(folderPath));
                EditorL10nUi.RegisterLocaleCallback(folderRow, () => folderRow.tooltip = Tr("skills.folder.tooltip"));
                box.Add(folderRow);
                return box;
            }

            // スキルの正本フォルダを Project ビューで選択・強調表示する（解決できない場合は何もしない）。
            private static void SelectSkillFolder(string assetPath)
            {
                var folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (folder == null)
                    return;
                Selection.activeObject = folder;
                EditorGUIUtility.PingObject(folder);
            }

            // ===== 開発者向け =====
            private VisualElement BuildDeveloperSection()
            {
                // 他の大項目と同じ折りたたみ語彙に統一する（以前は Foldout）。二次的詳細なので既定は畳む。
                var card = BuildSection("dev.title", "developer", false, out var content, out _);

                var toggle = new Toggle(Tr("diagnostics.label")) { value = EditorL10nPreferences.DiagnosticsEnabled };
                EditorL10nUiKit.AlignField(toggle);
                BindTooltip(toggle, "diagnostics.tooltip");
                EditorL10nUi.RegisterLocaleCallback(toggle, () => toggle.label = Tr("diagnostics.label"));
                toggle.RegisterValueChangedCallback(evt => EditorL10nPreferences.DiagnosticsEnabled = evt.newValue);
                content.Add(toggle);

                // トグルの意味が一目で分かるよう、何を・いつ・どう振る舞うかを永続のノートで添える
                // （tooltip だけだと開発者でも用途が分からない、という指摘への対応）。
                content.Add(EditorL10nUiKit.HintRow(Tr("diagnostics.note")).Also(label => BindLabel(label, "diagnostics.note")));

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

                    // カタログ構成が変わったので前回の検証結果は古い。破棄して要約行を畳む。
                    _lastValidation = null;
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

                    // 検証結果の要約行も保持スナップショットから新しい画面言語で再描画する。
                    UpdateCatalogsResultLine();
                }

                // カタログ節の scope 別グループはどちらの場合も現在の構成・現在の言語で描き直す
                // （ファイル一覧は構成に、文言・tooltip は表示言語に追従させる）。要約表示も更新する。
                RenderCatalogGroups();
                UpdateValidatedAtHint();
                UpdateCatalogsSummaryPills();
                UpdateGlobalSummary();
                UpdateScopeSummary();
                UpdateOverviewBadge();
            }

            private void UpdateOverviewBadge()
            {
                var scopes = EditorL10n.GetScopes();
                if (scopes.Count == 0)
                {
                    EditorL10nUiKit.SetBadge(_overviewBadge, Tr("header.overview.empty"), EditorL10nBadgeKind.Neutral);
                    _overviewBadge.tooltip = "";
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

                // 要約（ヘッダーバッジ）は個々の表示と矛盾させない総合判定にする:
                // 検証でエラーが出ていれば Error、検証警告または「要求ロケールへ fallback 中」の scope が
                // あれば Warning、それ以外は Neutral。文言は構成情報のままなので、色が変わった理由は
                // tooltip で回復可能にする（色だけに頼らない。tooltip の管理元はこのメソッドに一本化）。
                var kind = EditorL10nBadgeKind.Neutral;
                var reasons = new List<string>();
                if (_lastValidation != null && _lastValidation.ErrorCount > 0)
                {
                    kind = EditorL10nBadgeKind.Error;
                    reasons.Add(Tr("catalogs.result.issues", _lastValidation.ErrorCount, _lastValidation.WarningCount));
                }
                else if (_lastValidation != null && _lastValidation.WarningCount > 0)
                {
                    kind = EditorL10nBadgeKind.Warning;
                    reasons.Add(Tr("catalogs.result.warnings", _lastValidation.WarningCount));
                }
                if (hasIssue)
                {
                    if (kind == EditorL10nBadgeKind.Neutral)
                        kind = EditorL10nBadgeKind.Warning;
                    reasons.Add(Tr("header.overview.fallback"));
                }

                EditorL10nUiKit.SetBadge(
                    _overviewBadge,
                    Tr("header.overview", scopes.Count, locales.Count),
                    kind);
                _overviewBadge.tooltip = string.Join("\n", reasons);
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

        /// <summary>
        /// scope 1 件ぶんの設定カード。状態（pill/meta）は常時表示、言語ドロップダウンは折りたたみ。
        /// データ資産（manifest / テーブル）への導線はカタログ節が受け持ち、このカードは表示設定に徹する。
        /// </summary>
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

                var chevron = EditorL10nUiKit.Chevron();

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
