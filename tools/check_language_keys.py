#!/usr/bin/env python3
"""Find stale entries in TazUO's language.ini.

Parses the language file, scans the source tree for quoted key-shaped tokens, and
reports keys that are never referenced so they can be removed without leaving
dead localization behind.

Keys reachable only through dynamically built names (interpolated or concatenated
arguments to ``TazLang.Get``, string constants that act as key prefixes, or
prefixes passed via ``--dynamic-prefix``) are reported separately as "dynamic" -
they need a human eye before removal.

Usage:
    python tools/check_language_keys.py
    python tools/check_language_keys.py --show-used
    python tools/check_language_keys.py --json
    python tools/check_language_keys.py --fail
    python tools/check_language_keys.py --prune
    python tools/check_language_keys.py --dynamic-prefix prefix_one --dynamic-prefix prefix_two
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from dataclasses import dataclass
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
DEFAULT_INI = "src/ClassicUO.Client/Configuration/language.ini"
DEFAULT_SCAN_DIR = "src"
DEFAULT_EXTENSIONS = ["cs"]
DEFAULT_EXCLUDED_DIRS = {"bin", "obj", ".git", ".vs", ".idea", "node_modules"}

# Keys that are reserved by the loader and must never be reported.
RESERVED_KEYS = {"_version"}

# Language keys are all [a-z0-9_], so we only need to find that shape between a
# matching pair of quotes. This deliberately ignores full string-literal parsing:
# keys nested inside interpolated strings ($"...{Get("key")}...") would otherwise
# be swallowed by greedy quote pairing.
_TOKEN_RE = re.compile(r'(["\'])([a-z0-9_]+)\1')

# TazLang.Get($"literal-prefix{...}") / GetEx variant.
_INTERPOLATED_CALL_RE = re.compile(r'TazLang\.(?:Get|GetEx)\s*\(\s*\$"((?:[^"\\]|\\.)*)"')
# TazLang.Get("literal-prefix" + ...) / GetEx variant.
_CONCAT_CALL_RE = re.compile(r'TazLang\.(?:Get|GetEx)\s*\(\s*"((?:[^"\\]|\\.)*)"\s*\+')

# const string Name = "value"; - used to resolve prefixes referenced by name.
_CONST_STRING_RE = re.compile(r'\b(?:const|static\s+readonly)\s+string\s+(\w+)\s*=\s*"((?:[^"\\]|\\.)*)"')

# Identifier inside an interpolation hole, e.g. the "prefix" in $"{prefix}{suffix}".
_INTERP_HOLE_RE = re.compile(r"\{([A-Za-z_]\w*)")


@dataclass
class LangKey:
    key: str
    line: int


@dataclass
class Result:
    key: LangKey
    status: str  # "used", "dynamic", "stale"
    reason: str = ""


def parse_language_ini(path: Path) -> list[LangKey]:
    """Extract key/line pairs from the language file, preserving order."""
    keys: list[LangKey] = []
    for lineno, raw in enumerate(path.read_text(encoding="utf-8-sig").splitlines(), start=1):
        line = raw.strip()
        if not line or line.startswith(";"):
            continue
        eq = raw.find("=")
        if eq < 1:
            continue
        key = raw[:eq].strip()
        if key:
            keys.append(LangKey(key, lineno))
    return keys


def iter_source_files(root: Path, scan_dirs: list[Path], extensions: set[str], excluded: set[str]):
    """Yield source files under each scan dir, skipping build/vendor directories."""
    for scan_dir in scan_dirs:
        if not scan_dir.exists():
            continue
        for path in scan_dir.rglob("*"):
            if not path.is_file():
                continue
            if path.suffix.lstrip(".").lower() not in extensions:
                continue
            if excluded.intersection(path.parts):
                continue
            yield path


def scan_sources(files) -> tuple[set[str], set[str]]:
    """Collect quoted key-shaped tokens and dynamic localization prefixes.

    Returns (tokens, dynamic_prefixes).
    """
    tokens: set[str] = set()
    dynamic_prefixes: set[str] = set()
    const_strings: dict[str, str] = {}

    for path in files:
        try:
            text = path.read_text(encoding="utf-8-sig")
        except (OSError, UnicodeDecodeError):
            continue

        for match in _TOKEN_RE.finditer(text):
            tokens.add(match.group(2))

        for match in _CONST_STRING_RE.finditer(text):
            const_strings[match.group(1)] = match.group(2)

        for match in _INTERPOLATED_CALL_RE.finditer(text):
            captured = match.group(1)
            prefix = captured.split("{", 1)[0]
            if prefix:
                dynamic_prefixes.add(prefix)
            for hole in _INTERP_HOLE_RE.findall(captured):
                if hole in const_strings:
                    dynamic_prefixes.add(const_strings[hole])

        for match in _CONCAT_CALL_RE.finditer(text):
            prefix = match.group(1)
            if prefix:
                dynamic_prefixes.add(prefix)

    return tokens, dynamic_prefixes


def prefix_literals(tokens: set[str], keys: set[str]) -> set[str]:
    """Return quoted tokens that look like localization namespace prefixes.

    A token qualifies when it ends with ``_`` and is not itself a key - this
    catches prefixes stored in constants (e.g. ``KEY_PREFIX``) or inline
    ``StartsWith`` style checks that concatenate a key at runtime.
    """
    return {tok for tok in tokens if len(tok) >= 3 and tok.endswith("_") and tok not in keys}


def classify(keys: list[LangKey], tokens: set[str], dynamic_prefixes: set[str]) -> list[Result]:
    results: list[Result] = []
    for key in keys:
        if key.key in RESERVED_KEYS:
            results.append(Result(key, "used", "reserved"))
        elif key.key in tokens:
            results.append(Result(key, "used"))
        else:
            prefix = next((p for p in dynamic_prefixes if key.key.startswith(p)), None)
            if prefix:
                results.append(Result(key, "dynamic", f"prefix '{prefix}'"))
            else:
                results.append(Result(key, "stale"))
    return results


def prune_language_ini(path: Path, stale: set[str]) -> int:
    """Remove every line whose key is in ``stale``, preserving comments, blank
    lines and the original line endings. Returns the number of entries removed."""
    lines = path.read_text(encoding="utf-8-sig").splitlines(keepends=True)
    kept: list[str] = []
    removed = 0
    for line in lines:
        stripped = line.strip()
        if stripped and not stripped.startswith(";"):
            eq = line.find("=")
            if eq >= 1 and line[:eq].strip() in stale:
                removed += 1
                continue
        kept.append(line)
    path.write_text("".join(kept), encoding="utf-8")
    return removed


def format_text(results: list[Result], show_used: bool) -> str:
    stale = [r for r in results if r.status == "stale"]
    dynamic = [r for r in results if r.status == "dynamic"]
    used = [r for r in results if r.status == "used"]

    lines: list[str] = []
    if stale:
        lines.append(f"Stale keys ({len(stale)}) - not referenced anywhere:")
        lines.extend(f"  language.ini:{r.key.line}  {r.key.key}" for r in stale)
    else:
        lines.append("No stale keys found.")

    if dynamic:
        lines.append("")
        lines.append(f"Dynamic keys ({len(dynamic)}) - built at runtime, verify before removal:")
        lines.extend(f"  language.ini:{r.key.line}  {r.key.key}  ({r.reason})" for r in dynamic)

    if show_used:
        lines.append("")
        lines.append(f"Used keys ({len(used)}):")
        lines.extend(f"  language.ini:{r.key.line}  {r.key.key}" for r in used)

    lines.append("")
    lines.append(f"Summary: {len(results)} keys - {len(used)} used, {len(dynamic)} dynamic, {len(stale)} stale.")
    return "\n".join(lines)


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--root", type=Path, default=REPO_ROOT, help="repository root (default: auto-detected)")
    parser.add_argument("--ini", type=Path, default=None, help=f"language file (default: {DEFAULT_INI})")
    parser.add_argument(
        "--scan",
        type=Path,
        action="append",
        default=None,
        help=f"directory to scan, repeatable (default: {DEFAULT_SCAN_DIR})",
    )
    parser.add_argument(
        "--extensions",
        default=",".join(DEFAULT_EXTENSIONS),
        help="comma-separated file extensions to scan (default: cs)",
    )
    parser.add_argument(
        "--exclude-dir",
        action="append",
        default=None,
        help="directory name to skip, repeatable (default: bin,obj,.git,.vs,.idea,node_modules)",
    )
    parser.add_argument(
        "--dynamic-prefix",
        action="append",
        default=[],
        help="extra key prefix built at runtime, repeatable (e.g. overlaytrigger_playerattr_field_)",
    )
    parser.add_argument(
        "--no-prefix-heuristic",
        action="store_true",
        help="do not treat string literals ending in '_' as runtime key prefixes",
    )
    parser.add_argument("--show-used", action="store_true", help="also list keys that are referenced")
    parser.add_argument("--json", action="store_true", help="emit a JSON report")
    parser.add_argument("--fail", action="store_true", help="exit non-zero when stale keys are found")
    parser.add_argument("--prune", action="store_true", help="remove stale entries from the language file in place")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    root: Path = args.root.resolve()
    ini_path = args.ini or (root / DEFAULT_INI)
    if not ini_path.is_absolute():
        ini_path = root / ini_path

    if not ini_path.exists():
        print(f"error: language file not found: {ini_path}", file=sys.stderr)
        return 2

    scan_dirs = args.scan or [Path(DEFAULT_SCAN_DIR)]
    scan_dirs = [d if d.is_absolute() else root / d for d in scan_dirs]
    extensions = {e.strip().lstrip(".").lower() for e in args.extensions.split(",") if e.strip()}
    excluded = set(args.exclude_dir) if args.exclude_dir else set(DEFAULT_EXCLUDED_DIRS)

    keys = parse_language_ini(ini_path)
    files = list(iter_source_files(root, scan_dirs, extensions, excluded))
    tokens, dynamic_prefixes = scan_sources(files)
    dynamic_prefixes.update(p for p in args.dynamic_prefix if p)
    if not args.no_prefix_heuristic:
        dynamic_prefixes.update(prefix_literals(tokens, {k.key for k in keys}))

    results = classify(keys, tokens, dynamic_prefixes)
    stale_count = sum(1 for r in results if r.status == "stale")

    if args.prune:
        stale_keys = {r.key.key for r in results if r.status == "stale"}
        removed = prune_language_ini(ini_path, stale_keys)
        print(f"Removed {removed} stale entries from {ini_path}")
        return 0

    if args.json:
        payload = {
            "language_file": str(ini_path),
            "scanned_files": len(files),
            "summary": {
                "total": len(results),
                "used": sum(1 for r in results if r.status == "used"),
                "dynamic": sum(1 for r in results if r.status == "dynamic"),
                "stale": stale_count,
            },
            "stale": [{"key": r.key.key, "line": r.key.line} for r in results if r.status == "stale"],
            "dynamic": [
                {"key": r.key.key, "line": r.key.line, "reason": r.reason}
                for r in results
                if r.status == "dynamic"
            ],
        }
        print(json.dumps(payload, indent=2))
    else:
        print(format_text(results, args.show_used))

    return 1 if (args.fail and stale_count) else 0


if __name__ == "__main__":
    sys.exit(main())
