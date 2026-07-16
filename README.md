# UnityEditorLocalization

A lightweight, **Editor-only** localization foundation for Unity editor extensions.
Pull UI text (Inspector labels, HelpBoxes, buttons, Console logs, progress bars) from
per-scope translation catalogs keyed by **scope × locale × key**. Add a language by adding a
JSON file — no C# changes.

- **Editor-only.** No dependency on runtime code, Addressables, or the Unity Localization package.
- **Locales are string tags**, declared in a manifest — not a C# `enum`. New locales never touch C#.
- **19 languages** ship in the settings UI and the bundled sample catalog.
- **UI Toolkit helpers** bind `Label` / `Button` / `PropertyField` to keys and follow language changes automatically. A compact locale menu and a locale dropdown are included.
- **Optional-dependency friendly.** Consuming packages integrate it as an *optional* dependency: they compile and run standalone in a single default language, then light up multi-language UI and a locale switcher when this package is installed — with no hard assembly reference.
- **Catalog tooling.** A creation wizard scaffolds a manifest + empty tables. Validation checks missing keys, `string.Format` placeholder consistency, and untranslated-suspect entries (runnable in CI batchmode).
- **Bundled AI-agent skills** for translation quality and optional-integration scaffolding.

> This repository is the public MIT-licensed source. It is developed as a single embedded UPM
> package under [`Packages/com.kajitaharuka.unity-editor-localization/`](Packages/com.kajitaharuka.unity-editor-localization/);
> the rest of the repository (`Assets/`, `ProjectSettings/`) is only the host project used to
> develop and verify the package inside the Unity Editor.

## Installation

Requires **Unity 2022.3 or later**.

### Package Manager (Git URL)

In *Package Manager → Add package from git URL…*, enter:

```text
https://github.com/HarukaKajita/UnityEditorLocalization.git?path=Packages/com.kajitaharuka.unity-editor-localization
```

Pin a version by appending a release tag, e.g. `#1.2.1`. You can also add the same URL directly
to `Packages/manifest.json` under `dependencies`.

### VPM (VCC / ALCOM)

Add the VPM repository, then add UnityEditorLocalization from it:

```text
https://harukakajita.github.io/vpm-repos/index.json
```

### Booth

A packaged `.zip` (`.unitypackage` / `.tgz` inside) is available on Booth — free, with an optional
supporter tier:

```text
https://genera.booth.pm/items/8617787
```

## Documentation

- **Product page (multilingual):** <https://kajitaharuka.com/products/unity-editor-localization/>
- **Package usage (README):** [`Packages/com.kajitaharuka.unity-editor-localization/README.md`](Packages/com.kajitaharuka.unity-editor-localization/README.md) — overview, minimal setup, API, fallback, validation.
- **In-depth guides** (`Documentation~/`):
  - `DEVELOPER_GUIDE.md` — scope/key design guidance for consuming extensions.
  - `UI_TOOLKIT_LOCALIZATION_TIPS.md` — how to make UI Toolkit follow language changes.
  - `OPTIONAL_INTEGRATION.md` — the two-assembly optional-dependency pattern.

## License

MIT License. See [`Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md`](Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md).
Attribution is not required but always welcome.

---

## 日本語

Unity エディタ拡張向けの、**Editor 専用**の軽量な多言語化基盤です。Inspector・HelpBox・ボタン・
Console ログ・進捗表示などの文言を、**scope × ロケール × key** で引く翻訳カタログから取得します。
言語の追加は JSON ファイルを足すだけで、C# の変更は不要です。

- **Editor 専用。** ランタイム、Addressables、Unity Localization package には依存しません。
- **ロケールは文字列タグ**（manifest 宣言）で扱い、C# の `enum` にしません。言語追加で C# を触りません。
- 設定 UI と同梱サンプルカタログが **19 言語**に対応します。
- **UI Toolkit バインド**で `Label` / `Button` / `PropertyField` を key に結び付け、言語切替に自動追従します。コンパクト言語メニューと言語選択ドロップダウンを同梱します。
- **任意依存（optional）として組み込めます。** 利用側パッケージは基盤が無くても既定言語の単一言語で動作し、本基盤を入れると多言語化と言語切替 UI が点灯します（ハード参照なし）。
- **カタログ支援。** 作成ウィザードで manifest と空テーブルの雛形を生成し、検証で key 過不足・`string.Format` placeholder 整合・未翻訳疑いを確認できます（CI batchmode 対応）。
- 翻訳品質と任意依存統合の雛形生成を助ける **AI エージェント向けスキル**を同梱します。

> 本リポジトリは MIT ライセンスで公開しているソースです。開発対象は
> [`Packages/com.kajitaharuka.unity-editor-localization/`](Packages/com.kajitaharuka.unity-editor-localization/)
> 配下の単一 embedded UPM package で、リポジトリのその他（`Assets/`・`ProjectSettings/`）は
> Unity Editor 上で開発・検証するための器です。

### インストール

**Unity 2022.3 以降**が必要です。

- **Package Manager（Git URL）:** *Add package from git URL…* に次を入力します（バージョン固定はタグ `#1.2.1` を末尾に付与）。

  ```text
  https://github.com/HarukaKajita/UnityEditorLocalization.git?path=Packages/com.kajitaharuka.unity-editor-localization
  ```

- **VPM（VCC / ALCOM）:** 次の VPM リポジトリを追加してから UnityEditorLocalization を追加します。

  ```text
  https://harukakajita.github.io/vpm-repos/index.json
  ```

- **Booth:** パッケージ済みの `.zip` を配布しています（無料。お布施版あり）。

  ```text
  https://genera.booth.pm/items/8617787
  ```

### ドキュメント

- **商品ページ（多言語）:** <https://kajitaharuka.com/products/unity-editor-localization/>
- **パッケージ利用ガイド:** [`Packages/com.kajitaharuka.unity-editor-localization/README.md`](Packages/com.kajitaharuka.unity-editor-localization/README.md)
- **詳細ガイド（`Documentation~/`）:** `DEVELOPER_GUIDE.md` / `UI_TOOLKIT_LOCALIZATION_TIPS.md` / `OPTIONAL_INTEGRATION.md`

### ライセンス

MIT License です。詳細は [`Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md`](Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md) を参照してください。クレジット表示の義務はありませんが、付けていただけると嬉しいです。
