# Java — Coded Workflow API

`UiPath.Java.Activities`

Provides coded workflow operations for loading JAR files, creating Java objects, invoking instance and static methods, and reading fields from Java classes.

**Service accessor:** `java` (type `IJavaService`)
**Required package:** `"UiPath.Java.Activities": "*"` in project.json dependencies

## Auto-Imported Namespaces

These namespaces are automatically available in coded workflows when this package is installed:

```
System
System.Collections.Generic
UiPath.Java
UiPath.Java.Activities
UiPath.Java.Activities.API
UiPath.Java.Activities.API.Models
```

## Service Overview

The `java` service provides a handle-based API for Java interop. You open a Java scope via the service, receive a disposable handle, then call extension methods on the handle to load JARs, create objects, invoke methods, and read fields.

`IJavaScopeHandle` implements both `IDisposable` and `IAsyncDisposable` — prefer `await using` in async workflows to release the Java service process cleanly.

All operations return `JavaObject`, an opaque wrapper around a Java value. Convert it to a .NET type using `ConvertObject<T>`. Methods that return `void` in Java produce a `JavaObject` where `IsNull()` returns `true`.

When a Java class has overloaded methods, pass `parameterTypes` explicitly to ensure the correct overload is selected.

---

## Opening a Java Scope

### `Task<IJavaScopeHandle> UseJavaScope(JavaScopeOptions options, CancellationToken ct = default)`

Creates and initializes a Java scope configured with the given options.

**Parameters:**
- `options` (`JavaScopeOptions`) — Options for configuring the Java installation path and startup timeout
- `ct` (`CancellationToken`) — Cancellation token (default: `default`)

**Returns:** `Task<IJavaScopeHandle>` — Awaitable task producing a disposable handle to the active Java scope. Use with `await using` statement.

---

## Handle Type: `IJavaScopeHandle`

Disposable handle to an initialized Java scope. Operations are available as extension methods in `UiPath.Java.Activities.API.JavaOperations` and are called directly on the handle.

> This type implements `IDisposable` and `IAsyncDisposable`. Always use inside an `await using` statement or call `DisposeAsync()` explicitly to ensure the Java service process is stopped.

### Property

| Property | Type | Description |
|----------|------|-------------|
| `Invoker` | `IInvoker` | The underlying Java invoker. Provides direct access to low-level interop operations. |

### Extension Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `LoadJar(string jarPath, CancellationToken ct = default)` | `Task` | Loads a JAR file into the Java scope, making its classes available for use. |
| `CreateObject(string className, List<object> parameters = null, List<Type> parameterTypes = null, CancellationToken ct = default)` | `Task<JavaObject>` | Creates a new Java object by invoking its constructor. |
| `InvokeMethod(string methodName, JavaObject targetObject, List<object> parameters = null, List<Type> parameterTypes = null, CancellationToken ct = default)` | `Task<JavaObject>` | Invokes an instance method on a Java object. |
| `InvokeStaticMethod(string methodName, string className, List<object> parameters = null, List<Type> parameterTypes = null, CancellationToken ct = default)` | `Task<JavaObject>` | Invokes a static method on a Java class. |
| `GetField(string fieldName, JavaObject targetObject, CancellationToken ct = default)` | `Task<JavaObject>` | Gets the value of an instance field on a Java object. |
| `GetStaticField(string fieldName, string className, CancellationToken ct = default)` | `Task<JavaObject>` | Gets the value of a static field on a Java class. |
| `ConvertObject<T>(JavaObject javaObject)` | `T` | Converts a `JavaObject` to the specified .NET type. |

---

## Method Reference

### `Task LoadJar(string jarPath, CancellationToken ct = default)`

Loads a JAR file into the Java scope. All classes defined in the JAR become available for `CreateObject`, `InvokeStaticMethod`, and field access. Call this before any operation that requires classes from the JAR.

**Parameters:**
- `jarPath` (`string`) — Path to the JAR file to load
- `ct` (`CancellationToken`) — Cancellation token (default: `default`)

**Returns:** `Task`

---

### `Task<JavaObject> CreateObject(string className, List<object> parameters = null, List<Type> parameterTypes = null, CancellationToken ct = default)`

