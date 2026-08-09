# Changelog

## [Unreleased]

### Fixed
- **ロシア語・ウクライナ語・ポーランド語で「asset」の訳語が同じカタログ内で割れていたのを揃えた。**
  ロシア語とウクライナ語の 1 件が asset を `ресурс` と訳しており、これは Unity の `Resources`
  フォルダ（Unity 公式のロシア語ドキュメントが `ресурс` と訳し分けている別概念）と衝突していた。
  それぞれ `ассет` / `асет` へ改めた。ウクライナ語の 2 件は `ассет` と `с` を重ねていたが、
  ウクライナ語正書法は外来語の子音を重ねないため `асет` へ統一した。ポーランド語の 2 件は
  借用語 `asset` のままで、同じファイル内の `zasób` と食い違っていたので `zasób` へ揃えた。
  表示される意味は変わらない。

## [1.3.3] - 2026-08-02

### Added
- 購入者のエージェント向けガイド `CLAUDE.md` / `AGENTS.md` をパッケージ直下へ同梱した（2 ファイルは同一内容）。このパッケージを**使う**コーディングエージェント向けの手引きで、要約 / 適用範囲と前提 / 入口 / 基本の使い方 / 典型タスクのレシピ / やってはいけないこと / うまくいかないときの切り分け / 参照先の 8 章からなる。開発リポジトリの文脈をまったく持たないエージェントを前提に、「ソースを読めば分かること」ではなく「読んでも分からない・読む前に知る必要がある・読んだ結果を誤読させる」ことに絞って書いてある（このパッケージでできないこと＝ランタイムの多言語化・IMGUI 用のバインド API・訳文の自動生成、も明記している）。開発者向けの情報は従来どおりリポジトリ直下の `CLAUDE.md` / `AGENTS.md` に置く。「やってはいけないこと」には下記の訂正と同じ内容（パッケージの実体パスが導入形態で変わることと解決済みパスの確認先、Package Manager の Samples 展開先を package name やサンプルのフォルダ名から組み立てないこと）を含めている。

### Fixed
- `Documentation~/DEVELOPER_GUIDE.md` の表示ロケール解決順が 1 段抜けていたのを訂正した。「scope 個別設定 → グローバル設定 → scope の `defaultLocale`」の 3 段と書いていたが、実装（`EditorL10n.GetActiveLocale`）は「scope 個別設定 → グローバル設定 → システム言語（システム言語フォールバックが有効なとき。既定は有効）→ scope の `defaultLocale`」の 4 段である。グローバル設定を明示していない環境では `defaultLocale` より先に OS の言語が使われるため、この記述のままでは「なぜ `defaultLocale` にならないのか」の切り分けを誤らせる。`README.md` は元から 4 段で正しく、パッケージ内で記述が食い違っていた。あわせて `defaultLocale` で表示させたい場合の対処（`Preferences > UnityEditorLocalization`でグローバル設定を明示する、または「システム言語フォールバックを使う」を OFF にする）も追記した。
- **サンプル `Localized Editor Window` のウィンドウ上に表示していた翻訳品質スクリプトのパスが、埋め込み導入前提の固定値だったのを直した。** 「検証」節の「翻訳品質スクリプト」の行が `Packages/com.kajitaharuka.unity-editor-localization/skills/.../validate_locale_quality.py` を直書きしており、Git URL / registry / VPM 経由で導入した利用者には**存在しないパスを見せていた**。下記の `README.md` の訂正と同じ問題が、その `README.md` が説明している当のサンプル UI に残っていた。解決済みのパッケージパス（`PackageManager.PackageInfo` の `resolvedPath`）から組み立てるようにしたので、どの導入形態でも**シェルへそのまま貼れる実在のパス**が表示される（パッケージ情報を取得できない場合のみ、パッケージルート相対のパスを表示する）。
- サンプル `Localized Editor Window` の `README.md` にある翻訳品質チェックの実行例を、導入形態に依存しない書き方へ改めた。スクリプトのパスを `Packages/com.kajitaharuka.unity-editor-localization/skills/...` と埋め込み導入前提で固定していたが、Git URL / registry / VPM 経由では実体が `Library/PackageCache/com.kajitaharuka.unity-editor-localization@<version>/skills/...` になるため、そのまま実行しても動かない。実体パスの確認先（`Tools > UnityEditorLocalization > AI Agent Skills` が開く Preferences の「AIエージェント連携スキル」節）を示す形にした。
- 同じサンプルの `README.md` に書いていたサンプル展開先を、Package Manager の実際の挙動へ合わせた。`Assets/Samples/<package-name>/<version>/LocalizedEditorWindow` と書いていたが、Package Manager が使うのは package name ではなくパッケージとサンプルの `displayName` であり、実際の展開先は `Assets/Samples/UnityEditorLocalization/<version>/Localized Editor Window` である（import 済みのプロジェクトで実物を確認した）。スペースを含むためシェルでは引用符が要る点も明記した。

