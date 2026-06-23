# Python — Coded Workflow API

`UiPath.Python.Activities`

Provides coded workflow operations for running Python scripts, executing inline code, and interoperating with Python objects from .NET.

**Service accessor:** `python` (type `IPythonService`)
**Required package:** `"UiPath.Python.Activities": "*"` in project.json dependencies

## Auto-Imported Namespaces

These namespaces are automatically available in coded workflows when this package is installed:

```
System
System.Collections.Generic
UiPath.Python
UiPath.Python.Activities
UiPath.Python.Activities.API
UiPath.Python.Activities.API.Models
```

## Service Overview

The `python` service provides a handle-based API for Python scripting. You open a Python scope via the service, receive a disposable handle, then call extension methods on the handle to run scripts, execute code, and interoperate with Python objects.

`IPythonScopeHandle` implements both `IDisposable` and `IAsyncDisposable` — prefer `await using` in async workflows to release the Python engine cleanly.

---

## Opening a Python Scope

### `Task<IPythonScopeHandle> UsePythonScope(PythonScopeOptions options, CancellationToken ct = default)`

Creates and initializes a Python scope configured with the given options.

**Parameters:**
- `options` (`PythonScopeOptions`) — Options for configuring the Python installation, version, library paths, working folder, and timeout
- `ct` (`CancellationToken`) — Cancellation token (default: `default`)

**Returns:** `Task<IPythonScopeHandle>` — Awaitable task producing a disposable handle to the active Python scope. Use with `await using` statement.

---

## Handle Type: `IPythonScopeHandle`

Disposable handle to an initialized Python scope. Operations are available as extension methods in `UiPath.Python.Activities.API.PythonOperations` and are called directly on the handle.

> This type implements `IDisposable` and `IAsyncDisposable`. Always use inside an `await using` statement or call `DisposeAsync()` explicitly to ensure the Python engine is released.

### Property

| Property | Type | Description |
|----------|------|-------------|
| `Engine` | `IEngine` | The underlying Python engine. Provides direct access to low-level engine operations. |

### Extension Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `RunScript(string scriptFile, CancellationToken ct = default)` | `Task` | Reads and executes a Python script from a file path. |
| `RunCode(string code, CancellationToken ct = default)` | `Task` | Executes inline Python code. |
| `LoadScript(string scriptFile, CancellationToken ct = default)` | `Task<PythonObject>` | Reads and loads a Python script from a file, returning a handle to the script's global scope for method invocation. |
| `LoadCode(string code, CancellationToken ct = default)` | `Task<PythonObject>` | Loads inline Python code, returning a handle to the script's global scope for method invocation. |
| `InvokeMethod(PythonObject instance, string methodName, IEnumerable<object> parameters = null, CancellationToken ct = default)` | `Task<PythonObject>` | Invokes a named method on a loaded Python script instance. |
| `GetObject<T>(PythonObject pythonObject)` | `T` | Converts a `PythonObject` to the specified .NET type. |

---

## Method Reference

### `Task RunScript(string scriptFile, CancellationToken ct = default)`

Reads and executes a Python script file. Use this when you only need side effects (e.g., installing packages, writing files, printing output) and do not need to call specific functions or retrieve return values.

**Parameters:**
- `scriptFile` (`string`) — Path to the Python `.py` script file
- `ct` (`CancellationToken`) — Cancellation token (default: `default`)

**Returns:** `Task`

---

### `Task RunCode(string code, CancellationToken ct = default)`

Executes inline Python code. Use this for short, self-contained scripts defined directly in the workflow.

**Parameters:**
- `code` (`string`) — The Python source code to execute
- `ct` (`CancellationToken`) — Cancellation token (default: `default`)

**Returns:** `Task`

---

### `Task<PythonObject> LoadScript(string scriptFile, CancellationToken ct = default)`

Reads and loads a Python script file, returning a `PythonObject` handle to the script's global scope. Use this to set up a module whose functions you will invoke via `InvokeMethod`.

