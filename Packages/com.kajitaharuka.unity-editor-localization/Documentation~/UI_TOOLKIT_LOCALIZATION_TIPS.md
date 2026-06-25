# UI Toolkit 多言語化Tips

Unity Editor拡張の多言語化では、UI Toolkitで表示文言を管理すると、表示言語の変更へ追従しやすくなります。

## TooltipAttributeを主表示に使わない

`[Tooltip]`はSerializedPropertyに付いた静的な属性です。言語変更のたびに文言を差し替える用途には向きません。

多言語化したInspectorでは、UI Toolkitで生成した要素にtooltipを設定してください。

```csharp
var field = new PropertyField(property);
EditorL10nUi.BindPropertyField(field, Scope, "path.label", "path.tooltip");
root.Add(field);
```

既存コードに`[Tooltip]`がある場合は、後方互換の補足として残すか、localized Inspector側のtooltipへ役割を移してください。

## PropertyFieldのラベルを後から差し替える

`PropertyField`は内部要素を遅れて生成します。生成直後にlabel要素が見つからない場合があります。

`EditorL10nUi.BindPropertyField`は`GeometryChangedEvent`後にlabelを再適用し、ロケール変更時にも更新します。

配列やリストの`PropertyField`は`Foldout`で描画され、そのタイトルは`BaseField`のラベル(`labelUssClassName`)ではありません。`BindPropertyField`は`Foldout`を検出した場合に`Foldout.text`を更新します（スカラー時のみ`BaseField`のラベルを更新し、配列の要素ラベルを誤って書き換えません）。自前でラベルを差し替える場合も、対象が配列フィールドなら`Foldout.text`を更新してください。

## Label、Button、Foldoutはイベントで更新する

開いているInspectorを言語変更に追従させるには、`EditorL10nUi.RegisterLocaleCallback`で`EditorL10n.LocaleChanged`へ追従させます。このhelperは要素がpanelにattachされている間だけ購読し、attach時にも現在ロケールの表示へ再適用し、`DetachFromPanelEvent`で解除します。一度もrootへ追加しない要素は購読しません。

```csharp
void Apply()
{
    foldout.text = EditorL10n.Tr(Scope, "assetList.title");
}

Apply();
EditorL10nUi.RegisterLocaleCallback(foldout, Apply);
```

## 長い翻訳への備え

ドイツ語、ロシア語、タイ語、ベトナム語などでは、同じ意味でも文字列が長くなる場合があります。

UI Toolkitでは次を意識してください。

- `white-space: normal`で折り返しを許可する
- 値表示には`flex-shrink: 1`を設定する
- 横方向に縮む子要素には`min-width: 0`を設定する
- ボタンを横詰めしすぎず、必要なら縦積みにする
- 長いファイルパスにはゼロ幅スペースなどで折り返し機会を作る

## 言語選択UI

多数の言語を扱う場合は、セグメントトグルではなくDropdownまたはコンパクトメニューを使います。

Inspectorヘッダーやツールバーの右上に常設する場合は、コンパクトメニューを使います。表示は`A/文 日本語 ▾`のように短く、メニュー項目には`日本語 (ja)`のようにlocale tagが入ります。

```csharp
var localeMenu = EditorL10nUi.CreateLocalizedCompactLocaleMenu(Scope, "common.locale.label");
localeMenu.AddToClassList("my-locale-menu");
headerActions.Add(localeMenu);
```

設定セクションの1項目として明示的に置く場合は、ラベル付きDropdownを使います。

```csharp
root.Add(EditorL10nUi.CreateLocalizedLocaleDropdown(Scope, "common.locale.label"));
```

ロケール候補はmanifestから読み込まれるため、後から言語を追加してもInspector側のC#を変更する必要はありません。

## C# only UI ToolkitとUXML

C# only UI Toolkitでは、生成時に`EditorL10nUi`でbindします。

UXMLを使う場合は、`name`やclassで対象要素を取得し、C#側でbindしてください。UXML内の固定文字列は、言語変更に追従させたい文言には使わないことを推奨します。

```csharp
var title = root.Q<Label>("Title");
EditorL10nUi.BindText(title, Scope, "window.title");
```
