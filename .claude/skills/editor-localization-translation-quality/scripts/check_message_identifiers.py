#!/usr/bin/env python3
"""カタログの文言が名乗っている識別子が、実際にコードへ存在するかを突き合わせる。

用途: 文言が**実在しない名前**でユーザーを誤誘導しているのを見つける。
翻訳の良し悪しでも placeholder でもないので、既存の検査では 1 つも拾えない層である。

実例（2026-07-29 / UberMaterialPropertyDrawer）:
- `[UberToggle]` のエラーが自分を `[Toggle]` と名乗っていた。**`[Toggle]` は Unity 標準の
  別 drawer** で、ShaderLab にそう書いてもこのパッケージの機能にはならない。
- `[UberEnum]` のエラーが `MaterialEnum` という型名を出していたが、この名前は
  リポジトリのどこにも存在しなかった。
どちらも全 19 ロケールへ翻訳済みで、19 言語すべてが誤った名前を伝えていた。

何を見るか:
- 角括弧属性 `[Xxx]`（ShaderLab / C# の属性として書かれる名前）
- PascalCase の識別子（型名・メソッド名らしきもの）
- `Xxx.Yyy` のドット区切り参照

これらを既定ロケールの値から抜き出し、`--src` 配下を grep して**ヒット 0 件**のものを報告する。
ヒットしても意味が正しいとは限らないので、これは「明らかな嘘の検出器」であって承認器ではない。

使い方:
  python3 check_message_identifiers.py \
    --catalog path/to/Locales/en.json \
    --src path/to/Packages/<package-root> [--src path/to/other-root] \
    [--allow Half --allow UNorm8]

終了コード: 実在しない識別子があれば 1、無ければ 0。
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

# 角括弧属性。ShaderLab / C# のどちらでもこの形で書かれる
BRACKET_RE = re.compile(r"\[([A-Z][A-Za-z0-9_]*)\]")
# ドット区切りの参照。型名 + メンバー
DOTTED_RE = re.compile(r"\b([A-Z][A-Za-z0-9_]*(?:\.[A-Z][A-Za-z0-9_]*)+)\b")
# 単独の PascalCase。2 語以上に見えるものだけ（Error / Warning のような普通名詞を避ける）
PASCAL_RE = re.compile(r"\b([A-Z][a-z0-9]+(?:[A-Z][a-z0-9]+)+)\b")

# コードを探しても出てこないが誤りではない語。UI 上の概念名・フォーマット名など
DEFAULT_ALLOW = {
    "GameObject", "ScriptableObject", "MonoBehaviour", "EditorWindow",
    "ProjectSettings", "PackageManager", "AssetDatabase",
}

# 検索対象は**実装だけ**。ドキュメントは根拠にしない。
# CHANGELOG が「MaterialEnum という誤りを直した」と書いているだけで実在扱いになり、
# 肝心の誤りを見逃す（実際にこれで一度取り逃した）。属性名は .shader に書かれるので含める。
SOURCE_SUFFIXES = {".cs", ".shader", ".cginc", ".hlsl", ".asmdef"}


def collect_identifiers(values: dict[str, str]) -> dict[tuple[str, str], list[str]]:
    """(識別子, 種別) → それを含むキーの一覧。種別で照合の仕方を変える。"""
    found: dict[tuple[str, str], list[str]] = {}
    for key, value in values.items():
        for pattern, kind in ((BRACKET_RE, "bracket"), (DOTTED_RE, "dotted"), (PASCAL_RE, "pascal")):
            for name in pattern.findall(value):
                found.setdefault((name, kind), []).append(key)
    return found


def exists_in_source(name: str, kind: str, haystack: str) -> bool:
    """識別子が実在するか。**部分一致では判定しない。**

    `[Toggle]` は `UberToggle` の部分文字列なので、素の `in` では
    「実在する」と誤判定して肝心の誤りを見逃す（実際にこれで一度取り逃した）。
    角括弧属性は書かれ方（`[X]` / `[X(...)]` / `class XDrawer`）で、
    それ以外は語境界で照合する。
    """
    if kind == "bracket":
        patterns = [
            re.escape(f"[{name}]"),
            re.escape(f"[{name}(") ,
            rf"\bclass\s+{re.escape(name)}Drawer\b",
            rf"\bclass\s+{re.escape(name)}\b",
        ]
        return any(re.search(p, haystack) for p in patterns)

    if re.search(rf"\b{re.escape(name)}\b", haystack) is not None:
        return True

    if kind == "dotted":
        # `型名.メンバー` は、**コード上でその綴りのまま現れるとは限らない。** インスタンス経由で
        # 書くのが普通だからである（文言の `CaptureOptions.Repaint` に対し、実装は
        # `focusedTabOptions.Repaint = ...`）。完全一致だけで判定すると、正しい参照を
        # 「実在しない」と鳴らして検査そのものを信用されなくする（2026-08-14 に UEWCE で実測）。
        # 型が**宣言として**在り、かつ各メンバー名が在ることを見る。組み合わせの正しさまでは
        # 保証しない（この検査は明らかな嘘の検出器であって承認器ではない、という前提のまま）。
        head, *members = name.split(".")
        if not re.search(rf"\b(?:class|struct|enum|interface|record)\s+{re.escape(head)}\b", haystack):
            return False
        return all(re.search(rf"\b{re.escape(member)}\b", haystack) for member in members)

    return False


def build_haystack(roots: list[Path]) -> str:
    """検索対象のソースを 1 つの文字列に連結する（件数が小さいので素朴でよい）。"""
    chunks: list[str] = []
    for root in roots:
        if root.is_file():
            files = [root]
        else:
            files = [p for p in root.rglob("*") if p.is_file() and p.suffix in SOURCE_SUFFIXES]
        for path in files:
            # カタログ自身は「文言に書いてあるから実在する」という循環になるので除く
            if path.parent.name == "Locales":
                continue
            try:
                chunks.append(path.read_text(encoding="utf-8", errors="ignore"))
            except OSError:
                continue
    return "\n".join(chunks)


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description="文言中の識別子が実在するかを検査する")
    parser.add_argument("--catalog", required=True, help="既定ロケールの locale JSON")
    parser.add_argument("--src", action="append", required=True, help="検索対象のルート（複数可）")
    parser.add_argument("--allow", action="append", default=[], help="実在しなくてよい語")
    args = parser.parse_args(argv)

    catalog = json.loads(Path(args.catalog).read_text(encoding="utf-8"))
    values = {e["key"]: e["value"] for e in catalog["entries"]}
    allow = DEFAULT_ALLOW | set(args.allow)

    haystack = build_haystack([Path(s) for s in args.src])
    if not haystack:
        print("ERROR: --src 配下に検索対象のファイルがありません", file=sys.stderr)
        return 2

    missing: list[tuple[str, str, list[str]]] = []
    for (name, kind), keys in sorted(collect_identifiers(values).items()):
        if name in allow:
            continue
        if exists_in_source(name, kind, haystack):
            continue
        missing.append((name, kind, sorted(set(keys))))

    if not missing:
        print(f"OK: 文言中の識別子はすべて実在します（{Path(args.catalog).name}）")
        return 0

    print(f"NG: 実在しない識別子 {len(missing)} 件")
    for name, kind, keys in missing:
        shown = f"[{name}]" if kind == "bracket" else name
        print(f"  {shown}  ← {', '.join(keys)}")
    print()
    print("  実在する名前へ直すか、コードに無いのが正しい語なら --allow で宣言してください。")
    print("  よくある原因: 実装をリネームしたのに文言が旧名のまま / 似た名前の別 API と取り違え。")
    return 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
