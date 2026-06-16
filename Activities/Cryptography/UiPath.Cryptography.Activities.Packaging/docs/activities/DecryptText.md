# Decrypt Text

`UiPath.Cryptography.Activities.DecryptText`

Decrypts a text string using a symmetric algorithm, or using PGP with a private key. Returns the plaintext string.

**Package:** `UiPath.Cryptography.Activities`
**Category:** Cryptography

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Description |
|------|-------------|------|------|----------|---------|-------------|
| `Algorithm` | Algorithm | Property | `EncryptionAlgorithm` | Yes |  | The cryptographic algorithm to use. See the Enum Reference below. |
| `Input` | Text | InArgument | `string` | Yes |  | The encrypted text to decrypt (Base64-encoded for symmetric algorithms, ASCII-armored for PGP). |
| `Key` | Key | InArgument | `string` | Conditional |  | The key used to decrypt the input. Provide either `Key` or `KeySecureString`. Symmetric algorithms only. |
| `KeySecureString` | Key secure string | InArgument | `SecureString` | Conditional |  | Secure-string variant of the key. Symmetric algorithms only. |
| `Encoding` | Encoding | InArgument | `Encoding` |  |  | The encoding used to interpret the input text and the key. Symmetric algorithms only. |
| `Format` | Wire format | Property | `SymmetricWireFormat` |  | `Classic` | The symmetric ciphertext layout to decrypt. Must match the format used at encrypt time. Symmetric algorithms only. |
| `KeyFormat` | Key bytes format | Property | `KeyBytesFormat` |  | `Encoded` | How the `Key` string is interpreted. `Hex` or `Base64` are required when `Format = Raw`. Symmetric algorithms only. |
| `KdfIterations` | KDF iterations | InArgument | `int` |  | `0` | PBKDF2 iteration count. Must match the value used at encrypt time. `0` uses the format's OWASP-recommended default. Rejected for `Classic` and `Raw`. |
| `PrivateKeyFilePath` | Private key file path | InArgument | `string` | Conditional |  | Path to your PGP private key file. Required when `Algorithm = PGP`. |
| `Passphrase` | Passphrase | InArgument | `string` | Conditional |  | Passphrase that unlocks the private key. Provide either `Passphrase` or `PassphraseSecureString`. PGP only. |
| `PassphraseSecureString` | Passphrase (secure) | InArgument | `SecureString` | Conditional |  | Secure-string variant of the passphrase. PGP only. |
| `PublicKeyFilePath` | Public key file path | InArgument | `string` | Conditional |  | Path to the signer's PGP public key file. Required only when `VerifySignature = True`. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `VerifySignature` | Verify signature | `bool` | `false` | When enabled, verifies the PGP signature of the decrypted data using the public key. PGP only. |
| `ContinueOnError` | Continue on error | `InArgument<bool>` |  | Specifies if the automation should continue when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Result` | Decrypted text | OutArgument | `string` | The decrypted plaintext. |

## Valid Configurations

The activity has two modes selected by `Algorithm`:

**Symmetric mode** (`AESGCM`, `ChaCha20Poly1305`, `AES`, `TripleDES`, `DES`, `RC2`, `Rijndael`):
- Provide `Input`, `Algorithm`, and exactly one of `Key` / `KeySecureString`.
- `Encoding` defaults to UTF-8.
- `Format` defaults to `Classic` — must match what was used at encrypt time. The IV (when present) is read from the ciphertext stream prefix automatically.
- For `Owasp2026` and `OpenSslEnc`: set `KdfIterations` to the same value used at encrypt time (the iteration count is **not** carried in the wire format).
- For `Raw`: set `KeyFormat = Hex` or `Base64` and supply the same raw key bytes used at encrypt time.

**PGP mode** (`Algorithm = PGP`):
- Provide `Input`, `PrivateKeyFilePath`, and exactly one of `Passphrase` / `PassphraseSecureString`.
- Set `VerifySignature = True` and provide `PublicKeyFilePath` to additionally verify the embedded signature.
- Symmetric properties (`Key`, `KeySecureString`, `Encoding`, `Format`, `KeyFormat`, `KdfIterations`) are ignored.

The default symmetric format (`Classic`) is UiPath-specific (`salt(8) || IV || ciphertext [|| tag]`, PBKDF2-HMAC-SHA1 @ 10 000 iterations). Use `Raw` or `OpenSslEnc` to decrypt ciphertext produced by `openssl enc`, Java `javax.crypto`, Python `cryptography`, browser tools, etc. See `docs/symmetric-wire-format.md` for byte layouts and decoder examples.

### Enum Reference

**`EncryptionAlgorithm`**: `AESGCM`, `ChaCha20Poly1305`, `PGP`, `AES` *(deprecated)*, `DES` *(deprecated)*, `RC2` *(deprecated)*, `Rijndael` *(deprecated)*, `TripleDES` *(deprecated)*.

**`SymmetricWireFormat`**: `Classic` (default), `Owasp2026`, `Raw`, `OpenSslEnc`.

**`KeyBytesFormat`**: `Encoded` (default — string is a password), `Hex`, `Base64`. The activity's dropdown only surfaces `Hex` / `Base64` because `Encoded` is the implicit non-Raw choice.

## XAML Example

Symmetric decrypt — Classic (default):

```xml
<ui:DecryptText DisplayName="Decrypt Text"
                Algorithm="AESGCM"
                Input="[ciphertextBase64]"
                Key="[passphrase]"
                Result="[plaintext]" />
```

Symmetric decrypt — `Raw` with caller-supplied key:

```xml
<ui:DecryptText DisplayName="Decrypt Text (Raw)"
                Algorithm="AESGCM"
                Format="Raw"
                Input="[ciphertextBase64]"
                Key="a3f1b2c4d5e6f70819a0b1c2d3e4f5061718293a4b5c6d7e8f90a1b2c3d4e5f6"
                KeyFormat="Hex"
                Result="[plaintext]" />
```

Symmetric decrypt — `OpenSslEnc` (input produced by `openssl enc -pbkdf2 -iter 600000 -md sha256 -salt -k password`):

```xml
<ui:DecryptText DisplayName="Decrypt Text (openssl)"
                Algorithm="AES"
                Format="OpenSslEnc"
                Input="[ciphertextBase64]"
                Key="[passphrase]"
                Result="[plaintext]" />
```

PGP decrypt:

```xml
<ui:DecryptText DisplayName="Decrypt Text (PGP)"
                Algorithm="PGP"
                Input="[pgpArmoredText]"
                PrivateKeyFilePath="C:\keys\private.asc"
                Passphrase="[keyPassphrase]"
                Result="[plaintext]" />
```

## Notes

- `Key` ↔ `KeySecureString` and `Passphrase` ↔ `PassphraseSecureString` are paired via a designer menu action: only one side of each pair is active at a time.
- For PGP signature verification, also set `VerifySignature = True` and supply `PublicKeyFilePath`.
