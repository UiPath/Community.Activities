# Java Scope

`UiPath.Java.Activities.JavaScope`

Starts and manages a Java runtime bridge used by all Java child activities.

**Package:** `UiPath.Java.Activities`
**Category:** Java
**Platform:** Windows only

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Description |
|------|-------------|------|------|----------|---------|-------------|
| `JavaPath` | Java Path | InArgument | `string` |  |  | Java installation folder path. If empty, the system Java executable is used. |
| `TimeoutMS` | Timeout (ms) | InArgument | `int` |  | `15000` | Timeout in milliseconds for Java service initialization. |

## XAML Example

```xml
<java:JavaScope DisplayName="Java Scope"
                JavaPath="[&quot;C:\Program Files\Java\jdk-17&quot;]"
                TimeoutMS="[15000]">
  <java:LoadJar DisplayName="Load Jar"
                JarPath="[&quot;C:\libs\example.jar&quot;]" />
</java:JavaScope>
```

## Notes

- Java activities must run inside `JavaScope`.
- The scope starts and cleans up the Java invoker lifecycle for child activities.
