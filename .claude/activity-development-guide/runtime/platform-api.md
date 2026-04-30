# Platform API (Runtime and Design-Time Services)

> **When to read this**: You need to interact with UiPath platform services from within an activity -- logging messages, reading project settings, obtaining OAuth tokens, checking robot/orchestrator configuration, or detecting available features at runtime or design time.

**Related files**:
- [Activity Code](../runtime/activity-code.md) -- base classes and property types
- [Orchestrator Integration](../runtime/orchestrator.md) -- Orchestrator-specific API calls and version detection

---

## API Package Hierarchy

Activities interact with the UiPath platform through a layered set of NuGet packages. The source definitions live in the Studio repository.

```
UiPath.Activities.Api
|-- UiPath.Activities.Api.Base         Base interfaces (all platforms)
|   |-- IAccessProvider                OAuth token management
|   |-- IActivitiesSettingsReader      Settings access
|   |-- IDotnetRuntime                 .NET runtime info
|   |-- IConfiguration                 Job-specific configuration
|   +-- ITelemetryCustomDimensions     Custom telemetry fields
|
|-- UiPath.Robot.Activities.Api        Robot/Executor runtime APIs
|   |-- IExecutorRuntime               Main executor service
|   |-- IRunningJobInformation         Current job details
|   |-- IWorkflowCommunication         Workflow-to-client messaging
|   |-- IProfilingCollector            Performance profiling
|   |-- LogMessage                     Logging DTO
|   +-- ExecutorFeatureKeys            Feature version constants
|
|-- UiPath.Activities.Contracts        Core runtime contracts
|   |-- IWorkflowRuntime              Workflow-level services
|   |-- IRobotSettings                Robot configuration
|   |-- IOrchestratorSettings         Orchestrator URLs and headers
|   |-- RuntimeFeatures               Runtime feature constants
|   +-- WorkflowParameters            Workflow invocation DTOs
|
+-- UiPath.Studio.Activities.Api       Design-time (Studio) APIs
    |-- IWorkflowDesignApi             Master design-time interface
    |-- IDialogService                 Show dialogs
    |-- IStudioBusyService             Busy indicators
    |-- IOrchestratorApiService        Orchestrator access at design time
    |-- IVariableService               Variable management
    |-- IExpressionService             Expression analysis
    |-- DesignFeatureKeys              Design-time feature constants
    +-- ... (50+ other design-time services)
```

---

## IExecutorRuntime (Robot Runtime Services)

The primary runtime interface. Accessed via `context.GetExtension<IExecutorRuntime>()`.

```csharp
public interface IExecutorRuntime : IHasFeature
{
    IActivitiesSettingsReader Settings { get; }      // Project settings reader
    IAccessProvider AccessProvider { get; }           // OAuth tokens
    IRunningJobInformation RunningJobInformation { get; } // Job info

    void LogMessage(LogMessage message);             // Runtime logging

    // Feature-gated services (check HasFeature before accessing)
    IWorkflowCommunication WorkflowCommunication { get; } // Workflow <-> client messaging
    IProfilingCollector ProfilingCollector { get; }        // Performance profiling
    IDotnetRuntime DotnetRuntime { get; }                  // .NET runtime info
    IConfiguration Configuration { get; }                  // Extended job settings
}
```

### Logging

```csharp
var runtime = context.GetExtension<IExecutorRuntime>();
runtime.LogMessage(new LogMessage
{
    EventType = TraceEventType.Information,
    Message = "Processing item"
});
```

### Reading project settings at runtime

```csharp
if (runtime.Settings.TryGetValue<string>("MyPackage.DefaultTimeout", out var timeout))
{
    // Use the project-level setting
}
```

### Getting OAuth tokens for API calls

```csharp
var token = await runtime.AccessProvider.GetAccessToken("openid", cancellationToken: ct);
var resourceUrl = await runtime.AccessProvider.GetResourceUrl("orchestrator", ct);
```

---

## IWorkflowRuntime (Workflow Execution Services)

Accessed via `context.GetExtension<IWorkflowRuntime>()`. Provides orchestration-level services.

```csharp
public interface IWorkflowRuntime
{
    // Configuration
    IRobotSettings RobotSettings { get; }
    IOrchestratorSettings OrchestratorSettings { get; }

    // Workflow management
    IWorkflowExecutor CreateWorkflowExecutor(WorkflowParameters parameters);
    void CancelWorkflow(Guid workflowInstanceId);

    // Logging
    void Log(LogMessage message, Guid workflowInstanceId);
    Task FlushLogs();

    // Feature detection
    bool HasFeature(string featureName);
}
```

### Accessing Orchestrator settings

```csharp
var wfRuntime = context.GetExtension<IWorkflowRuntime>();
var queuesUrl = wfRuntime.OrchestratorSettings.QueuesUrl;
var robotId = wfRuntime.RobotSettings.RobotId;
var headers = wfRuntime.OrchestratorSettings.GetHeaders();
```

### Extension helper methods

Defined in `Extensions.cs` for convenience:

```csharp
context.GetWorkflowRuntime()          // -> IWorkflowRuntime
context.GetRobotSettings()            // -> IRobotSettings
context.GetOrchestratorSettings()     // -> IOrchestratorSettings
context.GetExecutorRuntime()          // -> IExecutorRuntime (custom helper)
```