## [1.3.2] - 2026-07-30

### Added
- `.unitypackage` で導入した場合に `Samples~/`（サンプル `Localized Editor Window`）と `Documentation~/`（開発者向けガイド 3 本）が含まれないことを `README.md` へ明記した。Unity の AssetDatabase が `~` 付きの隠しフォルダを扱わないことによる構造的な差で、基盤の動作に必要なファイルはどちらの形式にも含まれる。1.3.1 の実物（`.tgz` / `.unitypackage`）を突き合わせて確認した差は `Samples~/` 6 ファイルと `Documentation~/` 3 ファイルの計 9 ファイルのみで、残る 66 ファイルは両方に入っている。README は「インストール」に注意節を設け、サンプル節と関連資料節の 2 箇所からも参照するようにした（この 2 つが `.unitypackage` 利用者にとって行き止まりになるため）。
- 同梱の translation-quality スキルに、文言が名乗っている型名・属性名が実装に存在するかを突き合わせる検査スクリプト（`check_message_identifiers.py`）を追加した。翻訳の質でも placeholder でもないため、`Validate Catalogs` も `validate_locale_quality.py` もこの層を 1 件も見ていない。あわせて SKILL.md へ 3 節を追加した: 文中で UI ラベルを参照するときはそのラベルの訳をプレースホルダで渡すこと、識別子の実在確認、分担して展開するときは変更を「意味変更」と「正本言語だけの文法修正」へ先に分類すること。

### Changed
- `README.md` のバージョン固定例から具体的なバージョン番号を外し、プレースホルダ `#<tag>` と、タグ名の在り処（`CHANGELOG.md` に並ぶバージョン番号と同じ。一覧はリポジトリの Releases）を示す形へ改めた。固定例に具体的な番号を書くと、リリースのたびに書き換えない限り古い版を指し続け、コピーした利用者が意図せず旧版を掴む（実際に 1.2.1 の例が 1.3.1 の時点まで残っていた）。プレースホルダなら陳腐化せず、そのまま貼った場合も解決に失敗して誤りに気づける。

### Fixed
- 同梱ドキュメントの記述を実装と一致させた。`README.md` の「新しいロケールはJSONファイルを追加するだけで増やせます」は manifest の `locales` への登録が必要な点が抜けていたため「翻訳JSONを追加し、manifestの`locales`へ登録するだけ（C#の変更は不要）」へ訂正し、`package.json` の `description` にも同じ訂正を反映した。`Documentation~/DEVELOPER_GUIDE.md` の scope の説明は「翻訳カタログの名前空間」という表現が C# の namespace と混同されうるため、「翻訳カタログを拡張ごとに分ける一意な識別子（C#のnamespaceとは無関係）」へ改めた。
- AI エージェント連携スキルの登録について、`README.md` の「macOS / Linux / Windows いずれでも動作します」という断定を、実装している範囲（macOS/Linux は `ln`、Windows は `mklink /D`。権限が無い環境では junction へフォールバック）と「権限やファイルシステムの設定によっては登録に失敗することがある」旨の記述へ改めた。

## [1.3.1] - 2026-07-25

### Fixed
- カタログ検証で、本パッケージ自身の `pill.fallback` が 8 ロケール（es-ES / es-419 / pt-BR / pt-PT / it / pl / th / id）で「未翻訳の可能性」と警告されていた問題を修正した。これらの言語では `fallback` を原語のまま使うことが用語方針として一貫しており（同カタログの `system.fallback.label` などの関連文言もすべて原語表記）、誤検出だった。manifest の `fixedTerms` に `pill.fallback` を追加して抑止した。`fixedTerms` は「ファイル名・型名など全ロケールで同値が正当なもの」に加えて「一部の言語だけ原語のまま使うことが正当なもの」にも使える旨をコメントへ明記した。

## [1.3.0] - 2026-07-25

