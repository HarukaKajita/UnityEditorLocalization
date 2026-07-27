#!/usr/bin/env python3
# 生成物: この内容はテンプレートリポジトリ UnityTemplate_2022_3_22f1 から配布されたコピーです。
# 編集はテンプレート側で行い、scripts/distribute_standard.py で再配布してください。
# source: UnityTemplate_2022_3_22f1/scripts/pipeline/emit_release_manifest.py
# source-sha256: fdc7dd74597be16585f0d290c2b0eec52af8c52d81d724d6dc49c70db9765eb5
"""リリース契約ファイル `release-<version>.json` を決定的に生成する（ゴールド標準 第3層）。

`pipeline/repo.json` の宣言と `package.json` / `CHANGELOG.md` / `Publish/` / `git` だけを入力に
生成するので、**何度実行しても同じ内容**になる。成果物集合は「Publish/ を走査して見つかったもの」
ではなく `saleUnit.distribution × saleUnit.packages` から先に構成し、欠落・想定外を error にする。

`sourceCommit` は「成果物を最後に変更したコミット」で、契約ファイル自身のコミットは契約の対象外。
`tag` は version から決まる期待名で、実在する場合は sourceCommit を含むことを検証する。**書き出し先は 2 箇所**:

- `<repo>/Publish/release-<version>.json`                       生成元の証跡（開発リポジトリにコミット）
- `<external-content>/products/{slug}/releases/<version>.json`   検証の入力（MySite 側が読む）

この二重化により、MySite 側の検証は external-content 内で完結する。開発リポジトリを
解決できない環境（CI・別マシン・クラウドレビュー）でもそのまま動く。

使い方（対象リポジトリのルートで実行）:
    python3 scripts/pipeline/emit_release_manifest.py                     # 現在の version で生成
    python3 scripts/pipeline/emit_release_manifest.py --version 1.3.1   # 版の一致を明示的に確認する
    python3 scripts/pipeline/emit_release_manifest.py --check             # 書かずに差分だけ検査
    python3 scripts/pipeline/emit_release_manifest.py --external-content <path>

正本: UnityTemplate_2022_3_22f1/scripts/pipeline/emit_release_manifest.py
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path

SCHEMA_VERSION = 1
PERSONAL_CONFIG = Path.home() / ".kajitaharuka-pipeline.json"
DEFAULT_REGISTRY_CANDIDATES = (
    "~/dev/MySite/pipeline/repositories.json",
    "~/MySite/pipeline/repositories.json",
)
SITE_PRODUCT_PREFIX = "https://kajitaharuka.com/products/"

VERSION_HEADING_RE = re.compile(r"^##\s*\[(?P<version>[^\]]+)\]\s*-\s*(?P<date>\d{4}-\d{2}-\d{2})\s*$")
SUBSECTION_RE = re.compile(r"^###\s+(?P<title>.+?)\s*$")


# ---------------------------------------------------------------------------
# 共通
# ---------------------------------------------------------------------------


def load_json(path: Path) -> dict | None:
    try:
        with path.open(encoding="utf-8") as handle:
            return json.load(handle)
    except (OSError, json.JSONDecodeError):
        return None


def run_git(root: Path, *args: str) -> str:
    result = subprocess.run(
        ["git", "-C", str(root), *args], capture_output=True, text=True, check=False
    )
    return result.stdout.strip() if result.returncode == 0 else ""


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def discover_packages(root: Path) -> list[tuple[str, Path, dict]]:
    packages = []
    packages_dir = root / "Packages"
    if not packages_dir.is_dir():
        return packages
    for entry in sorted(packages_dir.iterdir()):
        if not entry.is_dir():
            continue
        meta = load_json(entry / "package.json")
        if meta and meta.get("name"):
            packages.append((meta["name"], entry, meta))
    return packages


def pick_primary(packages: list[tuple[str, Path, dict]], config: dict) -> tuple[str, Path, dict]:
    """販売単位の代表パッケージ（version と CHANGELOG の出所）を決める。"""
    declared = (config.get("saleUnit") or {}).get("primaryPackage")
    if declared:
        for package in packages:
            if package[0] == declared:
                return package
    # サブパッケージ（ドットが多い方）ではなく、名前が最も短いものを代表とする
    return min(packages, key=lambda p: (p[0].count("."), len(p[0])))


def derive_slug(config: dict, meta: dict) -> str | None:
    if config.get("productSlug"):
        return config["productSlug"]
    url = meta.get("documentationUrl") or ""
    if url.startswith(SITE_PRODUCT_PREFIX):
        tail = url[len(SITE_PRODUCT_PREFIX) :].strip("/")
        if tail and "/" not in tail:
            return tail
    return None


# ---------------------------------------------------------------------------
# CHANGELOG
# ---------------------------------------------------------------------------


def read_changelog(path: Path, version: str) -> tuple[str | None, list[str]]:
    """指定バージョンの (リリース日, 小見出し一覧) を返す。節が無ければ (None, [])。"""
    if not path.is_file():
        return None, []
    released_at: str | None = None
    sections: list[str] = []
    inside = False
    for line in path.read_text(encoding="utf-8").splitlines():
        heading = VERSION_HEADING_RE.match(line)
        if heading:
            if inside:
                break
            if heading.group("version") == version:
                inside = True
                released_at = heading.group("date")
            continue
        if line.startswith("## "):
            if inside:
                break
            continue
        if inside:
            sub = SUBSECTION_RE.match(line)
            if sub:
                sections.append(sub.group("title"))
    return released_at, sections


# ---------------------------------------------------------------------------
# 成果物
# ---------------------------------------------------------------------------


def expected_artifacts(sale_unit: dict, version: str) -> list[tuple[str, str]]:
    """distribution × packages から**期待成果物集合**を先に構成する（(kind, Publish 相対パス)）。

    Publish/ を全走査して「見つかったものを載せる」方式だと、欠落に気づかないまま
    不完全な契約ファイルを出してしまう。期待集合を先に決めて突き合わせる。
    """
    display_name = sale_unit.get("displayName")
    packages = sale_unit.get("packages") or []
    expected: list[tuple[str, str]] = []
    for kind in sale_unit.get("distribution") or []:
        if kind == "sale-zip":
            if display_name:
                expected.append((kind, f"{display_name}-{version}.zip"))
        elif kind == "tgz":
            expected += [(kind, f"{name}-{version}.tgz") for name in packages]
        elif kind == "unitypackage":
            expected += [(kind, f"{name}-{version}.unitypackage") for name in packages]
        elif kind == "vpm-zip":
            expected += [(kind, f"vpm/{name}-{version}.zip") for name in packages]
    return expected


def collect_artifacts(root: Path, sale_unit: dict, version: str) -> tuple[list[dict], list[str]]:
    publish = root / "Publish"
    artifacts: list[dict] = []
    problems: list[str] = []
    expected = expected_artifacts(sale_unit, version)
    if not expected:
        problems.append("saleUnit.distribution / packages から期待成果物を決められません")
        return artifacts, problems

    for kind, rel in expected:
        path = publish / rel
        if not path.is_file():
            problems.append(f"期待した成果物がありません: Publish/{rel}")
            continue
        artifacts.append(
            {"kind": kind, "file": rel, "bytes": path.stat().st_size, "sha256": sha256_file(path)}
        )

    # 同じ version の想定外ファイルは「命名を間違えた／余分に書き出した」の兆候なので拾う
    expected_files = {rel for _, rel in expected}
    if publish.is_dir():
        for path in sorted(publish.rglob(f"*{version}*")):
            if not path.is_file():
                continue
            rel = path.relative_to(publish).as_posix()
            if rel in expected_files or rel == f"release-{version}.json":
                continue
            problems.append(f"同じ version の想定外ファイルがあります: Publish/{rel}")

    order = {"sale-zip": 0, "tgz": 1, "unitypackage": 2, "vpm-zip": 3}
    artifacts.sort(key=lambda a: (order.get(a["kind"], 9), a["file"]))
    return artifacts, problems


def resolve_source_commit(root: Path, artifacts: list[dict]) -> tuple[str | None, list[str]]:
    """成果物を最後に変更したコミットを返す。

    生成時の HEAD を使うと、契約ファイル自体をコミットした瞬間に値が古くなり、
    再生成のたびに内容が変わってしまう。**成果物のパスを最後に触ったコミット**なら
    後から何度実行しても同じ値になり、「この成果物がどのソースから出たか」という
    本来の意味とも一致する。
    """
    if not artifacts:
        return None, ["成果物が無いため sourceCommit を決められません"]
    paths = [f"Publish/{artifact['file']}" for artifact in artifacts]
    untracked = [
        path for path in paths if not run_git(root, "ls-files", "--error-unmatch", path)
    ]
    if untracked:
        return None, [
            "成果物が git 追跡されていません。先に成果物をコミットしてから契約ファイルを生成してください: "
            + ", ".join(untracked)
        ]
    commit = run_git(root, "log", "-1", "--format=%H", "--", *paths)
    if not commit:
        return None, ["成果物のコミットを特定できません"]
    return commit, []


def resolve_tag(root: Path, version: str, source_commit: str) -> tuple[str | None, list[str]]:
    """version から決まる期待タグ名を返し、実在する場合は sourceCommit を含むことを検証する。

    タグは契約ファイルのコミットより後に作られるため、生成時点では未作成が普通。
    期待名を先に決めておき、実在したときだけ整合を検査する。
    """
    existing = run_git(root, "tag", "--list").split()
    prefix = "v" if any(tag.startswith("v") and tag[1:2].isdigit() for tag in existing) else ""
    expected = f"{prefix}{version}"
    if expected not in existing:
        return expected, []
    tagged = run_git(root, "rev-list", "-n", "1", expected)
    if not tagged:
        return expected, [f"タグ {expected} を解決できません"]
    ancestor = subprocess.run(
        ["git", "-C", str(root), "merge-base", "--is-ancestor", source_commit, tagged],
        capture_output=True,
        check=False,
    )
    if ancestor.returncode != 0:
        return expected, [
            f"タグ {expected} が成果物のコミット {source_commit[:12]} を含みません（別の内容にタグを付けた疑い）"
        ]
    return expected, []


# ---------------------------------------------------------------------------
# external-content の解決
# ---------------------------------------------------------------------------


def find_external_content(explicit: str | None) -> Path | None:
    candidates: list[str] = []
    if explicit:
        candidates.append(explicit)
    personal = load_json(PERSONAL_CONFIG) or {}
    override = (personal.get("overrides") or {}).get("external-content")
    if override:
        candidates.append(override)

    registry_paths = []
    if personal.get("registryPath"):
        registry_paths.append(personal["registryPath"])
    registry_paths.extend(DEFAULT_REGISTRY_CANDIDATES)
    for registry_path in registry_paths:
        registry = load_json(Path(registry_path).expanduser())
        if not registry:
            continue
        for entry in registry.get("repositories", []):
            if entry.get("role") == "content":
                candidates.extend(entry.get("localPathCandidates") or [])

    candidates.extend(["~/dev/MySite/external-content"])
    for candidate in candidates:
        path = Path(str(candidate)).expanduser()
        if (path / "products").is_dir():
            return path
    return None


# ---------------------------------------------------------------------------
# 生成
# ---------------------------------------------------------------------------


def build_manifest(root: Path, asserted_version: str | None, config: dict) -> tuple[dict, list[str]]:
    problems: list[str] = []
    sale_unit = config.get("saleUnit")
    if not sale_unit:
        return {}, ["pipeline/repo.json に saleUnit がありません（成果物集合を決定できません）"]
    if not sale_unit.get("packages"):
        return {}, ["saleUnit.packages が空です（Unity パッケージを持たない商品はこのスクリプトの対象外）"]

    packages = discover_packages(root)
    declared = list(sale_unit.get("packages") or [])
    found = {name: (name, path, meta) for name, path, meta in packages}
    missing = [name for name in declared if name not in found]
    if missing:
        return {}, [f"saleUnit.packages にあるパッケージが Packages/ にありません: {', '.join(missing)}"]
    packages = [found[name] for name in declared]

    primary_name, primary_dir, primary_meta = pick_primary(packages, config)
    version = primary_meta.get("version")
    if not version:
        return {}, [f"{primary_name} の package.json に version がありません"]
    # --version は上書きではなく一致アサーション（package.json から決定的に生成する契約を守る）
    if asserted_version and asserted_version != version:
        return {}, [f"--version {asserted_version} が package.json の version {version} と一致しません"]

    if sale_unit.get("versionPolicy", "lockstep") == "lockstep":
        mismatched = [name for name, _, meta in packages if meta.get("version") != version]
        if mismatched:
            problems.append(
                f"version が {version} と異なるパッケージがあります: {', '.join(mismatched)}（versionPolicy=lockstep）"
            )

    slug = derive_slug(config, primary_meta)
    if not slug:
        problems.append("商品 slug を決められません（pipeline/repo.json の productSlug か documentationUrl が必要）")

    released_at, sections = read_changelog(primary_dir / "CHANGELOG.md", version)
    if released_at is None:
        problems.append(f"CHANGELOG.md に `## [{version}] - YYYY-MM-DD` の節がありません")

    artifacts, artifact_problems = collect_artifacts(root, sale_unit, version)
    problems.extend(artifact_problems)

    source_commit, commit_problems = resolve_source_commit(root, artifacts)
    problems.extend(commit_problems)
    tag = None
    if source_commit:
        tag, tag_problems = resolve_tag(root, version, source_commit)
        problems.extend(tag_problems)

    manifest = {
        "$schemaVersion": SCHEMA_VERSION,
        "slug": slug,
        "repository": config.get("repository") or root.name,
        "displayName": sale_unit.get("displayName") or primary_meta.get("displayName") or primary_name,
        "version": version,
        "releasedAt": released_at,
        "tag": tag,
        "sourceCommit": source_commit,
        "packages": [{"name": name, "version": meta.get("version")} for name, _, meta in packages],
        "artifacts": artifacts,
        "changelogSections": sections,
    }
    return manifest, problems


def dump(manifest: dict) -> str:
    return json.dumps(manifest, ensure_ascii=False, indent=2) + "\n"


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description="リリース契約ファイルを生成する")
    parser.add_argument("--root", default=".", help="対象リポジトリのルート（既定: カレント）")
    parser.add_argument("--version", help="期待バージョン（上書きではなく package.json との一致アサーション）")
    parser.add_argument("--external-content", help="external-content のルート")
    parser.add_argument("--check", action="store_true", help="書き込まずに差分だけ検査する")
    parser.add_argument("--skip-external-content", action="store_true", help="開発リポジトリ側だけ書き出す")
    args = parser.parse_args(argv)

    root = Path(args.root).resolve()
    config = load_json(root / "pipeline" / "repo.json") or {}
    manifest, problems = build_manifest(root, args.version, config)
    if not manifest:
        for problem in problems:
            print(f"ERROR: {problem}", file=sys.stderr)
        return 2

    # 不完全な契約ファイルは出荷事故の元なので、生成前に止める（fail-closed）
    if problems:
        for problem in problems:
            print(f"ERROR: {problem}", file=sys.stderr)
        return 1

    content = dump(manifest)
    version = manifest["version"]
    destinations: list[Path] = [root / "Publish" / f"release-{version}.json"]
    notes: list[str] = []

    if not args.skip_external_content:
        external = find_external_content(args.external_content)
        if external is None:
            notes.append("external-content を解決できないため、契約ファイルは開発リポジトリ側にのみ書きます")
        elif not (external / "products" / manifest["slug"]).is_dir():
            # 商品ディレクトリが無い＝まだ商品化していない。契約ファイルだけで半端な商品を作らない
            notes.append(
                f"external-content に products/{manifest['slug']}/ が無いため、契約ファイルは開発リポジトリ側にのみ書きます"
            )
        else:
            destinations.append(external / "products" / manifest["slug"] / "releases" / f"{version}.json")

    changed: list[str] = []
    for destination in destinations:
        current = destination.read_text(encoding="utf-8") if destination.is_file() else None
        if current == content:
            continue
        changed.append(str(destination))
        if not args.check:
            destination.parent.mkdir(parents=True, exist_ok=True)
            destination.write_text(content, encoding="utf-8")

    for note in notes:
        print(f"WARN: {note}")
    if changed:
        verb = "差分あり" if args.check else "書き出し"
        for path in changed:
            print(f"{verb}: {path}")
    else:
        print(f"最新: release-{version}.json（{len(destinations)} 箇所）")

    return 1 if (args.check and changed) else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
