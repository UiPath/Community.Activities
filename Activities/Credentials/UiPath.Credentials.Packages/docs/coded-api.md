# Credentials — Coded Workflow API

`UiPath.Credentials.Activities`

Provides coded workflow operations for reading, storing, and deleting credentials in the Windows Credential Manager, and for prompting the user to enter credentials interactively.

**Service accessor:** `credentials` (type `ICredentialsService`)
**Required package:** `"UiPath.Credentials.Activities": "*"` in project.json dependencies

> **Windows only.** All methods in this service require a Windows desktop session. The service is not available on Linux or macOS, and `RequestCredential` additionally requires an interactive desktop — it must not be called from unattended robots or Windows services.

## Auto-Imported Namespaces

These namespaces are automatically available in coded workflows when this package is installed:

```
System
System.Security
CredentialManagement
UiPath.Credentials.Activities
UiPath.Credentials.Activities.API
UiPath.Credentials.Activities.API.Models
```

## Service Overview

The `credentials` service exposes all operations as **direct method calls** — there is no connection, scope, or handle to open. Call methods on the service accessor directly:

```csharp
var result = credentials.GetCredential("MyApp");
if (result.Found)
    Log(result.Username);
```

All methods are synchronous (no `Task`, no `await`). Each retrieval method returns a result record rather than throwing when a credential is not found — always check `Found` or `Confirmed` before using the returned values.

---

## Methods

### `GetCredentialResult GetCredential(string target, CredentialType credentialType = CredentialType.Generic, PersistanceType persistanceType = PersistanceType.Enterprise)`

Retrieves a credential from the Windows Credential Manager. The password is returned as a plain `string`.

**Parameters:**
- `target` (`string`) — Name of the credential entry as it appears in Credential Manager
- `credentialType` (`CredentialType`) — Type of credential to look up (default: `CredentialType.Generic`)
- `persistanceType` (`PersistanceType`) — Persistence scope (default: `PersistanceType.Enterprise`)

**Returns:** `GetCredentialResult` — Record with `Found`, `Username`, and `Password` (plain string).

---

### `GetSecureCredentialResult GetSecureCredential(string target, CredentialType credentialType = CredentialType.Generic, PersistanceType persistanceType = PersistanceType.Enterprise)`

Retrieves a credential from the Windows Credential Manager. The password is returned as a `SecureString` to avoid keeping it in plain text on the managed heap.

**Parameters:**
- `target` (`string`) — Name of the credential entry
- `credentialType` (`CredentialType`) — Type of credential to look up (default: `CredentialType.Generic`)
- `persistanceType` (`PersistanceType`) — Persistence scope (default: `PersistanceType.Enterprise`)

**Returns:** `GetSecureCredentialResult` — Record with `Found`, `Username`, and `Password` (`SecureString`).

---

### `bool AddCredential(string target, string username, string password, CredentialType credentialType = CredentialType.Generic, PersistanceType persistanceType = PersistanceType.Enterprise)`

Adds a new credential or updates an existing one in the Windows Credential Manager. Use this overload when the password is already held as a plain string.

**Parameters:**
- `target` (`string`) — Name of the credential entry
- `username` (`string`) — Username to store
- `password` (`string`) — Plain-text password to store
- `credentialType` (`CredentialType`) — Type of credential (default: `CredentialType.Generic`)
- `persistanceType` (`PersistanceType`) — Persistence scope (default: `PersistanceType.Enterprise`)

**Returns:** `bool` — `true` if the credential was saved successfully.

---

### `bool AddCredential(string target, string username, SecureString password, CredentialType credentialType = CredentialType.Generic, PersistanceType persistanceType = PersistanceType.Enterprise)`

Adds a new credential or updates an existing one using a `SecureString` password. Prefer this overload when the password originated from `GetSecureCredential` or `RequestCredential` to avoid promoting it to a plain string.

**Parameters:**
- `target` (`string`) — Name of the credential entry
- `username` (`string`) — Username to store
- `password` (`SecureString`) — Password to store
- `credentialType` (`CredentialType`) — Type of credential (default: `CredentialType.Generic`)
- `persistanceType` (`PersistanceType`) — Persistence scope (default: `PersistanceType.Enterprise`)

**Returns:** `bool` — `true` if the credential was saved successfully.

---

### `bool DeleteCredential(string target)`

Deletes a credential entry from the Windows Credential Manager.

**Parameters:**
- `target` (`string`) — Name of the credential entry to delete

**Returns:** `bool` — `true` if the credential was deleted successfully; `false` if it was not found.

---

### `RequestCredentialResult RequestCredential(string message = null, string title = null)`

Displays the standard Windows credential prompt dialog and returns whatever the user entered.

> **Interactive use only.** This method shows a blocking Win32 dialog (`CredUIPromptForWindowsCredentials`). It must not be called from unattended robots, Windows services, or any context without an interactive desktop session — doing so will either hang indefinitely or throw a platform exception.

**Parameters:**
- `message` (`string`) — Optional descriptive message shown in the prompt dialog (default: `null`)
- `title` (`string`) — Optional title of the prompt dialog (default: `null`)

