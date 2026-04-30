# Metadata and Registration

> **When to read this**: You are registering a new activity so it appears in the Studio activities panel, or you are setting up the SDK registration class for bindings, settings, or triggers.

**Cross-references**: [ViewModel](viewmodel.md) | [Bindings](bindings.md) | [Project Settings](project-settings.md) | [Localization](localization.md)

---

## Activity Metadata JSON

Since activities are compiled into DLLs packaged in NuGet packages, a metadata JSON file is required for **activity discovery**. Services like TypeCache use this metadata to discover what activities are defined in the package without loading the assemblies.

---

## Platform Split

The metadata is split into two files based on platform availability:

| File | Purpose |
|---|---|
| `ActivitiesMetadataPortable.json` | Cross-platform activities available in **Studio Web** and Studio Desktop |
| `ActivitiesMetadataWindows.json` | Windows-only activities available only in **Studio Desktop** |

Both files are embedded as resources in the project.

For packages with **only cross-platform activities** (most common case): use a single `ActivitiesMetadata.json`.
For packages with **platform-split activities**: use `ActivitiesMetadataPortable.json` + `ActivitiesMetadataWindows.json`.

---

## Minimal Example

```json
{
  "resourceManagerName": "MyCompany.MyActivities.Resources",
  "activities": [
    {
      "fullName": "MyCompany.MyActivities.Calculator",
      "shortName": "Calculator",
      "displayNameKey": "Calculator_DisplayName",
      "descriptionKey": "Calculator_Description",
      "categoryKey": "Math",
      "iconKey": "calculator.svg",
      "viewModelType": "MyCompany.MyActivities.ViewModels.CalculatorViewModel"
    }
  ]
}
```

`resourceManagerName` is required. It is the fully qualified name of the auto-generated `Resources` class from `Resources.resx`. This tells TypeCache where to find localized display names and descriptions.

---

## Full Schema with All Fields

```json
{
  "resourceManagerName": "MyCompany.MyActivities.Resources.Resources",
  "activities": [
    {
      "fullName": "MyCompany.MyActivities.MyActivity",
      "shortName": "MyActivity",
      "displayNameKey": "MyActivity_DisplayName",
      "descriptionKey": "MyActivity_Description",
      "displayNameAliasKeys": ["MyActivity_Alias1"],
      "categoryKey": "MyCategory",
      "iconKey": "my-activity.svg",
      "viewModelType": "MyCompany.MyActivities.ViewModels.MyActivityViewModel",
      "defaultFactory": "MyCompany.MyActivities.Factories.MyActivityFactory",
      "typeArgumentProperty": "Collection",
      "codedWorkflowSupport": false,
      "browsable": true,
      "properties": [
        {
          "name": "InputProp",
          "displayNameKey": "MyActivity_InputProp_DisplayName",
          "tooltipKey": "MyActivity_InputProp_Tooltip",
          "isRequired": true,
          "isVisible": true,
          "isPrincipal": true,
          "category": {
            "name": "Input",
            "displayNameKey": "Input"
          },
          "widget": {
            "type": "variable"
          }
        }
      ]
    }
  ]
}
```

---

## Activity-Level Fields

| Field | Required | Description |
|---|---|---|
| `fullName` | Yes | Fully qualified activity class name (namespace + class) |
| `shortName` | No | Short name used internally |
| `displayNameKey` | Yes | Resource key for the activity's display name |
| `descriptionKey` | Yes | Resource key for the activity's description |
| `displayNameAliasKeys` | No | Alternative search names (array of resource keys) |
| `categoryKey` | Yes | Resource key or literal for the toolbox category |
| `iconKey` | Yes | Filename of the embedded SVG icon |
| `viewModelType` | No | Fully qualified ViewModel class name (if the activity has a custom ViewModel) |
| `defaultFactory` | No | Factory class for generic activities (e.g., `AddToCollection<T>`) |
| `typeArgumentProperty` | No | Property name that determines the type argument for generic activities |
| `codedWorkflowSupport` | No | Whether the activity supports coded workflows (default: true) |
| `browsable` | No | Whether to show in the activities panel (default: true). Set to `false` for legacy/internal activities |
| `resourceManagerName` | No | Fully qualified name of the `.resx` resource class (top-level, shared by all activities) |

