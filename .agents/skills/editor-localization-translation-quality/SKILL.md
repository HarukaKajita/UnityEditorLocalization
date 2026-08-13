---
name: editor-localization-translation-quality
description: Maintain high-quality translations for Unity EditorLocalization-based editor extensions and tools. Use when Codex needs to review, add, edit, or validate locale JSON files, improve untranslated or unnatural UI text, establish project-specific terminology, check placeholders, or keep multilingual Inspector/UI wording consistent and usable across any EditorLocalization consumer.
---

# UnityEditorLocalization Translation Quality

Use this skill for EditorLocalization locale work, especially `*.l10n-manifest.json` and `Locales/{locale}.json` files used by Unity editor extensions or other Unity tooling. Do not assume any specific target product; derive terminology and UI intent from the current project.

## Workflow

1. Identify the translation surface before editing.
   - Read the manifest to find `scope`, `defaultLocale`, locale tags, native names, and table paths.
   - Read the default locale table and the key constants or UI code that consume those keys.
   - Group keys by UI surface: compact labels, tooltips, warnings, errors, logs, progress text.
   - Note the target tool, user workflow, and any space-constrained surfaces such as toolbar headers, status badges, table cells, menu items, and Inspector rows.

2. Establish terminology before translating.
   - Read `references/terminology-and-style.md` when adding or changing terms, product names, file format names, or UI wording policy.
   - Build a small project-specific glossary from the manifest, default locale, type/class names, menus, docs, and existing UI before editing locale files.
   - Before translating new keys, dump the existing translations of related keys across all locales with `scripts/dump_catalog_terms.py` (see below) and match the catalog's established choices — the settled word for "asset", locales that keep `Editor` as a fixed term, full-width vs half-width colons, the apostrophe character — so new keys do not introduce a terminology split inside the same file.
   - Keep product/type names and file-format identifiers stable unless the current project explicitly has localized names.
   - **For ja / ko / zh-Hans / zh-Hant, use Unity's own official Editor translation.** Those four are the
     only languages Unity localizes the Editor UI into, so the term is not a judgement call — run
     `scripts/check_unity_official_terms.py path/to/Locales` and read `references/unity-official-terms.md`.
     When Unity's Editor UI and its documentation disagree, **the Editor UI wins**: the extension's text is
     rendered inside Unity's own window next to Unity's own strings, and the docs are read at a different time
     on a different screen. This is not hypothetical — Simplified Chinese "Asset" is `资产` in the Editor
     (245:45) and `资源` in the docs (0 occurrences of `资产`), and 61 strings across four repositories had
     picked the docs' word.

3. Translate for the UI, not word-for-word.
   - Labels and badges should be short.
   - Tooltips should explain the action result.
   - Errors should state what failed and what the user can change.
   - Logs can be slightly more explicit than UI labels.
   - Prefer natural editor/tool wording over literal dictionary output.

4. Preserve machine-sensitive text.
   - Keep placeholder numbers exactly aligned with the default locale: `{0}`, `{1}`, etc.
   - Keep file paths, extensions, JSON keys, code identifiers, package names, and user values unchanged unless the key explicitly describes them for humans.
   - Keep code-like inline tokens inside a message in English even though they look like words: `key=value` markers and bracketed debug fields such as `present=[...]`, `missing=[...]`, `expected=[...]`, `actual=[...]`. Translate only the human sentence around them.
   - Do not translate locale tags.
   - Treat repeated source-language values as suspicious, but accept that some translations legitimately equal the source: short status words (Spanish `Error` == English `Error`), brand/format/identifier names, and deliberate glossary fixed terms. Declare those keys in the manifest's `fixedTerms` array so that **both** the in-editor C# validator and this script skip the same-as-default warning for them (`--allow-same-key` remains for ad-hoc script runs). Record why, and do not reword a correct translation just to differ from the source.