### Changed
- スキル登録 CLI コマンドの表示を、既定で畳んだ折りたたみへ変更した（GOLD_STANDARD §2.6-1）。開くと実行中の OS 向けに生成されたコマンド全文を確認でき、確認してからコピーできる。開閉状態は保持される。
- package.json の説明文（`description`）を「英語 → 改行 → 日本語」の 2 段書きへ変更した。Package Manager の詳細欄で両言語の説明を確認できる。
- AI エージェント連携スキルのメニューを間接方式へ変更した（GOLD_STANDARD §2.6）。従来 `Tools > UnityEditorLocalization > AI Agent Skills` 直下にあった 3 項目（Install for current user / Install for this project / Copy CLI commands to clipboard）を廃止し、単一項目 `Tools > UnityEditorLocalization > AI Agent Skills` に統合した。この項目はスキル導入の説明・登録状態・実行ボタンを持つ Preferences ペイン（Preferences > UnityEditorLocalization）を開くだけで、メニューから直接スキル登録やクリップボード書き込みは行わない。意図しないスキル追加は開発者に敬遠されるため、登録はユーザーがペイン内で内容を理解したうえで明示的にボタンを押した場合にのみ行う方針へ改めた。登録・CLI コピー機能そのものは Preferences ペインに従来どおり存在する。
- 同梱の optional-integration スキルを更新: 連携 asmdef のファイル名を短縮形 `L10nIntegration.asmdef` に固定（assembly name は不変）。Unity Asset Store のファイルパス150字制約に適合するため。テンプレートフォルダ名も `templates/L10nIntegration/` へ短縮。
- 同スキルのテンプレートファイル名をさらに短縮した（`IntegrationAsmdef.asmdef.txt` → `Integration.asmdef.txt`〔当初 `Asmdef.asmdef.txt` としたが意味の分かりやすさを優先して再調整。144字で規則内〕、`EditorL10nBridgeInstaller.cs.txt` → `BridgeInstaller.cs.txt`。`.meta` 同伴・GUID 維持）。本パッケージ自身のファイルパスが UAS 150字規則を超えていたため（出力ファイル名の規約は不変で、生成結果に影響はない）。

### Added
- 同梱の translation-quality スキルに、翻訳カタログへの全ロケール一括挿入スクリプト（`insert_catalog_keys.py`。書式自動判定・fail-closed・dry-run 対応）と、既存訳語の横断ダンプスクリプト（`dump_catalog_terms.py`）を追加した。
- AI エージェント連携スキルの Preferences ペインに、同梱スキルごとの内容一覧を追加した（GOLD_STANDARD §2.6-1）。スキルの名前・要約説明に加えて「プロンプト例」1 行を表示し、登録ボタンを押す前に「エージェントへ何を頼めるスキルか」を把握できるようにした。併せて各スキルの正本フォルダ（package 同梱 `skills/`）へのクリック可能な導線を追加し、クリックすると Project ビューでフォルダを選択・強調表示して SKILL.md を含む全文を登録前に確認できる（表示は 19 言語対応）。
- 対応 Unity バージョンの表記に `unityRelease`（`2022.3` 系の任意パッチで動作）を明記し、Package Manager での互換性判定がより正確になった。
- サードパーティ成分の有無を明記する `Third Party Notices.md` を同梱した（本パッケージはサードパーティ成分を含まない）。

### Fixed
- AI エージェント連携スキルの登録で、登録先に実体ディレクトリ（symlink ではないフォルダ）が既にある場合は有効な登録として扱い、内部へ入れ子の symlink を作らないようにした。

## [1.2.1] - 2026-07-08

### Changed
- 翻訳カタログの fallback 訳語を言語内で統一した。it / es-ES / es-419 / pt-BR / pt-PT / pl / th / id は既に多数派だったラテン語 "fallback" へ（`pill.fallback` ほか少数派の ripiego / respaldo / alternativa / rezerwa / ถอยกลับ / cadangan を置換）、vi は自然語 dự phòng へ、ru / uk は резерв 系名詞へ統一（откат 等の別語を解消し、`system.fallback.label` の不自然な格支配も修正）。zh-Hant の「後援」は Microsoft 用語集（後援字型）の確立語のため維持。
- pt-PT カタログの pt-BR 語彙混入を修正した（`Desenvolvedor`→`Programador`、`Registra`/`registrar`→`Regista`/`registar`、`detectado`→`detetado`（性一致は `detetada`）、`idioma de exibição`→`idioma de apresentação`）。
- pl カタログの locale の文法性を女性形へ統一した（`Domyślna locale` / `tej locale` / `żadnej locale` / `nieustawiona`。コード識別子 `defaultLocale` は男性のまま。方針は翻訳品質スキルの language-notes に明記）。
- 同梱の翻訳品質スキル（`editor-localization-translation-quality`）を強化した。日本語原文由来の系統的な誤訳パターン（句読点・記号の伝播 / 漢語の同形借用 / コロケーション破壊 / 括弧注記の係り先反転 / メタ言語表現の偽友）の検査項目を language-notes へ、固定語検査の具体的な機構（原文との出現回数比較・既知誤訳形の grep）と「UI 引用の正はカタログ側」の工程順を terminology-and-style へ追加し、SKILL.md の workflow から参照するようにした。

## [1.2.0] - 2026-07-06