**Parameters:**
- `scriptFile` (`string`) — Path to the Python `.py` script file
- `ct` (`CancellationToken`) — Cancellation token (default: `default`)

**Returns:** `Task<PythonObject>` — Handle to the loaded script's global scope. Pass to `InvokeMethod` to call functions defined in the script.

---

### `Task<PythonObject> LoadCode(string code, CancellationToken ct = default)`

Loads inline Python code, returning a `PythonObject` handle to the script's global scope. Use this to define a module inline and then call specific functions from it.

**Parameters:**
- `code` (`string`) — The Python source code to load
- `ct` (`CancellationToken`) — Cancellation token (default: `default`)

**Returns:** `Task<PythonObject>` — Handle to the loaded script's global scope. Pass to `InvokeMethod` to call functions defined in the code.

---

### `Task<PythonObject> InvokeMethod(PythonObject instance, string methodName, IEnumerable<object> parameters = null, CancellationToken ct = default)`

Invokes a named function or method on a loaded Python script instance.

**Parameters:**
- `instance` (`PythonObject`) — The Python object handle returned by `LoadScript` or `LoadCode`
- `methodName` (`string`) — The name of the Python function to invoke
- `parameters` (`IEnumerable<object>`) — Optional parameters to pass to the function (default: `null`)
- `ct` (`CancellationToken`) — Cancellation token (default: `default`)

**Returns:** `Task<PythonObject>` — The function's return value wrapped as a `PythonObject`. Convert to a .NET type using `GetObject<T>`.

---

### `T GetObject<T>(PythonObject pythonObject)`

Converts a `PythonObject` returned by `InvokeMethod` or `LoadScript`/`LoadCode` to a .NET type.

**Type parameters:**
- `T` — The target .NET type (e.g., `string`, `int`, `double`, `List<object>`)

**Parameters:**
- `pythonObject` (`PythonObject`) — The Python object to convert

**Returns:** `T` — The converted .NET value.

---

## Return Types

### `PythonObject`

An opaque, disposable wrapper around a Python object. Returned by `LoadScript`, `LoadCode`, and `InvokeMethod`.

> This type implements `IDisposable`. When holding long-lived `PythonObject` instances, dispose them explicitly to release the underlying Python memory.

| Member | Type | Description |
|--------|------|-------------|
| `Id` | `Guid` | Unique identifier for this Python object instance. |

Pass a `PythonObject` to `GetObject<T>` to convert it to a usable .NET value, or pass it back to `InvokeMethod` as the `instance` argument.

---

## Options & Configuration

### `PythonScopeOptions`

