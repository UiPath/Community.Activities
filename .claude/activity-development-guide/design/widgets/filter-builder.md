# FilterBuilder Widget

> **When to read this:** You are building an activity that needs a visual filter/query builder UI where users construct conditions with criteria dropdowns, operator dropdowns, and typed value inputs connected by logical operators (AND/OR). Examples: email filtering, database record filtering, queue item filtering.

**Cross-references:**
- [ViewModel fundamentals](../design/viewmodel.md) — DesignProperty, Widget assignment
- [DataSource patterns](../design/datasources.md) — dropdown data sources (used within filter criteria)
- [Activity code and CacheMetadata](../runtime/activity-code.md) — `metadata.Bind()` and `metadata.AddArgument()`
- [Advanced patterns](./patterns.md) — localized enums, rule-driven properties

---

## Architecture Overview

The FilterBuilder requires several components working together:

| Component | Purpose |
|-----------|---------|
| **Criteria enum** | Defines the filterable fields (e.g., Sender, Subject, DateReceived) |
| **Operator enum** | Defines comparison operators (e.g., Contains, Equals, NewerThan) |
| **Logical operator enum** | AND/OR between conditions |
| **Filter collection class** | Runtime model holding the filter conditions with `InArgument<T>` values |
| **Filter argument class** | Individual condition with criteria, operator, and typed value |
| **FilterBuilder class** | ViewModel helper that configures `GenericFilterWidgetBuilder<T>` and provides a converter |

The data flow is:

```
User configures conditions in UI
        |
        v
GenericFilterWidgetBuilder captures DesignFilterBase
        |
        v
Converter function transforms DesignFilterBase -> MyFilterCollection
        |
        v
MyFilterCollection serialized into .xaml workflow file
        |
        v (at runtime)
Activity.CacheMetadata() registers all InArgument<T> values
        |
        v
Activity.Execute() resolves filter values via context
```

---

## Step 1: Define the Enums

Use `[LocalizedDisplayName]` for Studio display names.

```csharp
// Filters/MyCriteria.cs
public enum MyCriteria
{
    [LocalizedDisplayName(nameof(Resources.Filter_Name))]
    Name,
    [LocalizedDisplayName(nameof(Resources.Filter_Date))]
    Date,
    [LocalizedDisplayName(nameof(Resources.Filter_IsActive))]
    IsActive,
}

// Filters/MyOperator.cs
public enum MyOperator
{
    [LocalizedDisplayName(nameof(Resources.FilterOp_Contains))]
    Contains,
    [LocalizedDisplayName(nameof(Resources.FilterOp_Equals))]
    Equals,
    [LocalizedDisplayName(nameof(Resources.FilterOp_NotEquals))]
    NotEquals,
    [LocalizedDisplayName(nameof(Resources.FilterOp_NewerThan))]
    NewerThan,
    [LocalizedDisplayName(nameof(Resources.FilterOp_OlderThan))]
    OlderThan,
    [LocalizedDisplayName(nameof(Resources.FilterOp_IsTrue))]
    IsTrue,
    [LocalizedDisplayName(nameof(Resources.FilterOp_IsFalse))]
    IsFalse,
}

// Filters/MyLogicalOperator.cs
public enum MyLogicalOperator
{
    [LocalizedDisplayName(nameof(Resources.Filter_And))]
    And,
    [LocalizedDisplayName(nameof(Resources.Filter_Or))]
    Or
}
```

---

## Step 2: Define the Runtime Filter Model

Each condition holds `InArgument<T>` values so users can use expressions (e.g., `DateTime.Now.AddDays(-7)`).

**Critical: Why filter values must be `InArgument<T>`**

`InArgument<T>` values are **not automatically tracked** by the workflow engine. You must explicitly register them in `CacheMetadata` with **both** `metadata.Bind()` **and** `metadata.AddArgument()`. Without this, the workflow engine cannot evaluate the expressions and the values will be `null` at runtime.

