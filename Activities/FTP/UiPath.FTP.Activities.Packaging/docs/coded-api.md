# FTP — Coded Workflow API

`UiPath.FTP.Activities`

Provides coded workflow operations for connecting to FTP, FTPS, and SFTP servers to upload, download, delete, move, and enumerate remote files and directories.

**Service accessor:** `ftp` (type `IFtpService`)
**Required package:** `"UiPath.FTP.Activities": "*"` in project.json dependencies

## Auto-Imported Namespaces

These namespaces are automatically available in coded workflows when this package is installed:

```
System
System.Collections.Generic
UiPath.FTP
UiPath.FTP.Activities
UiPath.FTP.Activities.API
UiPath.FTP.Activities.API.Models
```

## Service Overview

The `ftp` service provides a handle-based API for remote file transfer. You open a session via the service, receive a disposable handle, then call extension methods on the handle to transfer and manage files on the server.

The service supports three protocols, selected via `FtpScopeOptions`:

| Protocol | Set via | Default port |
|----------|---------|-------------|
| Plain FTP | `FtpsMode = FtpsMode.None` (default) | 21 |
| FTPS Explicit | `FtpsMode = FtpsMode.Explicit` | 21 |
| FTPS Implicit | `FtpsMode = FtpsMode.Implicit` | 990 |
| SFTP | `UseSftp = true` | 22 |

`IFtpScopeHandle` implements both `IDisposable` and `IAsyncDisposable` — prefer `await using` to ensure the connection is closed when the scope exits.

> **Note:** `MoveItem` is the only synchronous operation (returns `void`). All other operations return `Task` and must be awaited.

---

## Opening an FTP Session

### `Task<IFtpScopeHandle> UseFtpSession(FtpScopeOptions options, CancellationToken ct = default)`

Opens a connection to an FTP/FTPS/SFTP server and returns a handle to the active session.

**Parameters:**
- `options` (`FtpScopeOptions`) — Connection options including host, credentials, protocol, and security settings
- `ct` (`CancellationToken`) — Cancellation token (default: `default`)

**Returns:** `Task<IFtpScopeHandle>` — Awaitable task producing a disposable handle to the open session. Use with `await using` statement.

---

## Handle Type: `IFtpScopeHandle`

Disposable handle to an active FTP/SFTP session. Operations are available as extension methods in `UiPath.FTP.Activities.API.FtpOperations` and are called directly on the handle.

> This type implements `IDisposable` and `IAsyncDisposable`. Always use inside an `await using` statement to ensure the server connection is closed.

### Property

| Property | Type | Description |
|----------|------|-------------|
| `Session` | `IFtpSession` | The underlying FTP session. Provides direct access to low-level session operations. |

### Extension Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `DownloadFiles(string remotePath, string localPath, bool overwrite = false, bool recursive = false, CancellationToken ct = default)` | `Task` | Downloads a file or directory from the server to a local path. |
| `UploadFiles(string localPath, string remotePath, bool overwrite = false, bool recursive = false, CancellationToken ct = default)` | `Task` | Uploads a file or directory to the server. |
| `Delete(string remotePath, CancellationToken ct = default)` | `Task` | Deletes a file or directory on the server. |
| `MoveItem(string remotePath, string newPath, bool overwrite = false)` | `void` | Moves or renames a file or directory on the server. **Synchronous — do not await.** |
| `FileExists(string remotePath, CancellationToken ct = default)` | `Task<bool>` | Returns `true` if the specified remote file exists. |
| `DirectoryExists(string remotePath, CancellationToken ct = default)` | `Task<bool>` | Returns `true` if the specified remote directory exists. |
| `EnumerateObjects(string remotePath, bool recursive = false, CancellationToken ct = default)` | `Task<IEnumerable<FtpObjectInfo>>` | Enumerates files and directories at the specified remote path. |

---

## Method Reference

### `Task DownloadFiles(string remotePath, string localPath, bool overwrite = false, bool recursive = false, CancellationToken ct = default)`

Downloads a file or directory from the FTP server to a local path. When `remotePath` points to a directory and `recursive` is `true`, all subdirectories and their contents are downloaded.

**Parameters:**
- `remotePath` (`string`) — Remote path of the file or directory to download
- `localPath` (`string`) — Local destination path
- `overwrite` (`bool`) — When `true`, overwrites existing local files (default: `false`)
- `recursive` (`bool`) — When `true`, downloads directories recursively (default: `false`)
- `ct` (`CancellationToken`) — Cancellation token (default: `default`)

**Returns:** `Task`

---

### `Task UploadFiles(string localPath, string remotePath, bool overwrite = false, bool recursive = false, CancellationToken ct = default)`

Uploads a file or directory to the FTP server. When `localPath` points to a directory and `recursive` is `true`, all subdirectories and their contents are uploaded.

