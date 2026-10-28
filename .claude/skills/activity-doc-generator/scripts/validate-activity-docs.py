#!/usr/bin/env python3
"""Validate generated activity markdown docs for common quality regressions.

Usage:
    python validate-activity-docs.py <docs_root> [--strict]
"""

from __future__ import annotations

import argparse
import re
import sys
from dataclasses import dataclass
from pathlib import Path


@dataclass
class Finding:
    path: Path
    message: str


INTERNAL_NAMES = {"Body", "DeprecatedWarning"}
ONE_OF_PAIRS = [
    ("Key", "KeySecureString"),
    ("ConnectionString", "ConnectionSecureString"),
    ("Password", "SecurePassword"),
    ("ProxyPassword", "ProxySecurePassword"),
    ("ClientCertificatePassword", "ClientCertificateSecurePassword"),
    ("Code", "ScriptFile"),
    ("TargetObject", "TargetType"),
]


def _extract_section(text: str, heading: str) -> str | None:
    pattern = re.compile(
        rf"^###\s+{re.escape(heading)}\s*$\n(?P<body>.*?)(?=^###\s+|^##\s+|\Z)",
        re.MULTILINE | re.DOTALL,
    )
    match = pattern.search(text)
    return match.group("body") if match else None


def _parse_markdown_table(section: str) -> list[dict[str, str]]:
    rows: list[dict[str, str]] = []
    table_lines = [line for line in section.splitlines() if line.strip().startswith("|")]
    if len(table_lines) < 3:
        return rows

    headers = [h.strip() for h in table_lines[0].strip("|").split("|")]
    for line in table_lines[2:]:
        parts = [part.strip() for part in line.strip("|").split("|")]
        if len(parts) != len(headers):
            continue
        row = {headers[i]: parts[i] for i in range(len(headers))}
        rows.append(row)
    return rows


def _unquote_cell(value: str) -> str:
    return value.strip().strip("`")


def _check_file(path: Path) -> list[Finding]:
    findings: list[Finding] = []
    text = path.read_text(encoding="utf-8")

    if not text.endswith("\n"):
        findings.append(Finding(path, "Missing trailing newline at end of file"))

    if re.search(r"(?<!`)`xml", text):
        findings.append(Finding(path, "Found invalid single-backtick XML code fence (`xml)"))

    if text.count("```") % 2 != 0:
        findings.append(Finding(path, "Unbalanced markdown fenced code blocks"))

    if "| `-` | - | - | `-` | - |" in text or "| `-` | - | - | - |" in text:
        findings.append(Finding(path, "Found placeholder empty table row"))

    if "## XAML Example" in text:
        xaml_match = re.search(r"##\s+XAML Example.*?```xml\s*(.*?)\s*```", text, re.DOTALL)
        if not xaml_match:
            findings.append(Finding(path, "XAML Example section exists but lacks a valid ```xml fenced block"))
            xaml_content = ""
        else:
            xaml_content = xaml_match.group(1)
    else:
        xaml_content = ""

    input_section = _extract_section(text, "Input")
    output_section = _extract_section(text, "Output")

    required_inputs: list[str] = []
    input_rows: list[dict[str, str]] = []
    if input_section:
        input_rows = _parse_markdown_table(input_section)
        for row in input_rows:
            name = _unquote_cell(row.get("Name", ""))
            kind = _unquote_cell(row.get("Kind", ""))
            required = _unquote_cell(row.get("Required", "")).lower()
            if name in INTERNAL_NAMES or name.endswith("InputModeSwitch"):
                findings.append(Finding(path, f"Internal property leaked in Input section: {name}"))
            if required == "yes":
                required_inputs.append(name)
            if kind == "Property" and name.endswith("Path") and required == "yes":
                # Soft heuristic for likely argument misclassification.
                findings.append(Finding(path, f"Likely argument misclassified as Property: {name}"))

    if output_section:
        output_rows = _parse_markdown_table(output_section)
        for row in output_rows:
            name = _unquote_cell(row.get("Name", ""))
            kind = _unquote_cell(row.get("Kind", ""))
            if name in INTERNAL_NAMES or name.endswith("InputModeSwitch"):
                findings.append(Finding(path, f"Internal property leaked in Output section: {name}"))
            if kind and kind not in {"OutArgument", "InOutArgument"}:
                findings.append(Finding(path, f"Output kind must be OutArgument or InOutArgument, found: {kind}"))

    # One-of required checks
    required_set = set(required_inputs)
    for left, right in ONE_OF_PAIRS:
        if left in required_set and right in required_set:
            findings.append(Finding(path, f"Mutually exclusive inputs both marked required: {left}, {right}"))

    # Required fields should appear in XAML example attrs.
    if xaml_content:
        for name in required_inputs:
            if re.search(rf"\b{name}\s*=", xaml_content) is None:
                findings.append(Finding(path, f"Required input missing from XAML example: {name}"))

    return findings


def _collect_docs(root: Path) -> list[Path]:
    return sorted([p for p in root.rglob("*.md") if p.is_file() and p.name.lower() != "readme.md"])


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate generated activity markdown docs")
    parser.add_argument("docs_root", help="Root folder containing generated docs")
    parser.add_argument("--strict", action="store_true", help="Treat findings as blocking failures")
    args = parser.parse_args()

    docs_root = Path(args.docs_root).resolve()
    if not docs_root.exists():
        print(f"Docs root not found: {docs_root}", file=sys.stderr)
        return 2

    files = _collect_docs(docs_root)
    if not files:
        print(f"No markdown files found under {docs_root}", file=sys.stderr)
        return 2

    all_findings: list[Finding] = []
    for path in files:
        all_findings.extend(_check_file(path))

    if all_findings:
        print("Validation findings:")
        for finding in all_findings:
            rel = finding.path.relative_to(docs_root)
            print(f"- {rel}: {finding.message}")
        if args.strict:
            print(f"\nValidation failed with {len(all_findings)} finding(s).", file=sys.stderr)
            return 1

    print(f"Validated {len(files)} markdown file(s).")
    if all_findings:
        print(f"Total findings: {len(all_findings)}")
    else:
        print("No findings.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
