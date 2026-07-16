---
name: editor-localization-optional-integration
description: Scaffold optional UnityEditorLocalization integration into a consuming Unity editor package. Use when a package (for example a paid asset) must compile and run standalone in a single default language and then automatically light up multi-language UI plus a locale switcher when UnityEditorLocalization (com.kajitaharuka.unity-editor-localization) is installed, without any hard assembly reference. Generates the bridge seam in the main assembly and a define-constrained integration assembly.
---

# UnityEditorLocalization Optional Integration

Scaffold the two-assembly optional-dependency pattern described in `Documentation~/OPTIONAL_INTEGRATION.md`. The reference implementation is ExportPackageExtension (`Kajitaharuka.ExportPackageExtension`).

The result: the consuming package works standalone in its `defaultLocale`, and—only when UnityEditorLocalization is present—an integration assembly compiles and swaps in multi-language behaviour with a locale switcher. No hard `asmdef` reference and no `dependencies` entry on the base package.

## When to use

- A package that must build and run without UnityEditorLocalization (single language) and gain multi-language + locale switching when it is installed.
- The integration must not add a hard dependency: integration code compiles only when the base package is present (via Version Define + Define Constraint).

## Inputs (gather before generating)

| Token | Meaning | Example |
| --- | --- | --- |
| `{{MAIN_ASMDEF}}` | Main editor assembly name | `Kajitaharuka.ExportPackageExtension` |
| `{{ROOT_NAMESPACE}}` | Main editor namespace | `Kajitaharuka.ExportPackageExtension.Editor` |
| `{{PREFIX}}` | Short facade prefix | `Epe` (yields `EpeL10n`, `EpeL10nUi`) |
| `{{SCOPE}}` | l10n scope = the package `*.l10n-manifest.json` `scope` | `com.kajitaharuka.export-package-extension` |
| `{{MIN_VERSION}}` | Minimum base version that has the needed API | `1.0.0` |
| `{{LOG_TAG}}` | Prefix for `Debug.LogError` | `ExportPackageExtension` |

Derived automatically:

- `{{INTEGRATION_ASMDEF}}` = `{{MAIN_ASMDEF}}.EditorLocalizationIntegration`
- `{{INTEGRATION_NAMESPACE}}` = `{{ROOT_NAMESPACE}}.LocalizationIntegration`
- Shared define symbol = `KAJITAHARUKA_EDITOR_L10N` (fixed; do not rename per package)
- Base package id = `com.kajitaharuka.unity-editor-localization` (fixed)

## Workflow

1. Read `Documentation~/OPTIONAL_INTEGRATION.md` for the pattern, naming rules, and gotchas.
2. Confirm the consuming package already has `*.l10n-manifest.json` and `Locales/*.json` (the `defaultLocale` table is required for standalone display). If missing, create them first (see `Documentation~/DEVELOPER_GUIDE.md`).
3. Copy `templates/` into the consuming package and apply the file mapping + token replacement below.
4. Remove `Kajitaharuka.EditorLocalization` from the **main** assembly asmdef `references`.
5. Route base-API calls in the main assembly through the facades:
   - `EditorL10nUi.*` → `{{PREFIX}}L10nUi.*`
   - `EditorL10n.Tr(scope, ...)` → `{{PREFIX}}L10n.Tr(...)`
   - `EditorL10nUi.CreateLocalizedCompactLocaleMenu(...)` → `{{PREFIX}}L10nUi.CreateCompactLocaleMenu(...)`; treat the result as a nullable `VisualElement` (it is `null` when the base package is absent).
   - Remove `using Kajitaharuka.EditorLocalization;` from every main-assembly file.
6. Do **not** add `com.kajitaharuka.unity-editor-localization` to the main `package.json` `dependencies`. Document it as a recommended optional add-on in the README instead.
7. Verify (see below).

## File mapping (templates → output)

Place under `<package>/Editor/`:

| Template | Output path |
| --- | --- |
| `templates/Localization/IEditorL10nBridge.cs.txt` | `Localization/IEditorL10nBridge.cs` |
| `templates/Localization/EditorL10nRuntime.cs.txt` | `Localization/EditorL10nRuntime.cs` |
| `templates/Localization/DefaultEditorL10nBridge.cs.txt` | `Localization/DefaultEditorL10nBridge.cs` |
| `templates/Localization/ProductL10n.cs.txt` | `Localization/{{PREFIX}}L10n.cs` |
| `templates/Localization/ProductL10nUi.cs.txt` | `Localization/{{PREFIX}}L10nUi.cs` |
| `templates/Localization/ProductL10nAssemblyInfo.cs.txt` | `Localization/{{PREFIX}}L10nAssemblyInfo.cs` |
| `templates/LocalizationIntegration/IntegrationAsmdef.asmdef.txt` | `LocalizationIntegration/{{INTEGRATION_ASMDEF}}.asmdef` |
| `templates/LocalizationIntegration/EditorL10nBridge.cs.txt` | `LocalizationIntegration/EditorL10nBridge.cs` |
| `templates/LocalizationIntegration/EditorL10nBridgeInstaller.cs.txt` | `LocalizationIntegration/EditorL10nBridgeInstaller.cs` |

If the package already has a `{{PREFIX}}L10n` facade, merge `Tr` to delegate to `{{PREFIX}}L10nRuntime`/`EditorL10nRuntime.Bridge` instead of calling `EditorL10n` directly, rather than overwriting unrelated members.

Replace every `{{TOKEN}}` occurrence (in file contents and in output file names) with the gathered values.

## Critical rules

- The asmdef `versionDefines.expression` must be a **bare version** such as `"1.0.0"`. Interval notation like `[1.0.0,)` throws `ExpressionNotValidException` and silently disables the integration (the Define Constraints shows a red mark). A bare version means "this version or newer".
- `defineConstraints` must contain `KAJITAHARUKA_EDITOR_L10N`, the same string the version define produces.
- Keep all code comments in Japanese (project rule).
- Wrap every generated source file in `#if UNITY_EDITOR ... #endif`.
- The main assembly must not reference `Kajitaharuka.EditorLocalization` in any way (asmdef, `using`, or API calls).

## Verify

- **Base absent**: main assembly compiles; UI shows `defaultLocale` text; no locale switcher.
- **Base present**: the integration assembly compiles (no red mark on Define Constraints; `Version Defines` `Expression outcome` is not `Invalid`); the locale switcher appears and the UI follows locale changes; `Tools > UnityEditorLocalization > Validate Catalogs` passes.
- If the switcher does not appear, check that the base package version satisfies `{{MIN_VERSION}}` (a stale git cache can resolve an older version below the minimum).
