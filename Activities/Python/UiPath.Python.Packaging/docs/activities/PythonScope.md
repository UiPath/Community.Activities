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
| `OperationTimeout` | Timeout | InArgument | `double` |  | 3600 |  | The amount of time in seconds to allow a Python script to run until it is terminated and an exception is thrown. |
| `Path` | Path | InArgument | `string` |  | null |  | Python home path |
| `WorkingFolder` | WorkingFolder | InArgument | `string` |  | null |  | Used to specify the working folder of the scripts executing under the current scope |
| `ScriptDataSizeLimitMB` | Script Data Size Limit (MB) | InArgument | `int` |  | null |  | Maximum size in MB of the data passed to the Python script as method arguments. If the size of the arguments exceeds this limit, an error is raised. Minimum accepted value is 1 MB. Leave empty to use the runtime default (25 MB). |
| `LogTraces` | Log Python Output to File (Diagnostic) | Property | `bool` |  | false (disabled) |  | When enabled, stdout/stderr from the Python host process is written to a per-host log file under the folder resolved by `Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)`, in the `UiPath\Logs\python` subdirectory. Each log file is capped at 50 MB; once the cap is reached, no further output is written to that file. At most 128 log files are kept — the oldest are automatically deleted when a new file is created. Output is NOT forwarded to Orchestrator. Intended for local diagnosis only — leave disabled in production to avoid accumulating log files. |

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

> **Leave `ScriptDataSizeLimitMB` and `LogTraces` unset in production workflows.**
> - Set `ScriptDataSizeLimitMB` only when a specific workflow needs payloads larger than the 25 MB default.
> - Enable `LogTraces` only temporarily for local diagnosis; disable it again before deploying. Logs land under `Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)\UiPath\Logs\python`, are capped at 50 MB per file, and at most 128 files are retained. Output is NOT forwarded to Orchestrator.
