# PlainNumber Widget

## When to read this

Read this document when you need to:
- Display a number input with min/max/step constraints
- Use a number input that does NOT support expressions (plain value only)
- Understand when to use `PlainNumber` vs `Number`

---

## Overview

The `PlainNumber` widget provides a constrained numeric input. Unlike the `Number` widget, `PlainNumber` does not allow expressions -- the user enters a literal numeric value. It supports `Min`, `Max`, and `Step` metadata to constrain the valid range and increment.

Use `PlainNumber` with `DesignProperty<int>` or `DesignProperty<double>`.

---

## Basic Usage

```csharp
Property.Widget = new DefaultWidget
{
    Type = ViewModelWidgetType.PlainNumber,
    Metadata = new Dictionary<string, string>
    {
        [PlainNumber.Min] = "-100",
        [PlainNumber.Max] = "100",
        [PlainNumber.Step] = "1"
    }
};
```

### Metadata Keys

| Key | Type | Description |
|---|---|---|
| `PlainNumber.Min` | string (numeric) | Minimum allowed value. User cannot enter below this. |
| `PlainNumber.Max` | string (numeric) | Maximum allowed value. User cannot enter above this. |
| `PlainNumber.Step` | string (numeric) | Increment/decrement step when using spinner controls. |

All metadata values are strings. The framework parses them as numbers internally.

---

## When to Use PlainNumber vs Number

| Scenario | Widget | Property Type |
|---|---|---|
| User needs to enter expressions like `variable + 1` | `Number` | `DesignInArgument<int>` |
| User enters a fixed numeric value, optionally constrained | `PlainNumber` | `DesignProperty<int>` |
| Configuration value with known bounds (e.g., retry count 0-10) | `PlainNumber` | `DesignProperty<int>` |
| Timeout or delay that might use a variable | `Number` | `DesignInArgument<int>` |

Use `PlainNumber` when the value is a fixed configuration parameter with known bounds. Use `Number` when the value might come from a variable or expression.

---

## Examples

### Retry count (0 to 5, step 1)

```csharp
RetryCount.Widget = new DefaultWidget
{
    Type = ViewModelWidgetType.PlainNumber,
    Metadata = new Dictionary<string, string>
    {
        [PlainNumber.Min] = "0",
        [PlainNumber.Max] = "5",
        [PlainNumber.Step] = "1"
    }
};
```

### Confidence threshold (0.0 to 1.0, step 0.05)

```csharp
Confidence.Widget = new DefaultWidget
{
    Type = ViewModelWidgetType.PlainNumber,
    Metadata = new Dictionary<string, string>
    {
        [PlainNumber.Min] = "0",
        [PlainNumber.Max] = "1",
        [PlainNumber.Step] = "0.05"
    }
};
```

### No constraints

All metadata keys are optional. Without constraints, `PlainNumber` is an unrestricted numeric input that does not accept expressions.

```csharp
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.PlainNumber };
```

---

## Troubleshooting

**User can still type values outside Min/Max.**
The Min/Max constraints are enforced by the UI spinner and validation, but some Studio versions may allow typing values outside the range. Add a validator on the property if strict enforcement is required:

```csharp
Property.Validators.Add(new RangeValidator<int>(min, max, "Value must be between {0} and {1}"));
```

**Step value has no visible effect.**
The `Step` metadata controls the increment when the user clicks the up/down spinner arrows. If the property type is `int` but `Step` is `"0.5"`, the fractional part is discarded. Ensure the step value matches the property type's precision.

**PlainNumber shows an expression editor.**
Verify the property is `DesignProperty<T>`, not `DesignInArgument<T>`. The `PlainNumber` widget is designed for direct-value properties.
