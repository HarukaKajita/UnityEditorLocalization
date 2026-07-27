# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## このリポジトリの位置づけ

このリポジトリは Unity プロジェクトの体裁を取っていますが、開発対象は単一の Unity package
`com.kajitaharuka.unity-editor-localization`（表示名: UnityEditorLocalization）です。実体はすべて
[Packages/com.kajitaharuka.unity-editor-localization/](Packages/com.kajitaharuka.unity-editor-localization/) 配下にあります。
リポジトリ直下の `Assets/`、`ProjectSettings/`、`Packages/manifest.json`、`Packages/packages-lock.json`、
`Packages/` のうち package 実体以外の要素、各 `*.csproj` / `*.sln` は、この package を
Unity Editor 上で開発・検証するための器に過ぎません。実装・ドキュメントの編集は基本的に
`Packages/com.kajitaharuka.unity-editor-localization/` 内で行います。

- Unity バージョン: 2022.3（`Packages/com.kajitaharuka.unity-editor-localization/package.json` の `unity` を参照）
- 対象: **Editor 専用**。ランタイム、Addressables、Unity Localization package には依存しません。
- package 本体の asmdef は `Kajitaharuka.EditorLocalization`（`includePlatforms: ["Editor"]`、`references: []`）です。
  EditMode テストは `Kajitaharuka.EditorLocalization.Tests` asmdef で分離します。

## ゴールド標準と開発フロー

このリポジトリは、kajitaharuka 名義で開発・販売する Unity パッケージ／アセット共通の**ゴールド標準**に準拠します。
標準の**正本**はテンプレートリポジトリ `UnityTemplate_2022_3_22f1` の `docs/GOLD_STANDARD.md` です
（リポジトリ構成・ガイド文書・コード標準・多言語・リリース資材・URL 規約・エージェント運用の各標準を定義）。
標準の変更は必ずテンプレートリポジトリ側で行い、本リポジトリへ反映します。

> **このリポジトリは公開リポジトリのため、標準の本体は配布されません。** ゴールド標準には価格方針・
> 未発売製品名・出品戦略といった公開前提でない内容が含まれるためです。同じ理由で `docs/REPOSITORY_MAP.md`
> も公開向けに縮約されており、非公開リポジトリの行と URL・ローカルパスは載りません。
> **このリポジトリで開発するときは、標準の正本（テンプレートリポジトリ）と運用の正本（サイトリポジトリ）も
> 併せてセッションから読める状態にしてください。** ここにあるファイルだけでは全体像が分かりません。

本リポジトリ固有の逸脱は次のとおりです。

- 本パッケージは MIT ライセンスで公開する OSS のため、`package.json` に `repository` を持ち、購入者向け文書からの
  リポジトリ言及も正当です（標準 §2.2 の「公開 OSS は例外」に該当。有料販売のみのパッケージでは `repository` を入れない）。
- **UAS 適格性: 対象（ただし出品は暫定見送り）**（GOLD_STANDARD §7.1）。MIT ライセンスで技術的な抵触は無く、UAS へ
  出品可能ですが、無料公開の本パッケージは UAS 出品のメリットが現時点では大きくないため暫定的に見送ります
  （2026-07-22 開発者判断。`publish.json` の `targets.unity-asset-store.enabled: false` で宣言）。出品したほうが
  よい状況になった時点で再判断します。

### 開発→リリース→商品化フローと使用スキル

各フェーズでは該当スキルを必ず参照します。スキルに無い判断が必要になったら作業を止めず最良判断で進め、
**暫定判断として最終報告で強調**します。

