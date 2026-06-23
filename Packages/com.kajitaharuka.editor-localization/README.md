# Editor Localization

Unity Editor拡張向けの軽量な多言語化基盤です。Editor上のInspector、HelpBox、Button、Consoleログ、進捗表示などの文言を、scopeごとの翻訳カタログから取得します。

## 特徴

- Editor専用です。ランタイム、Addressables、Unity Localization packageには依存しません。
- ロケールはC#のenumではなくmanifestの文字列タグで扱います。
- 新しいロケールはJSONファイルを追加するだけで増やせます。
- 表示言語はユーザーごとの`EditorPrefs`に保存し、全 scope 共通のグローバル設定と scope 個別設定を使い分けられます。
- UI Toolkit用のラベル、ボタン、PropertyField、言語選択Dropdown、コンパクトな言語選択メニューの補助APIを含みます。
- manifestや翻訳テーブルJSONの変更を検知し、カタログを自動リロードします。
- 欠落キーと`string.Format` placeholderの不一致を検証できます。

## パッケージ構成

このパッケージはEmbedded UPM packageとして、Unityプロジェクトの
`Packages/com.kajitaharuka.editor-localization/`配下に配置する構成です。
`README.md`、`CHANGELOG.md`、`package.json`はpackage rootに置き、補助資料は
`Documentation~/`配下に置いてUnityのAsset import対象から外します。

現時点でサンプルアセットは同梱していません。サンプルを追加する場合は、UPMの慣例に従って
`Samples~/`配下へ配置します。

このリポジトリでpackage内のEditModeテストを実行するため、host projectの`Packages/manifest.json`には
`testables`として`com.kajitaharuka.editor-localization`を登録しています。

## 最小構成

利用側パッケージにmanifestと翻訳テーブルを置きます。

```text
Assets/MyEditorExtension/Editor/Localization/
  my-extension.l10n-manifest.json
  Locales/
    ja.json
    en.json
```

manifestの例:

```json
{
  "scope": "com.example.my-editor-extension",
  "defaultLocale": "ja",
  "locales": [
    {
      "tag": "ja",
      "nativeName": "日本語",
      "englishName": "Japanese",
      "tablePath": "Locales/ja.json"
    },
    {
      "tag": "en",
      "nativeName": "English",
      "englishName": "English",
      "tablePath": "Locales/en.json"
    }
  ]
}
```

翻訳テーブルの例:

```json
{
  "locale": "ja",
  "entries": [
    {
      "key": "common.locale.label",
      "value": "表示言語"
    },
    {
      "key": "sample.count",
      "value": "対象: {0}件"
    }
  ]
}
```

## 基本API

```csharp
using Kajitaharuka.EditorLocalization;

var text = EditorL10n.Tr("com.example.my-editor-extension", "sample.count", 3);
```

全 scope 共通の表示言語はグローバル設定として扱えます。空文字を指定すると未設定に戻り、scope ごとの既定言語が使われます。

```csharp
EditorL10n.SetGlobalLocale("en");
var globalLocale = EditorL10n.GetGlobalLocale();

if (EditorL10n.TryGetScopeInfo("com.example.my-editor-extension", out var scopeInfo))
    Debug.Log($"{scopeInfo.Scope}: defaultLocale={scopeInfo.DefaultLocale}, manifest={scopeInfo.ManifestPath}");
```

UI Toolkitでは次のように使います。

```csharp
var label = new Label();
EditorL10nUi.BindText(label, Scope, "sample.count", 3);

var button = new Button(OnClick);
EditorL10nUi.BindButton(button, Scope, "sample.run", "sample.run.tooltip");

root.Add(EditorL10nUi.CreateLocalizedLocaleDropdown(Scope, "common.locale.label"));

var localeMenu = EditorL10nUi.CreateLocalizedCompactLocaleMenu(Scope, "common.locale.label");
toolbar.Add(localeMenu);
```

`CreateLocalizedCompactLocaleMenu`は、`A/文 日本語 ▾`のような短い表示と、`日本語 (ja)`形式のメニュー項目を持つ汎用の言語選択ボタンです。Inspectorヘッダーやツールバーの右上など、常時置いておきたい小さな導線に向いています。フォーム行としてラベル付きで配置したい場合は`CreateLocalizedLocaleDropdown`を使います。

コンパクトメニューには`editor-l10n-compact-locale-menu` USS classが付きます。利用側拡張で独自classを追加して、周囲のInspectorやツールバーに合わせて余白、色、最大幅を調整してください。

## ロケールの追加

1. `Locales/{locale}.json`を追加します。
2. manifestの`locales`へ`tag`、表示名、`tablePath`を追加します。
3. Unity Editorがmanifestと翻訳テーブルJSONをインポートすると、カタログは自動でリロードされます。
4. `Tools > Editor Localization > Validate Catalogs`を実行します。

C#コードの変更は不要です。必要に応じて`Tools > Editor Localization > Reload Catalogs`から手動で再読み込みすることもできます。

## fallback

表示言語は次の優先順位で決まります。

```text
scope 個別設定 -> グローバル設定 -> scope の defaultLocale
```

そのうえで、文言は次の順で探索します。

```text
選択ロケール -> 親ロケール -> defaultLocale -> key
```

グローバル設定は`Preferences > Editor Localization`で変更できます。scope 個別設定がある場合は、その scope では個別設定がグローバル設定より優先されます。scope 個別設定は、Preferences で「グローバル設定に従う」を選ぶと解除できます。Preferences では scope 文字列で検索でき、各 scope の現在の解決ロケール、`defaultLocale`、manifest パスを確認できます。

例:

- `es-419` -> `es` -> `ja` -> key
- `zh-Hant` -> `zh` -> `ja` -> key
- `pt-BR` -> `pt` -> `ja` -> key

## 検証

メニューから実行します。

```text
Tools > Editor Localization > Validate Catalogs
```

検証では次を確認します。

- `defaultLocale`のテーブルが存在すること
- defaultLocaleにあるkeyが各ロケールにも存在すること
- `string.Format`形式のplaceholder番号が一致すること
- placeholder番号が`0`から連続していること
- defaultLocaleと同一の値が残っていないか
- defaultLocaleにない余分なkeyがないか

## 関連資料

- `Documentation~/DEVELOPER_GUIDE.md`: 利用側拡張での設計指針
- `Documentation~/UI_TOOLKIT_LOCALIZATION_TIPS.md`: UI Toolkitで言語変更に追従するための実装Tips
- `skills/editor-localization-translation-quality/`: 翻訳品質ワークフロー
