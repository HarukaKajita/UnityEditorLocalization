<!-- unity-package-agent-guide: com.kajitaharuka.unity-editor-localization -->
# パッケージ利用ガイド（CLAUDE.md / AGENTS.md 共通）

このファイルは、このパッケージを**使う**コーディングエージェント向けの手引きです。
UnityEditorLocalization は、Unity Editor 拡張の UI 文言（Inspector・EditorWindow・HelpBox・Console ログ・進捗表示）を、JSON の翻訳カタログから引いて多言語表示するための Editor 専用基盤です。

> **注記**: `CLAUDE.md` と `AGENTS.md` は同一内容を保ちます。片方だけを更新しないでください。

## 1. 要約

| 項目 | 値 |
|---|---|
| パッケージ名 | `com.kajitaharuka.unity-editor-localization` |
| 名前空間 | `Kajitaharuka.EditorLocalization` |
| アセンブリ名 | `Kajitaharuka.EditorLocalization`（Editor 専用・`autoReferenced: true`） |
| 対応 Unity | 2022.3.0f1 以降 |
| 実行範囲 | Editor 専用（asmdef の `includePlatforms` は `Editor` のみ。全 `.cs` が `#if UNITY_EDITOR` で囲まれている） |
| 必須依存 | 無し（`package.json` に `dependencies` フィールドが存在しない） |
| 任意依存 | 無し |
| 主な入口 | `Tools > UnityEditorLocalization >` の 5 項目 ／ `Preferences > UnityEditorLocalization` ／ C# API `EditorL10n` `EditorL10nUi` |
| 成果物 | 利用者が作る JSON カタログ（manifest 1 つ＋ロケール別テーブル）。表示言語の選択は `EditorPrefs`（ユーザー・マシン単位） |

**このパッケージでできないこと**

- ランタイムの多言語化はできない。ゲーム内 UI・`MonoBehaviour`・ビルドに含まれるアセンブリから使える API は 1 つも無い。
- IMGUI 用のバインド API は無い。`OnInspectorGUI` 等では毎フレーム `EditorL10n.Tr(...)` を呼ぶ形になる。
- 翻訳文の自動生成はしない。訳文は利用者が JSON に書く。表示言語の選択もプロジェクト資産として共有できない（`EditorPrefs` 保存なので git に乗らない）。
- Unity Editor 本体の UI や、Unity 公式 Localization パッケージの資産には一切関与しない。

## 2. 適用範囲と前提

- **Editor 専用である。** ランタイムアセンブリ（`Assets/Scripts/` 直下の既定アセンブリ、`includePlatforms` を絞っていない自作 asmdef など）から参照するとプレイヤービルドが壊れる。使う側のスクリプトは `Editor/` フォルダ配下か、`includePlatforms: ["Editor"]` の asmdef に置く。
- **利用者が自作 asmdef を持つ場合は、その asmdef の `references` へ `Kajitaharuka.EditorLocalization` を追加する。** `autoReferenced: true` が効くのは「asmdef を持たない `Editor/` 配下のスクリプト（`Assembly-CSharp-Editor`）」だけで、asmdef を切った時点で明示参照が要る。
- **カタログは AssetDatabase から見える場所にしか置けない。** 探索は `AssetDatabase.FindAssets("l10n-manifest")` で、対象は `Assets/` 配下か `Packages/` 配下。`Samples~` のような `~` 付きフォルダの中身は（import されるまで）見つからない。
- **manifest のファイル名は `l10n-manifest` を含み、拡張子が `.json` であること。** 満たさないファイルは scope ごと存在しない扱いになる。推奨形は `<名前>.l10n-manifest.json`。
- ロケールテーブルの `tablePath` は、manifest が置かれたフォルダからの**相対パス**として解決される。
- **scope はプロジェクト内で一意にする。** 同じ scope の manifest が複数見つかると、アセットパスの昇順で最初の 1 つだけが採用され、残りは Console 警告のうえ無視される。
- 表示言語と開発時診断フラグは `EditorPrefs`（キー前置詞 `Kajitaharuka.EditorLocalization.`）に保存され、プロジェクト資産には書かれない。既定値と同じときはキーを削除する実装なので、キーの不在＝既定値である。
- **導入形態で同梱物が変わる。** `.tgz` / Git URL / VPM では `Samples~/` と `Documentation~/` が入るが、`.unitypackage` には入らない（Unity が `~` 付きフォルダを扱わないため）。`Editor/` 一式・19 ロケールのカタログ・`Tests/`・`skills/`・`README.md` / `CHANGELOG.md` はどちらにも入る。
- 同梱の EditMode テストは `defineConstraints: ["UNITY_INCLUDE_TESTS"]` を持つ。Test Runner に出すには、利用側 `Packages/manifest.json` の `testables` へ `com.kajitaharuka.unity-editor-localization` を追加する。
- ライセンスは MIT。クレジット表示の義務は無い（ただし言語メニューは既定でクレジット項目を出す。3 章・6 章を参照）。

