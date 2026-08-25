#!/usr/bin/env python3
"""Unity の言語モジュール（.po）から、UI 用語の公式訳を実測する。

対照表（`references/unity-official-terms.json`）の `official` は「**エディタの .po で msgid が
語と完全一致するエントリの msgstr**」と定義されている。この定義どおりに数え直すためのツール。
ドキュメント由来の推測（`evidence: docs`）を tier 1 へ格上げするときは必ずこれを通す。

.po の場所（Unity Hub で言語モジュールを追加すると出来る）:
    <Unity インストール先>/Editor/Data/Localization/{ja,ko,zh-hans,zh-hant}.po

使い方:
    # 対照表の全語を実測して、対照表の値と食い違うものだけ出す
    python3 scripts/extract_unity_po_terms.py --editor "C:/UnityEditors/2022.3.22f1" --compare

    # 語を指定して、完全一致・部分一致の両方を見る
    python3 scripts/extract_unity_po_terms.py --editor <path> --terms Local,Reset,Cell --context

    # JSON で出す（対照表へ書き戻す前段）
    python3 scripts/extract_unity_po_terms.py --editor <path> --compare --json

読み方の注意:
- **完全一致（msgid == 語）が最強の証拠。** 無い語は、その語が単独のラベルとして Unity の UI に
  出ていないということで、訳が無いわけではない。`--context` で複合語の中の訳を見る。
- **同じ msgid に複数の msgstr は無い**（.po の性質）が、**大文字小文字や複数形が別 msgid**として
  存在する（`Cell` と `Cells`、`Local` と `local`）。まとめて見えるよう変種も出す。
- msgstr が空のエントリは未翻訳。訳として数えない。
"""

from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys

LOCALE_FILES = {"ja": "ja.po", "ko": "ko.po", "zh-Hans": "zh-hans.po", "zh-Hant": "zh-hant.po"}
DEFAULT_GLOSSARY = pathlib.Path(__file__).resolve().parent.parent / "references" / "unity-official-terms.json"

# .po の 1 論理行（"..." の連結）を取り出す
STRING_RE = re.compile(r'"((?:[^"\\]|\\.)*)"')


def unescape(raw: str) -> str:
    return (raw.replace(r"\n", "\n").replace(r"\t", "\t")
               .replace(r"\"", '"').replace(r"\\", "\\"))


def parse_po(path: pathlib.Path) -> dict[str, str]:
    """msgid -> msgstr の辞書を返す（msgctxt は無視し、空の msgstr は落とす）。"""
    entries: dict[str, str] = {}
    current: str | None = None
    buffer: list[str] = []
    msgid: str | None = None

    def flush() -> None:
        nonlocal current, buffer, msgid
        if current == "msgid":
            msgid = "".join(buffer)
        elif current == "msgstr" and msgid is not None:
            value = "".join(buffer)
            if msgid and value:
                entries[msgid] = value
            msgid = None
        buffer = []

    for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
        stripped = line.strip()
        if stripped.startswith("#") or not stripped:
            continue
        if stripped.startswith("msgid_plural"):
            flush()
            current = "skip"
            buffer = []
            continue
        if stripped.startswith("msgid"):
            flush()
            current = "msgid"
            buffer = [unescape(m.group(1)) for m in STRING_RE.finditer(stripped)]
            continue
        if stripped.startswith("msgstr"):
            flush()
            current = "msgstr"
            buffer = [unescape(m.group(1)) for m in STRING_RE.finditer(stripped)]
            continue
        if stripped.startswith('"') and current:
            buffer.extend(unescape(m.group(1)) for m in STRING_RE.finditer(stripped))
    flush()
    return entries


def variants(term: str) -> list[str]:
    """完全一致で探す変種。単独ラベルは大文字始まり・小文字・複数形で別 msgid になる。"""
    forms = {term, term.lower(), term.capitalize()}
    if not term.endswith("s"):
        forms.update({term + "s", term.lower() + "s"})
    return sorted(forms)


