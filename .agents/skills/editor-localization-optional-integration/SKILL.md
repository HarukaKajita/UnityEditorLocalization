---
name: editor-localization-optional-integration
description: Scaffold optional UnityEditorLocalization integration into a consuming Unity editor package. Use when a package (for example a paid asset) must compile and run standalone in a single default language and then automatically light up multi-language UI plus a locale switcher when UnityEditorLocalization (com.kajitaharuka.unity-editor-localization) is installed, without any hard assembly reference. Generates the bridge seam in the main assembly, a define-constrained integration assembly, and the EditMode test that machine-checks translation-key catalog coverage.
---

# UnityEditorLocalization Optional Integration

Scaffold the two-assembly optional-dependency pattern described in `Documentation~/OPTIONAL_INTEGRATION.md`. The reference implementation is ExportPackageExtension (`Kajitaharuka.ExportPackageExtension`).

The result: the consuming package works standalone in its `defaultLocale`, and—only when UnityEditorLocalization is present—an integration assembly compiles and swaps in multi-language behaviour with a locale switcher. No hard `asmdef` reference and no `dependencies` entry on the base package.

## When to use

- A package that must build and run without UnityEditorLocalization (single language) and gain multi-language + locale switching when it is installed.
- The integration must not add a hard dependency: integration code compiles only when the base package is present (via Version Define + Define Constraint).

## Inputs (gather before generating)

| Token | Meaning | Example |
| --- | --- | --- |
| `{{MAIN_ASMDEF}}` | Main editor assembly name | `Kajitaharuka.ExportPackageExtension` |
| `{{ROOT_NAMESPACE}}` | Main editor namespace | `Kajitaharuka.ExportPackageExtension.Editor` |
| `{{PREFIX}}` | Short facade prefix | `Epe` (yields `EpeL10n`, `EpeL10nUi`) |
| `{{SCOPE}}` | l10n scope = the package `*.l10n-manifest.json` `scope` | `com.kajitaharuka.export-package-extension` |
| `{{MIN_VERSION}}` | Minimum base version that has the needed API | `1.0.0` |
| `{{LOG_TAG}}` | Prefix for `Debug.LogError` | `ExportPackageExtension` |
| `{{TESTS_NAMESPACE}}` | Namespace of the package's EditMode tests (catalog-coverage test only) | `Kajitaharuka.ExportPackageExtension.Tests` |
| `{{TESTS_ASMDEF}}` | EditMode test assembly name (catalog-coverage test only) | `Kajitaharuka.ExportPackageExtension.Tests` |

Derived automatically:

- `{{INTEGRATION_ASMDEF}}` = `{{MAIN_ASMDEF}}.EditorLocalizationIntegration`
- `{{INTEGRATION_NAMESPACE}}` = `{{ROOT_NAMESPACE}}.LocalizationIntegration`
- Shared define symbol = `KAJITAHARUKA_EDITOR_L10N` (fixed; do not rename per package)
- Base package id = `com.kajitaharuka.unity-editor-localization` (fixed)
- `{{PREFIX}}TextKey` = the translation-key aggregation class. **This skill does not generate it** — it must
  already exist in the consuming package (see `Documentation~/DEVELOPER_GUIDE.md`). The catalog-coverage test
  enumerates it.

## Workflow

1. Read `Documentation~/OPTIONAL_INTEGRATION.md` for the pattern, naming rules, and gotchas.
2. Confirm the consuming package already has `*.l10n-manifest.json` and `Locales/*.json` (the `defaultLocale` table is required for standalone display). If missing, create them first (see `Documentation~/DEVELOPER_GUIDE.md`).
3. Copy `templates/` into the consuming package and apply the file mapping + token replacement below.
4. Remove `Kajitaharuka.EditorLocalization` from the **main** assembly asmdef `references`.
5. Route base-API calls in the main assembly through the facades:
   - `EditorL10nUi.*` → `{{PREFIX}}L10nUi.*`
   - `EditorL10n.Tr(scope, ...)` → `{{PREFIX}}L10n.Tr(...)`
   - `EditorL10nUi.CreateLocalizedCompactLocaleMenu(...)` → `{{PREFIX}}L10nUi.CreateCompactLocaleMenu(...)`; treat the result as a nullable `VisualElement` (it is `null` when the base package is absent).
   - Remove `using Kajitaharuka.EditorLocalization;` from every main-assembly file.
