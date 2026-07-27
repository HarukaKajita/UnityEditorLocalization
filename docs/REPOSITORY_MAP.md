<!-- 生成物: この内容はテンプレートリポジトリ UnityTemplate_2022_3_22f1 から配布されたコピーです。
     編集はテンプレート側で行い、scripts/distribute_standard.py で再配布してください。
     source: UnityTemplate_2022_3_22f1/pipeline/REPOSITORY_MAP.md（MySite のレジストリから生成）
     source-sha256: d90338e51311c378136d454866488c37383960e338a3cd883dc1c2176df8c466 -->

# パイプラインのリポジトリ地図

kajitaharuka 名義の Unity パッケージ／アセットの開発〜販売パイプラインに乗っているリポジトリの一覧。**正本は MySite の `pipeline/repositories.json`** で、この文書はそこから生成される。

役割の分担は次のとおり（詳細は `docs/GOLD_STANDARD.md` と MySite `docs/pipeline-consistency-design.md`）。

| 場所 | 何の正本か |
|---|---|
| `UnityTemplate_2022_3_22f1` | 標準（ゴールド標準・配布器・検証スクリプト） |
| 各開発リポジトリ | 実装とリリース資材。標準は配布されたコピーを持つ |
| `MySite` | 運用（実在リポジトリ一覧・出品資料・商品ページの site 実装） |
| `external-content` | 商品情報とリリース契約 |

## 標準の正本

| リポジトリ | 商品 slug | 既定ブランチ / 作業ブランチ | remote |
|---|---|---|---|
| **UnityTemplate_2022_3_22f1** | — | main / main | `HarukaKajita/UnityTemplate_2022_3_22f1`（private） |

## 販売リポジトリ

| リポジトリ | 商品 slug | 既定ブランチ / 作業ブランチ | remote |
|---|---|---|---|
| **ExportPackageExtension** | export-package-extension | main / develop | `HarukaKajita/ExportPackageExtension`（private） |
| **UnityEditorLocalization** | unity-editor-localization | main / develop | `HarukaKajita/UnityEditorLocalization`（public） |
| **UnityEditorWindowCaptureExtension** | unity-editor-window-capture-extension | main / develop | `HarukaKajita/UnityEditorWindowCaptureExtension`（private） |
| **TextureAssetExtension** | texture-asset-extension | main / develop | `HarukaKajita/TextureAssetExtension`（private） |
| **UberMaterialPropertyDrawer** | uber-material-property-drawer | main / develop | `HarukaKajita/UberMaterialPropertyDrawer`（private） |
| **TechBook_GenerativeProgramming1** | generative-programming-1 | master / master | `HarukaKajita/TechBook_GenerativeProgramming1`（private） |

## サイト

| リポジトリ | 商品 slug | 既定ブランチ / 作業ブランチ | remote |
|---|---|---|---|
| **MySite** | — | main / develop | `HarukaKajita/MySite`（private） |

## 商品情報

| リポジトリ | 商品 slug | 既定ブランチ / 作業ブランチ | remote |
|---|---|---|---|
| **external-content** | — | main / main | `HarukaKajita/external-content`（private） |

## 配信基盤

| リポジトリ | 商品 slug | 既定ブランチ / 作業ブランチ | remote |
|---|---|---|---|
| **vpm-repos** | — | main / main | `HarukaKajita/vpm-repos`（public） |

## 検証サンドボックス

| リポジトリ | 商品 slug | 既定ブランチ / 作業ブランチ | remote |
|---|---|---|---|
| **My project** | — | None / None | `?/?` |

## ローカルパスの解決手順

ローカルの配置はマシンごとに異なるため、**この文書にはパスを書かない**。解決は必ず次の順で行う。

1. `~/.kajitaharuka-pipeline.json` の `overrides`（リポジトリにコミットしない個人設定）
2. レジストリの `localPathCandidates` を順に試す
3. **`git -C <path> remote get-url origin` がレジストリの remote と一致することを検証する**（同じパスにある別リポジトリを掴まないため）
4. 解決できなければ remote が入口。clone コマンドを提示して止まる（勝手に clone しない）

