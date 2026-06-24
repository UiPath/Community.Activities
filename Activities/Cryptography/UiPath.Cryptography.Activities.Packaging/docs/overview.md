# UiPath Cryptography Activities

`UiPath.Cryptography.Activities`

## Activities

### Cross-platform

| Activity | Description |
|----------|-------------|
| [Decrypt File](activities/DecryptFile.md) | Decrypts a file based on a specified key encoding and algorithm |
| [Decrypt Text](activities/DecryptText.md) | Decrypts text based on a specified key encoding and algorithm |
| [Encrypt File](activities/EncryptFile.md) | Encrypts a file with a key based on a specified key encoding and algorithm |
| [Encrypt Text](activities/EncryptText.md) | Encrypts a string with a key based on a specified key encoding and algorithm |
| [Hash File](activities/KeyedHashFile.md) | Hashes a file with a key using a specified algorithm and returns the hexadecimal string representation of the resulting hash |
| [Hash Text](activities/KeyedHashText.md) | Hashes a string with a key using a specified algorithm and returns the hexadecimal string representation of the resulting hash |
| [PGP Generate Keys](activities/PgpGenerateKeys.md) | Generates a PGP public/private key pair and saves them to the specified file paths |
| [PGP Sign File](activities/PgpSignFile.md) | Creates a PGP binary signature of a file using a private key |
| [PGP ClearSign File](activities/PgpClearSignFile.md) | Creates a PGP clear-text signature of a file using a private key |
| [PGP Verify](activities/PgpVerify.md) | Verifies a PGP signature, clearsignature, or validates a public key file |

## Coded workflows

The same capabilities are available to coded (C#) workflows through the `cryptography` service — see the [Coded Workflow API](coded-api.md). The activities and the coded API share one cryptographic core, so they are at full parity on algorithms, wire formats, and PGP operations. The coded API additionally offers `byte[]` I/O, raw-byte and in-memory keys, and Text/Bytes variants of sign/clearsign/verify; the activities add UiPath `IResource` handle support and the `ContinueOnError` option. See [Relationship to the XAML activities](coded-api.md#relationship-to-the-xaml-activities) for the full comparison.