## 3. 入口

**メニュー**（すべて `Tools/UnityEditorLocalization/` 直下。文字列は正確にこのとおり）

| メニュー | 何が起きるか |
|---|---|
| `Settings` | `Preferences/UnityEditorLocalization`（`SettingsScope.User`）を開く |
| `Create Catalog` | カタログ雛形の生成ウィザードを開く |
| `Validate Catalogs` | 全 scope を検証し Console へ出力する |
| `Reload Catalogs` | カタログを再読み込みする（診断の「1 回だけ警告」の記録もリセット） |
| `AI Agent Skills` | 同梱スキルの登録ペイン（Preferences 内）を開くだけ。押しただけでは何も登録されない |

**公開 C# API**（`using Kajitaharuka.EditorLocalization;`）。公開型は下記の 9 つで全部である。

`EditorL10n`（static）— 文言取得とロケール解決

| メンバー | 用途 |
|---|---|
| `string Tr(string scope, string key, params object[] args)` / `bool TryTranslate(string scope, string key, out string text)` | 文言取得。`Tr` は未解決なら key をそのまま返す（例外は投げない）。成否で分岐したいときは `TryTranslate` |
| `string GetActiveLocale(string scope)` / `void SetActiveLocale(string scope, string locale)` | 現在の表示ロケールタグの取得と、その scope の個別設定の書き換え |
| `string GetGlobalLocale()` / `void SetGlobalLocale(string locale)` / `string GetSystemLocale()` / `bool GetSystemLocaleFallbackEnabled()` / `void SetSystemLocaleFallbackEnabled(bool)` | 全 scope 共通の設定（空文字を渡すと未設定へ戻す）と、OS 言語の検出結果・そこへフォールバックするかの可否 |
| `IReadOnlyList<EditorL10nLocaleInfo> GetLocales(string scope)` / `IReadOnlyList<string> GetScopes()` / `bool TryGetScopeInfo(string scope, out EditorL10nScopeInfo info)` / `bool TryGetLocaleTablePath(string scope, string locale, out string tablePath)` | 自前の言語選択 UI や一覧表示の材料、scope の defaultLocale・manifest パス・テーブルパス |
| `void Reload()` / `string NormalizeLocaleTag(string locale)` / `event Action LocaleChanged` | カタログ再読込（`LocaleChanged` が発火）／タグ正規化（`ja_jp` → `ja-JP`）／言語変更イベント（static。手動購読なら解除必須） |

`EditorL10nUi`（static）— UI Toolkit へのバインド

| メンバー | 用途 |
|---|---|
| `BindText(Label, scope, key, params object[] args)` / `BindButton(Button, scope, textKey, tooltipKey = null, params object[] args)` | `Label` のテキスト、`Button` のテキストと tooltip |
| `BindPropertyField(PropertyField, scope, labelKey, tooltipKey = null)` | Inspector のラベルと tooltip |
| `CreateLocaleDropdown(scope, label)` / `CreateLocalizedLocaleDropdown(scope, labelKey)` | 設定フォーム向けの `DropdownField` |
| `CreateCompactLocaleMenu(scope, tooltipLabel = null, marker = "A/文", showAttribution = true)` / `CreateLocalizedCompactLocaleMenu(scope, tooltipLabelKey, marker = "A/文", showAttribution = true)` | ヘッダー・ツールバー向けの小さな言語メニュー |
| `CreateAttribution()` / `RegisterLocaleCallback(VisualElement, Action)` | 「Powered by UnityEditorLocalization」ボタン（任意）／バインド API が無い対象の再適用（要素の attach・detach に合わせて自動で購読・解除） |

