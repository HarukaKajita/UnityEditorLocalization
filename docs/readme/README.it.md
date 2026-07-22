# UnityEditorLocalization

**Languages:** [English](../../README.md) | [日本語](README.ja.md) | [简体中文](README.zh-Hans.md) | [繁體中文](README.zh-Hant.md) | [한국어](README.ko.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | Italiano | [Español (España)](README.es-ES.md) | [Español (Latinoamérica)](README.es-419.md) | [Português (Brasil)](README.pt-BR.md) | [Português (Portugal)](README.pt-PT.md) | [Русский](README.ru.md) | [Polski](README.pl.md) | [Türkçe](README.tr.md) | [ไทย](README.th.md) | [Tiếng Việt](README.vi.md) | [Українська](README.uk.md) | [Bahasa Indonesia](README.id.md)

> Questa è una versione tradotta; il documento di riferimento è il README in inglese ([English](../../README.md)).

Una base di localizzazione leggera e **solo per l'Editor (Editor-only)** per le estensioni dell'editor Unity. Il testo dell'interfaccia (etichette dell'Inspector, HelpBox, pulsanti, log della Console, barre di avanzamento) viene prelevato da cataloghi di traduzione per scope, indicizzati per **scope × locale × key**. Per aggiungere una lingua basta aggiungere un file JSON — senza modifiche al C#.

- **Solo Editor.** Nessuna dipendenza dal codice a runtime, da Addressables o dal package Unity Localization.
- **Le locale sono tag stringa**, dichiarate in un manifest — non un `enum` C#. Aggiungere una locale non tocca mai il C#.
- **19 lingue** sono incluse nell'UI delle impostazioni e nel catalogo di esempio fornito.
- **Gli helper UI Toolkit** collegano `Label` / `Button` / `PropertyField` alle key e seguono automaticamente i cambi di lingua. Sono inclusi un menu locale compatto e un menu a discesa per la locale.
- **Adatto alle dipendenze opzionali.** I package che lo usano lo integrano come dipendenza *opzionale*: compilano e funzionano in modo autonomo in un'unica lingua predefinita, poi attivano un'UI multilingue e un selettore di locale una volta installato questo package — senza alcun riferimento di assembly rigido.
- **Strumenti per i cataloghi.** Una procedura guidata di creazione genera un manifest e tabelle vuote. La validazione controlla le key mancanti, la coerenza dei segnaposto `string.Format` e le voci sospette non tradotte (eseguibile in batchmode nella CI).
- **Skill per agenti IA** inclusi per la qualità di traduzione e la generazione dell'integrazione opzionale.

> Questo repository è la fonte pubblica con licenza MIT. È sviluppato come un unico embedded UPM
> package sotto [`Packages/com.kajitaharuka.unity-editor-localization/`](../../Packages/com.kajitaharuka.unity-editor-localization/);
> il resto del repository (`Assets/`, `ProjectSettings/`) è soltanto il progetto host usato per
> sviluppare e verificare il package all'interno dell'Editor Unity.

## Installazione

Richiede **Unity 2022.3 o successivo**.

### Package Manager (Git URL)

In *Package Manager → Add package from git URL…*, inserisci:

```text
https://github.com/HarukaKajita/UnityEditorLocalization.git?path=Packages/com.kajitaharuka.unity-editor-localization
```

Fissa una versione aggiungendo un tag di release, ad es. `#1.2.1`. Puoi anche aggiungere lo stesso URL direttamente in `Packages/manifest.json`, sotto `dependencies`.

### VPM (VCC / ALCOM)

Aggiungi il repository VPM, quindi aggiungi UnityEditorLocalization da esso:

```text
https://harukakajita.github.io/vpm-repos/index.json
```

### Booth

Su Booth è disponibile un `.zip` pacchettizzato (con `.unitypackage` / `.tgz` all'interno) — gratuito, con un livello di sostegno facoltativo:

```text
https://genera.booth.pm/items/8617787
```

## Documentazione

- **Pagina prodotto (multilingue):** <https://kajitaharuka.com/products/unity-editor-localization/>
- **Uso del package (README):** [`Packages/com.kajitaharuka.unity-editor-localization/README.md`](../../Packages/com.kajitaharuka.unity-editor-localization/README.md) — panoramica, configurazione minima, API, fallback, validazione.
- **Guide approfondite** (`Documentation~/`):
  - `DEVELOPER_GUIDE.md` — indicazioni di progettazione scope/key per le estensioni che lo usano.
  - `UI_TOOLKIT_LOCALIZATION_TIPS.md` — come far seguire a UI Toolkit i cambi di lingua.
  - `OPTIONAL_INTEGRATION.md` — il pattern di dipendenza opzionale a due assembly.

## Licenza

Licenza MIT. Vedi [`Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md`](../../Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md). L'attribuzione non è richiesta ma sempre gradita.
