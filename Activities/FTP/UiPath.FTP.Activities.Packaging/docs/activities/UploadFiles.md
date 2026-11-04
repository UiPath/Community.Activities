# Upload Files

`UiPath.FTP.Activities.UploadFiles`

Uploads files to an FTP server. This activity only works if it is placed inside a [Use FTP Connection](WithFtpSession.md) scope activity.

**Package:** `UiPath.FTP.Activities`
**Category:** FTP

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `LocalPath` | Files to upload | InArgument | `string` | Yes |  | `C:\Local\folder` | Local path of the files to upload. |
| `RemotePath` | Where to upload | InArgument | `string` | Yes |  | `/remote/folder` | Remote destination path on the FTP server. |
| `Recursive` | Include subfolders | Property | `bool` |  |  |  | If turned on, the folders are uploaded with their respective subfolders. |
| `Create` | Create folder if missing | Property | `bool` |  |  |  | If turned on, the folder path is created on the FTP server in case it does not already exist. |
| `Overwrite` | Overwrite existing files | Property | `bool` |  |  |  | If turned on, the files are overwritten on the FTP server if they are already stored there. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `ContinueOnError` | Continue on error | `bool` |  | Specifies if the automation should continue even when the activity throws an error. |

## XAML Example

Requires the FTP namespace (`xmlns:ftp="http://schemas.uipath.com/workflow/activities/ftp"`, see [overview](../overview.md#xaml-namespace)) and must run inside a [`WithFtpSession`](WithFtpSession.md) scope.

```xml
<ftp:UploadFiles DisplayName="Upload Files" LocalPath="C:\\local\\files" RemotePath="/remote/destination" />
```
