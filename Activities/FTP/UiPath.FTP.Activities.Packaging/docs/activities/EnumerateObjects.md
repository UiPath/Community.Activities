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
| `Filter` | Object types | Property | `FtpFilterObjectType` |  | `Directory \| File \| Link \| Other` (all) |  | Filters returned items by type (selected types are included). See enum + semantics below. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `ContinueOnError` | Continue On Error | `bool` |  | Specifies if the automation should continue even when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Files` | Files | OutArgument | `IEnumerable<FtpObjectInfo>` | A collection of files that have been found on the FTP server. |

## `FtpFilterObjectType` (the `Filter` property)

`[Flags]` enum in `UiPath.FTP` (XAML: `ftp:FtpFilterObjectType`). Combine values with `|` (VB: `Or`).

| Value | Numeric | Meaning |
|-------|---------|---------|
| `None` | 0 | Returns an **empty** collection. |
| `Directory` | 1 | Include directories. |
| `File` | 2 | Include regular files. |
| `Link` | 4 | Include symbolic links. |
| `Other` | 8 | Include other entries (named pipe, device, etc.). |

**Semantics (as implemented):**
- Default is all four set (`Directory | File | Link | Other`) → returns everything, no filtering.
- `None` → returns an empty collection.
- Any subset → returns only entries whose `FtpObjectType` is selected.
- **Files only:** set `Filter="File"`.

Each returned `FtpObjectInfo` also carries its `Type` (`ftp:FtpObjectType`: `Directory`/`File`/`Link`/`Other`), so you can alternatively filter the output collection yourself.

## XAML Example

Requires the FTP namespace (`xmlns:ftp="http://schemas.uipath.com/workflow/activities/ftp"`) and must run inside a [`WithFtpSession`](WithFtpSession.md) scope. `Files` is an `OutArgument<IEnumerable<FtpObjectInfo>>`.

List **files only** in the current remote folder:

```xml
<ftp:EnumerateObjects DisplayName="Enumerate Objects"
                      RemotePath="." Recursive="False" Filter="File"
                      Files="[ftpFiles]" />
```

`ftpFiles` is a variable of type `IEnumerable<FtpObjectInfo>` (XAML: `scg:IEnumerable(ftp:FtpObjectInfo)`).
