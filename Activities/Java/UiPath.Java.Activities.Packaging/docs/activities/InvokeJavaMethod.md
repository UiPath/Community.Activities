# Invoke Java Method

`UiPath.Java.Activities.InvokeJavaMethod`

Invokes a Java method on an instance object or as a static class method, with optional parameters.

**Package:** `UiPath.Java.Activities`
**Category:** Java
**Required Scope:** `UiPath.Java.Activities.JavaScope`
**Platform:** Windows only

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Description |
|------|-------------|------|------|----------|---------|-------------|
| `MethodName` | Method Name | InArgument | `string` | Yes |  | Java method name to invoke. |
| `TargetObject` | Target Object | InArgument | `JavaObject` | Yes* |  | Java object instance used for instance method invocation. |
| `TargetType` | Target Type | InArgument | `string` | Yes* |  | Java class name used for static method invocation. |
| `Parameters` | Parameters | Property | `List<InArgument>` |  | `[]` | Method arguments declared individually. |
| `ParametersList` | Parameters List | InArgument | `List<object>` |  |  | Method arguments supplied as a single list. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Result` | Result | OutArgument | `JavaObject` | Return value of the Java method, wrapped as a Java object. |

## Valid Configurations

Target selection:

- Instance mode: set `TargetObject`.
- Static mode: set `TargetType`.

Parameter selection:

- Use `Parameters` for individual argument expressions.
- Or use `ParametersList` for a single list input.

Do not set both `Parameters` and `ParametersList` at the same time.

## XAML Example

```xml
<java:JavaScope DisplayName="Java Scope">
  <java:InvokeJavaMethod DisplayName="Invoke Java Method"
                         MethodName="[&quot;setName&quot;]"
                         TargetObject="[customerObj]"
                         ParametersList="[new List(Of Object) From {&quot;Bob&quot;}]"
                         Result="[methodResult]" />
</java:JavaScope>
```
