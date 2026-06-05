# Encrypt File

`UiPath.Cryptography.Activities.EncryptFile`

Encrypts a file using a symmetric algorithm and key, or using PGP with a recipient's public key. The result is written to a new file.

**Package:** `UiPath.Cryptography.Activities`
**Category:** Cryptography

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Description |
|------|-------------|------|------|----------|---------|-------------|
| `Algorithm` | Algorithm | Property | `EncryptionAlgorithm` | Yes |  | The cryptographic algorithm to use. See the Enum Reference below. |
| `InputFilePath` | File path | InArgument | `string` | Conditional |  | The path to the file that you want to encrypt. Paired with a hidden `IResource` alternative selectable via a designer menu action. |
| `Key` | Key | InArgument | `string` | Conditional |  | The key used to encrypt the file. Provide either `Key` or `KeySecureString`. Symmetric algorithms only. |
| `KeySecureString` | Key secure string | InArgument | `SecureString` | Conditional |  | Secure-string variant of the key. Symmetric algorithms only. |
| `KeyEncoding` | Key encoding | InArgument | `Encoding` |  |  | The encoding used to interpret the key. Symmetric algorithms only. |
| `OutputFilePath` | Output file path | InArgument | `string` |  |  | The full path where the encrypted file will be saved. When empty, the file is written next to the input file using the name `<input-name>_Encrypted<input-extension>`. |
| `OutputFileName` | Encrypted file name | InArgument | `string` |  |  | The file name to use for the encrypted file. Honored when `OutputFilePath` is empty. |
| `PublicKeyFilePath` | Public key file path | InArgument | `string` | Conditional |  | Path to the recipient's PGP public key file. Required when `Algorithm = PGP`. |
| `PrivateKeyFilePath` | Private key file path | InArgument | `string` | Conditional |  | Path to your PGP private key file. Required only when `SignData = True`. |
| `Passphrase` | Passphrase | InArgument | `string` | Conditional |  | Passphrase that unlocks the private key (signing). Provide either `Passphrase` or `PassphraseSecureString`. PGP-sign only. |
| `PassphraseSecureString` | Passphrase (secure) | InArgument | `SecureString` | Conditional |  | Secure-string variant of the passphrase. PGP-sign only. |

### Configuration

| Name | Display Name | Type | Required | Default | Description |
|------|-------------|------|----------|---------|-------------|
| `Overwrite` | Overwrite | `bool` | Yes |  | If a file already exists at the output path, this overwrites it. |
| `SignData` | Sign data | `bool` |  | `false` | When enabled, signs the encrypted data using the private key. PGP only. |
| `ContinueOnError` | Continue on error | `InArgument<bool>` |  |  | Specifies if the automation should continue when the activity throws an error. |

## Valid Configurations

The activity has two modes selected by `Algorithm`:

**Symmetric mode** (`AESGCM`, `ChaCha20Poly1305`, `AES`, `TripleDES`, `DES`, `RC2`, `Rijndael`):
- Provide `InputFilePath`, `Algorithm`, and exactly one of `Key` / `KeySecureString`.
- `KeyEncoding` defaults to UTF-8.
- PGP properties (`PublicKeyFilePath`, `PrivateKeyFilePath`, `Passphrase`, `SignData`) are ignored.

**PGP encrypt only** (`Algorithm = PGP`, `SignData = False`):
- Provide `InputFilePath` and `PublicKeyFilePath` (recipient).
- Symmetric properties are ignored.

**PGP encrypt + sign** (`Algorithm = PGP`, `SignData = True`):
- Provide `InputFilePath`, `PublicKeyFilePath`, `PrivateKeyFilePath`, and exactly one of `Passphrase` / `PassphraseSecureString`.

The symmetric ciphertext format produced by this activity is UiPath-specific (`salt(8) || IV || ciphertext [|| tag]`, PBKDF2-HMAC-SHA1 @ 10 000 iterations). See `docs/symmetric-wire-format.md` — it is not directly compatible with `openssl enc` or other standard tools.

### Enum Reference

**`EncryptionAlgorithm`**: `AESGCM`, `ChaCha20Poly1305`, `PGP`, `AES` *(deprecated)*, `DES` *(deprecated)*, `RC2` *(deprecated)*, `Rijndael` *(deprecated)*, `TripleDES` *(deprecated)*.

## XAML Example

Symmetric encrypt (AES-GCM):

```xml
<ui:EncryptFile DisplayName="Encrypt File"
                Algorithm="AESGCM"
                InputFilePath="C:\temp\plain.txt"
                Key="[passphrase]"
                OutputFilePath="C:\temp\plain.txt.encrypted"
                Overwrite="True" />
```

PGP encrypt and sign:

```xml
<ui:EncryptFile DisplayName="Encrypt File (PGP + sign)"
                Algorithm="PGP"
                InputFilePath="C:\temp\report.txt"
                PublicKeyFilePath="C:\keys\recipient_public.asc"
                SignData="True"
                PrivateKeyFilePath="C:\keys\private.asc"
                Passphrase="[keyPassphrase]"
                OutputFilePath="C:\temp\report.pgp"
                Overwrite="True" />
```

## Notes

- `Key` ↔ `KeySecureString` and `Passphrase` ↔ `PassphraseSecureString` are paired via a designer menu action: only one side of each pair is active at a time.
- This activity has no `OutArgument` — the encrypted bytes are written to the file at `OutputFilePath`.
