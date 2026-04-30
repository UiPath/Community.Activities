# Get Python Object

`UiPath.Python.Activities.GetObject<T>`

Gets a .NET value from a Python object reference.

**Package:** `UiPath.Python.Activities`
**Category:** App Invoker.Python

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `PythonObject` | Python object | InArgument | `PythonObject` | Yes |  |  | Python object to convert to a .NET value. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| - | - | - | - | - |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Result` | Result | OutArgument | `T` | Converted .NET value. |

## XAML Example

```xml
<py:GetObject x:TypeArguments="x:String" PythonObject="[pyObj]" Result="[textResult]" />
```
