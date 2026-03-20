# Decrypt Text

`UiPath.Cryptography.Activities.DecryptText`

Decrypts text based on a specified key encoding and algorithm.

**Package:** `UiPath.Cryptography.Activities`
**Category:** Cryptography

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `Algorithm` | Algorithm | Property | `EncryptionAlgorithm` | Yes |  |  | A drop-down which enables you to select the decryption algorithm you want to use. |
| `Input` | Text | InArgument | `string` | Yes |  |  | The text that you want to decrypt. |
| `Key` | Key | InArgument | `string` | Yes |  |  | The key that you want to use to decrypt the specified file. |
| `KeySecureString` | Key Secure String | InArgument | `SecureString` | Yes |  |  | The secure string used to decrypt the input string. |
| `PrivateKeyFilePath` | Private Key File Path | InArgument | `string` |  |  |  | The path to the PGP private key file used for decryption. |
| `Passphrase` | Passphrase | InArgument | `SecureString` |  |  |  | The passphrase for the PGP private key. |
| `PublicKeyFilePath` | Public Key File Path | InArgument | `string` |  |  |  | The path to the PGP public key file used for signature verification. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `KeyEncodingString` | Key Encoding | `string` |  | The encoding used to interpret the key specified in the Key property. |
| `ContinueOnError` | Continue On Error | `bool` |  | Specifies if the automation should continue even if the activity throws an error. |
| `VerifySignature` | Verify Signature | `bool` |  | When enabled, verifies the PGP signature of the decrypted data using the public key. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Result` | Decrypted Text | Property | `object` | The decrypted text, stored in a String variable. |

## Valid Configurations

Set required input properties and choose optional configuration properties based on your chosen algorithm and key source. Some properties are conditionally visible in the designer depending on algorithm or mode.

## XAML Example

`xml
<ui:DecryptText DisplayName="Decrypt Text" />
`
