# Rules and Dependencies

> **When to read this**: You need to react to property value changes in the designer -- for example, showing/hiding properties conditionally, updating a DataSource when a parent property changes, or running async operations with a busy indicator.

**Cross-references**: [ViewModel](viewmodel.md) | [DataSources](datasources.md) | [Validation](validation.md)

---

## Defining Rules

Rules are reactive handlers that execute when property values change. Define them in `InitializeRules()`.

```csharp
protected override void InitializeRules()
{
    base.InitializeRules();

    // Synchronous rule
    Rule(nameof(PropertyA), OnPropertyAChanged);

    // Async rule
    Rule(nameof(PropertyB), async () => await OnPropertyBChangedAsync());

    // Rule that also runs on initialization
    Rule("InitRule", OnSomethingChanged, runOnInit: true);

    // Rule that does NOT run on initialization
    Rule("LazyRule", OnLazyChange, runOnInit: false);
}
```

---

## Registering Dependencies

Dependencies tell the framework which property changes should trigger which rules.

```csharp
protected override void ManualRegisterDependencies()
{
    base.ManualRegisterDependencies();

    // When PropertyA.Value changes, trigger the rule named "PropertyA"
    RegisterDependency(PropertyA, nameof(PropertyA.Value), nameof(PropertyA));

    // Multiple properties can trigger the same rule
    RegisterDependency(PropertyB, nameof(PropertyB.Value), "SharedRule");
    RegisterDependency(PropertyC, nameof(PropertyC.Value), "SharedRule");
}
```

---

## Rule and Dependency Patterns

Three organizational patterns exist for rules and dependencies. Match the pattern used in the file you're editing.

### Pattern A: Separate methods (most explicit)

Rules in `InitializeRules()`, dependencies in `ManualRegisterDependencies()`:

```csharp
protected override void InitializeRules()
{
    base.InitializeRules();
    Rule(nameof(OnCategoryChanged), OnCategoryChanged, runOnInit: false);
}

protected override void ManualRegisterDependencies()
{
    base.ManualRegisterDependencies();
    RegisterDependency(Category, nameof(Category.Value), nameof(OnCategoryChanged));
}

private void OnCategoryChanged()
{
    CategoryItem!.IsVisible = Category!.HasValue;
}
```

### Pattern B: Inline (compact)

Both `Rule` and `RegisterDependency` inside `InitializeRules()`, no `ManualRegisterDependencies()`:

```csharp
protected override void InitializeRules()
{
    base.InitializeRules();
    Rule("OnCategoryChanged", OnCategoryChanged, false);
    RegisterDependency(Category, nameof(DesignProperty.Value), "OnCategoryChanged");
}
```

### Pattern C: Inline lambda

Rule handler is an anonymous lambda directly in `InitializeRules()`:

```csharp
protected override void InitializeRules()
{
    base.InitializeRules();

    Rule("VisibleActionType", () =>
    {
        MyProperty1!.IsVisible = someCondition;
        MyProperty2!.IsVisible = otherCondition;
    });

    // Async lambda
    Rule("LaunchAction", async () =>
    {
        await _someService.StartAsync();
    }, runOnInit: false);
}

protected override void ManualRegisterDependencies()
{
    RegisterDependency(TriggerProp, nameof(TriggerProp.Value), "VisibleActionType");
}
```

**Note**: `base.InitializeRules()` and `base.ManualRegisterDependencies()` calls are optional in many codebases. Include them unless the existing file omits them.

---

## Common Rule Patterns

### Conditional Visibility

```csharp
Rule("Visibility", () =>
{
    var mode = GetStringValue(ModeProperty.Value);
    AdvancedProp1.IsVisible = mode == "Advanced";
    AdvancedProp2.IsVisible = mode == "Advanced";
    SimpleProp.IsVisible = mode == "Simple";
});

// Register dependency
RegisterDependency(ModeProperty, nameof(ModeProperty.Value), "Visibility");
```

### Dynamic DataSource Update

```csharp
Rule(nameof(ParentProperty), () =>
{
    var parentValue = GetStringValue(ParentProperty.Value);
    _childDataSource.ParentFilter = parentValue;
    // DataSource re-queries automatically on next search
});

RegisterDependency(ParentProperty, nameof(ParentProperty.Value), nameof(ParentProperty));
```

### Async Rule with Busy Indicator

```csharp
// Pattern 1: Direct busy service usage
Rule(nameof(SourceProperty), async () =>
{
    await using (await _busyService.BeginAsync(
        new BusyOptions { Kind = BusyKind.Activity, Message = "Loading..." }))
    {
        var data = await _service.FetchAsync();
        Dispatcher.Invoke(() => TargetProperty.Value = data);
    }
});

// Pattern 2: RunWithBusyService helper (common in System Activities)
Rule(nameof(WorkflowFileName), async () => await RunWithBusyService(WorkflowFileNameChangedRule), false);

private async Task RunWithBusyService(Func<Task> action)
{
    if (_busyService != null)
    {
        await using (await _busyService.BeginAsync(
            new BusyOptions { Kind = BusyKind.Activity }))
        {
            await action();
        }
    }
    else
    {
        await action();
    }
}
```

---

## Important: Dispatcher.Invoke() for Async Rules

When updating properties from async rules or background threads, you **must** use `Dispatcher.Invoke()`. Failing to do so can cause cross-thread exceptions or silently dropped updates.

```csharp
Dispatcher.Invoke(() =>
{
    TargetProperty.Value = newValue;
    TargetProperty.IsVisible = true;
});
```

---

## Troubleshooting

<!-- AI agents: add entries here when you encounter issues related to this topic -->
