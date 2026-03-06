# ActionButton Widget

## When to read this

Read this document when you need to:
- Add a clickable button to the activity designer
- Trigger an action (open a dialog, run configuration, refresh data) from the designer
- Wire up button click handling via Rules and `RegisterDependency`

---

## Overview

The `ActionButton` widget renders a clickable button in the activity designer. When clicked, it triggers a Rule that executes your custom logic. Common uses include launching configuration dialogs, refreshing data from a server, or running validation.

Use with `DesignProperty<T>` (the property type is typically not meaningful -- the button is the interaction point, not the value).

---

## Basic Usage

```csharp
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.ActionButton };
Property.DisplayName = "Click Me";
Property.IsPrincipal = true;

// Handle the click via a Rule
Rule("ActionProperty", () => DoSomething(), false);
RegisterDependency(Property, nameof(DesignProperty.Value), "ActionProperty");
```

### How It Works

1. The `ActionButton` widget renders a button labeled with `Property.DisplayName`.
2. When the user clicks the button, the framework changes the property's `Value`, which triggers the registered dependency.
3. The dependency fires the Rule named `"ActionProperty"`.
4. Your Rule handler (`DoSomething()`) executes.

---

## Step-by-Step Setup

### 1. Define the property

```csharp
public DesignProperty<bool> ConfigureButton { get; set; } = new();
```

### 2. Configure the widget in InitializeModel

```csharp
protected override void InitializeModel()
{
    base.InitializeModel();

    ConfigureButton.Widget = new DefaultWidget { Type = ViewModelWidgetType.ActionButton };
    ConfigureButton.DisplayName = "Configure Fields";
    ConfigureButton.IsPrincipal = true;
    ConfigureButton.OrderIndex = PropertyOrderIndex++;
}
```

### 3. Register the click handler

```csharp
protected override void RegisterDependencies()
{
    base.RegisterDependencies();

    Rule("ConfigureButtonClicked", () =>
    {
        // Your action logic here
        OpenConfigurationDialog();
    }, false);

    RegisterDependency(ConfigureButton, nameof(DesignProperty.Value), "ConfigureButtonClicked");
}
```

The third parameter of `Rule()` (`false`) means the rule does not run during initialization -- it only runs when the user clicks.

---

## Examples

### Open a configuration dialog

```csharp
Rule("OpenDialog", async () =>
{
    var dialogService = Services.GetService<IDialogService>();
    var result = await dialogService.ShowDialogAsync(new MyConfigDialog(currentConfig));
    if (result != null)
    {
        ApplyConfiguration(result);
    }
}, false);

RegisterDependency(ConfigureButton, nameof(DesignProperty.Value), "OpenDialog");
```

### Refresh data from server

```csharp
Rule("RefreshData", async () =>
{
    var busyService = Services.GetService<IBusyService>();
    using (busyService.ShowBusy("Loading data..."))
    {
        var data = await _apiClient.FetchDataAsync();
        UpdateDataSource(data);
    }
}, false);

RegisterDependency(RefreshButton, nameof(DesignProperty.Value), "RefreshData");
```

---

## Troubleshooting

**Button click does nothing.**
Verify that `RegisterDependency` is called with the correct property, property name (`nameof(DesignProperty.Value)`), and rule name. The rule name in `RegisterDependency` must exactly match the name in `Rule()`.

**Button handler runs during initialization.**
Set the third parameter of `Rule()` to `false` to prevent the rule from running when the ViewModel initializes. If set to `true`, the handler runs once at startup.

**Button is not visible.**
Check that `Property.IsPrincipal = true` is set. Non-principal properties may be hidden in collapsed sections of the designer. Also verify `Property.IsVisible` is not set to `false`.