6. Do **not** add `com.kajitaharuka.unity-editor-localization` to the main `package.json` `dependencies`. Document it as a recommended optional add-on in the README instead.
7. Place the catalog-coverage EditMode test. **This step is not optional** — see
   「翻訳キーの網羅を機械で見る EditMode テストを必ず置く」below for what it catches, the prerequisites to
   check first, and what to do when a package does not meet them.
8. Verify (see below).

## File mapping (templates → output)

Place under `<package>/Editor/`:

| Template | Output path |
| --- | --- |
| `templates/Localization/IEditorL10nBridge.cs.txt` | `Localization/IEditorL10nBridge.cs` |
| `templates/Localization/EditorL10nRuntime.cs.txt` | `Localization/EditorL10nRuntime.cs` |
| `templates/Localization/DefaultEditorL10nBridge.cs.txt` | `Localization/DefaultEditorL10nBridge.cs` |
| `templates/Localization/ProductL10n.cs.txt` | `Localization/{{PREFIX}}L10n.cs` |
| `templates/Localization/ProductL10nUi.cs.txt` | `Localization/{{PREFIX}}L10nUi.cs` |
| `templates/Localization/ProductL10nAssemblyInfo.cs.txt` | `Localization/{{PREFIX}}L10nAssemblyInfo.cs` |
| `templates/L10nIntegration/Integration.asmdef.txt` | `LocalizationIntegration/L10nIntegration.asmdef`（ファイル名は短縮固定。assembly `name` はテンプレ内で `{{INTEGRATION_ASMDEF}}` のまま） |
| `templates/L10nIntegration/EditorL10nBridge.cs.txt` | `LocalizationIntegration/EditorL10nBridge.cs` |
| `templates/L10nIntegration/BridgeInstaller.cs.txt` | `LocalizationIntegration/EditorL10nBridgeInstaller.cs` |

Place under `<package>/Tests/Editor/`:

| Template | Output path |
| --- | --- |
| `templates/Tests/TextKeyCatalogCoverage.cs.txt` | `{{PREFIX}}TextKeyCatalogCoverageTests.cs` |

If the package already has a `{{PREFIX}}L10n` facade, merge `Tr` to delegate to `{{PREFIX}}L10nRuntime`/`EditorL10nRuntime.Bridge` instead of calling `EditorL10n` directly, rather than overwriting unrelated members.

Replace every `{{TOKEN}}` occurrence (in file contents and in output file names) with the gathered values.

## Critical rules

- The asmdef `versionDefines.expression` must be a **bare version** such as `"1.0.0"`. Interval notation like `[1.0.0,)` throws `ExpressionNotValidException` and silently disables the integration (the Define Constraints shows a red mark). A bare version means "this version or newer".
- `defineConstraints` must contain `KAJITAHARUKA_EDITOR_L10N`, the same string the version define produces.
- Keep all code comments in Japanese (project rule).
- Wrap every generated source file in `#if UNITY_EDITOR ... #endif`.
- The main assembly must not reference `Kajitaharuka.EditorLocalization` in any way (asmdef, `using`, or API calls).
- **Asmdef file names must stay short**: save the integration asmdef as `L10nIntegration.asmdef` (the assembly `name` field keeps the full `{{INTEGRATION_ASMDEF}}` value). Full-length file names such as `Kajitaharuka.X.EditorLocalizationIntegration.asmdef` push file paths past the Unity Asset Store 150-character limit (Submission Guidelines 2.1.e, measured including `.meta`). Existing integrations were renamed accordingly in 2026-07.
- **Packages that also ship a Runtime assembly** (e.g. baking/runtime APIs alongside editor tooling): place the
  bridge seam and every localized string lookup in the **Editor assembly only**. `{{MAIN_ASMDEF}}` in the token
  table means the *editor* asmdef (e.g. `Kajitaharuka.XxxExtension.Editor`), never the Runtime asmdef. The Runtime
  assembly must not reference the seam, the integration assembly, or the base package — runtime user-facing text
  is out of scope for editor localization. If a string is shown both at runtime and in the editor, localize only
  the editor-side presentation.
- **Multi-assembly / suite packages** (core + companion editor asmdefs sharing one product): keep **one scope**
  and place the seam in the **lowest-level editor asmdef that every consumer references** (e.g. `Core`). Then
  choose how companions reach the facade: same-package consumer asmdefs → keep the facade `internal` and add them
  to the facade's `InternalsVisibleTo`; a companion in a *different package* cannot receive `InternalsVisibleTo`
  practically → make the facade (`{{PREFIX}}L10n`, `{{PREFIX}}TextKey`) `public` while keeping the bridge seam
  types internal. Proven in TextureAssetExtension (IVT across suite editors) and UberMaterialPropertyDrawer
  (public facade for a separate companion package), 2026-07.
