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

After translating, **check every glossary fixed term in the output**. A bare grep for the term itself only proves it survived *somewhere* — it cannot show that an occurrence was translated away. Use one of these mechanical checks instead:

- Compare the term's occurrence count in the translation against the source text (counts should match unless a sentence was legitimately restructured), or
- Grep for the *known translated forms* of the term (`key` → `klucz*`, `Schlüssel`, `ключ*`, 鍵, 키, …) and treat any hit as a suspect.

Fixed-term breaks concentrate in dense prose such as changelogs and long descriptions, where the term appears unquoted mid-sentence and gets translated by momentum. Note that whether a term is fixed at all is a **per-project glossary decision, and it can differ by language**: a project may keep `fallback` in Latin script for most languages while deliberately using a natural word in others (e.g. a native term chosen to match the UI catalog). Do not "fix" a deliberate natural translation back to the Latin term — check the project glossary and the locale catalog before flagging.

## The UI Catalog Is the Source of Truth for Quoted UI Text

When prose (documentation, product pages, release notes) quotes a UI element — a Preferences section name, a status label, a toggle — quote the exact value from that locale's catalog, not a fresh translation. Users will look for the quoted string in the actual UI.

Two consequences:

- **Check the catalog itself first.** If the catalog has a regional-vocabulary slip or an inconsistent term, every downstream document inherits it, and a document-level review cannot fix it. Auditing the catalog (variant purity, one term per concept per language) is a prerequisite step, not an afterthought.
- **Concept words may legitimately split.** The catalog's UI label and the natural prose term can differ (a section is labeled with a noun while prose uses a verb phrase). Use the catalog value only where the text points the user at the UI; use natural prose wording elsewhere, and keep that split consistent.

## Source-Structure Ambiguity Is Fixed in the Source

If several translators independently produce the same misreading — a parenthetical note attaching to the wrong noun, an unclear antecedent — the source sentence is the defect. Rewrite or annotate the source (mark "do not translate literally" spots, move the parenthesis next to its head noun) before translating into many languages; fixing one source line is cheaper than fixing N translations.

When a fixed term legitimately keeps the same value in every locale (including the default locale), declare its key in the manifest's `fixedTerms` array. Both the in-editor C# validator and the validation script then skip the "same as default (possibly untranslated)" warning for it, so the intentional fixed term is not mistaken for a missed translation.

## Quality Bar

A translation is not complete just because every key has a value. Treat it as incomplete if:

- A non-English locale contains English sentences that are not deliberate fixed terms.
- The wording is literal but unnatural for editor UI.
- The sentence hides what the user should do next.
- A warning does not clearly state irreversible behavior.
- A label is so long that it will crowd a compact Inspector or header.
