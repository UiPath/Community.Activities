# Upload Files

`UiPath.FTP.Activities.UploadFiles`

Uploads a file to an FTP server. This activity only works if it is placed inside a With FTP Session scope activity.

**Package:** `UiPath.FTP.Activities`
**Category:** FTP

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `LocalPath` | Files to upload | Property | `Object` | Yes |  |  | Local path |
| `RemotePath` | Where to upload | Property | `Object` | Yes |  |  | Remote path |
| `Recursive` | Include subfolders | Property | `Object` |  |  |  | If this box is checked, the folders will be uploaded with their respective subfolders. |
| `Create` | Create | Property | `Object` |  |  |  | If this box is checked, the folder path will be created on the FTP server in case it does not already exist. |
| `Overwrite` | Overwrite | Property | `Object` |  |  |  | If this box is checked, the files will be overwritten on the FTP server if they're already stored there. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `ContinueOnError` | Continue On Error | `Object` |  | Specifies if the automation should continue even when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `-` | - | - | `-` | - |

## XAML Example

```xml
<ftp:UploadFiles DisplayName="Upload Files" />
```
