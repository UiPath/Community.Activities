#!/usr/bin/env python3
"""
Extract activity metadata from a UiPath activity domain.

Parses ActivitiesMetadata JSON, Activity .cs classes, ViewModel .cs classes,
and .resx resource files to produce a structured JSON catalog of all activities
with their properties, types, defaults, display names, and descriptions.

Usage:
    python extract-activity-metadata.py <domain_root> [--output <path>]

Output: JSON with the following structure per activity:
{
  "activities": [
    {
      "fullName": "Namespace.ClassName",
      "shortName": "ClassName",
      "displayName": "Resolved Display Name",
      "description": "Resolved Description",
      "category": "Resolved Category",
      "codedWorkflowSupport": false,
      "browsable": true,
      "mandatoryParent": null,
      "viewModelType": "Namespace.ViewModelClass",
      "sourceFile": "path/to/Activity.cs",
      "viewModelFile": "path/to/ViewModel.cs",
      "projectSettings": [ { "section": "...", "property": "...", "default": "..." } ],
      "properties": [
        {
          "name": "PropName",
          "displayName": "Resolved Display Name",
          "description": "Resolved Description/Tooltip",
          "kind": "InArgument|OutArgument|InOutArgument|Property",
          "type": "string",
          "genericType": "string",
          "required": true,
          "defaultValue": "value",
          "category": "Input",
          "browsable": true,
          "orderIndex": 1,
          "isVisible": true,
          "overloadGroup": null,
          "placeholder": null,
          "widget": null,
          "notMapped": false,
          "isProjectSetting": false,
          "projectSettingKey": null
        }
      ]
    }
  ],
  "resxKeys": { "KeyName": "Resolved Value", ... },
  "metadataFiles": ["path/to/ActivitiesMetadata.json", ...],
  "resxFiles": ["path/to/Resources.resx", ...]
}
"""

import json
import re
import sys
from pathlib import Path

# Add shared utilities to path (relative to this script's location)
sys.path.insert(0, str(Path(__file__).resolve().parent.parent.parent / "shared"))
from script_utils import find_files, read_file, parse_xml, run_cli  # noqa: E402


# ---------------------------------------------------------------------------
# .resx parsing
# ---------------------------------------------------------------------------

def parse_resx_files(resx_paths: list[str]) -> dict[str, str]:
    """Parse .resx XML files and return a key-to-value dictionary."""
    resx_map: dict[str, str] = {}
    for path in resx_paths:
        try:
            tree = parse_xml(path)
            for data in tree.getroot().findall("data"):
                name = data.get("name")
                value_elem = data.find("value")
                if name and value_elem is not None and value_elem.text:
                    resx_map[name] = value_elem.text.strip()
        except (Exception, OSError) as e:
            print(f"Warning: Could not parse resx file {path}: {e}", file=sys.stderr)
    return resx_map


# ---------------------------------------------------------------------------
# ActivitiesMetadata JSON parsing
# ---------------------------------------------------------------------------

def parse_metadata_json(json_paths: list[str]) -> tuple[list[dict], list[str]]:
    """Parse ActivitiesMetadata JSON files and return activity entries + category ordering."""
    activities: list[dict] = []
    category_order: list[str] = []
    seen_names: set[str] = set()

    for path in json_paths:
        try:
            with open(path, "r", encoding="utf-8-sig") as f:
                data = json.load(f)
        except (json.JSONDecodeError, OSError) as e:
            print(f"Warning: Could not parse metadata file {path}: {e}", file=sys.stderr)
            continue

        if "orderedCategoryDisplayNameKeys" in data:
            category_order = data["orderedCategoryDisplayNameKeys"]

        for act in data.get("activities", []):
            full_name = act.get("fullName", "")
            if not full_name or full_name in seen_names:
                continue
            seen_names.add(full_name)
            activities.append({
                "fullName": full_name,
                "shortName": act.get("shortName", full_name.split(".")[-1]),
                "displayNameKey": act.get("displayNameKey"),
                "descriptionKey": act.get("descriptionKey"),
                "categoryKey": act.get("categoryKey"),
                "viewModelType": act.get("viewModelType"),
                "codedWorkflowSupport": act.get("codedWorkflowSupport", False),
                "browsable": act.get("browsable", True),
                "mandatoryParentActivityFullName": act.get("mandatoryParentActivityFullName"),
                "properties": act.get("properties", []),
                "metadataFile": path,
            })

    return activities, category_order