5. Check language-specific risks.
   - Read `references/language-notes.md` when working on any supported locale beyond quick typo fixes.
   - When the source language is Japanese, apply the "Translating from a Japanese source" section of that file (punctuation leakage, kanji false-friend borrowings, broken collocations, parenthesis re-attachment, metalinguistic false friends) to every target locale.
   - Pay attention to regional variants such as `es-ES` vs `es-419` and `pt-BR` vs `pt-PT`. Differentiate where usage genuinely differs; do not fabricate differences for terse technical strings where the variants legitimately coincide.
   - When a native speaker or another agent reviews and proposes a change, verify the underlying grammar rule before applying it — reviewers can be confidently wrong. Example: a reviewer may "fix" a Korean particle after a Latin term by its spelling, but particle choice follows pronunciation (`Locale`→로케일 ends in ㄹ, a consonant, so `이`/`과`, not `가`/`와`). Apply only changes you can justify.

6. Validate mechanically before reporting done.
   - Run `scripts/validate_locale_quality.py` against the locale directory.
   - Run `scripts/check_unity_official_terms.py` against the locale directory: it compares ja / ko /
     zh-Hans / zh-Hant against Unity's own Editor translation, which no other gate looks at. Run it
     **across every repository in the product family**, not just the one you edited — the 2026-08-09 sweep
     found the Traditional Chinese word for "Asset" split three-to-one *between sibling repositories*, which
     is invisible from inside any single one of them.
   - Run `scripts/check_message_identifiers.py` (see "Identifier reality check" below): it catches
     messages that name a type or attribute **that does not exist**, which no other gate sees.
   - When C# code calls the catalog through a `Tr(...)` facade, also run
     `scripts/check_tr_placeholder_parity.py` (see below): it catches call sites that pass fewer
     format arguments than the catalog template's `{n}` placeholders require — a runtime
     `FormatException` / raw-`{n}` bug that neither the compiler nor the locale validator detects
     (generalized from the 2026-07 UMPD companion routing, 198 call sites).
   - Also run the project’s existing catalog validator or compile/test gate when available.
   - Investigate every unexpected English duplicate, placeholder mismatch, missing key, and extra key.
   - Check each glossary fixed term in every locale output for accidental translation (compare occurrence counts with the source, or grep the known translated forms — a bare grep for the term itself proves nothing; see `references/terminology-and-style.md`). When the source is Japanese, grep `[・「」【】]` for leaked source punctuation, excluding `「」` for zh-Hant where corner brackets are native (see `references/language-notes.md`).

## Localizing diagnostic and log messages

Validator, importer, and log messages are often hard-coded in one language. To make them follow the display language like the rest of the UI:

- Restructure each message into a translation key plus format arguments instead of a pre-built string. Hold the message in code as a *kind* (enum) + args, and format it at display time through the catalog (e.g. `Tr(scope, key, args)`), so both the Console and inline UI follow the current language.
- Pass machine tokens as arguments, not as translated words: key names, locale tags, and placeholder lists go into `{0}`/`{1}`/`{2}`; keep `present=`/`missing=` style markers literal in the template.
- Add the new message keys to every locale. A self-validating catalog will otherwise report them as missing, and the placeholder number set must be identical across locales. Insert them with `scripts/insert_catalog_keys.py` (see below), not a hand-written one-off script.
- Choose the scope deliberately: a tool's own diagnostics belong in that tool's own catalog scope, not the scope being validated.

## Bulk key insertion

When adding new keys to every locale, use `scripts/insert_catalog_keys.py`. Do not write a throwaway insertion script per task: catalog JSON formatting differs between repositories (2-space multi-line entries vs single-line entries), and ad-hoc scripts have repeatedly broken or nearly broken the formatting.

```bash
python3 scripts/insert_catalog_keys.py \
  --locales-dir path/to/Locales \
  --anchor existing.key.to.insert.after \
  --data new-keys.json \
  --dry-run
```

- `--data` is a translation JSON file of the form `{"key": {"locale": "value", ...}, ...}`. Keys are inserted in file order, into every locale file, immediately after `--anchor`.
- The script auto-detects each file's formatting and fails closed: a file matching neither supported format aborts the whole run with an example of that file's formatting. Follow the abort message (extend the script or normalize the file deliberately); do not hand-edit around it in a way that silently breaks the file's formatting.
- Before writing it verifies that the anchor exists, the keys do not, and the data covers every locale; after inserting it re-verifies JSON validity, zero duplicate keys, identical key sets, and identical placeholder number sets across locales. Run with `--dry-run` first, then without.

