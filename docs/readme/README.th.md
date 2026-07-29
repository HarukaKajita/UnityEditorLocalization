# UnityEditorLocalization

**Languages:** [English](../../README.md) | [日本語](README.ja.md) | [简体中文](README.zh-Hans.md) | [繁體中文](README.zh-Hant.md) | [한국어](README.ko.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Italiano](README.it.md) | [Español (España)](README.es-ES.md) | [Español (Latinoamérica)](README.es-419.md) | [Português (Brasil)](README.pt-BR.md) | [Português (Portugal)](README.pt-PT.md) | [Русский](README.ru.md) | [Polski](README.pl.md) | [Türkçe](README.tr.md) | ไทย | [Tiếng Việt](README.vi.md) | [Українська](README.uk.md) | [Bahasa Indonesia](README.id.md)

> นี่คือฉบับแปล เอกสารต้นฉบับคือ README ภาษาอังกฤษ ([English](../../README.md))

ฐานการทำโลคัลไลเซชันแบบเบาที่ทำงาน**เฉพาะใน Editor (Editor-only)** สำหรับส่วนขยายของ Unity editor ดึงข้อความ UI (ป้ายกำกับใน Inspector, HelpBox, ปุ่ม, ล็อกใน Console, แถบความคืบหน้า) จากแคตาล็อกการแปลแยกตาม scope ซึ่งจัดทำดัชนีด้วย **scope × locale × key** การเพิ่มภาษาทำได้โดยเพิ่มไฟล์ JSON การแปลแล้วลงทะเบียนใน manifest โดยไม่ต้องแก้ C#

- **เฉพาะ Editor** ไม่ขึ้นกับโค้ดรันไทม์ Addressables หรือ Unity Localization package
- **locale เป็นแท็กสตริง** ที่ประกาศไว้ใน manifest ไม่ใช่ `enum` ของ C# การเพิ่ม locale จะไม่แตะต้อง C# เลย
- มาพร้อม **19 ภาษา** ใน UI ตั้งค่า (แคตาล็อกตัวอย่างที่แนบมามี `ja` และ `en`)
- **ตัวช่วย UI Toolkit** ผูก `Label` / `Button` / `PropertyField` เข้ากับ key และติดตามการเปลี่ยนภาษาโดยอัตโนมัติ มีเมนู locale แบบกะทัดรัดและดรอปดาวน์ locale ให้ด้วย
- **เป็นมิตรกับ dependency แบบ optional** package ที่นำไปใช้สามารถผสานเป็น dependency แบบ *optional* ได้ กล่าวคือคอมไพล์และทำงานได้เองด้วยภาษาเริ่มต้นเพียงภาษาเดียว แล้วเมื่อติดตั้ง package นี้ UI หลายภาษาและตัวสลับ locale ก็จะเปิดใช้งาน โดยไม่ต้องมีการอ้างอิง assembly แบบตายตัว
- **เครื่องมือจัดการแคตาล็อก** ตัวช่วยสร้างจะวางโครง manifest และตารางว่าง การตรวจสอบจะเช็ก key ที่ขาดหาย ความสอดคล้องของ placeholder ใน `string.Format` และรายการที่น่าสงสัยว่ายังไม่ได้แปล (รันใน batchmode ของ CI ได้)
- แนบ **สกิลสำหรับเอเจนต์ AI (skills)** เพื่อคุณภาพการแปลและการวางโครงการผสานแบบ optional

> รีโพนี้คือซอร์สสาธารณะภายใต้สัญญาอนุญาต MIT พัฒนาเป็น embedded UPM package เดียวภายใต้ [`Packages/com.kajitaharuka.unity-editor-localization/`](../../Packages/com.kajitaharuka.unity-editor-localization/)
> ส่วนที่เหลือของรีโพ (`Assets/`, `ProjectSettings/`) เป็นเพียงโปรเจกต์โฮสต์ (host project) ที่ใช้
> พัฒนาและตรวจสอบ package ภายใน Unity Editor เท่านั้น

## การติดตั้ง

ต้องใช้ **Unity 2022.3 ขึ้นไป**

### Package Manager (Git URL)

ใน *Package Manager → Add package from git URL…* ให้กรอก:

```text
https://github.com/HarukaKajita/UnityEditorLocalization.git?path=Packages/com.kajitaharuka.unity-editor-localization
```

ตรึงเวอร์ชันได้โดยต่อท้ายด้วยแท็กรีลีส เช่น `#<tag>` โดยแทน `<tag>` ด้วยชื่อแท็กจาก Releases ของรีโพซิทอรี และสามารถเพิ่ม URL เดียวกันนี้ลงใน `Packages/manifest.json` ภายใต้ `dependencies` ได้โดยตรง

### VPM (VCC / ALCOM)

เพิ่มรีโพ VPM แล้วเพิ่ม UnityEditorLocalization จากรีโพนั้น:

```text
https://harukakajita.github.io/vpm-repos/index.json
```

### Booth

มี `.zip` ที่แพ็กไว้แล้ว (ภายในมี `.unitypackage` / `.tgz`) ให้บน Booth — ฟรี พร้อมระดับผู้สนับสนุนแบบเลือกได้:

```text
https://genera.booth.pm/items/8617787
```

`.unitypackage` จะไม่มีทั้ง `Samples~/` และ `Documentation~/` เพราะ AssetDatabase ของ Unity ไม่รองรับโฟลเดอร์ที่มี `~` หากต้องการตัวอย่างและคู่มือเชิงลึกด้วย ให้ติดตั้งด้วย `.tgz`, git URL หรือ VPM

## เอกสาร

- **หน้าสินค้า (หลายภาษา):** <https://kajitaharuka.com/products/unity-editor-localization/>
- **การใช้งาน package (README):** [`Packages/com.kajitaharuka.unity-editor-localization/README.md`](../../Packages/com.kajitaharuka.unity-editor-localization/README.md) — ภาพรวม การตั้งค่าขั้นต่ำ API การ fallback และการตรวจสอบ
- **คู่มือเชิงลึก** (`Documentation~/`):
  - `DEVELOPER_GUIDE.md` — แนวทางออกแบบ scope/key สำหรับส่วนขยายที่นำไปใช้
  - `UI_TOOLKIT_LOCALIZATION_TIPS.md` — วิธีทำให้ UI Toolkit ติดตามการเปลี่ยนภาษา
  - `OPTIONAL_INTEGRATION.md` — รูปแบบ dependency แบบ optional ที่ใช้สอง assembly

## สัญญาอนุญาต

สัญญาอนุญาต MIT ดูรายละเอียดที่ [`Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md`](../../Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md) ไม่บังคับให้ระบุเครดิต แต่ยินดีเสมอหากจะระบุ
