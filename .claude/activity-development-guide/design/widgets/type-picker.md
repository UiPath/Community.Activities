# TypePickerWidget

## When to read this

Read this document when you need to:
- Let the user select a .NET type for a property (e.g., exception type, data type)
- Filter the type picker to show only relevant types
- Provide recommended types for quick selection

---

## Overview

The `TypePickerWidget` opens a type browser dialog where the user selects a .NET type. Unlike most widgets that use `DefaultWidget`, the type picker uses a dedicated `TypePickerWidget` class with strongly-typed configuration properties.

Use with `DesignProperty<Type>`.

---

## Basic Type Picker

The simplest form shows all available types in the referenced assemblies.

```csharp
Property.Widget = new TypePickerWidget { };
```

---

## Filtered Type Picker

Use `RecommendedTypes` and `Filter` to constrain which types are shown.

```csharp
Property.Widget = new TypePickerWidget
{
    RecommendedTypes = new List<Type> { typeof(Exception), typeof(ArgumentException) },
    Filter = t => t == typeof(Exception) || t.IsSubclassOf(typeof(Exception))
};
```

### Configuration Properties

| Property | Type | Description |
|---|---|---|
| `RecommendedTypes` | `List<Type>` | Types shown at the top of the picker for quick selection. These appear as "recommended" before the user browses. |
| `Filter` | `Func<Type, bool>` | Predicate that controls which types appear in the browser. Only types returning `true` are shown. |

---

## Examples

### Exception type picker

Show only `Exception` and its subclasses, with common exception types recommended.

```csharp
ExceptionType.Widget = new TypePickerWidget
{
    RecommendedTypes = new List<Type>
    {
        typeof(Exception),
        typeof(ArgumentException),
        typeof(InvalidOperationException),
        typeof(TimeoutException),
        typeof(System.IO.IOException)
    },
    Filter = t => typeof(Exception).IsAssignableFrom(t)
};
```

### Data type picker for collection element type

Show common data types for a generic collection.

```csharp
ElementType.Widget = new TypePickerWidget
{
    RecommendedTypes = new List<Type>
    {
        typeof(string),
        typeof(int),
        typeof(double),
        typeof(bool),
        typeof(DateTime),
        typeof(System.Data.DataRow)
    }
};
```

### Unrestricted type picker with recommendations

No filter, but provide recommendations for the most common choices.

```csharp
OutputType.Widget = new TypePickerWidget
{
    RecommendedTypes = new List<Type>
    {
        typeof(string),
        typeof(int),
        typeof(bool),
        typeof(System.Data.DataTable),
        typeof(Newtonsoft.Json.Linq.JObject)
    }
};
```

---

## Troubleshooting

**Type picker dialog shows no types.**
If `Filter` is set, verify the predicate returns `true` for at least one type. A filter like `t => t == typeof(MyInternalType)` may return no results if `MyInternalType` is not in the referenced assemblies.

**Recommended types do not appear.**
Ensure the types in `RecommendedTypes` are loadable in the current project context. If a type is from an unreferenced assembly, it may not appear.

**Selected type is null after picker closes.**
The type picker sets the property value when the user confirms selection. If the user cancels the dialog, the property retains its previous value. Ensure your code handles a `null` type value gracefully.
