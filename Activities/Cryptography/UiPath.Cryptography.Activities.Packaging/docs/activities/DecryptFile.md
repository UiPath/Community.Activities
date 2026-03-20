# Decrypt File

`UiPath.Cryptography.Activities.DecryptFile`

Decrypts a file based on a specified key encoding and algorithm.

**Package:** `UiPath.Cryptography.Activities`
**Category:** Cryptography

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `Algorithm` | Algorithm | Property | `EncryptionAlgorithm` | Yes |  |  | A drop-down which enables you to select the decryption algorithm you want to use. |
| `InputFile` | File | InArgument | `IResource` |  |  |  | The file to be decrypted |
| `InputFilePath` | File path | InArgument | `string` |  |  |  | The path to the file that you want to decrypt. |
| `Key` | Key | InArgument | `string` |  |  |  | The key that you want to use to decrypt the specified file. |
| `KeySecureString` | Key Secure String | InArgument | `SecureString` |  |  |  | The secure string used to decrypt the input file. |
| `PrivateKeyFilePath` | Private Key File Path | InArgument | `string` |  |  |  | The path to the PGP private key file used for decryption. |
| `Passphrase` | Passphrase | InArgument | `SecureString` |  |  |  | The passphrase for the PGP private key. |
| `PublicKeyFilePath` | Public Key File Path | InArgument | `string` |  |  |  | The path to the PGP public key file used for signature verification. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `OutputFilePath` | Output file name and location | `string` |  | The path where you want to save the decrypted file. |
| `KeyEncodingString` | Key Encoding | `string` |  | The encoding used to interpret the key specified in the Key property. |
| `Overwrite` | Overwrite | `bool` |  | If a file already exists at the path specified in the Output path field, selecting this check box overwrites it. If unchecked, a new file is created. |
| `ContinueOnError` | Continue On Error | `bool` |  | Specifies if the automation should continue even when the activity throws an error. |
| `VerifySignature` | Verify Signature | `bool` |  | When enabled, verifies the PGP signature of the decrypted data using the public key. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `DecryptedFile` | Decrypted File | OutArgument | `ILocalResource` | The decrypted file |

## Valid Configurations

Set required input properties and choose optional configuration properties based on your chosen algorithm and key source. Some properties are conditionally visible in the designer depending on algorithm or mode.

## XAML Example

`xml
<ui:DecryptFile DisplayName="Decrypt File" />
`