### Changed
- Preferences の 5 つの大項目（表示言語 / scope 個別設定 / カタログ / AIエージェント連携スキル / 開発者向け）を折りたたみ可能にした。開閉状態は EditorPrefs（ユーザーごと）に永続化し、畳んだ見出しにも要約ピル（表示言語=現在の実効言語と由来、scope 個別設定=scope 数と個別設定数、カタログ=最終検証のエラー/警告件数）を表示する。開発者向けは従来どおり既定で畳む（Foldout を廃し、他の折りたたみと同じ共通部品へ統一）。
- セクション見出しの視覚的階層を強化（11px・淡色 → 12px・本文色。中身の scope 名より見出しが弱かった逆転を解消）。あわせて `EditorDesignTokens.uss` に間隔・フォント・角丸のトークン（`--eui-space-*` / `--eui-font-*` / `--eui-radius-*`）を導入して生値の散在を解消し、10px だった補足・バッジ・fallback 連鎖チップの文字サイズを 11px へ引き上げた（可読性）。
- カタログの大項目を scope 別グループへ統合再設計。各 scope のグループに**カタログのファイル一覧**（manifest 行と各 locale テーブル行。既定ロケールのバッジ・key 数ピル・manifest が宣言しているのに実体が無いテーブルの欠落マーカー `×` 付き。クリックで選択+Ping）と**検証結果**を同居させ、どのファイル・どの警告かを 1 箇所で追えるようにした。検証済みで問題の無い scope は ✓ ピルで示す（これに伴い「他 {0} scope は問題なし」の `catalogs.groups.clean` は廃止）。グループの開閉はユーザー操作を記憶し、未操作時の既定はエラーを含む scope のみ展開。
- scope 個別設定カードから manifest 行をカタログの大項目へ移設し、「表示の設定（scope 個別設定）」と「データ資産の保守（カタログ）」の責務を分離した。scope の絞り込み検索は従来どおり scope 個別設定のみに適用する（別の大項目の表示を暗黙に書き換えない方針）。
- 検証要約の色を内容と一致させた（エラーあり=エラー色 / 警告のみ=警告色＋専用文言 / 問題なし=OK 色。従来は警告があっても緑で表示されていた）。要約行に検証の実行時刻を添え、スナップショットの鮮度を明示する。
- ヘッダーの概況バッジを総合判定へ変更（検証エラーあり=Error、検証警告または要求ロケールへ fallback 中の scope あり=Warning、平常=Neutral。従来は検証結果がバッジへ反映されなかった）。色が変わった理由（検証エラー/警告の件数・fallback 中の scope あり）は tooltip で確認できる（色だけに頼らない）。
- カタログ作成ウィザードを改善。画面全体を ScrollView に収め（縦に狭いときやメッセージ表示時も潰れずスクロールで全体へ到達）、scope 入力欄に例示ヒント（`wizard.scope.hint`）を常設し、作成成功の表示を Info HelpBox から Ok 色のインライン結果行（Preferences と同じ `SetInlineResult`）へ統一、ヘッダー右肩にオンラインドキュメントボタンを追加した。表示中の結果/エラー文言は言語切替にも追従する。
- 公開 UI 部品を堅牢化。`CreateLocaleDropdown` に applying ガード（try/finally 保護）を導入し、カタログ再読込・言語変更時の choices 再代入が同期発火させる value-changed を「ユーザー操作」と誤認して過渡値で scope 個別設定を書き戻す恐れを解消。選択中ロケールが候補に無いときは空欄でなく「（登録済みカタログ外）」表記（`outOfCatalog`）を表示する。`CreateCompactLocaleMenu` はカタログ未登録で無効のとき、tooltip に「なぜ押せないか」（`menu.noCatalog.tooltip`）を出す。
- 操作結果のインライン 1 行表示を `EditorL10nUiKit.SetInlineResult` / `ResolveStatusColor` として共通化（Preferences 内の private ヘルパーから移設。画面間で同じ概念を同じ見た目にする）。
- カタログ節の操作ボタンを主操作順（検証 → 再読み込み → カタログを作成…）へ並べ替え、Preferences から作成ウィザードを直接開けるようにした。カタログ未登録時の空状態には作成導線を案内する。
- 検証 issue 行のクイックfix「+」を行頭から行末へ移動し、深刻度マーカー（`×`/`!`）の縦の整列が崩れないようにした（IDE のクイックfix と同じ位置の慣習）。
- AIエージェント連携スキルの登録先（ホーム / このプロジェクト）ごとに登録状態ピル（登録済み / 未登録 / 要再登録）を表示し、押す前から状態が分かるようにした。
- ヘッダー直下に常設していた保存先・解決順の説明ノートを、表示言語の大項目内へ移設した。
- スキル節・CLI 欄などに散在していた C# 直書きのスタイル（フォントサイズ・余白）を USS クラスへ移し、デザイントークンと一貫させた。
- 翻訳カタログの表記ゆれを統一。ja は「フォールバック」（カタカナ）へ（`scope.fallbackNote` / `header.overview.fallback` のラテン表記を修正）、de は敬称を Sie 体へ（`wizard.subtitle` / `wizard.error.*` の du 体を修正）。表記ポリシーは翻訳品質スキルの language-notes（ja / de 節）へ明記し再発を防ぐ。