## Terminology dump

`scripts/dump_catalog_terms.py` prints matching entries across all locales in one table, selected by key or by value substring:

```bash
python3 scripts/dump_catalog_terms.py --locales-dir path/to/Locales --keys skills.title,other.key
python3 scripts/dump_catalog_terms.py --locales-dir path/to/Locales --grep Assets [--ignore-case]
```

Use it for:

- Sampling established terminology before translating new keys (workflow step 2).
- Verbatim UI names in documents: when a product page, README, or manual references an Inspector field or button name in bold, the bold text must match that locale's actual catalog label character-for-character — dump the label and compare. A paraphrase breaks findability: an es document saying "en Assets" while the real label reads "dentro de Assets" leaves the user unable to locate the field.

## Validation Script

Run from this skill directory. Pass the locale directory as an absolute path or as a path relative to your current working directory, and replace allowed fixed-term keys for the current project:

```bash
python3 scripts/validate_locale_quality.py \
  path/to/Locales \
  --default-locale en \
  --allow-same-key key.for.deliberate.fixed.term
```

Use `--allow-same-key` only for deliberate fixed terms. Do not allow broad categories just to silence failures. The script also auto-loads `fixedTerms` from the `*.l10n-manifest.json` next to the `Locales/` directory — the same declaration the C# `EditorL10nValidator` reads — and treats those keys like `--allow-same-key`. Prefer declaring fixed terms in the manifest (one source, both validators) over passing flags.

## Tr-call / placeholder parity check

For consumers that call the catalog through a facade (e.g. `EpeL10n.Tr`, `TaeL10n.Tr`, `UmpdL10n.Tr`):

```bash
python3 scripts/check_tr_placeholder_parity.py \
  --catalog path/to/Locales/<defaultLocale>.json \
  --src path/to/Packages/<package-root> \
  --method UmpdL10n.Tr [--method OtherFacade.Tr]
```

- Fails (exit 1) when a call passes fewer arguments than the template's `max({n})+1`, or uses a key missing from the catalog. Surplus arguments and statically unresolvable keys (dynamic variables) are reported as warnings only.
- Resolves TextKey constants (`const string Xxx = "...";`) from the `--src` tree automatically; string-literal keys work as-is.
- Placeholder count parity *across locales* is `validate_locale_quality.py`'s job; this script covers the *code side* of the same contract. Run both.

The per-locale line reports `placeholder=` (placeholder set mismatch vs the default locale) and `gap=` (placeholder numbers that are not consecutive from `0`) as separate counts, mirroring the C# `EditorL10nValidator` so the two gates can cross-check. Add `--report-variant-duplicates` to print (non-failing) the keys whose value is identical across the locales of a regional-variant group (`es-ES`/`es-419`, `pt-BR`/`pt-PT`, `zh-Hans`/`zh-Hant`, grouped by primary subtag), to review for copy-paste left-overs — remembering that identical values are legitimate for terse technical strings.

## UI ラベルを文中で参照するときは、そのラベルの訳を渡す（2026-07-28 制定）

文言の中で**画面上のボタン名・タブ名・メニュー名に言及する**とき、その名前を**文字列で書き込んではいけない。**
そのラベル自体が別のキーで翻訳されているなら、**en 以外の全ロケールで、文言が指す名前と画面の表示が食い違う。**

実例（UMPD・2026-07-28）: 警告文が掃除先を `Open Material Inspector` と英語で固定していたが、そのボタンは
`matInspector.button.openMaterialInspector` で全ロケール翻訳済みだった（ja は「マテリアルインスペクターを開く」）。
**ja を含む 18 ロケールで、文言が存在しないボタン名を指していた。**

正しい形は、**ラベルをプレースホルダで受け取り、呼び出し側でそのキーの訳を渡す**:

```csharp
var inspectorButton = UmpdL10n.Tr(UmpdTextKey.MatInspectorButtonOpenMaterialInspector);
message = UmpdL10n.Tr(UmpdTextKey.GenTexWarningUnreadableSubAsset, dataAssetName, inspectorButton);
```

カタログ側は `「{1}」から …` のようにプレースホルダで書く。こうすると**ボタン名を後から訳し直しても文言が自動で追従**する。

**機械検査**: 既存の値に、他のキーの `defaultLocale` の値が**文字列として埋まっていないか**を探す。
埋まっていれば、その参照はプレースホルダへ移すべき候補である。

```bash
python3 - <<'CHECK'
import json, pathlib, sys
base = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else "Locales")
default = json.loads((base / "en.json").read_text(encoding="utf-8"))
values = {e["key"]: e["value"] for e in default["entries"]}
# UI ラベルらしいキー（短く、プレースホルダを持たない）を参照候補にする
labels = {k: v for k, v in values.items() if len(v) <= 40 and "{" not in v}
for key, value in values.items():
    for label_key, label in labels.items():
        if label_key != key and len(label) >= 8 and label in value:
            print(f"{key} が {label_key} の値『{label}』を文字列で含んでいる")
CHECK
```

## Identifier reality check (2026-07-29 制定)

**文言が名乗っている型名・属性名が、実装に存在するかを機械で確かめる。** 翻訳の質でも placeholder でもないので、`Validate Catalogs` も `validate_locale_quality.py` も**この層を 1 件も見ていない**。

実例（UMPD・2026-07-29）:

- `[UberToggle]` のエラーが自分を `[Toggle]` と名乗っていた。**`[Toggle]` は Unity 標準の別 drawer** で、ShaderLab にそう書いてもこのパッケージの機能にはならない。**利用者を別物へ誘導していた。**
- `[UberEnum]` のエラーが `MaterialEnum` という型名を出していた。この名前は**リポジトリのどこにも存在しない**。

どちらも 19 ロケール全部へ正しく翻訳されており、**19 言語すべてが誤った名前を伝えていた**。翻訳としては完璧で、内容が嘘だった。

```bash
python3 scripts/check_message_identifiers.py \
  --catalog path/to/Locales/<defaultLocale>.json \
  --src path/to/Packages/<package-root> \
  [--allow SomeConceptName]
```

- 角括弧属性 `[Xxx]` / ドット区切り参照 `Xxx.Yyy` / PascalCase の識別子を既定ロケールの値から抜き、`--src` 配下の**実装だけ**（`.cs` / `.shader` / `.cginc` / `.hlsl` / `.asmdef`）を照合する。ヒット 0 件を error にする。
- **ドキュメントは根拠にしない。** CHANGELOG が「`MaterialEnum` という誤りを直した」と書いているだけで実在扱いになり、肝心の誤りを見逃す（実装中にこれで一度取り逃した）。
- **部分一致で判定しない。** `[Toggle]` は `UberToggle` の部分文字列なので、素の `in` では実在すると誤判定する（これも一度取り逃した）。角括弧属性は書かれ方（`[X]` / `[X(` / `class XDrawer`）で、それ以外は語境界で照合する。
- ヒットしても意味が正しいとは限らない。**明らかな嘘の検出器であって、承認器ではない。** 対応型の列挙（「Float / Range / Int にのみ使える」）が実装の判定と一致するかは、別途ソースを読んで確かめる。UMPD ではこれも食い違っていた（実装は `Int` を受け付けるのに「non-float property」と書いていた）。

## 分担して展開するときは、先に変更を分類する（2026-07-29 制定）

正本ロケールの変更を N ロケールへ展開する作業を**複数のエージェント／人で分担すると、グループの境界に沿って方針が割れる。** 各担当は正本を参照して訳すため、正本を書いた人の判断（語の追加・法の選択）がそのまま伝播し、参照したグループにだけ乗るからである。

**着手前に、変更したキーを 2 つに分類して渡す。**