| フェーズ | 内容 | 使用スキル |
|---|---|---|
| 開発 | 実装・Inspector 設計・多言語化・カタログ整備 | `unity-editor-ui-design` / `editor-localization-optional-integration` / `editor-localization-translation-quality` / `unity-mcp-skill` / `unity-cli` |
| 検証 | Test Runner・手動チェック・スクリーンショット | `editor-window-capture` |
| リリース | version 確定 → CHANGELOG 畳み込み → `Publish/` 書き出し → コミット/タグ → publish.json 追従 | `release-unity-package` |
| 商品化 | promo 画像 → meta/pages（ja/en → 19 言語）→ publish.json → 出品下書き | `new-product-onboarding`（`write-my-promo-images` / `write-my-product-page` / `publish-to-platform` を束ねる） |
| 出品・公開 | フォーム入力・添付まで自動、**公開ボタンは人間** | `publish-to-platform` |

出品の保存・公開・削除の確定操作はエージェントが行いません（入力・添付・下書きまで）。

### 同梱スキルとミラーの運用（正本と生成物の分離）

- AI エージェント向けスキルの**正本**は package 同梱の
  [`Packages/com.kajitaharuka.unity-editor-localization/skills/`](Packages/com.kajitaharuka.unity-editor-localization/skills/) です。
- リポジトリ直下の `.claude/skills` / `.agents/skills` は `scripts/sync-agent-skills.mjs` が正本から生成する
  **実体コピーのミラー**で、Git 追跡します（標準 §2.6-4 / §2.9）。**ミラーを直接編集しないこと。**
  編集は必ず正本側で行い、`node scripts/sync-agent-skills.mjs` で再生成します（`--check` で drift 検査）。
- エディタ拡張の**利用者**がスキルをローカル登録する導線は別途 `EditorL10nSkillInstaller` が symlink 方式で
  提供します（上記ミラーとは別機構）。メニュー `Tools > UnityEditorLocalization > AI Agent Skills` は間接方式
  （標準 §2.6）で Preferences ペインを開くだけで、登録自体はペイン内のボタンから行います。

### 改善提案の義務

作業の中で、スキル化したほうがよい反復工程、既存スキルの一般に通用する改善点、ゴールド標準自体の改善点を
発見したら、作業完了報告に「提案」としてまとめて積極的に共有してください。
標準の変更は、テンプレートリポジトリの `docs/GOLD_STANDARD.md`（このリポジトリには配布されません）へ
反映してから各リポジトリへ展開します。

## パイプライン整合性の 3 層（配布物・検査・契約）

このリポジトリには、テンプレートリポジトリ `UnityTemplate_2022_3_22f1` から**配布された生成物**があります（GOLD_STANDARD §2.10）。**配布物は編集しないでください。** 変更が必要なときはテンプレートリポジトリ側の正本を直して再配布します（配布物の先頭には `source-sha256` の生成物ヘッダがあり、書き換えると検査 3 が落ちます）。

配布プロファイルは **`public`**（公開リポジトリ向け）です。標準本体は配られず、検査・契約生成・CI・縮約版の地図だけが入ります。

| ファイル | 位置づけ |
|---|---|
| `docs/REPOSITORY_MAP.md` | パイプラインのリポジトリ地図（**公開向けに縮約**。正本は運用リポジトリ側） |
| `scripts/pipeline/verify_repo_guide.py` | 標準準拠検査（第 2 層） |
| `scripts/pipeline/emit_release_manifest.py` | リリース契約ファイルの生成（第 3 層） |
| `pipeline/repo.json` | **このリポジトリの手書き宣言**（配布物ではない） |

```bash
python3 scripts/pipeline/verify_repo_guide.py       # 標準準拠検査。error があれば非ゼロ終了
python3 scripts/pipeline/emit_release_manifest.py   # リリース後に契約ファイルを 2 箇所へ書く
```

- 検査はリリース工程で**省略不可**（`release-unity-package` の検証ゲート 1.5）。日常の実行は任意です。
- ヒューリスティックな検査（文書内のパス参照・テスト整備の記述・スキル名）の誤検出は、`pipeline/repo.json` の `waivers` へ**理由を添えて**登録します。検査そのものを消さないでください。
- `pipeline/repo.json` の `saleUnit.exporterAssets` は販売単位の成果物を作る Exporter 設定アセットの宣言です。Exporter を増減したら合わせて更新します。