# ---------------------------------------------------------------------------
# .cs class file lookup
# ---------------------------------------------------------------------------

def find_cs_file_for_class(root: str, full_class_name: str) -> str | None:
    """Find the .cs file containing a class by its fully qualified name."""
    class_name = full_class_name.split(".")[-1]
    pattern = re.compile(rf"\bclass\s+{re.escape(class_name)}\b")

    # Try files named after the class first (fast path)
    candidates = find_files(root, f"{class_name}.cs")
    if not candidates:
        candidates = find_files(root, "*.cs")

    for path in candidates:
        content = read_file(path)
        if content and pattern.search(content):
            return path

    return None


# ---------------------------------------------------------------------------
# Activity class property extraction
# ---------------------------------------------------------------------------

# Matches public auto-properties: plain types, InArgument<T>, OutArgument<T>, InOutArgument<T>
RE_PROPERTY = re.compile(
    r"""
    (?P<attrs>(?:\[.*?\]\s*)*)                      # attribute block
    public\s+
    (?:(?:new|override|virtual|sealed)\s+)*          # optional modifiers
    (?P<full_type>
        (?P<arg_type>InArgument|OutArgument|InOutArgument)
        (?:<(?P<generic_type>[^>]+)>)?
        |
        (?P<plain_type>[A-Za-z_][\w.<>,\[\]\s?]*)
    )\s+
    (?P<name>[A-Za-z_]\w*)\s*
    \{\s*get;\s*set;\s*\}
    (?:\s*=\s*(?P<default>[^;]+))?
    """,
    re.VERBOSE | re.MULTILINE | re.DOTALL,
)

RE_LOCALIZED_DISPLAY_NAME = re.compile(
    r"\[LocalizedDisplayName\(nameof\((?:\w+\.)*(\w+)\)\)\]"
)
RE_LOCALIZED_DESCRIPTION = re.compile(
    r"\[LocalizedDescription\(nameof\((?:\w+\.)*(\w+)\)\)\]"
)
RE_LOCALIZED_CATEGORY = re.compile(
    r"\[LocalizedCategory\(nameof\((?:\w+\.)*(\w+)\)\)\]"
)
RE_REQUIRED = re.compile(r"\[RequiredArgument\]")
RE_DEFAULT_VALUE = re.compile(r'\[DefaultValue\(([^)]+)\)\]')
RE_BROWSABLE_FALSE = re.compile(r"\[Browsable\(\s*false\s*\)\]")
RE_OVERLOAD_GROUP = re.compile(r'\[OverloadGroup\("([^"]+)"\)\]')
RE_ARGUMENT_SETTING = re.compile(r'\[(?:\w*)?ArgumentSetting\w*\(([^)]+)\)\]')

# Properties that are framework internals, not user-facing
_SKIP_PROPERTY_NAMES = frozenset({"Body", "CacheId", "Implementation"})
_SKIP_TYPE_PREFIXES = ("ActivityAction", "ActivityFunc")


def extract_activity_properties(content: str) -> list[dict]:
    """Extract public auto-properties from an Activity .cs file."""
    properties = []

    for m in RE_PROPERTY.finditer(content):
        name = m.group("name")
        if name in _SKIP_PROPERTY_NAMES:
            continue

        arg_type = m.group("arg_type")
        generic_type = m.group("generic_type")
        plain_type = m.group("plain_type")

        if arg_type:
            kind = arg_type
            prop_type = generic_type or "object"
        else:
            kind = "Property"
            prop_type = plain_type.strip() if plain_type else "object"

        if any(prop_type.startswith(prefix) for prefix in _SKIP_TYPE_PREFIXES):
            continue

        attrs = m.group("attrs") or ""
        inline_default = m.group("default")

        prop = _build_property_dict(name, kind, prop_type, generic_type, attrs, inline_default)
        properties.append(prop)

    return properties


