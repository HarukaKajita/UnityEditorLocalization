#!/usr/bin/env python3
# 生成物: この内容はテンプレートリポジトリ UnityTemplate_2022_3_22f1 から配布されたコピーです。
# 編集はテンプレート側で行い、scripts/distribute_standard.py で再配布してください。
# source: UnityTemplate_2022_3_22f1/scripts/pipeline/verify_repo_guide.py
# source-sha256: 2d88d524ac1da2c6dc58d23c9aa2e97dd8ff4762a2dc5ca17e6e128ca3de787b
"""リポジトリガイドと実装の整合を機械検証する（ゴールド標準 第2層）。

「文書がリポジトリ自身の状態について主張することは、すべて機械で確かめられる」を原則に、
CLAUDE.md / AGENTS.md・package.json・Packages/manifest.json・Publish/ の整合を検査する。

使い方（対象リポジトリのルートで実行）:
    python3 scripts/pipeline/verify_repo_guide.py            # 検査（error があれば非ゼロ終了）
    python3 scripts/pipeline/verify_repo_guide.py --strict   # warn も error として扱う
    python3 scripts/pipeline/verify_repo_guide.py --json     # 機械可読の結果を出力

設定はリポジトリ直下 `pipeline/repo.json`（手書き・配布対象外）。無い場合も既定値で動作する。

正本: UnityTemplate_2022_3_22f1/scripts/pipeline/verify_repo_guide.py
各開発リポジトリへは scripts/distribute_standard.py が配布する（配布物は編集しない）。
"""

from __future__ import annotations

import argparse
import fnmatch
import hashlib
import json
import os
import re
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path

# ---------------------------------------------------------------------------
# 定数
# ---------------------------------------------------------------------------

ERROR = "error"
WARN = "warn"

# Unity が無視するファイル・フォルダ名（.meta を持たない）
IGNORED_NAME_PATTERNS = (".*", "*~", "*.tmp", "cvs", "CVS")

# 検査 2: リポジトリルート起点とみなすバッククォート内パスの接頭辞
REPO_ROOT_PREFIXES = (
    "Packages/",
    "Assets/",
    "ProjectSettings/",
    "Publish/",
    "docs/",
    "scripts/",
    "pipeline/",
    ".claude/",
    ".agents/",
)

# 検査 2: この語が同じ行にあるパスは「他リポジトリの話」として扱い、実在検査から外す
FOREIGN_REPO_MARKERS = (
    "MySite",
    "external-content",
    "テンプレートリポジトリ",
    "UnityTemplate",
    "基盤側",
    "基盤リポジトリ",
    "利用者",
    "購入者",
    "ホストプロジェクト側",
    "別リポジトリ",
    "他リポジトリ",
)

# 検査 4: テスト整備状況の否定的な主張を検出する
TEST_SUBJECT_RE = re.compile(r"(EditMode|テスト|Tests|testables)")
NEGATIVE_CLAIM_RE = re.compile(r"(未整備|未登録|未導入|存在しない|ありません|は無い|はない|が無い|がない)")

# 検査 10: スキル名とみなすバッククォート内トークン（kebab-case・2語以上）
SKILL_TOKEN_RE = re.compile(r"^[a-z][a-z0-9]*(?:-[a-z0-9]+)+$")
SKILL_LINE_RE = re.compile(r"(スキル|skill|Skill)")
# スキル名の形をしているが、スキルではないことが分かっている語
SKILL_TOKEN_DENYLIST = {
    "kebab-case",
    "camel-case",
    "es-419",
    "zh-hans",
    "zh-hant",
    "pt-br",
    "pt-pt",
    "es-es",
    "keep-a-changelog",
    "read-only",
    "fail-closed",
}

GENERATED_MARKER = "source-sha256:"
GENERATED_HEADER_MARKER = "生成物:"
GENERATED_SHA_RE = re.compile(r"source-sha256:\s*([0-9a-f]{64})")

MAX_PATH_LENGTH = 150

