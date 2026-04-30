# Advanced Patterns

> **When to read this:** You are building a complex activity that goes beyond simple property binding — for example, an activity with multiple input modes, dynamic display names, composite child activities, persistence/bookmarks, or inverse property mappings. These patterns are drawn from real implementations like RunJob, InvokeWorkflow, and Orchestrator activities.

**Cross-references:**
- [ViewModel fundamentals](../design/viewmodel.md)
- [Activity code and CacheMetadata](../runtime/activity-code.md)
- [Architecture overview](../core/architecture.md)
- [FilterBuilder widget](./filter-builder.md) (another advanced topic)
- [SDK framework](./sdk-framework.md) (alternative base classes)

---

## Bidirectional Property Mapping (Inverted Boolean)

When the ViewModel needs a user-friendly property that maps inversely to the Activity property:

```csharp
// Activity has: public bool FailWhenFaulted { get; set; }
// ViewModel exposes the opposite for better UX:

[NotMappedProperty]
public DesignProperty<bool> ContinueWhenFaulted { get; set; }

private ModelProperty _failWhenFaultedModelProperty;

protected override async ValueTask InitializeModelAsync()
{
    await base.InitializeModelAsync();
    _failWhenFaultedModelProperty = ModelItem.Properties[nameof(RunJob.FailWhenFaulted)];
    ContinueWhenFaulted.Value = !(bool)_failWhenFaultedModelProperty.ComputedValue;
}

// Sync back on change
public override async ValueTask<UpdateViewModelResult> UpdateAsync(string propertyName, object value)
{
    if (propertyName == nameof(ContinueWhenFaulted))
        _failWhenFaultedModelProperty.SetValue(!(bool)value);
    return await base.UpdateAsync(propertyName, value);
}
```

Key points:
- Mark the ViewModel property with `[NotMappedProperty]` so the framework does not attempt automatic mapping.
- Read the activity's `ModelProperty` directly in `InitializeModelAsync`.
- Write back through `ModelProperty.SetValue()` in `UpdateAsync`.

---

## Multiple Input Mode Switching (3+ Modes)

For activities supporting fundamentally different input strategies (e.g., RunJob supports Object, Dictionary, and DataMapper modes):

```csharp
private readonly MenuAction _objectInputSwitch = new();
private readonly MenuAction _dictionaryInputSwitch = new();
private readonly MenuAction _dataMapperInputSwitch = new();

private async Task SwitchToObjectHandler(MenuAction _)
{
    // Clear other modes
    _inputModelProperty.SetValue(null);
    // Build new InArgument<object>
    var argument = Argument.Create(typeof(object), ArgumentDirection.In);
    _inputDesignProperty = BuildInputDesignProperty(argument);
    // Optionally import output schema
    await ImportArgument(processName, ArgumentDirection.Out);
}

private Task SwitchToDictionaryHandler(MenuAction _)
{
    _inputModelProperty.SetValue(null);
    _outputModelProperty.SetValue(null);
    var args = new Dictionary<string, Argument>();
    _argumentsModelProperty.SetValue(args);
    return Task.CompletedTask;
}

private async Task SwitchToDataMapperHandler(MenuAction _)
{
    // Fetch schema and generate JIT types
    var metadata = await _schemaProvider.GetArguments(processName);
    await ImportArgument(processName, ArgumentDirection.In, metadata?.Input);
}
```

Each `MenuAction` is wired to a handler that:
1. Clears properties belonging to other modes (set to `null`).
2. Creates and configures the new mode's properties.
3. Optionally fetches external metadata (schema, arguments).

---

## Localized Enum Values

Enum values can carry localization attributes for display in the designer:

```csharp
public enum JobExecutionMode
{
    [LocalizedDisplayName(nameof(Resources.RunJob_ExecutionMode_None_DisplayName))]
    [LocalizedDescription(nameof(Resources.RunJob_ExecutionMode_None_Description))]
    None,

    [LocalizedDisplayName(nameof(Resources.RunJob_ExecutionMode_Busy_DisplayName))]
    [LocalizedDescription(nameof(Resources.RunJob_ExecutionMode_Busy_Description))]
    Busy,

    [LocalizedDisplayName(nameof(Resources.RunJob_ExecutionMode_Suspend_DisplayName))]
    [LocalizedDescription(nameof(Resources.RunJob_ExecutionMode_Suspend_Description))]
    Suspend
}
```