残る 7 型は値・結果の受け皿。`EditorL10nLocaleInfo`（`Tag` / `NativeName` / `EnglishName` / `DisplayName`）、`EditorL10nScopeInfo`（`Scope` / `DefaultLocale` / `ManifestPath`）、`EditorL10nValidator`（`ValidateAll` / `ValidateAndLog` / `ValidateForCI` / `ComputeExitCode` / `ReloadCatalogs`）と結果型 `EditorL10nValidationResult`・`EditorL10nValidationIssue`・`EditorL10nValidationSeverity`・`EditorL10nValidationMessageKind`。

**存在しないもの**（探しに行かないこと）

- 設定用の ScriptableObject アセットは無い（`[CreateAssetMenu]` は 0 件）。`Project Settings` 側のページも無く、設定は `Preferences`（ユーザースコープ）のみ。
- ランタイム API は無い。IMGUI 専用のバインド補助も無い。
- UI 部品ファクトリ `EditorL10nUiKit`、カタログ実体 `EditorL10nCatalog`、`EditorL10nPreferences`、Preferences 実装、カタログ生成ウィザード、スキルインストーラは **internal** で、利用側からは呼べない。

## 4. 基本の使い方

1. カタログ（manifest 1 つ＋ロケール別テーブル）を用意する。GUI から作るなら `Tools > UnityEditorLocalization > Create Catalog`。ただし**出力フォルダは既存のフォルダアセットを `ObjectField` で選ぶ形式**（パス文字列は打ち込めない）なので、先にフォルダを作っておくこと。エージェント単独で進めるなら下記の最小例をそのまま手で書けばよい。scope は自分のパッケージ名を推奨（例 `com.example.my-editor-extension`）。
2. `Locales/<defaultLocale>.json` へ key と value を書く。可変値は `{0}` 形式の番号 placeholder を 0 から連番で使う。
3. 文言を使うスクリプトを `Editor/` 配下に置き（自作 asmdef があれば `references` へ `Kajitaharuka.EditorLocalization` を追加）、`EditorL10n.Tr(Scope, key)` で文言を引く。scope 文字列は manifest と同じ値を `const` で 1 か所に持つ。
4. UI Toolkit の要素は `Tr` の結果を一度代入するのではなく、`EditorL10nUi.BindText` / `BindButton` / `BindPropertyField` で束ねる。バインド API が無い対象（HelpBox・Foldout・ProgressBar・ウィンドウタイトル・自前 tooltip）は `EditorL10nUi.RegisterLocaleCallback(element, Apply)` で再適用する。
5. 言語切替 UI を置く。ヘッダー常設なら `CreateLocalizedCompactLocaleMenu(Scope, key)`、設定フォームの 1 行なら `CreateLocalizedLocaleDropdown(Scope, key)`。
6. `Validate Catalogs` を実行し、Console の Error が 0 になるまで直す。**成功の確認方法**: 言語切替 UI で言語を変えたとき、ウィンドウを開き直さずにその場で文言が変わること。

**カタログの最小例。** `Create Catalog` が生成するのも同じスキーマである。まず manifest（例 `Assets/MyExtension/Editor/Localization/my-extension.l10n-manifest.json`）。`fixedTerms` は省略可（5 章参照）。

```json
{
  "scope": "com.example.my-editor-extension",
  "defaultLocale": "ja",
  "fixedTerms": [],
  "locales": [
    { "tag": "ja", "nativeName": "日本語", "englishName": "Japanese", "tablePath": "Locales/ja.json" },
    { "tag": "en", "nativeName": "English", "englishName": "English", "tablePath": "Locales/en.json" }
  ]
}
```

次に翻訳テーブル（`.../Localization/Locales/ja.json`）。`en.json` も同じ形で、`locale` を `en` に、value を英訳にする。

```json
{
  "locale": "ja",
  "entries": [
    { "key": "window.title", "value": "マイツール" },
    { "key": "action.run", "value": "実行" },
    { "key": "locale.label", "value": "表示言語" },
    { "key": "log.done", "value": "{0} 件を処理しました" }
  ]
}
```

呼び出し側（`Editor/` 配下に置く）。

