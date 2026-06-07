# Decrypt File

`UiPath.Cryptography.Activities.DecryptFile`

Decrypts a file using a symmetric algorithm and key, or using PGP with a private key. The result is written to a new file.

**Package:** `UiPath.Cryptography.Activities`
**Category:** Cryptography

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Description |
|------|-------------|------|------|----------|---------|-------------|
| `Algorithm` | Algorithm | Property | `EncryptionAlgorithm` | Yes |  | The cryptographic algorithm to use. See the Enum Reference below. |
| `InputFilePath` | File path | InArgument | `string` | Conditional |  | The path to the file that you want to decrypt. Paired with a hidden `IResource` alternative selectable via a designer menu action. |
| `Key` | Key | InArgument | `string` | Conditional |  | The key used to decrypt the file. Provide either `Key` or `KeySecureString`. Symmetric algorithms only. |
| `KeySecureString` | Key secure string | InArgument | `SecureString` | Conditional |  | Secure-string variant of the key. Provide either `Key` or `KeySecureString`. Symmetric algorithms only. |
| `KeyEncoding` | Key encoding | InArgument | `Encoding` |  |  | The encoding used to interpret the key. Symmetric algorithms only. |
| `Format` | Wire format | Property | `SymmetricWireFormat` |  | `Classic` | The symmetric ciphertext layout to decrypt. Must match the format used at encrypt time. Symmetric algorithms only. |
| `KeyFormat` | Key bytes format | Property | `KeyBytesFormat` |  | `Encoded` | How the `Key` string is interpreted. `Hex` or `Base64` are required when `Format = Raw`. Symmetric algorithms only. |
| `KdfIterations` | KDF iterations | InArgument | `int` |  | `0` | PBKDF2 iteration count. Must match the value used at encrypt time. `0` uses the format's OWASP-recommended default. Rejected for `Classic` and `Raw`. |
| `OutputFilePath` | Output file path | InArgument | `string` |  |  | The full path where the decrypted file will be saved. When empty, the file is written next to the input file using the name `<input-name>_Decrypted<input-extension>`. |
| `OutputFileName` | Decrypted file name | InArgument | `string` |  |  | The file name to use for the decrypted file. Honored when `OutputFilePath` is empty. |
| `PrivateKeyFilePath` | Private key file path | InArgument | `string` | Conditional |  | Path to your PGP private key file. Required when `Algorithm = PGP`. |
| `Passphrase` | Passphrase | InArgument | `string` | Conditional |  | Passphrase that unlocks the private key. Provide either `Passphrase` or `PassphraseSecureString`. PGP only. |
| `PassphraseSecureString` | Passphrase (secure) | InArgument | `SecureString` | Conditional |  | Secure-string variant of the passphrase. Provide either `Passphrase` or `PassphraseSecureString`. PGP only. |
| `PublicKeyFilePath` | Public key file path | InArgument | `string` | Conditional |  | Path to the signer's PGP public key file. Required only when `VerifySignature = True`. |

### Configuration

| Name | Display Name | Type | Required | Default | Description |
|------|-------------|------|----------|---------|-------------|
| `Overwrite` | Overwrite | `bool` | Yes |  | If a file already exists at the output path, this overwrites it. |
| `VerifySignature` | Verify signature | `bool` |  | `false` | When enabled, verifies the PGP signature of the decrypted data using the public key. PGP only. |
| `ContinueOnError` | Continue on error | `InArgument<bool>` |  |  | Specifies if the automation should continue when the activity throws an error. |

## Valid Configurations

The activity has two modes selected by `Algorithm`:

**Symmetric mode** (`AESGCM`, `ChaCha20Poly1305`, `AES`, `TripleDES`, `DES`, `RC2`, `Rijndael`):
- Provide `InputFilePath`, `Algorithm`, and exactly one of `Key` / `KeySecureString`.
- `KeyEncoding` defaults to UTF-8.
- `Format` defaults to `Classic` — must match what was used at encrypt time. The IV (when present) is read from the ciphertext stream prefix automatically.
- For `Owasp2026` and `OpenSslEnc`: set `KdfIterations` to the same value used at encrypt time.
- For `Raw`: set `KeyFormat = Hex` or `Base64` and supply the same raw key bytes used at encrypt time.
- PGP properties (private/public key, passphrase, `VerifySignature`) are ignored.

**PGP mode** (`Algorithm = PGP`):
- Provide `InputFilePath`, `PrivateKeyFilePath`, and exactly one of `Passphrase` / `PassphraseSecureString`.
- Set `VerifySignature = True` and provide `PublicKeyFilePath` to additionally verify the signature embedded in the encrypted payload.
- Symmetric properties (`Key`, `KeySecureString`, `KeyEncoding`, `Format`, `KeyFormat`, `KdfIterations`) are ignored.

The default symmetric format (`Classic`) is UiPath-specific (`salt(8) || IV || ciphertext [|| tag]`, PBKDF2-HMAC-SHA1 @ 10 000 iterations). Use `Raw` or `OpenSslEnc` to decrypt ciphertext produced by `openssl enc`, Java `javax.crypto`, Python `cryptography`, browser tools, etc. See `docs/symmetric-wire-format.md` for byte layouts and decoder examples.

### Enum Reference

**`EncryptionAlgorithm`**: `AESGCM`, `ChaCha20Poly1305`, `PGP`, `AES` *(deprecated)*, `DES` *(deprecated)*, `RC2` *(deprecated)*, `Rijndael` *(deprecated)*, `TripleDES` *(deprecated)*.

**`SymmetricWireFormat`**: `Classic` (default), `Owasp2026`, `Raw`, `OpenSslEnc`.

**`KeyBytesFormat`**: `Encoded` (default — string is a password), `Hex`, `Base64`. The activity's dropdown only surfaces `Hex` / `Base64` because `Encoded` is the implicit non-Raw choice.

## XAML Example

Symmetric decrypt — Classic (default):

```xml
<ui:DecryptFile DisplayName="Decrypt File"
                Algorithm="AESGCM"
                InputFilePath="C:\temp\plain.txt.encrypted"
                Key="[passphrase]"
                OutputFilePath="C:\temp\plain.txt"
                Overwrite="True" />
```

Symmetric decrypt — `OpenSslEnc` (input produced by `openssl enc -pbkdf2 -iter 600000 -md sha256 -salt -k password`):

```xml
<ui:DecryptFile DisplayName="Decrypt File (openssl)"
                Algorithm="AES"
                Format="OpenSslEnc"
                InputFilePath="C:\temp\plain.txt.enc"
                Key="[passphrase]"
                OutputFilePath="C:\temp\plain.txt"
                Overwrite="True" />
```

PGP decrypt with signature verification:

```xml
<ui:DecryptFile DisplayName="Decrypt File (PGP)"
                Algorithm="PGP"
                InputFilePath="C:\temp\report.pgp"
                PrivateKeyFilePath="C:\keys\private.asc"
                Passphrase="[keyPassphrase]"
                VerifySignature="True"
                PublicKeyFilePath="C:\keys\sender_public.asc"
                OutputFilePath="C:\temp\report.txt"
                Overwrite="True" />
```

## Notes

- `Key` ↔ `KeySecureString` and `Passphrase` ↔ `PassphraseSecureString` are paired via a designer menu action: only one side of each pair is active at a time.
- This activity has no `OutArgument` — the decrypted bytes are written to the file at `OutputFilePath`.
