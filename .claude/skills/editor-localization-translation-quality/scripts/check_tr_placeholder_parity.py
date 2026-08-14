#!/usr/bin/env python3
"""Tr 呼び出しの引数個数とカタログ placeholder 個数の整合を静的検査する。

C# コンパイラもロケール validator も検出できない「実行時 FormatException /
引数不足による {n} 露出」を事前に捕捉する（2026-07 UMPD companion routing の
実走で有効性を確認したチェックの汎用化）。

使い方（リポジトリルートで実行）:
  python3 check_tr_placeholder_parity.py \
    --catalog Packages/<pkg>/Editor/.../Locales/en.json \
    --src Packages/<pkg> [--src Packages/<companion-pkg>] \
    --method UmpdL10n.Tr [--method OtherFacade.Tr]

仕様:
- --catalog: defaultLocale のテーブル JSON（{"key": "text {0} ..."} のフラット辞書、
  または {"entries": {...}} 形式。どちらも受け付ける）。
- --src: 走査するルート。**複数指定可**（スイート構成で core と companion が別パッケージに
  分かれている場合、両方を渡さないと定数の宣言側か呼び出し側のどちらかを取り逃がす）。
  重なり合うルートを渡してもファイル単位で重複排除するので、同じ指摘が二重に出ることはない。
- --method: 検査する Tr ファサード呼び出し（"クラス名.メソッド名"）。複数指定可。
- 検出規則: `<Method>(<TextKey定数 or "文字列リテラル">, arg1, arg2, ...)` を
  正規表現＋括弧バランスで走査し、
    渡された追加引数の個数 >= カタログ側 max({n})+1 （不足のみエラー。
    余剰は string.Format では無害だが警告する）
  を全呼び出しで検査する。
- key の解決: 第1引数が "..." リテラルならその値。識別子（TextKey 定数）なら
  --src 配下の *.cs から `internal const string <名前> = "...";` / `public const ...`
  を収集して解決する。解決できない呼び出しは警告として列挙する（失敗にはしない）。
- 終了コード: エラーが 1 件でもあれば 1、なければ 0。

**「1 件も検査できなかった」は成功ではない**（2026-08-14 に実測した偽の緑）。呼び出しは見つかって
いるのに定数を 1 つも解決できず、全件が警告になって `checked=0 / errors=0` で正常終了していた。
原因は --src の渡し漏れ（当時は複数指定が効かず最後の 1 つしか使われなかった）で、定数の宣言が
走査範囲の外にあったこと。検査したつもりのまま素通りするので、以下は**エラー**として落とす:
  - 呼び出しを検出したのに解決できた key が 1 つも無い
  - 定数参照らしい形（`XxxTextKey.Foo` / `XxxKey.Foo`）なのに定数表に無い
"""

import argparse
import json
import re
import sys
from pathlib import Path

PLACEHOLDER_RE = re.compile(r"\{(\d+)(?::[^{}]*)?\}")
CONST_RE = re.compile(
    r"const\s+string\s+(?P<name>\w+)\s*=\s*\"(?P<value>(?:[^\"\\]|\\.)*)\"\s*;")
# 翻訳キー定数の参照らしい形（`EpeTextKey.Foo` / `UmpdKey.Bar`）。この形で解決できないのは
# 走査範囲の指定ミスであって「動的なキーなので解決できない」ではないため、警告ではなく落とす。
LOOKS_LIKE_KEY_CONST_RE = re.compile(r"^\w*(?:TextKey|Keys?)\.\w+$")


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


def load_catalogs(paths: list) -> dict:
    """複数のカタログを 1 つの key→text へまとめる（**先に渡した方が勝つ**）。

    1 つのパッケージが複数 scope を持つ構成（本体＋ `Samples~` など）では、呼び出し側の
    コードが 1 本の走査範囲に混在する。カタログを 1 つしか渡せないと、もう一方の scope の
    キーが軒並み「カタログに key がありません」になるので、まとめて渡せるようにする。
    引数個数の検査はキー単位で完結するため、束ねても判定は変わらない。
    """
    merged: dict = {}
    for path in paths:
        for key, value in load_catalog(path).items():
            merged.setdefault(key, value)
    return merged


def required_arg_count(text: str) -> int:
    nums = [int(m.group(1)) for m in PLACEHOLDER_RE.finditer(text)]
    return (max(nums) + 1) if nums else 0