```csharp
using Kajitaharuka.EditorLocalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class MyToolWindow : EditorWindow
{
    // manifest の "scope" と完全に同じ文字列
    private const string Scope = "com.example.my-editor-extension";

    private void CreateGUI() // 言語切替はこの scope の個別設定を変える
    {
        var title = new Label();
        EditorL10nUi.BindText(title, Scope, "window.title");
        rootVisualElement.Add(title);

        var run = new Button(() => Debug.Log(EditorL10n.Tr(Scope, "log.done", 3)));
        EditorL10nUi.BindButton(run, Scope, "action.run");
        rootVisualElement.Add(run);
        rootVisualElement.Add(EditorL10nUi.CreateLocalizedCompactLocaleMenu(Scope, "locale.label"));
    }
}
```

## 5. 典型タスクのレシピ

### 「既存の Editor 拡張を多言語化したい」

4 章の手順に加えて、移行時だけ効く注意が 4 つある。

1. key は「機能領域から始め役割が分かる名前」（例 `export.path.mode.label`）にし、文言そのものや表示順を key に含めない。
2. `[Tooltip]` 属性で出していた説明は `BindPropertyField` の `tooltipKey` へ移す（属性は言語変更に追従しない）。
3. `Debug.Log` や `EditorUtility.DisplayProgressBar` の文言も `Tr` の対象にする。UI だけ訳して Console が原文のまま残るのがよくある取りこぼし。
4. 訳し漏れは `Preferences > UnityEditorLocalization` のカタログ節で検証を実行すると scope ごとに並び、不足 key は行末の `+` ボタンでその場に追加できる（値は `defaultLocale` からコピーされる）。大量に足すなら同梱スキルの `scripts/insert_catalog_keys.py`、品質の静的検査は `scripts/validate_locale_quality.py`。

### 「ロケールを 1 つ増やしたい」

1. `Locales/<tag>.json` を追加する。既存テーブルを複製して訳す場合は、**先頭の `"locale"` を新しいタグへ必ず書き換える**。manifest 側の `tag` と食い違うと Console に「tableのlocaleとmanifestのtagが一致しません」の警告が出る。
2. key 集合は `defaultLocale` テーブルと一致させ、placeholder の番号集合も揃える（並び順は言語ごとに変えてよい）。
3. manifest の `locales` へ `tag` / `nativeName` / `englishName` / `tablePath` を追加する。
4. 保存すると `AssetPostprocessor` が自動でリロードする。反映されないときだけ `Reload Catalogs`。
5. `Validate Catalogs` で Error が 0 になるまで直す。**C# の変更は一切不要**。
6. 「defaultLocale と同じ値です（未翻訳の可能性）」は Warning であって Error ではない。製品名・記号・意図的に原語のまま使う語のように全ロケールで同値が正しい key は、manifest の `fixedTerms` へ key 名を並べると個別に抑制できる（例 `"fixedTerms": ["product.name", "menu.shortcut"]`）。

### 「CI でカタログを検証したい」

```bash
Unity -batchmode -quit -projectPath . \
  -executeMethod Kajitaharuka.EditorLocalization.EditorL10nValidator.ValidateForCI
```

既定では Error があるときだけ非 0 で終了する。`-l10nFailOnWarnings` を足すと Warning でも失敗扱いになる。対話モード（通常起動）では Editor を閉じずログのみ出す。落とし穴: `ValidateAll()` は内部で `EditorL10n.Reload()` を呼ぶため、呼ぶだけで `LocaleChanged` が発火し、購読中の UI が再適用される。

## 6. やってはいけないこと

