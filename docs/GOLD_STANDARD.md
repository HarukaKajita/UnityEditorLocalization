<!-- 生成物: この内容はテンプレートリポジトリ UnityTemplate_2022_3_22f1 から配布されたコピーです。
     編集はテンプレート側で行い、scripts/distribute_standard.py で再配布してください。
     source: UnityTemplate_2022_3_22f1/docs/GOLD_STANDARD.md
     source-sha256: f15607be160cafce7ecffb7d0b510c6bd92a02fde370b3210ca47cd558bf3f74 -->

# ゴールド標準 — Unityパッケージ開発・販売パイプライン標準 v1.0

最終更新: 2026-07-15 / 正本: このファイル（UnityTemplate_2022_3_22f1 リポジトリ `docs/GOLD_STANDARD.md`）

この文書は、kajitaharuka 名義で開発・販売する Unity パッケージ／アセットの全リポジトリが従う標準の**正本**である。
テンプレートリポジトリから派生した各リポジトリはクローン時にこの文書を継承する。標準の変更は必ずテンプレートリポジトリ側で行い、既存リポジトリへは変更内容に応じて反映する。各リポジトリの `CLAUDE.md` / `AGENTS.md` は、この標準の準拠を前提に書かれる。

> 実証元: ExportPackageExtension（EPE）/ UnityEditorLocalization（UEL）/ UnityEditorWindowCaptureExtension（UEWCE）の 3 リポジトリと MySite（kajitaharuka.com）で確立・実証済みの構成を標準化したもの。2026-07-15 の包括監査で採用決定。

---

## 1. 全体像 — 4層パイプライン

```
[1] 開発リポジトリ（このテンプレートから派生）
      │  Unity 2022.3+ / embedded UPM package / 19言語対応 / Tests
      ▼
[2] リリース資材（各リポジトリ Publish/）
      │  EPE の Exporter で .unitypackage / .tgz / .zip / VPM zip を書き出し
      ▼
[3] 商品情報（MySite の external-content submodule が正本）
      │  meta.json + pages(5種×19言語) + publish.json + promo-kit 画像
      ▼
[4] 出品（Booth / Gumroad / ArtStation、将来 Unity Asset Store）
         入力・添付はエージェントが自動化し、保存/公開の確定操作は人間が行う
```

- 層をまたぐ運用の正本は MySite 側: `docs/publish-json-schema.md`（販売正本スキーマ）、`docs/roadmap-multi-platform-publishing.md`（設計）、`docs/runbook-content-sync.md`（同期手順）。
- 原則は「**入力の自動化、確定の人間化**」。エージェントは出品の公開ボタンを押さない。

## 2. リポジトリ標準

### 2.1 構成 — 「器」と「実体」の分離

- リポジトリは Unity プロジェクトの体裁を取るが、開発対象は `Packages/` 配下の **embedded UPM package**。フォルダ名は package name（`com.kajitaharuka.{kebab-case}`）に一致させる。
- package name はすべて小文字。ドット区切りはリバースドメイン＋サブパッケージ表現にのみ使い、各セグメント内の語区切りは **kebab-case**（例: `com.kajitaharuka.uber-material-property-drawer.generated-texture`）。
- ホスト側 `Assets/` は手動検証の場（Inspector 検証用 ScriptableObject インスタンス、サンプルシーン等）に限定し、パッケージ本体のコード・資産を置かない。
- パッケージルートのフォルダ自体は `.meta` を持たない（UPM 慣例）。内部のファイル・サブフォルダは `.meta` を保持する。
- 複数パッケージ（core＋companion 等）を持つ場合も同様に `Packages/` 直下へ並置する。

### 2.2 ガイド文書 — 必須3点セットと情報分離

**リポジトリ直下（開発者向け）— 必須:**

| ファイル | 役割 |
|---|---|
| `CLAUDE.md` / `AGENTS.md` | AI エージェントと人間開発者への完全なガイド。**両ファイルは常に同一内容**を保つ（片方だけ更新しない）。ファイル名と見出しの矛盾を避けるため、H1 は「`# リポジトリガイド（CLAUDE.md / AGENTS.md 共通）`」のような中立な見出しにする。 |
| `README.md` | リポジトリのランディング。開発者向け導入（Git URL / 開発環境）と、公開商品ページへのリンク。 |

**root README の多言語対応（2026-07-22 制定・順次展開）:** root `README.md` は商品ページと同じ **19 言語**へ展開する。パターン: 正本（英語または日本語）を root に置き、H1 直後に**言語セレクタ行**（各言語の native name でのリンク列）を置く。翻訳は `docs/readme/README.{locale}.md`（locale はカタログと同じタグ表記。例 `README.zh-Hans.md`）に置き、各翻訳ファイルの冒頭に同じセレクタ行＋「正本は root の README」という注記を入れる。翻訳ファイルは配布パッケージには含めない（リポジトリ直下 `docs/` のため自然に除外される）。実証: UEL（2026-07-22）。他リポジトリへは順次展開する。

`CLAUDE.md` / `AGENTS.md` は「エージェントが事前知識ゼロから開発〜リリース直前まで自律作業できる」ことを目標に、最低限つぎを含める:

