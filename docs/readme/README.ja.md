# UnityEditorLocalization

**Languages:** [English](../../README.md) | 日本語 | [简体中文](README.zh-Hans.md) | [繁體中文](README.zh-Hant.md) | [한국어](README.ko.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Italiano](README.it.md) | [Español (España)](README.es-ES.md) | [Español (Latinoamérica)](README.es-419.md) | [Português (Brasil)](README.pt-BR.md) | [Português (Portugal)](README.pt-PT.md) | [Русский](README.ru.md) | [Polski](README.pl.md) | [Türkçe](README.tr.md) | [ไทย](README.th.md) | [Tiếng Việt](README.vi.md) | [Українська](README.uk.md) | [Bahasa Indonesia](README.id.md)

> これは翻訳版です。正本は英語版 README（[English](../../README.md)）です。

Unity エディタ拡張向けの、**Editor 専用**の軽量な多言語化基盤です。Inspector ラベル・HelpBox・ボタン・Console ログ・進捗バーなどの UI 文言を、**scope × locale × key** で引く scope ごとの翻訳カタログから取得します。言語の追加は JSON ファイルを足すだけで、C# の変更は不要です。

- **Editor 専用。** ランタイムコード、Addressables、Unity Localization package のいずれにも依存しません。
- **ロケールは文字列タグ**で、manifest で宣言します（C# の `enum` ではありません）。ロケール追加で C# を触ることはありません。
- 設定 UI と同梱サンプルカタログが **19 言語**に対応します。
- **UI Toolkit ヘルパー**が `Label` / `Button` / `PropertyField` を key に結び付け、言語切替に自動追従します。コンパクトなロケールメニューとロケールのドロップダウンを同梱します。
- **任意依存（optional）にやさしい設計。** 利用側パッケージは本パッケージを*任意*依存として組み込めます。基盤が無くても既定言語の単一言語でコンパイル・動作し、本パッケージを入れると多言語 UI とロケール切替が点灯します（ハードなアセンブリ参照は不要）。
- **カタログ支援ツール。** 作成ウィザードが manifest と空テーブルの雛形を生成します。検証では key の欠落、`string.Format` の placeholder 整合、未翻訳が疑われるエントリを確認できます（CI の batchmode でも実行可）。
- 翻訳品質と任意依存統合の雛形生成を助ける **AI エージェント向けスキル**を同梱します。

> 本リポジトリは MIT ライセンスで公開しているソースです。開発対象は [`Packages/com.kajitaharuka.unity-editor-localization/`](../../Packages/com.kajitaharuka.unity-editor-localization/)
> 配下の単一 embedded UPM package で、リポジトリのその他（`Assets/`・`ProjectSettings/`）は
> Unity Editor 上でパッケージを開発・検証するための器（host project）にすぎません。

## インストール

**Unity 2022.3 以降**が必要です。

### Package Manager (Git URL)

*Package Manager → Add package from git URL…* に次を入力します。

```text
https://github.com/HarukaKajita/UnityEditorLocalization.git?path=Packages/com.kajitaharuka.unity-editor-localization
```

リリースタグを末尾に付けるとバージョンを固定できます（例 `#1.2.1`）。同じ URL を `Packages/manifest.json` の `dependencies` に直接追記することもできます。

### VPM (VCC / ALCOM)

次の VPM リポジトリを追加してから、そこから UnityEditorLocalization を追加します。

```text
https://harukakajita.github.io/vpm-repos/index.json
```

### Booth

パッケージ済みの `.zip`（中に `.unitypackage` / `.tgz` を同梱）を Booth で配布しています。無料で、任意のサポーター（お布施）ティアがあります。

```text
https://genera.booth.pm/items/8617787
```

## ドキュメント

- **商品ページ（多言語）:** <https://kajitaharuka.com/products/unity-editor-localization/>
- **パッケージ利用ガイド（README）:** [`Packages/com.kajitaharuka.unity-editor-localization/README.md`](../../Packages/com.kajitaharuka.unity-editor-localization/README.md) — 概要、最小セットアップ、API、フォールバック、検証。
- **詳細ガイド**（`Documentation~/`）:
  - `DEVELOPER_GUIDE.md` — 利用側拡張向けの scope/key 設計ガイド。
  - `UI_TOOLKIT_LOCALIZATION_TIPS.md` — UI Toolkit を言語切替に追従させる方法。
  - `OPTIONAL_INTEGRATION.md` — 2 アセンブリ構成の任意依存パターン。

## ライセンス

MIT License です。詳細は [`Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md`](../../Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md) を参照してください。クレジット表示の義務はありませんが、付けていただけると嬉しいです。