def _build_property_dict(
    name: str,
    kind: str,
    prop_type: str,
    generic_type: str | None,
    attrs: str,
    inline_default: str | None,
) -> dict:
    """Build a structured property dict from parsed regex groups and attributes."""
    prop = {
        "name": name,
        "kind": kind,
        "type": prop_type,
        "genericType": generic_type,
        "required": bool(RE_REQUIRED.search(attrs)),
        "browsable": not bool(RE_BROWSABLE_FALSE.search(attrs)),
        "displayNameKey": None,
        "descriptionKey": None,
        "categoryKey": None,
        "defaultValue": None,
        "overloadGroup": None,
        "isProjectSetting": False,
        "projectSettingArgs": None,
    }

    dn = RE_LOCALIZED_DISPLAY_NAME.search(attrs)
    if dn:
        prop["displayNameKey"] = dn.group(1)

    desc = RE_LOCALIZED_DESCRIPTION.search(attrs)
    if desc:
        prop["descriptionKey"] = desc.group(1)

    cat = RE_LOCALIZED_CATEGORY.search(attrs)
    if cat:
        prop["categoryKey"] = cat.group(1)

    dv = RE_DEFAULT_VALUE.search(attrs)
    if dv:
        prop["defaultValue"] = dv.group(1).strip().strip('"')
    elif inline_default:
        prop["defaultValue"] = inline_default.strip().rstrip(";").strip()

    og = RE_OVERLOAD_GROUP.search(attrs)
    if og:
        prop["overloadGroup"] = og.group(1)

    asetting = RE_ARGUMENT_SETTING.search(attrs)
    if asetting:
        prop["isProjectSetting"] = True
        prop["projectSettingArgs"] = asetting.group(1).strip()

    return prop


# ---------------------------------------------------------------------------
# ViewModel property extraction
# ---------------------------------------------------------------------------

RE_VM_PROPERTY = re.compile(
    r"public\s+"
    r"(?P<vm_type>DesignInArgument|DesignOutArgument|DesignInOutArgument|DesignProperty)"
    r"<(?P<generic_type>[^>]+)>\s+"
    r"(?P<name>\w+)\s*\{",
)

RE_VM_ASSIGNMENT = re.compile(
    r"(?P<prop>\w+)\.(?P<field>OrderIndex|ColumnOrder|Category|DisplayName|Tooltip|"
    r"Placeholder|IsRequired|IsPrincipal|IsVisible|Widget|NotMappedProperty)\s*=\s*(?P<value>[^;]+);"
)

_VM_KIND_MAP = {
    "DesignInArgument": "InArgument",
    "DesignOutArgument": "OutArgument",
    "DesignInOutArgument": "InOutArgument",
    "DesignProperty": "Property",
}

_VM_FIELD_MAP = {
    "OrderIndex": "orderIndex",
    "ColumnOrder": "columnOrder",
    "Category": "category",
    "DisplayName": "displayName",
    "Tooltip": "tooltip",
    "Placeholder": "placeholder",
    "IsRequired": "isRequired",
    "IsPrincipal": "isPrincipal",
    "IsVisible": "isVisible",
    "NotMappedProperty": "notMapped",
}


def extract_viewmodel_metadata(content: str) -> dict[str, dict]:
    """Extract property metadata from a ViewModel .cs file."""
    vm_props: dict[str, dict] = {}

    for m in RE_VM_PROPERTY.finditer(content):
        name = m.group("name")
        vm_props[name] = {
            "kind": _VM_KIND_MAP.get(m.group("vm_type"), "Property"),
            "type": m.group("generic_type"),
            "orderIndex": None,
            "category": None,
            "displayName": None,
            "tooltip": None,
            "placeholder": None,
            "isRequired": None,
            "isVisible": None,
            "isPrincipal": None,
            "widget": None,
            "notMapped": False,
        }

    for m in RE_VM_ASSIGNMENT.finditer(content):
        prop_name = m.group("prop")
        if prop_name not in vm_props:
            continue

        field = m.group("field")
        value = m.group("value").strip()
        mapped = _VM_FIELD_MAP.get(field)

        if mapped:
            vm_props[prop_name][mapped] = _coerce_assignment_value(value)

        if field == "Widget":
            wt = re.search(r"ViewModelWidgetType\.(\w+)", value)
            if wt:
                vm_props[prop_name]["widget"] = wt.group(1)

    return vm_props


def _coerce_assignment_value(value: str) -> str | bool | int:
    """Coerce a C# assignment RHS to a Python primitive."""
    if value.lower() in ("true", "false"):
        return value.lower() == "true"
    if value.isdigit() or (value.startswith("-") and value[1:].isdigit()):
        return int(value)
    # Extract resource key from Resources.XXX
    if value.startswith("Resources.") or value.startswith("resources."):
        return value.split(".")[-1]
    return value