1. リポジトリの位置づけ（器と実体の分離、package name、Unity バージョン）
2. アーキテクチャとコードから読み取れない設計判断（採用理由・却下した代替案）
3. 開発コマンド・検証手順（Unity メニュー・Test Runner・手動チェック項目）
4. この標準（GOLD_STANDARD）への準拠宣言と、リポジトリ固有の逸脱があればその根拠
5. 開発→リリースのフローと、各工程で使うスキルの名前（§3 の一覧を実態に合わせて）
6. **改善提案の義務**: 「作業の中でスキル化したほうがよい工程、既存スキルの一般に通用する改善点、標準自体の改善点を発見したら、作業報告にまとめて積極的に提案する」旨の指示
7. コーディングスタイル・コミット規約（日本語・1行サマリー）

**情報分離の原則（購入者向け vs 開発者向け）:**

- パッケージ内の `README.md` / `Documentation~/` / `CHANGELOG.md` は**購入者が読む文書**。開発者だけが知るべき情報（プライベートリポジトリの URL、開発フロー、社内向け手順、リポジトリ構成の説明）を含めない。
- 開発者向け情報はリポジトリ直下の `README.md` / `CLAUDE.md` / `AGENTS.md` に置く。
- 例外: UEL のような公開 OSS は package.json の `repository` と購入者向け文書からのリポジトリ言及が正当（入手経路の一つのため）。有料販売のみのパッケージでは package.json に `repository` を**入れない**。

### 2.3 コード標準

- Editor 専用パッケージは asmdef で `includePlatforms: ["Editor"]`。Runtime を持つパッケージは Runtime / Editor を asmdef で分離する。
- 名前空間・asmdef 名は `Kajitaharuka.` プレフィックス（PascalCase）。
- 新規の Inspector / EditorWindow は UI Toolkit（`CreateInspectorGUI` 等）＋共通部品・USS デザイントークンで実装する。`Experimental`・内部 API・Unity 内部 USS 変数には依存しない。既存の IMGUI 資産（MaterialPropertyDrawer 等、IMGUI が前提の拡張点を含む）は無理に移行しない。
- **Unity Editor 内部 API のリフレクション使用は原則避ける**（UAS 出品ガイドライン 2.5.g が禁止）。核心価値のために意図的に使う場合（例: UEWCE の描画バッファ読み取り）は、その製品が **UAS 対象外（オプトアウト）**になることを CLAUDE/AGENTS の準拠節に明記して受け入れる。
- 新規コードは **Domain Reload 無効（Fast Enter Playmode）でも壊れない**設計を前提にする（static 状態のリセット対応。Unity 6.6+ の UAS 提出要件 2.5.h）。
- **ファイルパスは 150 字未満に保つ**（UAS 2.1.e。計測は `.meta` を含み、UPM は `<package-name>/` 起算・.unitypackage は `Assets/<DisplayName>/` 起算・スイートは実出品レイアウトで再計測）。長い asmdef は**ファイル名だけを短縮**してよい（assembly `name` は不変。例: 連携 asmdef は `L10nIntegration.asmdef`。2026-07 に既存6パッケージへ適用済み）。
- EditMode テストを `{Asmdef名}.Tests` asmdef で分離し、ホストの `Packages/manifest.json` の `testables` に package name を登録する。パス処理・文字列処理・検証ロジックなど Unity 非依存に切り出せる部分を優先的にテストする。
- ユーザー向け文言はハードコードせず翻訳カタログ経由（§2.4）。`[Tooltip]` 等の属性文字列は言語切替に追従しないため主表示に使わない。

### 2.4 多言語 — UEL 任意依存統合（19言語）

- UnityEditorLocalization（`com.kajitaharuka.unity-editor-localization`）を**任意依存（optional）**として統合する。本体 asmdef は基盤を参照せず、ブリッジ seam ＋ Version Define / Define Constraint 連携 assembly（共有シンボル `KAJITAHARUKA_EDITOR_L10N`）で分離する。基盤が無ければ defaultLocale（ja）の単一言語で動作する。
- 雛形生成はスキル `editor-localization-optional-integration`、翻訳の追加・検証はスキル `editor-localization-translation-quality` を使う。
- 対応ロケールは 19 言語（ja, en, zh-hans, zh-hant, ko, fr, de, it, es-es, es-419, pt-br, pt-pt, ru, pl, tr, th, vi, uk, id）。MySite の `SUPPORTED_LANGS` と一致させる。
- 本体 `package.json` の `dependencies` に基盤を入れない。開発時はホストの `Packages/manifest.json` でローカル参照または Git 参照。

### 2.5 パッケージ同梱物

package 直下に置く（すべて購入者向け。§2.2 の情報分離を守る）:

| 同梱物 | 要件 |
|---|---|
| `package.json` | `displayName` / bilingual `description`（**英語説明→改行（`\n`）→日本語説明の2段書き**。2026-07-23 制定: 1行目は英語・2行目は日本語とし、`" / "` 連結の1行書きは使わない。Package Manager の詳細欄では改行がそのまま表示される）/ `unity` / **`unityRelease`**（例 `"0f1"`。UAS の UPM 検証要件・他経路でも無害）/ `documentationUrl` / `changelogUrl` / `licensesUrl`（§2.8 の URL 規約）/ `author.name`。販売物には問い合わせ導線として `author.url` を推奨。 |
| `README.md` | 導入手順（購入者が実際に受け取る形式 = .tgz / .unitypackage / VPM 前提）と最初の一歩。 |
| `CHANGELOG.md` | Keep a Changelog 形式。`Unreleased` 節を開発中の受け皿にし、リリース時に版へ畳む。 |
| `LICENSE.md` | ライセンス全文（**ファイル名は `.md` に統一**）。 |
| `Third Party Notices.md` | サードパーティ成分（フォント・音源・ライブラリ等）と各ライセンスの列挙（UAS 1.2.a 要件）。**成分が無い場合も「サードパーティ成分は含まれない」と明記した最小ファイルを置く**。GPL/LGPL/CC/帰属要求付き Apache 2.0 の成分は混入させない（UAS 1.2.b）。 |
| `Documentation~/` | 詳細ガイド（Unity にインポートされない UPM 慣例フォルダ）。商品ページの usage / installation と内容整合を保つ。 |
| `Samples~/` | サンプルを持つ場合。`package.json` の `samples` 配列に必ず定義する。 |
| `skills/` | AI エージェント向けスキルを同梱する場合（§2.6）。 |

