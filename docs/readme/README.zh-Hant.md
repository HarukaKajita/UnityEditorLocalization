# UnityEditorLocalization

**Languages:** [English](../../README.md) | [日本語](README.ja.md) | [简体中文](README.zh-Hans.md) | 繁體中文 | [한국어](README.ko.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Italiano](README.it.md) | [Español (España)](README.es-ES.md) | [Español (Latinoamérica)](README.es-419.md) | [Português (Brasil)](README.pt-BR.md) | [Português (Portugal)](README.pt-PT.md) | [Русский](README.ru.md) | [Polski](README.pl.md) | [Türkçe](README.tr.md) | [ไทย](README.th.md) | [Tiếng Việt](README.vi.md) | [Українська](README.uk.md) | [Bahasa Indonesia](README.id.md)

> 這是翻譯版本，正式版本以英文 README（[English](../../README.md)）為準。

一個面向 Unity 編輯器擴充功能的**僅編輯器（Editor-only）**輕量本地化基礎庫。它依 **scope × locale × key** 從各 scope 的翻譯目錄中讀取 UI 文字（Inspector 標籤、HelpBox、按鈕、Console 記錄、進度條）。新增一種語言只需新增一個 JSON 檔案，無需改動 C#。

- **僅編輯器。** 不依賴執行時程式碼、Addressables，也不依賴 Unity Localization package。
- **locale 是字串標籤**，在 manifest 中宣告，而非 C# 的 `enum`。新增 locale 永遠不需要改動 C#。
- 設定 UI 與隨附範例目錄內建 **19 種語言**。
- **UI Toolkit 輔助方法**可將 `Label` / `Button` / `PropertyField` 綁定到 key，並自動跟隨語言切換。同時提供一個精簡的 locale 選單與一個 locale 下拉選單。
- **對可選依賴友善。** 使用方 package 可將其作為*可選*依賴整合：在未安裝本 package 時，以單一預設語言獨立編譯並執行；安裝本 package 後，即可點亮多語言 UI 與 locale 切換器——無需硬性的組件參照。
- **目錄工具。** 建立精靈可產生 manifest 與空白資料表的骨架。驗證會檢查缺少的 key、`string.Format` 佔位符一致性，以及疑似未翻譯的項目（可在 CI 的 batchmode 中執行）。
- 內建用於翻譯品質與可選整合骨架產生的 **AI 代理技能（skills）**。

> 本儲存庫是採用 MIT 授權公開的原始碼。它作為一個位於 [`Packages/com.kajitaharuka.unity-editor-localization/`](../../Packages/com.kajitaharuka.unity-editor-localization/)
> 之下的單一 embedded UPM package 進行開發；儲存庫中的其餘部分（`Assets/`、`ProjectSettings/`）
> 只是用於在 Unity Editor 中開發與驗證該 package 的宿主專案（host project）。

## 安裝

需要 **Unity 2022.3 或更新版本**。

### Package Manager (Git URL)

在 *Package Manager → Add package from git URL…* 中輸入：

```text
https://github.com/HarukaKajita/UnityEditorLocalization.git?path=Packages/com.kajitaharuka.unity-editor-localization
```

在結尾附加發行標籤即可鎖定版本，例如 `#1.2.1`。你也可以將同一 URL 直接加入 `Packages/manifest.json` 的 `dependencies` 中。

### VPM (VCC / ALCOM)

加入以下 VPM 儲存庫，然後從中加入 UnityEditorLocalization：

```text
https://harukakajita.github.io/vpm-repos/index.json
```

### Booth

Booth 上提供打包好的 `.zip`（內含 `.unitypackage` / `.tgz`）——免費，並附有一個可選的支持者級別。

```text
https://genera.booth.pm/items/8617787
```

## 文件

- **商品頁面（多語言）:** <https://kajitaharuka.com/products/unity-editor-localization/>
- **Package 使用說明（README）:** [`Packages/com.kajitaharuka.unity-editor-localization/README.md`](../../Packages/com.kajitaharuka.unity-editor-localization/README.md) —— 概覽、最小設定、API、回退、驗證。
- **深入指南**（`Documentation~/`）:
  - `DEVELOPER_GUIDE.md` —— 面向使用方擴充功能的 scope/key 設計指引。
  - `UI_TOOLKIT_LOCALIZATION_TIPS.md` —— 如何讓 UI Toolkit 跟隨語言切換。
  - `OPTIONAL_INTEGRATION.md` —— 雙組件的可選依賴模式。

## 授權

MIT License。詳見 [`Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md`](../../Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md)。不要求標示出處，但始終歡迎。
