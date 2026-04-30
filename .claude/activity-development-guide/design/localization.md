# Localization

> **When to read this**: You are adding user-visible strings (display names, tooltips, descriptions, categories) to an activity or its properties.

**Cross-references**: [ViewModel](viewmodel.md) | [Metadata](metadata.md)

---

## Resource File Convention

All user-visible strings go in `Resources.resx`:

```xml
<!-- Activity-level strings -->
<data name="Calculator_DisplayName" xml:space="preserve">
  <value>Calculator</value>
  <comment>Activity display name</comment>
</data>
<data name="Calculator_Description" xml:space="preserve">
  <value>Performs basic arithmetic operations</value>
  <comment>Activity description</comment>
</data>

<!-- Property-level strings -->
<data name="Calculator_FirstNumber_DisplayName" xml:space="preserve">
  <value>First Number</value>
  <comment>Property display name</comment>
</data>
<data name="Calculator_FirstNumber_Tooltip" xml:space="preserve">
  <value>The first operand for the calculation</value>
  <comment>Property tooltip</comment>
</data>

<!-- Category strings -->
<data name="Input" xml:space="preserve">
  <value>Input</value>
</data>
<data name="Output" xml:space="preserve">
  <value>Output</value>
</data>
```

---

## Naming Convention

```
{ActivityName}_DisplayName                -> Activity display name
{ActivityName}_Description                -> Activity description
{ActivityName}_{PropertyName}_DisplayName -> Property display name
{ActivityName}_{PropertyName}_Tooltip     -> Property tooltip/description
```

---

## Usage in ViewModel

```csharp
Property.DisplayName = Resources.Calculator_FirstNumber_DisplayName;
Property.Tooltip = Resources.Calculator_FirstNumber_Tooltip;
Property.Category = Resources.Input;
```

---

## Troubleshooting

<!-- AI agents: add entries here when you encounter issues related to this topic -->
