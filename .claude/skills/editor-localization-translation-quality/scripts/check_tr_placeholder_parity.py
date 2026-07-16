#!/usr/bin/env python3
"""Tr 呼び出しの引数個数とカタログ placeholder 個数の整合を静的検査する。

C# コンパイラもロケール validator も検出できない「実行時 FormatException /
引数不足による {n} 露出」を事前に捕捉する（2026-07 UMPD companion routing の
実走で有効性を確認したチェックの汎用化）。

使い方（リポジトリルートで実行）:
  python3 check_tr_placeholder_parity.py \
    --catalog Packages/<pkg>/Editor/.../Locales/en.json \
    --src Packages/<pkg> \
    --method UmpdL10n.Tr [--method OtherFacade.Tr]

仕様:
- --catalog: defaultLocale のテーブル JSON（{"key": "text {0} ..."} のフラット辞書、
  または {"entries": {...}} 形式。どちらも受け付ける）。
- --method: 検査する Tr ファサード呼び出し（"クラス名.メソッド名"）。複数指定可。
- 検出規則: `<Method>(<TextKey定数 or "文字列リテラル">, arg1, arg2, ...)` を
  正規表現＋括弧バランスで走査し、
    渡された追加引数の個数 >= カタログ側 max({n})+1 （不足のみエラー。
    余剰は string.Format では無害だが警告する）
  を全呼び出しで検査する。
- key の解決: 第1引数が "..." リテラルならその値。識別子（TextKey 定数）なら
  --src 配下の *.cs から `internal const string <名前> = "...";` / `public const ...`
  を収集して解決する。解決できない呼び出しは警告として列挙する（失敗にはしない）。
- 終了コード: 不足エラーが 1 件でもあれば 1、なければ 0。
"""

import argparse
import json
import re
import sys
from pathlib import Path

PLACEHOLDER_RE = re.compile(r"\{(\d+)(?::[^{}]*)?\}")
CONST_RE = re.compile(
    r"const\s+string\s+(?P<name>\w+)\s*=\s*\"(?P<value>(?:[^\"\\]|\\.)*)\"\s*;")


def load_catalog(path: Path) -> dict:
    """UEL のテーブル形式（{"locale": "en", "entries": [{"key","value"},…]}）と
    フラット辞書（{"key": "text"}）の両方を受け付ける。"""
    data = json.loads(path.read_text(encoding="utf-8"))
    if isinstance(data, dict) and isinstance(data.get("entries"), list):
        return {e["key"]: e["value"] for e in data["entries"]
                if isinstance(e, dict) and isinstance(e.get("value"), str)}
    if isinstance(data, dict) and isinstance(data.get("entries"), dict):
        data = data["entries"]
    if not isinstance(data, dict):
        raise SystemExit(f"NG: カタログ形式を解釈できません: {path}")
    return {k: v for k, v in data.items() if isinstance(v, str)}


def required_arg_count(text: str) -> int:
    nums = [int(m.group(1)) for m in PLACEHOLDER_RE.finditer(text)]
    return (max(nums) + 1) if nums else 0


def collect_constants(src_root: Path) -> dict:
    consts = {}
    for cs in src_root.rglob("*.cs"):
        try:
            content = cs.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            continue
        for m in CONST_RE.finditer(content):
            consts[m.group("name")] = m.group("value")
    return consts


def split_top_level_args(argtext: str) -> list:
    """括弧・文字列リテラルを考慮してトップレベルのカンマで分割する。"""
    args, depth, in_str, escape, current = [], 0, False, False, []
    for ch in argtext:
        if in_str:
            current.append(ch)
            if escape:
                escape = False
            elif ch == "\\":
                escape = True
            elif ch == '"':
                in_str = False
            continue
        if ch == '"':
            in_str = True
            current.append(ch)
        elif ch in "([{":
            depth += 1
            current.append(ch)
        elif ch in ")]}":
            depth -= 1
            current.append(ch)
        elif ch == "," and depth == 0:
            args.append("".join(current).strip())
            current = []
        else:
            current.append(ch)
    tail = "".join(current).strip()
    if tail:
        args.append(tail)
    return args


def find_calls(content: str, method: str):
    """`method(` の各出現について、対応する閉じ括弧までの引数文字列を返す。"""
    results = []
    start = 0
    needle = method + "("
    while True:
        idx = content.find(needle, start)
        if idx == -1:
            return results
        # 識別子の途中（例: XxxUmpdL10n.Tr）を除外
        if idx > 0 and (content[idx - 1].isalnum() or content[idx - 1] in "._"):
            start = idx + 1
            continue
        depth, in_str, escape = 0, False, False
        for j in range(idx + len(needle) - 1, len(content)):
            ch = content[j]
            if in_str:
                if escape:
                    escape = False
                elif ch == "\\":
                    escape = True
                elif ch == '"':
                    in_str = False
                continue
            if ch == '"':
                in_str = True
            elif ch == "(":
                depth += 1
            elif ch == ")":
                depth -= 1
                if depth == 0:
                    line = content.count("\n", 0, idx) + 1
                    results.append((line, content[idx + len(needle):j]))
                    start = j + 1
                    break
        else:
            return results


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--catalog", required=True, type=Path)
    ap.add_argument("--src", required=True, type=Path)
    ap.add_argument("--method", action="append", required=True,
                    help='例: UmpdL10n.Tr（複数指定可）')
    args = ap.parse_args()

    catalog = load_catalog(args.catalog)
    consts = collect_constants(args.src)
    errors, warnings, checked = [], [], 0

    for cs in args.src.rglob("*.cs"):
        try:
            content = cs.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            continue
        for method in args.method:
            for line, argtext in find_calls(content, method):
                parts = split_top_level_args(argtext)
                if not parts:
                    continue
                first = parts[0]
                if first.startswith('"') and first.endswith('"'):
                    key = first[1:-1]
                else:
                    ident = first.split(".")[-1].strip()
                    key = consts.get(ident)
                    if key is None:
                        warnings.append(f"{cs}:{line}: key を静的解決できません: {first}")
                        continue
                text = catalog.get(key)
                if text is None:
                    errors.append(f"{cs}:{line}: カタログに key がありません: {key}")
                    continue
                need = required_arg_count(text)
                got = len(parts) - 1
                checked += 1
                if got < need:
                    errors.append(
                        f"{cs}:{line}: 引数不足: key '{key}' は {need} 個必要だが {got} 個"
                        f"（実行時 FormatException / {{n}} 露出の原因）")
                elif got > need:
                    warnings.append(
                        f"{cs}:{line}: 引数過剰: key '{key}' は {need} 個だが {got} 個（無害だが不整合）")

    for w in warnings:
        print("WARN:", w)
    for e in errors:
        print("ERROR:", e, file=sys.stderr)
    print(f"checked={checked} errors={len(errors)} warnings={len(warnings)}")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
