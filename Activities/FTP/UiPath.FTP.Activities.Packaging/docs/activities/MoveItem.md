# Move File or Folder

`UiPath.FTP.Activities.MoveItem`

Moves an item on an FTP server to a different remote path. This activity only works if it is placed inside a With FTP

**Package:** `UiPath.FTP.Activities`
**Category:** FTP

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `RemotePath` | File or folder to move | Property | `Object` | Yes |  |  | Remote path |
| `NewPath` | Where to move | Property | `Object` | Yes |  |  | The new path where to move the item |
| `Overwrite` | Overwrite | Property | `Object` |  |  |  | If this box is checked, the files will be overwritten in the new remote directory if they're already stored there. |

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
<ftp:MoveItem DisplayName="Move File or Folder" />
```
