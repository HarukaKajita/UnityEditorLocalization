# UnityEditorLocalization

**Languages:** [English](../../README.md) | [日本語](README.ja.md) | [简体中文](README.zh-Hans.md) | [繁體中文](README.zh-Hant.md) | [한국어](README.ko.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Italiano](README.it.md) | [Español (España)](README.es-ES.md) | Español (Latinoamérica) | [Português (Brasil)](README.pt-BR.md) | [Português (Portugal)](README.pt-PT.md) | [Русский](README.ru.md) | [Polski](README.pl.md) | [Türkçe](README.tr.md) | [ไทย](README.th.md) | [Tiếng Việt](README.vi.md) | [Українська](README.uk.md) | [Bahasa Indonesia](README.id.md)

> Esta es una versión traducida; la versión canónica es el README en inglés ([English](../../README.md)).

Una base de localización ligera y **solo para el Editor (Editor-only)** para extensiones del editor de Unity. Extrae el texto de la interfaz (etiquetas del Inspector, HelpBox, botones, registros de la Console, barras de progreso) de catálogos de traducción por scope, indexados por **scope × locale × key**. Para añadir un idioma basta con añadir un archivo JSON, sin cambios en C#.

- **Solo Editor.** Sin dependencia del código en tiempo de ejecución, de Addressables ni del package Unity Localization.
- **Las locales son etiquetas de cadena**, declaradas en un manifest, no un `enum` de C#. Añadir una locale nunca toca el C#.
- **19 idiomas** vienen incluidos en la UI de configuración y en el catálogo de ejemplo adjunto.
- **Los helpers de UI Toolkit** enlazan `Label` / `Button` / `PropertyField` con keys y siguen los cambios de idioma automáticamente. Se incluyen un menú de locale compacto y un desplegable de locale.
- **Compatible con dependencias opcionales.** Los packages consumidores lo integran como dependencia *opcional*: compilan y funcionan de forma autónoma en un único idioma por defecto y, después, activan una UI multilingüe y un selector de locale cuando este package está instalado, sin ninguna referencia de assembly rígida.
- **Herramientas de catálogo.** Un asistente de creación genera un manifest y tablas vacías. La validación comprueba keys faltantes, la coherencia de los marcadores de posición de `string.Format` y las entradas sospechosas de estar sin traducir (ejecutable en batchmode de CI).
- **Skills para agentes de IA** incluidas para la calidad de traducción y la generación de la integración opcional.

> Este repositorio es la fuente pública con licencia MIT. Se desarrolla como un único embedded UPM
> package en [`Packages/com.kajitaharuka.unity-editor-localization/`](../../Packages/com.kajitaharuka.unity-editor-localization/);
> el resto del repositorio (`Assets/`, `ProjectSettings/`) es solamente el proyecto host que se usa para
> desarrollar y verificar el package dentro del Editor de Unity.

## Instalación

Requiere **Unity 2022.3 o posterior**.

### Package Manager (Git URL)

En *Package Manager → Add package from git URL…*, introduce:

```text
https://github.com/HarukaKajita/UnityEditorLocalization.git?path=Packages/com.kajitaharuka.unity-editor-localization
```

Fija una versión añadiendo una etiqueta de release, p. ej. `#1.2.1`. También puedes añadir la misma URL directamente en `Packages/manifest.json`, dentro de `dependencies`.

### VPM (VCC / ALCOM)

Añade el repositorio VPM y, a continuación, añade UnityEditorLocalization desde él:

```text
https://harukakajita.github.io/vpm-repos/index.json
```

### Booth

En Booth hay disponible un `.zip` empaquetado (con `.unitypackage` / `.tgz` dentro): gratuito, con un nivel de apoyo opcional:

```text
https://genera.booth.pm/items/8617787
```

## Documentación

- **Página de producto (multilingüe):** <https://kajitaharuka.com/products/unity-editor-localization/>
- **Uso del package (README):** [`Packages/com.kajitaharuka.unity-editor-localization/README.md`](../../Packages/com.kajitaharuka.unity-editor-localization/README.md) — visión general, configuración mínima, API, fallback, validación.
- **Guías en profundidad** (`Documentation~/`):
  - `DEVELOPER_GUIDE.md` — orientación de diseño de scope/key para las extensiones consumidoras.
  - `UI_TOOLKIT_LOCALIZATION_TIPS.md` — cómo hacer que UI Toolkit siga los cambios de idioma.
  - `OPTIONAL_INTEGRATION.md` — el patrón de dependencia opcional de dos assemblies.

## Licencia

Licencia MIT. Consulta [`Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md`](../../Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md). La atribución no es obligatoria, pero siempre es bienvenida.
