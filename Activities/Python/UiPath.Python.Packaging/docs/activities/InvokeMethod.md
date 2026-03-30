# Invoke Python Method

`UiPath.Python.Activities.InvokeMethod`

Invokes a method on a Python object instance and returns the method result.

**Package:** `UiPath.Python.Activities`
**Category:** App Invoker.Python

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `Instance` | Instance | InArgument | `PythonObject` |  | null |  | The Python object instance on which to invoke the method. Leave empty for module-level calls. |
| `Name` | Name | InArgument | `string` | Yes |  |  | Name of the method to be invoked |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `Parameters` | InputParameters | `InArgument<IEnumerable<object>>` |  | Input parameters for Python script |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Result` | Result | OutArgument | `PythonObject` | The result of script invocation |

## XAML Example

```xml
<py:InvokeMethod Instance="[pyObj]" Name="[methodName]" Parameters="[args]" Result="[methodResult]" />
```
