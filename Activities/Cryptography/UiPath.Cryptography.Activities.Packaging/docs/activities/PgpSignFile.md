# PGP Sign File

`UiPath.Cryptography.Activities.PgpSignFile`

Creates a PGP binary signature of a file using a private key. The signed output contains the original data and the signature; verify it later with `PgpVerify` in `Signature` mode.

**Package:** `UiPath.Cryptography.Activities`
**Category:** Cryptography

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Description |
|------|-------------|------|------|----------|---------|-------------|
| `InputFilePath` | Input file path | InArgument | `string` | Conditional |  | The path to the file you want to sign. Paired with a hidden `IResource` alternative selectable via a designer menu action. |
| `PrivateKeyFilePath` | Private key file path | InArgument | `string` | Conditional |  | The path to your PGP private key file, used to sign the data. Paired with a hidden `IResource` alternative selectable via a designer menu action. |
| `OutputFilePath` | Output file path | InArgument | `string` | Yes |  | The full path where the signed file will be saved. |
| `Passphrase` | Passphrase | InArgument | `string` | Conditional |  | Passphrase that unlocks the private key. Provide either `Passphrase` or `PassphraseSecureString`. |
| `PassphraseSecureString` | Passphrase (secure) | InArgument | `SecureString` | Conditional |  | Secure-string variant of the passphrase. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `Overwrite` | Overwrite | `bool` | `false` | If a file already exists at the output path, this overwrites it. |
| `ContinueOnError` | Continue on error | `InArgument<bool>` |  | Specifies if the automation should continue when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `SignedFile` | Signed file | OutArgument | `ILocalResource` | A resource handle to the signed file that was written. Hidden in the designer (`[Browsable(false)]`) but populated at runtime — bind it to chain the signed file into a downstream activity. The signed bytes are also written to `OutputFilePath`. |

## Valid Configurations

- Provide `InputFilePath`, `PrivateKeyFilePath`, and `OutputFilePath`.
- Provide exactly one of `Passphrase` / `PassphraseSecureString`.

## XAML Example

```xml
<ui:PgpSignFile DisplayName="PGP Sign File"
                InputFilePath="C:\temp\report.txt"
                PrivateKeyFilePath="C:\keys\private.asc"
                Passphrase="[keyPassphrase]"
                OutputFilePath="C:\temp\report.txt.signed"
                Overwrite="True" />
```

## Notes

- `Passphrase` ↔ `PassphraseSecureString` are paired via a designer menu action: only one side is active at a time.
- Produces a binary OpenPGP signature. For a text-friendly armored signature that embeds the plaintext, use `PgpClearSignFile` instead.
- The signed bytes are written to the file at `OutputFilePath`. The activity also exposes a hidden `SignedFile` output (`OutArgument<ILocalResource>`) — see the Output table above.
