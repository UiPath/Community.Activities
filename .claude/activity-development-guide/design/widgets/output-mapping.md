# Output Mapping Widget

## When to read this

Read this document when you need to:
- Add an output field mapping UI where users configure which output fields to save and how to name the variables
- Display a "Configure fields" button that opens a mapping editor

---

## Overview

The `outputmapping` widget provides a UI for mapping output fields to workflow variables. It renders a configuration button that opens an editor where users can see available output fields and assign variable names to each field. This is commonly used for activities that return dynamic or configurable sets of output values (e.g., parsed document fields, API response fields).

Use with `DesignProperty<T>`.

---

## Basic Usage

```csharp
Property.Widget = new DefaultWidget
{
    Type = "outputmapping",
    Metadata = new Dictionary<string, string>
    {
        ["configureButton"] = "Configure fields",
        ["title"] = "Configure data",
        ["description"] = "Fields below will be dynamically processed",
        ["fieldLabel"] = "Field name",
        ["saveAsValueLabel"] = "Save as Variable",
        ["fieldPlaceholder"] = "Value name for {{field}}"
    }
};
```

**Note:** The widget type string is `"outputmapping"` (lowercase), not a `ViewModelWidgetType` constant.

---

## Metadata Keys

| Key | Description |
|---|---|
| `configureButton` | Label text for the button that opens the mapping editor. |
| `title` | Title of the mapping editor dialog/panel. |
| `description` | Descriptive text shown at the top of the editor. |
| `fieldLabel` | Column header for the field names in the mapping table. |
| `saveAsValueLabel` | Column header for the variable name input in the mapping table. |
| `fieldPlaceholder` | Placeholder text in the variable name input. Use `{{field}}` as a token that gets replaced with the actual field name. |

All metadata values are strings. They control the labels and text in the mapping UI.

---

## Example: Document extraction output

```csharp
OutputFields.Widget = new DefaultWidget
{
    Type = "outputmapping",
    Metadata = new Dictionary<string, string>
    {
        ["configureButton"] = "Configure extracted fields",
        ["title"] = "Map Extracted Fields",
        ["description"] = "Select which fields to extract and specify variable names",
        ["fieldLabel"] = "Document field",
        ["saveAsValueLabel"] = "Save to variable",
        ["fieldPlaceholder"] = "Variable for {{field}}"
    }
};
OutputFields.IsPrincipal = true;
```

---

## Troubleshooting

**Configure button does not appear.**
Verify the widget type string is exactly `"outputmapping"` (lowercase). Using `"OutputMapping"` or other casing may not match.

**`{{field}}` placeholder is not replaced.**
The `{{field}}` token in `fieldPlaceholder` is replaced by the framework at render time. If it shows as literal text, ensure you are using double curly braces: `{{field}}`, not `{field}`.

**Mapping data is not persisted.**
Ensure the property is properly bound in the ViewModel and that the activity's corresponding property can serialize the mapping data. The output mapping widget stores its configuration in the property value.
