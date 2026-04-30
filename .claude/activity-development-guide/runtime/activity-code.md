# Activity Code (Runtime Implementation)

> **When to read this**: You are implementing the runtime logic of a UiPath activity -- choosing a base class, defining properties, handling defaults, or adding logging. This covers everything inside the `Execute` method and the class structure around it.

**Related files**:
- [Platform API](../runtime/platform-api.md) -- runtime services (logging, settings, OAuth)
- [Orchestrator Integration](../runtime/orchestrator.md) -- calling Orchestrator APIs from activities

---

## Base Class Options

| Base Class | When to Use |
|---|---|
| `CodeActivity<T>` | Simple synchronous activities returning a value |
| `CodeActivity` | Simple synchronous activities with no return value |
| `AsyncCodeActivity<T>` | Async activities (I/O, network calls) |
| `SdkActivity<T>` | Activities using the SDK framework (DI, telemetry, bindings) |
| `SdkNativeActivity<T>` | Complex activities needing retry, bookmarks, or child activities |

**Decision guide**:
- No async work, no DI needed --> `CodeActivity` or `CodeActivity<T>`
- Async I/O but no SDK features --> `AsyncCodeActivity<T>`
- Need DI, telemetry, or project settings bindings --> `SdkActivity<T>`
- Need retry logic, bookmarks, or scheduling child activities --> `SdkNativeActivity<T>`

---

## Simple Activity (CodeActivity)

```csharp
using System.Activities;
using System.ComponentModel;

namespace MyCompany.MyActivities;

public class Calculator : CodeActivity<int>
{
    [RequiredArgument]
    public InArgument<int> FirstNumber { get; set; }

    [RequiredArgument]
    public InArgument<int> SecondNumber { get; set; }

    [RequiredArgument]
    public Operation SelectedOperation { get; set; } = Operation.Add;

    protected override int Execute(CodeActivityContext context)
    {
        var a = FirstNumber.Get(context);
        var b = SecondNumber.Get(context);
        return ExecuteInternal(a, b);
    }

    // Separate business logic for testability
    public int ExecuteInternal(int a, int b)
    {
        return SelectedOperation switch
        {
            Operation.Add => a + b,
            Operation.Subtract => a - b,
            Operation.Multiply => a * b,
            Operation.Divide => a / b,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}

public enum Operation { Add, Subtract, Multiply, Divide }
```

Key points:
- `CodeActivity<int>` means the activity returns an `int` via its `Result` OutArgument.
- `Execute` receives a `CodeActivityContext` used to resolve argument values.
- Extract business logic into a separate method (`ExecuteInternal`) for unit testing without a workflow context.

---

## SDK Activity (SdkActivity with DI)

Use when the activity needs dependency injection, telemetry, bindings, or project settings.

```csharp
using UiPath.Sdk.Activities;

// Link activity to its ViewModel
[ViewModelClass(typeof(DepositInAccountViewModel))]
public partial class DepositInAccount : SdkActivity<decimal>
{
    [RequiredArgument]
    public InArgument<string> AccountHolder { get; set; }

    [RequiredArgument]
    public InArgument<decimal> Amount { get; set; }

    [ArgumentSetting("DepositInAccount", "Currency")]
    public InArgument<string> Currency { get; set; }

    protected override async Task<decimal> ExecuteAsync(
        AsyncCodeActivityContext context,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var bank = serviceProvider.GetRequiredService<IBank>();
        var holder = AccountHolder.Get(context);
        var amount = Amount.Get(context);

        return await bank.DepositAsync(holder, amount, cancellationToken);
    }
}
```

Key points:
- `[ViewModelClass]` links the activity to its design-time ViewModel.
- `[ArgumentSetting]` binds a property to a project setting (auto-populated from Studio settings UI).
- `serviceProvider.GetRequiredService<T>()` resolves DI-registered services.
- `ExecuteAsync` provides a `CancellationToken` for cooperative cancellation.

---

## SDK Activity with Retry (SdkNativeActivity)

