# UnityEditorLocalization

**Languages:** [English](../../README.md) | [日本語](README.ja.md) | 简体中文 | [繁體中文](README.zh-Hant.md) | [한국어](README.ko.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Italiano](README.it.md) | [Español (España)](README.es-ES.md) | [Español (Latinoamérica)](README.es-419.md) | [Português (Brasil)](README.pt-BR.md) | [Português (Portugal)](README.pt-PT.md) | [Русский](README.ru.md) | [Polski](README.pl.md) | [Türkçe](README.tr.md) | [ไทย](README.th.md) | [Tiếng Việt](README.vi.md) | [Українська](README.uk.md) | [Bahasa Indonesia](README.id.md)

> 这是翻译版本，正式版本以英文 README（[English](../../README.md)）为准。

一个面向 Unity 编辑器扩展的**仅编辑器（Editor-only）**轻量本地化基础库。它按 **scope × locale × key** 从各 scope 的翻译目录中读取 UI 文本（Inspector 标签、HelpBox、按钮、Console 日志、进度条）。添加一种语言只需新增一个 JSON 文件，无需改动 C#。

- **仅编辑器。** 不依赖运行时代码、Addressables，也不依赖 Unity Localization package。
- **locale 是字符串标签**，在 manifest 中声明，而不是 C# 的 `enum`。新增 locale 永远不需要改动 C#。
- 设置 UI 与随附示例目录内置 **19 种语言**。
- **UI Toolkit 辅助方法**可将 `Label` / `Button` / `PropertyField` 绑定到 key，并自动跟随语言切换。同时提供一个紧凑的 locale 菜单和一个 locale 下拉框。
- **对可选依赖友好。** 使用方 package 可将其作为*可选*依赖集成：在未安装本 package 时，以单一默认语言独立编译并运行；安装本 package 后，即可点亮多语言 UI 与 locale 切换器——无需硬性的程序集引用。
- **目录工具。** 创建向导可生成 manifest 与空表的脚手架。校验会检查缺失的 key、`string.Format` 占位符一致性，以及疑似未翻译的条目（可在 CI 的 batchmode 中运行）。
- 内置用于翻译质量与可选集成脚手架的 **AI 智能体技能（skills）**。

> 本仓库是采用 MIT 许可证公开的源码。它作为一个位于 [`Packages/com.kajitaharuka.unity-editor-localization/`](../../Packages/com.kajitaharuka.unity-editor-localization/)
> 之下的单一 embedded UPM package 进行开发；仓库中的其余部分（`Assets/`、`ProjectSettings/`）
> 只是用于在 Unity Editor 中开发和验证该 package 的宿主工程（host project）。

## 安装

需要 **Unity 2022.3 或更高版本**。

### Package Manager (Git URL)

在 *Package Manager → Add package from git URL…* 中输入：

```text
https://github.com/HarukaKajita/UnityEditorLocalization.git?path=Packages/com.kajitaharuka.unity-editor-localization
```

在末尾追加发布标签即可锁定版本，例如 `#1.2.1`。你也可以将同一 URL 直接添加到 `Packages/manifest.json` 的 `dependencies` 中。

### VPM (VCC / ALCOM)

添加以下 VPM 仓库，然后从中添加 UnityEditorLocalization：

```text
https://harukakajita.github.io/vpm-repos/index.json
```

### Booth

Booth 上提供打包好的 `.zip`（内含 `.unitypackage` / `.tgz`）——免费，并带有一个可选的支持者档位。

```text
https://genera.booth.pm/items/8617787
```

## 文档

- **商品页面（多语言）:** <https://kajitaharuka.com/products/unity-editor-localization/>
- **Package 使用说明（README）:** [`Packages/com.kajitaharuka.unity-editor-localization/README.md`](../../Packages/com.kajitaharuka.unity-editor-localization/README.md) —— 概览、最小配置、API、回退、校验。
- **深入指南**（`Documentation~/`）:
  - `DEVELOPER_GUIDE.md` —— 面向使用方扩展的 scope/key 设计指引。
  - `UI_TOOLKIT_LOCALIZATION_TIPS.md` —— 如何让 UI Toolkit 跟随语言切换。
  - `OPTIONAL_INTEGRATION.md` —— 双程序集的可选依赖模式。

## 许可证

MIT License。详见 [`Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md`](../../Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md)。不要求署名，但始终欢迎。
