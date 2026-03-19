#!/usr/bin/env python3
"""
Extract coded workflow API surface from a UiPath activity domain.

Finds ICodedWorkflowsServiceRegistry implementations and extracts the public
API: service interfaces, handle types, options classes, and model types.

Usage:
    python extract-coded-api.py <domain_root> [--output <path>]

Output: JSON with the following structure:
{
  "hasCodedApi": true|false,
  "registry": {
    "file": "path/to/Registry.cs",
    "className": "MailRegistry",
    "serviceName": "mail",
    "serviceInterface": "IMailService",
    "autoImportedNamespaces": ["Namespace1", ...],
    "assemblyAttribute": true
  },
  "serviceInterface": {
    "file": "path/to/IMailService.cs",
    "name": "IMailService",
    "methods": [
      {
        "name": "MethodName",
        "returnType": "IWorkHandle",
        "parameters": [
          { "name": "param", "type": "string", "default": null }
        ],
        "xmlDoc": "Summary from XML comments"
      }
    ]
  },
  "handleTypes": [
    {
      "file": "path/to/IWorkHandle.cs",
      "name": "IWorkHandle",
      "isDisposable": true,
      "methods": [...],
      "properties": [...]
    }
  ],
  "optionsClasses": [
    {
      "file": "path/to/Options.cs",
      "name": "WorkbookOptions",
      "properties": [...]
    }
  ],
  "enums": [
    { "name": "EnumName", "values": ["Value1", "Value2"] }
  ],
  "codedWorkflowSupportedActivities": 42
}
"""

import json
import re
import sys
from pathlib import Path

# Add shared utilities to path (relative to this script's location)
sys.path.insert(0, str(Path(__file__).resolve().parent.parent.parent / "shared"))
from script_utils import find_files, read_file, find_brace_block, run_cli  # noqa: E402


# ---------------------------------------------------------------------------
# Registry extraction
# ---------------------------------------------------------------------------

RE_REGISTRY_CLASS = re.compile(
    r"class\s+(\w+)\s*:\s*ICodedWorkflowsServiceRegistry"
)
RE_SERVICE_NAME = re.compile(
    r'(?:_serviceName|serviceName)\s*=\s*"(\w+)"'
)
# Accepts namespace-qualified and global:: prefixed type names
RE_AUTO_IMPORTED_NS = re.compile(
    r'AutoImportedNamespaces\s*=>\s*new\s*\[\]\s*\{([^}]+)\}'
    r'|'
    r'AutoImportedNamespaces\s*=>\s*\[([^\]]+)\]'
)
RE_AUTO_IMPORTED_TYPES = re.compile(
    r'\{\s*(?:"(\w+)"|_?\w+)\s*,\s*typeof\((?:global::)?([\w.]+(?:<[^>]+>)?)\)\s*\}'
)
# Matches both [assembly: CodedWorkflowsServiceRegistryAttribute(...)] and
# the short form [assembly: CodedWorkflowsServiceRegistry(...)] (C# allows
# omitting the Attribute suffix in attribute syntax).
RE_ASSEMBLY_ATTR = re.compile(
    r'\[assembly:\s*CodedWorkflowsServiceRegistry(?:Attribute)?\(typeof\((?:global::)?([\w.]+(?:<[^>]+>)?)\)\)\]'
)
RE_REGISTER_TYPE = re.compile(
    r'serviceLocator\.RegisterType<\s*(?:global::)?([\w.]+(?:<[^>]+>)?)\s*,\s*(?:global::)?([\w.]+(?:<[^>]+>)?)\s*>\(\)'
)


def _extract_registry_details(content: str, path: str, class_name: str) -> dict:
    """Extract service name, interface, namespaces, and registered types from registry content."""
    # Service name from field/const
    service_name = None
    sn_match = RE_SERVICE_NAME.search(content)
    if sn_match:
        service_name = sn_match.group(1)

    # Service interface from AutoImportedTypes
    service_interface = None
    ait_match = RE_AUTO_IMPORTED_TYPES.search(content)
    if ait_match:
        if ait_match.group(1) and not service_name:
            service_name = ait_match.group(1)
        # Use the short name (last segment) for file lookups
        service_interface = ait_match.group(2).split(".")[-1]

    # Auto-imported namespaces
    ns_match = RE_AUTO_IMPORTED_NS.search(content)
    namespaces: list[str] = []
    if ns_match:
        ns_str = ns_match.group(1) or ns_match.group(2) or ""
        namespaces = [s.strip().strip('"') for s in ns_str.split(",") if s.strip().strip('"')]

    # Assembly attribute
    has_assembly_attr = bool(RE_ASSEMBLY_ATTR.search(content))

    # Registered types
    registered_types = [
        {"interface": rt.group(1).split(".")[-1], "implementation": rt.group(2).split(".")[-1]}
        for rt in RE_REGISTER_TYPE.finditer(content)
    ]

    return {
        "file": path,
        "className": class_name,
        "serviceName": service_name,
        "serviceInterface": service_interface,
        "autoImportedNamespaces": namespaces,
        "assemblyAttribute": has_assembly_attr,
        "registeredTypes": registered_types,
    }


