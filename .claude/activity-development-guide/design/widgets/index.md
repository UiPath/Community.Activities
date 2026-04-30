# Widget Reference

## When to read this

Read this document when you need to:
- Choose which widget to use for an activity property
- Configure a widget with metadata options
- Look up the correct `ViewModelWidgetType` constant for a property type
- Understand how widgets map to property types (`DesignProperty<T>` vs `DesignInArgument<T>`)

For the FilterBuilder widget (complex filter conditions UI), see [filter-builder.md](filter-builder.md).

---

## What is a Widget?

A **widget** is the UI control that renders and edits an activity property in the Studio designer. The framework selects a default widget based on the property type (e.g., a text box for strings, an expression editor for `InArgument<T>`), but you can override this to use a more specific control.

For example, a `bool` property can render as a checkbox (default) or a toggle switch (`ViewModelWidgetType.Toggle`). A `string` property can be a text box (default), a dropdown, an autocomplete with search, or a rich text editor.

---

## Setting a Widget

Every `DesignProperty` or `DesignInArgument` has a `.Widget` property. Assign a `DefaultWidget` (or a specialized widget class) to override the default control.

```csharp
// Basic widget assignment
property.Widget = new DefaultWidget { Type = ViewModelWidgetType.WidgetName };

// Widget with metadata
property.Widget = new DefaultWidget
{
    Type = ViewModelWidgetType.WidgetName,
    Metadata = new Dictionary<string, string>
    {
        { "key", "value" }
    }
};
```

Some widgets use specialized classes instead of `DefaultWidget`:
- `TypePickerWidget` for type pickers
- `TextBlockWidget` for read-only text display
- `SolutionResourcesWidget` for Solutions resource pickers

---

## Quick Reference Table

| Widget Type | Property Type | Use Case | Details |
|---|---|---|---|
| *(default)* | `DesignInArgument<string>` | Expression editor | Simple (below) |
| `TextComposer` | `DesignInArgument<string>` | Rich text / multiline input | [text-input.md](text-input.md) |
| `RichTextComposer` | `DesignInArgument<string>` | HTML / rich text editor | [text-input.md](text-input.md) |
| `PromptComposer` | `DesignInArgument<string>` | AI prompt with @ variables | [text-input.md](text-input.md) |
| `Number` | `DesignInArgument<int/double>` | Number with expression support | Simple (below) |
| `PlainNumber` | `DesignProperty<int/double>` | Constrained number (no expressions) | [plain-number.md](plain-number.md) |
| `Toggle` | `DesignProperty<bool>` | Boolean switch | Simple (below) |
| `NullableBoolean` | `DesignProperty<bool?>` | True / False / Null | Simple (below) |
| `Date` | `DesignInArgument<DateTime>` | Date picker | Simple (below) |
| `Time` | `DesignInArgument<DateTime>` | Time picker | Simple (below) |
| `DateTime` | `DesignInArgument<DateTime>` | Date + time picker | Simple (below) |
| `TimeSpan` | `DesignInArgument<TimeSpan>` | Duration picker | Simple (below) |
| `Dropdown` | `DesignProperty<T>` | Fixed options dropdown | [dropdown.md](dropdown.md) |
| `AutoCompleteForExpression` | `DesignInArgument<T>` | Searchable dropdown + expression | [autocomplete.md](autocomplete.md) |
| `Collection` | `DesignProperty<IEnumerable<T>>` | Collection editor | Simple (below) |
| `Dictionary` | `DesignProperty<Dictionary<K,V>>` | Key-value editor | Simple (below) |
| `RawStringArray` | `DesignProperty<string[]>` | String array editor | Simple (below) |
| `MultiSelect` | `DesignProperty<T>` | Multi-select dropdown (Flags enums) | Simple (below) |
| `AddActivityWidget` | `DesignProperty<T>` | Activity picker for containers | Simple (below) |
| `Variable` | `DesignInArgument<T>` | Variable picker | Simple (below) |
| `TypePicker` / `TypePickerWidget` | `DesignProperty<Type>` | .NET type selector | [type-picker.md](type-picker.md) |
| `Connection` | `DesignProperty<string>` | Integration Service connection | [connection.md](connection.md) |
| `ActionButton` | `DesignProperty<T>` | Clickable button | [action-button.md](action-button.md) |
| `TextBlockWidget` | `DesignProperty<string>` | Read-only text display | [text-block.md](text-block.md) |
| `Text` | `DesignProperty<string>` | Plain text with metadata (e.g., `"multiline": "true"`) | Simple (below) |
| `Input` | `DesignInArgument<T>` | Basic input widget | Simple (below) |
| `Container` | N/A | Container / group widget | Simple (below) |
| `AppsExpression` | `DesignInArgument<T>` | UiPath Apps expression editor | Simple (below) |
| `FilterWidgetBuilder` | `DesignProperty<T>` | Complex filter configuration | [filter-builder.md](filter-builder.md) |
| `outputmapping` | `DesignProperty<T>` | Output field mapping UI | [output-mapping.md](output-mapping.md) |
| `SolutionResourcesWidget` | `DesignProperty<T>` | Solutions resource picker | [solution-resources.md](solution-resources.md) |