```csharp
// Filters/MyFilterArgument.cs
public class MyFilterArgument
{
    public MyCriteria Criteria { get; set; }
    public MyOperator Operator { get; set; }
    public InArgument<string> Value { get; set; }
    public InArgument<DateTime> DateValue { get; set; }
    public InArgument<bool> BoolValue { get; set; }

    internal ResolvedValue Resolve(ActivityContext context) => new()
    {
        Criteria = Criteria,
        Operator = Operator,
        StringValue = Value?.Get(context),
        DateValue = DateValue?.Get(context),
        BoolValue = BoolValue?.Get(context),
    };

    internal class ResolvedValue
    {
        public MyCriteria Criteria { get; set; }
        public MyOperator Operator { get; set; }
        public string StringValue { get; set; }
        public DateTime? DateValue { get; set; }
        public bool? BoolValue { get; set; }
    }
}

// Filters/MyFilterCollection.cs
public class MyFilterCollection
{
    public MyLogicalOperator LogicalOperator { get; set; }
    public List<MyFilterArgument> Conditions { get; } = new();

    /// <summary>
    /// Register all InArgument values in the activity metadata so the
    /// workflow engine tracks them. Each argument must have a UNIQUE name
    /// and must be BOUND to its RuntimeArgument — otherwise the workflow
    /// engine cannot evaluate expressions and values will be null at runtime.
    /// </summary>
    public void RegisterArguments(CodeActivityMetadata metadata)
    {
        var args = Conditions
            .SelectMany(c => new Argument[] { c.Value, c.DateValue, c.BoolValue })
            .Where(a => a != null)
            .ToList();

        for (int i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            // Each argument MUST have a unique name
            var runtimeArg = new RuntimeArgument(
                $"FilterArg{i}", arg.ArgumentType, ArgumentDirection.In);
            // CRITICAL: Bind links the InArgument to its RuntimeArgument.
            // Without Bind(), the workflow engine won't evaluate the expression
            // and .Get(context) will return null/default at runtime.
            metadata.Bind(arg, runtimeArg);
            metadata.AddArgument(runtimeArg);
        }
    }
}
```

**Common mistake**: calling only `metadata.AddArgument()` without `metadata.Bind()`. The `AddArgument` call declares the slot; the `Bind` call links your `InArgument<T>` instance to that slot. Both are required. Also, each `RuntimeArgument` must have a **unique name** -- using the same name for all arguments silently overwrites them.

---

## Step 3: Create the FilterBuilder Helper

This configures the `GenericFilterWidgetBuilder<T>` with criteria, operators, value types, and a converter function.

```csharp
// ViewModels/Filters/MyFilterBuilder.cs
using System.Activities.ViewModels.Interfaces;

internal class MyFilterBuilder
{
    private readonly GenericFilterWidgetBuilder<MyCriteria> _builder;
    private readonly DesignProperty<MyFilterCollection> _filter;

    public MyFilterBuilder(DesignProperty<MyFilterCollection> filter)
    {
        _filter = filter;

        _builder = new GenericFilterWidgetBuilder<MyCriteria>()
            // 1. Configure logical operator (AND/OR toggle)
            .AddLogicalOperator<MyLogicalOperator>(
                _filter.Value?.LogicalOperator ?? MyLogicalOperator.And,
                s => ActivitiesEnumExtensions.GetLocalizedDisplayName(s),  // display label
                s => s.ToString(),                                          // ID
                Enum.GetValues<MyLogicalOperator>().ToList())
            // 2. Configure criteria dropdown data source
            .SetCriteriaDataSourceBuilder(
                f => ActivitiesEnumExtensions.GetLocalizedDisplayName(f),
                f => f.ToString());

        // 3. Configure each criteria with its operators and value type
        ConfigureAllCriteria();

        // 4. Set the converter that transforms design-time model -> runtime model
        _builder.AddConverter(ConvertToFilterCollection);

        // 5. Restore any existing conditions (when reopening the designer)
        AddExistingConditions();
    }

    public IWidget Build() => _builder.Build();

    private void ConfigureAllCriteria()
    {
        // Text criteria -- string value
        _builder.AddCriteria(MyCriteria.Name)
            .WithOperators<MyOperator>(
                s => ActivitiesEnumExtensions.GetLocalizedDisplayName(s),
                s => s.ToString(),
                new List<MyOperator> { MyOperator.Contains, MyOperator.Equals, MyOperator.NotEquals })
            .AndArgument<string>();

        // Date criteria -- DateTime value
        _builder.AddCriteria(MyCriteria.Date)
            .WithOperators<MyOperator>(
                s => ActivitiesEnumExtensions.GetLocalizedDisplayName(s),
                s => s.ToString(),
                new List<MyOperator> { MyOperator.NewerThan, MyOperator.OlderThan })
            .AndArgument<DateTime>();

        // Boolean criteria -- some operators need no value, others need bool
        _builder.AddCriteria(MyCriteria.IsActive)
            .WithOperators<MyOperator>(
                s => ActivitiesEnumExtensions.GetLocalizedDisplayName(s),
                s => s.ToString(),
                new List<MyOperator> { MyOperator.IsTrue, MyOperator.IsFalse, MyOperator.Equals })
            .ConfigureOperator(MyOperator.IsTrue).WithNoValue()
            .AndOperator(MyOperator.IsFalse).WithNoValue()
            .AndOperator(MyOperator.Equals).WithArgument<bool>();
    }

    private void AddExistingConditions()
    {
        if (_filter.Value == null) return;
        foreach (var c in _filter.Value.Conditions)
        {
            if (c.Criteria == MyCriteria.Date)
                _builder.AddCondition(c.Criteria, c.Operator, c.DateValue);
            else if (c.Criteria == MyCriteria.IsActive)
                _builder.AddCondition(c.Criteria, c.Operator, c.BoolValue);
            else if (c.Criteria == MyCriteria.Category)
            {
                // IMPORTANT: Dropdown criteria -- AddCondition expects plain string
                // to match against the dropdown items list. The converter wrapped
                // the plain value in InArgument<string>, so unwrap it here.
                var plainValue = (c.Value?.Expression as Literal<string>)?.Value;
                _builder.AddCondition(c.Criteria, c.Operator, plainValue);
            }
            else
                _builder.AddCondition(c.Criteria, c.Operator, c.Value);
        }
    }

    private object ConvertToFilterCollection(DesignFilterBase df)
    {
        var result = new MyFilterCollection
        {
            LogicalOperator = (MyLogicalOperator)df.LogicalOperator,
        };
        foreach (var condition in df.Collection.Conditions)
        {
            var criteria = (MyCriteria)condition.Criteria;
            var arg = new MyFilterArgument
            {
                Criteria = criteria,
                Operator = (MyOperator)condition.Operator,
            };
            if (criteria == MyCriteria.Date)
                arg.DateValue = (InArgument<DateTime>)condition.Value;
            else if (criteria == MyCriteria.IsActive)
                arg.BoolValue = (InArgument<bool>)condition.Value;
            else if (criteria == MyCriteria.Category)
            {
                // WithDropDown gives plain string -- wrap for runtime storage
                if (condition.Value is string plainStr)
                    arg.Value = new InArgument<string>(plainStr);
                else
                    arg.Value = (InArgument<string>)condition.Value;
            }
            else
                arg.Value = (InArgument<string>)condition.Value;

            result.Conditions.Add(arg);
        }
        return result;
    }
}
```