DISTRIBUTED_FILES = (
    "docs/GOLD_STANDARD.md",
    "docs/REPOSITORY_MAP.md",
)
DISTRIBUTED_GLOBS = ("scripts/pipeline/*.py",)
# 標準の正本リポジトリ（テンプレート）で「正本そのもの」であるファイル。
# ここに無い配布物（レジストリから生成される地図など）はテンプレートでも生成物として扱う。
CANONICAL_IN_TEMPLATE = ("docs/GOLD_STANDARD.md",)
CANONICAL_GLOBS_IN_TEMPLATE = ("scripts/pipeline/*.py",)


# ---------------------------------------------------------------------------
# 結果
# ---------------------------------------------------------------------------


@dataclass
class Finding:
    check: str
    severity: str
    message: str
    path: str | None = None

    def format(self) -> str:
        mark = "ERROR" if self.severity == ERROR else "WARN "
        where = f" [{self.path}]" if self.path else ""
        return f"{mark} 検査{self.check}: {self.message}{where}"


def collapse_findings(findings: list[Finding]) -> list[str]:
    """同じ指摘が多数のファイルで出る場合に 1 行へまとめる（翻訳 README 等で埋もれるのを防ぐ）。"""
    grouped: dict[tuple[str, str, str], list[str]] = {}
    order: list[tuple[str, str, str]] = []
    for finding in findings:
        key = (finding.check, finding.severity, finding.message)
        if key not in grouped:
            grouped[key] = []
            order.append(key)
        if finding.path and finding.path not in grouped[key]:
            grouped[key].append(finding.path)

    lines = []
    for key in order:
        check, severity, message = key
        paths = grouped[key]
        mark = "ERROR" if severity == ERROR else "WARN "
        if not paths:
            lines.append(f"{mark} 検査{check}: {message}")
        elif len(paths) <= 3:
            lines.append(f"{mark} 検査{check}: {message} [{', '.join(paths)}]")
        else:
            head = ", ".join(paths[:3])
            lines.append(f"{mark} 検査{check}: {message} [{head} ほか {len(paths) - 3} 件]")
    return lines


@dataclass
class RepoContext:
    root: Path
    config: dict
    tracked: set[str]
    packages: list[tuple[str, Path, dict]] = field(default_factory=list)
    findings: list[Finding] = field(default_factory=list)

    def add(self, check: str, severity: str, message: str, path: str | None = None) -> None:
        self.findings.append(Finding(check, severity, message, path))

    # -- 設定アクセス -------------------------------------------------------

    @property
    def role(self) -> str:
        return self.config.get("role", "product")

    @property
    def is_standard_source(self) -> bool:
        """標準の正本リポジトリ（テンプレート）か。配布物ヘッダを持たない側。"""
        return self.role == "standard"

    def waivers(self, key: str) -> list[str]:
        raw = self.config.get("waivers", {}).get(key, [])
        return [str(item) for item in raw]

    def is_waived(self, key: str, value: str) -> bool:
        return any(pattern in value or fnmatch.fnmatch(value, pattern) for pattern in self.waivers(key))


# ---------------------------------------------------------------------------
# 補助
# ---------------------------------------------------------------------------


def run_git(root: Path, *args: str) -> str:
    result = subprocess.run(
        ["git", "-C", str(root), *args],
        capture_output=True,
        text=True,
        check=False,
    )
    return result.stdout if result.returncode == 0 else ""


def load_json(path: Path) -> dict | None:
    try:
        with path.open(encoding="utf-8") as handle:
            return json.load(handle)
    except (OSError, json.JSONDecodeError):
        return None


def is_unity_ignored_name(name: str) -> bool:
    return any(fnmatch.fnmatch(name, pattern) for pattern in IGNORED_NAME_PATTERNS)


def sha256_text(text: str) -> str:
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


def strip_generated_header(text: str) -> tuple[str | None, str]:
    """生成物ヘッダを取り除き (宣言された sha256, 正本と同一になるはずの本文) を返す。

    ヘッダが無ければ (None, 原文)。除去規則は distribute_standard.py の挿入規則と対になっている。
    """
    match = GENERATED_SHA_RE.search(text)
    if not match:
        return None, text
    declared = match.group(1)

    if text.lstrip().startswith("<!--"):
        start = text.index("<!--")
        end = text.find("-->", start)
        if end == -1:
            return declared, text
        return declared, (text[:start] + text[end + 3 :]).lstrip("\n")

    # 行コメント形式（Python 等）: 生成物ヘッダのコメント行と直後の空行だけを落とす
    lines = text.splitlines(keepends=True)
    output: list[str] = []
    in_header = False
    done = False
    for line in lines:
        if not done and not in_header and line.startswith("#") and GENERATED_HEADER_MARKER in line:
            in_header = True
            continue
        if in_header:
            if line.startswith("#"):
                continue
            in_header, done = False, True
            if line.strip() == "":
                continue
        output.append(line)
    return declared, "".join(output)