def measure(po: dict[str, str], term: str, context: bool) -> dict:
    exact = {}
    for form in variants(term):
        if form in po:
            exact[form] = po[form]

    result = {"exact": exact}
    if context:
        needle = term.lower()
        hits = []
        for msgid, msgstr in po.items():
            if len(msgid) > 60:
                continue
            if re.search(r"\b" + re.escape(needle) + r"\b", msgid.lower()):
                hits.append({"msgid": msgid, "msgstr": msgstr})
        hits.sort(key=lambda h: len(h["msgid"]))
        result["context"] = hits[:12]
        result["contextTotal"] = len(hits)
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--editor", required=True, help="Unity インストール先（Editor/Data/Localization を含む）")
    parser.add_argument("--terms", help="対象の語をカンマ区切りで指定（既定は対照表の全語）")
    parser.add_argument("--glossary", type=pathlib.Path, default=DEFAULT_GLOSSARY)
    parser.add_argument("--compare", action="store_true", help="対照表の値と突き合わせ、食い違いだけ出す")
    parser.add_argument("--context", action="store_true", help="複合語の中の訳も出す")
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args()

    localization = pathlib.Path(args.editor) / "Editor" / "Data" / "Localization"
    if not localization.is_dir():
        print(f"ERROR: 言語モジュールがありません: {localization}", file=sys.stderr)
        print("  Unity Hub の Add modules で言語（Language packs）を追加してください。", file=sys.stderr)
        return 2

    catalogs = {}
    for locale, name in LOCALE_FILES.items():
        path = localization / name
        if path.exists():
            catalogs[locale] = parse_po(path)
    if not catalogs:
        print(f"ERROR: .po が見つかりません: {localization}", file=sys.stderr)
        return 2

    glossary = json.loads(args.glossary.read_text(encoding="utf-8"))
    if args.terms:
        terms = [t.strip() for t in args.terms.split(",") if t.strip()]
    else:
        terms = sorted(glossary["terms"].keys())

    output = {"editor": str(args.editor), "locales": sorted(catalogs), "entryCounts": {k: len(v) for k, v in catalogs.items()}, "terms": {}}
    mismatches = 0
    for term in terms:
        per_locale = {}
        for locale, po in catalogs.items():
            measured = measure(po, term, args.context)
            expected = (glossary["terms"].get(term) or {}).get(locale, {}).get("official")
            # msgstr には末尾の空白が入っていることがある（.po の都合）。比較は strip して行い、
            # 表示は原文のまま出す。空白差を用語の食い違いとして報告すると本物が埋もれる。
            exact_values = list(dict.fromkeys(measured["exact"].values()))
            stripped = {v.strip() for v in exact_values}
            verdict = "no-exact-msgid"
            if exact_values:
                if expected is None:
                    verdict = "new"
                elif expected.strip() in stripped:
                    verdict = "matches"
                else:
                    verdict = "differs"
            if verdict == "differs":
                mismatches += 1
            per_locale[locale] = {**measured, "expected": expected, "verdict": verdict}
        output["terms"][term] = per_locale

    if args.json:
        print(json.dumps(output, ensure_ascii=False, indent=2))
        return 1 if (args.compare and mismatches) else 0

    print(f"[po] {localization}")
    print("  " + " / ".join(f"{k}={len(v)} msgid" for k, v in sorted(catalogs.items())))
    for term, per_locale in output["terms"].items():
        rows = []
        for locale in sorted(per_locale):
            info = per_locale[locale]
            values = list(dict.fromkeys(info["exact"].values()))
            shown = ",".join(values) if values else "-"
            mark = {"matches": "=", "differs": "!", "new": "+", "no-exact-msgid": "?"}[info["verdict"]]
            rows.append(f"{locale}{mark}{shown}")
        if args.compare and all(per_locale[l]["verdict"] != "differs" for l in per_locale):
            continue
        print(f"  {term:<14} " + "  ".join(rows))
        if args.context:
            for locale in sorted(per_locale):
                for hit in per_locale[locale].get("context", [])[:4]:
                    print(f"      {locale} {hit['msgid']!r} -> {hit['msgstr']!r}")

    print("  凡例: = 対照表と一致 / ! 食い違い / + 対照表に無い / ? 完全一致の msgid が無い")
    if args.compare:
        print(f"  食い違い: {mismatches} 件")
    return 1 if (args.compare and mismatches) else 0


if __name__ == "__main__":
    raise SystemExit(main())