- **Choosing `defaultLocale`**: match the language of the *current* hard-coded UI strings so the no-base display
  stays byte-identical after routing. Existing UI in Japanese → `ja` (EPE). Existing UI in English → `en` and add
  `ja` as a translation (UMPD). Do not translate strings while routing them.
- **Seam-unusable zones**: code that only runs when the seam itself may be absent — e.g. an InstallGuard assembly
  compiled when the *core* package is missing — must keep plain string literals. Never route such strings through
  the facade.
- **The catalog-coverage EditMode test ships with the seam, not after it.** A missing key does not throw — it
  renders the key name and drops the format arguments, so nothing else catches it. Details, prerequisites, and
  the handling for packages that cannot host the test: 「翻訳キーの網羅を機械で見る EditMode テストを必ず置く」
  below.

## 翻訳キーの網羅を機械で見る EditMode テストを必ず置く（2026-08-14 制定）

`Tr` はキーが未登録のとき **キー文字列をそのまま返し、しかも書式引数を捨てる**。コード側だけが先に進んだ
状態で起きるのは「訳が出ない」ではなく、**表示がキー名に化け、埋め込むはずだった値ごと消える**ことである。
例外は出ないので、機能テストも Inspector の目視も緑のまま通り抜ける。

実測（2026-08-14 / UberMaterialPropertyDrawer）: `UmpdTextKey.cs` へ 13 キーが足されたのに 19 ロケールの
JSON が 1 つも更新されないまま、リリース直前まで進んだ。`Rendering (Mixed)` と出ていたグループ見出しが
`group.header.mixed` に化け、**グループ名が丸ごと消えた**。`Tr` へ寄せる前のハードコード英語より確実に悪い。
コードとカタログを別の担当が並行して触るときに、この型が起きやすい。

seam の形（`{{PREFIX}}TextKey` + `DefaultEditorL10nBridge`）は消費側パッケージのどれでも同じなので、
**scaffold の時点で入れてしまうのが最も確実**である。後から各リポジトリへ配って回ると必ず取りこぼす。

参照実装:
`UberMaterialPropertyDrawer/Packages/com.kajitaharuka.uber-material-property-drawer/Tests/Editor/UmpdTextKeyCatalogCoverageTests.cs`
（テンプレートはこれを一般化したもの。テンプレート側は internal な TextKey も拾えるよう
`BindingFlags.NonPublic` を足し、名前空間・クラス名・scope をトークン化してある）

### 設計の要点（勝手に変えないこと）

- **判定には出荷コードのブリッジ（`DefaultEditorL10nBridge`）をそのまま使う。** カタログの探索と解釈を
  テストへ書き写すと二重管理になり、**出荷側の探索がずれてもテストだけが通る**。ブリッジ経由なら、
  多言語基盤の有無や現在の表示言語にも結果が左右されない（連携ブリッジが入っていても結果が揺れない）。
- **土台テストを別に持つ。**「カタログに無いキーはキー文字列がそのまま返る」ことを固定する。ここが変わると
  網羅テストは何も検出せず静かに緑になるので、先に落ちてもらう必要がある。
- **列挙が 0 件なら落とす。** `Assert.That(declaredKeys, Is.Not.Empty, ...)` が無いと、リフレクションの失敗が
  「ループが 1 度も回らないまま緑」になる。
- **翻訳キーを命名規則で絞り込まない。** 接頭辞などで除外すると、規則から外れた名前を付けた瞬間に本物の
  キーが静かに検査から抜ける（いま塞ぎたい穴と同じ形）。`{{PREFIX}}TextKey` の責務は翻訳キーの集約だけ
  なので、scope 名のような翻訳キーでない定数が紛れ込んだら**それ自体を失敗として検出する**のが正しい。
  検査側で除外するのではなく、その定数を `{{PREFIX}}L10n` などへ移す。
- **defaultLocale のテーブルしか見ない。**「defaultLocale には在るが他のロケールに無い」半分は、ロケール間の
  key 集合一致を見る検査（`Tools > UnityEditorLocalization > Validate Catalogs` と
  `editor-localization-translation-quality` スキルの `scripts/validate_locale_quality.py`）の担当。
  あえて重ねない — 全ロケールの JSON をテストから直接読み始めると、上と同じ二重管理になる。

