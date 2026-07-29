# UnityEditorLocalization

**Languages:** [English](../../README.md) | [日本語](README.ja.md) | [简体中文](README.zh-Hans.md) | [繁體中文](README.zh-Hant.md) | [한국어](README.ko.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Italiano](README.it.md) | [Español (España)](README.es-ES.md) | [Español (Latinoamérica)](README.es-419.md) | [Português (Brasil)](README.pt-BR.md) | Português (Portugal) | [Русский](README.ru.md) | [Polski](README.pl.md) | [Türkçe](README.tr.md) | [ไทย](README.th.md) | [Tiếng Việt](README.vi.md) | [Українська](README.uk.md) | [Bahasa Indonesia](README.id.md)

> Esta é uma versão traduzida; a versão canónica é o README em inglês ([English](../../README.md)).

Uma base de localização leve e **apenas para o Editor (Editor-only)** para extensões do editor Unity. O texto da interface (etiquetas do Inspector, HelpBox, botões, registos da Console, barras de progresso) é obtido de catálogos de tradução por scope, indexados por **scope × locale × key**. Para adicionar um idioma, basta acrescentar o ficheiro JSON de tradução e registá-lo no manifest — sem alterações em C#.

- **Apenas Editor.** Sem dependência de código em tempo de execução, de Addressables ou do package Unity Localization.
- **As locales são etiquetas de string**, declaradas num manifest — não um `enum` de C#. Adicionar uma locale nunca mexe no C#.
- **19 idiomas** vêm incluídos na UI de definições (o catálogo de exemplo fornecido traz `ja` e `en`).
- **Os helpers do UI Toolkit** ligam `Label` / `Button` / `PropertyField` a keys e acompanham as mudanças de idioma automaticamente. Estão incluídos um menu de locale compacto e uma lista pendente de locale.
- **Compatível com dependências opcionais.** Os packages consumidores integram-no como dependência *opcional*: compilam e funcionam de forma autónoma num único idioma predefinido e, depois, ativam uma UI multilingue e um seletor de locale quando este package está instalado — sem qualquer referência rígida de assembly.
- **Ferramentas de catálogo.** Um assistente de criação gera um manifest e tabelas vazias. A validação verifica keys em falta, a consistência dos marcadores de posição de `string.Format` e as entradas suspeitas de não tradução (executável em batchmode na CI).
- **Skills para agentes de IA** incluídas para a qualidade da tradução e a geração da integração opcional.

> Este repositório é a fonte pública sob licença MIT. É desenvolvido como um único embedded UPM
> package em [`Packages/com.kajitaharuka.unity-editor-localization/`](../../Packages/com.kajitaharuka.unity-editor-localization/);
> o resto do repositório (`Assets/`, `ProjectSettings/`) é apenas o projeto host usado para
> desenvolver e verificar o package dentro do Editor Unity.

## Instalação

Requer **Unity 2022.3 ou posterior**.

### Package Manager (Git URL)

Em *Package Manager → Add package from git URL…*, introduza:

```text
https://github.com/HarukaKajita/UnityEditorLocalization.git?path=Packages/com.kajitaharuka.unity-editor-localization
```

Fixe uma versão anexando uma tag de release, por exemplo `#<tag>`; substitua `<tag>` pelo nome de uma tag em Releases do repositório. Também pode adicionar o mesmo URL diretamente em `Packages/manifest.json`, dentro de `dependencies`.

### VPM (VCC / ALCOM)

Adicione o repositório VPM e, em seguida, adicione o UnityEditorLocalization a partir dele:

```text
https://harukakajita.github.io/vpm-repos/index.json
```

### Booth

Está disponível no Booth um `.zip` empacotado (com `.unitypackage` / `.tgz` no interior) — gratuito, com um nível de apoio opcional:

```text
https://genera.booth.pm/items/8617787
```

O `.unitypackage` não inclui `Samples~/` nem `Documentation~/`, porque o AssetDatabase do Unity não processa pastas com `~`. Use o `.tgz`, a git URL ou o VPM se quiser o exemplo e os guias detalhados.

## Documentação

- **Página do produto (multilingue):** <https://kajitaharuka.com/products/unity-editor-localization/>
- **Utilização do package (README):** [`Packages/com.kajitaharuka.unity-editor-localization/README.md`](../../Packages/com.kajitaharuka.unity-editor-localization/README.md) — descrição geral, configuração mínima, API, fallback, validação.
- **Guias aprofundados** (`Documentation~/`):
  - `DEVELOPER_GUIDE.md` — orientações de design de scope/key para as extensões consumidoras.
  - `UI_TOOLKIT_LOCALIZATION_TIPS.md` — como fazer o UI Toolkit acompanhar as mudanças de idioma.
  - `OPTIONAL_INTEGRATION.md` — o padrão de dependência opcional com duas assemblies.

## Licença

Licença MIT. Consulte [`Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md`](../../Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md). A atribuição não é obrigatória, mas é sempre bem-vinda.