def collect_doc_files(ctx: RepoContext) -> list[Path]:
    """検査対象の「このリポジトリ自身が書いた文書」を集める（生成物・正本の規範文書は除外）。"""
    candidates: list[Path] = []
    for name in ("CLAUDE.md", "AGENTS.md", "README.md"):
        path = ctx.root / name
        if path.is_file():
            candidates.append(path)
    # CLAUDE.md と AGENTS.md が同一内容なら片方だけ見る（同じ指摘の二重出力を避ける）
    claude, agents = ctx.root / "CLAUDE.md", ctx.root / "AGENTS.md"
    if claude.is_file() and agents.is_file() and claude.read_bytes() == agents.read_bytes():
        candidates = [p for p in candidates if p != agents]
    docs_dir = ctx.root / "docs"
    if docs_dir.is_dir():
        for path in sorted(docs_dir.rglob("*.md")):
            candidates.append(path)

    result = []
    for path in candidates:
        try:
            text = path.read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError):
            continue
        if GENERATED_MARKER in text[:1000]:
            continue  # 配布された生成物は正本側で検査する
        rel = path.relative_to(ctx.root).as_posix()
        if ctx.is_waived("docFiles", rel):
            continue
        result.append(path)
    return result


def package_files(package_dir: Path) -> list[Path]:
    """パッケージ配下のファイルを Unity 無視名を除いて列挙する（.meta を含む）。"""
    result: list[Path] = []
    for current, dirnames, filenames in os.walk(package_dir):
        dirnames[:] = [d for d in dirnames if not is_unity_ignored_name(d)]
        for name in filenames:
            if is_unity_ignored_name(name) and not name.endswith(".meta"):
                continue
            result.append(Path(current) / name)
    return result


# ---------------------------------------------------------------------------
# 検査 1: CLAUDE.md と AGENTS.md がバイト同一
# ---------------------------------------------------------------------------


def check_01_guide_pair(ctx: RepoContext) -> None:
    claude = ctx.root / "CLAUDE.md"
    agents = ctx.root / "AGENTS.md"
    if not claude.is_file() and not agents.is_file():
        ctx.add("1", ERROR, "CLAUDE.md と AGENTS.md が両方とも存在しません（GOLD_STANDARD §2.2 必須）")
        return
    if not claude.is_file():
        ctx.add("1", ERROR, "CLAUDE.md がありません（AGENTS.md のみ存在）")
        return
    if not agents.is_file():
        ctx.add("1", ERROR, "AGENTS.md がありません（CLAUDE.md のみ存在）")
        return
    if claude.read_bytes() != agents.read_bytes():
        ctx.add("1", ERROR, "CLAUDE.md と AGENTS.md の内容が一致しません（常に同一内容を保つ規約）")


# ---------------------------------------------------------------------------
# 検査 2: 文書内の相対パス参照が実在する
# ---------------------------------------------------------------------------

MD_LINK_RE = re.compile(r"\[[^\]]*\]\(([^)]+)\)")
BACKTICK_RE = re.compile(r"`([^`\n]+)`")


def _path_exists(root: Path, rel: str) -> bool:
    target = rel.rstrip("/")
    if any(ch in target for ch in "*?["):
        return bool(list(root.glob(target)))
    return (root / target).exists()


def _skip_path_token(token: str) -> bool:
    """プレースホルダを含む「形の説明」は実在検査の対象にしない。"""
    return (
        not token
        or "{" in token
        or "}" in token
        or "..." in token
        or "<" in token
        or "|" in token
        or token.startswith("http")
        or token.startswith("#")
        or token.startswith("mailto:")
    )


