# UnityEditorLocalization

**Languages:** [English](../../README.md) | [日本語](README.ja.md) | [简体中文](README.zh-Hans.md) | [繁體中文](README.zh-Hant.md) | [한국어](README.ko.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Italiano](README.it.md) | [Español (España)](README.es-ES.md) | [Español (Latinoamérica)](README.es-419.md) | [Português (Brasil)](README.pt-BR.md) | [Português (Portugal)](README.pt-PT.md) | [Русский](README.ru.md) | Polski | [Türkçe](README.tr.md) | [ไทย](README.th.md) | [Tiếng Việt](README.vi.md) | [Українська](README.uk.md) | [Bahasa Indonesia](README.id.md)

> To jest wersja przetłumaczona; wersją źródłową jest README w języku angielskim ([English](../../README.md)).

Lekka, **działająca wyłącznie w Edytorze (Editor-only)** podstawa lokalizacji dla rozszerzeń edytora Unity. Tekst interfejsu (etykiety Inspectora, HelpBox, przyciski, logi Console, paski postępu) jest pobierany z katalogów tłumaczeń per scope, indeksowanych według **scope × locale × key**. Aby dodać język, wystarczy dodać plik JSON z tłumaczeniami i zarejestrować go w manifeście — bez zmian w C#.

- **Tylko Edytor.** Brak zależności od kodu uruchomieniowego, Addressables czy package Unity Localization.
- **Locale to tagi tekstowe**, deklarowane w manifeście — a nie `enum` w C#. Dodanie locale nigdy nie dotyka C#.
- **19 języków** dostarczanych jest w UI ustawień (dołączony przykładowy katalog zawiera `ja` i `en`).
- **Pomocniki UI Toolkit** wiążą `Label` / `Button` / `PropertyField` z key i automatycznie podążają za zmianami języka. Dołączono kompaktowe menu locale oraz listę rozwijaną locale.
- **Przyjazne zależnościom opcjonalnym.** Korzystające packages integrują je jako *opcjonalną* zależność: kompilują się i działają samodzielnie w jednym języku domyślnym, a po zainstalowaniu tego package rozświetlają wielojęzyczne UI i przełącznik locale — bez sztywnego odwołania do assembly.
- **Narzędzia katalogowe.** Kreator tworzenia generuje manifest i puste tabele. Walidacja sprawdza brakujące key, spójność symboli zastępczych `string.Format` oraz wpisy podejrzane o brak tłumaczenia (uruchamialne w batchmode CI).
- Dołączone **umiejętności dla agentów AI (skills)** dla jakości tłumaczeń i generowania opcjonalnej integracji.

> To repozytorium jest publicznym źródłem na licencji MIT. Jest rozwijane jako pojedynczy embedded UPM
> package w [`Packages/com.kajitaharuka.unity-editor-localization/`](../../Packages/com.kajitaharuka.unity-editor-localization/);
> reszta repozytorium (`Assets/`, `ProjectSettings/`) to jedynie projekt hosta służący do
> rozwijania i weryfikowania package w Edytorze Unity.

## Instalacja

Wymaga **Unity 2022.3 lub nowszego**.

### Package Manager (Git URL)

W *Package Manager → Add package from git URL…* wpisz:

```text
https://github.com/HarukaKajita/UnityEditorLocalization.git?path=Packages/com.kajitaharuka.unity-editor-localization
```

Przypnij wersję, dodając tag wydania, np. `#<tag>`; w miejsce `<tag>` wstaw nazwę tagu z sekcji Releases repozytorium. Ten sam URL możesz też dodać bezpośrednio w `Packages/manifest.json`, w sekcji `dependencies`.

### VPM (VCC / ALCOM)

Dodaj repozytorium VPM, a następnie dodaj z niego UnityEditorLocalization:

```text
https://harukakajita.github.io/vpm-repos/index.json
```

### Booth

Na Booth dostępny jest spakowany `.zip` (z `.unitypackage` / `.tgz` w środku) — darmowy, z opcjonalnym progiem wsparcia:

```text
https://genera.booth.pm/items/8617787
```

`.unitypackage` nie zawiera ani `Samples~/`, ani `Documentation~/`, ponieważ AssetDatabase Unity nie obsługuje folderów z `~`. Jeśli potrzebujesz przykładu i szczegółowych przewodników, użyj `.tgz`, git URL albo VPM.

## Dokumentacja

- **Strona produktu (wielojęzyczna):** <https://kajitaharuka.com/products/unity-editor-localization/>
- **Użycie package (README):** [`Packages/com.kajitaharuka.unity-editor-localization/README.md`](../../Packages/com.kajitaharuka.unity-editor-localization/README.md) — przegląd, minimalna konfiguracja, API, fallback, walidacja.
- **Szczegółowe przewodniki** (`Documentation~/`):
  - `DEVELOPER_GUIDE.md` — wskazówki projektowe scope/key dla rozszerzeń korzystających.
  - `UI_TOOLKIT_LOCALIZATION_TIPS.md` — jak sprawić, by UI Toolkit podążał za zmianami języka.
  - `OPTIONAL_INTEGRATION.md` — wzorzec opcjonalnej zależności z dwoma assembly.

## Licencja

Licencja MIT. Zobacz [`Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md`](../../Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md). Atrybucja nie jest wymagana, ale zawsze mile widziana.
