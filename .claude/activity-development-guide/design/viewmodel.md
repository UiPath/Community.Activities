# ViewModel (Design-Time)

> **When to read this**: You are creating or modifying a ViewModel class for a UiPath activity. You need to understand base class selection, property types, configuration API, lifecycle hooks, or partial class organization.

**Cross-references**: [DataSources](datasources.md) | [Rules and Dependencies](rules-and-dependencies.md) | [Menu Actions](menu-actions.md) | [Validation](validation.md) | [Metadata](metadata.md) | [Localization](localization.md)

---

## Base Class Options

| Base Class | When to Use |
|---|---|
| `DesignPropertiesViewModel` | Standard activities (most common) |
| `BaseViewModel` / `BaseViewModel<TPolicy>` | SDK activities needing DI and service policies |
| `PreviewActivityViewModel` | Activities with live preview (formatting, regex) |
| `BaseOrchestratorClientActivityViewModel` | Activities calling Orchestrator APIs |
| `BaseSolutionResourceViewModel` | Activities working with Solutions resources |

---

## Minimal ViewModel Example

```csharp
using System.Activities.DesignViewModels;

namespace MyCompany.MyActivities.ViewModels;

public class CalculatorViewModel : DesignPropertiesViewModel
{
    // Property names MUST match the Activity class property names exactly
    public DesignInArgument<int>? FirstNumber { get; set; }
    public DesignInArgument<int>? SecondNumber { get; set; }
    public DesignProperty<Operation>? SelectedOperation { get; set; }
    public DesignOutArgument<int>? Result { get; set; }

    public CalculatorViewModel(IDesignServices services) : base(services) { }

    protected override void InitializeModel()
    {
        base.InitializeModel();
        PersistValuesChangedDuringInit();

        var order = 0;

        // Input properties
        FirstNumber!.DisplayName = Resources.Calculator_FirstNumber_DisplayName;
        FirstNumber!.Tooltip = Resources.Calculator_FirstNumber_Tooltip;
        FirstNumber!.IsRequired = true;
        FirstNumber!.IsPrincipal = true;
        FirstNumber!.OrderIndex = order++;

        SecondNumber!.DisplayName = Resources.Calculator_SecondNumber_DisplayName;
        SecondNumber!.Tooltip = Resources.Calculator_SecondNumber_Tooltip;
        SecondNumber!.IsRequired = true;
        SecondNumber!.IsPrincipal = true;
        SecondNumber!.OrderIndex = order++;

        SelectedOperation!.DisplayName = Resources.Calculator_Operation_DisplayName;
        SelectedOperation!.Tooltip = Resources.Calculator_Operation_Tooltip;
        SelectedOperation!.IsPrincipal = true;
        SelectedOperation!.OrderIndex = order++;

        // Output properties (not principal, at the end)
        Result!.DisplayName = Resources.Calculator_Result_DisplayName;
        Result!.Tooltip = Resources.Calculator_Result_Tooltip;
        Result!.OrderIndex = order++;
    }
}
```

### Alternative: Primary Constructor (C# 12)

```csharp
public class MyViewModel(IDesignServices services) : DesignPropertiesViewModel(services)
{
    // No explicit constructor body needed
}
```

When adding to an existing file, match its constructor style.

---

## ViewModel Property Types

| ViewModel Type | Maps to Activity Type | Purpose |
|---|---|---|
| `DesignInArgument<T>` | `InArgument<T>` | Input that accepts expressions/variables |
| `DesignOutArgument<T>` | `OutArgument<T>` | Output written to variables |
| `DesignInOutArgument<T>` | `InOutArgument<T>` | Bidirectional argument |
| `DesignProperty<T>` | Direct `T` property | Constants, enums, non-argument values |

---

## Nullable Properties and the `!` Operator

In projects with `<Nullable>enable</Nullable>`, all ViewModel design properties must be declared nullable:

```csharp
public DesignInArgument<string>? Input { get; set; }
public DesignOutArgument<int>? Result { get; set; }
public DesignProperty<MyEnum>? Mode { get; set; }
```

The framework initializes these before `InitializeModel()` runs, so they are never actually null at that point. Use the null-forgiving operator (`!`) on every access inside `InitializeModel()`:

```csharp
Input!.DisplayName = Resources.MyActivity_Input_DisplayName;
Input!.IsRequired = true;
Input!.IsPrincipal = true;
```

**Every individual member access needs `!`** — it is not sufficient to use it only on the first access to a property.

The `= new()` inline initializer is optional. `DesignPropertiesViewModel` initializes all design properties via reflection. Many codebases use `= new()` anyway for clarity — match the style of the file you're editing.

