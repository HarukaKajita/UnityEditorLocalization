# Changelog

## [Unreleased]

### Changed
- 表示ロケールの解決順に**システム言語（OS）フォールバック**を追加。グローバル設定が未設定のとき、OS の優先言語（Unity の `Application.systemLanguage` を主に、地域は `CultureInfo` で補完。macOS でも信頼可）を推定して表示に使い、対応する翻訳が無ければ既存の fallback 連鎖が各 scope の `defaultLocale` へ落とす。解決順は `scope 個別設定 → グローバル設定 → システム言語 → defaultLocale`。検出のみ OS API（enum）を言語タグ表へ対応付け、カタログ/解決は文字列タグのみで扱う方針（言語追加で解決ロジックの C# は不変）を維持する。
- 表示ロケールの解決順を `EditorL10n.GetActiveLocale(scope, out source)` に集約し、由来（`EditorL10nLocaleSource`）を単一情報源化。Preferences の scope メタ表示はこの source を使い、System を含む由来を正しく表示する。
- Preferences（`Preferences > UnityEditorLocalization`）を IMGUI から UI Toolkit へ全面再設計。2 ゾーンヘッダー（左=タイトル＋概況バッジ／右=オンラインドキュメントを開くボタン）、グループ再編（表示言語 → scope 個別設定 → 開発者向けの段階的開示）、scope ごとの解決状態（override/fallback）のバッジ可視化、manifest の選択+ping、両スキン・キーボード操作・長い識別子の折り返し・狭い描画領域でのスクロール表示に対応。
- Preferences 画面自身の文言を、パッケージ自身の翻訳カタログ（scope=`com.kajitaharuka.unity-editor-localization`、`Editor/Localization/` の en/ja）から引くドッグフーディング構成へ変更。表示言語の変更に画面自身が追従し、国際的なアクセシビリティを確保する。
- `package.json` の `documentationUrl`/`changelogUrl`/`licensesUrl` を `https://kajitaharuka.com/products/unity-editor-localization/` 系へ統一（製品ページ URL と一致）。

### Added
- Preferences の scope 個別設定で、各 scope の解決順の下に、その scope が対応する言語コードの一覧（`en · ja · zh-Hans · …`）を表示。
- Preferences 画面自身の翻訳を **19 言語**へ拡張。既存 en（defaultLocale）/ja に加え、zh-Hans・zh-Hant・ko・fr・de・it・es-ES・es-419・pt-BR・pt-PT・ru・pl・tr・th・vi・uk・id の 17 言語（各 67 キー）を `Editor/Localization/Locales/` に追加し、manifest へ登録。固定語（scope/locale/manifest/EditorPrefs/Console/CLI/defaultLocale/パス/Claude Code/placeholder/記号）は保持。キー過不足・placeholder・未翻訳疑いの機械検証をクリア（カタログ検証は Errors 0、新規 17 言語の警告 0）。CJK・タイ語など一部は最終的なネイティブレビュー推奨。
- `Tools > UnityEditorLocalization > Settings` メニューを追加。Preferences を開いて UnityEditorLocalization の項目を選択状態にする（`SettingsService.OpenUserPreferences`）。
- 同梱スキル（翻訳ワークフロー / 既存拡張の多言語化連携）を `.claude/skills` と `.agents/skills` へ symlink 登録する `EditorL10nSkillInstaller` を追加。登録先はユーザー（ホーム）/ プロジェクト（リポジトリ直下）から選べる。`Tools > UnityEditorLocalization > AI Agent Skills` のメニューと、`Preferences > UnityEditorLocalization` の「AIエージェント連携スキル」節（登録ボタン＋CLI コマンドの明示・コピー）から実行できる。skills 実体パスは `PackageInfo.resolvedPath` で解決し、埋め込み/PackageCache のどちらでも動作する。登録は macOS/Linux=`ln`、Windows=`mklink /D`（権限が無ければ junction にフォールバック）でクロスプラットフォーム対応し、表示・コピーする CLI コマンドも OS に合わせて出力する。
- `EditorL10n.GetSystemLocale()` と `Get`/`SetSystemLocaleFallbackEnabled()` を公開 API として追加。システム言語タグの供給元は差し替え可能な `SystemLocaleProvider`（テスト用シーム）とし、EditMode テストで解決順と由来を検証する。フォールバックの有効/無効は `EditorPrefs` に保存する（既定は有効）。
- Preferences の表示言語セクションに、検出したシステム言語の表示・システム言語フォールバックの有効/無効トグル・未設定時の解決先を示す動的ヒントを追加。
- Preferences の各 scope カードに、実際に効いている fallback 連鎖（要求 → 親 → defaultLocale）をチップ列で可視化し、実際に翻訳が当たった段を色＋太字で強調する表示を追加。
- Preferences にカタログの Reload / Validate ボタンと、両操作の意味を確認できる説明トグル（ⓘ で HelpBox を開閉、ホバー tooltip でも要約）を追加。検証結果をインラインで表示する。
- エディタ UI 再利用部品 `Editor/UI/EditorL10nUiKit.cs`（2 ゾーンヘッダー・状態バッジ・ドキュメントボタン・セクションカード等）と、両スキンのデザイントークン `Editor/UI/EditorDesignTokens.uss`（Unity 内部 `--unity-*` 変数に非依存）を追加。
- オンラインドキュメント URL を集約する `Editor/UI/EditorL10nDocs.cs` を追加。
- `EditorL10nValidator.ValidateAndLog()` を追加し、メニューと Preferences の検証ボタンの共通入口にした。

