# UnityEditorLocalization

**Languages:** [English](../../README.md) | [日本語](README.ja.md) | [简体中文](README.zh-Hans.md) | [繁體中文](README.zh-Hant.md) | [한국어](README.ko.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Italiano](README.it.md) | [Español (España)](README.es-ES.md) | [Español (Latinoamérica)](README.es-419.md) | Português (Brasil) | [Português (Portugal)](README.pt-PT.md) | [Русский](README.ru.md) | [Polski](README.pl.md) | [Türkçe](README.tr.md) | [ไทย](README.th.md) | [Tiếng Việt](README.vi.md) | [Українська](README.uk.md) | [Bahasa Indonesia](README.id.md)

> Esta é uma versão traduzida; a versão canônica é o README em inglês ([English](../../README.md)).

Uma base de localização leve e **somente para o Editor (Editor-only)** para extensões do editor Unity. O texto da interface (rótulos do Inspector, HelpBox, botões, logs do Console, barras de progresso) é obtido de catálogos de tradução por scope, indexados por **scope × locale × key**. Para adicionar um idioma, basta adicionar um arquivo JSON — sem alterações em C#.

- **Somente Editor.** Sem dependência de código em tempo de execução, de Addressables ou do package Unity Localization.
- **As locales são tags de string**, declaradas em um manifest — não um `enum` de C#. Adicionar uma locale nunca mexe no C#.
- **19 idiomas** já vêm na UI de configurações e no catálogo de exemplo incluído.
- **Os helpers do UI Toolkit** vinculam `Label` / `Button` / `PropertyField` a keys e acompanham as mudanças de idioma automaticamente. Um menu de locale compacto e um dropdown de locale estão incluídos.
- **Amigável a dependências opcionais.** Os packages consumidores o integram como dependência *opcional*: compilam e funcionam de forma autônoma em um único idioma padrão e, então, acendem uma UI multilíngue e um seletor de locale quando este package está instalado — sem nenhuma referência rígida de assembly.
- **Ferramentas de catálogo.** Um assistente de criação gera um manifest e tabelas vazias. A validação verifica keys ausentes, a consistência dos placeholders de `string.Format` e as entradas suspeitas de não tradução (executável em batchmode na CI).
- **Skills para agentes de IA** incluídas para qualidade de tradução e geração da integração opcional.

> Este repositório é a fonte pública sob licença MIT. Ele é desenvolvido como um único embedded UPM
> package em [`Packages/com.kajitaharuka.unity-editor-localization/`](../../Packages/com.kajitaharuka.unity-editor-localization/);
> o restante do repositório (`Assets/`, `ProjectSettings/`) é apenas o projeto host usado para
> desenvolver e verificar o package dentro do Editor Unity.

## Instalação

Requer **Unity 2022.3 ou posterior**.

### Package Manager (Git URL)

Em *Package Manager → Add package from git URL…*, insira:

```text
https://github.com/HarukaKajita/UnityEditorLocalization.git?path=Packages/com.kajitaharuka.unity-editor-localization
```

Fixe uma versão anexando uma tag de release, por exemplo `#1.2.1`. Você também pode adicionar a mesma URL diretamente em `Packages/manifest.json`, dentro de `dependencies`.

### VPM (VCC / ALCOM)

Adicione o repositório VPM e, em seguida, adicione o UnityEditorLocalization a partir dele:

```text
https://harukakajita.github.io/vpm-repos/index.json
```

### Booth

Um `.zip` empacotado (com `.unitypackage` / `.tgz` dentro) está disponível no Booth — gratuito, com um nível de apoio opcional:

```text
https://genera.booth.pm/items/8617787
```

## Documentação

- **Página do produto (multilíngue):** <https://kajitaharuka.com/products/unity-editor-localization/>
- **Uso do package (README):** [`Packages/com.kajitaharuka.unity-editor-localization/README.md`](../../Packages/com.kajitaharuka.unity-editor-localization/README.md) — visão geral, configuração mínima, API, fallback, validação.
- **Guias aprofundados** (`Documentation~/`):
  - `DEVELOPER_GUIDE.md` — orientação de design de scope/key para as extensões consumidoras.
  - `UI_TOOLKIT_LOCALIZATION_TIPS.md` — como fazer o UI Toolkit acompanhar as mudanças de idioma.
  - `OPTIONAL_INTEGRATION.md` — o padrão de dependência opcional com duas assemblies.

## Licença

Licença MIT. Consulte [`Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md`](../../Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md). A atribuição não é obrigatória, mas é sempre bem-vinda.
