"""
Shared utilities for UiPath activity/API extraction scripts.

Provides common file I/O helpers and CLI boilerplate used by both
extract-activity-metadata.py and extract-coded-api.py.
"""

import argparse
import json
import os
import sys
from pathlib import Path


# ---------------------------------------------------------------------------
# File I/O helpers
# ---------------------------------------------------------------------------

_EXCLUDED_DIRS = {"bin", "obj", "test", "tests", "node_modules", ".git"}


def find_files(root: str, pattern: str) -> list[str]:
    """Recursively find files matching a glob pattern.

    Excludes common build-output and test directories (bin/, obj/, test/,
    tests/, node_modules/, .git/) so that compiled copies and test fakes
    are not picked up.  Comparison is case-insensitive (Windows paths may
    use mixed casing like 'Tests/' or 'Bin/').
    """
    results: list[str] = []
    for p in Path(root).rglob(pattern):
        parts_lower = {part.lower() for part in p.relative_to(root).parts}
        if _EXCLUDED_DIRS.isdisjoint(parts_lower):
            results.append(str(p))
    return sorted(results)


def read_file(path: str) -> str | None:
    """Read a text file, tolerating non-UTF-8 byte sequences.

    Uses 'replace' error handling so that stray Windows-1252 characters
    (e.g. en-dash 0x96, em-dash 0x97) become U+FFFD instead of raising
    UnicodeDecodeError. The ASCII-only patterns we regex-match are unaffected.
    """
    try:
        with open(path, "r", encoding="utf-8-sig", errors="replace") as f:
            return f.read()
    except OSError as e:
        print(f"Warning: Could not read {path}: {e}", file=sys.stderr)
        return None


# ---------------------------------------------------------------------------
# XML parsing (safe)
# ---------------------------------------------------------------------------

def parse_xml(path: str):
    """Parse an XML file safely, using defusedxml if available.

    Returns an ElementTree object, or raises on parse errors.
    defusedxml protects against XML External Entity (XXE) attacks;
    falls back to stdlib xml.etree.ElementTree when unavailable
    (scripts only process trusted local .resx files from this repository).
    """
    try:
        import defusedxml.ElementTree as SafeET
        return SafeET.parse(path)
    except ImportError:
        import xml.etree.ElementTree as ET  # noqa: N812 — stdlib fallback for local-only .resx files
        return ET.parse(path)


# ---------------------------------------------------------------------------
# C# parsing helpers
# ---------------------------------------------------------------------------

def find_brace_block(content: str, open_brace_pos: int) -> str:
    """Return the content between a '{' at open_brace_pos and its matching '}'.

    The returned string excludes the outer braces themselves.
    Returns an empty string if braces are unbalanced.
    """
    depth = 1
    i = open_brace_pos + 1
    while i < len(content) and depth > 0:
        if content[i] == "{":
            depth += 1
        elif content[i] == "}":
            depth -= 1
        i += 1
    if depth != 0:
        return ""
    return content[open_brace_pos + 1 : i - 1]


# ---------------------------------------------------------------------------
# CLI entry point helper
# ---------------------------------------------------------------------------

def run_cli(description: str, scan_fn):
    """Common CLI entry point for extraction scripts.

    Args:
        description: argparse description string.
        scan_fn: callable(root: str) -> dict that performs the domain scan.
    """
    parser = argparse.ArgumentParser(description=description)
    parser.add_argument("domain_root", help="Path to the domain root directory")
    parser.add_argument("--output", "-o", help="Output JSON file path (default: stdout)")
    parser.add_argument("--pretty", action="store_true", help="Pretty-print JSON output")
    args = parser.parse_args()

    root = os.path.abspath(args.domain_root)
    if not os.path.isdir(root):
        print(f"Error: {root} is not a directory", file=sys.stderr)
        sys.exit(1)

    output = scan_fn(root)
    json_str = json.dumps(output, indent=2 if args.pretty else None, ensure_ascii=False)

    if args.output:
        with open(args.output, "w", encoding="utf-8") as f:
            f.write(json_str)
        print(f"\nOutput written to {args.output}", file=sys.stderr)
    else:
        print(json_str)
