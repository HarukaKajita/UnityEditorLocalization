# Language Notes

Use these notes as review prompts, not as a substitute for native-speaker review.

## Regional variants

Variants such as `es-ES`/`es-419`, `pt-BR`/`pt-PT`, and `zh-Hans`/`zh-Hant` should differ where real usage differs (vocabulary, spelling, idiom). But for terse, code-adjacent strings (short diagnostics, messages built mostly from fixed terms) the variants legitimately coincide. Differentiate where natural; do not fabricate differences just to make the files non-identical. Identical variant values are acceptable when no genuine regional difference exists.

## Japanese `ja`

- Do not omit particles in documentation or final user-facing prose.
- Prefer `してください` over `下さい`.
- Avoid awkward punctuation such as `。:`.
- Keep spaces around Latin fixed terms when readability improves: `<FixedTerm> を指定してください。`
- Keep file extensions with the dot when referring to formats, such as `.json`, `.asset`, or `.zip`.

## English `en`

- Badges should be concise: `Fix required` is clearer than `Needs fix`.
- Prefer natural tool wording over literal phrasing, such as `Export as...` rather than `Export into...` when describing an output format.
- Use consistent capitalization for fixed concepts and product/type names.

## Chinese `zh-Hans` / `zh-Hant`

- Use simplified/traditional terminology consistently:
  - `zh-Hans`: `导出`, `文件`, `设置`, `依赖项`, `资源`.
  - `zh-Hant`: `匯出`, `檔案`, `設定`, `相依項目`, `資產`.
- Keep UI labels short; Chinese can often be more compact than English.
- Keep fixed product/type names in Latin script.

## Korean `ko`

- Use polite concise UI endings: `지정하세요`, `내보냅니다`, `취소되었습니다`.
- `에셋` is natural in Unity contexts.
- Keep fixed product/type names in Latin script and attach particles naturally: `<FixedTerm>을`, `<FixedTerm>에서`.
- Choose particles by the Korean *pronunciation* of a Latin term, not its spelling. A word ending in a silent `e` can still be consonant-final when read: `Locale`→로케일 ends in ㄹ, so use consonant-form particles (`이`/`과`/`을`), as in `URL을`, `Google이`. Do not switch to `가`/`와`/`를` just because the Latin spelling ends in a vowel letter.

## Spanish `es-ES` / `es-419`

- Keep regional variation:
  - `es-ES`: `Añadir`, `Ajustes` can be natural.
  - `es-419`: `Agregar`, `Configuración` can be more broadly natural.
- English technical adjectives may be acceptable as fixed terms only when the project glossary says so; otherwise use natural Spanish when space allows.
- Avoid over-translating fixed project concepts, product names, and type-like names.

## Portuguese `pt-BR` / `pt-PT`

- Keep regional vocabulary:
  - `pt-BR`: `arquivo`, `prévia`, `sistema operacional`.
  - `pt-PT`: `ficheiro`, `pré-visualização`, `sistema operativo`.
- Use `excluir` for destructive deletion in `pt-BR`; `eliminar` in `pt-PT`.

## French `fr`

- Use `asset` when it is clearer for Unity users than a generic `ressource`.
- Use `package` for Unity package contexts.
- Avoid overly long button labels; move detail to tooltip.

## German `de`

- Expect long compounds. Keep labels short and move detail to tooltip.
- `Assets` is acceptable in Unity contexts.
- Use formal imperative for user instructions: `Geben Sie...`

## Italian `it`

- `asset` is common in Unity contexts.
- Use `Esporta`, `Aggiungi`, `Rimuovi` for actions.
- Keep irreversible warnings explicit.

## Polish `pl`

- Inflection matters. If keeping a Latin fixed term, inflect surrounding words rather than altering the fixed term where possible.
- `zasób` is acceptable for asset, but avoid making labels too long.

## Russian `ru`

- Prefer concise imperative: `Укажите...`, `Измените...`.
- Keep fixed Latin terms when they are product/type names.
- Be careful with genitive after numbers only when visible counts are embedded in a natural sentence.

## Ukrainian `uk`

- Prefer `вивід`/`вихідний` consistently for output.
- Use `асет` for Unity context if it reads more naturally than a generic resource.
- Keep warnings direct and explicit.

## Turkish `tr`

- Turkish suffixes on Latin fixed terms can read awkwardly. Prefer surrounding grammar that avoids ambiguous apostrophes unless needed.
- `asset`, `artifact`, and project-specific fixed terms may remain in Latin script for Unity/tooling context.
- Use clear action verbs: `dışa aktar`, `ekle`, `kaldır`, `sil`.

## Thai `th`

- Do not rely on spaces for word boundaries inside Thai prose, but keep spaces around Latin fixed terms.
- Keep Latin fixed terms for tool concepts.
- Make warnings explicit because compact Thai labels can hide severity.

## Vietnamese `vi`

- Keep tone marks.
- `tệp` is good for file; `đường dẫn` for path.
- `asset`, `artifact`, and fixed tool names may remain in Latin script for Unity/tooling context.

## Indonesian `id`

- Use `path`, `output`, and `asset` when they match common developer UI vocabulary.
- Use `hapus` for delete, `simpan` or `pertahankan` for keep depending on UI brevity.
- Keep sentences direct and avoid overly formal wording.
