# Delete File or Folder

`UiPath.FTP.Activities.Delete`

Removes a specified file from an FTP server. This activity only works if it is placed inside a [Use FTP Connection](WithFtpSession.md) scope activity.

**Package:** `UiPath.FTP.Activities`
**Category:** FTP

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `RemotePath` | File or folder to delete | InArgument | `string` | Yes |  |  | Remote path to the file or folder. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `ContinueOnError` | Continue On Error | `bool` |  | Specifies if the automation should continue even when the activity throws an error. |

## XAML Example

```xml
<ftp:Delete DisplayName="Delete File or Folder" RemotePath="/remote/path/file.txt" />
```
