#!/usr/bin/env python3
"""翻訳カタログ（ロケール別 JSON）へ新規キーを全ロケール一括で挿入する。

リポジトリごとにカタログの JSON 書式が異なる（例: indent=2 の複数行エントリ /
4スペース+1行エントリ）ため、使い捨ての挿入スクリプトを都度書くと書式差で
事故が起きる。本スクリプトは書式を自動判定し、判定できない書式では黙って
書き換えずに中断する（fail-closed）。

使い方:
  python3 insert_catalog_keys.py \
    --locales-dir path/to/Locales \
    --anchor common.section.settings \
    --data new-keys.json \
    [--dry-run]

仕様:
- --data: {"キー": {"ロケール": "値", ...}, ...} 形式の対訳 JSON ファイル。
  キーの記載順を挿入順とし、--anchor キーの直後へ順に挿入する。
- 対象カタログ形式: {"locale": tag, "entries": [{"key","value"},...]}
  （check_tr_placeholder_parity.py が受ける形式と同系）。
- 書式自動判定（fail-closed）:
  (a) ファイル全体が json.dumps(obj, indent=2, ensure_ascii=False)+"\n" と一致
      → 構造編集モード（オブジェクトへ挿入して同書式で再出力）
  (b) 各エントリが `{ "key": ..., "value": ... },` の 1 行形式
      → 行ベース編集モード（インデント・空白はアンカー行から採取して複製）
  (c) どちらでもない → そのファイルの書式例を示して中断（黙って書式を壊さない）
- 検証:
  挿入前: 全ロケールで anchor が存在し、挿入キーが未存在であること。
          --data が対象ディレクトリの全ロケールを過不足なく網羅していること。
  挿入後: JSON 妥当性・キー重複ゼロ・全ロケールでキー集合が一致・
          placeholder（{0}{1}…）の番号集合が全ロケールで一致、を機械確認して表示。
- 終了コード: すべて成功で 0。検証失敗・書式非対応が 1 件でもあれば 1 とし、
  その場合はどのファイルも書き換えない（全ファイル検証後に一括書き込み）。
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

# placeholder 抽出は validate_locale_quality.py（= C# EditorL10nValidator）と同一規則。
# エスケープされた {{0}} は除外し、{0:N2} のような書式指定子付きでも番号だけを取り出す。
PLACEHOLDER_RE = re.compile(r"(?<!\{)\{(\d+)[^}]*\}(?!\})")

# 1 行エントリ形式（例: `        { "key": "a.b", "value": "text" },`）。
# 空白の入り方をグループで捕捉し、新規行を生成するときにアンカー行の書式を複製する。
JSON_STRING = r'"(?:[^"\\]|\\.)*"'
ENTRY_LINE_RE = re.compile(
    r'^(?P<indent>\s*)\{(?P<sp1>\s*)"key"(?P<c1>\s*):(?P<c2>\s*)'
    r"(?P<key>" + JSON_STRING + r')(?P<sep>\s*,\s*)"value"(?P<c3>\s*):(?P<c4>\s*)'
    r"(?P<value>" + JSON_STRING + r")(?P<sp2>\s*)\}(?P<tail>\s*,?\s*)$"
)


class FormatError(Exception):
    """対応外の書式を検出したときに、対象ファイルの書式例を添えて送出する。"""


def load_data(path: Path) -> dict[str, dict[str, str]]:
    """対訳 JSON（{"キー": {"ロケール": "値"}}）を読み、構造を検証する。キー順は記載順を保持する。"""
    try:
        text = path.read_text(encoding="utf-8")
    except OSError as error:
        raise SystemExit(f"NG: --data を読み取れません: {path}: {error}")
    try:
        document = json.loads(text)
    except ValueError as error:
        raise SystemExit(f"NG: --data の JSON を解釈できません: {path}: {error}")
    if not isinstance(document, dict) or not document:
        raise SystemExit(f"NG: --data は空でない JSON オブジェクトが必要です: {path}")
    for key, locales in document.items():
        if not isinstance(locales, dict) or not locales:
            raise SystemExit(f"NG: --data のキー '{key}' の値は {{ロケール: 値}} のオブジェクトが必要です")
        for tag, value in locales.items():
            if not isinstance(value, str):
                raise SystemExit(f"NG: --data のキー '{key}' ロケール '{tag}' の値は文字列が必要です")
    return document


def table_keys(document: dict) -> list[str]:
    """カタログ JSON の entries からキー一覧を（重複込みで）返す。"""
    entries = document.get("entries")
    if not isinstance(entries, list):
        raise FormatError('カタログに "entries" 配列がありません')
    keys = []
    for entry in entries:
        if isinstance(entry, dict) and isinstance(entry.get("key"), str):
            keys.append(entry["key"])
    return keys


def format_example(content: str, limit: int = 6) -> str:
    """中断メッセージに添える書式例（entries 付近の数行）を返す。"""
    lines = content.splitlines()
    start = 0
    for index, line in enumerate(lines):
        if '"entries"' in line:
            start = index
            break
    sample = lines[start:start + limit]
    return "\n".join("    | " + line for line in sample)


def detect_and_insert(path: Path, content: str, document: dict,
                      anchor: str, new_entries: list[tuple[str, str]]) -> tuple[str, str]:
    """書式を自動判定し、(モード名, 挿入後の新コンテンツ) を返す。判定不能なら FormatError。"""
    # (a) 構造編集モード: ファイル全体が json.dumps(indent=2, ensure_ascii=False)+"\n" と一致
    canonical = json.dumps(document, indent=2, ensure_ascii=False) + "\n"
    if content == canonical:
        entries = document["entries"]
        anchor_index = next(
            index for index, entry in enumerate(entries)
            if isinstance(entry, dict) and entry.get("key") == anchor
        )
        inserted = [{"key": key, "value": value} for key, value in new_entries]
        entries[anchor_index + 1:anchor_index + 1] = inserted
        return "structured(indent2)", json.dumps(document, indent=2, ensure_ascii=False) + "\n"

    # (b) 行ベース編集モード: 全エントリが 1 行形式で表現されている
    lines = content.split("\n")
    entry_line_indices = [index for index, line in enumerate(lines) if ENTRY_LINE_RE.match(line)]
    entry_count = len(table_keys(document))
    if entry_count > 0 and len(entry_line_indices) == entry_count:
        anchor_line_index = None
        anchor_match = None
        for index in entry_line_indices:
            match = ENTRY_LINE_RE.match(lines[index])
            if json.loads(match.group("key")) == anchor:
                anchor_line_index = index
                anchor_match = match
                break
        if anchor_line_index is None:
            raise FormatError(f"anchor '{anchor}' の行を特定できません")

        def build_line(key: str, value: str, with_comma: bool) -> str:
            # アンカー行の空白の入り方をそのまま複製して 1 行エントリを生成する
            m = anchor_match
            line = (m.group("indent") + "{" + m.group("sp1")
                    + '"key"' + m.group("c1") + ":" + m.group("c2")
                    + json.dumps(key, ensure_ascii=False)
                    + m.group("sep")
                    + '"value"' + m.group("c3") + ":" + m.group("c4")
                    + json.dumps(value, ensure_ascii=False)
                    + m.group("sp2") + "}")
            return line + ("," if with_comma else "")

        anchor_had_comma = "," in anchor_match.group("tail")
        if anchor_had_comma:
            # アンカーが末尾エントリではない → 新規行はすべてカンマ付き
            new_lines = [build_line(key, value, True) for key, value in new_entries]
        else:
            # アンカーが末尾エントリ → アンカーにカンマを付け、新規の最終行のみカンマなし
            lines[anchor_line_index] = build_line(
                json.loads(anchor_match.group("key")),
                json.loads(anchor_match.group("value")), True)
            new_lines = [
                build_line(key, value, index < len(new_entries) - 1)
                for index, (key, value) in enumerate(new_entries)
            ]
        lines[anchor_line_index + 1:anchor_line_index + 1] = new_lines
        return "line-based", "\n".join(lines)

    # (c) どちらでもない → 書式例を示して中断
    raise FormatError(
        "対応書式に一致しません（(a) json.dumps(indent=2, ensure_ascii=False)+改行 / "
        "(b) 1 行エントリ形式）。このファイルの書式例:\n" + format_example(content))


def main() -> int:
    parser = argparse.ArgumentParser(
        description="翻訳カタログへ新規キーを全ロケール一括で挿入する（書式自動判定・fail-closed）。")
    parser.add_argument("--locales-dir", required=True, type=Path,
                        help="{locale}.json を含むディレクトリ")
    parser.add_argument("--anchor", required=True,
                        help="このキーの直後へ挿入する（全ロケールに存在する既存キー）")
    parser.add_argument("--data", required=True, type=Path,
                        help='対訳 JSON ファイル（{"キー": {"ロケール": "値", ...}, ...}。記載順に挿入）')
    parser.add_argument("--dry-run", action="store_true",
                        help="書き込みを行わず、判定・挿入結果・検証のみ表示する")
    args = parser.parse_args()

    data = load_data(args.data)
    new_keys = list(data.keys())

    files = sorted(args.locales_dir.glob("*.json"))
    if not files:
        print(f"NG: ロケール JSON が見つかりません: {args.locales_dir}", file=sys.stderr)
        return 1

    # ---- 読み込みとロケールタグの対応付け ----
    errors: list[str] = []
    catalogs: dict[str, tuple[Path, str, dict]] = {}  # tag -> (path, content, document)
    for path in files:
        try:
            content = path.read_text(encoding="utf-8")
        except OSError as error:
            errors.append(f"{path.name}: ファイルを読み取れません: {error}")
            continue
        try:
            document = json.loads(content)
        except ValueError as error:
            errors.append(f"{path.name}: JSON を解釈できません: {error}")
            continue
        if not isinstance(document, dict):
            errors.append(f"{path.name}: JSON のルートがオブジェクトではありません")
            continue
        tag = document.get("locale") or path.stem
        if tag in catalogs:
            errors.append(f"{path.name}: ロケールタグ '{tag}' が重複しています")
            continue
        catalogs[tag] = (path, content, document)

    # ---- 挿入前検証: --data が全ロケールを過不足なく網羅しているか ----
    file_tags = set(catalogs)
    for key in new_keys:
        data_tags = set(data[key])
        missing = sorted(file_tags - data_tags)
        extra = sorted(data_tags - file_tags)
        if missing:
            errors.append(f"--data: キー '{key}' に不足ロケール: {', '.join(missing)}")
        if extra:
            errors.append(f"--data: キー '{key}' に対象外ロケール: {', '.join(extra)}")

    # ---- 挿入前検証 + 書式判定 + 新コンテンツ生成（この段階では書き込まない） ----
    results: dict[str, tuple[Path, str, str]] = {}  # tag -> (path, mode, new_content)
    for tag in sorted(catalogs):
        path, content, document = catalogs[tag]
        try:
            existing = table_keys(document)
        except FormatError as error:
            errors.append(f"{path.name}: {error}")
            continue
        if args.anchor not in existing:
            errors.append(f"{path.name}: anchor '{args.anchor}' が存在しません")
            continue
        already = [key for key in new_keys if key in existing]
        if already:
            errors.append(f"{path.name}: 挿入キーが既に存在します: {', '.join(already)}")
            continue
        new_entries = [(key, data[key][tag]) for key in new_keys]
        try:
            mode, new_content = detect_and_insert(path, content, document, args.anchor, new_entries)
        except FormatError as error:
            errors.append(f"{path.name}: 書式を判定できません。中断します（黙って書式を壊さない）。\n  {error}")
            continue
        results[tag] = (path, mode, new_content)

    if errors:
        for error in errors:
            print("NG:", error, file=sys.stderr)
        print(f"中断: どのファイルも書き換えていません（エラー {len(errors)} 件）", file=sys.stderr)
        return 1

    # ---- 挿入後検証（メモリ上の新コンテンツに対して実施） ----
    tables: dict[str, dict[str, str]] = {}
    for tag in sorted(results):
        path, mode, new_content = results[tag]
        try:
            document = json.loads(new_content)
        except ValueError as error:
            errors.append(f"{path.name}: 挿入後 JSON が不正です: {error}")
            continue
        keys = table_keys(document)
        duplicates = sorted({key for key in keys if keys.count(key) > 1})
        if duplicates:
            errors.append(f"{path.name}: 挿入後にキーが重複しています: {', '.join(duplicates)}")
        tables[tag] = {
            entry["key"]: entry.get("value") or ""
            for entry in document["entries"] if isinstance(entry, dict) and entry.get("key")
        }

    # 全ロケールでキー集合が一致するか
    if tables:
        reference_tag = sorted(tables)[0]
        reference_keys = set(tables[reference_tag])
        for tag in sorted(tables):
            missing = sorted(reference_keys - set(tables[tag]))
            extra = sorted(set(tables[tag]) - reference_keys)
            if missing or extra:
                errors.append(
                    f"{tag}: キー集合が {reference_tag} と不一致"
                    f"（missing: {', '.join(missing) or 'なし'} / extra: {', '.join(extra) or 'なし'}）")

    # placeholder（{0}{1}…）の番号集合が全ロケールで一致するか
    placeholder_mismatch = 0
    if tables and not errors:
        for key in sorted(reference_keys):
            number_sets = {
                tag: frozenset(int(m.group(1)) for m in PLACEHOLDER_RE.finditer(tables[tag][key]))
                for tag in tables
            }
            if len(set(number_sets.values())) > 1:
                placeholder_mismatch += 1
                detail = "; ".join(
                    f"{tag}={sorted(numbers)}" for tag, numbers in sorted(number_sets.items()))
                errors.append(f"placeholder 番号集合が不一致: key '{key}' → {detail}")

    # ---- 結果表示 ----
    for tag in sorted(results):
        path, mode, _ = results[tag]
        print(f"{tag}: mode={mode} insert={len(new_keys)} after='{args.anchor}'"
              + (" [dry-run]" if args.dry_run else ""))
    print(f"挿入キー: {', '.join(new_keys)}")

    if errors:
        for error in errors:
            print("NG:", error, file=sys.stderr)
        print(f"中断: どのファイルも書き換えていません（挿入後検証エラー {len(errors)} 件）", file=sys.stderr)
        return 1

    print(f"検証OK: locales={len(tables)} keys={len(reference_keys)} "
          f"キー重複=0 キー集合一致=OK placeholder番号集合一致=OK")

    if args.dry_run:
        print("[dry-run] 書き込みは行いませんでした")
        return 0

    for tag in sorted(results):
        path, _, new_content = results[tag]
        path.write_text(new_content, encoding="utf-8")
    print(f"書き込み完了: {len(results)} ファイル")
    return 0


if __name__ == "__main__":
    sys.exit(main())
