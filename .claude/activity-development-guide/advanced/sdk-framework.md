# Activities SDK Framework

> **When to read this:** You are building activities that need dependency injection, automatic telemetry, project settings integration, connection/binding support, built-in retry logic, or governance. If your activity is a simple synchronous operation (string manipulation, math), the classic `CodeActivity<T>` pattern is sufficient and you do not need the SDK.

**Cross-references:**
- [Activity code and CacheMetadata](../runtime/activity-code.md) — classic activity patterns (CodeActivity, AsyncCodeActivity, NativeActivity)
- [ViewModel fundamentals](../design/viewmodel.md) — classic DesignPropertiesViewModel
- [Architecture overview](../core/architecture.md) — project structure, naming conventions
- [Advanced patterns](./patterns.md) — NativeActivity composites, bookmarks, interface contracts

---

## When to Use the SDK (vs Classic CodeActivity)

Use the SDK when your activities need:
- **Dependency injection** -- register and resolve services via `Microsoft.Extensions.DependencyInjection`
- **Automatic telemetry** -- execution metrics tracked without manual instrumentation
- **Project settings integration** -- `[ArgumentSetting]` attribute auto-reads values from Studio project settings
- **Connection/binding support** -- `[ConnectionBinding]`, `[PropertyBinding]`, `[BindingContract]` attributes
- **Built-in retry logic** -- `SdkNativeActivity<T>` provides `ShouldRetry()` and `PrepareForRetry()`
- **Governance** -- runtime governance checks applied automatically

For simple synchronous activities (e.g., string manipulation, math), the classic `CodeActivity<T>` pattern is sufficient -- the SDK adds unnecessary complexity.

---

## SDK Setup

The SDK is added as a git submodule and integrated via MSBuild targets:

```
repo/
  +-- Activities.SDK/           <-- git submodule
  |   +-- Sdk.imports.build.targets
  |   +-- Sdk.dependencies.build.targets
  |   +-- Sdk.constants.build.targets
  +-- MyActivityPack/
  |   +-- Directory.build.targets  <-- imports SDK targets
  |   +-- Activities/
  |   |   +-- Activities.csproj
  |   |   +-- MyActivity.cs
  |   +-- Activities.UnitTest/
  |       +-- Activities.UnitTest.csproj
```

**Directory.build.targets** (in the activity pack directory):

```xml
<Project>
  <Import Project="..\Activities.SDK\Sdk.dependencies.build.targets"/>
  <Import Project="..\Activities.SDK\Sdk.imports.build.targets"/>
</Project>
```

**Activity .csproj** -- enable SDK shared project imports:

```xml
<PropertyGroup>
  <UiPathActivityProject>True</UiPathActivityProject>         <!-- imports UiPath.Sdk.Activities.shproj -->
  <UiPathActivityDesignProject>True</UiPathActivityDesignProject> <!-- imports UiPath.Sdk.Activities.Design.shproj -->
</PropertyGroup>
```

Package versions are centrally managed by `Sdk.dependencies.build.targets` -- do NOT specify versions for SDK-managed packages in individual `.csproj` files.

---

## SDK Build Constants

The SDK defines conditional compilation constants in `Sdk.constants.build.targets`:

| Constant | Condition |
|----------|-----------|
| `NETCORE_UIPATH` | `net5.0+` (any .NET Core target) |
| `WINDOWS_UIPATH` | `net461`/`net462` or `*-windows` |
| `NETPORTABLE_UIPATH` | `net5.0`/`net6.0`/`net7.0`/`net8.0` (non-Windows) |
| `INTEGRATION_SERVICE` | `<UseIntegrationService>True</UseIntegrationService>` |
| `GOVERNANCE_SERVICE` | `<UseGovernanceService>True</UseGovernanceService>` |

Use these constants for platform-specific code:

```csharp
#if NETCORE_UIPATH
    // .NET Core-specific implementation
#elif WINDOWS_UIPATH
    // .NET Framework / Windows-specific implementation
#endif
```

---

## SdkActivity\<T\>

The primary SDK base class for async activities. Derives from `AsyncCodeActivity<T>` and wraps the APM pattern into a clean async/await API with automatic service scope management.

### Class Hierarchy

