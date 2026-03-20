# Download Files

`UiPath.FTP.Activities.DownloadFiles`

Downloads the specified files from an FTP server to the specified local folder. This activity only works if it is placed inside a With FTP Session scope activity.

**Package:** `UiPath.FTP.Activities`
**Category:** FTP

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `RemotePath` | Path to files to download | Property | `Object` | Yes |  |  | The path of the files on the FTP server that are to be downloaded. |
| `LocalPath` | Where to download | Property | `Object` | Yes |  |  | The local path for the files that are to be downloaded. |
| `Recursive` | Include subfolders | Property | `Object` |  |  |  | If this box is checked, the folders will be downloaded with their respective subfolders. |
| `Create` | Create | Property | `Object` |  |  |  | If this box is checked, the folder path will be created locally in case it does not already exist. |
| `Overwrite` | Overwrite | Property | `Object` |  |  |  | If this box is checked, the files will be overwritten locally if they're already stored there. |

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
<ftp:DownloadFiles DisplayName="Download Files" />
```
