# Python Scope

`UiPath.Python.Activities.PythonScope`

Container activity that initializes and manages the Python runtime session for child Python activities.

**Package:** `UiPath.Python.Activities`
**Category:** App Invoker.Python

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `LibraryPath` | Library path (Linux or version>3.9) | InArgument | `string` |  | null |  | For Linux is the path to Python libpython*.so library including library name. For Windows (Version>3.9) path to python**.dll including library name(usually is in Python Home path. For Windows (Version<=3.9) leave empty. |
| `OperationTimeout` | Timeout | InArgument | `double` |  | 3600 |  | The amount of time to allow a Python script to run until it is terminated and an exception is thrown. |
| `Path` | Path | InArgument | `string` |  | null |  | Python home path |
| `WorkingFolder` | WorkingFolder | InArgument | `string` |  | null |  | Used to specify the working folder of the scripts executing under the current scope |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `Version` | Version | `Version` | Version.Auto | Python version to use. Set to Auto to detect automatically. |
| `TargetPlatform` | Target | `TargetPlatform` | TargetPlatform.x64 | Specifies the Python runtime platform |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| - | - | - | - | - |

## XAML Example

```xml
<py:PythonScope Path="[pythonHome]" Version="Auto" TargetPlatform="x64" />
```