- **ランタイムコードから参照しない。** Editor 専用アセンブリなので、参照した時点でプレイヤービルドが壊れる。ゲーム内 UI には Unity 公式の Localization パッケージなど別の手段を使う。
- **翻訳テーブル JSON を辞書形式（key をフィールド名にした形）で書かない。** `JsonUtility` で読むため `entries: [{ "key", "value" }]` の配列でないと 1 件も読まれず、エラーも出ずに空テーブルになる。
- **manifest のファイル名から `l10n-manifest` を外さない。** 名前に含まれない、または拡張子が `.json` でないと探索対象から外れ、scope ごと存在しないことになる。
- **manifest のフィールド名を独自に変えない。** 読めるのは `scope` / `defaultLocale` / `fixedTerms` / `locales[{tag, nativeName, englishName, tablePath}]` だけで、綴りを外したフィールドは無言で捨てられる（4 章の最小例が正）。
- **カタログを `~` 付きフォルダ（`Samples~` など）や `Assets/`・`Packages/` の外へ置かない。** AssetDatabase から見えず、発見されない。
- **`Tr` の結果を初期化時に一度だけ代入して終わりにしない。** 言語変更に追従しない。UI Toolkit なら `EditorL10nUi.Bind*` か `RegisterLocaleCallback` で束ねる。
- **`[Tooltip]` 属性を多言語表示の主手段に使わない。** `SerializedProperty` に付いた静的文字列で言語変更に追従できない。`BindPropertyField` の `tooltipKey` へ寄せる。
- **`EditorL10n.LocaleChanged` を自前で `+=` したまま解除し忘れない。** static イベントなので Editor のドメイン内でリークする。`RegisterLocaleCallback` を使えば panel への attach / detach に合わせて自動で購読・解除される。
- **ロケールを C# の `enum` や `switch` で扱わない。** 言語の増減は manifest と JSON の追加だけで完結させる設計である。`SystemLanguage` の対応表は「OS 言語の検出」専用で、カタログのロケール定義ではない（表に無い言語もカタログ追加だけで使える）。
- **placeholder の番号集合をロケール間で変えない。** 順序は変えてよいが集合が違うと検証で Error になり、実行時は `LogError` のうえ整形前の文字列が表示される。
- **リテラルの波括弧を `{{` `}}` でエスケープするのは、`args` を渡す文言だけにする。** `Tr` は `args` が空のとき `string.Format` を通さずそのまま返すため、引数の無い文言で `{{` と書くと画面に `{{` がそのまま出る。`{0}` を含む文言でだけエスケープする。
- **言語メニューを既定のまま出荷する前に、末尾のクレジット項目の可否を決める。** `CreateCompactLocaleMenu` / `CreateLocalizedCompactLocaleMenu` は既定（`showAttribution: true`）で、開いたメニューの末尾に区切り線と `Powered by UnityEditorLocalization` を挿入し、クリックで販売元サイトを開く。自分のツールに出したくなければ引数へ `showAttribution: false` を渡す。
- **internal 型に手を伸ばさない。** `EditorL10nUiKit` `EditorL10nCatalog` `EditorL10nPreferences` `EditorL10nSettingsProvider` `EditorL10nCatalogWizard` `EditorL10nSkillInstaller` は internal であり、リフレクションで叩くのはサポート外。UI 部品が要るなら利用側で自作する。
- **パッケージ同梱の `Editor/Localization/Locales/*.json`（この基盤自身の 19 言語カタログ）を書き換えない。** パッケージ更新で失われるうえ、書き換えても自分の拡張の文言には効かない。自分の文言は自分の scope のカタログへ書く。
- **scope を複数の manifest で重複させない。** 後から見つかった方は警告のみで無視され、症状から原因を追いにくい。
- **`CreateLocaleDropdown` / `CreateCompactLocaleMenu` が全体設定を変えると思わない。** これらが書き換えるのは**その scope の個別設定**である。全 scope 共通の設定は `EditorL10n.SetGlobalLocale` か Preferences で扱う。
- **表示言語の設定をプロジェクト資産へ保存し直そうとしない。** 設計上ユーザーごとの `EditorPrefs` であり、チームで共有しない前提である。
- **パッケージの実体パスを固定値だと思わない。** 同梱スキルのスクリプトを直接叩くときのパスは導入形態で変わり、プロジェクトの `Packages/` へ直接置く埋め込み導入なら `Packages/<パッケージ名>/...`、Git URL / registry / VPM 経由なら `Library/PackageCache/<パッケージ名>@<version>/...` になる。解決済みのパスは `Tools > UnityEditorLocalization > AI Agent Skills` が開く Preferences ペインの表示・コピーするコマンドから得られる。
- **Package Manager の Samples 展開先をパッケージ名やフォルダ名から組み立てない。** 展開先は `Assets/Samples/<パッケージの displayName>/<パッケージの version>/<サンプルの displayName>/` で、このパッケージなら `Assets/Samples/UnityEditorLocalization/<version>/Localized Editor Window/` になる（package name `com.kajitaharuka.unity-editor-localization` でも、`Samples~/` 内のフォルダ名 `LocalizedEditorWindow` でもない）。スペースを含むのでシェルでは引用符で囲む。
- **`.unitypackage` で導入した環境で `Samples~/` や `Documentation~/` を探さない。** 含まれていない。必要なら `.tgz` / Git URL / VPM で導入し直す。

