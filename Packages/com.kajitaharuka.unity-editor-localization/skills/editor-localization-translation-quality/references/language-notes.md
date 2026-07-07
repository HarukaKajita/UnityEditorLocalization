# Language Notes

Use these notes as review prompts, not as a substitute for native-speaker review.

## Translating from a Japanese source

These failure modes recur across *all* target languages when the source text is Japanese. Check them explicitly; they account for most review findings in practice.

- **Punctuation and symbols do not travel.** Japanese middle dot `・`, corner brackets `「」`, lenticular brackets `【】`, and fullwidth parentheses must be replaced by the target language's own conventions (and, for quoted UI text, by the style the project's own locale catalog already uses). Concretely: `・` enumerations become `、` in Chinese only when listing parallel nouns (clause breaks take `，`), a comma/slash elsewhere; quotes become `“ ”` in zh-Hans, `「」` stays legitimate in zh-Hant, `' '` or `" "` in Korean, `« »` in Russian/French, etc. After translating, grep the output for `[・「」【】]` — any hit outside code spans and deliberate Japanese quotations is a bug.
- **Kanji words must not be borrowed by their characters.** Japanese Sino-vocabulary often does not exist, or means something else, in Chinese and Korean. Observed examples: 組入 (not Chinese; use 嵌入/集成/整合), 表記 (zh 表记 does not mean "notation"; use 写法/寫法), 推定 (legal register in zh; use 推测/检测), 節 (ko 절 is unnatural for a UI section; use 섹션), 概況 (ko 개황 is stiff; use 상태 요약), 正準 (ko 정준 is mathematics jargon; use 표준/정규). When translating ja → zh/ko, treat every directly reused two-character compound as suspect until confirmed.
- **Collocations break under literal transfer.** Recurring Japanese phrasings that must be re-expressed by intent, not word-for-word: 「リークを改善」 (say *reduce* the leaks — "improve the leaks" is illogical in most languages), 「（機能が）点灯する」 (say the feature *becomes active / lights up as available*, not literal ignition), 「例外で落ちる」 (say it *crashes / fails with an exception*, not "falls"). When the same source sentence reads oddly in your draft for two different languages, the source idiom is the cause — re-express the meaning.
- **Parenthetical notes re-attach to the nearest noun.** A Japanese pattern like 「X 以外の外部ライブラリ（A・B を含む）に依存しません」 must keep the parenthesis directly after the noun it qualifies. If the translation moves it after "X", the sentence claims the opposite dependency ("depends on X (including A and B)"). Place the note immediately after its head noun, or expand it into a relative clause.
- **Metalinguistic terms are false-friend traps.** 「敬称」/"form of address" (as in German Sie vs du): fr *forme de politesse* / *vouvoiement* (not *adresse*), it *forma di cortesia* (not *allocuzione*), es *tratamiento*, pl *forma grzecznościowa*, pt *forma de tratamento*, vi *cách xưng hô*. Verify any sentence that talks *about* a language rather than in it.

## Regional variants

Variants such as `es-ES`/`es-419`, `pt-BR`/`pt-PT`, and `zh-Hans`/`zh-Hant` should differ where real usage differs (vocabulary, spelling, idiom). But for terse, code-adjacent strings (short diagnostics, messages built mostly from fixed terms) the variants legitimately coincide. Differentiate where natural; do not fabricate differences just to make the files non-identical. Identical variant values are acceptable when no genuine regional difference exists.

## Japanese `ja`

- Do not omit particles in documentation or final user-facing prose.
- Prefer `してください` over `下さい`.
- Avoid awkward punctuation such as `。:`.
- Keep spaces around Latin fixed terms when readability improves: `<FixedTerm> を指定してください。`
- Keep file extensions with the dot when referring to formats, such as `.json`, `.asset`, or `.zip`.
- Write "fallback" as katakana `フォールバック` in user-facing catalog strings, and do not
  mix the Latin spelling into UI text. The always-visible settings UI set the precedent
  (`system.fallback.*`, `pill.fallback`), and katakana reads as natural Japanese in both
  noun and verb use (`フォールバックしました`). Developer-facing docs and technical prose
  (for example "fallback 連鎖" or API names) may keep the Latin term.

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
- Quotes: `zh-Hans` uses `“ ”`; `「」` is legitimate in `zh-Hant` (Taiwan convention). Never carry the Japanese middle dot `・` over; the enumeration mark `、` joins parallel nouns only, and clause-level breaks take `，`.
- Watch mainland-vs-Taiwan register: `接入` reads mainland-only — prefer `整合`/`串接`/`導入` for `zh-Hant`.
- Audit any two-character compound copied from a Japanese source (see "Translating from a Japanese source"); 組入/表記/推定 are known offenders.

## Korean `ko`

- Use polite concise UI endings: `지정하세요`, `내보냅니다`, `취소되었습니다`.
- `에셋` is natural in Unity contexts.
- Keep fixed product/type names in Latin script and attach particles naturally: `<FixedTerm>을`, `<FixedTerm>에서`.
- Choose particles by the Korean *pronunciation* of a Latin term, not its spelling. A word ending in a silent `e` can still be consonant-final when read: `Locale`→로케일 ends in ㄹ, so use consonant-form particles (`이`/`과`/`을`), as in `URL을`, `Google이`. Do not switch to `가`/`와`/`를` just because the Latin spelling ends in a vowel letter.
- Quotes are `' '` / `" "`, never Japanese `「」`. Sino-Korean readings of Japanese compounds are frequent false friends: 절 (節) → 섹션, 개황 (概況) → 상태 요약, 정준 (正準) → 표준/정규, 표기 흔들림 (表記ゆれ) → 표기 불일치.

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
- Address the user consistently in the formal Sie form across the whole catalog:
  `Geben Sie…`, `Wählen Sie…`, `Ihre Erweiterung`. Never mix du forms (`Gib…`, `deine…`)
  into the same catalog — mixed address reads as sloppy in German tool UI, and formal
  address matches the convention of professional developer tools.

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
