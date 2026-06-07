# PGP ClearSign File

`UiPath.Cryptography.Activities.PgpClearSignFile`

Creates a PGP clear-text signature of a file using a private key. The clearsigned output contains the original plaintext wrapped between `-----BEGIN PGP SIGNED MESSAGE-----` and `-----END PGP SIGNATURE-----` markers, so it is human-readable and tamper-evident.

**Package:** `UiPath.Cryptography.Activities`
**Category:** Cryptography

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Description |
|------|-------------|------|------|----------|---------|-------------|
| `InputFilePath` | Input file path | InArgument | `string` | Conditional |  | The path to the file you want to clearsign. Paired with a hidden `IResource` alternative selectable via a designer menu action. |
| `PrivateKeyFilePath` | Private key file path | InArgument | `string` | Conditional |  | The path to your PGP private key file, used to clearsign the data. Paired with a hidden `IResource` alternative selectable via a designer menu action. |
| `OutputFilePath` | Output file path | InArgument | `string` | Yes |  | The full path where the clearsigned file will be saved. |
| `Passphrase` | Passphrase | InArgument | `string` | Conditional |  | Passphrase that unlocks the private key. Provide either `Passphrase` or `PassphraseSecureString`. |
| `PassphraseSecureString` | Passphrase (secure) | InArgument | `SecureString` | Conditional |  | Secure-string variant of the passphrase. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `Overwrite` | Overwrite | `bool` | `false` | If a file already exists at the output path, this overwrites it. |
| `ContinueOnError` | Continue on error | `InArgument<bool>` |  | Specifies if the automation should continue when the activity throws an error. |

## Valid Configurations

- Provide `InputFilePath`, `PrivateKeyFilePath`, and `OutputFilePath`.
- Provide exactly one of `Passphrase` / `PassphraseSecureString`.

## XAML Example

```xml
<ui:PgpClearSignFile DisplayName="PGP ClearSign File"
                     InputFilePath="C:\temp\notice.txt"
                     PrivateKeyFilePath="C:\keys\private.asc"
                     Passphrase="[keyPassphrase]"
                     OutputFilePath="C:\temp\notice.txt.asc"
                     Overwrite="True" />
```

## Notes

- `Passphrase` ↔ `PassphraseSecureString` are paired via a designer menu action: only one side is active at a time.
- The input file must be text — clearsigning is designed for ASCII/UTF-8 content. For binary data, use `PgpSignFile`.
- Verify the output with `PgpVerify` in `ClearSignature` mode.
- This activity has no `OutArgument` — the clearsigned text is written to the file at `OutputFilePath`.
