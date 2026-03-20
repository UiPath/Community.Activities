# Delete File or Folder

`UiPath.FTP.Activities.Delete`

Removes a specified file from an FTP server. This activity only works if it is placed inside a With FTP Session scope activity.

**Package:** `UiPath.FTP.Activities`
**Category:** FTP

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `RemotePath` | File or folder to delete | Property | `Object` | Yes |  |  | Remote path to the file or folder |

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
<ftp:Delete DisplayName="Delete File or Folder" />
```
