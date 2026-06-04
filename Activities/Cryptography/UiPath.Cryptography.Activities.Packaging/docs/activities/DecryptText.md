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
- Input must be the Base64 string produced by `EncryptText`.

**PGP mode** (`Algorithm = PGP`):
- Provide `Input`, `PrivateKeyFilePath`, and exactly one of `Passphrase` / `PassphraseSecureString`.
- Set `VerifySignature = True` and provide `PublicKeyFilePath` to additionally verify the embedded signature.
- Symmetric properties (`Key`, `KeySecureString`, `Encoding`) are ignored.

The symmetric format is UiPath-specific (`salt(8) || IV || ciphertext [|| tag]`, PBKDF2-HMAC-SHA1 @ 10 000 iterations) — see `docs/symmetric-wire-format.md`.

### Enum Reference

**`EncryptionAlgorithm`**: `AESGCM`, `ChaCha20Poly1305`, `PGP`, `AES` *(deprecated)*, `DES` *(deprecated)*, `RC2` *(deprecated)*, `Rijndael` *(deprecated)*, `TripleDES` *(deprecated)*.

## XAML Example

Symmetric decrypt (AES-GCM):

```xml
<ui:DecryptText DisplayName="Decrypt Text"
                Algorithm="AESGCM"
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
