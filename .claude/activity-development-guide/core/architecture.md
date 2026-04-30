# Architecture Overview

> **When to read this**: Before writing your first UiPath activity. This file explains the fundamental 3-part model (Activity, ViewModel, Metadata), the platform components your code interacts with, and the runtime execution contexts. Every other file in this guide assumes you understand these concepts.

---

## The 3-Part Model

A UiPath activity consists of three parts that are linked by naming conventions:

```
+---------------------------------------------------------+
|  Activity (Runtime)                                      |
|  - Inherits CodeActivity<T> or SdkActivity<T>           |
|  - Contains execution logic                              |
|  - Defines InArgument/OutArgument properties              |
+----------------------------+----------------------------+
                             | property names must match
+----------------------------v----------------------------+
|  ViewModel (Design-Time)                                 |
|  - Inherits DesignPropertiesViewModel or BaseViewModel   |
|  - Configures designer UI (widgets, layout, rules)       |
|  - Maps to activity properties by name                   |
+----------------------------+----------------------------+
                             | referenced by
+----------------------------v----------------------------+
|  Metadata (ActivitiesMetadata.json)                      |
|  - Links activity class -> ViewModel class               |
|  - Defines display name, description, category, icon     |
|  - Uses resource keys for localization                   |
+---------------------------------------------------------+
```

**Key principle**: The Activity handles *what happens at runtime*. The ViewModel handles *how the activity looks in the designer*. Property names on the ViewModel MUST exactly match the Activity's `InArgument`/`OutArgument`/property names.

### Activity (Runtime)

The activity class contains the execution logic. It declares typed input/output arguments and overrides an `Execute` method.

```csharp
public class MyActivity : CodeActivity
{
    [RequiredArgument]
    public InArgument<string> Name { get; set; }

    public OutArgument<string> Result { get; set; }

    protected override void Execute(CodeActivityContext context)
    {
        var name = Name.Get(context);
        Result.Set(context, $"Hello, {name}");
    }
}
```

See `../design/viewmodel.md` for the ViewModel side.

### ViewModel (Design-Time)

The ViewModel configures how the activity appears in UiPath Studio. Each property on the ViewModel corresponds 1:1 with an Activity property by name.

```csharp
public class MyActivityViewModel : DesignPropertiesViewModel
{
    public DesignInArgument<string> Name { get; set; }  // matches Activity.Name
    public DesignOutArgument<string> Result { get; set; } // matches Activity.Result

    public MyActivityViewModel(IDesignServices services) : base(services) { }

    protected override void InitializeModel()
    {
        base.InitializeModel();
        PersistValuesChangedDuringInit();

        Name.IsPrincipal = true;
        Name.IsRequired = true;
    }
}
```

### Metadata (ActivitiesMetadata.json)

The metadata JSON links the Activity class to its ViewModel and defines display attributes:

```json
{
  "activities": [
    {
      "fullName": "MyCompany.MyActivities.MyActivity",
      "viewModelFullName": "MyCompany.MyActivities.ViewModels.MyActivityViewModel",
      "displayNameResource": "MyActivity_DisplayName",
      "descriptionResource": "MyActivity_Description",
      "categoryResource": "Category_General",
      "iconName": "my-activity",
      "codedWorkflowSupport": true
    }
  ]
}
```

---

## Platform Components

