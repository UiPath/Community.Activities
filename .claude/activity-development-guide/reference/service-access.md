# Service Access Reference

> **When to read this:** You need to interact with Studio services from a ViewModel -- showing dialogs,
> detecting features, registering types, or obtaining auth tokens. This table lists every service
> available through the `Services` property on `DesignPropertiesViewModel`.

## Cross-references

- ViewModel basics: [../design/viewmodel-basics.md](../design/viewmodel-basics.md)
- Feature flags: [feature-flags.md](feature-flags.md)
- Complete example (ViewModel usage): [../examples/complete-example.md](../examples/complete-example.md)

---

## Service table

All services are obtained via `Services.GetService<T>()` inside a ViewModel.

| Service | How to Access | Purpose |
|---|---|---|
| `IWorkflowDesignApi` | `Services.GetService<IWorkflowDesignApi>()` | Workflow/project APIs, feature detection via `HasFeature()` |
| `IBusyService` | `Services.GetService<IBusyService>()` | Show loading indicators during long-running operations |
| `IDialogService` | `Services.GetService<IDialogService>()` | Show dialogs and confirmation prompts to the user |
| `IDesignerCustomTypesService` | `Services.GetService<IDesignerCustomTypesService>()` | Register custom .NET types for use in expressions |
| `IUserDesignContext` | `Services.GetService<IUserDesignContext>()` | Check whether the activity is running in a Solutions context |
| `ISolutionResources` | `Services.GetService<ISolutionResources>()` | Manage and access Solutions resource files |
| `IAccessProvider` | `Services.GetService<IAccessProvider>()` | Obtain auth tokens for external API calls |
| `IViewModelActionCallbackFactory` | `Services.GetService<IViewModelActionCallbackFactory>()` | Create callbacks for link clicks and action buttons |

## Usage example

```csharp
protected override void InitializeModel()
{
    base.InitializeModel();

    // Feature detection
    var api = Services.GetService<IWorkflowDesignApi>();
    if (api.HasFeature(DesignFeatureKeys.Settings))
    {
        // Project settings are supported -- configure accordingly
    }

    // Show a loading indicator during async initialization
    var busy = Services.GetService<IBusyService>();
    using (busy.ShowBusy())
    {
        // ... long-running init work ...
    }
}
```

## Notes

- `GetService<T>()` returns `null` if the service is not available in the current host. Always
  null-check when the service may be absent (e.g., `ISolutionResources` is only present in
  Solutions-enabled Studio).
- `IWorkflowDesignApi.HasFeature()` is the primary way to check Studio capabilities at design time.
  See [feature-flags.md](feature-flags.md) for the list of known feature keys.
