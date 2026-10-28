# PGP Generate Keys

`UiPath.Cryptography.Activities.PgpGenerateKeys`

Generates an OpenPGP public/private RSA key pair and saves them to the specified file paths in ASCII-armored format.

**Package:** `UiPath.Cryptography.Activities`
**Category:** Cryptography

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Description |
|------|-------------|------|------|----------|---------|-------------|
| `PublicKeyFilePath` | Public key output path | InArgument | `string` | Yes |  | The file path where the generated PGP public key will be saved. |
| `PrivateKeyFilePath` | Private key output path | InArgument | `string` | Yes |  | The file path where the generated PGP private key will be saved. |
| `UserId` | User ID | InArgument | `string` | Yes |  | OpenPGP User ID for the generated key. Conventionally an RFC 2822 mailbox, e.g. `Alice Doe <alice@example.com>`. Any UTF-8 string is accepted, but the mailbox form is what most PGP tools and key servers expect. |
| `Passphrase` | Passphrase | InArgument | `string` | Conditional |  | The passphrase to protect the generated private key. Provide either `Passphrase` or `PassphraseSecureString`. |
| `PassphraseSecureString` | Passphrase (secure) | InArgument | `SecureString` | Conditional |  | Secure-string variant of the passphrase. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `KeySize` | Key size | `RsaKeySize` | `Rsa4096` | The RSA key size in bits. Supported: 2048, 3072, 4096. Defaults to 4096 for enterprise-grade security. |
| `Overwrite` | Overwrite | `InArgument<bool>` | `false` | If files already exist at the output paths, this overwrites them. |
| `ContinueOnError` | Continue on error | `InArgument<bool>` |  | Specifies if the automation should continue when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `PublicKeyFile` | Public key file | OutArgument | `ILocalResource` | A resource handle to the generated public key file. Hidden in the designer (`[Browsable(false)]`) but populated at runtime — bind it to chain the file into a downstream activity. The key is also written to `PublicKeyFilePath`. |
| `PrivateKeyFile` | Private key file | OutArgument | `ILocalResource` | A resource handle to the generated private key file. Hidden in the designer (`[Browsable(false)]`) but populated at runtime. The key is also written to `PrivateKeyFilePath`. |

## Valid Configurations

- Provide `PublicKeyFilePath`, `PrivateKeyFilePath`, and `UserId`.
- Provide exactly one of `Passphrase` / `PassphraseSecureString`.
- `KeySize` defaults to `Rsa4096`; reduce to `Rsa3072` or `Rsa2048` only when the consuming system cannot handle 4096-bit keys.

### Enum Reference

**`RsaKeySize`**: `Rsa2048`, `Rsa3072`, `Rsa4096` *(default)*.

## XAML Example

```xml
<ui:PgpGenerateKeys DisplayName="PGP Generate Keys"
                    UserId="UiPath Automation &lt;automation@uipath.com&gt;"
                    PublicKeyFilePath="C:\keys\public.asc"
                    PrivateKeyFilePath="C:\keys\private.asc"
                    Passphrase="[keyPassphrase]"
                    KeySize="Rsa4096"
                    Overwrite="True" />
```

## Notes

- `Passphrase` ↔ `PassphraseSecureString` are paired via a designer menu action: only one side is active at a time.
- The generated keys are written to the file paths above. The activity also exposes hidden `PublicKeyFile` / `PrivateKeyFile` outputs (`OutArgument<ILocalResource>`) — see the Output table above.
- The default `KeySize` is `Rsa4096`, which meets NIST guidance through 2030+ and aligns with the strictest enterprise security recommendations.