### Added
- UI 部品 `EditorL10nUiKit.CollapsibleSection`（チェブロン＋見出し＋要約スロット。見出し行全体のクリックで開閉、開閉状態を EditorPrefs へ永続化）と `Chevron` を追加。scope カード・検証グループの自前チェブロンも共通部品へ統一した。
- `EditorL10n.TryGetEntryCount(scope, locale, out count)`（internal）を追加（カタログ一覧の key 数表示用）。
- `EditorL10nSkillInstaller.GetUserInstallState()` / `GetProjectInstallState()` を追加。登録先の symlink の有無・リンク切れを確認し、`EditorL10nSkillInstallState`（Installed / NotInstalled / NeedsReinstall）で返す。
- UI 監査対応の翻訳 3 キーを **19 言語**で追加（各 111→114 キー）: `header.overview.fallback`（概況バッジが警告色になる理由）/ `wizard.scope.hint`（scope の例示）/ `menu.noCatalog.tooltip`（言語メニューの無効理由）。fr は既存用語（repli 系）へ統一。キー過不足・placeholder・未翻訳疑いの機械検証をクリア。
- Preferences 翻訳へ 14 キーを **19 言語**で追加し、2 キーの文言を更新、1 キーを削除（各 98→111 キー）。追加: `catalogs.empty`（空状態と作成導線）/ `catalogs.create.tooltip` / `catalogs.result.warnings`（警告のみの要約）/ `catalogs.validatedAt`（検証時刻）/ `catalogs.pill.default` / `catalogs.table.tooltip` / `catalogs.table.missing.tooltip` / `catalogs.entries.tooltip` / `scope.summary` / `skills.status.installed` / `skills.status.notInstalled` / `skills.status.reinstall` / `skills.status.tooltip` / `summary.localeWithSource`。更新: `catalogs.result.ok`（警告件数への言及を分離）/ `catalogs.help.tooltip`（主操作の検証を先に説明）。削除: `catalogs.groups.clean`。`summary.localeWithSource` は句読点のみのパターンで原語と同値が正当なため manifest の `fixedTerms` へ宣言。キー過不足・placeholder・未翻訳疑いの機械検証をクリア。

## [1.1.0] - 2026-06-28

### Changed
- Preferences の開発者向け「未解決警告」トグルを分かりやすく改善。ラベルを「未解決の翻訳を警告」へ変更し、**何を・いつ・どう振る舞うか**を説明する永続ノートを追加した（`Tr()` が未登録 scope や、どの locale にも無い key を引いたとき Console に警告。タイプミスや未追加 key を開発中に見つける用途。`Tr()` は key を返すので壊れない／同一 scope/key につき 1 回／既定オフ）。ノートは 19 言語へ追加。
- 「再読み込み・検証」を独立した大項目（**カタログ** Section、タイトル付き）へ昇格し、主要操作の**表示言語**・**scope 個別設定**の下へ移動。検証結果はその Section 内に scope ごとに分類して表示する。
- `EditorL10nValidator` の結果を構造化。`EditorL10nValidationIssue`（`Severity` / `Kind`〈`EditorL10nValidationMessageKind`〉/ `Scope` / `Locale` / `Args`）と `EditorL10nValidationResult.Issues` を追加し、UI が由来 scope ごとに分類できるようにした。診断の詳細文は文字列で固定せず**種類＋引数**で持ち、表示時（`Message` プロパティ）にパッケージ自身の翻訳カタログから現在の表示言語で整形する。既存の `Errors` / `Warnings`（`{scope}/{locale}: 詳細` の平坦な文字列）と Console 出力は後方互換だが、文面は表示言語に追従するようになった。
- 表示ロケールの解決順に**システム言語（OS）フォールバック**を追加。グローバル設定が未設定のとき、OS の優先言語（Unity の `Application.systemLanguage` を主に、地域は `CultureInfo` で補完。macOS でも信頼可）を推定して表示に使い、対応する翻訳が無ければ既存の fallback 連鎖が各 scope の `defaultLocale` へ落とす。解決順は `scope 個別設定 → グローバル設定 → システム言語 → defaultLocale`。検出のみ OS API（enum）を言語タグ表へ対応付け、カタログ/解決は文字列タグのみで扱う方針（言語追加で解決ロジックの C# は不変）を維持する。
- 表示ロケールの解決順を `EditorL10n.GetActiveLocale(scope, out source)` に集約し、由来（`EditorL10nLocaleSource`）を単一情報源化。Preferences の scope メタ表示はこの source を使い、System を含む由来を正しく表示する。
- Preferences（`Preferences > UnityEditorLocalization`）を IMGUI から UI Toolkit へ全面再設計。2 ゾーンヘッダー（左=タイトル＋概況バッジ／右=オンラインドキュメントを開くボタン）、グループ再編（表示言語 → scope 個別設定 → 開発者向けの段階的開示）、scope ごとの解決状態（override/fallback）のバッジ可視化、manifest の選択+ping、両スキン・キーボード操作・長い識別子の折り返し・狭い描画領域でのスクロール表示に対応。
- Preferences 画面自身の文言を、パッケージ自身の翻訳カタログ（scope=`com.kajitaharuka.unity-editor-localization`、`Editor/Localization/` の en/ja）から引くドッグフーディング構成へ変更。表示言語の変更に画面自身が追従し、国際的なアクセシビリティを確保する。
- `package.json` の `documentationUrl`/`changelogUrl`/`licensesUrl` を `https://kajitaharuka.com/products/unity-editor-localization/` 系へ統一（製品ページ URL と一致）。