## よく使う操作

このリポジトリには CLI ベースのビルド/テスト基盤はなく、検証は Unity Editor のメニューから行います。

- カタログ作成: `Tools > UnityEditorLocalization > Create Catalog`（ウィザード。scope・出力フォルダ・defaultLocale・対象ロケールから manifest と空テーブルの雛形を正準フォーマットで生成。画面は全体スクロール・scope の例示ヒント・成功のインライン結果表示〈Ok 色・言語追従〉・ドキュメントボタン付き。Preferences のカタログ節からも開ける。[Editor/Authoring/EditorL10nCatalogWizard.cs](Packages/com.kajitaharuka.unity-editor-localization/Editor/Authoring/EditorL10nCatalogWizard.cs) / 書き出しは [Editor/Core/EditorL10nCatalogWriter.cs](Packages/com.kajitaharuka.unity-editor-localization/Editor/Core/EditorL10nCatalogWriter.cs)）
- カタログ再読み込み: `Tools > UnityEditorLocalization > Reload Catalogs`
- カタログ検証: `Tools > UnityEditorLocalization > Validate Catalogs`
  - defaultLocale テーブルの存在、各ロケールでの key 過不足、`string.Format` placeholder 番号の一致と連番欠落、defaultLocale と同値の未翻訳疑いを検査します（manifest の `fixedTerms` で宣言した固定語キーは同値疑いから除外）。
  - CI からは batchmode で `EditorL10nValidator.ValidateForCI()`（`-executeMethod`）を使います。エラーで非 0 終了し CI を止める。既定はエラーのみ、`-l10nFailOnWarnings` で警告も失敗。対話モードでは終了しません。終了コード判定は純関数 `ComputeExitCode` に分離。
- 表示言語の確認/変更（グローバルおよび scope ごと）: `Preferences > UnityEditorLocalization`（`Tools > UnityEditorLocalization > Settings` から開いて選択状態にできる。`SettingsService.OpenUserPreferences`）
- 同梱スキル（翻訳ワークフロー / 既存拡張の多言語化連携）の登録: メニュー `Tools > UnityEditorLocalization > AI Agent Skills` は間接方式（標準 §2.6）で Preferences の「AIエージェント連携スキル」節（`Preferences > UnityEditorLocalization`）を開くだけ。user / project スコープの登録・CLI コマンドの明示/コピーはこのペイン内のボタンから行う（メニューからは登録もクリップボード書き込みも実行しない。意図しないスキル追加を避けるため、登録はペインで内容を理解したうえで明示的に押した場合のみ）。ペインには同梱スキルごとに「名前・要約・プロンプト例・正本フォルダへのクリック導線（Project ビューで選択＋Ping）」を登録操作より上に一覧表示する（標準 §2.6-1。2026-07-23 追加）。`.claude/skills` と `.agents/skills` へ symlink を張る（[Editor/Skills/EditorL10nSkillInstaller.cs](Packages/com.kajitaharuka.unity-editor-localization/Editor/Skills/EditorL10nSkillInstaller.cs)）。mac/Linux=`ln`、Windows=`mklink /D`（不可なら junction）でクロスプラットフォーム動作し、表示する CLI コマンドも OS 別。この installer はエディタ拡張の**利用者**がローカルへスキルを登録するための機能。**このリポジトリ自身**が追跡する `.claude/skills`・`.agents/skills` は、それとは別に `scripts/sync-agent-skills.mjs` が package 同梱 `skills/` を正本として生成する実体コピーのミラー（symlink ではない）で、直接編集しない（後述「ゴールド標準と開発フロー」を参照）。
- 翻訳テキスト品質の静的検証（Python、利用側の locale 群に対して実行）:

  ```bash
  python3 Packages/com.kajitaharuka.unity-editor-localization/skills/editor-localization-translation-quality/scripts/validate_locale_quality.py \
    <locales_dir> --default-locale <tag>
  ```

