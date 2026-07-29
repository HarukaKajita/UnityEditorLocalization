# UnityEditorLocalization

**Languages:** [English](../../README.md) | [日本語](README.ja.md) | [简体中文](README.zh-Hans.md) | [繁體中文](README.zh-Hant.md) | [한국어](README.ko.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Italiano](README.it.md) | [Español (España)](README.es-ES.md) | [Español (Latinoamérica)](README.es-419.md) | [Português (Brasil)](README.pt-BR.md) | [Português (Portugal)](README.pt-PT.md) | [Русский](README.ru.md) | [Polski](README.pl.md) | [Türkçe](README.tr.md) | [ไทย](README.th.md) | [Tiếng Việt](README.vi.md) | [Українська](README.uk.md) | Bahasa Indonesia

> Ini adalah versi terjemahan; versi kanonis adalah README bahasa Inggris ([English](../../README.md)).

Fondasi lokalisasi ringan yang **khusus Editor (Editor-only)** untuk ekstensi editor Unity. Teks UI (label Inspector, HelpBox, tombol, log Console, bilah kemajuan) diambil dari katalog terjemahan per scope yang diindeks berdasarkan **scope × locale × key**. Menambahkan sebuah bahasa cukup dengan menambahkan satu berkas JSON terjemahan dan mendaftarkannya di manifest — tanpa perubahan C#.

- **Khusus Editor.** Tidak bergantung pada kode runtime, Addressables, maupun package Unity Localization.
- **Locale berupa tag string**, dideklarasikan dalam manifest — bukan `enum` C#. Menambahkan locale tidak pernah menyentuh C#.
- **19 bahasa** disertakan di UI pengaturan (katalog contoh bawaan berisi `ja` dan `en`).
- **Helper UI Toolkit** mengikat `Label` / `Button` / `PropertyField` ke key dan otomatis mengikuti perubahan bahasa. Disertakan menu locale ringkas dan dropdown locale.
- **Ramah dependensi opsional.** Package yang memakainya mengintegrasikannya sebagai dependensi *opsional*: mereka dikompilasi dan berjalan mandiri dalam satu bahasa default, lalu menyalakan UI multibahasa dan pengalih locale saat package ini terpasang — tanpa referensi assembly yang kaku.
- **Perkakas katalog.** Wizard pembuatan menyiapkan kerangka manifest dan tabel kosong. Validasi memeriksa key yang hilang, konsistensi placeholder `string.Format`, dan entri yang diduga belum diterjemahkan (dapat dijalankan dalam batchmode CI).
- **Skill agen AI** bawaan untuk kualitas terjemahan dan penyiapan kerangka integrasi opsional.

> Repositori ini adalah sumber publik berlisensi MIT. Ia dikembangkan sebagai satu embedded UPM
> package di [`Packages/com.kajitaharuka.unity-editor-localization/`](../../Packages/com.kajitaharuka.unity-editor-localization/);
> sisa repositori (`Assets/`, `ProjectSettings/`) hanyalah proyek host yang dipakai untuk
> mengembangkan dan memverifikasi package di dalam Unity Editor.

## Instalasi

Membutuhkan **Unity 2022.3 atau yang lebih baru**.

### Package Manager (Git URL)

Di *Package Manager → Add package from git URL…*, masukkan:

```text
https://github.com/HarukaKajita/UnityEditorLocalization.git?path=Packages/com.kajitaharuka.unity-editor-localization
```

Sematkan sebuah versi dengan menambahkan tag rilis, mis. `#<tag>`; ganti `<tag>` dengan nama tag dari Releases repositori. Anda juga dapat menambahkan URL yang sama langsung ke `Packages/manifest.json`, di bawah `dependencies`.

### VPM (VCC / ALCOM)

Tambahkan repositori VPM, lalu tambahkan UnityEditorLocalization dari sana:

```text
https://harukakajita.github.io/vpm-repos/index.json
```

### Booth

Sebuah `.zip` terpaket (berisi `.unitypackage` / `.tgz`) tersedia di Booth — gratis, dengan tingkat dukungan opsional:

```text
https://genera.booth.pm/items/8617787
```

`.unitypackage` tidak menyertakan `Samples~/` maupun `Documentation~/`, karena AssetDatabase Unity tidak menangani folder ber-`~`. Gunakan `.tgz`, git URL, atau VPM jika Anda membutuhkan contoh dan panduan mendalam.

## Dokumentasi

- **Halaman produk (multibahasa):** <https://kajitaharuka.com/products/unity-editor-localization/>
- **Penggunaan package (README):** [`Packages/com.kajitaharuka.unity-editor-localization/README.md`](../../Packages/com.kajitaharuka.unity-editor-localization/README.md) — ikhtisar, penyiapan minimal, API, fallback, validasi.
- **Panduan mendalam** (`Documentation~/`):
  - `DEVELOPER_GUIDE.md` — panduan desain scope/key untuk ekstensi yang memakainya.
  - `UI_TOOLKIT_LOCALIZATION_TIPS.md` — cara membuat UI Toolkit mengikuti perubahan bahasa.
  - `OPTIONAL_INTEGRATION.md` — pola dependensi opsional dua assembly.

## Lisensi

Lisensi MIT. Lihat [`Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md`](../../Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md). Atribusi tidak diwajibkan, tetapi selalu disambut baik.
