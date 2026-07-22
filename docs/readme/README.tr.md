# UnityEditorLocalization

**Languages:** [English](../../README.md) | [日本語](README.ja.md) | [简体中文](README.zh-Hans.md) | [繁體中文](README.zh-Hant.md) | [한국어](README.ko.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Italiano](README.it.md) | [Español (España)](README.es-ES.md) | [Español (Latinoamérica)](README.es-419.md) | [Português (Brasil)](README.pt-BR.md) | [Português (Portugal)](README.pt-PT.md) | [Русский](README.ru.md) | [Polski](README.pl.md) | Türkçe | [ไทย](README.th.md) | [Tiếng Việt](README.vi.md) | [Українська](README.uk.md) | [Bahasa Indonesia](README.id.md)

> Bu çevrilmiş bir sürümdür; asıl kaynak İngilizce README’dir ([English](../../README.md)).

Unity editör uzantıları için hafif, **yalnızca Editör’de çalışan (Editor-only)** bir yerelleştirme altyapısı. Arayüz metnini (Inspector etiketleri, HelpBox’lar, düğmeler, Console günlükleri, ilerleme çubukları) **scope × locale × key** ile anahtarlanan, scope başına çeviri kataloglarından çeker. Bir dil eklemek, bir JSON dosyası eklemekle olur — C# değişikliği gerekmez.

- **Yalnızca Editör.** Çalışma zamanı koduna, Addressables’a veya Unity Localization package’ına bağımlılık yoktur.
- **Locale’ler dize etiketleridir**, bir manifest’te bildirilir — C# `enum`’u değildir. Locale eklemek C#’a asla dokunmaz.
- Ayarlar arayüzünde ve birlikte gelen örnek katalogda **19 dil** sunulur.
- **UI Toolkit yardımcıları** `Label` / `Button` / `PropertyField` öğelerini key’lere bağlar ve dil değişikliklerini otomatik olarak izler. Kompakt bir locale menüsü ve bir locale açılır listesi dahildir.
- **İsteğe bağlı bağımlılığa dostane.** Kullanan packages onu *isteğe bağlı* bir bağımlılık olarak entegre eder: tek bir varsayılan dilde bağımsız derlenip çalışır, ardından bu package kurulduğunda çok dilli arayüz ve bir locale değiştirici devreye girer — katı bir assembly referansı olmadan.
- **Katalog araçları.** Bir oluşturma sihirbazı, bir manifest ve boş tablolar iskeleti üretir. Doğrulama; eksik key’leri, `string.Format` yer tutucu tutarlılığını ve çevrilmemiş olabilecek girdileri denetler (CI batchmode’da çalıştırılabilir).
- Çeviri kalitesi ve isteğe bağlı entegrasyon iskeleti için birlikte gelen **yapay zeka aracı becerileri (skills)**.

> Bu depo, MIT lisanslı herkese açık kaynaktır. [`Packages/com.kajitaharuka.unity-editor-localization/`](../../Packages/com.kajitaharuka.unity-editor-localization/)
> altında tek bir embedded UPM package olarak geliştirilir; deponun geri kalanı (`Assets/`, `ProjectSettings/`)
> yalnızca package’ı Unity Editör içinde geliştirmek ve doğrulamak için kullanılan host projedir.

## Kurulum

**Unity 2022.3 veya sonrası** gerekir.

### Package Manager (Git URL)

*Package Manager → Add package from git URL…* içine şunu girin:

```text
https://github.com/HarukaKajita/UnityEditorLocalization.git?path=Packages/com.kajitaharuka.unity-editor-localization
```

Sona bir sürüm etiketi ekleyerek bir sürümü sabitleyin, örn. `#1.2.1`. Aynı URL’yi doğrudan `Packages/manifest.json` içindeki `dependencies` altına da ekleyebilirsiniz.

### VPM (VCC / ALCOM)

VPM deposunu ekleyin, ardından UnityEditorLocalization’ı oradan ekleyin:

```text
https://harukakajita.github.io/vpm-repos/index.json
```

### Booth

Booth’ta paketlenmiş bir `.zip` (içinde `.unitypackage` / `.tgz`) mevcuttur — ücretsiz, isteğe bağlı bir destekçi katmanıyla:

```text
https://genera.booth.pm/items/8617787
```

## Dokümantasyon

- **Ürün sayfası (çok dilli):** <https://kajitaharuka.com/products/unity-editor-localization/>
- **Package kullanımı (README):** [`Packages/com.kajitaharuka.unity-editor-localization/README.md`](../../Packages/com.kajitaharuka.unity-editor-localization/README.md) — genel bakış, en az kurulum, API, geri dönüş (fallback), doğrulama.
- **Ayrıntılı kılavuzlar** (`Documentation~/`):
  - `DEVELOPER_GUIDE.md` — kullanan uzantılar için scope/key tasarım rehberi.
  - `UI_TOOLKIT_LOCALIZATION_TIPS.md` — UI Toolkit’in dil değişikliklerini nasıl izleyeceği.
  - `OPTIONAL_INTEGRATION.md` — iki assembly’li isteğe bağlı bağımlılık deseni.

## Lisans

MIT Lisansı. Bkz. [`Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md`](../../Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md). Atıf zorunlu değildir ama her zaman memnuniyetle karşılanır.
