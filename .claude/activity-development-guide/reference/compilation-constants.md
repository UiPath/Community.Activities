# Conditional Compilation Constants

> **When to read this:** You are writing code that must behave differently depending on the target
> platform or available infrastructure (e.g., Windows-only code, Integration Service features, or
> governance checks). Use these symbols in `#if` / `#endif` blocks.

## Cross-references

- Activity class patterns: [../core/activity-classes.md](../core/activity-classes.md)
- Feature flags (design-time detection): [feature-flags.md](feature-flags.md)
- Complete example: [../examples/complete-example.md](../examples/complete-example.md)

---

## Compilation constants table

| Symbol | Defined When | Purpose |
|---|---|---|
| `NETPORTABLE_UIPATH` | Targeting net5.0 or net6.0 (but **not** net6.0-windows) | Guard Windows-specific code so it compiles on portable/cross-platform targets |
| `INTEGRATION_SERVICE` | Integration Service connection client is available | Enable Integration Service support (connection widgets, token acquisition) |
| `GOVERNANCE_SERVICE` | Governance infrastructure is available | Enable runtime governance checks and policy enforcement |

## Usage examples

### Guarding Windows-specific code

```csharp
public void ConfigurePlatform()
{
#if !NETPORTABLE_UIPATH
    // Windows-only: use Win32 APIs, WPF, etc.
    SetWindowsRegistryKey("MyApp", "Setting", value);
#else
    // Portable path: use cross-platform alternative
    SetConfigFile("MyApp", "Setting", value);
#endif
}
```

### Conditional Integration Service support

```csharp
protected override void InitializeModel()
{
    base.InitializeModel();

#if INTEGRATION_SERVICE
    Connection.Widget = new DefaultWidget
    {
        Type = ViewModelWidgetType.Connection
    };
    Connection.DisplayName = "Connection";
#endif
}
```

### Conditional governance checks

```csharp
protected override void Execute(CodeActivityContext context)
{
#if GOVERNANCE_SERVICE
    var governance = context.GetExtension<IGovernanceService>();
    governance?.ValidatePolicy(this);
#endif

    // Normal execution logic
}
```

## Notes

- These symbols are defined at the project level (in `.csproj` or `Directory.Build.props`) based on
  build configuration and target framework.
- Unlike design-time feature flags (checked via `IWorkflowDesignApi.HasFeature()`), compilation
  constants are resolved at build time and result in different compiled assemblies.
- When adding platform-specific behavior, prefer compilation constants over runtime checks to avoid
  pulling in unnecessary dependencies on unsupported platforms.
