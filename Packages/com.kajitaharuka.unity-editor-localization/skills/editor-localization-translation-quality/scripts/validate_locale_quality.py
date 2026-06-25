#!/usr/bin/env python3
"""Validate EditorLocalization locale JSON tables.

Checks key parity, placeholder parity, and unexpected exact duplicates of the
default locale text. Exact duplicates are allowed for fixed terms by key.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path


# C# 側の EditorL10nValidator と同じ抽出規則に揃える。
# エスケープされた {{0}} は除外し、{0:N2} のような書式指定子付きでも番号だけを取り出す。
PLACEHOLDER_RE = re.compile(r"(?<!\{)\{(\d+)[^}]*\}(?!\})")


def load_table(path: Path) -> tuple[str, dict[str, str]]:
    with path.open(encoding="utf-8") as file:
        document = json.load(file)

    locale = document.get("locale") or path.stem
    entries = document.get("entries") or []
    table: dict[str, str] = {}
    duplicate_keys: list[str] = []
    for entry in entries:
        key = entry.get("key")
        if not key:
            continue
        if key in table:
            duplicate_keys.append(key)
        table[key] = entry.get("value") or ""

    if duplicate_keys:
        raise ValueError(f"{path}: duplicate keys: {', '.join(sorted(set(duplicate_keys)))}")

    return locale, table


def placeholders(value: str) -> tuple[str, ...]:
    return tuple(sorted(PLACEHOLDER_RE.findall(value)))


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate EditorLocalization locale JSON quality.")
    parser.add_argument("locales_dir", type=Path, help="Directory containing {locale}.json files.")
    parser.add_argument("--default-locale", default="en", help="Default locale used as the key and placeholder baseline.")
    parser.add_argument(
        "--allow-same-key",
        action="append",
        default=[],
        help="Key that may intentionally have the same value as the default locale. Repeat as needed.",
    )
    args = parser.parse_args()

    files = sorted(args.locales_dir.glob("*.json"))
    if not files:
        print(f"No locale JSON files found: {args.locales_dir}", file=sys.stderr)
        return 2

    tables: dict[str, dict[str, str]] = {}
    for file in files:
        locale, table = load_table(file)
        tables[locale] = table

    if args.default_locale not in tables:
        print(f"Default locale not found: {args.default_locale}", file=sys.stderr)
        return 2

    default_table = tables[args.default_locale]
    default_keys = set(default_table)
    allowed_same = set(args.allow_same_key)
    failed = False

    for locale in sorted(tables):
        table = tables[locale]
        keys = set(table)
        missing = sorted(default_keys - keys)
        extra = sorted(keys - default_keys)
        placeholder_mismatch = [
            key
            for key in sorted(default_keys & keys)
            if placeholders(default_table[key]) != placeholders(table[key])
        ]
        same_unexpected = []
        if locale != args.default_locale:
            same_unexpected = [
                key
                for key in sorted(default_keys & keys)
                if table[key] == default_table[key] and key not in allowed_same
            ]

        print(
            f"{locale}: keys={len(keys)}, missing={len(missing)}, extra={len(extra)}, "
            f"placeholder={len(placeholder_mismatch)}, sameUnexpected={len(same_unexpected)}"
        )

        if missing or extra or placeholder_mismatch or same_unexpected:
            failed = True
            if missing:
                print("  missing: " + ", ".join(missing))
            if extra:
                print("  extra: " + ", ".join(extra))
            if placeholder_mismatch:
                details = [
                    f"{key} default={placeholders(default_table[key])} locale={placeholders(table[key])}"
                    for key in placeholder_mismatch
                ]
                print("  placeholder: " + "; ".join(details))
            if same_unexpected:
                print("  sameUnexpected: " + ", ".join(same_unexpected))

    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