```csharp
public class RetryableActivity : SdkNativeActivity<int>
{
    public InArgument<int> MaxRetries { get; set; } = new(3);

    protected override async Task<int> ExecuteAsync(
        NativeActivityContext context,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        // Execution logic
        return await DoWorkAsync(cancellationToken);
    }

    protected override bool ShouldRetry(
        NativeActivityContext context, object executionValue, int retryCount)
    {
        return retryCount < MaxRetries.Get(context);
    }

    protected override void PrepareForRetry(NativeActivityContext context, int retryCount)
    {
        // Reset state before retry
    }
}
```

Key points:
- Override `ShouldRetry` to control retry behavior based on execution result and retry count.
- Override `PrepareForRetry` to reset any internal state between attempts.
- The framework handles the retry loop; you supply the decision logic.

---

## Activity Property Types

| C# Type | Purpose | Designer Behavior |
|---|---|---|
| `InArgument<T>` | Input -- accepts expressions/variables | Expression editor |
| `OutArgument<T>` | Output -- writes to variables | Variable picker |
| `InOutArgument<T>` | Bidirectional -- read and modify | Expression editor |
| `T` (direct type) | Constant/enum value | Type-specific editor (dropdown, checkbox, etc.) |

---

## Enum Selector Properties

Properties that select behavior via an enum (operation mode, algorithm, format) must be declared as **plain `TEnum` properties**, never as `InArgument<TEnum>`:

```csharp
// CORRECT — plain property, maps to DesignProperty<TEnum> in ViewModel
public Operation SelectedOperation { get; set; } = Operation.Add;

// WRONG — InArgument wrapper mismatches DesignProperty binding
public InArgument<Operation> SelectedOperation { get; set; }
```

**Why**: The ViewModel binds enum selectors via `DesignProperty<TEnum>`, which maps to a plain property. `InArgument<TEnum>` mismatches this binding and changes how the value is retrieved in `Execute`.

The enum runtime auto-renders as a dropdown in the designer — no explicit `DataSource` setup is needed for simple cases. Only add `EnumDataSourceBuilder` when you need custom labels, custom ordering, or value conversion.

---

## ExecuteInternal Pattern

Extract business logic into a public `ExecuteInternal()` method for testability. Key rules:

1. **Read enum properties directly from the public property** — never capture into a private field in `Execute` and never pass the enum as a parameter:

```csharp
// CORRECT — reads public property directly, testable
public int ExecuteInternal(int a, int b)
{
    return SelectedOperation switch
    {
        Operation.Add => a + b,
        Operation.Subtract => a - b,
        _ => throw new NotSupportedException($"Operation '{SelectedOperation}' is not supported.")
    };
}

// WRONG — private field only gets set during Execute, breaks direct test calls
private Operation _op;
protected override int Execute(CodeActivityContext context)
{
    _op = SelectedOperation; // field stays default(Operation) when tests call ExecuteInternal directly
    return ExecuteInternal(FirstNumber.Get(context), SecondNumber.Get(context));
}
```

2. **Don't pass enum as a parameter** — it changes the public API contract and breaks tests written against the expected signature:

```csharp
// WRONG — enum as parameter
public int ExecuteInternal(int a, int b, Operation op) { ... }

// CORRECT — reads from public property
public int ExecuteInternal(int a, int b) { return SelectedOperation switch { ... }; }
```

A test sets the property directly and calls `ExecuteInternal`:

```csharp
var activity = new Calculator { SelectedOperation = Operation.Multiply };
var result = activity.ExecuteInternal(3, 4);
Assert.Equal(12, result);
```

---

## Default Values for InArgument (CRITICAL)

`InArgument<T>.Get(context)` returns the **value type default** (e.g., `0`, `0.0`, `false`) when the user does not set the property in Studio -- **not** `null`. Null-coalescing fallbacks will never trigger for value types.

### Wrong -- null-coalescing does not work for value types

```csharp
// Rotation.Get(context) returns 0.0 when unset, not null
var rotation = Rotation.Get(context) ?? -45.0;  // always 0.0, never -45.0

// Same problem with default keyword
var opacity = Opacity.Get(context);
if (opacity == default) opacity = 0.5;  // 0.0 IS the default for double, so this works
                                         // but breaks if 0.0 is a valid user input
```

### Correct -- initialize in the property declaration