### 2.6 同梱スキル標準

パッケージがエージェント連携機能を持つ場合、スキルを package 内 `skills/` に同梱する。その際つぎを**必須**とする:

1. **インストール支援機能を必ず同梱する。** UEL / UEWCE で実証済みのパターン: Settings/Preferences ペインから、**ユーザースコープ**（`~/.claude/skills`・`~/.agents/skills`）と**プロジェクトスコープ**（リポジトリ直下）へ symlink 登録できる。登録状態（有効・欠損・未登録）を状態ピルで表示し、symlink が使えない環境向けにコピペ実行可能な CLI スニペットのクリップボードコピーを提供する。参照実装: UEL `Editor/Skills/EditorL10nSkillInstaller.cs`、UEWCE `Editor/Skills/CaptureSkillInstaller.cs`。
   - **メニューは間接方式のみ**（2026-07-22 ユーザー指示）: `Tools > {製品名} > AI Agent Skills` は**単一項目**とし、押すとスキル導入の説明・登録状態・実行ボタンを持つ Settings/Preferences ペイン（または専用ウィンドウ）を**開くだけ**にする。メニュー項目から直接スキル登録・クリップボード書き込み等の副作用を実行してはならない。スキルの追加は、ユーザーがペイン内で内容を理解したうえで明示的にボタンを押した場合にのみ行う（意図しないスキル追加は開発者に敬遠される挙動のため）。
   - **ペインに同梱スキルの内容一覧を表示する**（2026-07-23 ユーザー指示）: スキルごとに「名前（ローカライズした概要名）・要約説明・プロンプト例 1 行（`プロンプト例:` ラベル付き）」を**登録操作より上**に短いリストで提示し、導入者が「何が追加されるのか」をペイン内で把握してから登録ボタンを押せるようにする。併せて各スキルの**正本フォルダ（package 同梱 `skills/<skill>`）へのクリック可能な導線**（クリックで Project ビューに選択＋Ping）を置き、SKILL.md を含む全文を登録前に確認できるようにする。スキル数が多く縦に長くなる場合は折りたたみ等で認知負荷を抑える（目安: 4 件以上でスキルごとに畳む）。実装は `unity-editor-ui-design` スキルの部品（HintRow / AssetRow 等）の合成で行い、文言は各パッケージの翻訳カタログへ全ロケール一括で追加する。参照実装: UEL `EditorL10nSettingsProvider.BuildSkillRow` / UEWCE `CaptureSettingsProvider.BuildSkillRow`。
   - **CLI コマンドは表示とセットで提供する**（2026-07-24 ユーザー指示）: コピーボタンだけを置かず、**実行中の OS に応じて生成したコマンド全文**を読み取り専用の複数行フィールドでペイン内に表示し、導入者がその場で内容を確認してからコピーできるようにする。コマンドは長くなるため、「案内文＋コマンド表示＋コピーボタン」を一式で**既定で畳んだ折りたたみ**（開閉は EditorPrefs に保持）へ収めて認知負荷を抑える。コピー結果はコンソールだけでなくボタン近くにインラインでも見せる。参照実装: UEL / UEWCE の Settings ペインの CLI 折りたたみ。
2. **二系統ハーネス対応。** スキルは Claude 系（`.claude/skills`）と AGENTS 規約系（`.agents/skills`）の両方で同一内容が動作するように書く。ハーネス固有機能（特定ツール名等）に依存する場合は代替手順を併記する。
3. **作成と相互レビューのプロセス。** スキルは最上位モデル（Claude Fable 5 等）で作成し、**gpt 系エージェント（Codex）のレビュー**を受けて、下位モデル・他系統モデルでも同品質で動く表現に改善してから確定する。書式は「番号付き一本道の手順・決定表（条件→結論）・明示的な禁止列・完了定義」を基本とする（MySite `skills/AGENTS.md` の規約と同一思想）。
4. **正本と生成ミラーの分離。** リポジトリ内での配置は package 同梱 `skills/` を正本とし、リポジトリ直下の `.claude/skills` / `.agents/skills` は `scripts/sync-agent-skills.mjs` による生成ミラーとする（直接編集しない。`--check` で drift 検査）。スクリプトの正本はテンプレートリポジトリ `scripts/sync-agent-skills.mjs` で、各リポジトリへはバイト同一で複製する（改修時はテンプレ側を直してから全リポジトリへ配布する）。
5. **開発リポジトリ内ではインストーラの project スコープ登録を使わない。** project スコープの登録は上記のコミット済み実体ミラーで満たされている。インストーラの「Install for this project」を開発リポジトリで実行すると、実体ディレクトリの中へ入れ子の symlink を作ってミラーを汚すおそれがある（既知の設計上の緊張点）。インストーラ実装側では、登録先に実体ディレクトリが既に存在する場合は no-op（登録済み扱い）とするガードを入れることが望ましい。利用者のプロジェクトでは従来どおり symlink 登録が正。

