# Hash Text

`UiPath.Cryptography.Activities.KeyedHashText`

Hashes a string with a key using a specified algorithm and returns the hexadecimal string representation of the resulting hash. It supports hashing algorithms with a key and without a key.

**Package:** `UiPath.Cryptography.Activities`
**Category:** Cryptography

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `Algorithm` | Algorithm | Property | `KeyedHashAlgorithms` | Yes |  |  | A drop-down which enables you to select the keyed hashing algorithm you want to use. |
| `Input` | Text | InArgument | `string` | Yes |  |  | The text that you want to hash. |
| `Key` | Key | InArgument | `string` |  |  |  | The key that you want to use to hash the specified text. |
| `KeySecureString` | Key Secure String | InArgument | `SecureString` |  |  |  | The secure string used to hash the input string. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `KeyEncodingString` | Key Encoding | `string` |  | The encoding used to interpret the key specified in the Key property. |
| `ContinueOnError` | Continue On Error | `bool` |  | Specifies if the automation should continue even when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Result` | Hash | OutArgument | `string` | The hashed text, stored in a String variable. |

## Valid Configurations

- HMAC algorithms: provide either `Key` or `KeySecureString`.
- Non-HMAC algorithms: `Key` and `KeySecureString` are ignored.
- `KeyEncodingString` is used when `Key` is provided as text.

## XAML Example

```xml
<ui:KeyedHashText DisplayName="Hash Text"
				  Algorithm="HMACSHA256"
				  Input="[inputText]"
				  Key="my-secret-key"
				  Result="[textHash]" />
```