事故そのものの説明とカタログ側の運用（キー追加は `insert_catalog_keys.py` で全ロケール一括、コードと
カタログは同一コミット）は `editor-localization-translation-quality` スキルが正本。ここでは重複させない。

### 置く前に満たすべき前提（欠けていれば先に直す）

1. **`{{PREFIX}}TextKey` クラスがあること。** 呼び出し側が文字列リテラルを直書きしていると、列挙する対象が
   無くこのテストは成立しない。実測 2026-08-14: UnityEditorWindowCaptureExtension は TextKey クラスを持たず、
   `CaptureL10n.Tr("...")` のリテラル呼び出しが 94 箇所ある。この場合は **先に TextKey クラスへ集約する**
   （集約自体が「キー名の打ち間違いをコンパイルエラーにする」という別の利得を持つ）。集約しないと決めた
   なら、このテストは置かずカタログ側の検査だけに頼ることになると明記して残すこと。
2. **EditMode テストの asmdef があること**（Editor 専用・本体 asmdef を参照・`defineConstraints` に
   `UNITY_INCLUDE_TESTS`）。無ければ先に作る。
3. **本体 assembly からテスト assembly へ `InternalsVisibleTo` が通っていること。**
   `DefaultEditorL10nBridge` と（多くのパッケージでは）`{{PREFIX}}L10n` / `{{PREFIX}}TextKey` は internal
   なので、`[assembly: InternalsVisibleTo("{{TESTS_ASMDEF}}")]` が要る。
   実測 2026-08-14: ExportPackageExtension / UnityEditorWindowCaptureExtension / UberMaterialPropertyDrawer は
   宣言済み。**TextureAssetExtension は基盤 Editor assembly に `Kajitaharuka.TextureAssetExtension.Tests` 向けの
   宣言が無い**（`Editor/Localization/TaeL10nAssemblyInfo.cs` にあるのは連携 assembly と Curve/Gradient 向けのみ）
   ため、テストを置く前に 1 行足す必要がある。
4. **scope が 1 つで、キーが 1 つの TextKey クラスへ集約されていること。** suite / companion 構成でも、この
   スキルの規則どおり scope を 1 つへ集約していればキーも 1 クラスへ集まるので、テスト 1 本で全パッケージ分を
   覆える（実測 2026-08-14 / UMPD: companion の GeneratedTexture 系キーも `UmpdTextKey` と core のカタログに
   入るため、core の Tests に置いた 1 本が両パッケージを覆っている）。assembly ごとに TextKey クラスを分けて
   いる場合のみ、クラスの数だけテストを増やす。
5. **このスキルの seam を使っていること。** 基盤パッケージ（UnityEditorLocalization）自身は消費側ではなく
   `EditorL10n.Tr(scope, key)` を直接呼ぶため、この seam ベースのテストは当てはまらない。基盤自身のカタログは
   `Validate Catalogs` と `validate_locale_quality.py` が受け持つ。

テストクラス名・テストメソッド名は日本語／英語のどちらでもよく、そのリポジトリの既存テストの流儀に合わせる
（実測 2026-08-14: UEWCE と UMPD は日本語のテストメソッド名、EPE と TAE は英語）。テンプレートは日本語の
メソッド名で書いてあるので、英語で揃えているリポジトリでは改名すること。アサーションとコメントが本体である。

## Verify

- **Base absent**: main assembly compiles; UI shows `defaultLocale` text; no locale switcher.
- **Base present**: the integration assembly compiles (no red mark on Define Constraints; `Version Defines` `Expression outcome` is not `Invalid`); the locale switcher appears and the UI follows locale changes; `Tools > UnityEditorLocalization > Validate Catalogs` passes.
- **Catalog coverage**: `Window > General > Test Runner` の `EditMode` で `{{TESTS_ASMDEF}}` を実行し、
  `{{PREFIX}}TextKeyCatalogCoverageTests` の 2 件が通ること。**土台テストが単独で緑になることも確かめる**
  （網羅テストだけを見ていると、土台が壊れたときに静かに緑へ変わったことに気付けない）。
  導入直後に網羅テストが落ちるなら、それは既存のカタログ取りこぼしを拾ったということなので、
  `editor-localization-translation-quality` スキルの `scripts/insert_catalog_keys.py` で全ロケールへ一括投入する。
- If the switcher does not appear, check that the base package version satisfies `{{MIN_VERSION}}` (a stale git cache can resolve an older version below the minimum).