```csharp
public abstract class SdkActivity<T> : SdkActivity<T, DefaultRuntimeServicePolicy> { }

public abstract class SdkActivity<T, TPolicy> : AsyncCodeActivity<T>, IDisposable
    where TPolicy : class, IRuntimeServicePolicy, new()
{
    protected IServiceProvider Services { get; }
    protected IRuntimeServices RuntimeServices { get; }

    protected abstract Task<T> ExecuteAsync(
        AsyncCodeActivityContext context,
        IServiceProvider serviceProvider,   // scoped to this execution
        CancellationToken cancellationToken);

    protected virtual void OnCompleted(
        AsyncCodeActivityContext context,
        IServiceProvider serviceProvider) { }  // runs with fresh context after await
}
```

### Lifecycle

1. SDK creates a DI scope tied to the execution context.
2. Applies project settings via `RuntimeServices.ProjectSettings?.Apply(context, this)`.
3. Applies bindings via `RuntimeServices.BindingService?.Apply(this)`.
4. Calls `ExecuteAsync()` -- read all inputs before the first `await` (context disposed after).
5. Stores the result in `Result`.
6. Creates a new DI scope and calls `OnCompleted()` with a fresh context.

### Example

```csharp
public partial class OpenAccount : SdkActivity<IAccount>
{
    [RequiredArgument]
    public string AccountHolder { get; set; }

    public OpenAccount() : base() { }
    internal OpenAccount(IServiceProvider provider) : base(provider) { }

    protected override async Task<IAccount> ExecuteAsync(
        AsyncCodeActivityContext context,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var bank = serviceProvider.GetService<IBank>();
        var client = await bank.GetClientAsync(AccountHolder, cancellationToken);

        RuntimeServices.ExecutorRuntime?.LogMessage(new LogMessage
        {
            EventType = TraceEventType.Information,
            Message = $"Account opened for {AccountHolder}"
        });

        return client.Accounts[0];
    }
}
```

Note the dual constructor pattern: the parameterless constructor is for the workflow runtime; the `IServiceProvider` constructor is for unit testing.

---

## SdkNativeActivity\<T\>

For activities that need WF4 native features: child activity scheduling, bookmarks, or built-in retry.

### Class Definition

```csharp
public abstract class SdkNativeActivity<T> : NativeActivity<T>
{
    protected abstract Task<T> ExecuteAsync(
        NativeActivityContext context,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);

    protected virtual bool ShouldRetry(NativeActivityContext context, object executionValue, int retryCount) => false;
    protected virtual void PrepareForRetry(NativeActivityContext context, int retryCount) { }
    protected int GetRetryCount(NativeActivityContext context);
    protected virtual void OnCompleted(NativeActivityContext context, IServiceProvider serviceProvider) { }
}
```

### Retry Support

Override `ShouldRetry` and `PrepareForRetry` for built-in retry logic:

```csharp
protected override bool ShouldRetry(NativeActivityContext context, object executionValue, int retryCount)
{
    // Retry up to 3 times on transient failures
    return retryCount < 3 && executionValue is TransientException;
}

protected override void PrepareForRetry(NativeActivityContext context, int retryCount)
{
    // Reset state before retry
    _cachedResult = null;
}
```

### Container Activity Example

Schedules child activities via `OnCompleted`:

```csharp
public partial class MyContainer : SdkNativeActivity<int>
{
    [Browsable(false)]
    public ActivityAction<object> Body { get; set; } = new()
    {
        Argument = new DelegateInArgument<object>("Service"),
        Handler = new Sequence() { DisplayName = "Do" }
    };

    protected override Task<int> ExecuteAsync(
        NativeActivityContext context, IServiceProvider sp, CancellationToken ct)
        => Task.FromResult(42);

    protected override void OnCompleted(NativeActivityContext context, IServiceProvider sp)
    {
        base.OnCompleted(context, sp);
        context.ScheduleAction(Body, new MyService());
    }
}
```

---

## Custom Service Policies

Override `DefaultRuntimeServicePolicy` to register custom services in the DI container:

```csharp
public class CustomServicePolicy : DefaultRuntimeServicePolicy
{
    public override IServicePolicy Register(Action<IServiceCollection> collection = null)
    {
        _services.TryAddSingleton<IMyService, MyService>();
        return base.Register(collection);
    }
}

// Use as second type parameter
public partial class MyActivity : SdkActivity<string, CustomServicePolicy> { ... }
```

