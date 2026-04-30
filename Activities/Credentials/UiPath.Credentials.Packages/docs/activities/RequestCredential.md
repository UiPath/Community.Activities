# Request Credentials

`UiPath.Credentials.Activities.RequestCredential`

Displays the Windows Vista-style credential prompt dialog and returns the username and password entered by the user. Returns `true` if the user confirmed the dialog, or `false` if they cancelled.

**Package:** `UiPath.Credentials.Activities`
**Category:** System > Credentials
**Platform:** Windows only

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Description |
|------|-------------|------|------|----------|---------|-------------|
| `Message` | Message | InArgument | `String` | | | Optional message to display in the credential prompt dialog. |
| `Title` | Title | InArgument | `String` | | | Optional title for the credential prompt dialog. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Username` | Username | OutArgument | `String` | The username entered by the user. |
| `Password` | Password | OutArgument | `String` | The password entered by the user as a plain-text string. |
| `PasswordSecureString` | Secure String Password | OutArgument | `SecureString` | The password entered by the user as a `SecureString`. |
| `Result` | Result | OutArgument | `Boolean` | `True` if the user clicked OK; `False` if the user cancelled the dialog. |

## XAML Example

```xml
<cr:RequestCredential
    DisplayName="Request Credentials"
    Title="Enter your credentials"
    Message="Please provide your login details to continue."
    Username="[enteredUsername]"
    Password="[enteredPassword]"
    PasswordSecureString="[enteredSecurePassword]"
    Result="[userConfirmed]"
    xmlns:cr="clr-namespace:UiPath.Credentials.Activities;assembly=UiPath.Credentials.Activities" />
```

## Notes

- Both `Password` (plain string) and `PasswordSecureString` (secure string) output the same value. Use `PasswordSecureString` when passing to activities that accept `SecureString` inputs for better security.
- If `Result` is `false` (user cancelled), `Username`, `Password`, and `PasswordSecureString` are not populated.
- This activity requires an interactive desktop session where a user can respond to the dialog. It is not suitable for unattended automation.
- Uses the `VistaPrompt` from the `CredentialManagement` library with `GenericCredentials = true`.
