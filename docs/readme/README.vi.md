# UnityEditorLocalization

**Languages:** [English](../../README.md) | [日本語](README.ja.md) | [简体中文](README.zh-Hans.md) | [繁體中文](README.zh-Hant.md) | [한국어](README.ko.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Italiano](README.it.md) | [Español (España)](README.es-ES.md) | [Español (Latinoamérica)](README.es-419.md) | [Português (Brasil)](README.pt-BR.md) | [Português (Portugal)](README.pt-PT.md) | [Русский](README.ru.md) | [Polski](README.pl.md) | [Türkçe](README.tr.md) | [ไทย](README.th.md) | Tiếng Việt | [Українська](README.uk.md) | [Bahasa Indonesia](README.id.md)

> Đây là bản dịch; bản gốc chính thức là README tiếng Anh ([English](../../README.md)).

Một nền tảng bản địa hóa nhẹ, **chỉ dành cho Editor (Editor-only)** cho các tiện ích mở rộng của Unity editor. Nó lấy văn bản UI (nhãn Inspector, HelpBox, nút, log Console, thanh tiến trình) từ các catalog dịch theo scope, được lập chỉ mục theo **scope × locale × key**. Thêm một ngôn ngữ chỉ cần thêm một tệp JSON dịch và đăng ký vào manifest — không cần thay đổi C#.

- **Chỉ Editor.** Không phụ thuộc vào mã runtime, Addressables hay package Unity Localization.
- **Locale là các thẻ chuỗi**, được khai báo trong manifest — không phải `enum` C#. Thêm locale không bao giờ động đến C#.
- **19 ngôn ngữ** được cung cấp sẵn trong UI cài đặt (catalog mẫu đi kèm có `ja` và `en`).
- **Các helper UI Toolkit** liên kết `Label` / `Button` / `PropertyField` với key và tự động theo dõi thay đổi ngôn ngữ. Đã bao gồm một menu locale gọn và một dropdown locale.
- **Thân thiện với phụ thuộc tùy chọn.** Các package sử dụng tích hợp nó như một phụ thuộc *tùy chọn*: chúng biên dịch và chạy độc lập với một ngôn ngữ mặc định duy nhất, rồi bật UI đa ngôn ngữ và bộ chuyển locale khi package này được cài đặt — mà không cần tham chiếu assembly cứng.
- **Bộ công cụ catalog.** Một trình hướng dẫn tạo sẽ dựng khung manifest và các bảng trống. Việc xác thực kiểm tra các key bị thiếu, tính nhất quán của placeholder `string.Format` và các mục nghi ngờ chưa dịch (có thể chạy trong batchmode của CI).
- **Các skill dành cho tác nhân AI** đi kèm nhằm nâng chất lượng dịch và dựng khung tích hợp tùy chọn.

> Kho lưu trữ này là mã nguồn công khai theo giấy phép MIT. Nó được phát triển như một embedded UPM
> package duy nhất tại [`Packages/com.kajitaharuka.unity-editor-localization/`](../../Packages/com.kajitaharuka.unity-editor-localization/);
> phần còn lại của kho (`Assets/`, `ProjectSettings/`) chỉ là dự án host dùng để
> phát triển và kiểm chứng package bên trong Unity Editor.

## Cài đặt

Yêu cầu **Unity 2022.3 trở lên**.

### Package Manager (Git URL)

Trong *Package Manager → Add package from git URL…*, nhập:

```text
https://github.com/HarukaKajita/UnityEditorLocalization.git?path=Packages/com.kajitaharuka.unity-editor-localization
```

Ghim một phiên bản bằng cách thêm thẻ release, ví dụ `#<tag>`; thay `<tag>` bằng tên thẻ trong Releases của repository. Bạn cũng có thể thêm chính URL đó trực tiếp vào `Packages/manifest.json`, trong mục `dependencies`.

### VPM (VCC / ALCOM)

Thêm kho VPM, rồi thêm UnityEditorLocalization từ đó:

```text
https://harukakajita.github.io/vpm-repos/index.json
```

### Booth

Một tệp `.zip` đã đóng gói (bên trong có `.unitypackage` / `.tgz`) có trên Booth — miễn phí, kèm một bậc ủng hộ tùy chọn:

```text
https://genera.booth.pm/items/8617787
```

`.unitypackage` không chứa `Samples~/` lẫn `Documentation~/`, vì AssetDatabase của Unity không xử lý thư mục có `~`. Hãy dùng `.tgz`, git URL hoặc VPM nếu bạn cần ví dụ và các hướng dẫn chi tiết.

## Tài liệu

- **Trang sản phẩm (đa ngôn ngữ):** <https://kajitaharuka.com/products/unity-editor-localization/>
- **Cách dùng package (README):** [`Packages/com.kajitaharuka.unity-editor-localization/README.md`](../../Packages/com.kajitaharuka.unity-editor-localization/README.md) — tổng quan, thiết lập tối thiểu, API, fallback, xác thực.
- **Hướng dẫn chuyên sâu** (`Documentation~/`):
  - `DEVELOPER_GUIDE.md` — hướng dẫn thiết kế scope/key cho các tiện ích mở rộng sử dụng.
  - `UI_TOOLKIT_LOCALIZATION_TIPS.md` — cách làm cho UI Toolkit theo dõi thay đổi ngôn ngữ.
  - `OPTIONAL_INTEGRATION.md` — mẫu phụ thuộc tùy chọn với hai assembly.

## Giấy phép

Giấy phép MIT. Xem [`Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md`](../../Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md). Không bắt buộc ghi công, nhưng luôn được hoan nghênh.
