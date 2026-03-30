# Enumerate Objects

`UiPath.FTP.Activities.EnumerateObjects`

Generates a collection of files that have been found on the FTP server. Subfolders can also be included in the search by checking the Includes subfolders box. This activity only works if it is placed inside a [Use FTP Connection](WithFtpSession.md) scope activity.

**Package:** `UiPath.FTP.Activities`
**Category:** FTP

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `RemotePath` | Remote Path | InArgument | `string` | Yes |  |  | The path of the directory on the FTP server whose files are to be enumerated. |
| `Recursive` | Include subfolders | Property | `bool` |  |  |  | If this check box is selected, the subfolders are also included in the enumeration of the files on the FTP server. |
| `Filter` | Object types | Property | `FtpFilterObjectType` |  |  |  | Filters items based on type (what is selected will be included). |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `ContinueOnError` | Continue On Error | `bool` |  | Specifies if the automation should continue even when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Files` | Files | OutArgument | `IEnumerable<FtpObjectInfo>` | A collection of files that have been found on the FTP server. |

## XAML Example

```xml
<ftp:EnumerateObjects DisplayName="Enumerate Objects" RemotePath="/remote/files" Files="[ftpFiles]" />
```
