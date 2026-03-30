# Load Jar

`UiPath.Java.Activities.LoadJar`

Loads a Java archive file into the active Java runtime so classes can be instantiated and invoked.

**Package:** `UiPath.Java.Activities`
**Category:** Java
**Required Scope:** `UiPath.Java.Activities.JavaScope`
**Platform:** Windows only

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Description |
|------|-------------|------|------|----------|---------|-------------|
| `JarPath` | Jar Path | InArgument | `string` | Yes |  | Full path to the JAR file that should be loaded. |

## XAML Example

```xml
<java:JavaScope DisplayName="Java Scope">
  <java:LoadJar DisplayName="Load Jar"
                JarPath="[&quot;C:\libs\example.jar&quot;]" />
</java:JavaScope>
```