### 2.7 リリース資材 — `Publish/` 運用

- 出力先はリポジトリ直下 `Publish/`。内容と命名:
  - 配布 zip: `{DisplayName}-{version}.zip`（**ハイフン区切りに統一**。アンダースコア禁止）
  - UPM tarball: `{package-name}-{version}.tgz`
  - unitypackage: `{package-name}-{version}.unitypackage`
  - VPM 用 zip: `Publish/vpm/{package-name}-{version}.zip`
- 書き出しは EPE の Exporter（UnityPackageExporter / TarballPackageExporter / VpmPackageExporter / ZipPacker）の設定アセット（`Assets/` 配下）から行う。設定アセットはリポジトリにコミットして再現可能にする。
- `.gitignore` は例外宣言方式で配布物をコミット対象にする（UEL / UEWCE 方式）:

```gitignore
# 販売資材（Publish 配下の配布物はコミット対象）
!Publish/*.zip
!Publish/*.tgz
!Publish/*.unitypackage
!Publish/vpm/*.zip
```

- 旧版の資材も履歴として残す（削除しない）。作業用の中間フォルダ・空フォルダは置かない。

### 2.8 URL 規約

- 商品ページ: `https://kajitaharuka.com/products/{slug}/`（言語プレフィックスなしの naked URL は MySite 側の言語リダイレクトが解決する）。
- `package.json` の各 URL:
  - `documentationUrl`: `https://kajitaharuka.com/products/{slug}/`
  - `changelogUrl`: `https://kajitaharuka.com/products/{slug}/changelog/`
  - `licensesUrl`: `https://kajitaharuka.com/products/{slug}/licenses/`
- コード内から開くオンラインドキュメント URL（ヘルプボタン等）は 1 クラスに定数として一元管理し（EPE `EpeDocs.cs` 方式）、**実在するページ**だけを指す。**ページ内アンカー（`#見出し`）は使わない** — 見出しアンカーは言語ごとに異なり、多言語サイトでは naked URL の言語リダイレクト先で壊れるため、ページ先頭への遷移に留める（2026-07 EPE DocButton 修正で確立）。
- Notion 等の外部サービスをドキュメント正本にしない（過去の暫定運用は kajitaharuka.com へ移行する）。

### 2.9 リポジトリ直下のエージェント設定

- `.claude/skills` と `.agents/skills`（スキルミラー）は Git 追跡する。ミラーは §2.6-4 の sync スクリプトで生成し、手動編集しない。
- `.claude/launch.json` は、**そのリポジトリに正当なプレビュー設定がある場合のみ**追跡する。個人ワークフロー由来の stray 設定（他リポジトリの dev サーバ設定等）はコミットせず、`.gitignore` で予防する（2026-07 の UEL 監査で他リポジトリ向け launch.json の紛れ込みを検出した教訓）。

### 2.10 パイプライン整合性の 3 層（2026-07-27 制定）

標準・実装・商品情報のずれを「規律」ではなく「仕組み」で防ぐ。設計の正本は MySite `docs/pipeline-consistency-design.md`。

| 層 | 目的 | 実体 |
|---|---|---|
| 第 1 層: 配布 | 参照切れを構造的に消す | テンプレートの `scripts/distribute_standard.py` が `docs/GOLD_STANDARD.md` / `scripts/pipeline/*.py` / `docs/REPOSITORY_MAP.md` を各リポジトリへ**コピー配布**する |
| 第 2 層: 検証 | 陳腐化を検出可能にする | 各リポジトリの `scripts/pipeline/verify_repo_guide.py`（10 検査） |
| 第 3 層: 契約 | 開発リポジトリと商品情報のずれを潰す | `scripts/pipeline/emit_release_manifest.py` が `release-<version>.json` を**2 箇所**へ書く |

```bash
python3 scripts/pipeline/verify_repo_guide.py            # 標準準拠検査（error があれば非ゼロ終了）
python3 scripts/pipeline/emit_release_manifest.py        # リリース契約ファイルの生成（成果物コミット後）
python3 scripts/distribute_standard.py --check           # テンプレート側: 配布の drift 検査
```

**配布プロファイル**（正本は MySite `pipeline/repositories.json` の `standardProfile`。リポジトリ側には持たせない）:

| プロファイル | 配る部品 | 対象 |
|---|---|---|
| `full` | 標準 ＋ 検査 ＋ 契約生成 ＋ CI ＋ 地図 | Unity パッケージの販売リポジトリ |
| `guide` | 検査 ＋ CI ＋ 地図 | サイト・商品情報・技術書・配信基盤（Unity 検査は対象が無いので自動的に空振りする） |
| `source` | CI ＋ 地図 | テンプレート自身（標準と検査の正本を持つため配布は受けない） |
| `none` | なし | git 管理外のサンドボックス |

**配布物は編集しない。** 冒頭に生成物ヘッダ（`source-sha256`）が機械挿入されており、本文を書き換えると検査 3 が落ちる。標準を変えるときはテンプレート側を直して再配布する。

