# Hash File

`UiPath.Cryptography.Activities.KeyedHashFile`

Hashes a file with a key using a specified algorithm and returns the hexadecimal string representation of the resulting hash. It supports hashing algorithms with a key and without a key.

**Package:** `UiPath.Cryptography.Activities`
**Category:** Cryptography

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `Algorithm` | Algorithm | Property | `KeyedHashAlgorithms` | Yes |  |  | A drop-down which enables you to select the keyed hashing algorithm you want to use. |
| `InputFile` | File | InArgument | `IResource` |  |  |  | The file to be hashed |
| `FilePath` | File path | InArgument | `string` |  |  |  | The path to the file you want to hash. |
| `Key` | Key | InArgument | `string` |  |  |  | The key that you want to use to hash the specified file. |
| `KeySecureString` | Key Secure String | InArgument | `SecureString` |  |  |  | The secure string used to hash the provided file. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `KeyEncodingString` | Key Encoding | `string` |  | The encoding used to interpret the key specified in the Key property. |
| `ContinueOnError` | Continue On Error | `bool` |  | Specifies if the automation should continue even when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Result` | Hash | OutArgument | `string` | The hashed file, stored in a String variable. |

## Valid Configurations

- File source: provide either `FilePath` or `InputFile`.
- HMAC algorithms: provide either `Key` or `KeySecureString`.
- Non-HMAC algorithms: `Key` and `KeySecureString` are ignored.

## XAML Example

```xml
<ui:KeyedHashFile DisplayName="Hash File"
				  Algorithm="HMACSHA256"
				  FilePath="C:\\temp\\input.txt"
				  Key="my-secret-key"
				  Result="[fileHash]" />
```

