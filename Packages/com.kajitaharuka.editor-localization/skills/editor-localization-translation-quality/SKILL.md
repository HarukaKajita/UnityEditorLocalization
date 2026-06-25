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
   - Do not translate locale tags.
   - Treat repeated source-language values as suspicious unless the key is a deliberate fixed term in the project glossary.

5. Check language-specific risks.
   - Read `references/language-notes.md` when working on any supported locale beyond quick typo fixes.
   - Pay attention to regional variants such as `es-ES` vs `es-419` and `pt-BR` vs `pt-PT`.

6. Validate mechanically before reporting done.
   - Run `scripts/validate_locale_quality.py` against the locale directory.
   - Also run the project’s existing catalog validator or compile/test gate when available.
   - Investigate every unexpected English duplicate, placeholder mismatch, missing key, and extra key.

## Validation Script

Run from this skill directory. Pass the locale directory as an absolute path or as a path relative to your current working directory, and replace allowed fixed-term keys for the current project:

```bash
python3 scripts/validate_locale_quality.py \
  path/to/Locales \
  --default-locale en \
  --allow-same-key key.for.deliberate.fixed.term
```

Use `--allow-same-key` only for deliberate fixed terms. Do not allow broad categories just to silence failures.

## Review Output

When reporting a translation review, include:

- Which locale files were checked or changed.
- Missing/extra key and placeholder status.
- Whether English leftovers remain, and why any remaining duplicate is intentional.
- Important terminology decisions.
- Any residual risk, especially for languages where native review is still recommended.
