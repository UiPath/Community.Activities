# Upload Files

`UiPath.FTP.Activities.UploadFiles`

Uploads a file to an FTP server. This activity only works if it is placed inside a [Use FTP Connection](WithFtpSession.md) scope activity.

**Package:** `UiPath.FTP.Activities`
**Category:** FTP

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `LocalPath` | Files to upload | InArgument | `string` | Yes |  |  | Local path of the files to upload. |
| `RemotePath` | Where to upload | InArgument | `string` | Yes |  |  | Remote destination path on the FTP server. |
| `Recursive` | Include subfolders | Property | `bool` |  |  |  | If this box is checked, the folders will be uploaded with their respective subfolders. |
| `Create` | Create | Property | `bool` |  |  |  | If this box is checked, the folder path will be created on the FTP server in case it does not already exist. |
| `Overwrite` | Overwrite | Property | `bool` |  |  |  | If this box is checked, the files will be overwritten on the FTP server if they're already stored there. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `ContinueOnError` | Continue On Error | `bool` |  | Specifies if the automation should continue even when the activity throws an error. |

## XAML Example

```xml
<ftp:UploadFiles DisplayName="Upload Files" LocalPath="C:\\local\\files" RemotePath="/remote/destination" />
```
