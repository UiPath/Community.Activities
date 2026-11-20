# Encrypt Text

`UiPath.Cryptography.Activities.EncryptText`

Encrypts a text string using a symmetric algorithm, or using PGP with a recipient's public key. Returns the encrypted output as a string.

**Package:** `UiPath.Cryptography.Activities`
**Category:** Cryptography

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Description |
|------|-------------|------|------|----------|---------|-------------|
| `Algorithm` | Algorithm | Property | `EncryptionAlgorithm` | Yes |  | The cryptographic algorithm to use. See the Enum Reference below. |
| `Input` | Text | InArgument | `string` | Yes |  | The text that you want to encrypt. |
| `Key` | Key | InArgument | `string` | Conditional |  | The key used to encrypt the input. Provide either `Key` or `KeySecureString`. Symmetric algorithms only. |
| `KeySecureString` | Key secure string | InArgument | `SecureString` | Conditional |  | Secure-string variant of the key. Symmetric algorithms only. |
| `Encoding` | Key encoding | InArgument | `Encoding` |  | UTF-8 | The encoding used to interpret the key/password in `Key`. Surfaced in the designer as a "Key encoding" dropdown. Symmetric algorithms only. |
| `PlaintextEncoding` | Text encoding | InArgument | `Encoding` |  | UTF-8 | The encoding used to convert the input text to bytes before encryption. Set this to match the encoding expected by the third-party tool that will consume the ciphertext. Symmetric algorithms only. |
| `Format` | Wire format | Property | `SymmetricWireFormat` |  | `Classic` | The symmetric ciphertext layout. `Classic` (default) is UiPath's byte-stable layout; `Owasp2026` uses the same layout with stronger KDF iterations; `Raw` is caller-supplied key + IV for third-party interop; `OpenSslEnc` produces `openssl enc`-compatible output. Symmetric algorithms only. |
| `KeyFormat` | Key bytes format | Property | `KeyBytesFormat` |  | `Encoded` | How the `Key` string is interpreted. `Hex` or `Base64` are required when `Format = Raw`; otherwise the key is treated as a password. Symmetric algorithms only. |
| `Iv` | IV | InArgument | `string` | Conditional |  | Initialization vector when `Format = Raw`. Interpreted per `KeyFormat`. Optional — leave empty to let the cipher generate one. Rejected for all other formats. Never reuse the same (Key, IV) pair — it breaks confidentiality, and under AEAD modes (AES-GCM, ChaCha20-Poly1305) also lets an attacker forge messages; use each pair at most once, or leave empty for a fresh random IV. |
| `KdfIterations` | KDF iterations | InArgument | `int` |  | `0` | PBKDF2 iteration count. `0` uses the format's OWASP-recommended default (1 300 000 for `Owasp2026`, 600 000 for `OpenSslEnc`). Rejected for `Classic` and `Raw`. |
| `AesKeySize` | AES key size | Property | `AesKeySize` |  | `Aes256` | AES key size in bits. Applies only when `Algorithm = AES` and `Format = OpenSslEnc`; ignored otherwise. Must match the key size the peer uses (e.g. `openssl enc -aes-128-cbc` / `-aes-192-cbc` / `-aes-256-cbc`). Not stored in the wire format — encrypt and decrypt sides must use matching values. |
| `PublicKeyFilePath` | Public key file path | InArgument | `string` | Conditional |  | Path to the recipient's PGP public key file. Required when `Algorithm = PGP`. Paired with a hidden `IResource` alternative (`PublicKeyFile`) selectable via a designer menu action. |
| `PrivateKeyFilePath` | Private key file path | InArgument | `string` | Conditional |  | Path to your PGP private key file. Required only when `SignData = True`. Paired with a hidden `IResource` alternative (`PrivateKeyFile`) selectable via a designer menu action. |
| `Passphrase` | Passphrase | InArgument | `string` | Conditional |  | Passphrase that unlocks the private key (signing). Provide either `Passphrase` or `PassphraseSecureString`. PGP-sign only. |
| `PassphraseSecureString` | Passphrase (secure) | InArgument | `SecureString` | Conditional |  | Secure-string variant of the passphrase. PGP-sign only. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `SignData` | Sign data | `bool` | `false` | When enabled, signs the encrypted data using the private key. PGP only. |
| `ContinueOnError` | Continue on error | `InArgument<bool>` |  | Specifies if the automation should continue when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Result` | Encrypted text | OutArgument | `string` | The encrypted text — Base64 for symmetric algorithms, ASCII-armored for PGP. |

## Valid Configurations

**Symmetric mode** (`AESGCM`, `ChaCha20Poly1305`, `AES`, `TripleDES`, `DES`, `RC2`, `Rijndael`):
- Provide `Input`, `Algorithm`, and exactly one of `Key` / `KeySecureString`.
- `Encoding` (key/password) and `PlaintextEncoding` (input text) are independent and **both default to UTF-8**. Set either one alone — e.g. change only `PlaintextEncoding` to match the encoding the consumer expects, while leaving the key as UTF-8.
- `Format` defaults to `Classic` — existing workflows that omit this property produce the same byte-stable output as before.
- For `Owasp2026` or `OpenSslEnc`: `KeyFormat` stays `Encoded`; optionally set `KdfIterations`.
- For `OpenSslEnc` with `Algorithm = AES`: optionally set `AesKeySize` (`Aes128` / `Aes192` / `Aes256`, default `Aes256`) to match the peer's `openssl enc -aes-N-cbc`.
- For `Raw`: set `KeyFormat = Hex` or `Base64` and supply a literal cipher key. `Iv` is optional. `KdfIterations` is rejected.

**PGP encrypt only** (`Algorithm = PGP`, `SignData = False`):
- Provide `Input` and `PublicKeyFilePath` (recipient). Symmetric properties (`Key`, `KeySecureString`, `Encoding`, `PlaintextEncoding`, `Format`, `KeyFormat`, `Iv`, `KdfIterations`) are ignored.

**PGP encrypt + sign** (`Algorithm = PGP`, `SignData = True`):
- Provide `Input`, `PublicKeyFilePath`, `PrivateKeyFilePath`, and exactly one of `Passphrase` / `PassphraseSecureString`.

The default symmetric format (`Classic`) is UiPath-specific (`salt(8) || IV || ciphertext [|| tag]`, PBKDF2-HMAC-SHA1 @ 10 000 iterations). Use `Raw` or `OpenSslEnc` for interop with `openssl enc`, Java `javax.crypto`, Python `cryptography`, browser tools, etc. See `docs/symmetric-wire-format.md` for byte layouts, decoder examples, and the per-format validation matrix.

### Enum Reference

**`EncryptionAlgorithm`**: `AESGCM`, `ChaCha20Poly1305`, `PGP`, `AES` *(deprecated)*, `DES` *(deprecated)*, `RC2` *(deprecated)*, `Rijndael` *(deprecated)*, `TripleDES` *(deprecated)*.

**`SymmetricWireFormat`**: `Classic` (default), `Owasp2026`, `Raw`, `OpenSslEnc`.

**`KeyBytesFormat`**: `Encoded` (default — string is a password), `Hex`, `Base64`. The activity's dropdown only surfaces `Hex` / `Base64` because `Encoded` is the implicit non-Raw choice.

**`AesKeySize`**: `Aes128`, `Aes192`, `Aes256` (default). Only consulted when `Format = OpenSslEnc` and `Algorithm = AES`.

## XAML Example

Symmetric encrypt — Classic (default):

```xml
<ui:EncryptText DisplayName="Encrypt Text"
                Algorithm="AESGCM"
                Input="hello world"
                Key="[passphrase]"
                Result="[ciphertextBase64]" />
