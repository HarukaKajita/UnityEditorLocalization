#!/usr/bin/env python3
# 生成物: この内容はテンプレートリポジトリ UnityTemplate_2022_3_22f1 から配布されたコピーです。
# 編集はテンプレート側で行い、scripts/distribute_standard.py で再配布してください。
# source: UnityTemplate_2022_3_22f1/scripts/pipeline/emit_release_manifest.py
# source-sha256: bd35971d32a915f69c246590705980515d57ca4ea4ab22e7ed100d9c0471db24
"""リリース契約ファイル `release-<version>.json` を決定的に生成する（ゴールド標準 第3層）。

`package.json` / `CHANGELOG.md` / `Publish/` / `git` だけを入力にして生成するので、
同じコミットからは常に同じ内容になる。**書き出し先は 2 箇所**:

- `<repo>/Publish/release-<version>.json`                       生成元の証跡（開発リポジトリにコミット）
- `<external-content>/products/{slug}/releases/<version>.json`   検証の入力（MySite 側が読む）

この二重化により、MySite 側の検証は external-content 内で完結する。開発リポジトリを
解決できない環境（CI・別マシン・クラウドレビュー）でもそのまま動く。

使い方（対象リポジトリのルートで実行）:
    python3 scripts/pipeline/emit_release_manifest.py                     # 現在の version で生成
    python3 scripts/pipeline/emit_release_manifest.py --version 1.3.1
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


def collect_artifacts(root: Path, version: str, packages: list[tuple[str, Path, dict]], display_names: set[str]) -> list[dict]:
    publish = root / "Publish"
    if not publish.is_dir():
        return []
    package_names = {name for name, _, _ in packages}
    artifacts: list[dict] = []

    for path in sorted(publish.rglob("*")):
        if not path.is_file():
            continue
        rel = path.relative_to(publish).as_posix()
        name = path.name
        kind: str | None = None
        if name.endswith(".tgz") and any(name == f"{p}-{version}.tgz" for p in package_names):
            kind = "tgz"
        elif name.endswith(".unitypackage") and any(name == f"{p}-{version}.unitypackage" for p in package_names):
            kind = "unitypackage"
        elif name.endswith(".zip") and rel.startswith("vpm/") and any(name == f"{p}-{version}.zip" for p in package_names):
            kind = "vpm-zip"
        elif name.endswith(".zip") and any(name == f"{d}-{version}.zip" for d in display_names):
            kind = "sale-zip"
        if kind is None:
            continue
        artifacts.append(
            {
                "kind": kind,
                "file": rel,
                "bytes": path.stat().st_size,
                "sha256": sha256_file(path),
            }
        )

    order = {"sale-zip": 0, "tgz": 1, "unitypackage": 2, "vpm-zip": 3}
    artifacts.sort(key=lambda a: (order.get(a["kind"], 9), a["file"]))
    return artifacts


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


def build_manifest(root: Path, version: str | None, config: dict) -> tuple[dict, list[str]]:
    problems: list[str] = []
    packages = discover_packages(root)
    if not packages:
        return {}, ["Packages/ に package.json を持つパッケージがありません"]

    primary_name, primary_dir, primary_meta = pick_primary(packages, config)
    version = version or primary_meta.get("version")
    if not version:
        return {}, [f"{primary_name} の package.json に version がありません"]

    mismatched = [name for name, _, meta in packages if meta.get("version") != version]
    if mismatched:
        problems.append(
            f"version が {version} と異なるパッケージがあります: {', '.join(mismatched)}（スイートは版を揃える）"
        )

    slug = derive_slug(config, primary_meta)
    if not slug:
        problems.append("商品 slug を決められません（pipeline/repo.json の productSlug か documentationUrl が必要）")

    released_at, sections = read_changelog(primary_dir / "CHANGELOG.md", version)
    if released_at is None:
        problems.append(f"CHANGELOG.md に `## [{version}] - YYYY-MM-DD` の節がありません")

    display_names = {meta.get("displayName") for _, _, meta in packages if meta.get("displayName")}
    sale_display = (config.get("saleUnit") or {}).get("displayName")
    if sale_display:
        display_names.add(sale_display)
    artifacts = collect_artifacts(root, version, packages, {d for d in display_names if d})
    if not artifacts:
        problems.append(f"Publish/ に version {version} の成果物が 1 つもありません")

    tag = None
    commit = run_git(root, "rev-parse", "HEAD")
    for candidate in (version, f"v{version}"):
        if run_git(root, "rev-parse", "-q", "--verify", f"refs/tags/{candidate}"):
            tag = candidate
            commit = run_git(root, "rev-list", "-n", "1", candidate) or commit
            break

    manifest = {
        "$schemaVersion": SCHEMA_VERSION,
        "slug": slug,
        "repository": config.get("repository") or root.name,
        "displayName": sale_display or primary_meta.get("displayName") or primary_name,
        "version": version,
        "releasedAt": released_at,
        "tag": tag,
        "commit": commit or None,
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
    parser.add_argument("--version", help="対象バージョン（既定: 代表パッケージの package.json）")
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