| 分類 | 例 | 他ロケールの扱い |
|---|---|---|
| **意味変更** | 「Half のときのみ有効」→「Half のときのみ編集できる」／`[Toggle]`→`[UberToggle]`／対応型の訂正 | **全ロケールが追従必須** |
| **正本言語だけの文法修正** | `empty or null`→`null or empty`／冠詞の追加／`explicit-deleted`→`deleted explicitly` | **既に同義なら据え置き。**無意味な差分を作らない |

分類を渡さないと、担当ごとに「既に同義なので据え置き」の判断がばらつく。実測（2026-07-29 / TAE・UMPD）では、分類を渡したことで **28 件の無駄な書き換えを回避**できた一方、分類が曖昧だったキーでは判断が割れた。

**正本ロケールを先に確定する。** `defaultLocale` はパッケージごとに違う（TAE は `ja`、UMPD は `en`）。**正本が `en` のパッケージでは `ja` も一般ロケールなので担当に含める。**「ja と en は修正済み」と一律に書くと、`en` 正本のパッケージの `ja` が全員の担当から漏れる（実際に漏れた）。

**書き戻しは行単位のツールで行わせる。** `json.load` → `json.dump` はインデントとエスケープの揺れでファイル全体を差分にする。`scripts/insert_catalog_keys.py`（新規キー）か、既存値の差し替えなら該当する `"value":` の 1 行だけを置換するツールを渡す。

**「訳さない」指定は識別子に限る。** `[UberToggle]` や `Float` は識別子だが、`Sub-Assets` / `Material` は普通名詞で、多くのロケールが自言語の語を使っている。過剰に指定すると訳文の質を下げる方向に働く（実際に 1 グループが既存訳を英語へ戻してしまった）。

### 分担後は必ず横断検証を 1 本走らせる

分担の境界・全キーの placeholder 一致・実装との突き合わせをまとめて見る**独立した検証**を最後に置く。2026-07-29 の実測では、要修正 2 件・改善推奨 7 件が**すべてこの検証でしか出なかった**。境界に出た不整合の型:

- 正本が原文の情報を落とし（「線形に」）、正本を参照したグループだけが落とした（11/19 ロケール）
- 正本が法を変え（推奨→命令形）、参照したグループだけが追従した（6/19 ロケール）
- スラブ語で `канале(ах)` のような括弧併記は**語として成立しない**（前置格は単数と複数で語幹末が違い、展開すると `каналеах` になる）。`string.Join` で複数入りうる placeholder は、格変化のある言語では**複数形一本化**が安全
- 終止符の有無が正本だけずれる。とくに `{0}` がファイルパスのときの末尾ピリオドは、Console 表示が `…/Foo.png.` になり貼り付け事故を招く

## 屈折語で「訳さない語」を文中へ置くときは、格を担う名詞を前に置く（2026-08-14 制定）

ロシア語・ウクライナ語・ポーランド語・チェコ語のような屈折語では、**前置詞の直後に
コードスパンを裸で置くと格が破綻する**。`zasób` のような保護語は「カタログ上の実際の語形」を
示すために辞書形のまま置く必要があるので、屈折させて解決することはできない。

**正しい対処は、格を担う普通名詞を前に置いて文を組み直すこと。**

```
NG  ... kłóciły się z `zasób` ...          （z は造格を要求するのに主格が来ている）
OK  ... kłóciły się z terminem `zasób` ...  （terminem が格を担い、コードスパンは辞書形のまま）
```

同じ形が既に使われていることが多い（`z folderem \`Resources\`` / `przy zapożyczeniu \`asset\``）。
**新しく書くときは、同じ文の中の既存パターンに合わせる。**

裏取り（2026-08-14）: ウッチ大学ポーランド語相談室は「斜格の位置で主格形を残したいなら、
格を担う普通名詞を前置しなければならない」とし、ポーランド語編集者の言語相談も
「引用語・外国語固有名も原則として屈折させる。屈折させられない／不自然な場合は**文を組み直す**」
としている。2 出典が一致して、前置詞の直後に裸の主格を置く選択肢は挙げていない。

**機械で洗える。** 屈折語のカタログ・ページに対し「前置詞 × 直後がコードスパン」で
パターンマッチすれば候補が出る。指摘と修正案（分類名詞の前置）はどちらも定型化できる。