def extract_registry(root: str) -> dict | None:
    """Find and extract ICodedWorkflowsServiceRegistry data.

    Detection uses two signals (either is sufficient):
      1. A class implementing ICodedWorkflowsServiceRegistry directly.
      2. An [assembly: CodedWorkflowsServiceRegistry(Attribute)] attribute.

    The broader substring 'CodedWorkflowsServiceRegistry' is used as the
    initial filter so that short-form attributes and interface implementations
    are both caught.
    """
    cs_files = find_files(root, "*.cs")

    # Pass 1: look for class implementing the interface (the common pattern)
    for path in cs_files:
        content = read_file(path)
        if not content or "CodedWorkflowsServiceRegistry" not in content:
            continue

        cls_match = RE_REGISTRY_CLASS.search(content)
        if not cls_match:
            continue

        return _extract_registry_details(content, path, cls_match.group(1))

    # Pass 2: fallback — look for assembly attribute without a direct
    # class : ICodedWorkflowsServiceRegistry in the same file (e.g. the
    # attribute and class may be split across files, or the class inherits
    # the interface via an intermediate base).
    for path in cs_files:
        content = read_file(path)
        if not content or "CodedWorkflowsServiceRegistry" not in content:
            continue

        attr_match = RE_ASSEMBLY_ATTR.search(content)
        if not attr_match:
            continue

        # The attribute references the registry class by typeof(ClassName)
        class_name = attr_match.group(1).split(".")[-1]
        print(
            f"  Note: Found assembly attribute referencing {class_name} but no "
            f"direct interface implementation in {path}. Extracting from attribute.",
            file=sys.stderr,
        )

        # Try to find the actual class file to extract details
        for cls_path in cs_files:
            cls_content = read_file(cls_path)
            if cls_content and f"class {class_name}" in cls_content:
                return _extract_registry_details(cls_content, cls_path, class_name)

        # Last resort: extract what we can from the attribute file itself
        return _extract_registry_details(content, path, class_name)

    return None


# ---------------------------------------------------------------------------
# XML doc extraction
# ---------------------------------------------------------------------------

RE_XML_SUMMARY_SINGLE = re.compile(r"///\s*<summary>(.*?)</summary>")
RE_XML_SUMMARY_MULTI = re.compile(
    r"///\s*<summary>\s*\n((?:\s*///.*\n)*?)\s*///\s*</summary>", re.MULTILINE
)


def extract_xml_doc(xmldoc: str) -> str | None:
    """Extract summary text from XML doc comments."""
    if not xmldoc:
        return None

    m = RE_XML_SUMMARY_SINGLE.search(xmldoc)
    if m:
        return m.group(1).strip()

    m = RE_XML_SUMMARY_MULTI.search(xmldoc)
    if m:
        lines = m.group(1).strip().split("\n")
        return " ".join(
            line.strip().lstrip("/").strip()
            for line in lines
            if line.strip().lstrip("/").strip()
        )

    return None


# ---------------------------------------------------------------------------
# Interface / class member extraction
# ---------------------------------------------------------------------------

RE_METHOD = re.compile(
    r"""
    (?P<xmldoc>(?:\s*///.*\n)*)                  # XML doc comments
    \s*(?P<return_type>[\w<>\[\]?,\s]+?)\s+      # return type
    (?P<name>\w+)\s*                              # method name
    \((?P<params>[^)]*)\)                         # parameters
    \s*;                                          # semicolon (interface method)
    """,
    re.VERBOSE,
)

RE_PROPERTY_DECL = re.compile(
    r"""
    (?P<xmldoc>(?:\s*///.*\n)*)                  # XML doc comments
    \s*(?:public\s+)?                             # optional access modifier
    (?P<type>[\w<>\[\]?,\s]+?)\s+                 # type
    (?P<name>\w+)\s*                              # property name
    \{\s*get;\s*(?:set;)?\s*\}                    # property accessors
    (?:\s*=\s*(?P<default>[^;]+))?                # optional default
    """,
    re.VERBOSE,
)


