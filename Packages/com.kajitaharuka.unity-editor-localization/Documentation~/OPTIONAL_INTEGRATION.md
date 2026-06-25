# UnityEditorLocalization 任意依存（Optional Integration）ガイド

このガイドは、販売パッケージなど他の Unity Editor 拡張が **UnityEditorLocalization を「任意の依存」として組み込む**ための設計指針です。基盤（UnityEditorLocalization）が無くても**単一言語（defaultLocale）で成立**し、基盤を追加すると**多言語表示と言語切替 UI が点灯**する構成を、C# のハード依存なしで実現します。

利用者（購入者）が「基盤を入れる／入れない」のどちらでも、コンパイルも表示も壊れないようにするのが目的です。リファレンス実装は ExportPackageExtension（`Kajitaharuka.ExportPackageExtension`）です。スキャフォールドには `editor-localization-optional-integration` スキルを使えます。

## 基本方針

- 本体 assembly は UnityEditorLocalization を**参照しない**。
- 連携コードを**別 assembly** に分け、Unity の Version Define + Define Constraint で「基盤がある時だけコンパイル」する。基盤が無ければその assembly ごとコンパイル対象から除外され、**参照エラーにならない**（エラーではなく非コンパイル）。
- 本体は「ブリッジ（seam）」越しに文言取得とロケール追従を行う。既定は基盤非依存の単一言語実装、基盤がある時は連携 assembly が基盤連携実装へ差し替える。

この方式により、`.unitypackage` / UPM tarball(.tgz) / VCC・ALCOM ローカル登録のいずれの配布形態でも、また導入順序に関わらず、挙動が一貫します。

## 全体像

```text
本体 assembly  <Product>                         連携 assembly  <Product>.EditorLocalizationIntegration
  ├ IEditorL10nBridge (抽象)        ←┐            ├ asmdef: 本体 + Kajitaharuka.EditorLocalization 参照
  ├ EditorL10nRuntime (差し替え点)   │InternalsVisibleTo  │        versionDefines / defineConstraints
  ├ DefaultEditorL10nBridge (既定)  ─┘            ├ EditorL10nBridge      → EditorL10n / EditorL10nUi へ委譲
  ├ <Prefix>L10n   (文言ファサード)               └ EditorL10nBridgeInstaller ([InitializeOnLoadMethod] で登録)
  └ <Prefix>L10nUi (UI バインドファサード)                       ▲
        ↑ 本体は基盤を参照しない                                 └ KAJITAHARUKA_EDITOR_L10N が定義された時だけコンパイル
```

- 基盤**なし**: シンボル未定義 → 連携 assembly は非コンパイル → `EditorL10nRuntime` は既定の `DefaultEditorL10nBridge` のまま → defaultLocale で単一言語表示。
- 基盤**あり**: シンボル定義 → 連携 assembly がコンパイル → `Installer` が基盤連携ブリッジへ差し替え → 多言語表示・言語切替 UI・ロケール変更追従。

## ファイル構成

本体 assembly `<Product>`（配置: `<package>/Editor/Localization/`）

| ファイル | 役割 |
| --- | --- |
| `IEditorL10nBridge` | 抽象。`Tr` / `RegisterLocaleCallback` / `CreateCompactLocaleMenu` の3点のみ |
| `EditorL10nRuntime` | 使用中ブリッジの保持と差し替え点（既定 = `DefaultEditorL10nBridge`） |
| `DefaultEditorL10nBridge` | 基盤なし時。defaultLocale テーブルのみ直接読み、単一言語で文言を返す |
| `<Prefix>L10n` | 翻訳 scope を隠す文言ファサード（`Tr`） |
| `<Prefix>L10nUi` | UI バインドのファサード（`BindText`/`BindButton`/`BindPropertyField`/`RegisterLocaleCallback`/`CreateCompactLocaleMenu`） |
| `<Prefix>L10nAssemblyInfo` | 連携 assembly への `InternalsVisibleTo` |

連携 assembly `<Product>.EditorLocalizationIntegration`（配置: `<package>/Editor/LocalizationIntegration/`）

| ファイル | 役割 |
| --- | --- |
| `*.asmdef` | 本体 + `Kajitaharuka.EditorLocalization` 参照、`versionDefines`、`defineConstraints` |
| `EditorL10nBridge` | `EditorL10n` / `EditorL10nUi` へ委譲する基盤連携ブリッジ |
| `EditorL10nBridgeInstaller` | `[InitializeOnLoadMethod]` でブリッジを登録 |

