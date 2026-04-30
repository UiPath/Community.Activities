# Connection Widget

## When to read this

Read this document when you need to:
- Add an Integration Service connection picker to an activity
- Configure the `Connection` widget with a connector
- Understand how connections relate to bindings

---

## Overview

The `Connection` widget provides a picker for UiPath Integration Service connections. When an activity connects to an external service (email, CRM, cloud storage), the Connection widget lets the user select a configured connection from their Orchestrator or Integration Service instance.

Use with `DesignProperty<string>` (the property stores the connection ID).

---

## Basic Usage

```csharp
ConnectionId.Widget = new DefaultWidget
{
    Type = ViewModelWidgetType.Connection,
    Metadata = new() { { nameof(Connector), GetConnector() } }
};
```

The `Metadata` dictionary must include a `Connector` key whose value is the connector identifier returned by a `GetConnector()` method (typically defined in the ViewModel base class or a shared helper).

---

## Activity-Side Setup

On the activity class, declare the connection property with the `[ConnectionBinding]` attribute. This tells the binding system that this property holds an Integration Service connection.

```csharp
[ConnectionBinding]
public InArgument<string> ConnectionId { get; set; }
```

---

## ViewModel-Side Setup

In the ViewModel's `InitializeModel` or `InitializeModelAsync`, assign the `Connection` widget:

```csharp
protected override void InitializeModel()
{
    base.InitializeModel();

    ConnectionId.Widget = new DefaultWidget
    {
        Type = ViewModelWidgetType.Connection,
        Metadata = new() { { nameof(Connector), GetConnector() } }
    };
    ConnectionId.IsPrincipal = true;
    ConnectionId.IsRequired = true;
}
```

---

## Binding Registration

Connections participate in the bindings system. Register bindings in the registration class so Orchestrator/Assistant can resolve connections at runtime:

```csharp
protected override void RegisterBindings(IWorkflowDesignApi api, bool enabled)
{
    if (!api.HasFeature(DesignFeatureKeys.PackageBindingsV3)) return;

    // Register connections, event triggers, etc.
}
```

For dependent properties that change based on the selected connection (e.g., account ID, folder ID), use `[PropertyBinding]`:

```csharp
// On the activity class
[PropertyBinding(nameof(ConnectionId))]
public InArgument<string> AccountId { get; set; }

[PropertyBinding(nameof(ConnectionId), SubKey = "folder")]
public InArgument<string> FolderId { get; set; }
```

---

## Conditional Compilation

The Integration Service client may not be available in all build configurations. Use conditional compilation:

```csharp
#if INTEGRATION_SERVICE
    ConnectionId.Widget = new DefaultWidget
    {
        Type = ViewModelWidgetType.Connection,
        Metadata = new() { { nameof(Connector), GetConnector() } }
    };
#endif
```

The `INTEGRATION_SERVICE` symbol is defined when the connection client assembly is available.

---

## Troubleshooting

**Connection picker shows no connections.**
The picker queries Integration Service for connections matching the connector type. If no connections are configured in the Orchestrator tenant, the picker will be empty. Verify that the connector is set up in Integration Service and that the user has access.

**GetConnector() returns null or empty.**
Ensure the connector identifier is correctly defined. The `GetConnector()` method should return the Integration Service connector key (e.g., `"UiPath.GSuite.Gmail"`, `"UiPath.Salesforce"`). Check your package's connector registration.

**Connection value is null at runtime.**
If the automation runs outside Orchestrator (e.g., local Studio run), the connection binding may not resolve. Ensure the activity handles the case where `ConnectionId` is null and provides a meaningful error message.

**Build error: `ViewModelWidgetType.Connection` not found.**
Ensure the project references the correct ViewModel SDK assembly. The `Connection` widget type is defined in the design-time SDK.
