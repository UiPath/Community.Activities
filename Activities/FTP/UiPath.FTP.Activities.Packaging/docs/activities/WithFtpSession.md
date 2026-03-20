# Use FTP Connection

`UiPath.FTP.Activities.WithFtpSession`

Connects to FTP server and provides a scope for other FTP activities.

**Package:** `UiPath.FTP.Activities`
**Category:** FTP

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `Body` | Body | Property | `Object` |  |  |  |  |
| `Host` | Host | Property | `Object` | Yes |  |  | The URL of the FTP server that you want to connect to. |
| `Port` | Port | Property | `Object` |  |  |  | The port of the FTP server that you want to connect to. |
| `Timeout` | Timeout (milliseconds) | Property | `Object` |  |  |  | The timeout value (in milliseconds) for the FTP/SFTP connection. If not set, the default timeout of the underlying library is used. |
| `Username` | Username | Property | `Object` |  |  |  | The username that will be used to connect to the FTP server. |
| `Password` | Password | Property | `Object` |  |  |  | The password that will be used to connect to the FTP server. |
| `SecurePassword` | Secure Password | Property | `Object` |  |  |  | The password in secure string format |
| `PasswordInputModeSwitch` | PasswordInputModeSwitch | Property | `Object` |  |  |  | The switch for password input mode |
| `UseAnonymousLogin` | Use Anonymous Login | Property | `Object` |  |  |  | When this box is checked, the username and password fields are ignored, and a standard anonymous user is used instead. |
| `FtpsMode` | FTPS Mode | Property | `Object` |  |  |  | Switches to the FTPS protocol. Choose one of the two available options: Explicit or Implicit. |
| `SslProtocols` | SSL Protocols | Property | `Object` |  |  |  | Select the SSL protocol to be used for the FTPS connection |
| `UseSftp` | Use SFTP | Property | `Object` |  |  |  | Check this box if you want to use the SFTP transfer protocol. |
| `ClientCertificatePath` | Client Certificate File | Property | `Object` |  |  |  | The path to the certificate used to verify the identity of the client. |
| `ClientCertificatePassword` | Client Certificate Password | Property | `Object` |  |  |  | The password for the client certificate. |
| `ClientCertificateSecurePassword` | Client Certificate Secure Password | Property | `Object` |  |  |  | The password for client certificate in secure string format |
| `CertificatePasswordInputModeSwitch` | SecurePasswordInputModeSwitch | Property | `Object` |  |  |  | The switch for certificate password input mode |
| `AcceptAllCertificates` | Accept All Certificates | Property | `Object` |  |  |  | If this box is checked, all certificates will be accepted, including the ones that are expired or not verified. |
| `ProxyType` | Proxy Type | Property | `Object` |  |  |  | The type of proxy |
| `ProxyServer` | Proxy Host | Property | `Object` |  |  |  | Proxy host name |
| `ProxyPort` | Proxy Host Port | Property | `Object` |  |  |  | Proxy port number |
| `ProxyUser` | Proxy Username | Property | `Object` |  |  |  | The user used for proxy if authentication is required |
| `ProxyPassword` | Proxy Password | Property | `Object` |  |  |  | The password used for proxy if authentication is required |
| `ProxySecurePassword` | Proxy Secure Password | Property | `Object` |  |  |  | The proxy password in secure string format |
| `ProxyPasswordInputModeSwitch` | ProxyPasswordInputModeSwitch | Property | `Object` |  |  |  |  |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `ContinueOnError` | Continue On Error | `Object` |  | Specifies if the automation should continue even when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `-` | - | - | `-` | - |

## XAML Example

```xml
<ftp:WithFtpSession DisplayName="Use FTP Connection" />
```