Both `[LocalizedDisplayName]` and `[LocalizedDescription]` reference keys in the `.resx` resource file. The Studio designer resolves these at design time for the current locale.

---

## Display Name Alias Keys (Fuzzy Search)

Activities can define alias keys so users can find them by searching different terms in the Studio activities panel:

```json
{
  "fullName": "UiPath.Activities.System.Jobs.RunJob",
  "displayNameKey": "RunJob_DisplayName",
  "displayNameAliasKeys": [
    "RunJob_Synonym_Agent",
    "RunJob_Synonym_API",
    "RunJob_Synonym_RPA",
    "RunJob_Synonym_RunAgent",
    "RunJob_Synonym_RunAPI",
    "RunJob_Synonym_CaseManagement"
  ]
}
```

Each alias key is a resource key in the `.resx` file. The Studio activities panel searches these aliases when the user types in the search box. This is configured in the activity metadata JSON file.

---

## Dynamic Display Name from External Data

Activities can update their display name based on runtime data (e.g., process type fetched from Orchestrator):

```csharp
private async Task ProcessNameChangedRule()
{
    if (!ProcessName.TryGetLiteralOfType(out var processName)) return;

    // Fetch process type from Orchestrator metadata
    var suffix = await _dataSourceBuilder.GetSuffixByProcessType(processName);
    // e.g., suffix = "RPA", "API", "Agent", "App"

    if (!string.IsNullOrWhiteSpace(suffix))
    {
        var newDisplayName = $"Run {suffix}: {processName}";
        ModelItem.Properties[nameof(RunJob.DisplayName)]?.SetValue(newDisplayName);
        Dispatcher.Invoke(() => DisplayName.Value = newDisplayName);
    }
}
```

Important: update both the `ModelItem` property (persisted to XAML) and the `DesignProperty.Value` (displayed in the UI). Use `Dispatcher.Invoke` for UI thread safety.

---

## Event-Driven DataSource

DataSource builders can raise events to notify the ViewModel when data changes:

```csharp
internal class ProcessesDataSourceBuilder : FolderPathDynamicDataSourceBuilder
{
    public event EventHandler<DataSource<ReleaseDto>> DataSourceGenerated;

    public override async ValueTask<IDataSource> GetDynamicDataSourceAsyncInternal(
        string searchText, int limit, CancellationToken ct)
    {
        var data = await GetProcesses(searchText, ct, limit);
        DataSource.Data = data ?? Array.Empty<ReleaseDto>();
        DataSourceGenerated?.Invoke(this, DataSource);
        return DataSource;
    }
}

// In ViewModel:
_dataSourceBuilder.DataSourceGenerated += (sender, ds) =>
{
    // React to data changes, e.g., update dependent properties
};
```

This pattern decouples data fetching from the ViewModel reaction logic. The DataSource builder handles the async fetch and notifies subscribers when fresh data is available.

---

## Conditional Rule Wrapping

Wrap rules with optional services (like BusyService) without duplicating logic:

```csharp
// Helper that conditionally wraps with busy indicator
private Func<Task> ResolveRule(Func<Task> rule)
    => _busyService is not null
        ? () => RunWithBusyService(() => rule())
        : rule;

protected override void InitializeRules()
{
    base.InitializeRules();
    Rule(nameof(ProcessName), ResolveRule(ProcessNameChangedRule), false);
    Rule(nameof(ExecutionMode), ResolveRule(ExecutionModeChangedRule), false);
}
```

This avoids duplicating busy-indicator logic in every rule handler. If `_busyService` is not available (e.g., in unit tests), the rule runs without the wrapper.

---

## NativeActivity with Composite Implementation

For activities that dynamically compose child activities at design time:

