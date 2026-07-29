# UnityEditorLocalization

**Languages:** [English](../../README.md) | [日本語](README.ja.md) | [简体中文](README.zh-Hans.md) | [繁體中文](README.zh-Hant.md) | [한국어](README.ko.md) | [Français](README.fr.md) | Deutsch | [Italiano](README.it.md) | [Español (España)](README.es-ES.md) | [Español (Latinoamérica)](README.es-419.md) | [Português (Brasil)](README.pt-BR.md) | [Português (Portugal)](README.pt-PT.md) | [Русский](README.ru.md) | [Polski](README.pl.md) | [Türkçe](README.tr.md) | [ไทย](README.th.md) | [Tiếng Việt](README.vi.md) | [Українська](README.uk.md) | [Bahasa Indonesia](README.id.md)

> Dies ist eine übersetzte Fassung; maßgeblich ist die englische README ([English](../../README.md)).

Eine leichtgewichtige, **rein editorseitige (Editor-only)** Lokalisierungsgrundlage für Unity-Editor-Erweiterungen. UI-Text (Inspector-Beschriftungen, HelpBoxes, Buttons, Console-Logs, Fortschrittsbalken) wird aus scope-spezifischen Übersetzungskatalogen bezogen, die per **scope × locale × key** indiziert sind. Eine Sprache fügt man durch Hinzufügen einer Übersetzungs-JSON und einen Eintrag im manifest hinzu — ganz ohne C#-Änderungen.

- **Nur im Editor.** Keine Abhängigkeit von Laufzeitcode, Addressables oder dem Unity-Localization-Package.
- **Locales sind String-Tags**, in einem Manifest deklariert — kein C#-`enum`. Neue Locales berühren nie den C#-Code.
- **19 Sprachen** sind in der Einstellungs-UI enthalten (der mitgelieferte Beispielkatalog enthält `ja` und `en`).
- **UI-Toolkit-Helfer** binden `Label` / `Button` / `PropertyField` an Keys und folgen Sprachwechseln automatisch. Ein kompaktes Locale-Menü und ein Locale-Dropdown sind enthalten.
- **Freundlich zu optionalen Abhängigkeiten.** Nutzende Packages binden es als *optionale* Abhängigkeit ein: Sie kompilieren und laufen eigenständig in einer einzigen Standardsprache und schalten dann eine mehrsprachige UI samt Locale-Umschalter frei, sobald dieses Package installiert ist — ohne harte Assembly-Referenz.
- **Katalog-Werkzeuge.** Ein Erstellungsassistent gerüstet ein Manifest samt leerer Tabellen. Die Validierung prüft fehlende Keys, die Konsistenz der `string.Format`-Platzhalter und mutmaßlich unübersetzte Einträge (im CI-Batchmode ausführbar).
- Mitgelieferte **KI-Agenten-Skills** für Übersetzungsqualität und das Gerüsten der optionalen Integration.

> Dieses Repository ist die öffentliche, MIT-lizenzierte Quelle. Es wird als ein einzelnes embedded UPM
> package unter [`Packages/com.kajitaharuka.unity-editor-localization/`](../../Packages/com.kajitaharuka.unity-editor-localization/) entwickelt;
> der Rest des Repositorys (`Assets/`, `ProjectSettings/`) ist lediglich das Host-Projekt, um das
> Package im Unity-Editor zu entwickeln und zu prüfen.

## Installation

Erfordert **Unity 2022.3 oder neuer**.

### Package Manager (Git URL)

Geben Sie in *Package Manager → Add package from git URL…* ein:

```text
https://github.com/HarukaKajita/UnityEditorLocalization.git?path=Packages/com.kajitaharuka.unity-editor-localization
```

Fixieren Sie eine Version, indem Sie einen Release-Tag anhängen, z. B. `#<tag>` — setzen Sie für `<tag>` einen Tag-Namen aus den Releases des Repositorys ein. Sie können dieselbe URL auch direkt in `Packages/manifest.json` unter `dependencies` eintragen.

### VPM (VCC / ALCOM)

Fügen Sie das VPM-Repository hinzu und fügen Sie anschließend UnityEditorLocalization daraus hinzu:

```text
https://harukakajita.github.io/vpm-repos/index.json
```

### Booth

Ein paketiertes `.zip` (mit `.unitypackage` / `.tgz` darin) ist auf Booth verfügbar — kostenlos, mit einer optionalen Unterstützer-Stufe:

```text
https://genera.booth.pm/items/8617787
```

Das `.unitypackage` enthält weder `Samples~/` noch `Documentation~/` — Unitys AssetDatabase verarbeitet keine `~`-Ordner. Nutzen Sie das `.tgz`, die git URL oder VPM, wenn Sie das Beispiel und die ausführlichen Leitfäden brauchen.

## Dokumentation

- **Produktseite (mehrsprachig):** <https://kajitaharuka.com/products/unity-editor-localization/>
- **Package-Nutzung (README):** [`Packages/com.kajitaharuka.unity-editor-localization/README.md`](../../Packages/com.kajitaharuka.unity-editor-localization/README.md) — Überblick, minimale Einrichtung, API, Fallback, Validierung.
- **Ausführliche Leitfäden** (`Documentation~/`):
  - `DEVELOPER_GUIDE.md` — Leitlinien zum scope/key-Entwurf für nutzende Erweiterungen.
  - `UI_TOOLKIT_LOCALIZATION_TIPS.md` — wie UI Toolkit Sprachwechseln folgt.
  - `OPTIONAL_INTEGRATION.md` — das Muster der optionalen Abhängigkeit mit zwei Assemblies.

## Lizenz

MIT-Lizenz. Siehe [`Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md`](../../Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md). Eine Namensnennung ist nicht erforderlich, aber stets willkommen.
