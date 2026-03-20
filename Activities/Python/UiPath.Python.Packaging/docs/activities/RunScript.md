# Run Python Script

`UiPath.Python.Activities.RunScript`

Executes Python script code or a script file inside an active Python runtime scope.

**Package:** `UiPath.Python.Activities`
**Category:** App Invoker.Python

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `Code` | Code | InArgument | `string` | Yes |  |  | Python script content |
| `ScriptFile` | Run Python Script | InArgument | `string` | Yes |  |  | Invoke Python script activity |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| - | - | - | - | - |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| - | - | - | - | - |

## Valid Configurations

- Use exactly one overload input: `Code` or `ScriptFile`.
- Do not set both `Code` and `ScriptFile` at the same time.

## XAML Example

```xml
<py:RunScript Code="[&quot;print('hello')&quot;]" />
```