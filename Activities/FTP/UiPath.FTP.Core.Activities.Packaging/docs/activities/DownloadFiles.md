# Download Files

`UiPath.FTP.Activities.DownloadFiles`

Downloads the specified files from an FTP server to the specified local folder. This activity only works if it is placed inside a [Use FTP Connection](WithFtpSession.md) scope activity.

**Package:** `UiPath.FTP.Activities`
**Category:** FTP

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `RemotePath` | Path to files to download | InArgument | `string` | Yes |  | `/remote/folder` | The path of the files on the FTP server that are to be downloaded. |
| `LocalPath` | Where to download | InArgument | `string` | Yes |  | `C:\Local\folder` | The local path for the files that are to be downloaded. |
| `Recursive` | Include subfolders | Property | `bool` |  |  |  | If turned on, the folders are downloaded with their respective subfolders. |
| `Create` | Create folder if missing | Property | `bool` |  |  |  | If turned on, the folder path is created locally in case it does not already exist. |
| `Overwrite` | Overwrite existing files | Property | `bool` |  |  |  | If turned on, the files are overwritten locally if they are already stored there. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `ContinueOnError` | Continue on error | `bool` |  | Specifies if the automation should continue even when the activity throws an error. |

## Behaviour notes

These apply to `Recursive="True"`:

- **Symbolic links are not descended into.** On SFTP a link is still transferred as a file (the server
  follows it on open), but the walk does not treat it as a folder, so files underneath a linked folder
  are not downloaded. This prevents a link cycle from recursing forever.
- **A sub-folder the server lists but refuses to open is skipped, not fatal.** Its contents are simply
  not downloaded, the path is written to the trace log as a warning, and the rest of the transfer
  completes. Note the activity reports success in this case — check the trace if you need certainty
  that every file was retrieved.
- **Connection and timeout failures still fail the activity**, so a dropped connection is never
  reported as a completed download.
- **A bad `RemotePath` still fails.** The tolerance above applies only to sub-folders discovered during
  the walk.

## XAML Example

Requires the FTP namespace (`xmlns:ftp="http://schemas.uipath.com/workflow/activities/ftp"`, see [overview](../overview.md#xaml-namespace)) and must run inside a [`WithFtpSession`](WithFtpSession.md) scope.

```xml
<ftp:DownloadFiles DisplayName="Download Files" RemotePath="/remote/files/*" LocalPath="C:\\local\\downloads" />
```
