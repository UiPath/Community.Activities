# PGP Generate Keys

`UiPath.Cryptography.Activities.PgpGenerateKeyPair`

Generates a PGP public/private key pair and saves them to the specified file paths.

**Package:** `UiPath.Cryptography.Activities`
**Category:** Cryptography

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `PublicKeyFilePath` | Public Key Output Path | InArgument | `string` | Yes |  |  | The file path where the generated PGP public key will be saved. |
| `PrivateKeyFilePath` | Private Key Output Path | InArgument | `string` | Yes |  |  | The file path where the generated PGP private key will be saved. |
| `Username` | Username | InArgument | `string` | Yes |  |  | The username (or email) to associate with the generated key pair. |
| `Password` | Passphrase | InArgument | `SecureString` | Yes |  |  | The passphrase to protect the generated private key. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `Overwrite` | Overwrite | `bool` |  | If files already exist at the output paths, selecting this overwrites them. |
| `ContinueOnError` | Continue On Error | `bool` |  | Specifies if the automation should continue even when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `PublicKeyFile` | Public Key File | OutArgument | `ILocalResource` | The generated public key as a file resource. |
| `PrivateKeyFile` | Private Key File | OutArgument | `ILocalResource` | The generated private key as a file resource. |

## Valid Configurations

- Required inputs: `PublicKeyFilePath`, `PrivateKeyFilePath`, `Username`, and `Password`.
- Set `Overwrite` to `True` to replace existing key files.
- Use output arguments `PublicKeyFile` and `PrivateKeyFile` for downstream file operations.

## XAML Example

```xml
<ui:PgpGenerateKeyPair DisplayName="PGP Generate Keys"
					   PublicKeyFilePath="C:\\keys\\public.asc"
					   PrivateKeyFilePath="C:\\keys\\private.asc"
					   Username="automation@uipath.com"
					   Password="[keyPassphrase]"
					   Overwrite="True"
					   PublicKeyFile="[publicKeyFile]"
					   PrivateKeyFile="[privateKeyFile]" />
```