```
+-----------------------------------------------------------------+
|  UiPath Studio / Studio Web                                      |
|  - Design-time environment where workflows are built             |
|  - Hosts activity designers (ViewModels)                         |
|  - Provides IWorkflowDesignApi for design-time services          |
|  - Studio Desktop: Windows, full feature set                     |
|  - Studio Web: browser-based, cross-platform subset              |
+-----------------------------+-----------------------------------+
                              | deploys workflows to
+-----------------------------v-----------------------------------+
|  UiPath Robot / Assistant                                        |
|  - Runtime engine that executes workflows                        |
|  - Provides IExecutorRuntime (logging, settings, auth tokens)    |
|  - Provides IWorkflowRuntime (execution, feature detection)      |
|  - Attended (user-triggered) or Unattended (Orchestrator-triggered)|
+-----------------------------+-----------------------------------+
                              | communicates with
+-----------------------------v-----------------------------------+
|  UiPath Orchestrator                                             |
|  - Server-side management and execution platform                 |
|  - Manages queues, assets, processes, jobs, storage buckets      |
|  - Provides REST APIs consumed by activities                     |
|  - Version-aware: activities adapt behavior to Orchestrator ver. |
+-----------------------------------------------------------------+
```

---

## Studio Desktop vs Studio Web

Activities run in two designer environments with different capabilities:

| Aspect | Studio Desktop | Studio Web |
|--------|---------------|------------|
| Platform | Windows (`net6.0-windows`) | Browser (`net6.0`, cross-platform) |
| Metadata file | `ActivitiesMetadataWindows.json` + `ActivitiesMetadataPortable.json` | `ActivitiesMetadataPortable.json` only |
| Feature detection | `workflowDesignApi.HasFeature(DesignFeatureKeys.StudioDesignSettingsV3)` | Returns `false` for Desktop-only features |
| Helper | `workflowDesignApi.RunningInStudioDesktop()` | Returns `false` |

```csharp
// Detecting Studio Desktop at design time
var workflowDesignApi = Services.GetService<IWorkflowDesignApi>();
bool isStudioDesktop = workflowDesignApi?.RunningInStudioDesktop() == true;
bool isStudioWeb = !isStudioDesktop;
```

For packages with platform-split activities, replace `ActivitiesMetadata.json` with both `ActivitiesMetadataPortable.json` and `ActivitiesMetadataWindows.json`. See `project-structure.md` for the .csproj configuration.

---

## Execution Contexts

Activities can be triggered from different sources. The `IRunningJobInformation.InitiatedBy` property indicates the source:

| Value | Context |
|-------|---------|
| `"Orchestrator"` | Unattended job triggered by Orchestrator |
| `"Studio"` | Debug run from Studio |
| `"StudioX"` | Debug run from StudioX |
| `"StudioPro"` | Debug run from Studio Pro |
| `"Assistant"` | Attended run via UiPath Assistant |
| `"CommandLine"` | Command-line execution |
| `"RobotAPI"` | Triggered via Robot API |

```csharp
// Access execution context at runtime
var executorRuntime = context.GetExtension<IExecutorRuntime>();
var jobInfo = executorRuntime?.RunningJobInformation;
var initiatedBy = jobInfo?.InitiatedBy;        // "Orchestrator", "Studio", etc.
var projectFramework = jobInfo?.TargetFramework; // Legacy, Windows, Portable
```

---

## Integration Service

Activities that connect to external services (email, CRM, cloud storage) use UiPath Integration Service via connection bindings:

```csharp
// Activity declares a connection binding
[ConnectionBinding]
public InArgument<string> ConnectionId { get; set; }
```

```csharp
// ViewModel shows the connection picker widget
ConnectionId.Widget = new DefaultWidget
{
    Type = ViewModelWidgetType.Connection,
    Metadata = new() { { nameof(Connector), GetConnector() } }
};
```

See `../design/widgets.md` for the full widget reference including the Connection widget.

---

## Coded Workflows

Activities can declare whether they support coded workflows (C# code files) vs classic XAML workflows:

```json
{
  "fullName": "MyCompany.MyActivity",
  "codedWorkflowSupport": true
}
```

Set `codedWorkflowSupport: false` in metadata for activities that only work in the XAML designer.

---

## Troubleshooting

<!-- Agents: add troubleshooting entries here as you encounter common architecture-related issues.
     Format: ### Problem title
             **Symptom**: what the developer sees
             **Cause**: why it happens
             **Fix**: how to resolve it -->