### Added
- 任意の「Powered by UnityEditorLocalization」クレジット部品を追加。`EditorL10nUi.CreateAttribution()`（クリックで製品ページを開く小さなリンク要素。文言はブランド固定）と、コンパクト言語メニュー（`CreateCompactLocaleMenu` / `CreateLocalizedCompactLocaleMenu`）の `showAttribution` 引数（**既定オン**＝開いたメニュー末尾に控えめ表示、`false` で無効化）。ライセンスは **MIT のまま**でクレジット表示は任意・歓迎（README「クレジット」節）。サンプルにも使用例を追加。
- カタログ検証結果の不足キー（`MissingKey`）行に「+」ボタンを追加し、**その場で該当 locale テーブルへキーを追加**できる（値は defaultLocale からコピーして種にする）。[EditorL10nCatalogWriter](Editor/Core/EditorL10nCatalogWriter.cs) で正準フォーマット書き戻し→再 import→再検証する。クイックfix の文言を 19 言語へ追加。
- `Tools > UnityEditorLocalization > Create Catalog` **カタログ作成ウィザード**を追加。scope・出力フォルダ・defaultLocale・対象ロケールを入力すると、manifest と空の翻訳テーブルの雛形を正準フォーマット（[EditorL10nCatalogWriter](Editor/Core/EditorL10nCatalogWriter.cs)）で生成し、import → 自動 reload → manifest を ping する。手書き JSON の手間を無くし最小構成をすぐ用意できる。入力検証（scope 空/重複・フォルダ妥当・既存 manifest）付き。ウィザード UI 文言を 19 言語へ追加。
- Preferences のカタログ検証結果の各 issue 行を**クリックで由来アセットへジャンプ**（選択+Ping）できるようにした。locale 由来 issue はその locale テーブル、scope 由来（locale 空）の issue は manifest を選択する。ホバーでクリック可能を示す。基盤として公開 API `EditorL10n.TryGetLocaleTablePath(scope, locale, out path)` を追加（tooltip 文言を 19 言語へ）。
- CI / batchmode 用の検証エントリ `EditorL10nValidator.ValidateForCI()` を追加。`-batchmode -quit -executeMethod Kajitaharuka.EditorLocalization.EditorL10nValidator.ValidateForCI` で実行し、エラーがあれば非 0 で終了して CI を止める。既定はエラーのみで失敗し、`-l10nFailOnWarnings` を付けると警告でも失敗扱いにする。対話モードでは Editor を閉じないようログのみ。終了コードの判定は純関数 `ComputeExitCode(errorCount, warningCount, failOnWarnings)` に切り出し、EditMode テストで検証。
- manifest に `fixedTerms`（key 配列）を追加。ファイル名・型名など全ロケールで `defaultLocale` と同値であることが正当な固定語キーを宣言すると、検証の「`defaultLocale` と同値（未翻訳の疑い）」警告を抑止する。`EditorL10nValidator`（in-editor）と翻訳品質スキルの `validate_locale_quality.py` の**双方が同じ manifest の宣言**を読むため、両検証で挙動が一致する（key 過不足・placeholder・連番欠落の検査は従来どおり適用）。リファレンス実装 ExportPackageExtension では `package.json`/`Exporter`/`Exporter {0}` を fixedTerms 化し、誤検知だった検証警告 54 件（3 固定語 × 18 ロケール）を解消。
- Preferences のカタログ検証結果を **scope ごとに分類**して表示。scope ごとの折りたたみグループ（エラーを含む scope は既定で展開、警告のみは折りたたみ）に、件数ピル（`エラー {n}` / `警告 {n}`）、各 issue 行（深刻度を色＋形 `×`/`!` で示すマーカー・由来 locale チップ・詳細メッセージ）を並べる。1 深刻度あたり 30 行で打ち切り、超過分は件数を示して Console（全件出力）へ誘導する。問題の無かった scope 数も控えめに示す。検証結果は **issue の詳細メッセージを含めて**画面の表示言語に追従する。
- Preferences 画面自身の翻訳へ、検証結果分類 UI 用の 5 キー（`catalogs.title` / `catalogs.count.errors` / `catalogs.count.warnings` / `catalogs.groups.clean` / `catalogs.more`）を **19 言語**へ追加（各 67→72 キー）。機械検証（キー過不足・placeholder・未翻訳疑い）をクリア。
- Validator の診断メッセージ 7 種を翻訳キー化（`validation.defaultLocaleEmpty` ほか）し、**19 言語**へ追加（各 72→79 キー）。これにより Preferences の検証結果一覧と Console 出力の**詳細文も表示言語に追従**する。固定語（`defaultLocale`/`key`/`placeholder`/`Console`）・技術トークン（`present=`/`missing=`/`expected=`/`actual=`）・`{0}` 引数は保持。機械検証（キー過不足・placeholder・未翻訳疑い）をクリアし、各言語のネイティブ目線レビューを実施（韓国語の助詞 `이`/`과` は `locale` が ㄹ 終わりのため原案どおり正と確認）。
- Preferences の scope 個別設定で、各 scope の解決順の下に、その scope が対応する言語コードの一覧（`en · ja · zh-Hans · …`）を表示。
- Preferences 画面自身の翻訳を **19 言語**へ拡張。既存 en（defaultLocale）/ja に加え、zh-Hans・zh-Hant・ko・fr・de・it・es-ES・es-419・pt-BR・pt-PT・ru・pl・tr・th・vi・uk・id の 17 言語（各 67 キー）を `Editor/Localization/Locales/` に追加し、manifest へ登録。固定語（scope/locale/manifest/EditorPrefs/Console/CLI/defaultLocale/パス/Claude Code/placeholder/記号）は保持。キー過不足・placeholder・未翻訳疑いの機械検証をクリア（カタログ検証は Errors 0、新規 17 言語の警告 0）。CJK・タイ語など一部は最終的なネイティブレビュー推奨。
- `Tools > UnityEditorLocalization > Settings` メニューを追加。Preferences を開いて UnityEditorLocalization の項目を選択状態にする（`SettingsService.OpenUserPreferences`）。
- 同梱スキル（翻訳ワークフロー / 既存拡張の多言語化連携）を `.claude/skills` と `.agents/skills` へ symlink 登録する `EditorL10nSkillInstaller` を追加。登録先はユーザー（ホーム）/ プロジェクト（リポジトリ直下）から選べる。`Tools > UnityEditorLocalization > AI Agent Skills` のメニューと、`Preferences > UnityEditorLocalization` の「AIエージェント連携スキル」節（登録ボタン＋CLI コマンドの明示・コピー）から実行できる。skills 実体パスは `PackageInfo.resolvedPath` で解決し、埋め込み/PackageCache のどちらでも動作する。登録は macOS/Linux=`ln`、Windows=`mklink /D`（権限が無ければ junction にフォールバック）でクロスプラットフォーム対応し、表示・コピーする CLI コマンドも OS に合わせて出力する。
- `EditorL10n.GetSystemLocale()` と `Get`/`SetSystemLocaleFallbackEnabled()` を公開 API として追加。システム言語タグの供給元は差し替え可能な `SystemLocaleProvider`（テスト用シーム）とし、EditMode テストで解決順と由来を検証する。フォールバックの有効/無効は `EditorPrefs` に保存する（既定は有効）。
- Preferences の表示言語セクションに、検出したシステム言語の表示・システム言語フォールバックの有効/無効トグル・未設定時の解決先を示す動的ヒントを追加。
- Preferences の各 scope カードに、実際に効いている fallback 連鎖（要求 → 親 → defaultLocale）をチップ列で可視化し、実際に翻訳が当たった段を色＋太字で強調する表示を追加。
- Preferences にカタログの Reload / Validate ボタンと、両操作の意味を確認できる説明トグル（ⓘ で HelpBox を開閉、ホバー tooltip でも要約）を追加。検証結果をインラインで表示する。
- エディタ UI 再利用部品 `Editor/UI/EditorL10nUiKit.cs`（2 ゾーンヘッダー・状態バッジ・ドキュメントボタン・セクションカード等）と、両スキンのデザイントークン `Editor/UI/EditorDesignTokens.uss`（Unity 内部 `--unity-*` 変数に非依存）を追加。
- オンラインドキュメント URL を集約する `Editor/UI/EditorL10nDocs.cs` を追加。
- `EditorL10nValidator.ValidateAndLog()` を追加し、メニューと Preferences の検証ボタンの共通入口にした。

