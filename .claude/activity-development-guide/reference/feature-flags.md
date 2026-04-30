# Design Feature Flags

> **When to read this:** You need to check which Studio capabilities are available at design time
> before enabling optional ViewModel behavior (e.g., project settings, advanced bindings, triggers).
> Feature flags are checked at runtime via `IWorkflowDesignApi.HasFeature()`.

## Cross-references

- Service access (how to get `IWorkflowDesignApi`): [service-access.md](service-access.md)
- Compilation constants (build-time platform checks): [compilation-constants.md](compilation-constants.md)
- ViewModel basics: [../design/viewmodel-basics.md](../design/viewmodel-basics.md)
- Complete example: [../examples/complete-example.md](../examples/complete-example.md)

---

## Feature flag reference

Feature flags are queried through `IWorkflowDesignApi`:

```csharp
var api = Services.GetService<IWorkflowDesignApi>();
```

| Feature Key | Check | Purpose |
|---|---|---|
| `DesignFeatureKeys.Settings` | `api.HasFeature(DesignFeatureKeys.Settings)` | Project settings support -- the host can persist per-project settings |
| `DesignFeatureKeys.PackageBindingsV3` | `api.HasFeature(DesignFeatureKeys.PackageBindingsV3)` | V3 binding support -- enables standard property bindings |
| `DesignFeatureKeys.PackageBindingsV4` | `api.HasFeature(DesignFeatureKeys.PackageBindingsV4)` | V4 dependent property bindings -- enables bindings that depend on other property values |
| `DesignFeatureKeys.ActivityTriggers` | `api.HasFeature(DesignFeatureKeys.ActivityTriggers)` | Trigger support -- the host supports activity-based triggers |
| `DesignFeatureKeys.WidgetSupportInfoService` | `api.HasFeature(DesignFeatureKeys.WidgetSupportInfoService)` | Widget availability checks -- can query whether a specific widget type is supported |

## Usage examples

### Conditional project settings

```csharp
protected override void InitializeModel()
{
    base.InitializeModel();

    var api = Services.GetService<IWorkflowDesignApi>();

    if (api.HasFeature(DesignFeatureKeys.Settings))
    {
        // Read a default value from project settings
        var defaultServer = api.ProjectSettings.GetValue("EmailServer");
        if (!string.IsNullOrEmpty(defaultServer))
        {
            Server.SetDefaultValue(defaultServer);
        }
    }
}
```

### Guarding V4 dependent bindings

```csharp
protected override void InitializeModel()
{
    base.InitializeModel();

    var api = Services.GetService<IWorkflowDesignApi>();

    if (api.HasFeature(DesignFeatureKeys.PackageBindingsV4))
    {
        // Use dependent property bindings (V4)
        ConfigureDependentBindings();
    }
    else if (api.HasFeature(DesignFeatureKeys.PackageBindingsV3))
    {
        // Fall back to standard bindings (V3)
        ConfigureStandardBindings();
    }
}
```

### Checking widget support before assignment

```csharp
protected override void InitializeModel()
{
    base.InitializeModel();

    var api = Services.GetService<IWorkflowDesignApi>();

    if (api.HasFeature(DesignFeatureKeys.WidgetSupportInfoService))
    {
        var widgetSupport = Services.GetService<IWidgetSupportInfoService>();
        if (widgetSupport.IsSupported(ViewModelWidgetType.PromptComposer))
        {
            Prompt.Widget = new DefaultWidget
            {
                Type = ViewModelWidgetType.PromptComposer
            };
        }
        else
        {
            // Fall back to a plain text composer
            Prompt.Widget = new DefaultWidget
            {
                Type = ViewModelWidgetType.TextComposer
            };
        }
    }
}
```

## Notes

- Feature flags are a design-time mechanism. They detect what the hosting Studio version supports.
  For build-time platform differences, use [compilation constants](compilation-constants.md) instead.
- Always provide a fallback when a feature is absent. Activities must degrade gracefully in older
  Studio versions.
- `HasFeature()` returns `false` if the key is unrecognized, so it is safe to check for newer flags
  in code that runs on older hosts.
