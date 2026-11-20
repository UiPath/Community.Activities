# Check If Folder Exists

`UiPath.FTP.Activities.DirectoryExists`

Checks whether a folder exists on an FTP server. This activity only works if it is placed inside a [Use FTP Connection](WithFtpSession.md) scope activity.

**Package:** `UiPath.FTP.Activities`
**Category:** FTP

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `RemotePath` | Folder path | InArgument | `string` | Yes |  | `/remote/folder` | Remote folder path to check. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `ContinueOnError` | Continue on error | `bool` |  | Specifies if the automation should continue even when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Exists` | Exists | OutArgument | `bool` | A boolean variable that states whether the indicated folder was found or not. |

> **A symbolic link pointing at a folder returns `False` on SFTP.** The SSH library classifies links as
> regular files, so this activity does not see them as folders. See
> [List Files and Folders](EnumerateObjects.md) for the full explanation. FTP and FTPS are unaffected.

## XAML Example

Requires the FTP namespace (`xmlns:ftp="http://schemas.uipath.com/workflow/activities/ftp"`, see [overview](../overview.md#xaml-namespace)) and must run inside a [`WithFtpSession`](WithFtpSession.md) scope.

```xml
<ftp:DirectoryExists DisplayName="Check If Folder Exists" RemotePath="/remote/folder" Exists="[dirExists]" />
```