---

## Step 4: Use in the ViewModel

```csharp
public DesignProperty<MyFilterCollection> Filter { get; set; } = new();

protected override void InitializeModel()
{
    base.InitializeModel();
    Filter.IsPrincipal = true;
    Filter.OrderIndex = PropertyOrderIndex++;
    Filter.Widget = new MyFilterBuilder(Filter).Build();
}
```

---

## Step 5: Use in the Activity

The filter property is a **direct type** (not `InArgument<MyFilterCollection>`). The collection itself contains `InArgument<T>` values that must be registered in `CacheMetadata`.

```csharp
// IMPORTANT: The filter property is a direct type, NOT InArgument<MyFilterCollection>.
// The collection is serialized as-is into the .xaml workflow. The InArgument<T> values
// INSIDE the collection are what need registration via CacheMetadata.
[DefaultValue(null)]
public MyFilterCollection Filter { get; set; }

protected override void CacheMetadata(CodeActivityMetadata metadata)
{
    base.CacheMetadata(metadata);
    // CRITICAL: Without this call, filter InArgument expressions won't be
    // evaluated at runtime -- .Get(context) will return null/default.
    Filter?.RegisterArguments(metadata);
}

protected override void Execute(CodeActivityContext context)
{
    var conditions = Filter?.Conditions?.Select(c => c.Resolve(context))
        ?? Enumerable.Empty<MyFilterArgument.ResolvedValue>();

    foreach (var c in conditions)
    {
        // c.Criteria, c.Operator, c.StringValue / c.DateValue / c.BoolValue
    }
}
```

---

## How Filter Persistence Works End-to-End

```
Design-Time (Studio)                              Runtime (Robot)
---------------------                              ------------------
1. User configures filter                         5. Activity.CacheMetadata() called
   conditions in widget UI                           -> Filter.RegisterArguments(metadata)
         |                                           -> metadata.Bind(inArg, runtimeArg)
         v                                           -> metadata.AddArgument(runtimeArg)
2. GenericFilterWidgetBuilder                              |
   captures DesignFilterBase                               v
         |                                        6. Activity.Execute() called
         v                                           -> condition.Value.Get(context)
3. Converter function called                            returns evaluated expression
   -> casts condition.Value                                  |
     to InArgument<T>                                        v
   -> builds MyFilterCollection                    7. Business logic processes
   -> sets on DesignProperty                          resolved filter values
         |
         v
4. MyFilterCollection serialized
   into .xaml workflow file
   (InArgument<T> values preserved
   as expression trees)
```

---

## Debugging Null Filter Values (5-Point Checklist)

If filter values are `null` at runtime, check these causes in order:

1. **`RegisterArguments()` not called** in `CacheMetadata` -- most common cause.
2. **`metadata.Bind()` missing** -- `AddArgument` alone is not enough.
3. **Duplicate argument names** -- each `RuntimeArgument` must have a unique name.
4. **Converter not casting to `InArgument<T>`** -- `condition.Value` must be cast to the correct `InArgument<T>` type in the converter function.
5. **Dropdown appears empty on reopen** -- `AddExistingConditions` passes `InArgument<T>` to `AddCondition` for a dropdown criterion. Unwrap with `(c.Value?.Expression as Literal<string>)?.Value` first.

---

## Understanding condition.Value Types in the Converter

The type of `condition.Value` in the converter function depends on how the operator was configured. This is a common source of bugs -- if you always cast to `InArgument<T>`, dropdown values will fail with an `InvalidCastException` or silently return `null`.

| Widget Configuration | `condition.Value` Type | Converter Pattern |
|---|---|---|
| `AndArgument<T>()` / `WithArgument<T>()` | `InArgument<T>` | `(InArgument<T>)condition.Value` |
| `WithDropDown<T>(...)` | plain `T` | `(T)condition.Value` or `condition.Value as T` |
| `WithDropDown<InArgument<T>>(...)` | `InArgument<T>` | `(InArgument<T>)condition.Value` |
| `WithMultiSelectDropDown<T>(...)` | `T[]` / `IEnumerable<T>` | Cast to collection or use `FilterHelper.GetCollectionOf<T>()` |
| `WithLiteral<T>(...)` | plain `T` | `(T)condition.Value` |
| `WithNoValue()` | `null` | Do not access `condition.Value` |

---

## Mixing Dropdowns and Arguments

When mixing dropdowns and arguments for the same criteria, check for the plain type first, then fall back to `InArgument<T>`:

```csharp
// Handle both plain dropdown values and InArgument expression values
if (condition.Value is string plainStr)
    arg.Value = new InArgument<string>(plainStr);        // Wrap plain value
else if (condition.Value is InArgument<string> strArg)
    arg.Value = strArg;                                   // Already InArgument

// Same pattern for other types
if (condition.Value is DateTime plainDate)
    arg.DateValue = new InArgument<DateTime>(plainDate);
else if (condition.Value is InArgument<DateTime> dateArg)
    arg.DateValue = dateArg;
```

Alternatively, if a dropdown criterion should always produce an `InArgument<T>`, use `WithDropDown<InArgument<T>>()` instead of `WithDropDown<T>()` -- then `condition.Value` is already an `InArgument<T>` and no wrapping is needed.

---

## Round-Trip: Dropdown Values Must Be Unwrapped in AddExistingConditions

Dropdown criteria have a round-trip mismatch:

1. **Design-time**: `WithDropDown<T>()` gives a **plain value** (e.g., `"Error"`)
2. **Converter**: wraps it in `new InArgument<string>("Error")` for runtime storage
3. **Restore (`AddExistingConditions`)**: the saved model now has `InArgument<string>`, but `AddCondition` for a dropdown expects the **plain value** to match against the dropdown items list

If you pass `InArgument<string>` to `AddCondition` for a dropdown criterion, the dropdown appears empty because the object does not match any item in the list.

**Fix**: unwrap the literal value before passing to `AddCondition`:

```csharp
// Dropdown criterion -- extract plain value for AddCondition to match
var plainValue = (c.Value?.Expression as Literal<string>)?.Value;
_builder.AddCondition(c.Criteria, c.Operator, plainValue);
```

**Alternative (avoid the problem entirely)**: Use `WithDropDown<InArgument<T>>()` instead of `WithDropDown<T>()`. Then the dropdown items are `InArgument<T>` objects, `condition.Value` is already `InArgument<T>` in the converter, and `AddCondition` receives `InArgument<T>` -- no wrapping or unwrapping needed. This is the pattern used by SharePoint's non-trigger filters.

---

## Advanced: Dropdown and Multi-Select Values

For criteria where the value should be a dropdown:

```csharp
_builder.AddCriteria(MyCriteria.Category)
    .WithOperators<MyOperator>(getLabel, getId, operators)
    .ConfigureOperator(MyOperator.Equals)
        .WithDropDown(
            getLabel: c => c,           // display name
            getId: c => c,              // value
            data: new List<string> { "TypeA", "TypeB", "TypeC" },
            comparer: StringComparer.OrdinalIgnoreCase)
    .Build();

// Multi-select dropdown
_builder.AddCriteria(MyCriteria.Labels)
    .WithOperators<MyOperator>(getLabel, getId, operators)
    .ConfigureOperator(MyOperator.Contains)
        .WithMultiSelectDropDown<string>(
            getLabel: l => l,
            getId: l => l,
            data: availableLabels)
    .Build();
```

---

## Troubleshooting

<!-- AI agents: add entries here when you encounter issues related to this topic -->