Creates a new Java object by invoking the constructor of the named class. Use `parameterTypes` when multiple constructors exist with the same parameter count to select the correct overload.

**Parameters:**
- `className` (`string`) — Fully-qualified Java class name (e.g., `"com.example.MyClass"`)
- `parameters` (`List<object>`) — Constructor arguments (default: `null`, treated as no-arg constructor)
- `parameterTypes` (`List<Type>`) — Explicit .NET types corresponding to each parameter, used for overload resolution. When `null`, types are inferred from the parameter values.
- `ct` (`CancellationToken`) — Cancellation token (default: `default`)

**Returns:** `Task<JavaObject>` — Handle to the newly created Java object instance.

---

### `Task<JavaObject> InvokeMethod(string methodName, JavaObject targetObject, List<object> parameters = null, List<Type> parameterTypes = null, CancellationToken ct = default)`

Invokes an instance method on an existing Java object. Use `parameterTypes` when the class has overloaded methods with the same name.

**Parameters:**
- `methodName` (`string`) — Name of the method to invoke
- `targetObject` (`JavaObject`) — The Java object instance to invoke the method on
- `parameters` (`List<object>`) — Method arguments (default: `null`, treated as no-arg call)
- `parameterTypes` (`List<Type>`) — Explicit .NET types for overload resolution. When `null`, types are inferred from the parameter values.
- `ct` (`CancellationToken`) — Cancellation token (default: `default`)

**Returns:** `Task<JavaObject>` — The method's return value. For `void` Java methods, the returned `JavaObject` has `IsNull()` returning `true`.

---

### `Task<JavaObject> InvokeStaticMethod(string methodName, string className, List<object> parameters = null, List<Type> parameterTypes = null, CancellationToken ct = default)`

Invokes a static method on a Java class without requiring an object instance.

**Parameters:**
- `methodName` (`string`) — Name of the static method to invoke
- `className` (`string`) — Fully-qualified Java class name (e.g., `"java.lang.Math"`)
- `parameters` (`List<object>`) — Method arguments (default: `null`, treated as no-arg call)
- `parameterTypes` (`List<Type>`) — Explicit .NET types for overload resolution. When `null`, types are inferred from the parameter values.
- `ct` (`CancellationToken`) — Cancellation token (default: `default`)

**Returns:** `Task<JavaObject>` — The method's return value. For `void` Java methods, the returned `JavaObject` has `IsNull()` returning `true`.

---

### `Task<JavaObject> GetField(string fieldName, JavaObject targetObject, CancellationToken ct = default)`

Reads the value of an instance field on a Java object.

**Parameters:**
- `fieldName` (`string`) — Name of the field to read
- `targetObject` (`JavaObject`) — The Java object instance
- `ct` (`CancellationToken`) — Cancellation token (default: `default`)

**Returns:** `Task<JavaObject>` — The field's value as a `JavaObject`.

---

### `Task<JavaObject> GetStaticField(string fieldName, string className, CancellationToken ct = default)`

Reads the value of a static field on a Java class.

**Parameters:**
- `fieldName` (`string`) — Name of the static field to read
- `className` (`string`) — Fully-qualified Java class name
- `ct` (`CancellationToken`) — Cancellation token (default: `default`)

**Returns:** `Task<JavaObject>` — The field's value as a `JavaObject`.

---

### `T ConvertObject<T>(JavaObject javaObject)`

Converts a `JavaObject` to a .NET type. Use this after `InvokeMethod`, `InvokeStaticMethod`, `CreateObject`, `GetField`, or `GetStaticField` to obtain a usable .NET value.

**Type parameters:**
- `T` — The target .NET type (e.g., `string`, `int`, `double`, `bool`)

**Parameters:**
- `javaObject` (`JavaObject`) — The Java object to convert

**Returns:** `T` — The converted .NET value.

---

## Return Types

### `JavaObject`

An opaque wrapper around a Java value. Returned by `CreateObject`, `InvokeMethod`, `InvokeStaticMethod`, `GetField`, and `GetStaticField`.

