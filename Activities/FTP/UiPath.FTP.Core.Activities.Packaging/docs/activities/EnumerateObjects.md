# List Files and Folders

`UiPath.FTP.Activities.EnumerateObjects`

Generates a collection of the files and folders found on the FTP server. Subfolders can also be included by turning on Include subfolders. This activity only works if it is placed inside a [Use FTP Connection](WithFtpSession.md) scope activity.

**Package:** `UiPath.FTP.Activities`
**Category:** FTP

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `RemotePath` | Folder path | InArgument | `string` | Yes |  | `/remote/folder` | The path of the folder on the FTP server whose contents are listed. |
| `Recursive` | Include subfolders | Property | `bool` |  |  |  | If turned on, the subfolders are also included in the listing. |
| `Filter` | Item types to include | Property | `FtpFilterObjectType` |  | `Directory \| File \| Link \| Other` (all) | `Select the item types to include` | Filters returned items by type (selected types are included). See enum + semantics below. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `ContinueOnError` | Continue on error | `bool` |  | Specifies if the automation should continue even when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Files` | Files and folders | OutArgument | `IEnumerable<FtpObjectInfo>` | The collection of files and folders found on the FTP server. |

## `FtpFilterObjectType` (the `Filter` property)

`[Flags]` enum in `UiPath.FTP` (XAML: `ftp:FtpFilterObjectType`). Combine values with `|` (VB: `Or`).

| Value | Numeric | Meaning |
|-------|---------|---------|
| `None` | 0 | Returns an **empty** collection. |
| `Directory` | 1 | Include directories. |
| `File` | 2 | Include regular files. On SFTP this also includes symbolic links — see the note below. |
| `Link` | 4 | Include symbolic links. **On SFTP this matches nothing** — see the note below. |
| `Other` | 8 | Include other entries (named pipe, device, etc.). |

**Semantics (as implemented):**
- Default is all four set (`Directory | File | Link | Other`) → returns everything, no filtering.
- `None` → returns an empty collection.
- Any subset → returns only entries whose `FtpObjectType` is selected.
- **Files only:** set `Filter="File"`.

Each returned `FtpObjectInfo` also carries its `Type` (`ftp:FtpObjectType`: `Directory`/`File`/`Link`/`Other`), so you can alternatively filter the output collection yourself.

> **Symbolic links on SFTP are reported as `File`, not `Link`.** The SSH library treats a link as a
> regular file, so on an SFTP connection `Filter="Link"` returns nothing and `Filter="File"` includes
> links alongside real files. FTP and FTPS are unaffected and report `Link` correctly. If you need to
> tell links apart on SFTP, this activity cannot currently do it.

## Behaviour notes

These apply to `Recursive="True"`:

- **Symbolic links are not followed.** A link is listed in the results, but the walk does not descend
  into it, so files underneath it are not returned. This prevents a link cycle from recursing forever.
- **A sub-folder the server lists but refuses to open is skipped, not fatal.** Servers can return an
  entry from a directory listing and then reject an attempt to open it — a dangling link, a stale
  mount point, an entry deleted mid-walk. The sub-folder still appears in the results, its contents
  are omitted, the offending path is written to the trace log as a warning, and the rest of the
  listing completes.
- **Connection and timeout failures still fail the activity.** A dropped connection is never reported
  as a successful partial listing.
- **A bad `RemotePath` still fails.** The tolerance above applies only to sub-folders discovered
  during the walk, never to the path you asked for.

## XAML Example

Requires the FTP namespace (`xmlns:ftp="http://schemas.uipath.com/workflow/activities/ftp"`) and must run inside a [`WithFtpSession`](WithFtpSession.md) scope. `Files` is an `OutArgument<IEnumerable<FtpObjectInfo>>`.

List **files only** in the current remote folder:

```xml
<ftp:EnumerateObjects DisplayName="List Files and Folders"
                      RemotePath="." Recursive="False" Filter="File"
                      Files="[ftpFiles]" />
```

`ftpFiles` is a variable of type `IEnumerable<FtpObjectInfo>` (XAML: `scg:IEnumerable(ftp:FtpObjectInfo)`).
