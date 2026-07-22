# UnityEditorLocalization

**Languages:** [English](../../README.md) | [日本語](README.ja.md) | [简体中文](README.zh-Hans.md) | [繁體中文](README.zh-Hant.md) | [한국어](README.ko.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Italiano](README.it.md) | [Español (España)](README.es-ES.md) | [Español (Latinoamérica)](README.es-419.md) | [Português (Brasil)](README.pt-BR.md) | [Português (Portugal)](README.pt-PT.md) | Русский | [Polski](README.pl.md) | [Türkçe](README.tr.md) | [ไทย](README.th.md) | [Tiếng Việt](README.vi.md) | [Українська](README.uk.md) | [Bahasa Indonesia](README.id.md)

> Это переведённая версия; эталоном является README на английском языке ([English](../../README.md)).

Лёгкая **работающая только в редакторе (Editor-only)** основа локализации для расширений редактора Unity. Текст интерфейса (подписи Inspector, HelpBox, кнопки, логи Console, индикаторы прогресса) берётся из каталогов перевода по scope, индексируемых по **scope × locale × key**. Чтобы добавить язык, достаточно добавить JSON-файл — без изменений в C#.

- **Только редактор.** Никакой зависимости от кода времени выполнения, Addressables или package Unity Localization.
- **Locale — это строковые теги**, объявляемые в manifest, а не C#-`enum`. Добавление locale никогда не затрагивает C#.
- **19 языков** поставляются в UI настроек и во встроенном примере каталога.
- **Хелперы UI Toolkit** привязывают `Label` / `Button` / `PropertyField` к key и автоматически следуют за сменой языка. Включены компактное меню locale и выпадающий список locale.
- **Дружелюбно к опциональным зависимостям.** Использующие packages подключают её как *опциональную* зависимость: они компилируются и работают автономно на одном языке по умолчанию, а после установки этого package загорается многоязычный UI и переключатель locale — без жёсткой ссылки на assembly.
- **Инструменты для каталогов.** Мастер создания формирует manifest и пустые таблицы. Проверка контролирует отсутствующие key, согласованность плейсхолдеров `string.Format` и записи, предположительно не переведённые (запускается в batchmode CI).
- Встроенные **навыки для ИИ-агентов (skills)** для качества перевода и генерации опциональной интеграции.

> Этот репозиторий — публичный исходный код под лицензией MIT. Он разрабатывается как единый embedded UPM
> package в [`Packages/com.kajitaharuka.unity-editor-localization/`](../../Packages/com.kajitaharuka.unity-editor-localization/);
> остальная часть репозитория (`Assets/`, `ProjectSettings/`) — лишь хост-проект, используемый для
> разработки и проверки package внутри редактора Unity.

## Установка

Требуется **Unity 2022.3 или новее**.

### Package Manager (Git URL)

В *Package Manager → Add package from git URL…* введите:

```text
https://github.com/HarukaKajita/UnityEditorLocalization.git?path=Packages/com.kajitaharuka.unity-editor-localization
```

Закрепите версию, добавив тег релиза, например `#1.2.1`. Тот же URL можно добавить напрямую в `Packages/manifest.json`, в раздел `dependencies`.

### VPM (VCC / ALCOM)

Добавьте репозиторий VPM, затем добавьте из него UnityEditorLocalization:

```text
https://harukakajita.github.io/vpm-repos/index.json
```

### Booth

На Booth доступен упакованный `.zip` (внутри `.unitypackage` / `.tgz`) — бесплатно, с необязательным уровнем поддержки:

```text
https://genera.booth.pm/items/8617787
```

## Документация

- **Страница продукта (многоязычная):** <https://kajitaharuka.com/products/unity-editor-localization/>
- **Использование package (README):** [`Packages/com.kajitaharuka.unity-editor-localization/README.md`](../../Packages/com.kajitaharuka.unity-editor-localization/README.md) — обзор, минимальная настройка, API, fallback, проверка.
- **Подробные руководства** (`Documentation~/`):
  - `DEVELOPER_GUIDE.md` — рекомендации по проектированию scope/key для использующих расширений.
  - `UI_TOOLKIT_LOCALIZATION_TIPS.md` — как заставить UI Toolkit следовать за сменой языка.
  - `OPTIONAL_INTEGRATION.md` — паттерн опциональной зависимости из двух assembly.

## Лицензия

Лицензия MIT. См. [`Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md`](../../Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md). Указание авторства не требуется, но всегда приветствуется.
