# UiPath FTP Activities

`UiPath.FTP.Activities`

## Activities

| Activity | Description |
|----------|-------------|
| [Delete File or Folder](activities/Delete.md) | Removes a file or folder from an FTP server. This activity only works if it is placed inside a Use FTP Connection activity. |
| [Check If Folder Exists](activities/DirectoryExists.md) | Checks whether a folder exists on an FTP server. This activity only works if it is placed inside a Use FTP Connection activity. |
| [Download Files](activities/DownloadFiles.md) | Downloads the specified files from an FTP server to the specified local folder. This activity only works if it is placed inside a Use FTP Connection activity. |
| [List Files and Folders](activities/EnumerateObjects.md) | Generates a collection of the files and folders found on the FTP server. Subfolders can also be included by turning on Include subfolders. This activity only works if it is placed inside a Use FTP Connection activity. |
| [Check If File Exists](activities/FileExists.md) | Checks whether a file exists in the specified FTP folder. This activity only works if it is placed inside a Use FTP Connection activity. |
| [Move File or Folder](activities/MoveItem.md) | Moves an item on an FTP server to a different remote path. This activity only works if it is placed inside a Use FTP Connection activity. |
| [Upload Files](activities/UploadFiles.md) | Uploads files to an FTP server. This activity only works if it is placed inside a Use FTP Connection activity. |
| [Use FTP Connection](activities/WithFtpSession.md) | Connects to FTP server and provides a scope for other FTP activities. |

## XAML Namespace

All FTP activities and types live under a single XAML namespace. Declare it on the root `<Activity>` and use the `ftp:` prefix for every FTP type:

```xml
xmlns:ftp="http://schemas.uipath.com/workflow/activities/ftp"
```

This one URI maps the whole package — the activities (`UiPath.FTP.Activities`), the session/result types (`UiPath.FTP`: `IFtpSession`, `FtpObjectInfo`, `FtpObjectType`, `FtpFilterObjectType`), and the design types. So `ftp:WithFtpSession`, `ftp:IFtpSession`, `ftp:FtpObjectInfo`, etc. all use the same prefix.

> **Do NOT use the `clr-namespace:` form** (e.g. `clr-namespace:UiPath.FTP.Activities;assembly=UiPath.FTP.Activities`). The package registers its types under the schema URI above via `[XmlnsDefinition]`. The `clr-namespace` form can pass `uipath validate` but then fail the project build with `Cannot create unknown type '{clr-namespace:...}WithFtpSession'`. Always declare the schema URI.

> **`validate` is not enough — run `build`.** A wrong namespace, a wrong scope-body shape, or a wrong property type frequently passes per-file `validate` and only fails at project `build` (or runtime). Treat a green `validate` + failing `build` whose error names a `{clr-namespace:...}` type as a namespace-declaration problem.

## Types

| Type | XAML | Notes |
|------|------|-------|
| `IFtpSession` | `ftp:IFtpSession` | Delegate argument exposed by the `WithFtpSession` scope body (`ActivityAction<IFtpSession>`, arg name `FtpSession`). |
| `FtpObjectInfo` | `ftp:FtpObjectInfo` | Returned by List Files and Folders. Members: `FullName`, `Name`, `Size`, `Created`, `Modified`, `Type` (`FtpObjectType`), `OwnerPermissions`, `GroupPermissions`, `OthersPermissions` (`FtpPermissions`). |
| `FtpObjectType` | `ftp:FtpObjectType` | Enum: `Directory`, `File`, `Link`, `Other`. **On SFTP, `Link` is never produced** — symbolic links are reported as `File`. FTP/FTPS report `Link` correctly. See [List Files and Folders](activities/EnumerateObjects.md). |
| `FtpFilterObjectType` | `ftp:FtpFilterObjectType` | `[Flags]` enum used by List Files and Folders `Filter` — see that activity's doc for values and semantics. |
