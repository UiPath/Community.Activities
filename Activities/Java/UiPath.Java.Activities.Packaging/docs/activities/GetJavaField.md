# Get Java Field

`UiPath.Java.Activities.GetJavaField`

Gets a field value from a Java object instance or from a static Java class field.

**Package:** `UiPath.Java.Activities`
**Category:** Java
**Required Scope:** `UiPath.Java.Activities.JavaScope`
**Platform:** Cross-platform

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Description |
|------|-------------|------|------|----------|---------|-------------|
| `FieldName` | Field Name | InArgument | `string` | Yes |  | Java field name to read. |
| `TargetObject` | Target Object | InArgument | `JavaObject` | Conditional |  | Java object instance used for instance-field access. |
| `TargetType` | Target Type | InArgument | `string` | Conditional |  | Java class name used for static-field access. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Result` | Result | OutArgument | `JavaObject` | Java object representing the field value. |

## Valid Configurations

This activity supports two valid target modes:

- Instance mode: set `TargetObject` and leave `TargetType` empty.
- Static mode: set `TargetType` and leave `TargetObject` empty.

`FieldName` is required in both modes.

## XAML Example

```xml
<java:JavaScope DisplayName="Java Scope">
  <java:GetJavaField DisplayName="Get Java Field"
                     FieldName="[&quot;Name&quot;]"
                     TargetObject="[customerObj]"
                     Result="[fieldValue]" />
</java:JavaScope>
```
