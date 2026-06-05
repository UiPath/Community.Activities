# Hash Text

`UiPath.Cryptography.Activities.KeyedHashText`

Hashes a text string using the specified algorithm and returns the hexadecimal hash string. Supports keyed HMAC algorithms (which require a key) and plain hash algorithms (which do not).

**Package:** `UiPath.Cryptography.Activities`
**Category:** Cryptography

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Description |
|------|-------------|------|------|----------|---------|-------------|
| `Algorithm` | Algorithm | Property | `KeyedHashAlgorithms` | Yes |  | The hash algorithm to use. See the Enum Reference below. |
| `Input` | Text | InArgument | `string` | Yes |  | The text that you want to hash. |
| `Key` | Key | InArgument | `string` | Conditional |  | The HMAC key. Required when `Algorithm` is an HMAC variant. Provide either `Key` or `KeySecureString`. |
| `KeySecureString` | Key secure string | InArgument | `SecureString` | Conditional |  | Secure-string variant of the HMAC key. Required when `Algorithm` is an HMAC variant. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `ContinueOnError` | Continue on error | `InArgument<bool>` |  | Specifies if the automation should continue when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Result` | Hash | OutArgument | `string` | The hash, as a lower-case hexadecimal string. |

## Valid Configurations

**Keyed (HMAC) mode** — `Algorithm` is one of `HMACMD5`, `HMACSHA1`, `HMACSHA256`, `HMACSHA384`, `HMACSHA512`:
- Provide `Input`, `Algorithm`, and exactly one of `Key` / `KeySecureString`.
- The text and the key are both interpreted as UTF-8.

**Plain hash mode** — `Algorithm` is one of `SHA1`, `SHA256`, `SHA384`, `SHA512`:
- Provide `Input` and `Algorithm`. `Key` / `KeySecureString` are not used.

### Enum Reference

**`KeyedHashAlgorithms`**: `HMACSHA256` *(default)*, `HMACSHA384`, `HMACSHA512`, `SHA256`, `SHA384`, `SHA512`, `HMACMD5` *(deprecated)*, `HMACSHA1` *(deprecated)*, `SHA1` *(deprecated)*.

## XAML Example

HMAC-SHA256 with a string key:

```xml
<ui:KeyedHashText DisplayName="Hash Text (HMAC-SHA256)"
                  Algorithm="HMACSHA256"
                  Input="hello"
                  Key="[hmacKey]"
                  Result="[hashHex]" />
```

Plain SHA-256 (no key):

```xml
<ui:KeyedHashText DisplayName="Hash Text (SHA-256)"
                  Algorithm="SHA256"
                  Input="hello"
                  Result="[hashHex]" />
```

## Notes

- `Key` ↔ `KeySecureString` are paired via a designer menu action: only one side is active at a time.
- The `Key` widget only appears when an `HMAC*` algorithm is selected (driven by the `isHmac` rule in the viewmodel).