# ---------------------------------------------------------------------------
# Resource key resolution
# ---------------------------------------------------------------------------

def resolve_key(key: str | None, resx_map: dict[str, str]) -> str | None:
    """Resolve a resource key to its string value, falling back to the key itself."""
    if not key:
        return None
    return resx_map.get(key, key)


# ---------------------------------------------------------------------------
# Merging: metadata + activity props + ViewModel props -> unified record
# ---------------------------------------------------------------------------

def _pascal_to_display(name: str) -> str:
    """Convert PascalCase to space-separated display name."""
    return re.sub(r"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ", name)


def _merge_single_property(
    prop: dict,
    vm: dict,
    resx_map: dict[str, str],
) -> dict:
    """Merge one activity property with its ViewModel counterpart."""
    display_name = (
        resolve_key(vm.get("displayName"), resx_map)
        or resolve_key(prop.get("displayNameKey"), resx_map)
        or _pascal_to_display(prop["name"])
    )
    description = (
        resolve_key(vm.get("tooltip"), resx_map)
        or resolve_key(prop.get("descriptionKey"), resx_map)
    )
    category = (
        resolve_key(vm.get("category"), resx_map)
        or resolve_key(prop.get("categoryKey"), resx_map)
    )

    return {
        "name": prop["name"],
        "displayName": display_name,
        "description": description,
        "kind": vm.get("kind") or prop["kind"],
        "type": prop["type"],
        "genericType": prop.get("genericType"),
        "required": vm.get("isRequired") if vm.get("isRequired") is not None else prop["required"],
        "defaultValue": prop.get("defaultValue"),
        "category": category,
        "browsable": prop.get("browsable", True),
        "orderIndex": vm.get("orderIndex"),
        "isVisible": vm.get("isVisible", True),
        "overloadGroup": prop.get("overloadGroup"),
        "placeholder": resolve_key(vm.get("placeholder"), resx_map),
        "widget": vm.get("widget"),
        "notMapped": False,
        "isProjectSetting": prop.get("isProjectSetting", False),
        "projectSettingArgs": prop.get("projectSettingArgs"),
    }


def _make_vm_only_property(name: str, vm: dict, resx_map: dict[str, str]) -> dict:
    """Build a property dict for a ViewModel-only property (no activity counterpart)."""
    return {
        "name": name,
        "displayName": resolve_key(vm.get("displayName"), resx_map) or _pascal_to_display(name),
        "description": resolve_key(vm.get("tooltip"), resx_map),
        "kind": vm.get("kind", "Property"),
        "type": vm.get("type", "object"),
        "genericType": None,
        "required": vm.get("isRequired", False),
        "defaultValue": None,
        "category": resolve_key(vm.get("category"), resx_map),
        "browsable": True,
        "orderIndex": vm.get("orderIndex"),
        "isVisible": vm.get("isVisible", True),
        "overloadGroup": None,
        "placeholder": resolve_key(vm.get("placeholder"), resx_map),
        "widget": vm.get("widget"),
        "notMapped": False,
        "isProjectSetting": False,
        "projectSettingArgs": None,
    }


def _property_sort_key(prop: dict) -> tuple:
    """Sort key: orderIndex ascending (None last), then name."""
    oi = prop["orderIndex"]
    if isinstance(oi, int):
        return (oi, prop["name"])
    return (9999, prop["name"])


def merge_activity_data(
    meta_entry: dict,
    activity_props: list[dict],
    vm_metadata: dict[str, dict],
    resx_map: dict[str, str],
    activity_file: str | None,
    vm_file: str | None,
) -> dict:
    """Merge metadata, activity properties, and ViewModel data into a single record."""
    merged_props = []

    # Merge activity properties with their ViewModel counterparts
    for prop in activity_props:
        vm = vm_metadata.get(prop["name"], {})
        if vm.get("notMapped"):
            continue
        merged_props.append(_merge_single_property(prop, vm, resx_map))

    # Add ViewModel-only properties not found in the activity class
    activity_prop_names = {p["name"] for p in activity_props}
    for name, vm in vm_metadata.items():
        if name not in activity_prop_names and not vm.get("notMapped"):
            merged_props.append(_make_vm_only_property(name, vm, resx_map))

    merged_props.sort(key=_property_sort_key)

    return {
        "fullName": meta_entry["fullName"],
        "shortName": meta_entry["shortName"],
        "displayName": resolve_key(meta_entry.get("displayNameKey"), resx_map) or meta_entry["shortName"],
        "description": resolve_key(meta_entry.get("descriptionKey"), resx_map),
        "category": resolve_key(meta_entry.get("categoryKey"), resx_map),
        "codedWorkflowSupport": meta_entry.get("codedWorkflowSupport", False),
        "browsable": meta_entry.get("browsable", True),
        "mandatoryParent": meta_entry.get("mandatoryParentActivityFullName"),
        "viewModelType": meta_entry.get("viewModelType"),
        "sourceFile": activity_file,
        "viewModelFile": vm_file,
        "properties": merged_props,
    }