- **保証範囲**: これは「うっかり配布物を直してしまった」を検出する仕組みであって改竄対策ではない（本文とヘッダを同時に書き換えれば通る）。**鮮度**は `pipeline/standard-manifest.json` の source commit と、テンプレート側での `--check` で確認する。テンプレートの変更は**先にコミットしてから配布する**（dirty のままだと台帳に source commit を記録できない）。
- **リポジトリ側の宣言は `pipeline/repo.json`**（手書き・配布対象外）。`role` / `productSlug` / `tagPolicy`（`bare` / `v-prefix`）/ `saleUnit`（`packages` / `versionPolicy` / `distribution` / `exporterAssets`）/ `packagePolicies` / `skillRefs` / `waivers` を持つ。**タグ名は推測しない** — `tagPolicy` が無い、または既存タグの命名が混在しているリリースは判定不能として停止する。
- **判定の主役は構造化された宣言**で、自然言語のヒューリスティック（文書の言い回し）は補助の warn に留める。誤検出は検査を消すのではなく `waivers` へ `{checkId, target, reason, expiresAt}` の形で**理由を添えて**登録する。理由なし・期限切れ・未知の checkId は error になる。
- **リポジトリ一覧の正本は MySite `pipeline/repositories.json`**。テンプレートには置かない（派生した新規リポジトリが古い一覧を持って生まれるため）。`docs/REPOSITORY_MAP.md` はそこから生成される地図で、**URL とローカルパスは含めない**（公開リポジトリ向けには非公開リポジトリの行も出さない）。
- 日常の実行は任意、**リリース時は省略不可**（§3 のリリース工程・`release-unity-package` の検証ゲート 1.5）。
- **Python 3 が無い環境**: 日常の実行では警告してスキップしてよい。**リリース工程では「判定不能」として停止する**（報告しただけで先へ進まない）。

**リリース時の実行順序（この順でないと契約ファイルが決まらない）**:

1. 成果物を書き出す → 内容を検証する
2. **成果物をコミットする**（契約ファイルの `sourceCommit` は「成果物を最後に変更したコミット」なので、未コミットだと決まらず fail-closed になる）
3. `emit_release_manifest.py` を実行し、開発リポジトリと external-content の 2 箇所へ契約ファイルを書く
4. 契約ファイルをコミットする（契約ファイル自身のコミットは契約の対象外）
5. タグを作る（`tag` は `tagPolicy` と version から決まる期待名）
6. **タグ作成後にもう一度 `emit_release_manifest.py --check` を実行**し、タグが `sourceCommit` を含むこと・契約ファイルに差分が無いことを確認する

> **注意**: Unity の `Client.Pack` は tgz 内の `package.json` へ `_upm.revision`（その時点の git commit SHA）を書き込む。**同じ version でも書き出し直すと中身が変わる**ため、成果物は 1 回だけ書き出し、契約ファイルを作った後に再書き出ししない。

## 3. 開発→リリース→商品化フロー

| フェーズ | 内容 | 使用スキル |
|---|---|---|
| 0. 立ち上げ | テンプレートからリポジトリ作成・リネーム・URL 差し込み | `scaffold-unity-package-repo` |
| 1. 開発 | 実装・Inspector 設計・多言語化 | `unity-editor-ui-design` / `editor-localization-optional-integration` / `editor-localization-translation-quality` / `unity-mcp-skill` / `unity-cli` |
| 2. 検証 | Test Runner・手動チェック・スクリーンショット | `editor-window-capture` |
| 3. リリース | version 確定 → **標準準拠検査（§2.10）** → CHANGELOG 畳み込み → Publish/ 書き出し → **契約ファイル生成（§2.10）** → コミット/タグ → publish.json 追従 | `release-unity-package` |
| 4. 商品化 | promo 画像 → meta/pages（ja/en → 19言語）→ publish.json → 出品下書き | `new-product-onboarding`（`write-my-promo-images` / `write-my-product-page` / `publish-to-platform` を束ねる） |
| 5. 出品・公開 | フォーム入力・添付まで自動、**公開ボタンは人間**。UAS 出品対象の商品は §7 のプロファイル（専用成果物・Validator・Key Images）を併用 | `publish-to-platform` |
| 6. 公開後 | platformRefs.status 更新・meta.platformLinks 昇格・公開後にしか編集できない項目の反映 | `publish-to-platform`（公開後フェーズ） |

- エージェントは各フェーズで該当スキルを必ず参照する。スキルに無い判断が必要になったら、作業を止めずに最良判断で進め、**暫定判断として最終報告で強調**する。

## 4. 商品情報標準（概要）

正本は MySite 側ドキュメント。ここでは構成の要点のみ:

```
external-content/products/{slug}/
  meta.json                 # サイト表示用（$schemaVersion 2・入手導線3状態モデル）
  publish.json              # 販売の単一正本（$schemaVersion 1 / v1.1 規約）
  pages/{overview,usage,installation,changelog,licenses}/{lang}.mdx   # 19言語
  assets/                   # cover / cover-card / cover-og ＋実機スクリーンショット
  descriptions/{ja,en}.md   # プラットフォーム出品用説明文
  publish-assets/           # booth-main / gumroad-cover / gumroad-thumbnail
  licenses/                 # ライセンス原文
```

- 実機スクリーンショットは UEWCE（editor-window-capture）で撮影する。合成・演出画像は promo-kit（`write-my-promo-images`）。
- 公開状態の対応: 未出品=`previewPlatforms` のみ → 公開済み=`platformRefs` の URL を `meta.platformLinks` へ昇格＋`status: "published"`。

