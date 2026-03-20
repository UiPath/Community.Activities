# File Exists

`UiPath.FTP.Activities.FileExists`

Checks whether a certain file exists in the specified FTP directory. This activity only works if it is placed inside a With FTP Session scope activity.

**Package:** `UiPath.FTP.Activities`
**Category:** FTP

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `RemotePath` | File Path | Property | `Object` | Yes |  |  | Remote file path to check |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `ContinueOnError` | Continue On Error | `Object` |  | Specifies if the automation should continue even when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Exists` | Exists | OutArgument | `Object` | A boolean variable that states whether the indicated file was found or not. |

## XAML Example

```xml
<ftp:FileExists DisplayName="File Exists" />
```