```csharp
public class RunJob : NativeActivity
{
    private readonly Sequence _implementationSequence = new();
    private readonly WaitForJob _waitForJobActivity = new();
    private readonly WaitForJobAndResume _waitForJobAndResumeActivity = new();

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        _implementationSequence.Activities.Clear();

        // Dynamically add the right child activity based on configuration
        Activity waitActivity = ExecutionMode switch
        {
            JobExecutionMode.Busy => _waitForJobActivity,
            JobExecutionMode.Suspend => _waitForJobAndResumeActivity,
            _ => throw new InvalidOperationException()
        };

        _implementationSequence.Activities.Add(waitActivity);
        metadata.AddImplementationChild(_implementationSequence);

        // Bind dynamic arguments to metadata
        foreach (var (name, argument) in Arguments)
        {
            var runtimeArgument = new RuntimeArgument(name, argument.ArgumentType, argument.Direction);
            metadata.Bind(argument, runtimeArgument);
            metadata.AddArgument(runtimeArgument);
        }
    }
}
```

Key points:
- `AddImplementationChild` registers the composed sequence as an implementation detail (not visible to the user).
- Dynamic arguments (e.g., from a dictionary) must be individually registered with `Bind` + `AddArgument`.
- The implementation sequence is rebuilt on every `CacheMetadata` call, so it always reflects the current configuration.

---

## Persistent Activity with Bookmarks

For long-running activities that survive process restarts:

```csharp
[PersistentActivity]
[ValidatePersistenceDependsOn(nameof(ExecutionMode), nameof(JobExecutionMode.Suspend))]
public class RunJob : NativeActivity
{
    // Dynamically add/remove persistence constraints
    private void UpdateNoPersistScopeConstraint(bool shouldHaveConstraint)
    {
        if (shouldHaveConstraint && !Constraints.Contains(_constraint))
            Constraints.Add(_constraint);
        else if (!shouldHaveConstraint && Constraints.Contains(_constraint))
            Constraints.Remove(_constraint);
    }
}

// In the child activity:
protected void Persist(NativeActivityContext context, object resumeTrigger)
{
    var bookmark = context.CreateBookmark(Guid.NewGuid().ToString(), OnWaitResume);
    var persistenceBookmarks = context.GetExtension<IPersistenceBookmarks>();
    persistenceBookmarks.RegisterBookmark(
        new PersistenceBookmark(bookmark.Name, resumeTrigger));
}
```

- `[PersistentActivity]` marks the activity as persistence-aware.
- `[ValidatePersistenceDependsOn]` conditionally validates persistence requirements based on property values.
- `CreateBookmark` pauses execution; the workflow can be persisted and later resumed.
- `IPersistenceBookmarks.RegisterBookmark` associates the bookmark with a resume trigger for the orchestrator.

---

## Interface Contracts for Bindings

Activities implement interfaces that the bindings system uses for polymorphic resource resolution:

```csharp
// Contract interfaces
internal interface IOrchestratorActivity
{
    InArgument<string> FolderPath { get; }
}

internal interface IProcessNameActivity : IOrchestratorActivity
{
    InArgument<string> ProcessName { get; }
}

// Activity implements the interface
public class RunJob : NativeActivity, IProcessNameActivity
{
    public InArgument<string> FolderPath { get; set; }
    public InArgument<string> ProcessName { get; set; }

    // Bindings key generated from interface contract
    public string BindingsKey => BindingsKeyFactory.GenerateBindingsKey(this);
}

// At runtime, check for binding overrides before using property value
private string GetProcessNameValue(ActivityContext context)
    => _bindingsService.GetBindingsOverride(
           SystemBindingTypes.Process, BindingsKey, PropertyContracts.Name, context)
       ?? ProcessName.Get(context);
```

The bindings system allows Studio to override property values at runtime based on project-level configuration (e.g., connecting a RunJob activity to a specific Orchestrator folder without hardcoding it). The interface contracts define which properties are bindable.

---

## Rule-Driven Property ReadOnly State

Change property editability dynamically based on another property's value:

```csharp
Rule(nameof(ExecutionMode), () =>
{
    var mode = ExecutionMode.Value;
    TimeoutMS.IsReadOnly = mode is not JobExecutionMode.Busy;
    ContinueOnError.IsReadOnly = mode is JobExecutionMode.None;
    ContinueWhenFaulted.IsReadOnly = mode is JobExecutionMode.None;
});
```

When `ExecutionMode` changes, the rule fires and updates `IsReadOnly` on dependent properties. This provides immediate visual feedback in the designer — readonly properties are grayed out and non-editable.

---

## Troubleshooting

<!-- AI agents: add entries here when you encounter issues related to this topic -->
