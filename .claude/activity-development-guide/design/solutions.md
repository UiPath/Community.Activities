# Solutions vs Project Scope

> **When to read this**: Your activity may behave differently when running inside a UiPath Solution (cloud-managed) versus a standalone project. You need to detect the context and configure properties accordingly.

**Cross-references**: [ViewModel](viewmodel.md) | [DataSources](datasources.md) | [Bindings](bindings.md)

---

## Detecting Solutions Context

```csharp
protected override async ValueTask InitializeModelAsync()
{
    await base.InitializeModelAsync();

    var userDesignContext = Services.GetService<IUserDesignContext>();
    bool isInSolution = !string.IsNullOrEmpty(userDesignContext?.SolutionId);

    if (isInSolution)
    {
        _solutionResources = Services.GetService<ISolutionResources>();
        await InitializeSolutionsScopePropertiesAsync();
    }
    else
    {
        await InitializeProjectScopePropertiesAsync();
    }
}
```

---

## BaseSolutionResourceViewModel Pattern

For activities that work with Orchestrator resources (assets, queues, etc.), derive from `BaseSolutionResourceViewModel` to get built-in project vs. solution branching:

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

---

## Feature Detection for Solutions

Always check for feature availability before using platform-specific APIs:

```csharp
if (_workflowDesignApi?.HasFeature(DesignFeatureKeys.Settings) == true)
{
    // Safe to use project settings API
}

if (_workflowDesignApi?.HasFeature(DesignFeatureKeys.PackageBindingsV4) == true)
{
    // Safe to use V4 bindings
}

if (_workflowDesignApi?.HasFeature(DesignFeatureKeys.WidgetSupportInfoService) == true)
{
    // Check specific widget availability
    if (_workflowDesignApi.WidgetSupportInfoService?.IsWidgetSupported(widgetType) == true)
    {
        Property.Widget = new DefaultWidget { Type = widgetType };
    }
}
```

---

## Troubleshooting

<!-- AI agents: add entries here when you encounter issues related to this topic -->
