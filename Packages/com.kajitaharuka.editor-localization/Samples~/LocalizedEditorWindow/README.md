# Localized Editor Window Sample

Editor Localization を使った、自己説明的な多言語 `EditorWindow` サンプルです。

## 開き方

1. Package Manager から `Localized Editor Window` サンプルを import します。
2. Unity Editor のメニューから `Tools > Editor Localization > Samples > Localized Window` を開きます。
3. ウィンドウ右上のコンパクト言語メニュー、またはフォーム内の言語ドロップダウンで表示言語を切り替えます。

## 含まれるもの

- `Editor/LocalizedEditorWindowSample.cs`: UI Toolkit だけで構築したサンプルウィンドウです。
- `Editor/LocalizedEditorWindowSample.asmdef`: Editor 専用の sample assembly です。
- `Editor/Localization/localized-editor-window-sample.l10n-manifest.json`: sample 用の manifest です。
- `Editor/Localization/Locales/ja.json` / `en.json`: 日本語と英語の locale table です。

## 確認ポイント

- `EditorL10nUi.CreateLocalizedCompactLocaleMenu`
- `EditorL10nUi.CreateLocalizedLocaleDropdown`
- `EditorL10nUi.BindText`
- `EditorL10nUi.BindButton`
- `EditorL10nUi.RegisterLocaleCallback` による `HelpBox`、`Foldout`、`ProgressBar`、tooltip、placeholder 表示の更新
- `Tools > Editor Localization > Validate Catalogs`
- `skills/editor-localization-translation-quality/scripts/validate_locale_quality.py`

## 翻訳品質チェック

利用側の locale table を静的に確認したい場合は、対象の `Locales` ディレクトリに対して次を実行します。

```bash
python3 Packages/com.kajitaharuka.editor-localization/skills/editor-localization-translation-quality/scripts/validate_locale_quality.py \
  Assets/Samples/<package-name>/<version>/LocalizedEditorWindow/Editor/Localization/Locales --default-locale ja
```