`Packages/com.kajitaharuka.unity-editor-localization/Tests/Editor/` には EditMode テスト用の
`Kajitaharuka.EditorLocalization.Tests` asmdef があります。`EditorL10n` の fallback 連鎖や
`NormalizeLocaleTag` など `internal` ロジックは `InternalsVisibleTo` 経由で検証します。
package 配下のテストをこの host Unity project で実行できるように、`Packages/manifest.json` の
`testables` には `com.kajitaharuka.unity-editor-localization` を含めます。

## アーキテクチャ

文言は「scope（名前空間）× ロケールタグ × key」で引きます。中核の流れは次の通りです。

1. **カタログ探索とロード** — [Editor/Core/EditorL10nCatalog.cs](Packages/com.kajitaharuka.unity-editor-localization/Editor/Core/EditorL10nCatalog.cs)
   - `AssetDatabase.FindAssets("l10n-manifest")` でプロジェクト内の `*.l10n-manifest.json` を全探索します。
     利用側拡張は manifest を置くだけで自動的にカタログへ登録され、この package の C# 変更は不要です。
   - manifest（`scope` / `defaultLocale` / 任意の `fixedTerms[]` / `locales[]`）を読み、各 locale の `tablePath`（manifest からの相対パス）の
     JSON テーブルをロードして、scope 単位の `EditorL10nScopeCatalog`（`locale -> (key -> value)` の辞書）を構築します。
     `fixedTerms` は全ロケールで `defaultLocale` と同値が正当な固定語キー（ファイル名・型名など）で、検証の未翻訳疑いから除外されます。
   - scope 重複、テーブルの `locale` と manifest の `tag` 不一致は warning を出して握りつぶします（実装堅牢性のため例外にしない方針）。

2. **公開 API とロケール解決** — [Editor/Core/EditorL10n.cs](Packages/com.kajitaharuka.unity-editor-localization/Editor/Core/EditorL10n.cs)
   - `EditorL10n.Tr(scope, key, args...)` が主入口。`TryTranslate` → `string.Format` の順で処理し、未解決時は key 自体を返します（落とさない）。
   - 表示ロケールは scope 個別設定 → グローバル設定 → システム言語（OS、無効化可）→ scope の defaultLocale の順で解決します。
   - **fallback 連鎖**: 解決済み表示ロケール → その親ロケール群 → defaultLocale → 親群、の順で最初に見つかった値を返します
     （例: `es-419` → `es` → `ja`）。親ロケールは `-` 区切りを末尾から削って導出します。
   - ロケールタグは `NormalizeLocaleTag` で正規化（言語小文字 / 地域大文字 / script は先頭大文字、`_`→`-`）します。
     ロケールは **enum ではなく文字列タグ** で扱う、というのが本 package の根幹方針です。新ロケール追加で C# を触らせないためです。
   - `GetGlobalLocale` / `SetGlobalLocale` で全 scope 共通の表示ロケールを取得・設定できます。
   - **システム言語フォールバック**: グローバル設定が未設定のとき、OS の優先言語を `GetSystemLocale` で推定して表示ロケールに使い、対応する翻訳が無ければ fallback 連鎖が defaultLocale へ落とします。検出は Unity の `Application.systemLanguage`（OS の優先言語を確実に取得。macOS でも信頼可）を主に、地域を `CultureInfo` で補い、未対応言語は `CultureInfo`→空へ degrade します。**enum 利用はこの検出（既定の推定）に限り**、カタログ/解決はあくまで文字列タグのみで扱う（言語追加で解決ロジックの C# は不変／`SystemLanguageToTag` 表に無い言語もカタログ追加だけで利用可。必要なら 1 行追加で OS 自動検出に対応）。`Get`/`SetSystemLocaleFallbackEnabled` で有効/無効を切り替えられます（既定は有効）。タグ供給元は差し替え可能な `SystemLocaleProvider`（テスト用シーム）です。解決順は `GetActiveLocale(scope, out source)` に集約し、由来 `EditorL10nLocaleSource` を UI 表示へ流用します。
   - `LocaleChanged` イベントで UI 層がロケール変更に追従します。