```

Symmetric encrypt — `Raw` with caller-supplied key (third-party interop):

```xml
<ui:EncryptText DisplayName="Encrypt Text (Raw)"
                Algorithm="AESGCM"
                Format="Raw"
                Input="hello world"
                Key="a3f1b2c4d5e6f70819a0b1c2d3e4f5061718293a4b5c6d7e8f90a1b2c3d4e5f6"
                KeyFormat="Hex"
                Result="[ciphertextBase64]" />
```

Symmetric encrypt — `OpenSslEnc` (decryptable by `openssl enc -d -pbkdf2 -iter 600000 -md sha256`):

```xml
<ui:EncryptText DisplayName="Encrypt Text (openssl)"
                Algorithm="AES"
                Format="OpenSslEnc"
                Input="hello world"
                Key="[passphrase]"
                Result="[ciphertextBase64]" />
```

PGP encrypt and sign:

```xml
<ui:EncryptText DisplayName="Encrypt Text (PGP + sign)"
                Algorithm="PGP"
                Input="hello PGP"
                PublicKeyFilePath="C:\keys\recipient_public.asc"
                SignData="True"
                PrivateKeyFilePath="C:\keys\private.asc"
                Passphrase="[keyPassphrase]"
                Result="[pgpArmoredText]" />
```

## Notes

- `Key` ↔ `KeySecureString` and `Passphrase` ↔ `PassphraseSecureString` are paired via a designer menu action: only one side of each pair is active at a time.