def parse_parameters(param_str: str) -> list[dict]:
    """Parse a C# method parameter string into structured data.

    Handles nested generics (e.g. Dictionary<string, List<int>>) by tracking
    bracket depth when splitting on commas.
    """
    if not param_str.strip():
        return []

    # Split on commas respecting generic bracket depth
    params: list[str] = []
    depth = 0
    current = ""
    for char in param_str:
        if char in "<(":
            depth += 1
        elif char in ">)":
            depth -= 1
        elif char == "," and depth == 0:
            if current.strip():
                params.append(current.strip())
            current = ""
            continue
        current += char
    if current.strip():
        params.append(current.strip())

    result = []
    for p in params:
        default = None
        if "=" in p:
            p, default = p.rsplit("=", 1)
            default = default.strip()
            p = p.strip()

        parts = p.rsplit(None, 1)
        if len(parts) == 2:
            result.append({
                "type": parts[0].strip(),
                "name": parts[1].strip(),
                "default": default,
            })

    return result


def extract_interface_members(content: str, interface_name: str) -> dict:
    """Extract methods and properties from an interface declaration."""
    pattern = re.compile(
        rf"(?:public\s+)?interface\s+{re.escape(interface_name)}(?:\s*:\s*[\w\s,<>]+)?\s*\{{",
    )
    m = pattern.search(content)
    if not m:
        return {"methods": [], "properties": []}

    body = find_brace_block(content, m.end() - 1)

    methods = [
        {
            "name": mm.group("name"),
            "returnType": mm.group("return_type").strip(),
            "parameters": parse_parameters(mm.group("params")),
            "xmlDoc": extract_xml_doc(mm.group("xmldoc")),
        }
        for mm in RE_METHOD.finditer(body)
    ]

    properties = [
        {
            "name": pm.group("name"),
            "type": pm.group("type").strip(),
            "default": pm.group("default").strip() if pm.group("default") else None,
            "xmlDoc": extract_xml_doc(pm.group("xmldoc")),
        }
        for pm in RE_PROPERTY_DECL.finditer(body)
    ]

    return {"methods": methods, "properties": properties}


def extract_class_properties(content: str, class_name: str) -> list[dict]:
    """Extract properties from a class (for Options/config classes)."""
    pattern = re.compile(rf"class\s+{re.escape(class_name)}[^{{]*\{{")
    m = pattern.search(content)
    if not m:
        return []

    body = find_brace_block(content, m.end() - 1)

    return [
        {
            "name": pm.group("name"),
            "type": pm.group("type").strip(),
            "default": pm.group("default").strip() if pm.group("default") else None,
            "xmlDoc": extract_xml_doc(pm.group("xmldoc")),
        }
        for pm in RE_PROPERTY_DECL.finditer(body)
    ]


# ---------------------------------------------------------------------------
# File lookup helpers
# ---------------------------------------------------------------------------

def find_interface_file(root: str, interface_name: str) -> str | None:
    """Find the .cs file containing an interface definition."""
    # Fast path: exact filename match
    for path in find_files(root, f"{interface_name}.cs"):
        content = read_file(path)
        if content and f"interface {interface_name}" in content:
            return path

    # Broader search in API-related projects
    for path in find_files(root, "*.cs"):
        if ".API" in path or ".Api" in path or "Api" in path:
            content = read_file(path)
            if content and f"interface {interface_name}" in content:
                return path

    return None


# ---------------------------------------------------------------------------
# Enum extraction
# ---------------------------------------------------------------------------

RE_ENUM = re.compile(r"public\s+enum\s+(\w+)\s*\{([^}]+)\}")


def extract_enums(root: str) -> list[dict]:
    """Extract enum types from API project files."""
    enums: list[dict] = []
    seen: set[str] = set()

    api_files = [f for f in find_files(root, "*.cs")
                 if ".API" in f or ".Api" in f or "API" in f]

    for path in api_files:
        content = read_file(path)
        if not content:
            continue

        for m in RE_ENUM.finditer(content):
            name = m.group(1)
            if name in seen:
                continue
            seen.add(name)

            values = [
                v.strip().split("=")[0].strip()
                for v in m.group(2).split(",")
                if v.strip() and not v.strip().startswith("//")
            ]
            enums.append({"name": name, "values": values, "file": path})

    return enums


# ---------------------------------------------------------------------------
# Metadata count helper
# ---------------------------------------------------------------------------

def count_coded_workflow_support(root: str) -> int:
    """Count activities with codedWorkflowSupport: true in metadata."""
    count = 0
    for path in find_files(root, "ActivitiesMetadata*.json"):
        content = read_file(path)
        if not content:
            continue
        try:
            data = json.loads(content)
            count += sum(1 for act in data.get("activities", []) if act.get("codedWorkflowSupport"))
        except json.JSONDecodeError:
            pass
    return count


# ---------------------------------------------------------------------------
# Handle / options / service extraction (orchestration helpers)
# ---------------------------------------------------------------------------

# Matches known handle naming conventions in return types
RE_HANDLE_TYPE = re.compile(r"\bI\w+Handle\b|\bI\w+Connection\b|\bUiTargetApp\b")