def iter_source_files(src_roots: list) -> list:
    """--src で与えた全ルート配下の *.cs を、実体パスで重複排除して返す。

    重なり合うルート（`Packages` と `Packages/foo` の同時指定など）を許すための重複排除。
    排除しないと同じファイルを 2 度走査し、同じ呼び出しが 2 件の指摘になる。
    """
    seen = set()
    files = []
    for root in src_roots:
        for cs in sorted(root.rglob("*.cs")):
            resolved = cs.resolve()
            if resolved in seen:
                continue
            seen.add(resolved)
            files.append(cs)
    return files


def collect_constants(files: list) -> dict:
    consts = {}
    for cs in files:
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
    ap.add_argument("--catalog", action="append", required=True, type=Path,
                    help="defaultLocale のテーブル（複数可。複数 scope をまとめて検査できる）")
    ap.add_argument("--src", action="append", required=True, type=Path,
                    help="走査するルート（複数可。スイート構成では全パッケージを渡すこと）")
    ap.add_argument("--method", action="append", default=[],
                    help='key 先頭型のファサード。例: UmpdL10n.Tr（複数指定可）')
    ap.add_argument("--method-scope-first", action="append", default=[],
                    help="scope 先頭型（`Tr(scope, key, args)`）のファサード。例: EditorL10n.Tr")
    args = ap.parse_args()

    if not args.method and not args.method_scope_first:
        ap.error("--method か --method-scope-first を 1 つ以上指定してください")

    catalog = load_catalogs(args.catalog)
    sources = iter_source_files(args.src)
    consts = collect_constants(sources)
    errors, warnings, checked, calls = [], [], 0, 0
    # (メソッド名, key が何番目の引数か)。scope 先頭型は第 1 引数が scope なので key は 2 番目。
    methods = [(name, 0) for name in args.method] + [(name, 1) for name in args.method_scope_first]

    for cs in sources:
        try:
            content = cs.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            continue
        for method, key_index in methods:
            for line, argtext in find_calls(content, method):
                parts = split_top_level_args(argtext)
                if len(parts) <= key_index:
                    continue
                calls += 1
                first = parts[key_index]
                if first.startswith('"') and first.endswith('"'):
                    key = first[1:-1]
                else:
                    ident = first.split(".")[-1].strip()
                    key = consts.get(ident)
                    if key is None:
                        # 定数参照の形をしているのに定数表に無いのは、走査範囲の外に宣言がある
                        # （--src の渡し漏れ）か、定数が消えたかのどちらか。どちらも「検査した
                        # つもりで素通り」を生むので警告ではなくエラーにする。
                        if LOOKS_LIKE_KEY_CONST_RE.match(first.strip()):
                            errors.append(
                                f"{cs}:{line}: 翻訳キー定数 {first} を解決できません。"
                                f"宣言のあるパッケージを --src に追加してください"
                                f"（このままではこの呼び出しは検査されません）")
                        else:
                            warnings.append(f"{cs}:{line}: key を静的解決できません: {first}")
                        continue
                text = catalog.get(key)
                if text is None:
                    errors.append(f"{cs}:{line}: カタログに key がありません: {key}")
                    continue
                need = required_arg_count(text)
                got = len(parts) - 1 - key_index  # key より後ろだけが書式引数
                checked += 1
                if got < need:
                    errors.append(
                        f"{cs}:{line}: 引数不足: key '{key}' は {need} 個必要だが {got} 個"
                        f"（実行時 FormatException / {{n}} 露出の原因）")
                elif got > need:
                    warnings.append(
                        f"{cs}:{line}: 引数過剰: key '{key}' は {need} 個だが {got} 個（無害だが不整合）")

    # 「呼び出しは見つかったが 1 件も検査できなかった」を成功として返さない。
    # 検査 0 件の緑と、検査して問題が無かった緑は、見分けが付かないと意味が無い。
    if calls > 0 and checked == 0:
        errors.append(
            f"{calls} 件の呼び出しを検出しましたが、key を 1 つも解決できず 1 件も検査できて"
            f"いません（定数表 {len(consts)} 件）。--src と --method の指定を見直してください")
    if not sources:
        errors.append("--src 配下に *.cs が 1 つもありません（パス指定の誤りです）")

    for w in warnings:
        print("WARN:", w)
    for e in errors:
        print("ERROR:", e, file=sys.stderr)
    print(f"files={len(sources)} calls={calls} checked={checked} "
          f"errors={len(errors)} warnings={len(warnings)}")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
