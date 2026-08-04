# Check If File Exists

`UiPath.FTP.Activities.FileExists`

Checks whether a file exists in the specified FTP folder. This activity only works if it is placed inside a [Use FTP Connection](WithFtpSession.md) scope activity.

**Package:** `UiPath.FTP.Activities`
**Category:** FTP

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `RemotePath` | File path | InArgument | `string` | Yes |  | `/remote/folder/file.txt` | Remote file path to check. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `ContinueOnError` | Continue on error | `bool` |  | Specifies if the automation should continue even when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Exists` | Exists | OutArgument | `bool` | A boolean variable that states whether the indicated file was found or not. |

> **On SFTP this also returns `True` for a symbolic link**, whatever the link points at, because the
> SSH library classifies links as regular files. See [List Files and Folders](EnumerateObjects.md).
> FTP and FTPS are unaffected.

## XAML Example

Requires the FTP namespace (`xmlns:ftp="http://schemas.uipath.com/workflow/activities/ftp"`, see [overview](../overview.md#xaml-namespace)) and must run inside a [`WithFtpSession`](WithFtpSession.md) scope.

```xml
<ftp:FileExists DisplayName="Check If File Exists" RemotePath="/remote/path/file.txt" Exists="[fileExists]" />
```