3. **表示言語の永続化** — [Editor/Core/EditorL10nPreferences.cs](Packages/com.kajitaharuka.unity-editor-localization/Editor/Core/EditorL10nPreferences.cs)
   - 選択言語は **`EditorPrefs`（ユーザーごと）** に保存し、プロジェクト資産には書き込みません。チーム内で言語の好みが衝突しないようにする設計です。
   - 解決優先度: scope 個別設定 → グローバル設定 → システム言語（OS、無効化可）→ scope の defaultLocale。システム言語フォールバックの有効/無効も `EditorPrefs` に保存します（既定は有効＝無効化時のみ保存）。
   - scope 個別設定は空文字で削除し、Preferences では「グローバル設定に従う」で解除します。

4. **UI Toolkit バインド層** — [Editor/UI/EditorL10nUi.cs](Packages/com.kajitaharuka.unity-editor-localization/Editor/UI/EditorL10nUi.cs)
   - `Label` / `Button` / `PropertyField` などを翻訳 key にバインドし、`LocaleChanged` を購読して言語変更に自動追従させます。
     購読解除は `DetachFromPanelEvent` で行う前提です。
   - 言語選択 UI は 2 種類: ヘッダー/ツールバー常設用の `CreateLocalizedCompactLocaleMenu`（`A/文 日本語 ▾` 風）と、
     設定フォーム行用の `CreateLocalizedLocaleDropdown`。候補は manifest から動的に読むため、言語追加で UI 側 C# の変更は不要です。
     ドロップダウンは choices 再代入の巻き戻し発火を applying ガード（try/finally）で防ぎ、選択中ロケールが
     カタログ外のときは「登録済みカタログ外」を表示します。コンパクトメニューはカタログ未登録で無効のとき
     理由を tooltip（`menu.noCatalog.tooltip`）で示します。部品自身の文言はパッケージ同梱カタログ
     （`EditorL10nPackage.Name` の scope）から引きます。
   - 任意の「Powered by UnityEditorLocalization」クレジットは `EditorL10nUi.CreateAttribution()`（クリックで製品ページ）で置けます。コンパクトメニューは `showAttribution`（既定 true）で開いたメニュー末尾に控えめ表示。ライセンスは MIT のままで表示は任意・歓迎（文言はブランド固定で言語非依存）。
   - エディタ UI の構造（2 ゾーンヘッダー・状態バッジ・ドキュメントボタン・セクションカード・折りたたみセクション `CollapsibleSection`〈チェブロン＋見出し＋要約スロット、開閉状態を EditorPrefs へ永続化〉等）は再利用部品
     [Editor/UI/EditorL10nUiKit.cs](Packages/com.kajitaharuka.unity-editor-localization/Editor/UI/EditorL10nUiKit.cs) と
     両スキンのデザイントークン [Editor/UI/EditorDesignTokens.uss](Packages/com.kajitaharuka.unity-editor-localization/Editor/UI/EditorDesignTokens.uss) を合成して組みます。
     色はスキン別トークン、間隔・フォント（11/12/13 の 3 段）・角丸はスキン非依存トークン（`--eui-space-*`/`--eui-font-*`/`--eui-radius-*`）で少数ステップに固定します。
     トークンは Unity 内部 `--unity-*` 変数に依存せず、`eui-dark`/`eui-light` の 2 系統で両スキンを確実に扱います（USS は名前で解決し、見つからなくても劣化動作）。