# ---------------------------------------------------------------------------
# Project settings extraction
# ---------------------------------------------------------------------------

def extract_project_settings(activity_props: list[dict]) -> list[dict]:
    """Extract project settings from activity properties with ArgumentSettingAttribute."""
    return [
        {
            "propertyName": prop["name"],
            "settingArgs": prop.get("projectSettingArgs"),
            "type": prop["type"],
        }
        for prop in activity_props
        if prop.get("isProjectSetting")
    ]


# ---------------------------------------------------------------------------
# Domain scanning orchestration
# ---------------------------------------------------------------------------

def _extract_single_activity(
    meta: dict,
    root: str,
    resx_map: dict[str, str],
) -> dict | None:
    """Extract and merge data for a single activity. Returns None if non-browsable."""
    if not meta.get("browsable", True):
        print(f"  Skipping non-browsable: {meta['shortName']}", file=sys.stderr)
        return None

    # Activity class
    activity_file = find_cs_file_for_class(root, meta["fullName"])
    activity_props: list[dict] = []
    if activity_file:
        content = read_file(activity_file)
        if content:
            activity_props = extract_activity_properties(content)
    else:
        print(f"  Warning: Could not find source for {meta['fullName']}", file=sys.stderr)

    # ViewModel class
    vm_file = None
    vm_metadata: dict[str, dict] = {}
    if meta.get("viewModelType"):
        vm_file = find_cs_file_for_class(root, meta["viewModelType"])
        if vm_file:
            content = read_file(vm_file)
            if content:
                vm_metadata = extract_viewmodel_metadata(content)
        else:
            print(f"  Warning: Could not find ViewModel for {meta['viewModelType']}", file=sys.stderr)

    merged = merge_activity_data(meta, activity_props, vm_metadata, resx_map, activity_file, vm_file)
    merged["projectSettings"] = extract_project_settings(activity_props)
    print(f"  Extracted: {meta['shortName']} ({len(merged['properties'])} properties)", file=sys.stderr)
    return merged


def scan_domain(root: str) -> dict:
    """Scan a domain directory and return the full activity catalog as a dict."""
    print(f"Scanning domain: {root}", file=sys.stderr)

    # 1. Parse .resx files (default locale only)
    resx_paths = find_files(root, "*.resx")
    resx_paths = [p for p in resx_paths if not re.search(r"\.\w{2}(-\w{2})?\.resx$", p)]
    print(f"Found {len(resx_paths)} .resx files", file=sys.stderr)
    resx_map = parse_resx_files(resx_paths)
    print(f"Resolved {len(resx_map)} resource strings", file=sys.stderr)

    # 2. Parse ActivitiesMetadata JSON
    metadata_paths = find_files(root, "ActivitiesMetadata*.json")
    print(f"Found {len(metadata_paths)} metadata files", file=sys.stderr)
    meta_activities, category_order = parse_metadata_json(metadata_paths)
    print(f"Found {len(meta_activities)} activities in metadata", file=sys.stderr)

    # 3. Extract each activity
    results = []
    for meta in meta_activities:
        merged = _extract_single_activity(meta, root, resx_map)
        if merged:
            results.append(merged)

    return {
        "domainRoot": root,
        "activityCount": len(results),
        "categoryOrder": category_order,
        "activities": results,
        "resxKeyCount": len(resx_map),
        "metadataFiles": metadata_paths,
        "resxFiles": resx_paths,
    }


# ---------------------------------------------------------------------------
# CLI entry point
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    run_cli("Extract UiPath activity metadata from a domain", scan_domain)
