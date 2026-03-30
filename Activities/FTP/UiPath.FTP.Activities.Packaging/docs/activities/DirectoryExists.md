# Directory Exists

`UiPath.FTP.Activities.DirectoryExists`

Checks whether a certain directory exists on an FTP server. This activity only works if it is placed inside a [Use FTP Connection](WithFtpSession.md) scope activity.

**Package:** `UiPath.FTP.Activities`
**Category:** FTP

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `RemotePath` | Folder Path | InArgument | `string` | Yes |  |  | Remote folder path to check. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `ContinueOnError` | Continue On Error | `bool` |  | Specifies if the automation should continue even when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Exists` | Exists | OutArgument | `bool` | A boolean variable that states whether the indicated directory was found or not. |

## XAML Example

```xml
<ftp:DirectoryExists DisplayName="Directory Exists" RemotePath="/remote/folder" Exists="[dirExists]" />
```
