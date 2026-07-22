# UnityEditorLocalization

**Languages:** [English](../../README.md) | [日本語](README.ja.md) | [简体中文](README.zh-Hans.md) | [繁體中文](README.zh-Hant.md) | [한국어](README.ko.md) | Français | [Deutsch](README.de.md) | [Italiano](README.it.md) | [Español (España)](README.es-ES.md) | [Español (Latinoamérica)](README.es-419.md) | [Português (Brasil)](README.pt-BR.md) | [Português (Portugal)](README.pt-PT.md) | [Русский](README.ru.md) | [Polski](README.pl.md) | [Türkçe](README.tr.md) | [ไทย](README.th.md) | [Tiếng Việt](README.vi.md) | [Українська](README.uk.md) | [Bahasa Indonesia](README.id.md)

> Ceci est une version traduite ; la version de référence est le README en anglais ([English](../../README.md)).

Une base de localisation légère et **réservée à l'Éditeur (Editor-only)** pour les extensions d'éditeur Unity. Le texte de l'interface (libellés de l'Inspector, HelpBox, boutons, journaux de la Console, barres de progression) est puisé dans des catalogues de traduction par scope, indexés par **scope × locale × key**. Ajouter une langue se fait en ajoutant un fichier JSON — sans modifier le C#.

- **Réservé à l'Éditeur.** Aucune dépendance au code d'exécution, à Addressables ni au package Unity Localization.
- **Les locales sont des balises de chaîne**, déclarées dans un manifest — pas un `enum` C#. Ajouter une locale ne touche jamais au C#.
- **19 langues** sont fournies dans l'UI de configuration et dans le catalogue d'exemple inclus.
- **Les aides UI Toolkit** lient `Label` / `Button` / `PropertyField` à des keys et suivent automatiquement les changements de langue. Un menu de locale compact et une liste déroulante de locale sont inclus.
- **Compatible avec les dépendances optionnelles.** Les packages consommateurs l'intègrent comme dépendance *optionnelle* : ils compilent et s'exécutent de façon autonome dans une seule langue par défaut, puis activent une UI multilingue et un sélecteur de locale une fois ce package installé — sans référence d'assembly stricte.
- **Outillage de catalogue.** Un assistant de création génère un manifest et des tables vides. La validation vérifie les keys manquantes, la cohérence des espaces réservés `string.Format` et les entrées suspectées non traduites (exécutable en batchmode CI).
- Des **skills d'agent IA** inclus pour la qualité de traduction et la génération de l'intégration optionnelle.

> Ce dépôt est la source publique sous licence MIT. Il est développé comme un unique embedded UPM
> package sous [`Packages/com.kajitaharuka.unity-editor-localization/`](../../Packages/com.kajitaharuka.unity-editor-localization/) ;
> le reste du dépôt (`Assets/`, `ProjectSettings/`) n'est que le projet hôte servant à
> développer et vérifier le package dans l'Éditeur Unity.

## Installation

Nécessite **Unity 2022.3 ou une version ultérieure**.

### Package Manager (Git URL)

Dans *Package Manager → Add package from git URL…*, saisissez :

```text
https://github.com/HarukaKajita/UnityEditorLocalization.git?path=Packages/com.kajitaharuka.unity-editor-localization
```

Fixez une version en ajoutant un tag de release, p. ex. `#1.2.1`. Vous pouvez aussi ajouter la même URL directement dans `Packages/manifest.json`, sous `dependencies`.

### VPM (VCC / ALCOM)

Ajoutez le dépôt VPM, puis ajoutez UnityEditorLocalization à partir de celui-ci :

```text
https://harukakajita.github.io/vpm-repos/index.json
```

### Booth

Un `.zip` empaqueté (contenant `.unitypackage` / `.tgz`) est disponible sur Booth — gratuit, avec un palier de soutien facultatif :

```text
https://genera.booth.pm/items/8617787
```

## Documentation

- **Page produit (multilingue) :** <https://kajitaharuka.com/products/unity-editor-localization/>
- **Utilisation du package (README) :** [`Packages/com.kajitaharuka.unity-editor-localization/README.md`](../../Packages/com.kajitaharuka.unity-editor-localization/README.md) — présentation, configuration minimale, API, repli (fallback), validation.
- **Guides approfondis** (`Documentation~/`) :
  - `DEVELOPER_GUIDE.md` — conseils de conception scope/key pour les extensions consommatrices.
  - `UI_TOOLKIT_LOCALIZATION_TIPS.md` — comment faire suivre les changements de langue à UI Toolkit.
  - `OPTIONAL_INTEGRATION.md` — le motif de dépendance optionnelle à deux assemblies.

## Licence

Licence MIT. Voir [`Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md`](../../Packages/com.kajitaharuka.unity-editor-localization/LICENSE.md). L'attribution n'est pas requise mais toujours bienvenue.
