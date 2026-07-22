# UnityEditorLocalization

**Languages:** [English](../../README.md) | [日本語](README.ja.md) | [简体中文](README.zh-Hans.md) | [繁體中文](README.zh-Hant.md) | 한국어 | [Français](README.fr.md) | [Deutsch](README.de.md) | [Italiano](README.it.md) | [Español (España)](README.es-ES.md) | [Español (Latinoamérica)](README.es-419.md) | [Português (Brasil)](README.pt-BR.md) | [Português (Portugal)](README.pt-PT.md) | [Русский](README.ru.md) | [Polski](README.pl.md) | [Türkçe](README.tr.md) | [ไทย](README.th.md) | [Tiếng Việt](README.vi.md) | [Українська](README.uk.md) | [Bahasa Indonesia](README.id.md)

> 이 문서는 번역본이며, 정본은 영어 README([English](../../README.md))입니다.

Unity 에디터 확장을 위한 **에디터 전용(Editor-only)** 경량 현지화 기반입니다. UI 텍스트(Inspector 레이블, HelpBox, 버튼, Console 로그, 진행 표시줄)를 **scope × locale × key**로 조회하는 scope별 번역 카탈로그에서 가져옵니다. 언어 추가는 JSON 파일 하나를 더하는 것으로 끝나며 C# 변경은 필요 없습니다.

- **에디터 전용.** 런타임 코드, Addressables, Unity Localization package 어디에도 의존하지 않습니다.
- **locale은 문자열 태그**로 manifest에서 선언하며 C#의 `enum`이 아닙니다. locale을 추가해도 C#은 건드리지 않습니다.
- 설정 UI와 동봉 샘플 카탈로그가 **19개 언어**를 지원합니다.
- **UI Toolkit 헬퍼**가 `Label` / `Button` / `PropertyField`를 key에 바인딩하고 언어 변경에 자동으로 따라갑니다. 컴팩트한 locale 메뉴와 locale 드롭다운이 포함됩니다.
- **선택적 의존성(optional)에 친화적.** 사용하는 package는 이를 *선택적* 의존성으로 통합할 수 있습니다. 본 package가 없어도 단일 기본 언어로 독립적으로 컴파일·동작하며, 설치하면 다국어 UI와 locale 전환기가 켜집니다 — 하드한 어셈블리 참조 없이.
- **카탈로그 도구.** 생성 마법사가 manifest와 빈 테이블의 스캐폴드를 만듭니다. 검증은 누락된 key, `string.Format` placeholder 일관성, 미번역 의심 항목을 확인합니다(CI batchmode에서 실행 가능).
- 번역 품질과 선택적 통합 스캐폴딩을 돕는 **AI 에이전트용 스킬(skills)**을 동봉합니다.

> 이 저장소는 MIT 라이선스로 공개된 소스입니다. 개발 대상은 [`Packages/com.kajitaharuka.unity-editor-localization/`](../../Packages/com.kajitaharuka.unity-editor-localization/)
> 아래의 단일 embedded UPM package이며, 저장소의 나머지(`Assets/`, `ProjectSettings/`)는
> Unity Editor 안에서 package를 개발·검증하기 위한 호스트 프로젝트(host project)일 뿐입니다.

## 설치

**Unity 2022.3 이상**이 필요합니다.

### Package Manager (Git URL)

*Package Manager → Add package from git URL…* 에 다음을 입력합니다:

```text
https://github.com/HarukaKajita/UnityEditorLocalization.git?path=Packages/com.kajitaharuka.unity-editor-localization
```

끝에 릴리스 태그를 붙이면 버전을 고정할 수 있습니다(예: `#1.2.1`). 같은 URL을 `Packages/manifest.json`의 `dependencies`에 직접 추가할 수도 있습니다.

### VPM (VCC / ALCOM)

다음 VPM 저장소를 추가한 뒤 거기에서 UnityEditorLocalization을 추가합니다:

```text
https://harukakajita.github.io/vpm-repos/index.json
```

### Booth

패키징된 `.zip`(내부에 `.unitypackage` / `.tgz` 포함)을 Booth에서 배포합니다 — 무료이며 선택적 후원 티어가 있습니다.

```text
https://genera.booth.pm/items/8617787
```

## 문서

- **상품 페이지(다국어):** <https://kajitaharuka.com/products/unity-editor-localization/>
- **Package 사용 안내(README):** [`Packages/com.kajitaharuka.unity-editor-localization/README.md`](../../Packages/com.kajitaharuka.unity-editor-localization/README.md) — 개요, 최소 설정, API, 폴백, 검증.
- **심화 가이드**(`Documentation~/`):
  - `DEVELOPER_GUIDE.md` — 사용하는 확장을 위한 scope/key 설계 지침.
  - `UI_TOOLKIT_LOCALIZATION_TIPS.md` — UI Toolkit이 언어 변경을 따라가게 하는 방법.
  - `OPTIONAL_INTEGRATION.md` — 두 어셈블리 구성의 선택적 의존성 패턴.

## 라이선스

MIT License입니다. [`Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md`](../../Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md) 를 참고하세요. 출처 표기는 의무가 아니지만 언제나 환영합니다.