def check_02_relative_paths(ctx: RepoContext) -> None:
    for doc in collect_doc_files(ctx):
        rel_doc = doc.relative_to(ctx.root).as_posix()
        text = doc.read_text(encoding="utf-8")

        # (a) 相対 markdown リンク — 精度が高いので error
        for target in MD_LINK_RE.findall(text):
            target = target.split(" ")[0].split("#")[0].strip()
            if _skip_path_token(target) or target.startswith("/"):
                continue
            if ctx.is_waived("docPaths", target):
                continue
            if not _path_exists(doc.parent, target) and not _path_exists(ctx.root, target):
                ctx.add("2", ERROR, f"リンク先が存在しません: {target}", rel_doc)

        # (b) リポジトリルート起点のバッククォート付きパス
        #     他リポジトリに言及している行のパスはここからは検証できないので対象外にする
        #     （warn として残すと恒常的なノイズになり、警告全体が無視されるようになるため）
        for line in text.splitlines():
            if any(marker in line for marker in FOREIGN_REPO_MARKERS):
                continue
            for token in BACKTICK_RE.findall(line):
                token = token.strip()
                if _skip_path_token(token):
                    continue
                if not token.startswith(REPO_ROOT_PREFIXES):
                    continue
                if ctx.is_waived("docPaths", token):
                    continue
                if _path_exists(ctx.root, token):
                    continue
                ctx.add("2", ERROR, f"参照先が存在しません: `{token}`", rel_doc)


# ---------------------------------------------------------------------------
# 検査 3: 配布された標準が正本と一致する
# ---------------------------------------------------------------------------


def _distributed_paths(ctx: RepoContext) -> list[Path]:
    paths = []
    for rel in DISTRIBUTED_FILES:
        path = ctx.root / rel
        if path.is_file():
            paths.append(path)
    for pattern in DISTRIBUTED_GLOBS:
        paths.extend(sorted(ctx.root.glob(pattern)))
    return paths


def check_03_distributed_standard(ctx: RepoContext) -> None:
    for path in _distributed_paths(ctx):
        rel = path.relative_to(ctx.root).as_posix()
        try:
            text = path.read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError):
            ctx.add("3", ERROR, "配布物を読み取れません", rel)
            continue

        is_canon_here = ctx.is_standard_source and (
            rel in CANONICAL_IN_TEMPLATE
            or any(fnmatch.fnmatch(rel, pattern) for pattern in CANONICAL_GLOBS_IN_TEMPLATE)
        )
        if is_canon_here:
            if GENERATED_MARKER in text[:1000]:
                ctx.add("3", ERROR, "標準の正本に生成物ヘッダが付いています（正本を配布物で上書きした疑い）", rel)
            continue

        declared, body = strip_generated_header(text)
        if declared is None:
            ctx.add(
                "3",
                ERROR,
                "配布物ヘッダ（source-sha256）がありません。テンプレートから再配布してください",
                rel,
            )
            continue
        actual = sha256_text(body)
        if actual != declared:
            ctx.add(
                "3",
                ERROR,
                f"配布物が改変されています（宣言 {declared[:12]}… / 実際 {actual[:12]}…）。編集はテンプレート側で行ってください",
                rel,
            )


# ---------------------------------------------------------------------------
# 検査 4: テスト整備状況の記述と実態の一致
# ---------------------------------------------------------------------------


def check_04_test_claims(ctx: RepoContext) -> None:
    manifest = load_json(ctx.root / "Packages" / "manifest.json") or {}
    testables = manifest.get("testables") or []
    has_tests = any(
        list((pkg_dir / "Tests").rglob("*.asmdef")) for _, pkg_dir, _ in ctx.packages
    )
    if not testables and not has_tests:
        return  # 未整備が事実なので記述と矛盾しない

    for doc in collect_doc_files(ctx):
        rel_doc = doc.relative_to(ctx.root).as_posix()
        for number, line in enumerate(doc.read_text(encoding="utf-8").splitlines(), start=1):
            if not TEST_SUBJECT_RE.search(line) or not NEGATIVE_CLAIM_RE.search(line):
                continue
            if ctx.is_waived("claims", line.strip()):
                continue
            excerpt = line.strip()
            if len(excerpt) > 90:
                excerpt = excerpt[:90] + "…"
            ctx.add(
                "4",
                WARN,
                f"テスト整備を否定する記述がありますが、実際には整備済みです（testables {len(testables)} 件 / Tests asmdef {'あり' if has_tests else 'なし'}）: {excerpt}",
                f"{rel_doc}:{number}",
            )


