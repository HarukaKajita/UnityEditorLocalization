# UnityEditorLocalization

**Languages:** English | [日本語](docs/readme/README.ja.md) | [简体中文](docs/readme/README.zh-Hans.md) | [繁體中文](docs/readme/README.zh-Hant.md) | [한국어](docs/readme/README.ko.md) | [Français](docs/readme/README.fr.md) | [Deutsch](docs/readme/README.de.md) | [Italiano](docs/readme/README.it.md) | [Español (España)](docs/readme/README.es-ES.md) | [Español (Latinoamérica)](docs/readme/README.es-419.md) | [Português (Brasil)](docs/readme/README.pt-BR.md) | [Português (Portugal)](docs/readme/README.pt-PT.md) | [Русский](docs/readme/README.ru.md) | [Polski](docs/readme/README.pl.md) | [Türkçe](docs/readme/README.tr.md) | [ไทย](docs/readme/README.th.md) | [Tiếng Việt](docs/readme/README.vi.md) | [Українська](docs/readme/README.uk.md) | [Bahasa Indonesia](docs/readme/README.id.md)

A lightweight, **Editor-only** localization foundation for Unity editor extensions.
Pull UI text (Inspector labels, HelpBoxes, buttons, Console logs, progress bars) from
per-scope translation catalogs keyed by **scope × locale × key**. Add a language by adding a
translation JSON and registering it in the manifest — no C# changes.

- **Editor-only.** No dependency on runtime code, Addressables, or the Unity Localization package.
- **Locales are string tags**, declared in a manifest — not a C# `enum`. New locales never touch C#.
- **19 languages** ship in the settings UI (the bundled sample catalog ships `ja` and `en`).
- **UI Toolkit helpers** bind `Label` / `Button` / `PropertyField` to keys and follow language changes automatically. A compact locale menu and a locale dropdown are included.
- **Optional-dependency friendly.** Consuming packages integrate it as an *optional* dependency: they compile and run standalone in a single default language, then light up multi-language UI and a locale switcher when this package is installed — with no hard assembly reference.
- **Catalog tooling.** A creation wizard scaffolds a manifest + empty tables. Validation checks missing keys, `string.Format` placeholder consistency, and untranslated-suspect entries (runnable in CI batchmode).
- **Bundled AI-agent skills** for translation quality and optional-integration scaffolding.

> This repository is the public MIT-licensed source. It is developed as a single embedded UPM
> package under [`Packages/com.kajitaharuka.unity-editor-localization/`](Packages/com.kajitaharuka.unity-editor-localization/);
> the rest of the repository (`Assets/`, `ProjectSettings/`) is only the host project used to
> develop and verify the package inside the Unity Editor.

## Installation

Requires **Unity 2022.3 or later**.

### Package Manager (Git URL)

In *Package Manager → Add package from git URL…*, enter:

```text
https://github.com/HarukaKajita/UnityEditorLocalization.git?path=Packages/com.kajitaharuka.unity-editor-localization
```

Pin a version by appending a release tag, e.g. `#<tag>` — replace `<tag>` with a tag name from the
repository's Releases. You can also add the same URL directly to `Packages/manifest.json` under
`dependencies`.

### VPM (VCC / ALCOM)

Add the VPM repository, then add UnityEditorLocalization from it:

```text
https://harukakajita.github.io/vpm-repos/index.json
```

### Booth

A packaged `.zip` (`.unitypackage` / `.tgz` inside) is available on Booth — free, with an optional
supporter tier:

```text
https://genera.booth.pm/items/8617787
```

The `.unitypackage` carries neither `Samples~/` nor `Documentation~/` — Unity's AssetDatabase does not
handle `~` folders. Use the `.tgz`, the git URL, or VPM if you want the sample and the in-depth guides.

## Documentation

- **Product page (multilingual):** <https://kajitaharuka.com/products/unity-editor-localization/>
- **Package usage (README):** [`Packages/com.kajitaharuka.unity-editor-localization/README.md`](Packages/com.kajitaharuka.unity-editor-localization/README.md) — overview, minimal setup, API, fallback, validation.
- **In-depth guides** (`Documentation~/`):
  - `DEVELOPER_GUIDE.md` — scope/key design guidance for consuming extensions.
  - `UI_TOOLKIT_LOCALIZATION_TIPS.md` — how to make UI Toolkit follow language changes.
  - `OPTIONAL_INTEGRATION.md` — the two-assembly optional-dependency pattern.

## License

MIT License. See [`Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md`](Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md).
Attribution is not required but always welcome.
