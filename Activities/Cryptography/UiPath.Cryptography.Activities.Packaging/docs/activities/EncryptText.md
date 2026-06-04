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
| `Encoding` | Encoding | InArgument | `Encoding` |  |  | The encoding used to interpret the input text and the key. Symmetric algorithms only. |
| `PublicKeyFilePath` | Public key file path | InArgument | `string` | Conditional |  | Path to the recipient's PGP public key file. Required when `Algorithm = PGP`. |
| `PrivateKeyFilePath` | Private key file path | InArgument | `string` | Conditional |  | Path to your PGP private key file. Required only when `SignData = True`. |
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
- `Encoding` defaults to UTF-8.

**PGP encrypt only** (`Algorithm = PGP`, `SignData = False`):
- Provide `Input` and `PublicKeyFilePath` (recipient).

**PGP encrypt + sign** (`Algorithm = PGP`, `SignData = True`):
- Provide `Input`, `PublicKeyFilePath`, `PrivateKeyFilePath`, and exactly one of `Passphrase` / `PassphraseSecureString`.

The symmetric format is UiPath-specific (`salt(8) || IV || ciphertext [|| tag]`, PBKDF2-HMAC-SHA1 @ 10 000 iterations) — see `docs/symmetric-wire-format.md`.

### Enum Reference

**`EncryptionAlgorithm`**: `AESGCM`, `ChaCha20Poly1305`, `PGP`, `AES` *(deprecated)*, `DES` *(deprecated)*, `RC2` *(deprecated)*, `Rijndael` *(deprecated)*, `TripleDES` *(deprecated)*.

## XAML Example

Symmetric encrypt (AES-GCM):

```xml
<ui:EncryptText DisplayName="Encrypt Text"
                Algorithm="AESGCM"
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