## [1.0.0] - 2026-06-26

### Added
- 任意依存（optional）として UnityEditorLocalization を組み込むための規約ドキュメント `Documentation~/OPTIONAL_INTEGRATION.md` と、雛形生成スキル `skills/editor-localization-optional-integration/`（テンプレート同梱）を追加。本体 assembly が基盤を参照せず、基盤が無ければ defaultLocale の単一言語で動作し、基盤導入時に多言語化と言語切替 UI が点灯する 2 アセンブリ方式（Version Define + Define Constraint）を提供する。リファレンス実装は ExportPackageExtension。
- Validator で placeholder 番号の連番欠落と、defaultLocale と同値の未翻訳疑いを警告する検査を追加。
- 未知 scope / 未解決 key を開発時だけ警告できる診断フラグと Preferences のトグルを追加。
- l10n manifest と翻訳テーブル JSON の import / delete / move を検知してカタログを自動リロードする AssetPostprocessor を追加。
- Preferences の scope 個別設定に検索、foldout、defaultLocale と manifest パスの補足表示を追加。
- scope の defaultLocale と manifest パスを取得できる `EditorL10n.TryGetScopeInfo` API を追加。
- UnityEditorLocalization の使い方を自己説明する `Localized Editor Window` UPM sample を追加。

### Changed
- 配布名を `UnityEditorLocalization` に統一。package 名を `com.kajitaharuka.editor-localization` から `com.kajitaharuka.unity-editor-localization` へ、`package.json` の `displayName` を `Editor Localization` から `UnityEditorLocalization` へ変更し、Preferences / メニューのラベル・パスも `Preferences > UnityEditorLocalization` / `Tools > UnityEditorLocalization > …` へ統一。embedded package のディレクトリも `Packages/com.kajitaharuka.unity-editor-localization/` へ改名。namespace（`Kajitaharuka.EditorLocalization`）と公開 API は不変。VCC/ALCOM 表示・利用側ドキュメント・連携パッケージの version define もこの名へ揃える。
- 同梱サンプル（`Localized Editor Window`）の表示文言・メニュー導線・翻訳 scope（`com.kajitaharuka.unity-editor-localization.samples.localized-window`）も新名へ更新。
- パッケージ実体を `Packages/com.kajitaharuka.unity-editor-localization/` 配下のEmbedded UPM package構成へ移行。
- 開発者向け補助資料を `Documentation~/` 配下へ整理し、`package.json` に検索用キーワードとリポジトリ情報を追加。
- `package.json` に MIT ライセンスの metadata を明示。
- host project の `Packages/manifest.json` に `testables` を追加し、package 配下のEditModeテストを実行できる導線を維持。
- `EditorL10n.Tr` / `TryTranslate` の fallback chain を `(locale, defaultLocale)` 単位でキャッシュし、繰り返し呼び出し時の chain 構築アロケーションを削減。

