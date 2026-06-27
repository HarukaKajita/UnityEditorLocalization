#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Preferences/UnityEditorLocalization", SettingsScope.User)
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
            private string _searchText = "";
            private string[] _builtScopes = Array.Empty<string>();
            private readonly List<ScopeCard> _cards = new();

            public void Build(VisualElement root)
            {
                root.Clear();
                EditorL10nUiKit.ApplyTheme(root);

                var header = BuildHeader();
                root.Add(header);
                root.Add(EditorL10nUiKit.Note(Tr("note")).Also(label => BindLabel(label, "note")));
                root.Add(BuildCatalogs());
                root.Add(BuildGlobalSection());
                root.Add(BuildScopeSection());
                root.Add(BuildDeveloperSection());

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

            // ===== カタログ操作（Reload / Validate / 説明）=====
            private VisualElement BuildCatalogs()
            {
                var container = new VisualElement();

                var row = new VisualElement();
                row.AddToClassList("l10n-catalogs");

                var result = new Label { name = "l10n-catalogs-result" };
                result.AddToClassList("l10n-catalogs__result");
                result.style.display = DisplayStyle.None;

                var reload = EditorL10nUiKit.ActionButton(Tr("catalogs.reload"), () =>
                {
                    EditorL10n.Reload();
                    SetResult(result, Tr("catalogs.reloaded"), EditorL10nBadgeKind.Neutral);
                }, Tr("catalogs.reload.tooltip"));
                BindButtonText(reload, "catalogs.reload", "catalogs.reload.tooltip");

                var validate = EditorL10nUiKit.ActionButton(Tr("catalogs.validate"), () =>
                {
                    var validation = EditorL10nValidator.ValidateAndLog();
                    if (validation.IsValid)
                        SetResult(result, Tr("catalogs.result.ok", validation.Warnings.Count), EditorL10nBadgeKind.Ok);
                    else
                        SetResult(result, Tr("catalogs.result.issues", validation.Errors.Count, validation.Warnings.Count), EditorL10nBadgeKind.Warning);
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

                container.Add(row);
                container.Add(help);
                return container;
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

                void Apply()
                {
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
                }

                dropdown.RegisterValueChangedCallback(_ =>
                {
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

                var search = new TextField(Tr("scope.search.label"));
                EditorL10nUiKit.AlignField(search);
                BindTooltip(search, "scope.search.tooltip");
                EditorL10nUi.RegisterLocaleCallback(search, () => search.label = Tr("scope.search.label"));
                search.RegisterValueChangedCallback(evt =>
                {
                    _searchText = evt.newValue ?? "";
                    ApplyFilter();
                });
                content.Add(search);

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
            private void ApplyFilter()
            {
                var total = _cards.Count;
                var shown = 0;
                foreach (var card in _cards)
                {
                    var match = ScopeMatchesSearch(card.Scope, _searchText);
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
                }
                else
                {
                    // 言語のみの変更。編集中のドロップダウンを壊さないよう、派生表示だけ in-place 更新。
                    foreach (var card in _cards)
                        card.UpdateState();
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

                void Apply()
                {
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
                }

                dropdown.RegisterValueChangedCallback(_ =>
                {
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
