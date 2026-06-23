# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## このリポジトリの位置づけ

このリポジトリは Unity プロジェクトの体裁を取っていますが、開発対象は単一の Unity package
`com.kajitaharuka.editor-localization`（表示名: Editor Localization）です。実体はすべて
[Assets/EditorLocalization/](Assets/EditorLocalization/) 配下にあり、リポジトリ直下の
`Assets/` の他要素・`ProjectSettings/`・`Packages/`・各 `*.csproj` / `*.sln` は、この package を
Unity Editor 上で開発・検証するための器に過ぎません。実装・ドキュメントの編集は基本的に
`Assets/EditorLocalization/` 内で行います。

- Unity バージョン: 2022.3（`Assets/EditorLocalization/package.json` の `unity` を参照）
- 対象: **Editor 専用**。ランタイム、Addressables、Unity Localization package には依存しません。
- package 本体の asmdef は `Kajitaharuka.EditorLocalization`（`includePlatforms: ["Editor"]`、`references: []`）です。
  EditMode テストは `Kajitaharuka.EditorLocalization.Tests` asmdef で分離します。

## よく使う操作

このリポジトリには CLI ベースのビルド/テスト基盤はなく、検証は Unity Editor のメニューから行います。

- カタログ再読み込み: `Tools > Editor Localization > Reload Catalogs`
- カタログ検証: `Tools > Editor Localization > Validate Catalogs`
  - defaultLocale テーブルの存在、各ロケールでの key 過不足、`string.Format` placeholder 番号の一致を検査します。
- 表示言語の確認/変更（グローバルおよび scope ごと）: `Preferences > Editor Localization`
- 翻訳テキスト品質の静的検証（Python、利用側の locale 群に対して実行）:

  ```bash
  python3 Assets/EditorLocalization/skills/editor-localization-translation-quality/scripts/validate_locale_quality.py \
    <locales_dir> --default-locale <tag>
  ```

`Assets/EditorLocalization/Tests/Editor/` には EditMode テスト用の
`Kajitaharuka.EditorLocalization.Tests` asmdef があります。`EditorL10n` の fallback 連鎖や
`NormalizeLocaleTag` など `internal` ロジックは `InternalsVisibleTo` 経由で検証します。

## アーキテクチャ

文言は「scope（名前空間）× ロケールタグ × key」で引きます。中核の流れは次の通りです。

1. **カタログ探索とロード** — [Editor/Core/EditorL10nCatalog.cs](Assets/EditorLocalization/Editor/Core/EditorL10nCatalog.cs)
   - `AssetDatabase.FindAssets("l10n-manifest")` でプロジェクト内の `*.l10n-manifest.json` を全探索します。
     利用側拡張は manifest を置くだけで自動的にカタログへ登録され、この package の C# 変更は不要です。
   - manifest（`scope` / `defaultLocale` / `locales[]`）を読み、各 locale の `tablePath`（manifest からの相対パス）の
     JSON テーブルをロードして、scope 単位の `EditorL10nScopeCatalog`（`locale -> (key -> value)` の辞書）を構築します。
   - scope 重複、テーブルの `locale` と manifest の `tag` 不一致は warning を出して握りつぶします（実装堅牢性のため例外にしない方針）。

2. **公開 API とロケール解決** — [Editor/Core/EditorL10n.cs](Assets/EditorLocalization/Editor/Core/EditorL10n.cs)
   - `EditorL10n.Tr(scope, key, args...)` が主入口。`TryTranslate` → `string.Format` の順で処理し、未解決時は key 自体を返します（落とさない）。
   - 表示ロケールは scope 個別設定 → グローバル設定 → scope の defaultLocale の順で解決します。
   - **fallback 連鎖**: 解決済み表示ロケール → その親ロケール群 → defaultLocale → 親群、の順で最初に見つかった値を返します
     （例: `es-419` → `es` → `ja`）。親ロケールは `-` 区切りを末尾から削って導出します。
   - ロケールタグは `NormalizeLocaleTag` で正規化（言語小文字 / 地域大文字 / script は先頭大文字、`_`→`-`）します。
     ロケールは **enum ではなく文字列タグ** で扱う、というのが本 package の根幹方針です。新ロケール追加で C# を触らせないためです。
   - `GetGlobalLocale` / `SetGlobalLocale` で全 scope 共通の表示ロケールを取得・設定できます。
   - `LocaleChanged` イベントで UI 層がロケール変更に追従します。

