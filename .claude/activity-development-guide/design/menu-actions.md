# Menu Actions

> **When to read this**: You are adding contextual buttons, links, or mode-switching toggles to activity properties in the designer panel.

**Cross-references**: [ViewModel](viewmodel.md) | [Rules and Dependencies](rules-and-dependencies.md)

---

## Basic Menu Action

```csharp
Property.AddMenuAction(new MenuAction
{
    DisplayName = "Open Settings",
    Handler = async _ => await OpenSettingsAsync()
});
```

---

## Main (Prominent) Menu Action

A main action is displayed as a primary action button, visually prominent.

```csharp
Property.AddMenuAction(new MenuAction
{
    DisplayName = "Import Arguments",
    IsMain = true,  // Displayed as primary action button
    Handler = async _ => await ImportArgumentsAsync()
});
```

---

## Menu Action with Icon

```csharp
Property.AddMenuAction(new MenuAction
{
    Id = "test-action",
    DisplayName = "Test",
    Handler = HandleTestAsync,
    Metadata = new Dictionary<string, string>
    {
        { MenuActionMetadataConstants.MaterialIconName, "lightbulb" }
    }
});
```

---

## Menu Action with URL (Studio Web)

For actions that open a URL in Studio Web but use a local handler in Studio Desktop:

```csharp
Property.AddMenuAction(new MenuAction
{
    DisplayName = "Manage in Orchestrator",
    IsMain = true,
    Url = _isStudioWeb ? orchestratorUrl : null,
    Handler = _isStudioWeb ? null : ManageLocalHandler
});
```

---

## Mode-Switching with MenuActionsService

For properties that toggle between input modes (e.g., Dictionary vs Variable):

```csharp
var menuService = new MenuActionsService(ModelItem);
menuService
    .RegisterProperty(DictionaryInput, "Use Dictionary", isDefault: true)
    .RegisterProperty(VariableInput, "Use Variable", isDefault: false)
    .SetDefaultProperty(DictionaryInput)
    .Build();
```

---

## Manual Mode Switching

If you need full control over mode toggling instead of using `MenuActionsService`:

```csharp
DictionaryInput.AddMenuAction(new MenuAction
{
    DisplayName = "Use Variable",
    Handler = _ =>
    {
        DictionaryInput.IsVisible = false;
        VariableInput.IsVisible = true;
        DictionaryInput.Value = null;
        return Task.CompletedTask;
    }
});

VariableInput.AddMenuAction(new MenuAction
{
    DisplayName = "Use Dictionary",
    Handler = _ =>
    {
        VariableInput.IsVisible = false;
        DictionaryInput.IsVisible = true;
        VariableInput.Value = null;
        return Task.CompletedTask;
    }
});
```

---

## Troubleshooting

<!-- AI agents: add entries here when you encounter issues related to this topic -->
