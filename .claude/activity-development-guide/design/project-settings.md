# Project Settings

> **When to read this**: You are exposing configurable default values at the Studio project level -- for example, a default timeout, API URL, or server address that applies to all instances of your activities in a project.

**Cross-references**: [ViewModel](viewmodel.md) | [Metadata](metadata.md) | [Validation](validation.md)

---

## Defining Settings on Activity Properties

Use `ArgumentSettingAttribute` on Activity class properties to declare them as project-level settings:

```csharp
// On the Activity class:
[ArgumentSetting("MyActivity", "DefaultTimeout", defaultValue: "30", required: false)]
public InArgument<int> Timeout { get; set; }

[ArgumentSetting("MyActivity", "ApiUrl", required: true, packageKey: "MyPackage")]
public InArgument<string> ApiUrl { get; set; }
```

The `ArgumentSettingAttribute` automatically generates a settings key like:
`UiPath.Sdk.Activities.{section}.{property}`

---

## Publishing Settings at Registration

```csharp
protected override void PublishSettings(IWorkflowDesignApi api, bool enabled)
{
    if (!api.HasFeature(DesignFeatureKeys.Settings)) return;

    var settings = new List<ProjectSettingsSection>();

    var section = new ProjectSettingsSection
    {
        Key = "MyActivity",
        Title = "My Activity Settings",
        Description = "Configure default values"
    };
    section.Add("DefaultTimeout", "Default Timeout (seconds)", defaultValue: 30);
    section.Add("ApiUrl", "API URL", defaultValue: "https://api.example.com");
    settings.Add(section);

    api.PublishProjectSettings(
        ArgumentSettingAttribute.SettingsPrefix,
        "My Activities",
        "Default settings for My Activities package",
        settings);
}
```

---

## Validating Settings in ViewModel

```csharp
protected override IEnumerable<ValidationResult> ValidateModel()
{
    return base.ValidateModel().Concat(
        DesignServices.ProjectSettings?.Validate(
            activity: ModelItem?.GetCurrentValue(),
            messageFormatter: propName => $"Setting '{propName}' is required")
        ?? Enumerable.Empty<ValidationResult>());
}
```

---

## Troubleshooting

<!-- AI agents: add entries here when you encounter issues related to this topic -->