3. **表示言語の永続化** — [Editor/Core/EditorL10nPreferences.cs](Assets/EditorLocalization/Editor/Core/EditorL10nPreferences.cs)
   - 選択言語は **`EditorPrefs`（ユーザーごと）** に保存し、プロジェクト資産には書き込みません。チーム内で言語の好みが衝突しないようにする設計です。
   - 解決優先度: scope 個別設定 → グローバル設定 → scope の defaultLocale。
   - scope 個別設定は空文字で削除し、Preferences では「グローバル設定に従う」で解除します。

4. **UI Toolkit バインド層** — [Editor/UI/EditorL10nUi.cs](Assets/EditorLocalization/Editor/UI/EditorL10nUi.cs)
   - `Label` / `Button` / `PropertyField` などを翻訳 key にバインドし、`LocaleChanged` を購読して言語変更に自動追従させます。
     購読解除は `DetachFromPanelEvent` で行う前提です。
   - 言語選択 UI は 2 種類: ヘッダー/ツールバー常設用の `CreateLocalizedCompactLocaleMenu`（`A/文 日本語 ▾` 風）と、
     設定フォーム行用の `CreateLocalizedLocaleDropdown`。候補は manifest から動的に読むため、言語追加で UI 側 C# の変更は不要です。

5. **検証** — [Editor/Validation/EditorL10nValidator.cs](Assets/EditorLocalization/Editor/Validation/EditorL10nValidator.cs)、
   **Preferences UI** — [Editor/Settings/EditorL10nSettingsProvider.cs](Assets/EditorLocalization/Editor/Settings/EditorL10nSettingsProvider.cs)、
   **JSON モデル** — [Editor/Core/EditorL10nJson.cs](Assets/EditorLocalization/Editor/Core/EditorL10nJson.cs)（`JsonUtility` 用の `[Serializable]` DTO 群）。

### 利用側データ構造（manifest + テーブル）

```text
Assets/<利用側拡張>/Editor/Localization/
  <name>.l10n-manifest.json   # scope, defaultLocale, locales[]（tag/nativeName/englishName/tablePath）
  Locales/
    ja.json                   # { "locale": "ja", "entries": [{ "key": ..., "value": ... }] }
    en.json
```

## 編集時の不変条件・方針

- **ロケールを enum やコード分岐で扱わない。** 言語の増減は manifest と JSON テーブルの追加だけで完結させ、`EditorL10n` を含む C# を改変しない設計を崩さないこと。
- **`Tr` は例外で落とさない。** 未解決 key、`string.Format` 失敗時は key やフォーマット前文字列を返す（`Debug.LogError` で通知）。この耐障害性を維持すること。
- **placeholder は番号 `{0}`/`{1}` 形式。** 全ロケールで番号集合を一致させる（順序は言語ごとに変えてよい）。Validator がこれを検査します。
- key は機能領域から始め役割が分かる名前にし、**表示文言や表示順を key に含めない**（[DEVELOPER_GUIDE.md](Assets/EditorLocalization/DEVELOPER_GUIDE.md) の命名規約に従う）。
- ソースは全ファイルが `#if UNITY_EDITOR ... #endif` で囲まれています。新規ファイルも同様に囲むこと。

## ドキュメント（変更時は実装との整合を保つ）

実装を変えたら、対応する以下のドキュメントの更新要否を必ず確認してください。

- [Assets/EditorLocalization/README.md](Assets/EditorLocalization/README.md): 利用者向け概要・最小構成・API・fallback・検証
- [Assets/EditorLocalization/DEVELOPER_GUIDE.md](Assets/EditorLocalization/DEVELOPER_GUIDE.md): 利用側拡張での scope/key 設計指針・レビュー観点
- [Assets/EditorLocalization/UI_TOOLKIT_LOCALIZATION_TIPS.md](Assets/EditorLocalization/UI_TOOLKIT_LOCALIZATION_TIPS.md): 言語変更に追従させる UI Toolkit 実装 Tips
- [Assets/EditorLocalization/CHANGELOG.md](Assets/EditorLocalization/CHANGELOG.md): リリースごとの変更（`package.json` の version と整合）
- [Assets/EditorLocalization/skills/editor-localization-translation-quality/](Assets/EditorLocalization/skills/editor-localization-translation-quality/): 翻訳品質ワークフロー（用語・スタイル・言語別注意・品質検証スクリプト）


## 備考
CLAUDE.mdとAGENTS.mdの内容は常に同じ内容になるように保つこと。
