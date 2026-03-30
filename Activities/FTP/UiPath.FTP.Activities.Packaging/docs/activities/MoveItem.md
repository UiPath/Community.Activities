# Move File or Folder

`UiPath.FTP.Activities.MoveItem`

Moves an item on an FTP server to a different remote path. This activity only works if it is placed inside a [Use FTP Connection](WithFtpSession.md) scope activity.

**Package:** `UiPath.FTP.Activities`
**Category:** FTP

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `RemotePath` | File or folder to move | InArgument | `string` | Yes |  |  | Remote path of the item to move. |
| `NewPath` | Where to move | InArgument | `string` | Yes |  |  | The new remote path to move the item to. |
| `Overwrite` | Overwrite | Property | `bool` |  |  |  | If this box is checked, the files will be overwritten in the new remote directory if they're already stored there. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `ContinueOnError` | Continue On Error | `bool` |  | Specifies if the automation should continue even when the activity throws an error. |

## XAML Example

```xml
<ftp:MoveItem DisplayName="Move File or Folder" RemotePath="/remote/source/file.txt" NewPath="/remote/destination/file.txt" />
```
