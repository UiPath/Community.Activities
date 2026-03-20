# Add Credentials

`UiPath.Credentials.Activities.AddCredential`

Stores a username and password credential in the Windows Credential Manager. Returns `true` if the credential was saved successfully.

**Package:** `UiPath.Credentials.Activities`
**Category:** System > Credentials
**Platform:** Windows only

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Description |
|------|-------------|------|------|----------|---------|-------------|
| `Target` | Target | InArgument | `String` | Yes | | The application name or network location used as the credential key in Windows Credential Manager. |
| `Username` | Username | InArgument | `String` | Yes | | The username to store. |
| `CredentialType` | Credential Type | Property | `CredentialType` | | `Generic` | The type of credential. Only `Generic` and `DomainPassword` are supported; other values cause a validation error. |
| `Password` | Password | InArgument | `String` | | | The plain-text password to store. Provide exactly one of `Password` or `PasswordSecureString`. |
| `PasswordSecureString` | Secure String Password | InArgument | `SecureString` | | | The password as a `SecureString`. Provide exactly one of `Password` or `PasswordSecureString`. |
| `PersistanceType` | PersistanceType | Property | `PersistanceType` | | `Enterprise` | Controls how long the credential persists in Windows Credential Manager. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Result` | Result | OutArgument | `Boolean` | `True` if the credential was saved successfully; `False` otherwise. |

### Enum Reference

**`CredentialType`**: `Generic`, `DomainPassword`

> Only `Generic` and `DomainPassword` are valid for this activity. Specifying any other value raises a validation error in the designer.

**`PersistanceType`**:

| Value | Description |
|-------|-------------|
| `Session` | Persists only for the life of the current logon session. |
| `LocalComputer` | Persists on the local machine until explicitly deleted. |
| `Enterprise` | Persists across all future logon sessions until explicitly deleted. |

## Valid Configurations

Exactly one password field must be provided:

**Plain-text password** — Set `Password` to a string value. Leave `PasswordSecureString` unset.

**Secure string password** — Set `PasswordSecureString` to a `SecureString` variable. Leave `Password` unset.

Providing both or neither raises an exception at runtime.

## XAML Example

```xml
<!-- Using plain-text password -->
<cr:AddCredential
    DisplayName="Add Credentials"
    Target="MyApplication"
    Username="domain\jsmith"
    Password="[myPassword]"
    CredentialType="Generic"
    PersistanceType="Enterprise"
    Result="[credentialSaved]"
    xmlns:cr="clr-namespace:UiPath.Credentials.Activities;assembly=UiPath.Credentials.Activities" />
```

```xml
<!-- Using secure string password -->
<cr:AddCredential
    DisplayName="Add Credentials"
    Target="MyApplication"
    Username="domain\jsmith"
    PasswordSecureString="[mySecurePassword]"
    CredentialType="Generic"
    PersistanceType="Enterprise"
    Result="[credentialSaved]"
    xmlns:cr="clr-namespace:UiPath.Credentials.Activities;assembly=UiPath.Credentials.Activities" />
```

## Notes

- `Password` and `PasswordSecureString` are mutually exclusive. Providing both throws `ArgumentException`; providing neither throws `ArgumentNullException`.
- `CredentialType` accepts only `Generic` or `DomainPassword`. Using another value (e.g., `DomainCertificate`) produces a designer validation error.
- This activity uses the Windows Credential Manager API (via the `CredentialManagement` library) and requires Windows.