```csharp
public InArgument<double> Rotation { get; set; } = new(-45.0);
public InArgument<double> Opacity { get; set; } = new(0.5);
public InArgument<int> MaxRetries { get; set; } = new(3);
public InArgument<bool> ContinueOnError { get; set; } = new(false);

protected override void Execute(CodeActivityContext context)
{
    // These return the initialized defaults when the user hasn't set them
    var rotation = Rotation.Get(context);   // -45.0 if unset
    var opacity = Opacity.Get(context);     // 0.5 if unset
}
```

The workflow engine uses the `InArgument<T>` constructor value when the user does not set the property.

### When `[DefaultValue]` is also needed

For backward compatibility (adding a new property to an existing activity), combine both:

```csharp
[DefaultValue(3)]
public InArgument<int> MaxRetries { get; set; } = new(3);
```

`[DefaultValue]` tells the XAML serializer to omit the property when it equals the default, keeping saved workflows clean. The `InArgument<T>` constructor sets the actual runtime value.

---

## Runtime Logging

Activities log via `IExecutorRuntime`, obtained from the activity context. Create a helper extension:

```csharp
// Helpers/ActivityContextExtensions.cs
using System.Activities;
using UiPath.Robot.Activities.Api;

namespace MyCompany.MyActivities.Helpers;

public static class ActivityContextExtensions
{
    public static IExecutorRuntime GetExecutorRuntime(this ActivityContext context)
        => context.GetExtension<IExecutorRuntime>();
}
```

Usage in an activity:

```csharp
protected override int Execute(CodeActivityContext context)
{
    context.GetExecutorRuntime().LogMessage(new LogMessage
    {
        EventType = TraceEventType.Information,
        Message = "Executing Calculator activity"
    });

    var a = FirstNumber.Get(context);
    var b = SecondNumber.Get(context);
    return ExecuteInternal(a, b);
}
```

**Always use `GetExecutorRuntime()`** (the extension method), not `context.GetExtension<IExecutorRuntime>()` with a null-conditional (`?.`). The null-conditional silently drops all log messages when the runtime is not registered — no error, nothing logged, invisible failure.

**Never define your own `GetExecutorRuntime` extension method** if the project already has one in its `Helpers/` folder. A duplicate causes a compile-time ambiguous invocation error.

See [Platform API](../runtime/platform-api.md) for the full `IExecutorRuntime` interface and additional logging details.

---

## Activity Attributes Reference

| Attribute | Purpose |
|---|---|
| `[RequiredArgument]` | Property must be set before execution |
| `[DefaultValue(value)]` | Default value (important for backward compatibility) |
| `[Browsable(false)]` | Hide from properties panel |
| `[Category("name")]` | Group in properties panel |
| `[DisplayName("name")]` | Override display name |
| `[Description("text")]` | Tooltip text |
| `[Obsolete("message")]` | Mark as deprecated |
| `[ViewModelClass(typeof(T))]` | Link to SDK ViewModel (SDK activities only) |
| `[ArgumentSetting("activity", "key")]` | Bind property to a project setting (SDK activities only) |

---

## Troubleshooting

<!-- Agents: add entries here as you encounter common issues during activity implementation.
     Format: ### Issue Title
             **Symptom**: what the developer sees
             **Cause**: why it happens
             **Fix**: how to resolve it -->

### Enum property always uses default value

**Symptom**: The enum property (e.g., `SelectedOperation`) always behaves as if it's the first enum value, regardless of what the user selected in Studio.
**Cause**: The property was declared as `InArgument<TEnum>` instead of plain `TEnum`. The ViewModel uses `DesignProperty<TEnum>` which maps to a plain property, so the binding silently fails.
**Fix**: Change `public InArgument<TEnum> Prop { get; set; }` to `public TEnum Prop { get; set; } = TEnum.Default;`

### ExecuteInternal returns wrong result in tests

**Symptom**: Unit tests calling `ExecuteInternal` directly always get the wrong result (usually the result for the default enum value).
**Cause**: The enum value was captured into a private instance field inside `Execute()`, which isn't called in unit tests.
**Fix**: Have `ExecuteInternal` read the enum directly from the public property (`this.SelectedOperation`) instead of a private field.
