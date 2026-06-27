# Terminology And Style

## Build The Project Glossary First

Before translating, identify terms that are intentionally stable for the current tool:

- Product, package, class, command, menu, and component names.
- File formats, extensions, protocol names, schema names, and generated artifact names.
- Config file names, field names, JSON/YAML keys, environment variables, and CLI flags.
- Unity API or editor UI nouns when they are product surfaces: `ScriptableObject`, `Project`, `Inspector`, `EditorPrefs`, `Unity Package Manager`.
- Placeholders and format markers: `{0}`, `{1}`, `<Name>`, `<Date>`, `<Time>`.

Keep these terms unchanged unless the current project already documents a localized name. Do not inherit fixed-term decisions from another extension just because this skill was first created during that work.

## Translate These When Natural

- Action verbs: export, import, add, remove, open, select, delete, keep, include, exclude, refresh, validate, generate.
- UI section names: settings, preview, target, source, output, dependency, result, validation.
- User-facing object names when a natural Unity term exists in that language.
- Error explanations and warnings.

## Labels, Tooltips, Errors, Logs

- Labels: compact noun phrases. Avoid full sentences.
- Buttons: verb-first where natural in the target language.
- Tooltips: state the action and result. Mention irreversible behavior explicitly.
- Errors: explain the failure and, when possible, what setting to change.
- Logs: may be longer and more diagnostic than labels.

## Common Fixed-Term Candidates

- Keep extension-style product names, class names, and command identifiers in their original script unless the project says otherwise.
- Keep file extensions with their dot when referring to the format, such as `.json`, `.asset`, `.prefab`, `.zip`, `.tgz`, and `.unitypackage`.
- Keep configuration keys and field names unchanged, such as `name`, `version`, `id`, `path`, or project-specific schema fields.
- Keep code-like inline tokens inside diagnostic and log messages in English even though they look like words: `key=value` markers and bracketed debug fields such as `present=[...]`, `missing=[...]`, `expected=[...]`, `actual=[...]`. Translate only the human sentence around them.
- Translate `asset` only where it sounds natural. Otherwise keep `asset` to match Unity user vocabulary.
- Avoid flag icons for languages. Use native names and locale tags.

If a term is both a natural user-facing noun and a code/type-like concept, decide once per project and apply the same treatment across all locales.

When a fixed term legitimately keeps the same value in every locale (including the default locale), declare its key in the manifest's `fixedTerms` array. Both the in-editor C# validator and the validation script then skip the "same as default (possibly untranslated)" warning for it, so the intentional fixed term is not mistaken for a missed translation.

## Quality Bar

A translation is not complete just because every key has a value. Treat it as incomplete if:

- A non-English locale contains English sentences that are not deliberate fixed terms.
- The wording is literal but unnatural for editor UI.
- The sentence hides what the user should do next.
- A warning does not clearly state irreversible behavior.
- A label is so long that it will crowd a compact Inspector or header.
