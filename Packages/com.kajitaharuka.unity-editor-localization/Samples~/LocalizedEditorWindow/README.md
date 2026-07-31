# Localized Editor Window Sample

UnityEditorLocalization を使った、自己説明的な多言語 `EditorWindow` サンプルです。

## 開き方

1. Package Manager から `Localized Editor Window` サンプルを import します。
2. Unity Editor のメニューから `Tools > UnityEditorLocalization > Samples > Localized Window` を開きます。
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
- `Tools > UnityEditorLocalization > Validate Catalogs`
- `skills/editor-localization-translation-quality/scripts/validate_locale_quality.py`

## 翻訳品質チェック

利用側の locale table を静的に確認したい場合は、対象の `Locales` ディレクトリに対して同梱スキルの
`editor-localization-translation-quality/scripts/validate_locale_quality.py` を実行します。

スクリプトの実体パスは導入形態で変わります。プロジェクトの `Packages/` へ直接置く埋め込み導入なら
`Packages/com.kajitaharuka.unity-editor-localization/skills/`、Git URL / registry / VPM 経由なら
`Library/PackageCache/com.kajitaharuka.unity-editor-localization@<version>/skills/` です。
自分の環境の正確なパスは、`Tools > UnityEditorLocalization > AI Agent Skills` を選ぶと開く
`Preferences > UnityEditorLocalization`の「AIエージェント連携スキル」節で確認できます
（この画面が表示・コピーするコマンドに、解決済みの `skills` フォルダのパスが埋め込まれています）。

次は、このサンプル自身の locale table を対象にする例です。Unity プロジェクトのルートで実行してください。
`<skills>` は上で確認した `skills` フォルダのパスに、`<version>` は import したパッケージのバージョンに
読み替えてください。

```bash
python3 "<skills>/editor-localization-translation-quality/scripts/validate_locale_quality.py" \
  "Assets/Samples/UnityEditorLocalization/<version>/Localized Editor Window/Editor/Localization/Locales" \
  --default-locale ja
```

Package Manager はサンプルを
`Assets/Samples/<パッケージの displayName>/<パッケージの version>/<サンプルの displayName>/` へ展開します。
このパッケージの `displayName` は `UnityEditorLocalization`、このサンプルの `displayName` は
`Localized Editor Window` なので、展開先はスペースを含むパスになります。シェルでは引用符で囲んでください。
