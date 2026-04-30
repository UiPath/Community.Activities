# Encrypt File

`UiPath.Cryptography.Activities.EncryptFile`

Encrypts a file with a key based on a specified key encoding and algorithm.

**Package:** `UiPath.Cryptography.Activities`
**Category:** Cryptography

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `Algorithm` | Algorithm | Property | `EncryptionAlgorithm` | Yes |  |  | A drop-down which enables you to select the encryption algorithm you want to use. |
| `InputFilePath` | File path | InArgument | `string` |  |  |  | The path to the file that you want to encrypt. |
| `InputFile` | File | InArgument | `IResource` |  |  |  | The file to be encrypted |
| `Key` | Key | InArgument | `string` |  |  |  | The key that you want to use to encrypt the specified file. Provide either `Key` or `KeySecureString`. |
| `KeySecureString` | Key Secure String | InArgument | `SecureString` |  |  |  | The secure string used to encrypt the input file. Provide either `Key` or `KeySecureString`. |
| `PublicKeyFilePath` | Public Key File Path | InArgument | `string` |  |  |  | The path to the PGP public key file used for encryption. |
| `PrivateKeyFilePath` | Private Key File Path | InArgument | `string` |  |  |  | The path to the PGP private key file used for signing. |
| `Passphrase` | Passphrase | InArgument | `SecureString` |  |  |  | The passphrase for the PGP private key used for signing. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `OutputFilePath` | Output file name and location | `string` |  | The path where you want to save the encrypted file. |
| `KeyEncodingString` | Key Encoding | `string` |  | The encoding used to interpret the key specified in the Key property. |
| `Overwrite` | Overwrite | `bool` |  | If a file already exists at the path specified in the Output path field, selecting this check box overwrites it. If unchecked, a new file is created. |
| `ContinueOnError` | Continue On Error | `bool` |  | Specifies if the automation should continue even when the activity throws an error. |
| `SignData` | Sign Data | `bool` |  | When enabled, signs the encrypted data using the private key. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `EncryptedFile` | Encrypted File | OutArgument | `ILocalResource` | The encrypted file |

## Valid Configurations

- File source: provide either `InputFilePath` or `InputFile`.
- Symmetric encryption mode: set `Algorithm`, then provide either `Key` or `KeySecureString`.
- PGP encryption mode: provide `PublicKeyFilePath`; for signed encryption also provide `PrivateKeyFilePath`, `Passphrase`, and set `SignData` to `True`.
- Set `OutputFilePath` and `Overwrite` based on destination behavior.

## XAML Example

```xml
<ui:EncryptFile DisplayName="Encrypt File"
				Algorithm="AESGCM"
				InputFilePath="C:\\temp\\plain.txt"
				Key="my-secret-key"
				OutputFilePath="C:\\temp\\plain.encrypted"
				Overwrite="True"
				EncryptedFile="[encryptedFile]" />
```

