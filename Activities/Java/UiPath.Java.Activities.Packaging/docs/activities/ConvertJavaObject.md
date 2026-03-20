# Convert Java Object

`UiPath.Java.Activities.ConvertJavaObject<T>`

Converts a Java object handle obtained from Java activities into a strongly typed .NET value.

**Package:** `UiPath.Java.Activities`
**Category:** Java
**Required Scope:** `UiPath.Java.Activities.JavaScope`
**Platform:** Windows only

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Description |
|------|-------------|------|------|----------|---------|-------------|
| `JavaObject` | Java Object | InArgument | `JavaObject` | Yes |  | Java object handle to convert. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Result` | Result | OutArgument | `T` | Converted value of the requested target type. |

## XAML Example

```xml
<java:JavaScope DisplayName="Java Scope">
  <java:ConvertJavaObject x:TypeArguments="x:String"
                          DisplayName="Convert Java Object"
                          JavaObject="[javaObj]"
                          Result="[convertedText]" />
</java:JavaScope>
```

## Notes

- Use `x:TypeArguments` to set the target type parameter `T`.
