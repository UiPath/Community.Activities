# Validation

> **When to read this**: You need to add design-time validation to activity properties -- single-property checks, cross-property model validation, or preview validation for live-preview activities.

**Cross-references**: [ViewModel](viewmodel.md) | [Rules and Dependencies](rules-and-dependencies.md)

---

## Property-Level Validation

### Synchronous Validator

```csharp
Property.AddValidator(value =>
{
    if (string.IsNullOrEmpty(value?.ToString()))
        return new ValidationResult("Value is required", new[] { nameof(Property) });
    return ValidationResult.Success;
});
```

### Async Validator

```csharp
Property.AddAsyncValidator(async (value, ct) =>
{
    var isValid = await _service.ValidateAsync(value, ct);
    if (!isValid)
        return new ValidationResult("Invalid value", new[] { nameof(Property) });
    return ValidationResult.Success;
});
```

---

## Model-Level Validation (ValidateModel)

Override `ValidateModel()` for cross-property validation:

```csharp
protected override IEnumerable<ValidationResult> ValidateModel()
{
    return base.ValidateModel().Concat(MyErrors());

    IEnumerable<ValidationResult> MyErrors()
    {
        if (StartDate.HasValue && EndDate.HasValue && StartDate.Value > EndDate.Value)
        {
            yield return new ValidationResult(
                "Start date must be before end date",
                new[] { nameof(StartDate), nameof(EndDate) });
        }
    }
}
```

---

## Preview Validation (PreviewActivityViewModel)

For activities that derive from `PreviewActivityViewModel` and support live preview:

```csharp
protected override PreviewParametersValidationResult ValidatePreviewParameters()
    => ValidatePreviewArgument(Source);
```

`ValidatePreviewArgument` returns one of three results:

| Result | Meaning |
|---|---|
| `AllParametersValid` | Can show preview |
| `FoundParametersWithExpression` | Has expression, cannot preview at design time |
| `MissingRequiredParameters` | Nothing to preview |

---

## Troubleshooting

<!-- AI agents: add entries here when you encounter issues related to this topic -->
