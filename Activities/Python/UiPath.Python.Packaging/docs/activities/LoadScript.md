# Load Python Script

`UiPath.Python.Activities.LoadScript`

Loads and executes Python script code or a script file and returns a Python object result.

**Package:** `UiPath.Python.Activities`
**Category:** App Invoker.Python

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `Code` | Code | InArgument | `string` |  |  |  | Python script content. Provide either `Code` or `ScriptFile`. |
| `ScriptFile` | Load Python Script | InArgument | `string` |  |  |  | Path to a Python script file to load and execute. Provide either `Code` or `ScriptFile`. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| - | - | - | - | - |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Result` | Result | OutArgument | `PythonObject` | The result of script invocation |

## Valid Configurations

- Use exactly one overload input: `Code` or `ScriptFile`.
- Do not set both `Code` and `ScriptFile` at the same time.

## XAML Example

```xml
<py:LoadScript ScriptFile="[scriptPath]" Result="[pyObj]" />
```