## 5. エージェント運用標準

- **コミュニケーション・コミット・文書・コメントは日本語**。コミットは短い1行サマリー（接頭辞なし）。
- 複数リポジトリを跨ぐ作業では**必ず明示的に cd してから git 操作**し、コミット後に diff stat の整合を確認する。
- 出品の保存・公開・削除の確定操作は行わない（入力・添付・下書きまで）。
- 作業は各リポジトリの **develop へ直接コミット**する（2026-07-23 指示。ターンごとの作業ブランチは作らない。品質は計画→実装→検証→Codex レビューの徹底で担保し、レビュー可能な単位でコミットする）。**例外: テンプレートリポジトリ（UnityTemplate_2022_3_22f1）は main 一本で運用する**（2026-07-23 指示。develop を作らない）。

### 5.1 エージェント検証基盤 — Unity 操作の自動化（2026-07-23 制定・Codex 協議反映・2026-07-24 実測改訂）

Unity のフォーカス・コンパイル・リインポート・テスト・`.meta` 生成をユーザー操作に依存させないための実行経路の決定表。Unity CLI（1.0.0-beta 系・`~/.unity/env` で PATH 追加）と MCP for Unity（全リポジトリ manifest に標準搭載）を基盤とする。

| 状況 | 経路 |
|---|---|
| **起動中の 2022.3 エディタへの操作** | MCP for Unity（refresh / console / tests）。stdio ブリッジは `[InitializeOnLoad]` で**常時自動起動**しており（EditorPrefs `MCPForUnity.UseHttpTransport=0` が前提。エディタ UI の Start Session 操作は不要）、エディタが起動していれば外部から接続できる。複数エディタ起動時は `set_active_instance` で `Name@hash` を必ず pin する。「No Unity Editor instances found」が出ても「未起動」の証明にしない — `~/.unity-mcp/unity-mcp-status-*.json` の heartbeat を確認し、生きていれば再試行する（初回接続に一時的な発見失敗があることを 2026-07-24 に実測。再試行で成功） |
| **非起動プロジェクトの読み取り型検証**（コンパイル・EditMode テスト） | 排他確認 → Unity CLI でエディタ解決 → compile/import は `-batchmode -quit`（`unity run` 相当）、テストは **`-quit` を付けず** `-runTests`（結果 XML 生成前の終了を防ぐ） |
| **`.meta` 生成・再シリアライズ**（作業ツリーが変わる操作） | 通常検証と分離した「**変異モード**」として専用 worktree で実行し、終了後に変更一覧を報告する。worktree は develop 本体と衝突しないよう **detached HEAD（対象コミットを直接 checkout）または検証専用の一時ブランチ**で作る（同一ブランチは複数 worktree に checkout できないため） |
| **worktree 上の未コミット変更の検証**（worktree で開発している場合） | worktree は Git 的にも Unity 的にも独立したプロジェクトなので、**本体エディタが起動中でも worktree を直接 `-batchmode` 起動**してコンパイル・EditMode テストを実行できる（`Temp/UnityLockfile` が別で排他に抵触しない。Library 構築済みなら数分）。相対 `file:` のローカル依存（EditorLocalization 等）は worktree からは解決できないため、manifest.json の該当エントリを**一時的に絶対パス化→検証後に復元**する（この一時変更と packages-lock.json の追従差分はコミットしない）。旧運用の「detached worktree へ `git diff \| git apply` で差分適用してから batch 検証」は、この直接 batch で代替できるため廃止（2026-07-24） |
| **UPM パッケージ増減** | `unity-package-management` スキル（Client API）。非同期 API（`Client.Add*` 等）は `-quit` なしで起動し、完了コールバックから `EditorApplication.Exit(code)` する専用エントリポイントを使う。manifest 手編集の例外は「bootstrap 不能時にユーザーが明示承認した宣言的変更」のみ |
| **同一プロジェクトが起動中／判定不能** | batchmode を**拒否**（fail-closed）。判定は project realpath の照合＋`Temp/UnityLockfile`＋実行中プロセスの `-projectPath` の多層で行う。MCP 操作か別 worktree へ切り替える |
| **並列実行** | マシン共通の直列化を既定とする（同時 batch 起動は既定 1。リソース計測後にのみ引き上げ） |
| **エディタ導入・環境診断** | Unity CLI（`install` / `editors` / `doctor`。`--format json` でパース） |