## 用語の是非を「調査」で決めるときの作法（2026-08-09 制定）

「この語はその言語のネイティブ開発者が実際にどう書くか」を調べて決める場面がある。
2026-08-09 に 19 ロケール分を実際に調べて、**調査そのものが壊れる型**が複数出たので手順にする。

### 証拠の序列

1. **Unity 公式のエディタ翻訳**（ja / ko / zh-Hans / zh-Hant のみ）。上記の
   `references/unity-official-terms.md` を見る。ここに答えがあるなら**それ以上調べない**。
2. **Unity 公式ドキュメントのその言語版**。エディタ翻訳と食い違ったらエディタ翻訳を採る。
3. **その言語の用語当局に「その語が無い」こと**。置換語を作っていない＝借用語のままが実態、という
   強い不在証拠になる。**当局は 1 つで足りない** — フランス語なら本国の Commission d'enrichissement
   だけでなく**ケベックの OQLF / GDT** まで見る（実測: 本国の用語集 55 語に `asset` は無く、
   OQLF の `asset` は会計語義だけでゲーム開発語義が存在しなかった。片方だけだと取りこぼす）。
4. **その言語圏の Unity 記事・フォーラムの実例**。人間が書いたものに限る。
5. 一般的な借用傾向。**これ単独で結論にしない。**

### 検索結果の要約を根拠にしない

**Web 検索の AI 要約が原文を書き換えている。** 実測（2026-08-09）:

```
検索要約: Unity'de "varlıklar" (assets), texture, obje...
実ページ: Unity3D 'de assets'ler texture, obje...      ← assets'ler を varlıklar に置換していた

検索要約: แอสเซท หมายถึงโฟลเดอร์และไฟล์ต่างๆ ...
実ページ: 4. Project / 5.Assets คือส่วนที่เอาไว้เก็บไฟล์ ...   ← 主語ごと別物
```

**用語の綴りそのものが争点の調査では、要約は使えない。**原ページを開き、争点のトークンを
**逐語で**抜き出す。イタリア語の複数形（`asset` か `assets` か）を要約経由で引いて、
結論を左右する `-s` が落ちた例も出た。

### 機械翻訳のページを実例に数えない

反例として量のあるページが機械翻訳だったことが複数回あった。見分け方:

- ページのメタデータに `ms.translationtype: MT`（Microsoft Learn）
- 全言語スイッチャがあり、原文へのフッタ帰属がある
- 本文中に別言語のパスやリンクが漏れている（`/fr/design-patterns/...` が
  ポーランド語ページに混ざっていた）
- 同一ページ内で同じ語の訳が 3 通りに揺れる

機械翻訳は**その言語の開発者が書いたもの**ではないので、実例として数えない。
ただし「その言語には定着した訳語が無い」ことの傍証にはなる。

### 取得できなかった出典を「確認済み」に混ぜない

403 / 404 / 証明書エラーで開けなかった出典は、結論の重みづけから**外す**。
代わりの一次資料へ振り替える（実測: Accademia della Crusca が 403 だったので
Treccani の一般規範へ差し替えた）。「出典を挙げたが読んでいない」が一番危ない。

### 調べた結果は必ず対照表へ落とす

調査は高くつく（19 ロケール分で数十の一次資料を当たった）。同じ問いを二度調べないよう、
結論は `references/unity-official-terms.json` の `knownWrong` か
`references/terminology-and-style.md` の用語欄へ**その場で書き戻す**。

## Review Output

When reporting a translation review, include:

- Which locale files were checked or changed.
- Missing/extra key and placeholder status.
- Whether English leftovers remain, and why any remaining duplicate is intentional.
- Important terminology decisions.
- Findings reviewed but deliberately not adopted, as a permanent record: locale, key, the finding, and the reason it was declined. Do not let declined findings disappear from the report.
- For declined findings caused by the source text, the condition that reopens them (for example, re-apply after the source wording is fixed), so a later pass can re-verify them without re-deriving the context.
- Any residual risk, especially for languages where native review is still recommended.
