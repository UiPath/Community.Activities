# Bindings

> **When to read this**: You need to integrate activity properties with Orchestrator, Assistant, or Integration Service at runtime -- for example, resolving queue names, asset names, folder paths, or connection details automatically via bindings.

**Cross-references**: [ViewModel](viewmodel.md) | [Metadata](metadata.md) | [Project Settings](project-settings.md) | [Solutions](solutions.md)

---

## ActivitiesBindings.json Structure

The most common way to define bindings is via an `ActivitiesBindings.json` file embedded as a resource. This declaratively maps activities to their Orchestrator resource types.

```json
{
  "ActivityBindings": [
    {
      "Activities": [
        "MyCompany.Activities.GetAssetValue",
        "MyCompany.Activities.SetAssetValue"
      ],
      "Type": "asset",
      "Key": {
        "Value": "BindingsKey",
        "ValueSource": "Property"
      },
      "Values": {
        "folderPath": "FolderPath",
        "name": "AssetName"
      },
      "Arguments": {
        "BindingsVersion": {
          "Constant": "2.2"
        }
      }
    }
  ]
}
```

### Fields

| Field | Description |
|---|---|
| `Activities` | Array of fully qualified activity class names that share this binding |
| `Type` | Orchestrator resource type: `"asset"`, `"queue"`, `"process"`, `"bucket"`, `"businessRule"`, `"QueueTrigger"`, `"TimeTrigger"` |
| `Key` | How the binding key is resolved. `ValueSource: "Property"` means it reads from an activity property |
| `Values` | Maps binding values to activity property names (e.g., `"folderPath"` maps to `"FolderPath"` property) |
| `Arguments` | Additional binding arguments. Can be `"Constant"` (literal value) or `"Property"` (from activity property) |

---

## Real-World Example

From System Activities (QueueTrigger):

```json
{
  "Activities": ["UiPath.Core.Activities.QueueTrigger"],
  "Type": "QueueTrigger",
  "Key": { "Value": "BindingsKey", "ValueSource": "Property" },
  "Values": {
    "folderPath": "FolderPath",
    "name": "QueueName"
  },
  "Arguments": {
    "ItemsActivationThreshold": { "Value": "ItemsActivationThreshold", "ValueSource": "Property" },
    "ItemsPerJobActivationTarget": { "Value": "ItemsPerJobActivationTarget", "ValueSource": "Property" },
    "MaxJobsForActivation": { "Value": "MaxJobsForActivation", "ValueSource": "Property" },
    "BindingsVersion": { "Constant": "2.2" }
  }
}
```

Embed the file as a resource:

```xml
<EmbeddedResource Include="Resources\ActivitiesBindings.json" />
```

---

## Binding Attributes on Activity Properties

For SDK-based activities, bindings can also be defined via attributes:

```csharp
// Connection binding - links to Integration Service connection
[ConnectionBinding]
public InArgument<string> ConnectionId { get; set; }

// Event trigger binding
[EventTriggerBinding]
public InArgument<string> EventTriggerId { get; set; }

// Property binding - dependent on another property
[PropertyBinding(nameof(ConnectionId))]
public InArgument<string> AccountId { get; set; }

// Property binding with sub-key
[PropertyBinding(nameof(ConnectionId), SubKey = "folder")]
public InArgument<string> FolderId { get; set; }

// Constant binding
[ConstantBinding("my-constant-key")]
public InArgument<string> ApiEndpoint { get; set; }

// Binding contract (known to Orchestrator/Assistant)
[BindingContract("email-send")]
public InArgument<string> EmailConfig { get; set; }

// Mark class as containing bindings
[HasBinding]
public class MyActivity : CodeActivity { ... }
```

---

## Registering Bindings at Design Time

In the registration class:

```csharp
protected override void RegisterBindings(IWorkflowDesignApi api, bool enabled)
{
    if (!api.HasFeature(DesignFeatureKeys.PackageBindingsV3)) return;

    // Register connections, event triggers, etc.
}

protected override void RegisterDependentPropertyBindings(
    IWorkflowDesignApi api, bool enabled)
{
    if (!api.HasFeature(DesignFeatureKeys.PackageBindingsV4)) return;

    // Register property-to-property bindings
}
```

---

## Connection Widget in ViewModel

```csharp
ConnectionId.Widget = new DefaultWidget
{
    Type = ViewModelWidgetType.Connection,
    Metadata = new() { { nameof(Connector), GetConnector() } }
};
```

---

## Troubleshooting

<!-- AI agents: add entries here when you encounter issues related to this topic -->