- **証跡の標準**: 検証実行は `Logs/AgentValidation/<run-id>/`（**gitignore 対象＝追跡外**。下記「Git 差分ゼロ」判定の対象にしない）に `editor.log`・テスト結果 XML・`summary.json`（project realpath / commit SHA / 前後の dirty / エディタと CLI の version / 引数 / 時刻 / exit code / テスト集計）を残す。
- **作業ツリー不変の契約**: 読み取り型検証は「開始前後で Git 差分ゼロ」を成功条件に含める（`.meta`・`packages-lock.json`・ProjectSettings の意図しない変化を検出する）。
- **Unity 6 ロードマップ**（2026-07-24 ユーザー方針で確定）: 現行の **Unity 2022.3 維持は VRChat の指定バージョン準拠が理由**（販売物の一級ターゲット= VRChat ユーザー、一般開発者はその次のボリューム想定のため、VRChat 制約を受け入れる）。VRChat は Unity 6 への移行を進行中（beta 版は既に特定の Unity 6 バージョンで稼働）で、**移行が確定した時点でテンプレートと開発中リポジトリを Unity 6 へ更新**する。その時点で `com.unity.pipeline`（Unity 6.0 LTS 以降・experimental）の `unity command` / `[CliCommand]` / トークンゲートの `eval` を**製品リポジトリ外の使い捨てコピーで隔離評価**してから正式経路へ昇格し、完全なエージェントネイティブ開発基盤を整える（MCP の全面置き換えではなく用途別併用。安定した登録コマンド= `[CliCommand]`、探索的読み取り= `eval`）。それまでは 2022.3 の範囲（MCP＋CLI batch＋worktree 検証）でできる対応を行う。
- **改善提案の義務**: スキル化すべき反復工程、既存スキルの改善点、この標準自体の改善点を発見したら、作業完了報告に「提案」節としてまとめる。標準の変更はテンプレートリポジトリの本ファイルに反映してから各リポジトリへ展開する。

## 6. 準拠チェックリスト

新規リポジトリの立ち上げ時・既存リポジトリの監査時に使う。
**テンプレートの最小充足セット**: テンプレートリポジトリ自体は、下記のうち「多言語（L10N 統合・19 言語カタログ）」「skills 同梱」「商品情報」を除く項目を雛形として満たす（L10N seam と skills は派生後に `editor-localization-optional-integration` 等のスキルで導入する設計。テンプレを重くしない）:

- [ ] パッケージ実体が `Packages/{package-name}/` にあり、フォルダ名が package name と一致する
- [ ] package name のセグメント内が kebab-case である
- [ ] `CLAUDE.md` と `AGENTS.md` が存在し**同一内容**である（§2.2 の 7 要素を含む）
- [ ] root `README.md` が存在し、開発者向け導線と商品ページリンクを持つ
- [ ] 購入者向け文書（package 内 README / Documentation~）に開発者専用情報が無い
- [ ] Editor 専用 asmdef（または Runtime/Editor 分離）が正しい
- [ ] EditMode Tests と `testables` 登録がある
- [ ] UEL 任意依存統合が入り、19 言語カタログがある
- [ ] package 直下に README / CHANGELOG / LICENSE.md（/ Documentation~ / Samples~ 定義）がある
- [ ] skills 同梱時: インストール支援機能・二系統ミラー・sync スクリプトがある
- [ ] `Publish/` 運用（命名・gitignore 例外・Exporter 設定アセット）が標準どおり
- [ ] `package.json` の URL 3 種が `/products/{slug}/` 規約に従い、実在ページを指す
- [ ] `package.json` に `unityRelease` がある／`Third Party Notices.md` を同梱している（§2.5）
- [ ] `package.json` の `description` が「英語→改行→日本語」の2段書きである（§2.5）
- [ ] ファイルパスが 150 字未満（実行手段は `audit-package-path-length` スキル。UPM は `<package-name>/` 起算・フォールバック .unitypackage は `Assets/<DisplayName>/` 起算・スイートは全パッケージ・`.meta` 含む）・コンソールのエラー/警告ゼロ・deprecated API 警告ゼロ（リリース前チェック）
- [ ] **パッケージ配下の全ファイル・全サブフォルダに `.meta` があり、git 追跡済みである**（Unity が無視する名前＝先頭 `.`・末尾 `~`・末尾 `.tmp`・`cvs` は除外）。`.meta` の欠落は開発リポジトリでは Unity が自動生成するため気付けず、**UPM/git 経由で導入した利用者側でのみ** `has no meta file, but it's in an immutable folder. The asset will be ignored.` として現れる。実行手段は `release-unity-package` スキルの検証ゲート 1.5-6
- [ ] UAS 適格性（対象/対象外と理由）が publish.json と CLAUDE/AGENTS 準拠節で宣言されている（§7.1）
- [ ] 商品情報（external-content 一式・publish.json・promo-kit data）が存在する（販売物のみ）
- [ ] **`pipeline/repo.json` があり、`python3 scripts/pipeline/verify_repo_guide.py` が error 0 で終わる**（§2.10）
- [ ] **MySite `pipeline/repositories.json` に登録され、標準の配布物（`docs/GOLD_STANDARD.md` / `scripts/pipeline/` / `docs/REPOSITORY_MAP.md`）が最新である**（§2.10）

## 7. Unity Asset Store 出品プロファイル（オプトイン）

UAS へ出品する商品にのみ適用する追加要件。調査正本と根拠は MySite `docs/unity-asset-store-publishing.md`（2026-07 調査。Submission Guidelines 2026-05-20 版に基づく）。

### 7.1 適格性の宣言（全商品で必須）

- 各商品は UAS 出品の対象/対象外を **`publish.json` の `targets.unity-asset-store.enabled`** と **CLAUDE/AGENTS 準拠節の「UAS 適格性」欄（理由付き）**で宣言する。
- オプトアウト事由: ①内部 API リフレクション等のガイドライン抵触が核心機能（例: UEWCE）②UdonSharp/VRChat SDK 等のプラットフォームロックイン ③戦略・工数判断。

### 7.2 配布形式

