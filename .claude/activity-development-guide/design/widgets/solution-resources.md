# SolutionResourcesWidget

## When to read this

Read this document when you need to:
- Add a Solutions resource picker (assets, queues, etc.) to an activity
- Configure resource types and expected properties for the picker
- Understand how the widget integrates with the Solutions context

For general Solutions integration patterns, see [../solutions.md](../solutions.md).

---

## Overview

The `SolutionResourcesWidget` provides a picker for resources managed by UiPath Solutions. When an automation runs inside a Solution, Orchestrator resources (assets, queues, buckets, etc.) are managed at the Solution level rather than configured per-activity. This widget replaces manual resource name inputs with a structured picker that shows available Solution resources.

`SolutionResourcesWidget` is a specialized class (not `DefaultWidget`). Use with `DesignProperty<T>`.

---

## Basic Usage

```csharp
Property.Widget = new SolutionResourcesWidget
{
    ResourceType = SolutionsResourceKind.Asset,
    ExpectedProperties = new List<string> { SolutionsPropertyContracts.ResourceName },
    ResourceSubTypes = resourceSubtypes,
};
```

---

## Configuration Properties

| Property | Type | Description |
|---|---|---|
| `ResourceType` | `SolutionsResourceKind` | The kind of Orchestrator resource (e.g., `Asset`, `Queue`, `Bucket`). |
| `ExpectedProperties` | `List<string>` | Property contracts that the selected resource must provide (e.g., `ResourceName`). |
| `ResourceSubTypes` | varies | Optional sub-type filter to narrow which resources are shown. |

---

## Integration with Solutions Context

The `SolutionResourcesWidget` should only be used when the activity is running inside a Solution. Check the `IUserDesignContext` service to determine this:

```csharp
protected override async ValueTask InitializeModelAsync()
{
    await base.InitializeModelAsync();

    var userDesignContext = Services.GetService<IUserDesignContext>();
    bool isInSolution = !string.IsNullOrEmpty(userDesignContext?.SolutionId);

    if (isInSolution)
    {
        await InitializeSolutionsScopePropertiesAsync();
    }
    else
    {
        await InitializeProjectScopePropertiesAsync();
    }
}
```

---

## Pattern: BaseSolutionResourceViewModel

For activities that work with Orchestrator resources (assets, queues, etc.), use the `BaseSolutionResourceViewModel` pattern. This base class provides two initialization paths:

```csharp
public class MyAssetViewModel : BaseSolutionResourceViewModel
{
    protected override async ValueTask InitializeProjectScopePropertiesAsync()
    {
        // Configure for standalone project:
        // - Show FolderPath property
        // - Register Orchestrator-based data sources
        AssetName.SupportsDynamicDataSourceQuery = true;
        AssetName.RegisterService<IDynamicDataSourceBuilder>(
            new OrchestratorAssetDataSource(_tokenProvider));
    }

    protected override async ValueTask InitializeSolutionsScopePropertiesAsync()
    {
        // Configure for Solutions:
        // - Hide FolderPath (managed by Solution)
        // - Use SolutionResourcesWidget instead
        FolderPath.IsVisible = false;
        AssetName.Widget = new SolutionResourcesWidget
        {
            ResourceType = SolutionsResourceKind.Asset
        };
    }
}
```

### Key points

- In **standalone project** mode: show manual configuration (FolderPath, dynamic data source for searching).
- In **Solutions** mode: hide FolderPath (the Solution manages it) and use `SolutionResourcesWidget` for the resource picker.
- Always provide both code paths. The activity must work in both contexts.

---

## Examples

### Asset picker

```csharp
AssetName.Widget = new SolutionResourcesWidget
{
    ResourceType = SolutionsResourceKind.Asset,
    ExpectedProperties = new List<string> { SolutionsPropertyContracts.ResourceName }
};
```

### Queue picker

```csharp
QueueName.Widget = new SolutionResourcesWidget
{
    ResourceType = SolutionsResourceKind.Queue,
    ExpectedProperties = new List<string> { SolutionsPropertyContracts.ResourceName }
};
```

---

## Troubleshooting

**Widget shows no resources.**
Verify that the activity is running inside a Solution context (`IUserDesignContext.SolutionId` is not empty). If the user opens the activity in a standalone project, the `SolutionResourcesWidget` will have no resources to display. Use the dual-path pattern described above to fall back to manual configuration.

**Widget appears in standalone project mode.**
Ensure you check `IUserDesignContext.SolutionId` before assigning the `SolutionResourcesWidget`. Only assign it in the Solutions scope initialization path.

**FolderPath still shows in Solutions mode.**
Set `FolderPath.IsVisible = false` in `InitializeSolutionsScopePropertiesAsync()`. The Solution manages the folder path, so it should be hidden from the user.

**`ISolutionResources` service is null.**
The `ISolutionResources` service is only available in Solutions-capable Studio versions. Check for null before using:

```csharp
var solutionResources = Services.GetService<ISolutionResources>();
if (solutionResources != null)
{
    // Safe to use Solutions APIs
}
```
