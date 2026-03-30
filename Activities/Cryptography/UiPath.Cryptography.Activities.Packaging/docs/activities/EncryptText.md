# Encrypt Text

`UiPath.Cryptography.Activities.EncryptText`

Encrypts a string with a key based on a specified key encoding and algorithm.

**Package:** `UiPath.Cryptography.Activities`
**Category:** Cryptography

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `Algorithm` | Algorithm | Property | `EncryptionAlgorithm` | Yes |  |  | A drop-down which enables you to select the encryption algorithm you want to use. |
| `Input` | Text | InArgument | `string` | Yes |  |  | The text that you want to encrypt. |
| `Key` | Key | InArgument | `string` |  |  |  | The key that you want to use to encrypt the specified file. Provide either `Key` or `KeySecureString`. |
| `KeySecureString` | Key Secure String | InArgument | `SecureString` |  |  |  | The secure string used to encrypt the input string. Provide either `Key` or `KeySecureString`. |
| `PublicKeyFilePath` | Public Key File Path | InArgument | `string` |  |  |  | The path to the PGP public key file used for encryption. |
| `PrivateKeyFilePath` | Private Key File Path | InArgument | `string` |  |  |  | The path to the PGP private key file used for signing. |
| `Passphrase` | Passphrase | InArgument | `SecureString` |  |  |  | The passphrase for the PGP private key used for signing. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `KeyEncodingString` | Key Encoding | `string` |  | The encoding used to interpret the key specified in the Key property. |
| `ContinueOnError` | Continue On Error | `bool` |  | Specifies if the automation should continue even when the activity throws an error. |
| `SignData` | Sign Data | `bool` |  | When enabled, signs the encrypted data using the private key. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Result` | Encrypted Text | OutArgument | `string` | The encrypted text, stored in a String variable. |

## Valid Configurations

- Symmetric encryption mode: set `Algorithm` and `Input`, then provide either `Key` or `KeySecureString`.
- PGP encryption mode: set `Algorithm` to PGP and provide `PublicKeyFilePath`.
- Signed PGP encryption mode: set `SignData` to `True` and also provide `PrivateKeyFilePath` and `Passphrase`.

## XAML Example

```xml
<ui:EncryptText DisplayName="Encrypt Text"
				Algorithm="AESGCM"
				Input="[plainText]"
				Key="my-secret-key"
				KeyEncodingString="65001"
				Result="[encryptedText]" />
```