---

## Property-Level Fields

Inside the `properties` array of an activity:

| Field | Description |
|---|---|
| `name` | Must match the Activity class property name exactly |
| `displayNameKey` | Resource key for display name |
| `tooltipKey` | Resource key for tooltip |
| `isRequired` | Whether the property is required |
| `isVisible` | Whether the property is visible |
| `isPrincipal` | Whether the property appears in the main panel |
| `category` | Category grouping with `name` and/or `displayNameKey` |
| `widget` | Widget override with `type` (e.g., `"variable"`, `"input"`) |

Property definitions in the metadata JSON provide the **initial/default** configuration. The ViewModel's `InitializeModel()` can override these settings at runtime. When a ViewModel is specified, it typically takes full control of property configuration. When no ViewModel is specified, the metadata JSON is the sole source of property configuration.

---

## Default Activity Icon

Every activity needs an icon. Place SVG files in `Resources/Icons/`. The `.csproj` glob `<EmbeddedResource Include="Resources\Icons\*.svg" />` picks them up automatically.

A generic default icon (24x24):

```xml
<svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
  <path d="M5.00671 19V14H7.00671V19H18V15.5H16L19 12.5L22 15.5H20V19C20 20.1 19.1 21 18 21H10.5H8.00671H7.00671C5.90214 21 5.00671 20.1046 5.00671 19Z" fill="#556068"/>
  <path fill-rule="evenodd" clip-rule="evenodd" d="M8.11702 2.87616L12 5.46482V9.67703L5.86054 12.1328L2 9.3446V5.32297L8.11702 2.87616ZM4 6.67703V8.32198L6.13946 9.86718L10 8.32297V6.53518L7.88298 5.12384L4 6.67703Z" fill="#556068"/>
  <path d="M16 7.5C16 9.15685 17.3431 10.5 19 10.5C20.6569 10.5 22 9.15685 22 7.5C22 5.84315 20.6569 4.5 19 4.5C17.3431 4.5 16 5.84315 16 7.5Z" fill="#556068"/>
</svg>
```

Reference it in `ActivitiesMetadata.json` as `"iconKey": "activityicon.svg"`. For per-activity icons, add separate SVGs and update each activity's `iconKey`.

---

## Embedding in .csproj

```xml
<ItemGroup>
  <EmbeddedResource Include="Resources\ActivitiesMetadataPortable.json" />
  <EmbeddedResource Include="Resources\ActivitiesMetadataWindows.json" />
</ItemGroup>
```

---

## SDK Registration (BaseViewModelDesignerRegistration)

For SDK-based activities, implement a registration class:

```csharp
internal class ViewModelDesignerRegistration : BaseViewModelDesignerRegistration
{
    protected override void RegisterTriggers(IWorkflowDesignApi api, bool enabled) { }

    protected override void RegisterBindings(IWorkflowDesignApi api, bool enabled)
    {
        // Register V3 bindings
        if (api.HasFeature(DesignFeatureKeys.PackageBindingsV3))
        {
            // Register connection bindings, event triggers, etc.
        }
    }

    protected override void RegisterDependentPropertyBindings(
        IWorkflowDesignApi api, bool enabled)
    {
        // Register V4 dependent property bindings
        if (api.HasFeature(DesignFeatureKeys.PackageBindingsV4))
        {
            // Register property-level bindings
        }
    }

    protected override void PublishSettings(IWorkflowDesignApi api, bool enabled)
    {
        if (api.HasFeature(DesignFeatureKeys.Settings))
        {
            api.PublishProjectSettings(
                ArgumentSettingAttribute.SettingsPrefix,
                "My Activities",
                "Settings for My Activities package",
                GetProjectSettings());
        }
    }

    protected override void FinalizeRegistration(IWorkflowDesignApi api) { }
}
```

---

## Troubleshooting

<!-- AI agents: add entries here when you encounter issues related to this topic -->
