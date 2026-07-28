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

## Review Output

When reporting a translation review, include:

- Which locale files were checked or changed.
- Missing/extra key and placeholder status.
- Whether English leftovers remain, and why any remaining duplicate is intentional.
- Important terminology decisions.
- Findings reviewed but deliberately not adopted, as a permanent record: locale, key, the finding, and the reason it was declined. Do not let declined findings disappear from the report.
- For declined findings caused by the source text, the condition that reopens them (for example, re-apply after the source wording is fixed), so a later pass can re-verify them without re-deriving the context.
- Any residual risk, especially for languages where native review is still recommended.