---

## Simple Widgets

These widgets require minimal configuration -- just set the `Type` and optionally some metadata.

### Toggle

Boolean on/off switch. Use for `DesignProperty<bool>`.

```csharp
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };
```

### NullableBoolean

Three-state control: True, False, or Null. Use for `DesignProperty<bool?>`.

```csharp
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.NullableBoolean };
```

### Number

Number input with expression support. Use for `DesignInArgument<int>` or `DesignInArgument<double>`. For constrained numbers without expression support, see [plain-number.md](plain-number.md).

```csharp
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.Number };
```

### Date, Time, DateTime, TimeSpan

Date and time picker widgets. Each variant exposes a different picker UI.

```csharp
// Date only
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.Date };

// Time only
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.Time };

// Date + Time
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.DateTime };

// TimeSpan (duration)
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.TimeSpan };

// DateTime with invariant offset (UTC-safe)
Property.Widget = new DefaultWidget
{
    Type = ViewModelWidgetType.DateTime,
    Metadata = new()
    {
        { WidgetMetadataConstants.DateTime.OffsetSetting,
          WidgetMetadataConstants.DateTime.OffsetInvariantSetting }
    }
};
```

### Variable

Variable picker. Lets the user select from existing workflow variables.

```csharp
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.Variable };
```

### Collection, Dictionary, RawStringArray

Editors for collection-typed properties.

```csharp
// Collection editor
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.Collection };

// Dictionary editor (key-value pairs)
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dictionary };

// String array editor
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.RawStringArray };
```

### Container

Container/group widget. Used to visually group related properties.

```csharp
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.Container };
```

### MultiSelect

Multi-select dropdown. Use for Flags enums or multi-select data sources.

```csharp
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.MultiSelect };
```

### AddActivityWidget

Activity picker for container activities. Lets users pick an activity type to add to a container body.

```csharp
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.AddActivityWidget };
// Set the container property name the activity will be added to:
Property.Value = nameof(ActivityClass.Body);
```

### Text

Plain text widget with optional metadata. For read-only display, prefer `TextBlockWidget` (see [text-block.md](text-block.md)).

```csharp
// Plain text
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.Text };

// Multiline text
Property.Widget = new DefaultWidget
{
    Type = ViewModelWidgetType.Text,
    Metadata = new() { { "multiline", "true" } }
};
```

### Input

Basic input widget. This is the standard expression-capable input.

```csharp
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };
```

### AppsExpression

Expression editor for UiPath Apps context. Used when the activity runs inside a UiPath App.

```csharp
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.AppsExpression };
```
