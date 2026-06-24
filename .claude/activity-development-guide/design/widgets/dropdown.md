# Dropdown Widget

## When to read this

Read this document when you need to:
- Display a fixed list of options as a dropdown for a property
- Configure a `DataSource` for a dropdown widget
- Choose between a dropdown (fixed list) and an autocomplete (searchable list with expressions)

---

## Overview

The `Dropdown` widget renders a static list of options. The user selects one value from the list. It does not support expressions -- use `AutoCompleteForExpression` if the user needs to type expressions or search a large list. See [autocomplete.md](autocomplete.md).

Use `Dropdown` with `DesignProperty<T>`.

---

## Basic Usage

A dropdown requires both a widget type and a `DataSource` that provides the list of options.

```csharp
Property.Widget = new DefaultWidget { Type = "Dropdown" };
Property.DataSource = DataSourceBuilder<string>
    .WithId(s => s)
    .WithLabel(s => s)
    .Build();
Property.DataSource.Data = new[] { "Option1", "Option2", "Option3" };
```

### DataSource Configuration

The `DataSourceBuilder<T>` requires two selectors:

| Selector | Purpose |
|---|---|
| `WithId(T => string)` | Returns the unique identifier for each item. This is the value stored in the property. |
| `WithLabel(T => string)` | Returns the display text shown in the dropdown. |

Optional selectors:

| Selector | Purpose |
|---|---|
| `WithCategory(T => string)` | Groups items under category headers in the dropdown. |
| `WithTooltip(T => string)` | Shows a tooltip when hovering over an item. |
| `WithDescription(T => string)` | Extended description below the item label. |

---

## Examples

### String options (ID equals label)

```csharp
Protocol.Widget = new DefaultWidget { Type = "Dropdown" };
Protocol.DataSource = DataSourceBuilder<string>
    .WithId(s => s)
    .WithLabel(s => s)
    .Build();
Protocol.DataSource.Data = new[] { "IMAP", "POP3", "Exchange" };
```

### Enum options with display names

```csharp
Priority.Widget = new DefaultWidget { Type = "Dropdown" };
Priority.DataSource = DataSourceBuilder<PriorityLevel>
    .WithId(p => p.ToString())
    .WithLabel(p => p switch
    {
        PriorityLevel.Low => "Low Priority",
        PriorityLevel.Normal => "Normal Priority",
        PriorityLevel.High => "High Priority",
        _ => p.ToString()
    })
    .Build();
Priority.DataSource.Data = Enum.GetValues<PriorityLevel>();
```

### Complex object options

```csharp
Server.Widget = new DefaultWidget { Type = "Dropdown" };
Server.DataSource = DataSourceBuilder<ServerConfig>
    .WithId(s => s.Id)
    .WithLabel(s => s.DisplayName)
    .WithCategory(s => s.Region)
    .WithTooltip(s => s.Url)
    .Build();
Server.DataSource.Data = availableServers;
```

---

## Dropdown vs AutoComplete

| Feature | Dropdown | AutoCompleteForExpression |
|---|---|---|
| Expression support | No | Yes |
| Search/filter | No | Yes (typed search) |
| Dynamic data loading | No | Yes (via `IDynamicDataSourceBuilder`) |
| Property type | `DesignProperty<T>` | `DesignInArgument<T>` |
| Best for | Small fixed lists (< 20 items) | Large or dynamic lists |

For data sources and dynamic autocomplete patterns, see [autocomplete.md](autocomplete.md) and [../datasources.md](../datasources.md).

---

## Troubleshooting

**Dropdown appears empty.**
Verify that `DataSource.Data` is set after building the data source. The `Build()` call creates the data source structure; `Data` must be assigned separately with the actual items.

**Selected value does not persist.**
Ensure the `WithId` selector returns a stable, unique string for each item. If the ID changes between sessions (e.g., uses a timestamp or random value), the saved value will not match any item on reload.

**Dropdown shows IDs instead of display names.**
Check that `WithLabel` returns the human-readable name. If `WithLabel` and `WithId` return the same value, the dropdown shows the ID string, which may be a technical identifier.
