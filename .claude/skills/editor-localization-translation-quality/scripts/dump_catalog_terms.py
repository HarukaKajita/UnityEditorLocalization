#!/usr/bin/env python3
"""指定キーまたは値の部分一致に該当するカタログエントリを全ロケール分まとめて表示する。

用途: 用語整合の確認。新規キーを訳す前に関連既存キーの全ロケール訳を抽出して
確立訳語（訳語の選択・固有語のまま保持する語・記号の字種など）に揃えるための
サンプリングと、ドキュメント中の太字 UI 名とカタログ実ラベルとの逐語一致の
突き合わせに使う。

使い方:
  python3 dump_catalog_terms.py --locales-dir path/to/Locales --keys k1,k2,...
  python3 dump_catalog_terms.py --locales-dir path/to/Locales --grep "部分文字列" [--ignore-case]

仕様:
- --keys: カンマ区切りのキー一覧。指定順に出力する。
- --grep: 値に対する部分文字列検索。いずれかのロケールの値が一致したキーを出力する。
  --ignore-case で大文字小文字を無視する。
- 対象カタログ形式: {"locale": tag, "entries": [{"key","value"},...]}
  （check_tr_placeholder_parity.py が受ける形式と同系）。
- 出力: キーごとに「ロケール | 値」を列挙する。キー欠落ロケールは (missing) と表示する。
- 終了コード: 全キーが少なくとも 1 ロケールに存在すれば 0。
  見つからないキーがある / --grep が 1 件も一致しない場合は 1。
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def load_tables(locales_dir: Path) -> dict[str, dict[str, str]]:
    """ロケールタグ -> {キー: 値} のテーブル群を読み込む。"""
    tables: dict[str, dict[str, str]] = {}
    for path in sorted(locales_dir.glob("*.json")):
        try:
            document = json.loads(path.read_text(encoding="utf-8"))
        except ValueError as error:
            raise SystemExit(f"NG: JSON を解釈できません: {path}: {error}")
        entries = document.get("entries")
        if not isinstance(entries, list):
            raise SystemExit(f'NG: カタログに "entries" 配列がありません: {path}')
        tag = document.get("locale") or path.stem
        tables[tag] = {
            entry["key"]: entry.get("value") or ""
            for entry in entries if isinstance(entry, dict) and isinstance(entry.get("key"), str)
        }
    if not tables:
        raise SystemExit(f"NG: ロケール JSON が見つかりません: {locales_dir}")
    return tables


def print_key(key: str, tables: dict[str, dict[str, str]], width: int) -> bool:
    """1 キー分の対訳を「ロケール | 値」形式で表示する。どこにも存在しなければ False。"""
    found = any(key in table for table in tables.values())
    print(f"== {key} ==" + ("" if found else " (not found in any locale)"))
    if not found:
        return False
    for tag in sorted(tables):
        value = tables[tag].get(key)
        rendered = "(missing)" if value is None else value
        print(f"  {tag:<{width}} | {rendered}")
    return True


def main() -> int:
    parser = argparse.ArgumentParser(
        description="カタログエントリを全ロケール分まとめて表示する（用語整合の確認用）。")
    parser.add_argument("--locales-dir", required=True, type=Path,
                        help="{locale}.json を含むディレクトリ")
    selector = parser.add_mutually_exclusive_group(required=True)
    selector.add_argument("--keys", help="カンマ区切りのキー一覧（指定順に出力）")
    selector.add_argument("--grep", help="値に対する部分文字列検索")
    parser.add_argument("--ignore-case", action="store_true",
                        help="--grep で大文字小文字を無視する")
    args = parser.parse_args()

    tables = load_tables(args.locales_dir)
    width = max(len(tag) for tag in tables)

    if args.keys:
        keys = [key.strip() for key in args.keys.split(",") if key.strip()]
        if not keys:
            print("NG: --keys にキーが指定されていません", file=sys.stderr)
            return 1
        missing = [key for key in keys if not print_key(key, tables, width)]
        if missing:
            print(f"NG: 見つからないキー: {', '.join(missing)}", file=sys.stderr)
            return 1
        return 0

    # --grep: いずれかのロケールの値が部分一致したキーを出力する
    needle = args.grep.casefold() if args.ignore_case else args.grep
    matched: list[str] = []
    seen: set[str] = set()
    for tag in sorted(tables):
        for key, value in tables[tag].items():
            haystack = value.casefold() if args.ignore_case else value
            if needle in haystack and key not in seen:
                seen.add(key)
                matched.append(key)
    if not matched:
        print(f"NG: 値に '{args.grep}' を含むエントリはありません", file=sys.stderr)
        return 1
    print(f"-- {len(matched)} keys matched --")
    for key in sorted(matched):
        print_key(key, tables, width)
    return 0


if __name__ == "__main__":
    sys.exit(main())