| Member | Return Type | Description |
|--------|-------------|-------------|
| `Convert<T>()` | `T` | Converts the Java object to the specified .NET type. |
| `IsNull()` | `bool` | Returns `true` if the underlying Java value is `null`. Java `void` methods produce a `JavaObject` where this returns `true`. |

Pass a `JavaObject` to `ConvertObject<T>` (or call `.Convert<T>()` directly) to obtain a .NET value, or pass it back to `InvokeMethod` as the `targetObject` for chained calls.

---

## Options & Configuration

### `JavaScopeOptions`

Options for configuring a Java scope passed to `UseJavaScope`.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `JavaPath` | `string` | — | Path to the Java installation directory (the folder containing the `bin` subfolder). When `null` or empty, the `java` executable on the system PATH is used. |
| `Timeout` | `TimeSpan?` | `TimeSpan.FromSeconds(15)` | Maximum time to wait for the Java service to start. |

---

## Common Patterns

### Load a JAR and invoke a static method

```csharp
[Workflow]
public async void Execute()
{
    await using var scope = await java.UseJavaScope(new JavaScopeOptions());

    await scope.LoadJar(@"C:\Libs\myutils.jar");

    var result = await scope.InvokeStaticMethod(
        "processData",
        "com.example.DataProcessor",
        parameters: new List<object> { "input.csv" });

    var output = scope.ConvertObject<string>(result);
    Log(output);
}
```

### Create a Java object and call instance methods

```csharp
[Workflow]
public async void Execute()
{
    await using var scope = await java.UseJavaScope(new JavaScopeOptions
    {
        JavaPath = @"C:\Program Files\Java\jdk-17",
        Timeout = TimeSpan.FromSeconds(30)
    });

    await scope.LoadJar(@"C:\Libs\calculator.jar");

    // Create an instance: new com.example.Calculator()
    var calculator = await scope.CreateObject("com.example.Calculator");

    // Invoke instance method: calculator.add(10, 32)
    var result = await scope.InvokeMethod(
        "add",
        calculator,
        parameters: new List<object> { 10, 32 });

    var sum = scope.ConvertObject<int>(result);
    Log($"Result: {sum}");  // Result: 42
}
```

### Resolve overloaded methods with explicit parameter types

```csharp
[Workflow]
public async void Execute()
{
    await using var scope = await java.UseJavaScope(new JavaScopeOptions());
    await scope.LoadJar(@"C:\Libs\formatter.jar");

    var formatter = await scope.CreateObject("com.example.Formatter");

    // Two overloads exist: format(String) and format(int)
    // Pass parameterTypes to select the correct one
    var result = await scope.InvokeMethod(
        "format",
        formatter,
        parameters: new List<object> { 42 },
        parameterTypes: new List<Type> { typeof(int) });

    var formatted = scope.ConvertObject<string>(result);
    Log(formatted);
}
```

### Read a static field

```csharp
[Workflow]
public async void Execute()
{
    await using var scope = await java.UseJavaScope(new JavaScopeOptions());
    await scope.LoadJar(@"C:\Libs\config.jar");

    // Read a static constant: com.example.Config.DEFAULT_TIMEOUT
    var timeoutObj = await scope.GetStaticField("DEFAULT_TIMEOUT", "com.example.Config");
    var timeoutValue = scope.ConvertObject<int>(timeoutObj);
    Log($"Default timeout: {timeoutValue}ms");
}
```

### Chain calls — read an instance field after creating an object

```csharp
[Workflow]
public async void Execute()
{
    await using var scope = await java.UseJavaScope(new JavaScopeOptions());
    await scope.LoadJar(@"C:\Libs\models.jar");

    // Create: new com.example.Person("Alice", 30)
    var person = await scope.CreateObject(
        "com.example.Person",
        parameters: new List<object> { "Alice", 30 },
        parameterTypes: new List<Type> { typeof(string), typeof(int) });

    // Read instance field: person.name
    var nameObj = await scope.GetField("name", person);
    var name = scope.ConvertObject<string>(nameObj);

    // Invoke instance method: person.getAge()
    var ageResult = await scope.InvokeMethod("getAge", person);
    var age = scope.ConvertObject<int>(ageResult);

    Log($"{name} is {age} years old.");
}
```
