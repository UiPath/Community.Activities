# PGP Verify

`UiPath.Cryptography.Activities.PgpVerify`

Verifies a PGP signature, a clearsignature, or the structural validity of a public key file. The mode selects which check is performed and which inputs are required.

**Package:** `UiPath.Cryptography.Activities`
**Category:** Cryptography

## Properties

### Input

| Name | Display Name | Kind | Type | Required | Default | Description |
|------|-------------|------|------|----------|---------|-------------|
| `Mode` | Mode | Property | `PgpVerifyMode` |  | `Signature` | Which check to perform. See the Enum Reference below. |
| `InputFilePath` | Signed file path | InArgument | `string` | Conditional |  | The path to the signed file to verify. Required for `Signature` and `ClearSignature` modes; ignored in `PublicKey` mode. |
| `PublicKeyFilePath` | Public key file path | InArgument | `string` | Conditional |  | The path to the signer's PGP public key file. In `PublicKey` mode this is the key being validated. Paired with a hidden `IResource` alternative selectable via a designer menu action. |

### Configuration

| Name | Display Name | Type | Default | Description |
|------|-------------|------|---------|-------------|
| `ContinueOnError` | Continue on error | `InArgument<bool>` |  | Specifies if the automation should continue when the activity throws an error. |

### Output

| Name | Display Name | Kind | Type | Description |
|------|-------------|------|------|-------------|
| `Result` | Result | OutArgument | `bool` | `True` if verification succeeded, `False` otherwise. The activity does not throw on a verification mismatch — check `Result`. |

## Valid Configurations

`Mode = Signature` (binary-signed file, produced by `PgpSignFile`):
- Provide `InputFilePath` and `PublicKeyFilePath` (signer's public key).

`Mode = ClearSignature` (clear-text signed file, produced by `PgpClearSignFile`):
- Provide `InputFilePath` and `PublicKeyFilePath`.

`Mode = PublicKey` (validate that a file is a well-formed public key):
- Provide `PublicKeyFilePath` only. `InputFilePath` is ignored.

### Enum Reference

**`PgpVerifyMode`**: `Signature` *(default)*, `ClearSignature`, `PublicKey`.

## XAML Example

Verify a binary signature:

```xml
<ui:PgpVerify DisplayName="PGP Verify (Signature)"
              Mode="Signature"
              InputFilePath="C:\temp\report.txt.signed"
              PublicKeyFilePath="C:\keys\signer_public.asc"
              Result="[verified]" />
```

Verify a clearsigned file:

```xml
<ui:PgpVerify DisplayName="PGP Verify (ClearSignature)"
              Mode="ClearSignature"
              InputFilePath="C:\temp\notice.txt.asc"
              PublicKeyFilePath="C:\keys\signer_public.asc"
              Result="[verified]" />
```

Validate a public key file:

```xml
<ui:PgpVerify DisplayName="PGP Verify (PublicKey)"
              Mode="PublicKey"
              PublicKeyFilePath="C:\keys\candidate_public.asc"
              Result="[isWellFormed]" />
```

## Notes

- `Result = False` means verification failed (wrong key, tampered payload, malformed file). It does not throw.
- The `InputFilePath` widget hides in `PublicKey` mode (driven by the `needsInput` rule in the viewmodel).
