# Use FTP Connection

`UiPath.FTP.Activities.WithFtpSession`

Connects to FTP server and provides a scope for other FTP activities.

**Package:** `UiPath.FTP.Activities`
**Category:** FTP

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `Host` | Host | InArgument | `string` | Yes |  | `ftp.example.com` | The URL of the FTP server that you want to connect to. |
| `Port` | Port | InArgument | `int` |  |  | `21 for FTP, 22 for SFTP` | The port of the FTP server that you want to connect to. |
| `Timeout` | Timeout | InArgument | `int` |  |  | `Milliseconds` | The timeout value (in milliseconds) for the FTP/SFTP connection. If not set, the default timeout of the underlying library is used. |
| `Username` | Username | InArgument | `string` |  |  |  | The username that will be used to connect to the FTP server. |
| `Password` | Password | InArgument | `string` |  |  |  | The password that will be used to connect to the FTP server. |
| `SecurePassword` | Secure password | InArgument | `SecureString` |  |  |  | The password in secure string format. Provide either `Password` or `SecurePassword`. |
| `UseAnonymousLogin` | Use anonymous login | Property | `bool` |  |  |  | If turned on, the username and password fields are ignored, and a standard anonymous user is used instead. |
| `FtpsMode` | FTPS mode | Property | `FtpsMode` |  |  |  | Switches to the FTPS protocol. Choose one of the two available options: Explicit or Implicit. |
| `SslProtocols` | SSL protocols | Property | `FtpSslProtocols` |  |  | `Auto: negotiate the best available protocol` | Select the SSL protocol to be used for the FTPS connection. |
| `UseSftp` | Use SFTP | Property | `bool` |  |  |  | Turn on to use the SFTP transfer protocol. |
| `ClientCertificatePath` | Client certificate file | InArgument | `string` |  |  | `C:\Certificates\client.pfx` | The path to the certificate used to verify the identity of the client. |
| `ClientCertificatePassword` | Client certificate password | InArgument | `string` |  |  |  | The password for the client certificate. |
| `ClientCertificateSecurePassword` | Client certificate secure password | InArgument | `SecureString` |  |  |  | The password for client certificate in secure string format. |
| `AcceptAllCertificates` | Accept all certificates | Property | `bool` |  |  |  | If turned on, all certificates are accepted, including the ones that are expired or not verified. |
| `ProxyType` | Proxy type | Property | `FtpProxyType` |  |  |  | The type of proxy. |
| `ProxyServer` | Proxy host | InArgument | `string` |  |  | `proxy.example.com` | Proxy host name. |
| `ProxyPort` | Proxy port | InArgument | `int` |  |  |  | Proxy port number. |
| `ProxyUser` | Proxy username | InArgument | `string` |  |  |  | The user used for proxy if authentication is required. |
| `ProxyPassword` | Proxy password | InArgument | `string` |  |  |  | The password used for proxy if authentication is required. |
| `ProxySecurePassword` | Proxy secure password | InArgument | `SecureString` |  |  |  | The proxy password in secure string format. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `ContinueOnError` | Continue on error | `bool` |  | Specifies if the automation should continue even when the activity throws an error. |

## XAML Example

Declare the FTP namespace on the root `<Activity>` (see [overview](../overview.md#xaml-namespace)) — use the schema URI, not `clr-namespace:`:

```xml
xmlns:ftp="http://schemas.uipath.com/workflow/activities/ftp"
```

`WithFtpSession` is a **scope**. Its `Body` is an `ActivityAction<IFtpSession>` whose delegate argument is named `FtpSession`; child FTP activities go inside the body's `Sequence`. For SFTP set `UseSftp="True"`.

```xml
<ftp:WithFtpSession DisplayName="Use FTP Connection"
                    Host="[ftpHost]" Username="[ftpUser]" Password="[ftpPass]"
                    UseSftp="True" Port="22">
  <ftp:WithFtpSession.Body>
    <ActivityAction x:TypeArguments="ftp:IFtpSession">
      <ActivityAction.Argument>
        <DelegateInArgument x:TypeArguments="ftp:IFtpSession" Name="FtpSession" />
      </ActivityAction.Argument>
      <Sequence DisplayName="Do">
        <!-- child FTP activities here, e.g. ftp:EnumerateObjects -->
      </Sequence>
    </ActivityAction>
  </ftp:WithFtpSession.Body>
</ftp:WithFtpSession>
```

> Expressions above use VB syntax (`[ftpHost]`). In a C# project, bind non-literal inputs with `<CSharpValue>` child elements instead of bracket attributes.