# ---------------------------------------------------------------------------
# 検査 5: Tests asmdef と testables の整合
# ---------------------------------------------------------------------------


def check_05_testables(ctx: RepoContext) -> None:
    manifest_path = ctx.root / "Packages" / "manifest.json"
    manifest = load_json(manifest_path)
    if manifest is None:
        if ctx.packages:
            ctx.add("5", ERROR, "Packages/manifest.json を読み取れません")
        return
    testables = manifest.get("testables") or []
    package_names = {name for name, _, _ in ctx.packages}

    for name in testables:
        if name not in package_names:
            ctx.add("5", ERROR, f"testables に登録された {name} が Packages/ に存在しません", "Packages/manifest.json")

    for name, pkg_dir, _ in ctx.packages:
        tests_asmdefs = list((pkg_dir / "Tests").rglob("*.asmdef"))
        if name not in testables:
            ctx.add("5", WARN, f"{name} が testables に登録されていません（GOLD_STANDARD §2.3）", "Packages/manifest.json")
        if not tests_asmdefs:
            ctx.add("5", WARN, f"{name} に Tests/**/*.asmdef がありません（EditMode テスト未整備）")


# ---------------------------------------------------------------------------
# 検査 6: .meta 完全性と git 追跡
# ---------------------------------------------------------------------------


def check_06_meta_completeness(ctx: RepoContext) -> None:
    for name, pkg_dir, _ in ctx.packages:
        # パッケージルート自身の .meta は UPM 慣例で持たない
        root_meta = pkg_dir.parent / f"{pkg_dir.name}.meta"
        if root_meta.exists():
            ctx.add(
                "6",
                ERROR,
                f"パッケージルートに .meta があります（UPM 慣例に反する。GOLD_STANDARD §2.1）",
                root_meta.relative_to(ctx.root).as_posix(),
            )

        for current, dirnames, filenames in os.walk(pkg_dir):
            dirnames[:] = [d for d in dirnames if not is_unity_ignored_name(d)]
            entries = [(Path(current) / d, True) for d in dirnames]
            entries += [
                (Path(current) / f, False)
                for f in filenames
                if not f.endswith(".meta") and not is_unity_ignored_name(f)
            ]
            for entry, _is_dir in entries:
                rel = entry.relative_to(ctx.root).as_posix()
                meta_rel = f"{rel}.meta"
                if not (ctx.root / meta_rel).exists():
                    ctx.add("6", ERROR, "ディスク上に .meta がありません", rel)
                elif meta_rel not in ctx.tracked:
                    ctx.add("6", ERROR, ".meta が git 追跡されていません（利用者側でだけ壊れる）", meta_rel)
                if not _is_dir and rel not in ctx.tracked:
                    ctx.add("6", ERROR, "アセット本体が git 追跡されていません", rel)
        _ = name


# ---------------------------------------------------------------------------
# 検査 7: パス長 150 字未満
# ---------------------------------------------------------------------------


def check_07_path_length(ctx: RepoContext) -> None:
    for name, pkg_dir, _ in ctx.packages:
        for path in package_files(pkg_dir):
            rel = f"{name}/{path.relative_to(pkg_dir).as_posix()}"
            if len(rel) >= MAX_PATH_LENGTH:
                ctx.add("7", ERROR, f"パス長 {len(rel)} 字（UAS 2.1.e は 150 字未満）", rel)


# ---------------------------------------------------------------------------
# 検査 8: package.json の URL 3 種が /products/{slug}/ 規約に合う
# ---------------------------------------------------------------------------

URL_SUFFIXES = {
    "documentationUrl": "",
    "changelogUrl": "changelog/",
    "licensesUrl": "licenses/",
}
SITE_BASE = "https://kajitaharuka.com/products/"


