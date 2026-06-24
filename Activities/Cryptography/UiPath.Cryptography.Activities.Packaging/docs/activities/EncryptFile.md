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
| `KeyEncoding` | Key encoding | InArgument | `Encoding` |  | UTF-8 | The encoding used to interpret the key. Symmetric algorithms only. |
| `Format` | Wire format | Property | `SymmetricWireFormat` |  | `Classic` | The symmetric ciphertext layout. `Classic` (default) is UiPath's byte-stable layout; `Owasp2026` uses the same layout with stronger KDF iterations; `Raw` is caller-supplied key + IV for third-party interop; `OpenSslEnc` produces `openssl enc`-compatible output. Symmetric algorithms only. |
| `KeyFormat` | Key bytes format | Property | `KeyBytesFormat` |  | `Encoded` | How the `Key` string is interpreted. `Hex` or `Base64` are required when `Format = Raw`. Symmetric algorithms only. |
| `Iv` | IV | InArgument | `string` | Conditional |  | Initialization vector when `Format = Raw`. Interpreted per `KeyFormat`. Optional — leave empty to let the cipher generate one. Rejected for all other formats. |
| `KdfIterations` | KDF iterations | InArgument | `int` |  | `0` | PBKDF2 iteration count. `0` uses the format's OWASP-recommended default (1 300 000 for `Owasp2026`, 600 000 for `OpenSslEnc`). Rejected for `Classic` and `Raw`. |
| `AesKeySize` | AES key size | Property | `AesKeySize` |  | `Aes256` | AES key size in bits. Applies only when `Algorithm = AES` and `Format = OpenSslEnc`; ignored otherwise. Must match the key size the peer uses (e.g. `openssl enc -aes-128-cbc` / `-aes-192-cbc` / `-aes-256-cbc`). Not stored in the wire format — encrypt and decrypt sides must use matching values. |
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

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `EncryptedFile` | Encrypted file | OutArgument | `ILocalResource` | A resource handle to the file that was written. Hidden in the designer (`[Browsable(false)]`) but populated at runtime — bind it to chain the encrypted file into a downstream activity. The encrypted bytes are also written to disk at `OutputFilePath`, or — when `OutputFilePath` is empty — at the computed default path next to the input file (`<input-name>_Encrypted<input-extension>`). |

## Valid Configurations

The activity has two modes selected by `Algorithm`:

**Symmetric mode** (`AESGCM`, `ChaCha20Poly1305`, `AES`, `TripleDES`, `DES`, `RC2`, `Rijndael`):
- Provide `InputFilePath`, `Algorithm`, and exactly one of `Key` / `KeySecureString`.
- `KeyEncoding` defaults to UTF-8.
- `Format` defaults to `Classic` — existing workflows that omit this property produce the same byte-stable output as before.
- For `Owasp2026` or `OpenSslEnc`: `KeyFormat` stays `Encoded`; optionally set `KdfIterations`.
- For `OpenSslEnc` with `Algorithm = AES`: optionally set `AesKeySize` (`Aes128` / `Aes192` / `Aes256`, default `Aes256`) to match the peer's `openssl enc -aes-N-cbc`.
- For `Raw`: set `KeyFormat = Hex` or `Base64` and supply a literal cipher key. `Iv` is optional. `KdfIterations` is rejected.
- PGP properties (`PublicKeyFilePath`, `PrivateKeyFilePath`, `Passphrase`, `SignData`) are ignored.

**PGP encrypt only** (`Algorithm = PGP`, `SignData = False`):
- Provide `InputFilePath` and `PublicKeyFilePath` (recipient).
- Symmetric properties (including `Format`, `KeyFormat`, `Iv`, `KdfIterations`) are ignored.

**PGP encrypt + sign** (`Algorithm = PGP`, `SignData = True`):
- Provide `InputFilePath`, `PublicKeyFilePath`, `PrivateKeyFilePath`, and exactly one of `Passphrase` / `PassphraseSecureString`.

The default symmetric format (`Classic`) is UiPath-specific (`salt(8) || IV || ciphertext [|| tag]`, PBKDF2-HMAC-SHA1 @ 10 000 iterations). Use `Raw` or `OpenSslEnc` for interop with `openssl enc`, Java `javax.crypto`, Python `cryptography`, browser tools, etc. See `docs/symmetric-wire-format.md` for byte layouts, decoder examples, and the per-format validation matrix.

### Enum Reference

**`EncryptionAlgorithm`**: `AESGCM`, `ChaCha20Poly1305`, `PGP`, `AES` *(deprecated)*, `DES` *(deprecated)*, `RC2` *(deprecated)*, `Rijndael` *(deprecated)*, `TripleDES` *(deprecated)*.

**`SymmetricWireFormat`**: `Classic` (default), `Owasp2026`, `Raw`, `OpenSslEnc`.

**`KeyBytesFormat`**: `Encoded` (default — string is a password), `Hex`, `Base64`. The activity's dropdown only surfaces `Hex` / `Base64` because `Encoded` is the implicit non-Raw choice.

**`AesKeySize`**: `Aes128`, `Aes192`, `Aes256` (default). Only consulted when `Format = OpenSslEnc` and `Algorithm = AES`.

## XAML Example

Symmetric encrypt — Classic (default):

```xml
<ui:EncryptFile DisplayName="Encrypt File"
                Algorithm="AESGCM"
                InputFilePath="C:\temp\plain.txt"
                Key="[passphrase]"
                OutputFilePath="C:\temp\plain.txt.encrypted"
                Overwrite="True" />
```

Symmetric encrypt — `OpenSslEnc` (decryptable by `openssl enc -d -pbkdf2 -iter 600000 -md sha256`):

```xml
<ui:EncryptFile DisplayName="Encrypt File (openssl)"
                Algorithm="AES"
                Format="OpenSslEnc"
                InputFilePath="C:\temp\plain.txt"
                Key="[passphrase]"
                OutputFilePath="C:\temp\plain.txt.enc"
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
- The encrypted bytes are written to the file at `OutputFilePath`, or — when `OutputFilePath` is empty — to the computed default path next to the input file (`<input-name>_Encrypted<input-extension>`). The activity also exposes a hidden `EncryptedFile` output (`OutArgument<ILocalResource>`) — see the Output table above.
