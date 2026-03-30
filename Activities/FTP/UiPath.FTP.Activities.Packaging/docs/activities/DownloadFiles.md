# Download Files

`UiPath.FTP.Activities.DownloadFiles`

Downloads the specified files from an FTP server to the specified local folder. This activity only works if it is placed inside a [Use FTP Connection](WithFtpSession.md) scope activity.

**Package:** `UiPath.FTP.Activities`
**Category:** FTP

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `RemotePath` | Path to files to download | InArgument | `string` | Yes |  |  | The path of the files on the FTP server that are to be downloaded. |
| `LocalPath` | Where to download | InArgument | `string` | Yes |  |  | The local path for the files that are to be downloaded. |
| `Recursive` | Include subfolders | Property | `bool` |  |  |  | If this box is checked, the folders will be downloaded with their respective subfolders. |
| `Create` | Create | Property | `bool` |  |  |  | If this box is checked, the folder path will be created locally in case it does not already exist. |
| `Overwrite` | Overwrite | Property | `bool` |  |  |  | If this box is checked, the files will be overwritten locally if they're already stored there. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `ContinueOnError` | Continue On Error | `bool` |  | Specifies if the automation should continue even when the activity throws an error. |

## XAML Example

```xml
<ftp:DownloadFiles DisplayName="Download Files" RemotePath="/remote/files/*" LocalPath="C:\\local\\downloads" />
```
