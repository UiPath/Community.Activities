# Move File or Folder

`UiPath.FTP.Activities.MoveItem`

Moves an item on an FTP server to a different remote path. This activity only works if it is placed inside a [Use FTP Connection](WithFtpSession.md) scope activity.

**Package:** `UiPath.FTP.Activities`
**Category:** FTP

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `RemotePath` | File or folder to move | InArgument | `string` | Yes |  | `/remote/folder/file.txt` | Remote path of the item to move. |
| `NewPath` | Where to move | InArgument | `string` | Yes |  | `/remote/new-folder/file.txt` | The new remote path to move the item to. |
| `Overwrite` | Overwrite existing files | Property | `bool` |  |  |  | If turned on, the files are overwritten in the new remote folder if they are already stored there. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `ContinueOnError` | Continue on error | `bool` |  | Specifies if the automation should continue even when the activity throws an error. |

## XAML Example

Requires the FTP namespace (`xmlns:ftp="http://schemas.uipath.com/workflow/activities/ftp"`, see [overview](../overview.md#xaml-namespace)) and must run inside a [`WithFtpSession`](WithFtpSession.md) scope.

```xml
<ftp:MoveItem DisplayName="Move File or Folder" RemotePath="/remote/source/file.txt" NewPath="/remote/destination/file.txt" />
```