**Returns:** `RequestCredentialResult` — Record with `Confirmed` (`bool`), `Username` (`string`), and `Password` (`SecureString`). When the user cancels the dialog, `Confirmed` is `false` and `Username`/`Password` are `null`.

---

## Return Types

### `GetCredentialResult`

Positional record returned by `GetCredential`.

| Property | Type | Description |
|----------|------|-------------|
| `Found` | `bool` | `true` if a matching credential was found in Credential Manager. |
| `Username` | `string` | The stored username. `null` when `Found` is `false`. |
| `Password` | `string` | The stored password as plain text. `null` when `Found` is `false`. |

---

### `GetSecureCredentialResult`

Positional record returned by `GetSecureCredential`.

| Property | Type | Description |
|----------|------|-------------|
| `Found` | `bool` | `true` if a matching credential was found in Credential Manager. |
| `Username` | `string` | The stored username. `null` when `Found` is `false`. |
| `Password` | `SecureString` | The stored password as a `SecureString`. `null` when `Found` is `false`. |

---

### `RequestCredentialResult`

Positional record returned by `RequestCredential`.

| Property | Type | Description |
|----------|------|-------------|
| `Confirmed` | `bool` | `true` if the user clicked OK; `false` if the user cancelled the dialog. |
| `Username` | `string` | The username the user entered. `null` when `Confirmed` is `false`. |
| `Password` | `SecureString` | The password the user entered. `null` when `Confirmed` is `false`. |

---

## Enum Reference

### `CredentialType` (from `CredentialManagement`)

Specifies the type of credential entry in Windows Credential Manager.

| Value | Description |
|-------|-------------|
| `Generic` | Generic credential not tied to a Windows authentication domain. **Default and most common type.** |
| `DomainPassword` | Domain credential used for Windows authentication. |
| `DomainCertificate` | Domain credential backed by a certificate. |
| `DomainVisiblePassword` | Domain credential visible in the UI. |
| `None` | No credential type specified. |

> **Note:** Only `Generic` and `DomainPassword` are fully supported and validated by the Credentials activities.

---

### `PersistanceType` (from `CredentialManagement`)

Controls how long a credential persists in Windows Credential Manager.

| Value | Description |
|-------|-------------|
| `Enterprise` | Credential persists across logon sessions and is roamed to domain controllers. **Default.** |
| `LocalComputer` | Credential persists across logon sessions on this machine only. |
| `Session` | Credential exists only for the current logon session and is lost on sign-out. |

---

## Common Patterns

### Retrieve a credential and use it

```csharp
[Workflow]
public void Execute()
{
    var result = credentials.GetCredential("MyApp\\DatabaseLogin");
    if (!result.Found)
        throw new InvalidOperationException("Credential 'MyApp\\DatabaseLogin' not found.");

    Log($"Connecting as {result.Username}");
    // use result.Username and result.Password
}
```

### Retrieve a credential with SecureString password

```csharp
[Workflow]
public void Execute()
{
    var result = credentials.GetSecureCredential("MyApp\\ApiKey");
    if (!result.Found)
        throw new InvalidOperationException("API key credential not found.");

    // result.Password is SecureString — avoids plain-text on the heap
    Log($"Retrieved credential for {result.Username}");
}
```

### Store a new credential

```csharp
[Workflow]
public void Execute()
{
    bool saved = credentials.AddCredential(
        target:          "MyApp\\ServiceAccount",
        username:        "svc_automation",
        password:        "P@ssw0rd!",
        credentialType:  CredentialType.Generic,
        persistanceType: PersistanceType.LocalComputer);

    if (!saved)
        throw new InvalidOperationException("Failed to save credential.");
}
```

### Rotate a credential — read, update, re-save

```csharp
[Workflow]
public void Execute()
{
    const string target = "MyApp\\ServiceAccount";

    var existing = credentials.GetSecureCredential(target);
    if (!existing.Found)
        throw new InvalidOperationException($"Credential '{target}' not found.");

    // Build new SecureString password from somewhere (e.g. a secret vault)
    var newPassword = new SecureString();
    foreach (char c in "NewP@ssw0rd!")
        newPassword.AppendChar(c);
    newPassword.MakeReadOnly();

    bool updated = credentials.AddCredential(
        target:   target,
        username: existing.Username,
        password: newPassword);

    Log(updated ? "Credential rotated." : "Rotation failed.");
}
```

### Prompt the user for credentials (attended only)

```csharp
[Workflow]
public void Execute()
{
    var result = credentials.RequestCredential(
        message: "Enter your VPN credentials to continue.",
        title:   "VPN Authentication");

    if (!result.Confirmed)
    {
        Log("User cancelled the credential prompt.");
        return;
    }

    Log($"Proceeding as {result.Username}");
    // use result.Password (SecureString) for the operation
}
```

### Delete a credential after use

```csharp
[Workflow]
public void Execute()
{
    bool deleted = credentials.DeleteCredential("MyApp\\TempToken");
    Log(deleted ? "Credential deleted." : "Credential not found — nothing to delete.");
}
```