---

## Property Configuration API

```csharp
property.DisplayName = "User-Facing Name";    // Label in designer
property.Tooltip = "Hover help text";          // Tooltip
property.EditPlaceholder = "Type here...";     // Placeholder in edit mode
property.IsPrincipal = true;                   // Show in main panel (non-collapsible)
property.IsRequired = true;                    // Validation: must be set
property.IsVisible = true;                     // Show/hide dynamically
property.IsReadOnly = false;                   // Enable/disable editing
property.OrderIndex = 0;                       // Display order (lower = higher)
property.Category = "Input";                   // Group label
property.Widget = new DefaultWidget { ... };   // Widget type override
property.DataSource = ...;                     // Dropdown/autocomplete data
property.SupportsDynamicDataSourceQuery = true;// Enable server-side search
```

**`Name` vs `DisplayName`**: `DisplayName` is the human-readable label (use `Resources.*` keys). `Name` is the internal property key (defaults to the C# property name). Only set `Name` explicitly for `[NotMappedProperty]` UI elements where you need a specific label without a resource string.

---

## Property Ordering

Two patterns exist for managing property order:

```csharp
// Pattern 1: Manual counter (standard DesignPropertiesViewModel)
var order = 0;
FirstProp.OrderIndex = order++;
SecondProp.OrderIndex = order++;

// Pattern 2: Built-in counter (SDK BaseViewModel)
// BaseViewModel provides a PropertyOrderIndex counter:
FirstProp.OrderIndex = PropertyOrderIndex++;
SecondProp.OrderIndex = PropertyOrderIndex++;
```

---

## Expression Language Detection (SDK)

SDK ViewModels can detect whether the project uses C# or VB.NET expressions:

```csharp
// Available in BaseViewModel<TPolicy>
if (ExpressionLanguage == ExpressionLanguage.CSharp)
{
    // Generate C# expression syntax
}
else
{
    // Generate VB.NET expression syntax
}
```

---

## Property Name Override

When the ViewModel property name differs from the Activity property name, override via `Name`:

```csharp
// ViewModel property "AssetName" maps to Activity property "OrchestratorCredentialName"
public DesignInArgument<string> AssetName { get; set; } = new() { Name = nameof(OrchestratorCredentialName) };

// ViewModel property "FolderPath" maps to Activity property "OrchestratorFolderPath"
public DesignInArgument<string> FolderPath { get; set; } = new() { Name = nameof(OrchestratorFolderPath) };
```

### Non-Generic DesignOutArgument

For activities with `OutArgument Output { get; set; }` (no type parameter), use `DesignOutArgument Output { get; set; }` (without `<T>`) in the ViewModel. This is for outputs whose type is determined dynamically at runtime.

---

## Property Attributes

```csharp
[NotMappedProperty]  // Property exists only in ViewModel, not persisted to Activity
public DesignProperty<string> Preview { get; set; }

[NotMapped]  // Alias for NotMappedProperty
public DesignProperty<string> InfoDisplay { get; set; }

[Browsable(false)]  // Hidden from properties panel
public DesignProperty<string> InternalState { get; set; }
```

`[NotMappedProperty]` marks a ViewModel property that has no corresponding Activity property. Use for UI-only elements (action buttons, display labels, preview fields):

```csharp
[NotMappedProperty]
public DesignProperty<object>? LaunchButton { get; set; }

// In InitializeModel():
LaunchButton!.DisplayName = Resources.MyActivity_Launch_DisplayName;
LaunchButton!.Widget = new DefaultWidget { Type = ViewModelWidgetType.ActionButton };
LaunchButton!.IsPrincipal = true;
```

---

## Reading Property Values in Rules

```csharp
// Try to get a literal value (non-expression) from an InArgument
if (Property.TryGetLiteralOfType(out var literalValue))
{
    // literalValue is the typed value
}

// Get expression text
var expressionText = Property.Value.GetExpressionText();

// Check if property has a value
if (Property.HasValue) { ... }
```

---

## ViewModel Lifecycle

The framework calls these methods in order during initialization:

```
1. Constructor(IDesignServices services)
2. InitializeModel()              <- Configure properties (sync)
3. InitializeModelAsync()         <- Configure properties (async, e.g., fetch data)
4. InitializeRules()              <- Register reactive rules (sync)
5. InitializeRulesAsync()         <- Register reactive rules (async)
6. AutoRegisterDependencies()     <- Framework auto-detects dependencies
7. ManualRegisterDependencies()   <- Register explicit dependencies
8. Execute all rules with runOnInit: true
9. ProcessDependencies()          <- Wire up property change handlers
```

### InitializeModel vs InitializeModelAsync

Two initialization methods are available:

- `InitializeModel()` (sync): Call `base.InitializeModel()` as the **first** line. Use when no async work is needed.
- `InitializeModelAsync()` (async): Call `base.InitializeModelAsync()` as the **last** line (return it). Use when initialization needs async calls.

```csharp
// Sync — base first, then configure
protected override void InitializeModel()
{
    base.InitializeModel();
    PersistValuesChangedDuringInit();
    MyProp!.IsPrincipal = true;
}

// Async — configure first, base last (returned)
protected override ValueTask InitializeModelAsync()
{
    MyProp!.IsPrincipal = true;
    return base.InitializeModelAsync();
}

// Async with actual awaits
protected override async ValueTask InitializeModelAsync()
{
    await base.InitializeModelAsync();
    var data = await someService.GetDataAsync();
    MyProp!.DataSource = BuildDataSource(data);
}
```

`PersistValuesChangedDuringInit()` is only needed when property values set during init must persist. Many simple ViewModels omit it. When used, it must go **immediately after** `base.InitializeModel()`, before any property assignments.

---

## Override Pattern Example

```csharp
protected override void InitializeModel()
{
    base.InitializeModel();
    PersistValuesChangedDuringInit();
    // Configure properties here
}

protected override async ValueTask InitializeModelAsync()
{
    await base.InitializeModelAsync();
    // Async initialization (API calls, data fetching)
}

protected override void InitializeRules()
{
    base.InitializeRules();
    Rule(nameof(PropertyA), OnPropertyAChanged);
    Rule("AsyncRule", async () => await DoSomethingAsync(), runOnInit: true);
}

protected override void ManualRegisterDependencies()
{
    base.ManualRegisterDependencies();
    RegisterDependency(PropertyA, nameof(PropertyA.Value), nameof(PropertyA));
}
```

---

## Constructor vs InitializeModel

Simple property configuration (IsPrincipal, IsReadOnly) can be done in the constructor. Reserve `InitializeModel()` for configuration that needs the full initialization context (DataSource, service access).

For service access:
- **Constructor**: use the `services` parameter: `services.GetService<T>()`
- **InitializeModel/rules**: use the base `Services` property: `Services.GetService<T>()`

---

## SDK ViewModel with Service Policy

When using the Activities.SDK framework with dependency injection:

```csharp
// Custom service policy for design-time DI
internal class MyDesignServicePolicy : DefaultDesignServicePolicy
{
    public override IServicePolicy Register(Action<IServiceCollection> collection = null)
    {
        _services.TryAddSingleton<IMyService, MyService>();
        return base.Register(collection);
    }
}

// ViewModel using the custom policy
public class MyActivityViewModel : BaseViewModel<MyDesignServicePolicy>
{
    public DesignInArgument<string> Input { get; set; }
    public DesignOutArgument<string> Output { get; set; }

    public MyActivityViewModel(IDesignServices services) : base(services) { }

    protected override void InitializeModel()
    {
        base.InitializeModel();
        // Access injected services
        var myService = ActivityServices.GetService<IMyService>();
    }
}
```

---

## Partial Class Organization (Large ViewModels)

For complex ViewModels, split into partial classes by concern:

```
MyActivityViewModel.cs                  # Core properties and initialization
MyActivityViewModel.Configuration.cs    # Widget/property configuration
MyActivityViewModel.Rules.cs            # Rule definitions and handlers
MyActivityViewModel.Actions.cs          # Menu action handlers
```

---

## Resource Cleanup

If a ViewModel subscribes to events or holds disposable resources, override `Dispose(bool)`:

```csharp
private bool _disposedValue;

protected override void Dispose(bool disposing)
{
    if (!_disposedValue)
    {
        if (disposing)
        {
            _channel.MessageReceived -= OnMessageReceived;
        }
        _disposedValue = true;
    }
    base.Dispose(disposing);
}
```

---

## UpdateAsync Override

Override `UpdateAsync` to intercept property value changes in the designer (e.g., to send a value to an external service):

```csharp
protected override async ValueTask<UpdateViewModelResult> UpdateAsync(
    string propertyName, object value)
{
    if (propertyName == nameof(MyProp))
    {
        await _service.ProcessAsync(value);
    }
    return await base.UpdateAsync(propertyName, value);
}
```

Always call `base.UpdateAsync()` to complete the standard update flow.

---

## Troubleshooting

<!-- AI agents: add entries here when you encounter issues related to this topic -->
