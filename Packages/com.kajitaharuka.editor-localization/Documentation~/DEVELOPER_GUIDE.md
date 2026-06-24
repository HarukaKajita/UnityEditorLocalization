# Editor Localization 開発者ガイド

このガイドは、Editor Localizationを使って他のUnity Editor拡張を多言語化するための設計指針です。

実装前に動作イメージを確認したい場合は、Package Managerから`Localized Editor Window`サンプルを
importしてください。`Tools > Editor Localization > Samples > Localized Window`で、言語切り替え、
tooltip、placeholder、Validator、翻訳品質workflowの導線を1つのEditorWindow上で確認できます。

## scope

scopeは翻訳カタログの名前空間です。Unity package nameと同じ文字列を推奨します。

```text
com.example.my-editor-extension
```

scopeを分けると、利用者は拡張ごとに表示言語を選べます。
利用者が全拡張をまとめて切り替えたい場合は、`Preferences > Editor Localization`のグローバル設定を使えます。
解決優先順位は scope 個別設定、グローバル設定、scope の`defaultLocale`の順です。Preferences で「グローバル設定に従う」を選ぶと、scope 個別設定を解除できます。

## key命名

keyは機能領域から始め、UI上の役割が分かる名前にします。

```text
common.locale.label
common.status.ready
export.path.mode.label
zipPacker.preview.empty
zipPacker.error.duplicateEntry
```

文言そのものや表示順をkeyに含めないでください。表示文言を変えてもkeyが変わらないようにします。

## 開発時診断

scope名やkeyのtypoを探すときは、`Preferences > Editor Localization`の「開発時診断」から未解決警告をONにできます。既定はOFFです。

ONの間は、`EditorL10n.Tr` / `TryTranslate`が未知scopeまたは未解決keyを検出したときに、同一scope/keyにつき一度だけConsoleへ警告します。`Tools > Editor Localization > Reload Catalogs`または`EditorL10n.Reload()`でカタログを再読み込みすると、警告済みの記録もリセットされます。

この診断は開発中のtypo検出を助けるためのものです。ON/OFFに関係なく、未解決時にkeyを返す耐障害方針は変わりません。

## 翻訳対象

翻訳する対象:

- Inspectorのセクション名、フィールドラベル、ボタン、tooltip
- HelpBox、ステータスバッジ、Foldout、Dropdownの表示名
- Consoleログ、検証エラー、進捗バー
- 取り消し不可操作や破壊的操作の警告

翻訳しない対象:

- クラス名、package名、asmdef名
- ファイルパス、アセット名、ユーザー入力値
- 拡張子や形式名そのもの（`.zip`、`.tgz`など）
- Unity API名、JSONファイル名、placeholder名

## placeholder

可変値は`string.Format`形式の番号placeholderを使います。

```json
{
  "key": "assetList.count",
  "value": "エントリ {0} / 依存 {1}"
}
```

placeholder番号は`0`から連続させ、すべてのロケールで番号集合を一致させてください。順序は言語ごとに変えて構いません。

```json
{
  "key": "assetList.count",
  "value": "Dependencies {1} / Entries {0}"
}
```

## locale追加の運用

ロケールはmanifestへ追加します。C#のenumやswitchは使わないでください。

```json
{
  "tag": "fr",
  "nativeName": "Français",
  "englishName": "French",
  "tablePath": "Locales/fr.json"
}
```

追加後は`Tools > Editor Localization > Validate Catalogs`を実行します。

## 汎用言語選択コンポーネント

Inspectorヘッダーやツールバーへ言語切り替えUIを置く場合は、`EditorL10nUi.CreateLocalizedCompactLocaleMenu`を使います。`A/文 日本語 ▾`のように、言語を読めなくても用途を推測しやすい視覚記号とネイティブ名を表示し、メニュー項目には`日本語 (ja)`のようにlocale tagを含めます。
このコンポーネントは特定 scope の個別設定を変更します。全 scope 共通のグローバル設定は、Preferences の導線または`EditorL10n.SetGlobalLocale`で扱います。

```csharp
using Kajitaharuka.EditorLocalization;
using UnityEngine.UIElements;

const string Scope = "com.example.my-editor-extension";

var headerActions = new VisualElement();
headerActions.AddToClassList("my-header__actions");

var localeMenu = EditorL10nUi.CreateLocalizedCompactLocaleMenu(Scope, "common.locale.label");
localeMenu.AddToClassList("my-locale-menu");
headerActions.Add(localeMenu);
```

`common.locale.label`は、tooltipで`表示言語: 日本語 (ja)`のように使われるラベルです。各ロケールの翻訳テーブルへ追加してください。

```json
{
  "key": "common.locale.label",
  "value": "表示言語"
}
```

見た目は利用側拡張で調整します。共通コンポーネントには`editor-l10n-compact-locale-menu` USS classが付くため、独自classを足さない場合でも最小限のスタイルを当てられます。

```css
.my-locale-menu {
    background-color: rgba(0, 0, 0, 0);
    border-width: 1px;
    border-radius: 4px;
    font-size: 10px;
    height: 20px;
    max-width: 152px;
    min-width: 68px;
}
```

設定フォームの1項目として言語を選ばせたい場合は、ラベル付きの`EditorL10nUi.CreateLocalizedLocaleDropdown`を使います。ヘッダーやツールバーの常設導線にはコンパクトメニュー、設定セクション内の明示的な項目にはDropdown、という使い分けを推奨します。

## Consoleログ

GUIとConsoleログで同じ意味の文言を出す場合でも、必要に応じてkeyは分けます。GUI向けには短く、ログ向けには原因や対象パスを含めた説明にできます。

```text
common.error.invalidPath
common.log.invalidPath
```

## fallback前提

defaultLocaleは、開発チームが常に内容を確認できる言語にしてください。defaultLocaleにないkeyは他ロケールにも要求されません。

## レビュー観点

- keyが機能名と役割を表しているか
- 文言にユーザー入力値を直接埋め込まずplaceholderを使っているか
- 取り消し不可操作の警告が全ロケールにあるか
- 長い翻訳でInspectorの横幅を壊さないか
- 言語変更時に開いているInspectorが更新されるか

## 任意依存（optional）として組み込む

販売パッケージなど、Editor Localizationを「入っていれば多言語化、無ければ単一言語」で組み込みたい場合は、ハード依存を避ける2アセンブリ方式を使います。本体assemblyは基盤を参照せず、ブリッジseam越しに文言取得とロケール追従を行い、基盤がある時だけコンパイルされる連携assemblyが多言語実装へ差し替えます。これにより、利用者が基盤を入れても入れなくてもコンパイル・表示が壊れません。

- 設計・命名規約・version defineの注意・チェックリスト: `OPTIONAL_INTEGRATION.md`
- 雛形生成: `editor-localization-optional-integration`スキル（本体seam＋連携assemblyをscaffold、テンプレート同梱）
- リファレンス実装: ExportPackageExtension
