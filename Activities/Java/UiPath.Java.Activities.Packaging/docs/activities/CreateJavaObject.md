# Create Java Object

`UiPath.Java.Activities.CreateJavaObject`

Creates a Java object by calling a constructor on the specified target type.

**Package:** `UiPath.Java.Activities`
**Category:** Java
**Required Scope:** `UiPath.Java.Activities.JavaScope`
**Platform:** Cross-platform

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Description |
|------|-------------|------|------|----------|---------|-------------|
| `TargetType` | Target Type | InArgument | `string` | Yes |  | Fully qualified Java class name to instantiate. |
| `Parameters` | Parameters | Property | `List<InArgument>` |  | `[]` | Constructor arguments declared individually. |
| `ParametersList` | Parameters List | InArgument | `List<object>` |  |  | Constructor arguments supplied as a single list. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Result` | Result | OutArgument | `JavaObject` | Java object instance created by the constructor call. |

## Valid Configurations

Use one input pattern for constructor arguments:

- Set `Parameters` with individual argument expressions.
- Or set `ParametersList` with a list object.

Do not set both `Parameters` and `ParametersList` at the same time.

## XAML Example

```xml
<java:JavaScope DisplayName="Java Scope">
  <java:CreateJavaObject DisplayName="Create Java Object"
                         TargetType="[&quot;com.example.Customer&quot;]"
                         ParametersList="[new List(Of Object) From {&quot;Alice&quot;, 42}]"
                         Result="[customerObj]" />
</java:JavaScope>
```