def check_08_package_urls(ctx: RepoContext) -> None:
    declared_slug = ctx.config.get("productSlug")
    for name, pkg_dir, meta in ctx.packages:
        rel = (pkg_dir / "package.json").relative_to(ctx.root).as_posix()
        slugs = set()
        for key, suffix in URL_SUFFIXES.items():
            url = meta.get(key)
            if not url:
                ctx.add("8", ERROR, f"{key} がありません", rel)
                continue
            if "{{" in url:
                continue  # テンプレートの雛形値
            expected_prefix = SITE_BASE
            if not url.startswith(expected_prefix) or not url.endswith("/"):
                ctx.add("8", ERROR, f"{key} が URL 規約（{SITE_BASE}{{slug}}/…）に合いません: {url}", rel)
                continue
            tail = url[len(expected_prefix) :]
            parts = [p for p in tail.split("/") if p]
            if suffix:
                expected_tail = suffix.rstrip("/")
                if len(parts) != 2 or parts[1] != expected_tail:
                    ctx.add("8", ERROR, f"{key} の末尾が /{expected_tail}/ ではありません: {url}", rel)
                    continue
            elif len(parts) != 1:
                ctx.add("8", ERROR, f"documentationUrl は商品ページ直下を指す必要があります: {url}", rel)
                continue
            slugs.add(parts[0])

        if len(slugs) > 1:
            ctx.add("8", ERROR, f"URL 3 種の slug が一致しません: {sorted(slugs)}", rel)
        elif slugs and declared_slug and next(iter(slugs)) != declared_slug:
            ctx.add(
                "8",
                ERROR,
                f"package.json の slug `{next(iter(slugs))}` が pipeline/repo.json の productSlug `{declared_slug}` と異なります",
                rel,
            )
        _ = name


# ---------------------------------------------------------------------------
# 検査 9: 販売単位の Exporter 設定アセットと Publish/ 命名
# ---------------------------------------------------------------------------

PUBLISH_EXTRA_ALLOWED = ("README.md", ".gitkeep")


def check_09_sale_unit(ctx: RepoContext) -> None:
    sale_unit = ctx.config.get("saleUnit")
    if ctx.role == "product":
        if not sale_unit:
            ctx.add("9", WARN, "pipeline/repo.json に saleUnit の宣言がありません（販売単位の成果物を検査できません）")
        else:
            assets = sale_unit.get("exporterAssets") or []
            if not assets:
                ctx.add("9", WARN, "saleUnit.exporterAssets が空です（Exporter 設定アセットの実在を検査できません）")
            for rel in assets:
                if not (ctx.root / rel).is_file():
                    ctx.add("9", ERROR, "Exporter 設定アセットが存在しません", rel)
                elif rel not in ctx.tracked:
                    ctx.add("9", ERROR, "Exporter 設定アセットが git 追跡されていません（再現不能）", rel)

    publish_dir = ctx.root / "Publish"
    if not publish_dir.is_dir():
        return
    display_names = {meta.get("displayName") for _, _, meta in ctx.packages if meta.get("displayName")}
    if sale_unit and sale_unit.get("displayName"):
        display_names.add(sale_unit["displayName"])
    package_names = {name for name, _, _ in ctx.packages}

    patterns: list[re.Pattern[str]] = []
    for display in display_names:
        patterns.append(re.compile(rf"^{re.escape(display)}-\d+\.\d+\.\d+.*\.zip$"))
    for name in package_names:
        patterns.append(re.compile(rf"^{re.escape(name)}-\d+\.\d+\.\d+.*\.(tgz|unitypackage|zip)$"))
    patterns.append(re.compile(r"^release-\d+\.\d+\.\d+.*\.json$"))

    for path in sorted(publish_dir.rglob("*")):
        if path.is_dir():
            continue
        rel = path.relative_to(ctx.root).as_posix()
        name = path.name
        if name in PUBLISH_EXTRA_ALLOWED or name.startswith("."):
            continue
        if ctx.is_waived("publishFiles", rel):
            continue
        if not any(pattern.match(name) for pattern in patterns):
            ctx.add("9", WARN, "Publish/ の命名規約（§2.7）に合いません", rel)


# ---------------------------------------------------------------------------
# 検査 10: 参照スキル名がいずれかのスコープに実在する
# ---------------------------------------------------------------------------