# Matches Options/Config/Settings type names inside generics or standalone
RE_OPTIONS_TYPE_NAME = re.compile(r"\b([\w.]*?(?:Options|Config|Settings))\b")


def _extract_handle_types(root: str, service_data: dict) -> list[dict]:
    """Extract handle/connection interfaces referenced by service method return types."""
    return_types: set[str] = set()
    for method in service_data.get("methods", []):
        for match in RE_HANDLE_TYPE.finditer(method["returnType"]):
            return_types.add(match.group())

    handle_types = []
    for handle_name in sorted(return_types):
        handle_file = find_interface_file(root, handle_name)
        if not handle_file:
            continue
        content = read_file(handle_file)
        if not content:
            continue

        members = extract_interface_members(content, handle_name)
        handle_decl = re.search(
            rf"interface\s+{re.escape(handle_name)}\s*:\s*([^{{]+)", content
        )
        is_disposable = bool(handle_decl and "IDisposable" in handle_decl.group(1))
        handle_types.append({
            "file": handle_file,
            "name": handle_name,
            "isDisposable": is_disposable,
            **members,
        })
        print(f"  Extracted handle: {handle_name} ({len(members['methods'])} methods)", file=sys.stderr)

    return handle_types


def _extract_options_classes(root: str, service_data: dict) -> list[dict]:
    """Extract Options/Config/Settings classes referenced by service method parameters."""
    param_types: set[str] = set()
    for method in service_data.get("methods", []):
        for param in method.get("parameters", []):
            # Extract candidate type names, handling generics like IEnumerable<WorkbookOptions>
            for match in RE_OPTIONS_TYPE_NAME.finditer(param["type"]):
                simple_name = match.group(1).split(".")[-1]
                param_types.add(simple_name)

    options_classes = []
    for opt_name in sorted(param_types):
        for path in find_files(root, f"{opt_name}.cs"):
            content = read_file(path)
            if content and f"class {opt_name}" in content:
                props = extract_class_properties(content, opt_name)
                options_classes.append({
                    "file": path,
                    "name": opt_name,
                    "properties": props,
                })
                print(f"  Extracted options: {opt_name} ({len(props)} properties)", file=sys.stderr)
                break

    return options_classes


# ---------------------------------------------------------------------------
# Domain scanning orchestration
# ---------------------------------------------------------------------------

def scan_domain(root: str) -> dict:
    """Scan a domain directory and return the coded API surface as a dict."""
    print(f"Scanning domain for coded API: {root}", file=sys.stderr)

    coded_count = count_coded_workflow_support(root)
    print(f"Activities with codedWorkflowSupport=true: {coded_count}", file=sys.stderr)

    registry = extract_registry(root)
    if not registry:
        print("No ICodedWorkflowsServiceRegistry found.", file=sys.stderr)
        return {
            "hasCodedApi": False,
            "codedWorkflowSupportedActivities": coded_count,
            "reason": "No ICodedWorkflowsServiceRegistry implementation found",
        }

    print(f"Found registry: {registry['className']} in {registry['file']}", file=sys.stderr)
    print(f"  Service: {registry['serviceName']} -> {registry['serviceInterface']}", file=sys.stderr)

    # Service interface
    service_data = None
    if registry["serviceInterface"]:
        iface_file = find_interface_file(root, registry["serviceInterface"])
        if iface_file:
            content = read_file(iface_file)
            if content:
                members = extract_interface_members(content, registry["serviceInterface"])
                iface_decl = re.search(
                    rf"interface\s+{re.escape(registry['serviceInterface'])}\s*:\s*([^{{]+)",
                    content,
                )
                is_disposable = bool(iface_decl and "IDisposable" in iface_decl.group(1))
                service_data = {
                    "file": iface_file,
                    "name": registry["serviceInterface"],
                    "isDisposable": is_disposable,
                    **members,
                }
                print(
                    f"  Extracted {len(members['methods'])} methods, "
                    f"{len(members['properties'])} properties from {registry['serviceInterface']}",
                    file=sys.stderr,
                )

    # Handle types, options classes, enums
    handle_types = _extract_handle_types(root, service_data) if service_data else []
    options_classes = _extract_options_classes(root, service_data) if service_data else []
    enums = extract_enums(root)
    print(f"  Found {len(enums)} enum types in API", file=sys.stderr)

    return {
        "hasCodedApi": True,
        "codedWorkflowSupportedActivities": coded_count,
        "registry": registry,
        "serviceInterface": service_data,
        "handleTypes": handle_types,
        "optionsClasses": options_classes,
        "enums": enums,
    }


# ---------------------------------------------------------------------------
# CLI entry point
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    run_cli("Extract coded workflow API from a UiPath domain", scan_domain)
