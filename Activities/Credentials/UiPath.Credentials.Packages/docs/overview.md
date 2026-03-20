# UiPath Credentials Activities

**Package:** `UiPath.Credentials.Activities`

Activities for reading and writing credentials stored in the **Windows Credential Manager**. Use these activities to securely manage application credentials in automation workflows.

> **Platform:** All activities in this package are **Windows only**.

## Activities

| Activity | Description |
|----------|-------------|
| [Add Credentials](activities/AddCredential.md) | Stores a username and password in the Windows Credential Manager. |
| [Delete Credentials](activities/DeleteCredential.md) | Removes a stored credential from the Windows Credential Manager. |
| [Get Secure Credentials](activities/GetSecureCredential.md) | Retrieves a stored credential, returning the password as a `SecureString`. |
| [Request Credentials](activities/RequestCredential.md) | Displays a Windows credential prompt dialog and returns the entered username and password. |