def _skill_scopes(ctx: RepoContext) -> tuple[set[str], list[str], bool]:
    names: set[str] = set()
    scopes: list[str] = []
    home = Path.home()
    candidates = [home / ".claude" / "skills", home / ".agents" / "skills"]
    candidates += [ctx.root / ".claude" / "skills", ctx.root / ".agents" / "skills"]
    candidates += sorted((ctx.root / "Packages").glob("*/skills")) if (ctx.root / "Packages").is_dir() else []

    site_root = resolve_site_repo(ctx)
    site_resolved = site_root is not None
    if site_root is not None:
        candidates.append(site_root / "skills")
        # レジストリで解決できる他リポジトリの同梱スキルも「パイプライン内に実在する」とみなす
        for repo_path in resolve_registry_repos(site_root):
            candidates += sorted(repo_path.glob("Packages/*/skills"))

    for directory in candidates:
        if not directory.is_dir():
            continue
        scopes.append(str(directory))
        for child in directory.iterdir():
            if child.is_dir():
                names.add(child.name)
    return names, scopes, site_resolved


def check_10_skill_references(ctx: RepoContext) -> None:
    names, scopes, site_resolved = _skill_scopes(ctx)
    if not scopes:
        ctx.add("10", WARN, "スキルのスコープを 1 つも解決できませんでした（検査をスキップ）")
        return

    for doc in collect_doc_files(ctx):
        rel_doc = doc.relative_to(ctx.root).as_posix()
        for number, line in enumerate(doc.read_text(encoding="utf-8").splitlines(), start=1):
            if not SKILL_LINE_RE.search(line):
                continue
            for token in BACKTICK_RE.findall(line):
                token = token.strip()
                if not SKILL_TOKEN_RE.match(token) or token in SKILL_TOKEN_DENYLIST:
                    continue
                if token in names or ctx.is_waived("skills", token):
                    continue
                if site_resolved:
                    ctx.add("10", ERROR, f"参照されたスキル `{token}` がどのスコープにも存在しません", f"{rel_doc}:{number}")
                else:
                    ctx.add(
                        "10",
                        WARN,
                        f"参照されたスキル `{token}` を確認できません（MySite の skills/ を解決できないため）",
                        f"{rel_doc}:{number}",
                    )


# ---------------------------------------------------------------------------
# 追加検査（枠外・warn）
# ---------------------------------------------------------------------------


def check_extra(ctx: RepoContext) -> None:
    if not (ctx.root / "pipeline" / "repo.json").is_file():
        ctx.add("+", WARN, "pipeline/repo.json がありません（既定値で検査しました）")
    for name, pkg_dir, meta in ctx.packages:
        rel = (pkg_dir / "package.json").relative_to(ctx.root).as_posix()
        if not meta.get("unityRelease"):
            ctx.add("+", WARN, f"{name}: package.json に unityRelease がありません（GOLD_STANDARD §2.5）", rel)
        if not (pkg_dir / "Third Party Notices.md").is_file():
            ctx.add("+", WARN, f"{name}: Third Party Notices.md がありません（UAS 1.2.a）")
        for required in ("README.md", "CHANGELOG.md", "LICENSE.md"):
            if not (pkg_dir / required).is_file():
                ctx.add("+", WARN, f"{name}: {required} がありません（GOLD_STANDARD §2.5）")


# ---------------------------------------------------------------------------
# 個人設定・サイトリポジトリの解決（設計 §5 の解決手順と同じ順序）
# ---------------------------------------------------------------------------

PERSONAL_CONFIG = Path.home() / ".kajitaharuka-pipeline.json"


def load_personal_config() -> dict:
    return load_json(PERSONAL_CONFIG) or {}


def resolve_site_repo(ctx: RepoContext) -> Path | None:
    """MySite（site ロール）のローカルパスを解決する。見つからなければ None。"""
    personal = load_personal_config()
    candidates: list[str] = []
    override = (personal.get("overrides") or {}).get("mysite")
    if override:
        candidates.append(override)
    if personal.get("registryPath"):
        candidates.append(str(Path(personal["registryPath"]).expanduser().parent.parent))
    candidates += ["~/dev/MySite", str(ctx.root.parent.parent / "MySite")]

    for candidate in candidates:
        path = Path(candidate).expanduser()
        if (path / "pipeline" / "repositories.json").is_file() or (path / "skills").is_dir():
            return path
    return None


