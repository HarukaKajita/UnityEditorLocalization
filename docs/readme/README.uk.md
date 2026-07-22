# UnityEditorLocalization

**Languages:** [English](../../README.md) | [日本語](README.ja.md) | [简体中文](README.zh-Hans.md) | [繁體中文](README.zh-Hant.md) | [한국어](README.ko.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Italiano](README.it.md) | [Español (España)](README.es-ES.md) | [Español (Latinoamérica)](README.es-419.md) | [Português (Brasil)](README.pt-BR.md) | [Português (Portugal)](README.pt-PT.md) | [Русский](README.ru.md) | [Polski](README.pl.md) | [Türkçe](README.tr.md) | [ไทย](README.th.md) | [Tiếng Việt](README.vi.md) | Українська | [Bahasa Indonesia](README.id.md)

> Це перекладена версія; основною є англійська версія README ([English](../../README.md)).

Легка основа локалізації, що працює **лише в редакторі (Editor-only)**, для розширень редактора Unity. Текст інтерфейсу (підписи Inspector, HelpBox, кнопки, логи Console, індикатори прогресу) береться з каталогів перекладу за scope, індексованих за **scope × locale × key**. Щоб додати мову, достатньо додати файл JSON — без змін у C#.

- **Лише редактор.** Немає залежності від коду часу виконання, Addressables чи package Unity Localization.
- **Locale — це рядкові теги**, оголошені в manifest, а не C#-`enum`. Додавання locale ніколи не торкається C#.
- **19 мов** постачаються в UI налаштувань і у вбудованому прикладі каталогу.
- **Хелпери UI Toolkit** прив’язують `Label` / `Button` / `PropertyField` до key і автоматично стежать за зміною мови. Включено компактне меню locale та випадний список locale.
- **Дружнє до опційних залежностей.** Пакети, що його використовують, підключають його як *опційну* залежність: вони компілюються та працюють автономно однією типовою мовою, а після встановлення цього package вмикається багатомовний UI та перемикач locale — без жорсткого посилання на assembly.
- **Інструменти для каталогів.** Майстер створення формує manifest і порожні таблиці. Перевірка контролює відсутні key, узгодженість заповнювачів `string.Format` та записи, які, ймовірно, не перекладені (запускається в batchmode CI).
- Вбудовані **навички для ШІ-агентів (skills)** для якості перекладу та генерації опційної інтеграції.

> Цей репозиторій — публічний вихідний код під ліцензією MIT. Він розробляється як єдиний embedded UPM
> package у [`Packages/com.kajitaharuka.unity-editor-localization/`](../../Packages/com.kajitaharuka.unity-editor-localization/);
> решта репозиторію (`Assets/`, `ProjectSettings/`) — лише хост-проєкт, який використовують для
> розробки та перевірки package всередині редактора Unity.

## Встановлення

Потрібен **Unity 2022.3 або новіший**.

### Package Manager (Git URL)

У *Package Manager → Add package from git URL…* введіть:

```text
https://github.com/HarukaKajita/UnityEditorLocalization.git?path=Packages/com.kajitaharuka.unity-editor-localization
```

Закріпіть версію, додавши тег релізу, наприклад `#1.2.1`. Той самий URL можна додати безпосередньо в `Packages/manifest.json`, у розділ `dependencies`.

### VPM (VCC / ALCOM)

Додайте репозиторій VPM, а потім додайте з нього UnityEditorLocalization:

```text
https://harukakajita.github.io/vpm-repos/index.json
```

### Booth

На Booth доступний запакований `.zip` (усередині `.unitypackage` / `.tgz`) — безкоштовно, з необов’язковим рівнем підтримки:

```text
https://genera.booth.pm/items/8617787
```

## Документація

- **Сторінка продукту (багатомовна):** <https://kajitaharuka.com/products/unity-editor-localization/>
- **Використання package (README):** [`Packages/com.kajitaharuka.unity-editor-localization/README.md`](../../Packages/com.kajitaharuka.unity-editor-localization/README.md) — огляд, мінімальне налаштування, API, fallback, перевірка.
- **Докладні посібники** (`Documentation~/`):
  - `DEVELOPER_GUIDE.md` — рекомендації з проєктування scope/key для розширень, що його використовують.
  - `UI_TOOLKIT_LOCALIZATION_TIPS.md` — як змусити UI Toolkit стежити за зміною мови.
  - `OPTIONAL_INTEGRATION.md` — патерн опційної залежності з двох assembly.

## Ліцензія

Ліцензія MIT. Див. [`Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md`](../../Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md). Зазначення авторства не є обов’язковим, але завжди вітається.
