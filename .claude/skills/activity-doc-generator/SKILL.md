---
name: activity-doc-generator
description: >
  Generate structured markdown documentation for UiPath XAML activities from source code.
  Produces per-activity docs with full properties (inputs, outputs, configuration), types, defaults,
  project settings, valid property combinations, and copy-paste XAML examples. Use when asked to
  document activities, generate activity docs, create activity reference files, build activity
  mini-skills, or prepare package documentation for any activity domain in this repository. Also use
  when the user mentions "activity reference", "activity cheat sheet", "activity docs", or wants to
  understand what an activity does as a black box. If the user also wants coded workflow API docs,
  use the `coded-api-doc-generator` skill for that separately.
---

# Activity Doc Generator (XAML)

Generate structured markdown documentation for UiPath XAML activities. One file per activity, documenting every configurable property, default, output, project setting, valid property combinations, and a XAML example.

## What These Docs Are For

UiPath activities are Windows Workflow Foundation (WF) components serialized as XAML. When an AI coding assistant needs to generate or modify a XAML workflow, it needs to know:

- What properties each activity has (names, types, whether they're `InArgument<T>` expressions or plain XML attributes)
- Which properties are required vs optional, and their defaults
- Which property combinations are valid (some are mutually exclusive, some are conditionally visible)
- What the activity outputs (`OutArgument<T>`) so downstream activities can consume them
- The correct XML namespace prefix and attribute syntax

These docs serve as the **contract specification** that enables an AI agent to produce valid XAML. They are extracted into each UiPath Studio project for AI assistants to reference during workflow generation.

## Core Principles

1. **ViewModel is the source of truth** — The ViewModel class defines what appears in the designer and what goes into the XAML. Properties, their visibility, ordering, categories, and display names all come from the ViewModel (following its inheritance chain). The activity class defines runtime behavior but the ViewModel controls the public contract.
2. **Copy-paste ready** — XAML snippets must be valid XML fragments that can be dropped into a workflow.
3. **Valid configurations** — Document which property combinations are valid. Not all properties can coexist; rules and dependencies in the ViewModel control visibility and mutual exclusion.
4. **Source-of-truth extraction** — All information comes from source code via the extraction script. Do not invent or guess behavior.
5. **Outputs matter** — Always document `OutArgument`/`InOutArgument` properties. These are how users get data back from activities.
6. **Behavioral context when useful** — While the focus is on the public contract, briefly noting how an activity works under the hood (e.g., "uses the Excel Interop library", "calls the Orchestrator REST API") helps AI agents understand when and why to use it.

---

## Workflow: Script-First, Then Document

The process has two phases: **automated extraction** (deterministic script) followed by **intelligent documentation** (you). This avoids expensive manual grep/read cycles.

### Phase 1: Run the Extraction Script

```bash
python {skillPath}/scripts/extract-activity-metadata.py "{domainRoot}" --pretty --output /tmp/{domain}-metadata.json
```

The script follows the correct extraction order:

1. **ActivitiesMetadata JSON** — Discovers the activity catalog. Each entry provides `fullName`, `viewModelType`, `mandatoryParent`, `browsable` flag, and category. This is the authoritative list of activities in the package. Note: many packages split metadata into `ActivitiesMetadata.json` (cross-platform) and `ActivitiesMetadataWindows.json` (Windows-only). The script reads both — document platform availability when activities appear only in the Windows metadata.
2. **Resource files (.resx)** — Resolves all localized string keys to display strings (display names, descriptions, tooltips, categories).
3. **ViewModel classes** — The primary source for property extraction. ViewModels define `DesignInArgument<T>`, `DesignOutArgument<T>`, `DesignProperty<T>` with `OrderIndex`, `IsVisible`, `Category`, `DisplayName`, `Tooltip`, and `Widget`. Properties are extracted following the ViewModel's inheritance chain (base ViewModels may define shared properties like `ContinueOnError`, `Timeout`).
4. **Activity classes** — Supplements the ViewModel data with `[RequiredArgument]`, `[DefaultValue]`, `[ArgumentSettingAttribute]` (project settings), and `[OverloadGroup]` decorators. Also used to discover `InArgument<T>`/`OutArgument<T>` type information when the ViewModel doesn't expose it.

The script merges these sources and outputs structured JSON with all properties, types, defaults, display names, and descriptions.

### Phase 2: Generate Documentation (You)

For each activity in the JSON output, generate a markdown doc using the template at [assets/xaml-activity-template.md](assets/xaml-activity-template.md). The script gives you the raw data; your job is to add judgment:

1. **Descriptions** — If the script resolved a description, use it. If empty/missing, write a concise one based on the activity name and properties.
2. **Valid property configurations** — Read the ViewModel's `InitializeRules()` and `ManualRegisterDependencies()` to identify which properties are conditionally visible or mutually exclusive (see [Rules and Dependencies](#rules-and-dependencies) below). Document valid configurations explicitly.
3. **XAML examples** — Craft valid XML fragments showing one or more valid property configurations. Not all properties can coexist — the example must reflect a valid combination.
4. **Notes** — Scope requirements, platform constraints (Windows-only vs cross-platform), mutual exclusions, deprecation.
5. **Enum values** — For enum-typed properties, find and list all values with descriptions.
6. **Project settings** — If `isProjectSetting: true` on any property, document in a dedicated section.
7. **Outputs** — Ensure every `OutArgument`/`InOutArgument` is documented with its type and what it returns.
8. **Behavioral context** — When the activity wraps a well-known library or API (e.g., Excel Interop, Orchestrator REST API, SMTP), briefly note this so the AI agent understands the activity's capabilities and limitations.
9. **References** (optional) — If an activity has supplementary reference files (detailed examples, extended guidance, complex scenarios) that would bloat the main doc, place them in a `{ActivityClassName}/` subdirectory alongside the `.md` file and add a References section linking to them. Only include when such files exist.

---

## Step-by-Step

### Step 1: Identify Target

Determine what to document:
- **Single activity**: User names a specific activity (e.g., "document the Click activity")
- **Activity domain**: User names a domain folder (e.g., "generate docs for Excel activities")
- **Entire package**: User wants all activities documented

If unclear, ask the user.

### Step 2: Run Extraction

```bash
python {skillPath}/scripts/extract-activity-metadata.py "{domainRoot}" --pretty --output /tmp/{domain}-metadata.json
```

Read the JSON output. Check:
- `activityCount` — Number of activities found
- Warnings in stderr about missing source files or ViewModels

If the count seems low or the domain lacks ActivitiesMetadata JSON, fall back to manual discovery:
```
Grep: pattern="class \w+ : .*(CodeActivity|NativeActivity|AsyncCodeActivity|AsyncTaskCodeActivity|SdkActivity)"
      path="{domainRoot}" glob="*.cs"
```

### Step 3: Generate Per-Activity Docs

For each activity, use the template from [assets/xaml-activity-template.md](assets/xaml-activity-template.md).

#### Property Grouping Order

1. **Input** — `InArgument<T>` and input-category plain properties. Required properties first.
2. **Options/Configuration** — Plain properties (bool, enum, string, int) in "Options" or other categories
3. **Output** — `OutArgument<T>` and `InOutArgument<T>` — the activity's results
4. **Common** — ContinueOnError, Timeout, DisplayName (when present)

#### Output Properties

Output properties are critical — they're how users get data back from activities:
- Always include the property name and inner type (the `T` in `OutArgument<T>`)
- Describe what the output contains
- Note if the output is conditional (only populated in certain scenarios)

#### Project Settings

If any property has `isProjectSetting: true` in the script output, add:

```markdown
## Project Settings

This activity reads default values from project-level settings (configurable in UiPath Studio under Project Settings):

| Property | Setting Key | Default | Description |
|----------|-----------|---------|-------------|
| `Endpoint` | `UiPath.Sdk.Activities.{Section}.{Property}` | `https://...` | The API endpoint URL |
```

These settings apply to all instances of the activity in a project, acting as configurable defaults.

#### Rules and Dependencies

The ViewModel defines **rules** that react to property value changes and **dependencies** that trigger those rules. These control which property combinations are valid. To extract them:

1. **Find the ViewModel** — The script output includes `viewModelFile` for each activity. Read that file.
2. **Look for `InitializeRules()`** — Rules define handlers (sync or async) that execute when properties change. Common patterns:
   - **Conditional visibility**: `MyProperty.IsVisible = someCondition;` — a property only appears when another property has a specific value
   - **Mutual exclusion**: Setting one property hides or disables another
   - **Dynamic defaults**: One property's default changes based on another
3. **Look for `ManualRegisterDependencies()`** — Maps which property changes trigger which rules via `RegisterDependency(SourceProp, nameof(SourceProp.Value), "RuleName")`
4. **Look for `[OverloadGroup]`** on the activity class — These define mutually exclusive property groups at the WF level (independent of ViewModel rules)

Document these as valid configurations in the activity doc:

```markdown
## Valid Configurations

This activity supports two modes:

**Mode A — By Range**: Set `Range` to a cell reference. `SheetName` is optional (defaults to active sheet).
**Mode B — By Table**: Set `TableName` instead. `Range` and `SheetName` are ignored.

Properties `Range` and `TableName` are mutually exclusive.
```

When generating XAML examples, show at least one valid configuration. If there are multiple modes, show examples for each.

### Step 4: Generate Overview

Create `overview.md` at the package level listing all documented activities by category, with links and one-line descriptions. See the overview template section in [assets/xaml-activity-template.md](assets/xaml-activity-template.md).

Note if the package has activities in both cross-platform and Windows-only metadata files — group them accordingly in the overview.

### Step 5: Place Output Files

Place docs inside the project that **produces the published `.nupkg`** — the one with a `<PackageId>` that is packable. This is NOT always the `*.Package.csproj` — some domains split into design-time and runtime packaging projects. Check `<PackageId>` and `<IsPackable>` to identify the correct project (see [shared/output-structure.md](../../shared/output-structure.md) for details and examples).

```
{PublishedPackageProject}/
  docs/
    overview.md                # Package overview with activity index
    activities/                # Per-activity docs
      ActivityOne.md
      ActivityOne/             # Optional reference files for ActivityOne
        some-reference.md
      ActivityTwo.md
      ...
```

Inform the user about the nuspec/csproj packaging change needed — do NOT modify packaging files unless asked.

---

## Portable vs Windows Metadata

Many packages split their `ActivitiesMetadata*.json` into two files:

- **`ActivitiesMetadata.json`** — Cross-platform (Portable) activities that work on both Windows and Linux
- **`ActivitiesMetadataWindows.json`** — Windows-only activities (e.g., activities using Win32 APIs, COM interop, or desktop UI automation)

When documenting:
- Note platform availability on Windows-only activities: "**Platform:** Windows only"
- In the overview, group cross-platform and Windows-only activities separately if the package has both
- The extraction script reads both files automatically

---

## What NOT to Document

- **Abstract base classes** — Not user-facing
- **Internal/private classes** — Not part of public API
- **Designer/ViewModel classes** — Use as metadata sources only
- **`[NotMappedProperty]`** — UI-only, not part of activity contract
- **`[Browsable(false)]`** — Unless functional (e.g., `Body` on scope activities)
- **Telemetry properties** — Internal plumbing
- **Non-browsable metadata entries** — `"browsable": false` in metadata JSON
- **Deprecated activities** — Unless they are the only option (note as deprecated)

---

## Quality Checklist

- [ ] Every activity has a clear one-sentence description
- [ ] ALL configurable properties are listed (InArgument, OutArgument, AND plain properties)
- [ ] **Output properties are always included** with clear type and description
- [ ] Properties come primarily from the ViewModel (following inheritance chain)
- [ ] Properties are grouped by category (Input > Options > Output > Common)
- [ ] `[RequiredArgument]` properties are marked as required
- [ ] Types are accurate (from script extraction)
- [ ] Enum types list their values
- [ ] Default values are included
- [ ] **Valid property configurations are documented** (rules, dependencies, OverloadGroups)
- [ ] XAML example shows a valid property combination
- [ ] Scope requirements are noted (mandatoryParentActivity)
- [ ] Platform noted when Windows-only (from Windows metadata)
- [ ] Project settings are documented when present (`[ArgumentSettingAttribute]`)
- [ ] References section included when supplementary files exist in `{ActivityClassName}/` subdirectory
- [ ] `overview.md` lists all activities with correct relative links