5. **検証** — [Editor/Validation/EditorL10nValidator.cs](Packages/com.kajitaharuka.unity-editor-localization/Editor/Validation/EditorL10nValidator.cs)（`ValidateAndLog` がメニューと Preferences の検証ボタンの共通入口。結果は `EditorL10nValidationIssue`（`Severity`/`Scope`/`Locale`/`Message`）と `EditorL10nValidationResult.Issues` で構造化し、UI が由来 scope ごとに分類できる。`Errors`/`Warnings` の平坦文字列（`{scope}/{locale}: 詳細`）と Console 出力は不変で後方互換）、
   **Preferences UI** — [Editor/Settings/EditorL10nSettingsProvider.cs](Packages/com.kajitaharuka.unity-editor-localization/Editor/Settings/EditorL10nSettingsProvider.cs)（UI Toolkit 製。2 ゾーンヘッダー（概況バッジは検証結果も加味した総合判定: エラー=Error/警告・fallback 中=Warning。色が変わった理由は tooltip で提示）＋ドキュメントボタン（[Editor/UI/EditorL10nDocs.cs](Packages/com.kajitaharuka.unity-editor-localization/Editor/UI/EditorL10nDocs.cs) の URL を `OpenURL`）の下に、**折りたたみ可能な 5 大項目**（`CollapsibleSection` で開閉を EditorPrefs に永続化・畳んだ見出しに要約ピル）: 表示言語（保存先と解決順のノート・検出システム言語の表示・システム言語フォールバックのトグル・未設定時の解決先ヒント。要約=実効言語と由来）→scope 個別設定（scope 検索〈この節のみに適用。別大項目の表示を暗黙に変えない方針〉・各 scope で実際に効く fallback 連鎖をチップ列で可視化し使用段を強調・対応言語コードの一覧。要約=scope 数と個別設定数。manifest への導線はカタログ側へ移設し表示設定に専念）→**カタログ**（操作列は主操作順に 検証→再読み込み→作成ウィザード導線、空状態にも作成案内。検証要約は内容と色を一致させ〈警告のみは警告色〉実行時刻を明示。**scope ごとのグループにファイル一覧と検証結果を統合**＝manifest 行と locale テーブル行〈既定バッジ・key 数ピル・宣言済みで実体が無いテーブルの欠落マーカー ×・クリックで選択+Ping〉、件数ピル・問題なし ✓・深刻度を色＋形 `×`/`!` で示すマーカー・由来 locale チップ・行クリックで由来アセットへジャンプ・不足キーは行末の「+」でその場追加するクイックfix・1 深刻度 30 行で打ち切り Console へ誘導。グループ開閉はユーザー操作を記憶し既定はエラー scope のみ展開。要約=最終検証の件数ピル）→AIエージェント連携スキル（同梱スキルの登録・登録先ごとの状態ピル〈登録済み/未登録/要再登録。`EditorL10nSkillInstaller.Get*InstallState`〉・CLI コマンドの明示・コピー)→開発者向け（既定折りたたみ）。**画面自身の文言はパッケージ自身の翻訳カタログ** [Editor/Localization/](Packages/com.kajitaharuka.unity-editor-localization/Editor/Localization/)（scope=package 名、19 言語）から引くドッグフーディング構成で、表示言語の変更に画面自身（検証結果の分類表示を含む）が追従します。検証の診断文も文字列で固定せず**種類（`EditorL10nValidationMessageKind`）＋引数**で持ち、表示時にこのカタログから整形するため UI・Console とも表示言語に追従します。固定語（`defaultLocale`/`key`/`placeholder`/`Console`）と技術トークン（`present=`/`missing=` ほか）・`{0}` 引数は保持）、
   **JSON モデル** — [Editor/Core/EditorL10nJson.cs](Packages/com.kajitaharuka.unity-editor-localization/Editor/Core/EditorL10nJson.cs)（`JsonUtility` 用の `[Serializable]` DTO 群）。

