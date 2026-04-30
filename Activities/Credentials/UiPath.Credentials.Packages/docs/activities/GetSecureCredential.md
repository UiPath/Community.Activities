# Get Secure Credentials

`UiPath.Credentials.Activities.GetSecureCredential`

Retrieves a stored credential from the Windows Credential Manager, returning the password as a `SecureString`. Returns `true` if the credential was found and loaded successfully.

**Package:** `UiPath.Credentials.Activities`
**Category:** System > Credentials
**Platform:** Windows only

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Description |
|------|-------------|------|------|----------|---------|-------------|
| `Target` | Target | InArgument | `String` | Yes | | The application name or network location identifying the credential to retrieve. |
| `CredentialType` | Credential Type | Property | `CredentialType` | | `Generic` | The type of credential to look up. |
| `PersistanceType` | PersistanceType | Property | `PersistanceType` | | `Enterprise` | The persistence scope of the credential to look up. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Username` | Username | OutArgument | `String` | The username retrieved from Credential Manager. |
| `Password` | Password | OutArgument | `SecureString` | The password retrieved from Credential Manager as a `SecureString`. |
| `Result` | Result | OutArgument | `Boolean` | `True` if the credential was found and loaded; `False` if no matching credential exists. |

### Enum Reference

**`CredentialType`**: `Generic`, `DomainPassword`, `DomainCertificate`, `DomainVisiblePassword`, `GenericCertificate`, `DomainExtended`, `Maximum`, `MaximumEx`

**`PersistanceType`**:

| Value | Description |
|-------|-------------|
| `Session` | Persists only for the life of the current logon session. |
| `LocalComputer` | Persists on the local machine until explicitly deleted. |
| `Enterprise` | Persists across all future logon sessions until explicitly deleted. |

## XAML Example

```xml
<cr:GetSecureCredential
    DisplayName="Get Secure Credentials"
    Target="MyApplication"
    CredentialType="Generic"
    PersistanceType="Enterprise"
    Username="[retrievedUsername]"
    Password="[retrievedSecurePassword]"
    Result="[credentialFound]"
    xmlns:cr="clr-namespace:UiPath.Credentials.Activities;assembly=UiPath.Credentials.Activities" />
```

## Notes

- The password output is a `SecureString`, which is safer to use in downstream activities than a plain string. Use `System.Runtime.InteropServices.Marshal.PtrToStringBSTR(...)` or pass it directly to activities that accept `SecureString` inputs.
- If no matching credential is found, `Result` is `false` and `Username`/`Password` are not set.
- This activity uses the Windows Credential Manager API (via the `CredentialManagement` library) and requires Windows.