## [1.0.0] - 2026-06-26

### Added
- 任意依存（optional）として UnityEditorLocalization を組み込むための規約ドキュメント `Documentation~/OPTIONAL_INTEGRATION.md` と、雛形生成スキル `skills/editor-localization-optional-integration/`（テンプレート同梱）を追加。本体 assembly が基盤を参照せず、基盤が無ければ defaultLocale の単一言語で動作し、基盤導入時に多言語化と言語切替 UI が点灯する 2 アセンブリ方式（Version Define + Define Constraint）を提供する。リファレンス実装は ExportPackageExtension。
- Validator で placeholder 番号の連番欠落と、defaultLocale と同値の未翻訳疑いを警告する検査を追加。
- 未知 scope / 未解決 key を開発時だけ警告できる診断フラグと Preferences のトグルを追加。
- l10n manifest と翻訳テーブル JSON の import / delete / move を検知してカタログを自動リロードする AssetPostprocessor を追加。
- Preferences の scope 個別設定に検索、foldout、defaultLocale と manifest パスの補足表示を追加。
- scope の defaultLocale と manifest パスを取得できる `EditorL10n.TryGetScopeInfo` API を追加。
- UnityEditorLocalization の使い方を自己説明する `Localized Editor Window` UPM sample を追加。

### Changed
- 配布名を `UnityEditorLocalization` に統一。package 名を `com.kajitaharuka.editor-localization` から `com.kajitaharuka.unity-editor-localization` へ、`package.json` の `displayName` を `Editor Localization` から `UnityEditorLocalization` へ変更し、Preferences / メニューのラベル・パスも `Preferences > UnityEditorLocalization` / `Tools > UnityEditorLocalization > …` へ統一。embedded package のディレクトリも `Packages/com.kajitaharuka.unity-editor-localization/` へ改名。namespace（`Kajitaharuka.EditorLocalization`）と公開 API は不変。VCC/ALCOM 表示・利用側ドキュメント・連携パッケージの version define もこの名へ揃える。
- 同梱サンプル（`Localized Editor Window`）の表示文言・メニュー導線・翻訳 scope（`com.kajitaharuka.unity-editor-localization.samples.localized-window`）も新名へ更新。
- パッケージ実体を `Packages/com.kajitaharuka.unity-editor-localization/` 配下のEmbedded UPM package構成へ移行。
- 開発者向け補助資料を `Documentation~/` 配下へ整理し、`package.json` に検索用キーワードとリポジトリ情報を追加。
- `package.json` に MIT ライセンスの metadata を明示。
- host project の `Packages/manifest.json` に `testables` を追加し、package 配下のEditModeテストを実行できる導線を維持。
- `EditorL10n.Tr` / `TryTranslate` の fallback chain を `(locale, defaultLocale)` 単位でキャッシュし、繰り返し呼び出し時の chain 構築アロケーションを削減。

### Fixed
- `EditorL10nUi.BindPropertyField` で、配列/リスト（Foldout で描画される PropertyField）のラベルが言語切替に追従せず生成時の言語のまま固定されていた問題を修正。Foldout のタイトルは `BaseField` のラベル（`labelUssClassName`）ではないため、Foldout 時は `Foldout.text` を更新する経路を追加した。
- UI Toolkit bind helper の `LocaleChanged` 購読を panel attach 中だけに限定し、attach時の再適用、未追加要素の購読リーク、attach/detach 時の多重購読を改善。

## [0.1.0] - 2026-06-22

### Added
- Unity Editor拡張向けの軽量ローカライズ基盤を追加。
- manifestとJSON tableによるscope別カタログ読み込みを追加。
- 文字列タグによるロケール管理とfallbackを追加。
- UI Toolkit向けのbind helperと言語選択Dropdownを追加。
- Inspectorヘッダーやツールバーに置ける汎用のコンパクト言語選択メニューを追加。
- 全 scope 共通のグローバル言語設定 API と Preferences の設定導線、scope 個別設定の解除導線を追加。
- 翻訳key欠落とplaceholder不一致の検証メニューを追加。
- 利用者向けREADME、開発者ガイド、UI Toolkit多言語化Tipsを追加。
