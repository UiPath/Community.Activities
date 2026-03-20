# PGP Clear Sign File

`UiPath.Cryptography.Activities.PgpClearSignFile`

Creates a PGP clear-text signature of a file using a private key.

**Package:** `UiPath.Cryptography.Activities`
**Category:** Cryptography

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Placeholder | Description |
|------|-------------|------|------|----------|---------|-------------|-------------|
| `InputFilePath` | Input File Path | InArgument | `string` | Yes |  |  | The path to the file that you want to clear-sign. |
| `PrivateKeyFilePath` | Private Key File Path | InArgument | `string` | Yes |  |  | The path to the PGP private key file used for signing. |
| `Passphrase` | Passphrase | InArgument | `SecureString` | Yes |  |  | The passphrase for the PGP private key. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `OutputFilePath` | Output File Path | `string` |  | The path where the clear-signed file will be saved. |
| `Overwrite` | Overwrite | `bool` |  | If a file already exists at the output path, selecting this overwrites it. |
| `ContinueOnError` | Continue On Error | `bool` |  | Specifies if the automation should continue even when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `ClearSignedFile` | Clear-Signed File | OutArgument | `ILocalResource` | The clear-signed file as a file resource. |

## Valid Configurations

Set required input properties and choose optional configuration properties based on your chosen algorithm and key source. Some properties are conditionally visible in the designer depending on algorithm or mode.

## XAML Example

`xml
<ui:PgpClearSignFile DisplayName="PGP Clear Sign File" />
`
