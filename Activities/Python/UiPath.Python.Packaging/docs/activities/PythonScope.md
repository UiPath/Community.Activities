# Python Scope

`UiPath.Python.Activities.PythonScope`

Container activity that initializes and manages the Python runtime session for child Python activities.

**Package:** `UiPath.Python.Activities`
**Category:** App Invoker.Python

> **Supported runtimes:** 64-bit Python **3.10–3.14**. Python versions older than 3.10 and 32-bit (x86) installations are no longer supported and raise a validation error at runtime.

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `InstalledVersions` | Installed Python Versions | Property (design-time only) | `string` |  | null | No Python installations were detected | Design-time helper dropdown listing the Python installations detected on the machine (via the `py` launcher). Selecting one auto-fills `Path` and `LibraryPath`. Not mapped to an activity argument and not persisted to the workflow. |
| `Path` | Path | InArgument | `string` |  | null |  | Python home path. Optional — when empty or null, the `PYTHONHOME` environment variable is used instead. |
| `LibraryPath` | Library path | InArgument | `string` | ✓ | null |  | Required. Full path to the Python runtime library including the file name — `python**.dll` on Windows (e.g. `python313.dll`, usually in the Python home folder), `libpython*.so` on Linux, or `libpython*.dylib` on macOS. pythonnet uses it to locate the runtime; an empty or missing file raises an error. |
| `OperationTimeout` | Timeout | InArgument | `double` |  | 3600 |  | The amount of time in seconds to allow a Python script to run until it is terminated and an exception is thrown. |
| `WorkingFolder` | WorkingFolder | InArgument | `string` |  | null |  | Used to specify the working folder of the scripts executing under the current scope |
| `ScriptDataSizeLimitMB` | Script Data Size Limit (MB) | InArgument | `int` |  | null |  | Maximum size in MB of the data passed to the Python script as method arguments. If the size of the arguments exceeds this limit, an error is raised. Minimum accepted value is 1 MB. Leave empty to use the runtime default (25 MB). |
| `LogTraces` | Log Python Output to File (Diagnostic) | Property | `bool` |  | false (disabled) |  | When enabled, stdout/stderr from the Python host process is written to a per-host log file under the folder resolved by `Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)`, in the `UiPath\Logs\python` subdirectory. Each log file is capped at 50 MB; once the cap is reached, no further output is written to that file. At most 128 log files are kept — the oldest are automatically deleted when a new file is created. Output is NOT forwarded to Orchestrator. Intended for local diagnosis only — leave disabled in production to avoid accumulating log files. |

### Configuration (Deprecated)

> The `Version` and `TargetPlatform` properties are obsolete and hidden in Studio (`[Browsable(false)]` + `[Obsolete]`). The Python version is now detected automatically from the installation at `Path`/`LibraryPath`, and only 64-bit in-process execution is supported. These properties remain solely for backward compatibility with existing workflows and **have no effect**.

| Name | Display Name | Type | Default | Status |
|------|-------------|------|---------|--------|
| `Version` | Version | `Version` | Version.Auto | Deprecated — hidden, no effect (version detected automatically). |
| `TargetPlatform` | Target | `TargetPlatform` | TargetPlatform.x64 | Deprecated — hidden, no effect (only 64-bit supported). |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| - | - | - | - | - |

## XAML Example

```xml
<py:PythonScope Path="[pythonHome]" LibraryPath="[pythonDllPath]" />
```

> **Leave `ScriptDataSizeLimitMB` and `LogTraces` unset in production workflows.**
> - Set `ScriptDataSizeLimitMB` only when a specific workflow needs payloads larger than the 25 MB default.
> - Enable `LogTraces` only temporarily for local diagnosis; disable it again before deploying. Logs land under `Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)\UiPath\Logs\python`, are capped at 50 MB per file, and at most 128 files are retained. Output is NOT forwarded to Orchestrator.
> - Do not set the deprecated `Version` / `TargetPlatform` properties — they are ignored.
