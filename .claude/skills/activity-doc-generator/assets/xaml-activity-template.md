# XAML Activity Doc Template

This is the template for generating per-activity markdown documentation files for XAML workflows. Replace all `{{placeholders}}` with extracted values.

---

## Template

````markdown
# {{DisplayName}}

`{{FullyQualifiedClassName}}`

{{Description}}

**Package:** `{{PackageId}}`
**Category:** {{Category}}
{{#if MandatoryParent}}
**Required Scope:** `{{MandatoryParent}}`
{{/if}}
{{#if WindowsOnly}}
**Platform:** Windows only
{{/if}}

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
{{#each inputProperties}}
| `{{Name}}` | {{DisplayName}} | {{Kind}} | `{{Type}}` | {{Required}} | {{Default}} | {{Placeholder}} | {{Description}} |
{{/each}}

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
{{#each configProperties}}
| `{{Name}}` | {{DisplayName}} | `{{Type}}` | {{Default}} | {{Description}} |
{{/each}}

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
{{#each outputProperties}}
| `{{Name}}` | {{DisplayName}} | {{Kind}} | `{{Type}}` | {{Description}} |
{{/each}}

{{#if validConfigurations}}
## Valid Configurations

{{validConfigurations}}
{{/if}}

{{#if conditionalProperties}}
### Conditional Properties

These properties appear or change behavior based on other property values (controlled by ViewModel rules and dependencies):

{{#each conditionalProperties}}
- **`{{Name}}`** ({{Type}}) — {{Condition}}. {{Description}}
{{/each}}
{{/if}}

{{#if enumTypes}}
### Enum Reference

{{#each enumTypes}}
**`{{EnumName}}`**: {{#each values}}`{{this}}`{{#unless @last}}, {{/unless}}{{/each}}
{{/each}}
{{/if}}

## XAML Example

```xml
{{XamlExample}}
```

{{#if projectSettings}}
## Project Settings

This activity reads default values from project-level settings (configurable in UiPath Studio under Project Settings). When a property's value is not explicitly set, the project setting is used as the default.

| Property | Setting Key | Default | Description |
|----------|-----------|---------|-------------|
{{#each projectSettings}}
| `{{PropertyName}}` | `UiPath.Sdk.Activities.{{Section}}.{{SettingProperty}}` | `{{Default}}` | {{Description}} |
{{/each}}
{{/if}}

{{#if Notes}}
## Notes

{{Notes}}
{{/if}}

{{#if References}}
## References

{{#each References}}
- [{{DisplayName}}]({{ActivityClassName}}/{{FileName}}) — {{Description}}
{{/each}}
{{/if}}
````

---

## Field Guidelines

### DisplayName
The display name from `ActivitiesMetadata*.json` (`displayNameKey`/`shortName`), resolved via the `.resx` file. This is the source of truth. Falls back to `[LocalizedDisplayName]` attribute if not present in metadata, then to PascalCase → spaced (e.g., `ExtractPDFText` → `Extract PDF Text`).

### FullyQualifiedClassName
Full namespace + class name as it appears in XAML. Example: `UiPath.Excel.Activities.Business.ReadRangeX`.

### Description
One to three sentences from the user's perspective. Sources: `[LocalizedDescription]`, `[Description]`, `descriptionKey` in metadata JSON. If none, write a concise description based on the activity name and its properties.

### MandatoryParent
From `ActivitiesMetadata*.json` `mandatoryParentActivityFullName`. If the activity must be placed inside a scope, note it here. Common examples:
- Excel Business activities → `ExcelApplicationCard`
- Word activities → `WordApplicationScope`
- UIAutomation activities → `NApplicationCard` (Use Application/Browser)

### Properties Table

Properties come primarily from the **ViewModel** (following its inheritance chain). The ViewModel defines `DesignInArgument<T>`, `DesignOutArgument<T>`, and `DesignProperty<T>` with display names, visibility, ordering, categories, and tooltips. The activity class supplements with `[RequiredArgument]`, `[DefaultValue]`, `[OverloadGroup]`, and type information.

The properties table is split into three sections:

#### Input Properties
Properties the user provides to configure what the activity does:

- **Name**: C# property name (e.g., `Range`, `HasHeaders`, `ClickType`)
- **Display Name**: From the ViewModel's `DisplayName` property or `[LocalizedDisplayName]`, resolved via `.resx`. Fall back to property name
- **Kind**: One of:
  - `InArgument` — `InArgument<T>` / `DesignInArgument<T>`, accepts expressions/variables
  - `InOut` — `InOutArgument<T>`
  - `Property` — Plain property (`bool`, `enum`, `string`, `int`) / `DesignProperty<T>`, set directly in designer
- **Type**:
  - For `InArgument<T>`: the `T` type
  - For plain properties: the property type directly
  - For enum types: `EnumName` (list values in Enum Reference section)
- **Required**: `Yes` if `[RequiredArgument]` is present or `IsRequired` is set in the ViewModel, otherwise leave empty
- **Default**: From `[DefaultValue(x)]`, inline initializer (`= value`), or constructor assignment
- **Placeholder**: From the ViewModel's `Placeholder` property (resolved via `.resx`). Shows the expected format to the user (e.g., `"hh:mm:ss"`, `"dd/MM/yyyy"`). Include when present — it helps coding agents provide correctly formatted values and avoids unnecessary errors
- **Description**: From the ViewModel's `Tooltip` property, or `[LocalizedDescription]` on the activity class

#### Configuration Properties
Plain (non-argument) properties that configure activity behavior — booleans, enums, strings. These appear as direct XML attributes in XAML (not wrapped in InArgument expressions).

#### Output Properties
- **Kind**: `OutArgument` or `InOut`
- Same fields as inputs but without Required and Default columns

### Valid Configurations
When an activity has mutually exclusive property groups or mode-dependent properties, document each valid configuration explicitly. This comes from analyzing the ViewModel's rules and dependencies (see below).

### Conditional Properties
Properties whose visibility or applicability depends on other property values. Document the condition clearly. Primary sources (in priority order):
- **ViewModel's `InitializeRules()` and `ManualRegisterDependencies()`** — Rules that toggle `IsVisible` on properties based on other property values. This is the authoritative source for conditional visibility.
- **`[OverloadGroup]` attributes** on the activity class — Mutually exclusive property groups at the WF level
- **ViewModel's `ModelItemOnPropertyChanged`** — Legacy pattern for visibility toggles (older activities)

### Enum Reference
For each enum type used in properties, list all values. Resolve display names from .resx if available.

### XAML Example

A minimal, working XAML fragment showing the activity configured with a **valid property combination**:

- Wrap in required parent scope if applicable
- Use the correct XML namespace prefix (discover from existing XAML or derive from assembly namespace)
- Show `DisplayName` attribute
- Set ALL required properties with descriptive placeholder values
- Show 2-3 commonly-configured optional properties
- Show plain properties (bool, enum) as direct XML attributes: `HasHeaders="True"`, `ClickType="Single"`
- Show InArgument properties in VB expression syntax: `Range="[Excel.Sheet(&quot;Sheet1&quot;)]"`
- Omit properties that use their default values
- **Do not combine mutually exclusive properties** — if the activity has multiple modes (from rules/OverloadGroups), pick one valid configuration per example. If there are multiple important modes, show separate examples.

### Project Settings

Some activities use `[ArgumentSettingAttribute]` to declare properties as project-level settings. These are configurable in UiPath Studio under Project Settings and apply to all instances of the activity in a project. The setting key follows the format `UiPath.Sdk.Activities.{Section}.{Property}`.

Include this section only when the extraction script reports `isProjectSetting: true` for one or more properties.

### Notes

Optional section for:
- Scope requirements (e.g., "Must be placed inside an ExcelApplicationCard")
- Mutually exclusive property groups (`[OverloadGroup]`)
- Known constraints (e.g., "Only works on Windows", "Password and SecurePassword cannot both be set")
- Related activities
- Platform requirements (Windows-only vs cross-platform)
- Deprecation notices

### References

Optional section for supplementary reference files that provide additional context beyond the main activity doc. Use when an activity has complex behavior, detailed examples, or extended guidance that would bloat the main doc.

Reference files live in a subdirectory named after the activity class, alongside the activity's `.md` file:

```
activities/
  WebHttpRequest.md
  WebHttpRequest/
    authentication-examples.md
    error-handling.md
```

Each reference entry has:
- **DisplayName**: A human-readable title for the reference
- **FileName**: The file name within the `{ActivityClassName}/` subdirectory
- **Description**: A one-line summary of what the reference covers

Only include this section when supplementary files exist. Do not create empty reference directories.

---

## Overview Template

The package-level `overview.md` follows this structure:

````markdown
# {{PackageDisplayName}}

`{{PackageId}}` v{{Version}}

{{PackageDescription}}

## Documentation

- [XAML Activities Reference](activities/) — Per-activity documentation for XAML workflows
{{#if hasCodedApi}}
- [Coded Workflow API Reference](coded/coded-api.md) — Service API for coded C# workflows
{{/if}}

## Activities

### {{CategoryName}}

| Activity | Description |
|----------|-------------|
{{#each activities}}
| [{{DisplayName}}](activities/{{FileName}}) | {{BriefDescription}} |
{{/each}}

### {{AnotherCategory}}

| Activity | Description |
|----------|-------------|
...
````

Group activities by their `[LocalizedCategory]` at class level, or by folder structure if no category is set. Link to each activity's individual doc file.