> 本体コードからは `<Prefix>L10n.Tr(...)` と `<Prefix>L10nUi.*(...)` だけを呼びます。`EditorL10n` / `EditorL10nUi`（基盤 API）を本体 assembly から直接呼ばないこと。

## 命名・配置規約

- 連携 assembly 名: `<本体 assembly 名>.EditorLocalizationIntegration`
- 共有シンボル: **`KAJITAHARUKA_EDITOR_L10N`**（全パッケージで同一文字列にする）
- 配置: 本体 seam = `Editor/Localization/`、連携 = `Editor/LocalizationIntegration/`（連携フォルダに asmdef を置きサブツリーを分離）
- 文言ファサードの `Scope` 定数 = 利用側の l10n scope（`*.l10n-manifest.json` の `scope` と一致させる）

## 重要な注意点

1. **version define の `expression` は裸のバージョン**（例 `"1.0.0"`）を使う。区間記法 `[1.0.0,)` は Unity でパースできず `ExpressionNotValidException` になる。裸バージョンは「そのバージョン以上」を意味する。式が無効、または基盤の version が式を満たさないと、**連携 assembly が黙って非コンパイルになり多言語化が効かない**（Define Constraints に赤マークが残る）。
2. `expression` は「連携が必要とする API を備えた最低バージョン」を指定する。基盤に破壊的変更を入れたらここを更新する。
3. `InternalsVisibleTo` で本体の `IEditorL10nBridge` / `EditorL10nRuntime`（`internal`）を連携 assembly へ公開する。
4. **標準時（基盤なし）の文言データ源は JSON テーブル一本に保つ**。`DefaultEditorL10nBridge` は defaultLocale テーブルのみを直接読む（fallback 連鎖・ロケール解決・`EditorPrefs` は持たない）。読み込み失敗時はキャッシュせず次回再試行する（AssetDatabase 準備前の一時失敗で key が生表示され続けるのを防ぐため）。
5. `CreateCompactLocaleMenu` は標準時に **null** を返す（単一言語なので言語切替 UI を出さない）。呼び出し側は null 安全に扱う。

## 導入チェックリスト

1. 本体 assembly の asmdef から `Kajitaharuka.EditorLocalization` 参照を**外す**。
2. 本体 `Editor/Localization/` に seam 6 ファイルを置く（`<Prefix>` と namespace を差し替え）。
3. `<Prefix>L10nAssemblyInfo` の `InternalsVisibleTo` を連携 assembly 名に合わせる。
4. 本体 UI から基盤 API（`EditorL10nUi.*` / `EditorL10n.*`）を直接呼ばず、`<Prefix>L10nUi.*` / `<Prefix>L10n.Tr` 経由にする。
5. `Editor/LocalizationIntegration/` に連携 asmdef + 2 ファイルを置く。asmdef の `references` / `versionDefines.name`(=基盤パッケージ id) / `expression` / `define` / `defineConstraints` を設定。
6. 利用側の `*.l10n-manifest.json` と `Locales/*.json` を同梱する（defaultLocale のテーブルは必須）。
7. 本体 `package.json` の `dependencies` には基盤を**入れない**（任意依存のため）。README で「推奨アドオン」として案内する。

## 検証

- **基盤なし**: コンパイル通過・defaultLocale で表示・言語切替 UI が出ないこと。
- **基盤あり**: 言語切替 UI が点灯・切替が即追従すること。`Tools > UnityEditorLocalization > Validate Catalogs` が通ること。
- 連携 asmdef の Inspector で **Define Constraints の赤マークが消え**、`Version Defines` の `Expression outcome` が `Invalid` でないこと。

## トラブルシュート

- **言語切替 UI が出ない／Define Constraints に赤マークが残る**: ①`expression` が valid な裸バージョンか、②基盤パッケージの version が `expression` を満たすか（古い git キャッシュ等で下回っていないか）を確認する。dev プロジェクトで基盤をローカル参照する場合は `file:` 参照にするとローカルの version を確実に拾える。
- **すべて key が生表示される**: defaultLocale テーブルが見つかっていない（manifest / テーブル未 import、AssetDatabase 準備前）。`DefaultEditorL10nBridge` は失敗時にキャッシュしないため、import 完了後の再描画／再起動で回復する。
- **一部の文言だけ切替に追従しない**: その要素を `<Prefix>L10n.Tr` で一度だけ設定していないか確認する。切替追従が必要なら `<Prefix>L10nUi.Bind*` または `RegisterLocaleCallback` で束ねる。
