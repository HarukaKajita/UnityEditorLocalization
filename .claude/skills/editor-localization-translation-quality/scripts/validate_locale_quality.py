#!/usr/bin/env python3
"""Validate EditorLocalization locale JSON tables.

Checks key parity, placeholder set parity vs the default locale, placeholder
number gaps (numbers not consecutive from 0), and unexpected exact duplicates of
the default locale text. Exact duplicates are allowed for fixed terms by key.
Optionally reports values left identical across regional variants (opt-in).
These mirror the C# EditorL10nValidator so the two gates can cross-check.
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


def has_placeholder_gap(value: str) -> bool:
    """番号が 0 から連続していない（連番欠落）かを返す。C# 側 FindMissingPlaceholderNumbers と対応。"""
    numbers = sorted({int(n) for n in PLACEHOLDER_RE.findall(value)})
    if not numbers:
        return False
    return numbers != list(range(numbers[-1] + 1))


def load_manifest_fixed_terms(locales_dir: Path) -> set[str]:
    """locales_dir の親にある *.l10n-manifest.json の fixedTerms を読む。
    C# の EditorL10nValidator と同じ宣言を共有し、意図的な固定語を両方の検証で同様に除外する。"""
    terms: set[str] = set()
    for manifest in sorted(locales_dir.parent.glob("*.l10n-manifest.json")):
        try:
            document = json.loads(manifest.read_text(encoding="utf-8"))
        except (OSError, ValueError):
            continue
        value = document.get("fixedTerms")
        if isinstance(value, list):
            terms.update(str(item) for item in value)
    return terms


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
    parser.add_argument(
        "--report-variant-duplicates",
        action="store_true",
        help="Also print (non-failing) keys whose value is identical across the locales of a "
        "regional-variant group (grouped by primary subtag, e.g. es-ES/es-419), to review for copy-paste left-overs.",
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
    # manifest の fixedTerms（C# Validator と共有の宣言）と --allow-same-key を併用する。
    manifest_fixed = load_manifest_fixed_terms(args.locales_dir)
    allowed_same = set(args.allow_same_key) | manifest_fixed
    if manifest_fixed:
        print(f"(loaded {len(manifest_fixed)} fixedTerms from manifest)")
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
        # 連番欠落（{0} から連続しない）。default との不一致とは別軸なので独立して報告する。
        placeholder_gap = [key for key in sorted(keys) if has_placeholder_gap(table[key])]
        same_unexpected = []
        if locale != args.default_locale:
            same_unexpected = [
                key
                for key in sorted(default_keys & keys)
                if table[key] == default_table[key] and key not in allowed_same
            ]

        print(
            f"{locale}: keys={len(keys)}, missing={len(missing)}, extra={len(extra)}, "
            f"placeholder={len(placeholder_mismatch)}, gap={len(placeholder_gap)}, "
            f"sameUnexpected={len(same_unexpected)}"
        )

        if missing or extra or placeholder_mismatch or placeholder_gap or same_unexpected:
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
            if placeholder_gap:
                gap_details = [f"{key} {placeholders(table[key])}" for key in placeholder_gap]
                print("  gap: " + "; ".join(gap_details))
            if same_unexpected:
                print("  sameUnexpected: " + ", ".join(same_unexpected))

    if args.report_variant_duplicates:
        report_variant_duplicates(tables)

    return 1 if failed else 0


def report_variant_duplicates(tables: dict[str, dict[str, str]]) -> None:
    """地域バリアント（同一の primary subtag を持つ複数ロケール）間で値が同一なキーを情報表示する。
    技術文では一致が正当な場合も多いため、失敗にはせずレビュー用に列挙するだけ。"""
    groups: dict[str, list[str]] = {}
    for locale in tables:
        primary = locale.split("-")[0].lower()
        groups.setdefault(primary, []).append(locale)

    for primary, locales in sorted(groups.items()):
        if len(locales) < 2:
            continue
        locales = sorted(locales)
        shared_keys = set.intersection(*(set(tables[loc]) for loc in locales))
        identical = [
            key
            for key in sorted(shared_keys)
            if len({tables[loc][key] for loc in locales}) == 1
        ]
        print(f"[variant-duplicates] {primary} ({', '.join(locales)}): {len(identical)} identical across all")
        if identical:
            print("  " + ", ".join(identical))


if __name__ == "__main__":
    raise SystemExit(main())
