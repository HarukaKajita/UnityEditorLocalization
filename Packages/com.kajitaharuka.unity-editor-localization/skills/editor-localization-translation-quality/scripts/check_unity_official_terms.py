#!/usr/bin/env python3
"""翻訳カタログの UI 用語が Unity 公式のエディタ翻訳と一致しているかを検査する。

Unity がエディタ UI を公式にローカライズしているのは **ja / ko / zh-Hans / zh-Hant の 4 言語**だけ
（Unity Hub の言語モジュール。2022.3・Unity 6 とも同じ）。この 4 言語では「その言語の Unity 開発者が
どう書くか」を推測する必要が無く、**公式訳が答えそのもの**になる。拡張の文言は Unity のウィンドウの
中に Unity 自身の文言と隣り合って出るので、ここがずれると同じ画面に 1 つの概念の 2 つの呼び名が並ぶ。

対照表の正本は `references/unity-official-terms.json`。判定方針と出所は
`references/unity-official-terms.md` に書いてある。

使い方:
    python3 scripts/check_unity_official_terms.py path/to/Locales
    python3 scripts/check_unity_official_terms.py path/to/Locales --terms Asset,Texture
    python3 scripts/check_unity_official_terms.py path/to/Locales --json

終了コード: 誤用が 1 件でもあれば 1、無ければ 0。

**この検査は「明らかな取り違えの検出器」であって承認器ではない。** 対照表に載っていない語は何も言わない。
逆に、鳴った箇所が本当に Unity の概念を指しているかは人が読んで確かめること（例: 中国語の `文件` は
「ファイル」と「文書」の両方に使われ、後者なら正しい）。
"""

from __future__ import annotations

import argparse
import json
import pathlib
import sys

DEFAULT_GLOSSARY = pathlib.Path(__file__).resolve().parent.parent / "references" / "unity-official-terms.json"
# 公式ローカライズがある言語だけを見る。ほかの言語は公式訳が存在しないので、
# この検査の守備範囲外（コミュニティの慣用で決める。SKILL.md の証拠の序列を参照）。
SUPPORTED = ("ja", "ko", "zh-Hans", "zh-Hant")


def load_catalog(path: pathlib.Path) -> tuple[str, dict[str, str]]:
    document = json.loads(path.read_text(encoding="utf-8"))
    entries = {entry["key"]: entry["value"] for entry in document.get("entries", [])}
    return document.get("locale", path.stem), entries


def check(locales_dir: pathlib.Path, glossary: dict, only: set[str] | None) -> list[dict]:
    findings = []
    for table in sorted(locales_dir.glob("*.json")):
        locale, entries = load_catalog(table)
        if locale not in SUPPORTED:
            continue
        for term, per_locale in glossary["terms"].items():
            if only and term not in only:
                continue
            info = per_locale.get(locale)
            if not info:
                continue
            official = info["official"]
            for wrong in info.get("knownWrong", []):
                for key, value in entries.items():
                    if wrong not in value:
                        continue
                    # 公式訳が誤用形を含む場合（例: 公式 `資源資料庫` と誤用 `資源`）は、
                    # 公式訳の一部として現れた分を誤用と数えない。
                    if official != wrong and wrong in official and value.count(wrong) <= value.count(official):
                        continue
                    findings.append({
                        "file": table.name,
                        "locale": locale,
                        "key": key,
                        "term": term,
                        "official": official,
                        "found": wrong,
                        "value": value,
                    })
    return findings


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("locales_dir", type=pathlib.Path)
    parser.add_argument("--glossary", type=pathlib.Path, default=DEFAULT_GLOSSARY)
    parser.add_argument("--terms", help="対象の用語をカンマ区切りで絞る（既定は全語）")
    parser.add_argument("--json", action="store_true", help="結果を JSON で出す")
    args = parser.parse_args()

    if not args.locales_dir.is_dir():
        print(f"ERROR: Locales ディレクトリがありません: {args.locales_dir}", file=sys.stderr)
        return 2
    glossary = json.loads(args.glossary.read_text(encoding="utf-8"))
    only = {t.strip() for t in args.terms.split(",")} if args.terms else None

    findings = check(args.locales_dir, glossary, only)

    if args.json:
        print(json.dumps(findings, ensure_ascii=False, indent=2))
    elif not findings:
        print(f"[unity-terms] Unity 公式訳と食い違う UI 用語はありません（{args.locales_dir}）")
    else:
        print(f"[unity-terms] Unity 公式のエディタ翻訳と食い違う UI 用語: {len(findings)} 件", file=sys.stderr)
        for f in findings:
            print(f"  {f['file']}: {f['key']}", file=sys.stderr)
            print(f"    {f['term']} は公式では `{f['official']}`。`{f['found']}` を使っています", file=sys.stderr)
            print(f"    {f['value'][:100]}", file=sys.stderr)
        print("", file=sys.stderr)
        print("  Unity の公式訳へ揃えてください。ドキュメントと公式エディタ翻訳で訳語が割れている語は、", file=sys.stderr)
        print("  **開発者が実際に目にするエディタ UI の訳語を優先します**（references/unity-official-terms.md）。", file=sys.stderr)
        print("  文脈上その語が Unity の概念でないなら（中国語の `文件`＝文書 など）例外です。", file=sys.stderr)
    return 1 if findings else 0


if __name__ == "__main__":
    raise SystemExit(main())