**Parameters:**
- `localPath` (`string`) — Local path of the file or directory to upload
- `remotePath` (`string`) — Remote destination path
- `overwrite` (`bool`) — When `true`, overwrites existing remote files (default: `false`)
- `recursive` (`bool`) — When `true`, uploads directories recursively (default: `false`)
- `ct` (`CancellationToken`) — Cancellation token (default: `default`)

**Returns:** `Task`

---

### `Task Delete(string remotePath, CancellationToken ct = default)`

Deletes a file or directory on the FTP server.

**Parameters:**
- `remotePath` (`string`) — Remote path of the file or directory to delete
- `ct` (`CancellationToken`) — Cancellation token (default: `default`)

**Returns:** `Task`

---

### `void MoveItem(string remotePath, string newPath, bool overwrite = false)`

Moves or renames a file or directory on the FTP server. This method is **synchronous** — it returns `void` and should not be awaited.

**Parameters:**
- `remotePath` (`string`) — Current remote path
- `newPath` (`string`) — New remote path (rename or move destination)
- `overwrite` (`bool`) — When `true`, overwrites the destination if it exists (default: `false`)

**Returns:** `void`

---

### `Task<bool> FileExists(string remotePath, CancellationToken ct = default)`

Checks whether a file exists at the specified remote path.

**Parameters:**
- `remotePath` (`string`) — Remote path to check
- `ct` (`CancellationToken`) — Cancellation token (default: `default`)

**Returns:** `Task<bool>` — `true` if the remote file exists; `false` otherwise.

---

### `Task<bool> DirectoryExists(string remotePath, CancellationToken ct = default)`

Checks whether a directory exists at the specified remote path.

**Parameters:**
- `remotePath` (`string`) — Remote path to check
- `ct` (`CancellationToken`) — Cancellation token (default: `default`)

**Returns:** `Task<bool>` — `true` if the remote directory exists; `false` otherwise.

---

### `Task<IEnumerable<FtpObjectInfo>> EnumerateObjects(string remotePath, bool recursive = false, CancellationToken ct = default)`

Lists files and directories at the specified remote path.

**Parameters:**
- `remotePath` (`string`) — Remote path to enumerate
- `recursive` (`bool`) — When `true`, includes subdirectories and their contents recursively (default: `false`)
- `ct` (`CancellationToken`) — Cancellation token (default: `default`)

**Returns:** `Task<IEnumerable<FtpObjectInfo>>` — Collection of `FtpObjectInfo` items describing each file and directory found.

---

## Return Types

### `FtpObjectInfo`

Describes a single file or directory entry returned by `EnumerateObjects`.

| Property | Type | Description |
|----------|------|-------------|
| `FullName` | `string` | Full remote path of the item. |
| `Name` | `string` | File or directory name (without path). |
| `Size` | `long` | Size in bytes. `0` for directories. |
| `Created` | `DateTime` | Creation timestamp (server-reported; may be `DateTime.MinValue` if not supported). |
| `Modified` | `DateTime` | Last-modified timestamp (server-reported). |
| `Type` | `FtpObjectType` | Whether the item is a file, directory, symbolic link, or other. |
| `OwnerPermissions` | `FtpPermissions` | Unix-style owner permissions (flags: `Read`, `Write`, `Execute`). |
| `GroupPermissions` | `FtpPermissions` | Unix-style group permissions. |
| `OthersPermissions` | `FtpPermissions` | Unix-style other permissions. |

---

## Options & Configuration

### `FtpScopeOptions`

Options for configuring an FTP/FTPS/SFTP session. Properties are grouped by concern below.

#### Connection

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Host` | `string` | — | FTP server hostname or IP address. |
| `Port` | `int?` | — | Server port. When `null`, the default for the chosen protocol is used (21 for FTP/FTPS Explicit, 990 for FTPS Implicit, 22 for SFTP). |
| `Timeout` | `TimeSpan?` | — | Connection timeout. When `null`, the underlying library default applies. |

#### Authentication

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Username` | `string` | — | Username for credential-based authentication. |
| `Password` | `string` | — | Password for credential-based authentication. |
| `UseAnonymousLogin` | `bool` | `false` | When `true`, connects anonymously and ignores `Username` and `Password`. |
| `ClientCertificatePath` | `string` | — | Path to a client certificate file for FTPS or SFTP certificate authentication. |
| `ClientCertificatePassword` | `string` | — | Password protecting the client certificate file. |

#### Protocol & Security

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `UseSftp` | `bool` | `false` | When `true`, uses SFTP (SSH File Transfer Protocol) instead of FTP/FTPS. |
| `FtpsMode` | `FtpsMode` | `FtpsMode.None` | FTPS security mode. `None` = plain FTP, `Explicit` = STARTTLS on port 21, `Implicit` = TLS from the start on port 990. Ignored when `UseSftp` is `true`. |
| `SslProtocols` | `FtpSslProtocols` | `FtpSslProtocols.Auto` | SSL/TLS protocol version for FTPS. Relevant only when `FtpsMode` is not `None`. |
| `AcceptAllCertificates` | `bool` | `false` | When `true`, accepts all server certificates without validation. **Warning: Use only in development or isolated test environments — this disables protection against man-in-the-middle attacks.** |