### Fixed
- `EditorL10nUi.BindPropertyField` で、配列/リスト（Foldout で描画される PropertyField）のラベルが言語切替に追従せず生成時の言語のまま固定されていた問題を修正。Foldout のタイトルは `BaseField` のラベル（`labelUssClassName`）ではないため、Foldout 時は `Foldout.text` を更新する経路を追加した。
- UI Toolkit bind helper の `LocaleChanged` 購読を panel attach 中だけに限定し、attach時の再適用、未追加要素の購読リーク、attach/detach 時の多重購読を改善。

## [0.1.0] - 2026-06-22

### Added
- Unity Editor拡張向けの軽量ローカライズ基盤を追加。
- manifestとJSON tableによるscope別カタログ読み込みを追加。
- 文字列タグによるロケール管理とfallbackを追加。
- UI Toolkit向けのbind helperと言語選択Dropdownを追加。
- Inspectorヘッダーやツールバーに置ける汎用のコンパクト言語選択メニューを追加。
- 全 scope 共通のグローバル言語設定 API と Preferences の設定導線、scope 個別設定の解除導線を追加。
- 翻訳key欠落とplaceholder不一致の検証メニューを追加。
- 利用者向けREADME、開発者ガイド、UI Toolkit多言語化Tipsを追加。