Options for configuring a Python scope passed to `UsePythonScope`.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Path` | `string` | — | Path to the Python installation directory. Optional — when omitted, the `PYTHONHOME` environment variable is used. |
| `LibraryPath` | `string` | — (**required**) | **Required.** Full path to the versioned Python runtime library including the file name — `python**.dll` on Windows (e.g. `python313.dll`), `libpython*.so` on Linux, or `libpython*.dylib` on macOS. pythonnet uses it to locate the runtime; an empty or missing file raises `FileNotFoundException`. |
| `Version` | `Version` | `Version.Auto` | **Deprecated — has no effect.** The Python version is detected automatically from the installation at `Path`/`LibraryPath`. Retained only for backward compatibility. |
| `WorkingFolder` | `string` | — | Working directory for the Python process. Relative imports in scripts resolve from this path. |
| `OperationTimeout` | `TimeSpan?` | `null` (1 hour) | Maximum time to wait for Python operations to complete. When `null`, defaults to 1 hour. |
| `Target` | `TargetPlatform` | `TargetPlatform.x64` | **Deprecated — has no effect.** Only 64-bit in-process execution is supported. Retained only for backward compatibility. |
| `LogTraces` | `bool` | `false` | When `true`, stdout/stderr from the Python host process is written to a per-host diagnostic log file under the folder resolved by `Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)`, in the `UiPath\Logs\python` subdirectory. Each log file is capped at 50 MB; once the cap is reached, no further output is written to that file. At most 128 log files are kept — the oldest are automatically deleted when a new file is created. Output is NOT forwarded to Orchestrator — intended for local diagnosis only. |
| `ScriptDataSizeLimitMB` | `int?` | `null` (25 MB) | Maximum request payload size in megabytes sent to the Python host. When `null`, defaults to the engine default (25 MB). Must be at least 1 if specified. |

---

## Enum Reference

### `Version`

> **Deprecated — has no effect.** The Python version is now detected automatically from the installation. This enum is retained only so existing serialized workflows continue to deserialize.

| Value | Description |
|-------|-------------|
| `Auto` | Automatically detect the installed Python version. |
| `Python_36` | Python 3.6 |
| `Python_37` | Python 3.7 |
| `Python_38` | Python 3.8 |
| `Python_39` | Python 3.9 |
| `Python_310` | Python 3.10 and above |

> **Note:** Only Python **3.10–3.14** are supported at runtime. The Python 2.7, 3.3–3.5 and 3.6–3.9 values remain in the enum for serialization compatibility but are not supported and will raise a validation error at runtime if used.

---

### `TargetPlatform`

> **Deprecated — has no effect.** Only 64-bit Python is supported. This enum is retained only for backward compatibility.

| Value | Description |
|-------|-------------|
| `x64` | 64-bit Python (the only supported architecture). |
| `x86` | 32-bit Python. **No longer supported** — a 32-bit installation raises an error at scope initialization. |

---

## Common Patterns

### Run a Python script file

```csharp
[Workflow]
public async void Execute()
{
    var options = new PythonScopeOptions
    {
        Path = @"C:\Python313",
        LibraryPath = @"C:\Python313\python313.dll"
    };

    await using var scope = await python.UsePythonScope(options);
    await scope.RunScript(@"Scripts\process_data.py");
}
```

### Execute inline Python code

```csharp
[Workflow]
public async void Execute()
{
    await using var scope = await python.UsePythonScope(new PythonScopeOptions
    {
        Path = @"C:\Python313",
        LibraryPath = @"C:\Python313\python313.dll"
    });

    await scope.RunCode(@"
import json
data = {'status': 'ok', 'count': 42}
print(json.dumps(data))
");
}
```

### Load a script and invoke a function, then convert the result

```csharp
[Workflow]
public async void Execute()
{
    await using var scope = await python.UsePythonScope(new PythonScopeOptions
    {
        Path = @"C:\Python313",
        LibraryPath = @"C:\Python313\python313.dll",
        WorkingFolder = @"C:\Automation\Scripts"
    });

    // Load the script — returns a handle to the module's global scope
    using var module = await scope.LoadScript(@"C:\Automation\Scripts\calculator.py");

    // Invoke a function with parameters
    using var result = await scope.InvokeMethod(module, "add_numbers", new object[] { 10, 32 });

    // Convert the PythonObject to a .NET type
    var sum = scope.GetObject<int>(result);
    Log($"Result: {sum}");  // Result: 42
}
```

### Load inline code and invoke a function

```csharp
[Workflow]
public async void Execute()
{
    await using var scope = await python.UsePythonScope(new PythonScopeOptions
    {
        Path = @"C:\Python313",
        LibraryPath = @"C:\Python313\python313.dll"
    });

    using var module = await scope.LoadCode(@"
def greet(name):
    return f'Hello, {name}!'
");

    using var result = await scope.InvokeMethod(module, "greet", new object[] { "World" });
    var greeting = scope.GetObject<string>(result);
    Log(greeting);  // Hello, World!
}
```

### Use a virtual environment with timeout control

```csharp
[Workflow]
public async void Execute()
{
    var options = new PythonScopeOptions
    {
        Path = @"C:\Envs\myenv\Scripts",
        LibraryPath = @"C:\Python313\python313.dll",
        WorkingFolder = @"C:\Automation",
        OperationTimeout = TimeSpan.FromMinutes(5)
    };

    await using var scope = await python.UsePythonScope(options);
    await scope.RunScript(@"C:\Automation\Scripts\ml_inference.py");
}
```