---

## IRobotSettings

```csharp
public interface IRobotSettings
{
    Guid RobotId { get; }                           // Robot identifier
    string RobotName { get; }                       // Robot display name
    bool ShouldStop { get; }                        // Stop requested?
    IDictionary<string, object> LogFields { get; }  // Custom log fields
    IReadOnlyCollection<Package> ReferencedPackages { get; }
}
```

---

## IOrchestratorSettings

```csharp
public interface IOrchestratorSettings
{
    string ConfigurationUrl { get; }    // Base Orchestrator URL
    string QueuesUrl { get; }           // Queue API endpoint
    string MonitoringUrl { get; }       // Monitoring endpoint
    string ProcessName { get; }         // Current process name
    IReadOnlyDictionary<string, string> GetHeaders(); // Auth/folder headers
}
```

---

## IRunningJobInformation

Rich context about the current execution:

```csharp
var jobInfo = executorRuntime.RunningJobInformation;

jobInfo.JobId               // Orchestrator job ID
jobInfo.ProcessName         // Process name
jobInfo.ProcessVersion      // Package version
jobInfo.ProjectDirectory    // Local project path
jobInfo.TargetFramework     // Legacy, Windows, Portable
jobInfo.InitiatedBy         // "Orchestrator", "Studio", "Assistant"...
jobInfo.FolderId            // Orchestrator folder ID
jobInfo.FolderName          // Orchestrator folder name
jobInfo.TenantId            // Tenant identifier
jobInfo.UserEmail           // Executing user's email
jobInfo.RuntimeGovernanceEnabled // Governance policy active?
```

---

## IWorkflowDesignApi (Design-Time Services)

The master design-time interface. Accessed in ViewModels via `Services.GetService<IWorkflowDesignApi>()`.

| Service | Purpose |
|---------|---------|
| `HasFeature(key)` | Feature detection |
| `AccessProvider` | OAuth tokens at design time |
| `ProjectPropertiesService` | Project configuration |
| `StudioDesignSettings` | Studio settings |
| `ActivitiesSettingsService` | Activity settings |
| `DialogService` | Show dialogs |
| `StudioBusyService` | Busy indicators |
| `OrchestratorApiService` | Orchestrator API access |
| `VariableService` | Variable management |
| `ExpressionService` | Expression analysis |
| `LibraryService` | Object Repository |
| `WizardsService` | Wizard dialogs |
| `ActivityTriggerService` | Trigger definitions |

---

## Feature Detection Pattern

Services are versioned. Always check feature availability before access to avoid `NullReferenceException` or `NotSupportedException` on older hosts.

### Runtime feature check

```csharp
var wfRuntime = context.GetExtension<IWorkflowRuntime>();
if (wfRuntime?.HasFeature(RuntimeFeatures.FlushLogs) == true)
{
    await wfRuntime.FlushLogs();
}
```

### Executor feature check

```csharp
var executor = context.GetExtension<IExecutorRuntime>();
if (executor?.HasFeature(ExecutorFeatureKeys.AccessProviderV3) == true)
{
    var token = await executor.AccessProvider.GetAccessToken("scope", ct);
}
```

### Design-time feature check

```csharp
var designApi = Services.GetService<IWorkflowDesignApi>();
if (designApi?.HasFeature(DesignFeatureKeys.ProjectPropertiesV2) == true)
{
    var props = designApi.ProjectPropertiesService;
}
```

**Pattern**: Always use `?.HasFeature(...) == true` (null-safe) because the extension may not be registered in all host environments (e.g., unit tests, older Studio versions).

---

## RuntimeFeatures Constants

```csharp
RuntimeFeatures.OrchestratorHeaders     // Orchestrator auth headers available
RuntimeFeatures.FolderPath              // Folder path supported
RuntimeFeatures.FlushLogs              // Log flushing supported
RuntimeFeatures.CancelWorkflow         // Workflow cancellation supported
RuntimeFeatures.UsePackage             // Package invocation supported
RuntimeFeatures.InvokeProcess          // Process invocation supported
RuntimeFeatures.CrossWorkflowProperties // Cross-workflow properties supported
RuntimeFeatures.ActivityTracing        // Activity tracing supported
```

---

## ExecutorFeatureKeys Constants

Feature versions tied to Robot/Executor releases:

| Feature | Executor Version |
|---------|-----------------|
| Settings, AccessProvider | 19.8 |
| RunningJobInformation | 19.10 |
| WorkflowCommunication | 22.4 |
| Profiling | 22.6 |
| DotnetRuntime | 23.2 |
| Configuration | 24.4 |
| TelemetryCustomDimensions | 24.10 |

Use this table to determine the minimum Robot version your activity requires. If you call `executor.AccessProvider`, your activity needs Robot 19.8+. Guard access with `HasFeature` for backward compatibility with older robots.

---

## Troubleshooting

<!-- Agents: add entries here as you encounter common issues with the platform API.
     Format: ### Issue Title
             **Symptom**: what the developer sees
             **Cause**: why it happens
             **Fix**: how to resolve it -->