## 7. うまくいかないときの切り分け

| 症状 | 原因 | 対処 |
|---|---|---|
| 文言が key 文字列（`window.title` 等）のまま出る | scope 未登録、または key が未解決。`Tr` は例外を投げず key を返す | `Preferences > UnityEditorLocalization` の「開発者向け」節（既定で畳まれている）にある「未解決の翻訳を警告」を ON にする。**既定は OFF** なので何も警告が出ない。ON にすると未知 scope / 未解決 key が 1 回ずつ Console に出る |
| JSON を書いたのに 1 件も反映されない | テーブルが辞書形式になっている／`entries` の形が崩れている | `{"locale":"...","entries":[{"key":"...","value":"..."}]}` の形に直す。無言で空になるのでエラーは出ない |
| 新しく作った manifest の scope が見つからない | ファイル名に `l10n-manifest` が無い、`.json` でない、`~` 付きフォルダ配下にある | ファイル名と置き場所を直し、`Reload Catalogs` を実行する |
| Console に「tableのlocaleとmanifestのtagが一致しません」と出る | テーブル先頭の `"locale"` が manifest の `tag` と違う（既存テーブルの複製で起きやすい） | テーブルの `"locale"` を正しいタグへ直す。読み込みは続行されるが、取り違えの徴候なので放置しない |
| JSON を編集しても反映されない | 自動リロードは AssetDatabase のインポートを起点にする。Unity 外で書き換えた直後は未反映のことがある | Unity にフォーカスを戻すか `Tools > UnityEditorLocalization > Reload Catalogs`（コードからは `EditorL10n.Reload()`） |
| `defaultLocale` にならず OS の言語で表示される | 解決順は「scope 個別設定 → グローバル設定 → **システム言語（既定で有効）** → scope の defaultLocale」の 4 段。グローバル未設定だとシステム言語が挟まる | Preferences でグローバル設定を明示するか、システム言語フォールバックのトグルを OFF にする |
| 言語を切り替えても一部の文言だけ変わらない | その要素が `Tr` の一度きりの代入になっている | `Bind*` か `RegisterLocaleCallback` に置き換える |
| `en-GB` を選んだのに `en` の文言が出る | 仕様。文言の探索は「選択ロケール → その親ロケール → defaultLocale → その親」の順に降りる | 差分だけを `en-GB` テーブルに書けばよい |
| コンパクト言語メニューが灰色で押せない | その scope のカタログが 1 つも登録されていない | tooltip に理由が出る。manifest の scope 文字列とコード側の `Scope` 定数が一致しているか確認する |
| `Kajitaharuka.EditorLocalization` が見つからずコンパイルエラー | 自作 asmdef の `references` 未追加、またはランタイムアセンブリから参照している | asmdef へ参照を追加する。ランタイム側なら Editor 専用アセンブリへ移す |

## 8. 参照先

- 公式ドキュメント（正本）: https://kajitaharuka.com/products/unity-editor-localization/
- 同梱: `README.md` / `CHANGELOG.md` / `LICENSE.md` / `Third Party Notices.md`
- `Samples~/LocalizedEditorWindow`（Package Manager の Samples タブから import。`Tools > UnityEditorLocalization > Samples > Localized Window` で開く）と `Documentation~/`（`DEVELOPER_GUIDE.md` / `OPTIONAL_INTEGRATION.md` / `UI_TOOLKIT_LOCALIZATION_TIPS.md`）は、`.tgz` / Git URL / VPM 導入時のみ同梱。**`.unitypackage` には入りません。**
- 同梱スキル `skills/editor-localization-translation-quality`（翻訳品質の検査・キー一括追加）と `skills/editor-localization-optional-integration`（この基盤を任意依存として組み込む 2 アセンブリ構成の雛形生成）。登録は `Tools > UnityEditorLocalization > AI Agent Skills` が開くペイン内のボタン、または同ペインが表示する CLI コマンドで行う。
