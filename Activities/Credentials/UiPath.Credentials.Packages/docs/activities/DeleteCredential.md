# Delete Credentials

`UiPath.Credentials.Activities.DeleteCredential`

Removes a stored credential from the Windows Credential Manager by its target name. Returns `true` if the credential was deleted successfully.

**Package:** `UiPath.Credentials.Activities`
**Category:** System > Credentials
**Platform:** Windows only

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Description |
|------|-------------|------|------|----------|---------|-------------|
| `Target` | Target | InArgument | `String` | Yes | | The application name or network location identifying the credential to delete. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Result` | Result | OutArgument | `Boolean` | `True` if the credential was deleted successfully; `False` if it was not found. |

## XAML Example

```xml
<cr:DeleteCredential
    DisplayName="Delete Credentials"
    Target="MyApplication"
    Result="[credentialDeleted]"
    xmlns:cr="clr-namespace:UiPath.Credentials.Activities;assembly=UiPath.Credentials.Activities" />
```

## Notes

- This activity uses the Windows Credential Manager API (via the `CredentialManagement` library) and requires Windows.
- The behavior when a credential is not found depends on the underlying `CredentialManagement` library. Typically, the activity returns `false` if the credential does not exist, but this behavior is not guaranteed by the code in this package.
