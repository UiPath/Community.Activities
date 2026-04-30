# Use FTP Connection

`UiPath.FTP.Activities.WithFtpSession`

Connects to FTP server and provides a scope for other FTP activities.

**Package:** `UiPath.FTP.Activities`
**Category:** FTP

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `Host` | Host | InArgument | `string` | Yes |  |  | The URL of the FTP server that you want to connect to. |
| `Port` | Port | InArgument | `int` |  |  |  | The port of the FTP server that you want to connect to. |
| `Timeout` | Timeout (milliseconds) | InArgument | `int` |  |  |  | The timeout value (in milliseconds) for the FTP/SFTP connection. If not set, the default timeout of the underlying library is used. |
| `Username` | Username | InArgument | `string` |  |  |  | The username that will be used to connect to the FTP server. |
| `Password` | Password | InArgument | `string` |  |  |  | The password that will be used to connect to the FTP server. |
| `SecurePassword` | Secure Password | InArgument | `SecureString` |  |  |  | The password in secure string format. Provide either `Password` or `SecurePassword`. |
| `UseAnonymousLogin` | Use Anonymous Login | Property | `bool` |  |  |  | When this box is checked, the username and password fields are ignored, and a standard anonymous user is used instead. |
| `FtpsMode` | FTPS Mode | Property | `FtpsMode` |  |  |  | Switches to the FTPS protocol. Choose one of the two available options: Explicit or Implicit. |
| `SslProtocols` | SSL Protocols | Property | `FtpSslProtocols` |  |  |  | Select the SSL protocol to be used for the FTPS connection. |
| `UseSftp` | Use SFTP | Property | `bool` |  |  |  | Check this box if you want to use the SFTP transfer protocol. |
| `ClientCertificatePath` | Client Certificate File | InArgument | `string` |  |  |  | The path to the certificate used to verify the identity of the client. |
| `ClientCertificatePassword` | Client Certificate Password | InArgument | `string` |  |  |  | The password for the client certificate. |
| `ClientCertificateSecurePassword` | Client Certificate Secure Password | InArgument | `SecureString` |  |  |  | The password for client certificate in secure string format. |
| `AcceptAllCertificates` | Accept All Certificates | Property | `bool` |  |  |  | If this box is checked, all certificates will be accepted, including the ones that are expired or not verified. |
| `ProxyType` | Proxy Type | Property | `FtpProxyType` |  |  |  | The type of proxy. |
| `ProxyServer` | Proxy Host | InArgument | `string` |  |  |  | Proxy host name. |
| `ProxyPort` | Proxy Host Port | InArgument | `int` |  |  |  | Proxy port number. |
| `ProxyUser` | Proxy Username | InArgument | `string` |  |  |  | The user used for proxy if authentication is required. |
| `ProxyPassword` | Proxy Password | InArgument | `string` |  |  |  | The password used for proxy if authentication is required. |
| `ProxySecurePassword` | Proxy Secure Password | InArgument | `SecureString` |  |  |  | The proxy password in secure string format. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `ContinueOnError` | Continue On Error | `bool` |  | Specifies if the automation should continue even when the activity throws an error. |

## XAML Example

```xml
<ftp:WithFtpSession DisplayName="Use FTP Connection" Host="ftp.example.com" Username="[ftpUser]" Password="[ftpPass]" />
```