#### Proxy

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ProxyType` | `FtpProxyType` | `FtpProxyType.None` | Proxy protocol to route the connection through. |
| `ProxyServer` | `string` | — | Proxy server hostname. Required when `ProxyType` is not `None`. |
| `ProxyPort` | `int?` | — | Proxy server port. Required when `ProxyType` is not `None`. |
| `ProxyUsername` | `string` | — | Username for proxy authentication. |
| `ProxyPassword` | `string` | — | Password for proxy authentication. |

---

## Enum Reference

**`FtpsMode`**: `None` (plain FTP), `Explicit` (STARTTLS), `Implicit` (TLS from connect)

**`FtpSslProtocols`** (`[Flags]`):

| Value | Notes |
|-------|-------|
| `Auto` | Lets the OS negotiate the best available protocol. **Recommended.** |
| `TLS_1_2` | Require TLS 1.2. |
| `TLS_1_0` | **`[Obsolete]`** — weak protocol; do not use. |
| `TLS_1_1` | **`[Obsolete]`** — weak protocol; do not use. |

**`FtpProxyType`**: `None`, `Socks4`, `Socks5`, `Http`

**`FtpObjectType`**: `Directory`, `File`, `Link`, `Other`

**`FtpPermissions`** (`[Flags]`): `None`, `Read`, `Write`, `Execute`

---

## Common Patterns

### Connect to a plain FTP server and download a file

```csharp
[Workflow]
public async void Execute()
{
    await using var session = await ftp.UseFtpSession(new FtpScopeOptions
    {
        Host = "ftp.example.com",
        Username = "ftpuser",
        Password = "s3cr3t"
    });

    await session.DownloadFiles(
        remotePath: "/reports/monthly.csv",
        localPath: @"C:\Data\monthly.csv",
        overwrite: true);
}
```

### Connect over SFTP and upload a directory recursively

```csharp
[Workflow]
public async void Execute()
{
    await using var session = await ftp.UseFtpSession(new FtpScopeOptions
    {
        Host = "sftp.example.com",
        UseSftp = true,
        Username = "sftpuser",
        Password = "s3cr3t"
    });

    await session.UploadFiles(
        localPath: @"C:\Exports\2025-Q1",
        remotePath: "/incoming/2025-Q1",
        overwrite: false,
        recursive: true);
}
```

### Connect over FTPS (Explicit) with a client certificate

```csharp
[Workflow]
public async void Execute()
{
    await using var session = await ftp.UseFtpSession(new FtpScopeOptions
    {
        Host = "secure-ftp.example.com",
        FtpsMode = FtpsMode.Explicit,
        SslProtocols = FtpSslProtocols.TLS_1_2,
        Username = "ftpsuser",
        ClientCertificatePath = @"C:\Certs\client.pfx",
        ClientCertificatePassword = "certpass"
    });

    await session.UploadFiles(
        localPath: @"C:\Invoices\invoice_001.pdf",
        remotePath: "/invoices/invoice_001.pdf",
        overwrite: true);
}
```

### Enumerate remote files and filter by type

```csharp
[Workflow]
public async void Execute()
{
    await using var session = await ftp.UseFtpSession(new FtpScopeOptions
    {
        Host = "ftp.example.com",
        Username = "ftpuser",
        Password = "s3cr3t"
    });

    var items = await session.EnumerateObjects("/data", recursive: false);
    foreach (var item in items)
    {
        if (item.Type == FtpObjectType.File)
            Log($"{item.Name} — {item.Size} bytes, modified {item.Modified:yyyy-MM-dd}");
    }
}
```

### Conditionally download and clean up remote files

```csharp
[Workflow]
public async void Execute()
{
    await using var session = await ftp.UseFtpSession(new FtpScopeOptions
    {
        Host = "ftp.example.com",
        Username = "ftpuser",
        Password = "s3cr3t"
    });

    const string remotePath = "/outbox/result.csv";

    if (await session.FileExists(remotePath))
    {
        await session.DownloadFiles(remotePath, @"C:\Data\result.csv", overwrite: true);

        // Archive by moving — MoveItem is synchronous, no await
        session.MoveItem(remotePath, "/archive/result.csv", overwrite: true);
    }
    else
    {
        Log("File not found on server.");
    }
}
```

### Route through a SOCKS5 proxy

```csharp
[Workflow]
public async void Execute()
{
    await using var session = await ftp.UseFtpSession(new FtpScopeOptions
    {
        Host = "ftp.example.com",
        Username = "ftpuser",
        Password = "s3cr3t",
        ProxyType = FtpProxyType.Socks5,
        ProxyServer = "proxy.internal",
        ProxyPort = 1080,
        ProxyUsername = "proxyuser",
        ProxyPassword = "proxypass"
    });

    await session.DownloadFiles("/data/export.zip", @"C:\Data\export.zip", overwrite: true);
}
```