`TryAddSingleton` ensures the service is only registered once, even if multiple activities use the same policy.

---

## SDK ViewModel Pattern

SDK ViewModels inherit from `BaseViewModel` (or `BaseViewModel<TPolicy>`) and are linked to activities via the `[ViewModelClass]` attribute on a partial class.

### Activity Partial for ViewModel Registration

```csharp
// ViewModels/MyActivity.Design.cs
[ViewModelClass(typeof(MyActivityViewModel))]
public partial class MyActivity { }
```

### ViewModel Class

```csharp
internal class MyActivityViewModel : BaseViewModel
{
    public DesignInArgument<string> Input { get; set; } = new();
    public DesignOutArgument<string> Result { get; set; } = new();

    public MyActivityViewModel(IDesignServices services) : base(services) { }
    internal MyActivityViewModel(IDesignServices services, IServiceProvider sp) : base(services, sp) { }

    protected override void InitializeModel()
    {
        base.InitializeModel();
        Input.IsPrincipal = true;
        Input.OrderIndex = PropertyOrderIndex++;   // built-in counter
        Result.OrderIndex = PropertyOrderIndex++;
    }
}
```

---

## Differences Table: Classic vs SDK

| Aspect | Classic (`DesignPropertiesViewModel`) | SDK (`BaseViewModel`) |
|--------|--------------------------------------|----------------------|
| Order counter | Manual `var order = 0` | Built-in `PropertyOrderIndex` |
| Class visibility | `public` | `internal` |
| Activity linking | `viewModelType` in metadata JSON | `[ViewModelClass]` attribute |
| DI in ViewModel | Not available | `ActivityServices.GetService<T>()` |
| Design-time services | Via `IDesignServices` | Via `DesignServices` property |
| Init call | `base.InitializeModel()` + `PersistValuesChangedDuringInit()` | `base.InitializeModel()` only |
| Testability | Mock `IDesignServices` | Mock `IDesignServices` + inject `IServiceProvider` |

---

## SDK-Specific Attributes

| Attribute | Purpose |
|-----------|---------|
| `[ConnectionBinding]` | Marks a property as an Integration Service connection |
| `[PropertyBinding(nameof(X))]` | Declares a dependency on another property for binding |
| `[BindingContract("Name")]` | Defines a contract name for the binding system |
| `[ArgumentSetting(section, property)]` | Auto-reads value from Studio project settings |
| `[Sensitive]` | Excludes property data from telemetry |
| `[ViewModelClass(typeof(VM))]` | Links activity to its ViewModel (on partial class) |

### Example: ArgumentSetting

```csharp
// Automatically reads from project settings at runtime
[ArgumentSetting("OrchestratorSettings", "DefaultFolderPath")]
public InArgument<string> FolderPath { get; set; }
```

### Example: ConnectionBinding

```csharp
[ConnectionBinding]
[BindingContract("SalesforceConnection")]
public InArgument<string> ConnectionId { get; set; }
```

---

## IRuntimeServices

Available via the `RuntimeServices` property on any SDK activity:

```csharp
RuntimeServices.ExecutorRuntime   // IExecutorRuntime -- logging, settings, OAuth
RuntimeServices.WorkflowRuntime   // IWorkflowRuntime -- workflow services
RuntimeServices.ProjectSettings   // Read/apply project settings
RuntimeServices.Telemetry         // Automatic telemetry tracking
RuntimeServices.BindingService    // Apply connection/property bindings
RuntimeServices.Governance        // Runtime governance (if GOVERNANCE_SERVICE defined)
RuntimeServices.ConnectionClient  // Integration Service client (if INTEGRATION_SERVICE defined)
```

All properties are nullable -- the SDK gracefully handles missing services (e.g., when running outside Studio or in unit tests). Always use null-conditional access:

```csharp
RuntimeServices.ExecutorRuntime?.LogMessage(new LogMessage { ... });
RuntimeServices.Governance?.ValidateExecution(this);
```

---

## Troubleshooting

<!-- AI agents: add entries here when you encounter issues related to this topic -->