### 利用側データ構造（manifest + テーブル）

```text
Assets/<利用側拡張>/Editor/Localization/
  <name>.l10n-manifest.json   # scope, defaultLocale, fixedTerms[]（任意）, locales[]（tag/nativeName/englishName/tablePath）
  Locales/
    ja.json                   # { "locale": "ja", "entries": [{ "key": ..., "value": ... }] }
    en.json
```

## 編集時の不変条件・方針

- **ロケールを enum やコード分岐で扱わない。** 言語の増減は manifest と JSON テーブルの追加だけで完結させ、`EditorL10n` を含む C# を改変しない設計を崩さないこと。
- **`Tr` は例外で落とさない。** 未解決 key、`string.Format` 失敗時は key やフォーマット前文字列を返す（`Debug.LogError` で通知）。この耐障害性を維持すること。
- **placeholder は番号 `{0}`/`{1}` 形式。** `0` から連続した番号を使い、全ロケールで番号集合を一致させる（順序は言語ごとに変えてよい）。Validator がこれを検査します。
- key は機能領域から始め役割が分かる名前にし、**表示文言や表示順を key に含めない**（[DEVELOPER_GUIDE.md](Packages/com.kajitaharuka.unity-editor-localization/Documentation~/DEVELOPER_GUIDE.md) の命名規約に従う）。
- ソースは全ファイルが `#if UNITY_EDITOR ... #endif` で囲まれています。新規ファイルも同様に囲むこと。

## ドキュメント（変更時は実装との整合を保つ）

実装を変えたら、対応する以下のドキュメントの更新要否を必ず確認してください。

- [Packages/com.kajitaharuka.unity-editor-localization/README.md](Packages/com.kajitaharuka.unity-editor-localization/README.md): 利用者向け概要・最小構成・API・fallback・検証
- [Packages/com.kajitaharuka.unity-editor-localization/Documentation~/DEVELOPER_GUIDE.md](Packages/com.kajitaharuka.unity-editor-localization/Documentation~/DEVELOPER_GUIDE.md): 利用側拡張での scope/key 設計指針・レビュー観点
- [Packages/com.kajitaharuka.unity-editor-localization/Documentation~/UI_TOOLKIT_LOCALIZATION_TIPS.md](Packages/com.kajitaharuka.unity-editor-localization/Documentation~/UI_TOOLKIT_LOCALIZATION_TIPS.md): 言語変更に追従させる UI Toolkit 実装 Tips
- [Packages/com.kajitaharuka.unity-editor-localization/Documentation~/OPTIONAL_INTEGRATION.md](Packages/com.kajitaharuka.unity-editor-localization/Documentation~/OPTIONAL_INTEGRATION.md): UnityEditorLocalization を **任意依存（optional）** として組み込むための 2 アセンブリ方式・命名規約・version define の注意・チェックリスト（リファレンス実装: ExportPackageExtension）
- [Packages/com.kajitaharuka.unity-editor-localization/CHANGELOG.md](Packages/com.kajitaharuka.unity-editor-localization/CHANGELOG.md): リリースごとの変更（`package.json` の version と整合）
- [Packages/com.kajitaharuka.unity-editor-localization/skills/editor-localization-translation-quality/](Packages/com.kajitaharuka.unity-editor-localization/skills/editor-localization-translation-quality/): 翻訳ワークフロー（用語・スタイル・言語別注意・品質検証スクリプト）
- [Packages/com.kajitaharuka.unity-editor-localization/skills/editor-localization-optional-integration/](Packages/com.kajitaharuka.unity-editor-localization/skills/editor-localization-optional-integration/): 既存拡張の多言語化連携スキル（本体ブリッジ seam ＋連携 assembly を scaffold。テンプレート同梱。`OPTIONAL_INTEGRATION.md` と整合）


## 備考
CLAUDE.mdとAGENTS.mdの内容は常に同じ内容になるように保つこと。
