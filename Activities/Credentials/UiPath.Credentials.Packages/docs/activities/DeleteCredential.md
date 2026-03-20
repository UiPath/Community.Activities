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

- If no credential with the specified `Target` exists, the activity returns `false` without throwing an exception.
- This activity uses the Windows Credential Manager API (via the `CredentialManagement` library) and requires Windows.