- **既定 = UPM 商品**（enrollment〔Persona 本人確認＋ `com.kajitaharuka` namespace 検証〕は 2026-07 完了済み。2026-07-22 開発者決定で .unitypackage 既定から変更）。開発中の embedded package（`Packages/<package-name>/`）をそのまま出品単位とし、UAS 専用の再配置成果物は作らない。
  - manifest 要件（5.2.e-k）: `name`・`displayName`・semver `version`・`unity`/`unityRelease`・`author`（= パブリッシャー名）・`description`。
  - 依存は「Unity 公式パッケージ」または「**同一商品に含まれる**自作パッケージ」のみ（5.2.c）。UEL 任意連携は Version Defines 方式で依存宣言を持たないため適合。
  - サイズ上限 **700MB**（.unitypackage の 6GB より厳格）。パス長は `<package-name>/` 起算 150 字未満（2.1.e）。
- **スイート商品（core＋companion）= UPM マルチパッケージ 1 商品**。パッケージは分けたまま 1 つの商品に複数パッケージを同梱する（5.2.c の「same published product」依存許可が根拠）。旧方針の「.unitypackage 1 ルート同梱」は廃止し、フォールバック時のみ使う。
- **配布形式は商品タイプで選択する**（2026-07-23 改訂: 単なるフォールバックではなく一級の選択肢へ。publish.json の `platforms.unity-asset-store.distribution` で宣言し、未記載は upm とみなす）:

| 商品タイプ | 配布形式 |
|---|---|
| エディタ拡張・ツール・ライブラリ（コード系。現行 5 製品すべて） | **UPM（既定）** |
| 3D / 2D / VFX / Animation / Template 等のコンテンツ系（デモシーン必須カテゴリ）、ProjectSettings 同梱の Complete Project | **.unitypackage** |
| UPM 審査で想定外の却下があったコード系商品 | .unitypackage（フォールバック） |

  .unitypackage を使う場合は `Assets/<DisplayName>/` 1 ルートに収めた UAS 専用成果物（自販路の Packages/ パス構成 .unitypackage とは別物）を書き出す。tgz 同梱で本体を隠す形式は使わない（2.2.a）。
- 未確定（初回 UPM 出品時に Publisher Portal / Uploader で確認）: スイートの core を `com.publisher.product`（サブ名なし）のままにできるか、マルチパッケージ商品の実際の登録手順。

### 7.3 出品前チェック（Validator の前に自前で満たす）

- [ ] エラー/警告ゼロ・deprecated API 警告ゼロ（1.1.b / 2.5.i）
- [ ] ファイルパス < 150 字（.unitypackage は `Assets/` 起算、UPM は `<package-name>/` 起算。2.1.e）
- [ ] Third Party Notices 同梱・GPL 系混入なし（1.2）
- [ ] ドキュメント同梱（.md/.pdf 等。2.3）・外部アセット操作ツールはサンプル同梱（1.1.f）
- [ ] メニューは Tools/ 配下・エディタ外への自動リダイレクトなし・パッケージの自動追加/削除なし（2.5.1）
- [ ] Domain Reload オフ監査（2.5.h）: `RuntimeInitializeOnLoadMethod|InitializeOnLoad|playModeStateChanged|AssemblyReloadEvents|static event|ScriptableSingleton` を grep し、「Play モード出入りで static がリセットされる前提」のコードが無いことを確認（判定基準: Domain 生存中維持が正しい常駐購読/レジストリは適合、Play セッション毎の可変 static は `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` でリセット。2026-07 全 5 リポジトリ監査済み・指摘 0）
- [ ] 説明文に依存・制約を透明開示（1.1.c / 3.1）。UEL 任意連携も明記
- [ ] Key Images: Icon 160×160（文字不可）/ Card 420×280 / Cover 1950×1300 / Social 1200×630（文字不可）、24bit PNG・スクリーンショット幅 1200px 以上（promo-kit で生成）
- [ ] Asset Store Publishing Tools の **Validator を実行して合格**（UPM の場合は Validation Type: UPM）
- [ ] 提出エディタは 2022.3 系を維持（1.3。6.5+ で提出すると URP/HDRP 要件が付く）
- [ ] 有償商品の価格は **US$5.00 以上**（2026-07-23 制定の自主フロア。市場実勢の最低価格 $4.99 の観測に基づき区切りよく $5.00 とする。公式の最低価格規定は初回出品時に Portal で確認）

### 7.4 ライセンスの二本立て

- UAS 販売分は **Asset Store EULA** に従う（独自 EULA の掲示は原則不可・代替条項は Unity の書面同意が必要）。
- 自販路（Booth/Gumroad/自サイト）は独自 EULA。両者が実質的に矛盾しない設計にする（ライセンス設計の検討時にこの前提を必ず含める）。

### 7.5 提出と公開

- Publisher Portal でのドラフト作成・メタデータ入力・審査提出は当面手動（自動化は将来。**保存・提出・公開の確定操作は人間** — §1 の原則どおり）。
- 審査で却下された場合は指摘を修正して再提出（無修正の再提出はアカウント停止事由。2.1.h）。

## 付録: 参照先

- パイプライン正本: MySite `docs/publish-json-schema.md` / `docs/roadmap-multi-platform-publishing.md` / `docs/runbook-content-sync.md` / `skills/AGENTS.md`
- 実証リポジトリ: ExportPackageExtension / UnityEditorLocalization / UnityEditorWindowCaptureExtension
- スキル: `~/.claude/skills`（グローバル）、MySite `skills/`（商品情報・出品系）、各パッケージ同梱 `skills/`
