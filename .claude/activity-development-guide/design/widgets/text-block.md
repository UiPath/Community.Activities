# TextBlockWidget (Read-Only Display)

## When to read this

Read this document when you need to:
- Display read-only text in the activity designer (informational messages, warnings, errors)
- Show status text, instructions, or labels that the user cannot edit
- Use different visual levels (Info, Warning, Error, Experimental)

---

## Overview

The `TextBlockWidget` renders read-only text in the designer. Unlike input widgets, the user cannot edit the displayed value. Use it for informational messages, status indicators, validation summaries, or instructional text.

`TextBlockWidget` is a specialized class (not `DefaultWidget`). Use with `DesignProperty<string>`.

---

## Basic Usage

### Simple text

```csharp
Property.Widget = new TextBlockWidget();
```

The property's `Value` is displayed as plain text.

### Centered text

```csharp
Property.Widget = new TextBlockWidget { Center = true };
```

### Multiline text

```csharp
Property.Widget = new TextBlockWidget { Multiline = true };
```

---

## Visual Levels

Set the `Level` property to change the visual styling (icon, color, background).

### Info

Displays with an info icon and blue styling. Use for helpful context or instructions.

```csharp
Property.Widget = new TextBlockWidget
{
    Multiline = true,
    Level = TextBlockWidgetLevel.Info
};
```

### Warning

Displays with a warning icon and yellow/amber styling. Use for important caveats or non-blocking issues.

```csharp
Property.Widget = new TextBlockWidget
{
    Multiline = true,
    Level = TextBlockWidgetLevel.Warning
};
```

### Error

Displays with an error icon and red styling. Use for validation errors or blocking issues.

```csharp
Property.Widget = new TextBlockWidget
{
    Multiline = true,
    Level = TextBlockWidgetLevel.Error
};
```

### Experimental

Displays with an experimental/beta indicator. Use for features that are in preview or under active development.

```csharp
Property.Widget = new TextBlockWidget
{
    Level = TextBlockWidgetLevel.Experimental
};
```

---

## Configuration Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Center` | `bool` | `false` | Center-aligns the text horizontally. |
| `Multiline` | `bool` | `false` | Allows text to wrap across multiple lines. |
| `Level` | `TextBlockWidgetLevel` | (none) | Visual styling level: `Info`, `Warning`, `Error`, `Experimental`. |

---

## Dynamic Text Updates

Since the displayed text comes from the property's `Value`, you can update it dynamically in rules or event handlers:

```csharp
StatusMessage.Widget = new TextBlockWidget
{
    Multiline = true,
    Level = TextBlockWidgetLevel.Info
};

// Update text dynamically based on another property
Rule("UpdateStatus", () =>
{
    StatusMessage.Value = SelectedOption.Value switch
    {
        "Advanced" => "Advanced mode enables additional configuration options.",
        "Simple" => "Simple mode uses default settings.",
        _ => ""
    };
    StatusMessage.IsVisible = !string.IsNullOrEmpty(StatusMessage.Value);
}, false);

RegisterDependency(SelectedOption, nameof(DesignProperty.Value), "UpdateStatus");
```

---

## Troubleshooting

**Text is truncated (single line).**
Set `Multiline = true` to allow wrapping. Without it, long text is clipped to a single line.

**Level styling not visible.**
Ensure the `Level` property is set to a valid `TextBlockWidgetLevel` enum value. If `Level` is not set, the text block renders without any icon or colored background.

**Text block is visible but empty.**
The displayed text comes from the property's `.Value`. Set the value before or during `InitializeModel`:

```csharp
StatusMessage.Value = "Select an option to continue.";
StatusMessage.Widget = new TextBlockWidget { Level = TextBlockWidgetLevel.Info };
```
