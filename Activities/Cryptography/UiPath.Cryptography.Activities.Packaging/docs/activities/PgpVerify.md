# PGP Verify

`UiPath.Cryptography.Activities.PgpVerify`

Verifies a PGP signature, clear signature, or validates a public key file.

**Package:** `UiPath.Cryptography.Activities`
**Category:** Cryptography

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `Mode` | Verification Type | Property | `PgpVerifyMode` | Yes |  |  | Select the type of verification. 'Signed File (Binary)' verifies a binary-signed file. 'Clear-Signed File (Text)' verifies a text file with an embedded signature. 'Validate Public Key' checks that a file contains a valid PGP public key. |
| `InputFilePath` | Signed File Path | InArgument | `string` |  |  |  | The path to the signed file to verify. Required for 'Signed File (Binary)' and 'Clear-Signed File (Text)' modes. |
| `PublicKeyFilePath` | Public Key File Path | InArgument | `string` | Yes |  |  | The path to the PGP public key file used for verification. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `ContinueOnError` | Continue On Error | `bool` |  | Specifies if the automation should continue even when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Result` | Result | Property | `object` | True if verification succeeded, False otherwise. |

## Valid Configurations

Set required input properties and choose optional configuration properties based on your chosen algorithm and key source. Some properties are conditionally visible in the designer depending on algorithm or mode.

## XAML Example

`xml
<ui:PgpVerify DisplayName="PGP Verify" />
`