def resolve_registry_repos(site_root: Path) -> list[Path]:
    """レジストリに載っているリポジトリのうち、ローカルで解決できたものを返す。

    解決順は設計 §5 と同じ: 個人設定の overrides → localPathCandidates →
    `git remote get-url origin` による同一性検証。検証に通らない候補は採用しない。
    """
    registry = load_json(site_root / "pipeline" / "repositories.json")
    if not registry:
        return []
    overrides = (load_personal_config().get("overrides") or {})
    resolved: list[Path] = []
    for entry in registry.get("repositories", []):
        repo_id = entry.get("id")
        remote = entry.get("remote") or {}
        expected = {remote.get("https"), remote.get("ssh")} - {None}
        candidates = []
        if repo_id and repo_id in overrides:
            candidates.append(overrides[repo_id])
        candidates += entry.get("localPathCandidates") or []
        for candidate in candidates:
            path = Path(str(candidate)).expanduser()
            if not (path / ".git").exists():
                continue
            actual = run_git(path, "remote", "get-url", "origin").strip()
            if expected and actual and actual not in expected:
                continue  # 同じパスにある別リポジトリを掴まない
            resolved.append(path)
            break
    return resolved


# ---------------------------------------------------------------------------
# エントリポイント
# ---------------------------------------------------------------------------


def build_context(root: Path) -> RepoContext:
    config = load_json(root / "pipeline" / "repo.json") or {}
    tracked = {line for line in run_git(root, "ls-files").splitlines() if line}
    ctx = RepoContext(root=root, config=config, tracked=tracked)

    packages_dir = root / "Packages"
    if packages_dir.is_dir():
        for entry in sorted(packages_dir.iterdir()):
            if not entry.is_dir():
                continue
            meta = load_json(entry / "package.json")
            if meta is None or not meta.get("name"):
                continue
            ctx.packages.append((meta["name"], entry, meta))
    return ctx


CHECKS = (
    check_01_guide_pair,
    check_02_relative_paths,
    check_03_distributed_standard,
    check_04_test_claims,
    check_05_testables,
    check_06_meta_completeness,
    check_07_path_length,
    check_08_package_urls,
    check_09_sale_unit,
    check_10_skill_references,
    check_extra,
)


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description="リポジトリガイドと実装の整合を検査する")
    parser.add_argument("--root", default=".", help="対象リポジトリのルート（既定: カレント）")
    parser.add_argument("--strict", action="store_true", help="warn も失敗として扱う")
    parser.add_argument("--json", action="store_true", help="結果を JSON で出力する")
    parser.add_argument("--only", help="実行する検査番号をカンマ区切りで指定（例: 1,3,6）")
    args = parser.parse_args(argv)

    root = Path(args.root).resolve()
    if not root.is_dir():
        print(f"ERROR: 対象ディレクトリがありません: {root}", file=sys.stderr)
        return 2

    ctx = build_context(root)
    only = {item.strip() for item in args.only.split(",")} if args.only else None
    for check in CHECKS:
        number = check.__name__.split("_")[1].lstrip("0") or "0"
        if only is not None and number not in only and check is not check_extra:
            continue
        if only is not None and check is check_extra and "+" not in only:
            continue
        check(ctx)

    errors = [f for f in ctx.findings if f.severity == ERROR]
    warnings = [f for f in ctx.findings if f.severity == WARN]

    if args.json:
        payload = {
            "root": str(root),
            "role": ctx.role,
            "packages": [name for name, _, _ in ctx.packages],
            "errors": [f.__dict__ for f in errors],
            "warnings": [f.__dict__ for f in warnings],
        }
        print(json.dumps(payload, ensure_ascii=False, indent=2))
    else:
        label = ctx.config.get("repository") or root.name
        print(f"== 標準準拠検査: {label}（role={ctx.role}, packages={len(ctx.packages)}）")
        for line in collapse_findings(ctx.findings):
            print(line)
        if not ctx.findings:
            print("問題は見つかりませんでした。")
        print(f"-- error {len(errors)} 件 / warn {len(warnings)} 件")

    if errors:
        return 1
    if args.strict and warnings:
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
