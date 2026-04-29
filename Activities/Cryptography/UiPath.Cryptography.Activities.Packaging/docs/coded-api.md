# Cryptography — Coded Workflow API

`UiPath.Cryptography.Activities`

Provides coded workflow operations for symmetric encryption/decryption of text and files, keyed hashing, and PGP encryption, signing, verification, and key generation.

**Service accessor:** `cryptography` (type `ICryptographyService`)
**Required package:** `"UiPath.Cryptography.Activities": "*"` in project.json dependencies

## Auto-Imported Namespaces

These namespaces are automatically available in coded workflows when this package is installed:

```
System
System.IO
System.Text
UiPath.Cryptography
UiPath.Cryptography.Activities
UiPath.Cryptography.Activities.API
UiPath.Cryptography.Enums
```

## Service Overview

The `cryptography` service exposes all operations as **direct method calls** — there is no connection, handle, or scope to open. Call methods on the service accessor directly:

```csharp
var ciphertext = cryptography.EncryptText("secret", EncryptionAlgorithm.AESGCM, "mykey", Encoding.UTF8);
```

The API is organized into three families:

| Family | Methods | Input/Output |
|--------|---------|-------------|
| **Symmetric** | `EncryptText`, `DecryptText`, `EncryptFile`, `DecryptFile` | Text ↔ Base64 string; File ↔ File |
| **Keyed hash** | `KeyedHashText`, `KeyedHashFile` | Text or File → hex string |
| **PGP** | `PgpEncrypt`, `PgpDecrypt`, `PgpEncryptText`, `PgpDecryptText`, `PgpSignFile`, `PgpClearSignFile`, `PgpVerify`, `PgpVerifyClear`, `PgpGenerateKeyPair` | Bytes ↔ Bytes; String ↔ String |

### Key material overloads

Every symmetric and keyed-hash method has three overloads that accept different key formats. Choose the one that matches how your key is stored:

| Overload | Parameter type | When to use |
|----------|---------------|-------------|
| String key | `string key` | Simple passwords or keys already held as strings. Note: string values are immutable and may linger on the heap until GC. |
| SecureString key | `SecureString key` | Keys sourced from user input or secret stores that surface `SecureString`. |
| Raw bytes | `byte[] keyBytes` | Keys already loaded as raw bytes (most efficient; avoids string encoding round-trip). |

### Symmetric encryption — IV / salt strategy

All symmetric encrypt methods are **non-deterministic**: a fresh random 8-byte salt (PBKDF2) and IV/nonce are generated on every call and prepended to the ciphertext. Encrypting the same plaintext twice always produces different ciphertext. The matching Decrypt method reconstructs the salt and IV from the same prefix automatically — the output of Encrypt can always be fed directly to Decrypt without supplying IV separately.

- **CBC-family** (`TripleDES`): PKCS7 padding, CBC mode, random IV.
- **AES-GCM** (`AESGCM`): random 96-bit nonce, 128-bit authentication tag — authenticated encryption (AEAD). **Recommended algorithm for new workflows.**

---

## Symmetric Text Encryption

### `string EncryptText(...)`

Encrypts a string and returns the ciphertext as a Base64-encoded string.

| Overload | Signature |
|----------|-----------|
| String key | `string EncryptText(string input, EncryptionAlgorithm algorithm, string key, Encoding encoding)` |
| SecureString key | `string EncryptText(string input, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding)` |
| Raw bytes | `string EncryptText(string input, EncryptionAlgorithm algorithm, byte[] keyBytes, Encoding encoding)` |

**Parameters:**
- `input` (`string`) — Plaintext to encrypt
- `algorithm` (`EncryptionAlgorithm`) — Encryption algorithm
- `key` / `keyBytes` — Key material in the chosen format
- `encoding` (`Encoding`) — Encoding used to convert the input string and string key to bytes (e.g., `Encoding.UTF8`)

**Returns:** `string` — Base64-encoded ciphertext (salt + IV/nonce prepended).

---

### `string DecryptText(...)`

Decrypts a Base64-encoded ciphertext produced by `EncryptText` and returns the original plaintext.

| Overload | Signature |
|----------|-----------|
| String key | `string DecryptText(string input, EncryptionAlgorithm algorithm, string key, Encoding encoding)` |
| SecureString key | `string DecryptText(string input, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding)` |
| Raw bytes | `string DecryptText(string input, EncryptionAlgorithm algorithm, byte[] keyBytes, Encoding encoding)` |

**Parameters:**
- `input` (`string`) — Base64-encoded ciphertext produced by `EncryptText`
- `algorithm` (`EncryptionAlgorithm`) — Must match the algorithm used to encrypt
- `key` / `keyBytes` — Must match the key used to encrypt
- `encoding` (`Encoding`) — Must match the encoding used to encrypt

**Returns:** `string` — Original plaintext.

---

## Symmetric File Encryption

### `void EncryptFile(...)`

Reads a file, encrypts it, and writes the result to an output path.

| Overload | Key parameter |
|----------|--------------|
| String key | `string key, Encoding encoding` |
| SecureString key | `SecureString key, Encoding encoding` |
| Raw bytes | `byte[] keyBytes` (no `encoding` parameter) |

**Common parameters (all overloads):**
- `inputFilePath` (`string`) — Path to the source file
- `outputFilePath` (`string`) — Path where the encrypted file is written
- `algorithm` (`EncryptionAlgorithm`) — Encryption algorithm
- `overwrite` (`bool`) — When `true`, overwrites an existing output file

**Returns:** `void`

---

### `void DecryptFile(...)`

Reads an encrypted file produced by `EncryptFile`, decrypts it, and writes the plaintext to an output path.

| Overload | Key parameter |
|----------|--------------|
| String key | `string key, Encoding encoding` |
| SecureString key | `SecureString key, Encoding encoding` |
| Raw bytes | `byte[] keyBytes` (no `encoding` parameter) |

**Common parameters (all overloads):**
- `inputFilePath` (`string`) — Path to the encrypted file
- `outputFilePath` (`string`) — Path where the decrypted file is written
- `algorithm` (`EncryptionAlgorithm`) — Must match the algorithm used to encrypt
- `overwrite` (`bool`) — When `true`, overwrites an existing output file

**Returns:** `void`

---

## Keyed Hashing

Keyed hash methods compute an HMAC or plain hash and return the result as a lowercase hex string. The result is one-way — there is no corresponding "unhash" operation.

### `string KeyedHashText(...)`

Computes a keyed hash of a string.

| Overload | Signature |
|----------|-----------|
| String key | `string KeyedHashText(string input, KeyedHashAlgorithms algorithm, string key, Encoding encoding)` |
| SecureString key | `string KeyedHashText(string input, KeyedHashAlgorithms algorithm, SecureString key, Encoding encoding)` |
| Raw bytes | `string KeyedHashText(string input, KeyedHashAlgorithms algorithm, byte[] keyBytes, Encoding encoding)` |

**Parameters:**
- `input` (`string`) — Text to hash
- `algorithm` (`KeyedHashAlgorithms`) — Hash algorithm
- `key` / `keyBytes` — Key material (used as the HMAC key; ignored for non-keyed SHA variants)
- `encoding` (`Encoding`) — Encoding used to convert the input and string key to bytes

**Returns:** `string` — Lowercase hex-encoded hash digest.

---

### `string KeyedHashFile(...)`

Computes a keyed hash of a file.

| Overload | Signature |
|----------|-----------|
| String key | `string KeyedHashFile(string filePath, KeyedHashAlgorithms algorithm, string key, Encoding encoding)` |
| SecureString key | `string KeyedHashFile(string filePath, KeyedHashAlgorithms algorithm, SecureString key, Encoding encoding)` |
| Raw bytes | `string KeyedHashFile(string filePath, KeyedHashAlgorithms algorithm, byte[] keyBytes)` — **no `encoding` parameter** |

**Parameters:**
- `filePath` (`string`) — Path to the file to hash
- `algorithm` (`KeyedHashAlgorithms`) — Hash algorithm
- `key` / `keyBytes` — Key material
- `encoding` (`Encoding`) — Encoding for the string key (string and SecureString overloads only)

**Returns:** `string` — Lowercase hex-encoded hash digest.

---

## PGP Encryption

PGP methods operate on raw `byte[]` or `string`. Keys are supplied as open `Stream` objects pointing to the armored PGP key file.

### `byte[] PgpEncrypt(...)`

Encrypts bytes using the recipient's public key. Optionally signs the data with the sender's private key.

| Overload | Passphrase type |
|----------|----------------|
| String passphrase | `byte[] PgpEncrypt(byte[] inputBytes, Stream publicKeyStream, Stream privateKeyStream = null, string passphrase = null, bool sign = false)` |
| SecureString passphrase | `byte[] PgpEncrypt(byte[] inputBytes, Stream publicKeyStream, Stream privateKeyStream, SecureString passphrase, bool sign = false)` |

**Parameters:**
- `inputBytes` (`byte[]`) — Data to encrypt
- `publicKeyStream` (`Stream`) — Stream of the recipient's armored PGP public key
- `privateKeyStream` (`Stream`) — Stream of the sender's armored PGP private key. Required when `sign` is `true`.
- `passphrase` — Passphrase protecting the private key. Required when `sign` is `true`.
- `sign` (`bool`) — When `true`, signs the encrypted data with the private key (default: `false`)

**Returns:** `byte[]` — PGP-encrypted (and optionally signed) data.

---

### `byte[] PgpDecrypt(...)`

Decrypts PGP-encrypted bytes using the recipient's private key. Optionally verifies the sender's signature.

| Overload | Passphrase type |
|----------|----------------|
| String passphrase | `byte[] PgpDecrypt(byte[] inputBytes, Stream privateKeyStream, string passphrase, Stream publicKeyStream = null, bool verifySignature = false)` |
| SecureString passphrase | `byte[] PgpDecrypt(byte[] inputBytes, Stream privateKeyStream, SecureString passphrase, Stream publicKeyStream = null, bool verifySignature = false)` |

**Parameters:**
- `inputBytes` (`byte[]`) — PGP-encrypted data
- `privateKeyStream` (`Stream`) — Stream of the recipient's armored PGP private key
- `passphrase` — Passphrase protecting the private key
- `publicKeyStream` (`Stream`) — Stream of the sender's armored PGP public key. Required when `verifySignature` is `true`.
- `verifySignature` (`bool`) — When `true`, verifies the embedded signature against the public key (default: `false`)

**Returns:** `byte[]` — Decrypted plaintext bytes.

---

### `string PgpEncryptText(...)`

Encrypts a string using PGP and returns the result as an armored PGP string.

| Overload | Passphrase type |
|----------|----------------|
| String passphrase | `string PgpEncryptText(string input, Stream publicKeyStream, Stream privateKeyStream = null, string passphrase = null, bool sign = false)` |
| SecureString passphrase | `string PgpEncryptText(string input, Stream publicKeyStream, Stream privateKeyStream, SecureString passphrase, bool sign = false)` |

Parameters and behavior are identical to `PgpEncrypt` but accept and return `string` instead of `byte[]`.

**Returns:** `string` — PGP armored ciphertext.

---

### `string PgpDecryptText(...)`

Decrypts a PGP-encrypted armored string.

| Overload | Passphrase type |
|----------|----------------|
| String passphrase | `string PgpDecryptText(string input, Stream privateKeyStream, string passphrase, Stream publicKeyStream = null, bool verifySignature = false)` |
| SecureString passphrase | `string PgpDecryptText(string input, Stream privateKeyStream, SecureString passphrase, Stream publicKeyStream = null, bool verifySignature = false)` |

Parameters and behavior are identical to `PgpDecrypt` but accept and return `string`.

**Returns:** `string` — Decrypted plaintext.

---

## PGP Signing

### `byte[] PgpSignFile(...)`

Signs bytes with a PGP private key and returns a binary PGP signature packet embedding the original data.

| Overload | Passphrase type |
|----------|----------------|
| String passphrase | `byte[] PgpSignFile(byte[] inputBytes, Stream privateKeyStream, string passphrase)` |
| SecureString passphrase | `byte[] PgpSignFile(byte[] inputBytes, Stream privateKeyStream, SecureString passphrase)` |

**Parameters:**
- `inputBytes` (`byte[]`) — Data to sign
- `privateKeyStream` (`Stream`) — Stream of the signer's armored PGP private key
- `passphrase` — Passphrase protecting the private key

**Returns:** `byte[]` — Signed PGP data. Verify with `PgpVerify`.

---

### `byte[] PgpClearSignFile(...)`

Creates a PGP clear-text signature: the original data remains readable in plain text with the signature appended.

| Overload | Passphrase type |
|----------|----------------|
| String passphrase | `byte[] PgpClearSignFile(byte[] inputBytes, Stream privateKeyStream, string passphrase)` |
| SecureString passphrase | `byte[] PgpClearSignFile(byte[] inputBytes, Stream privateKeyStream, SecureString passphrase)` |

**Parameters:** Same as `PgpSignFile`.

**Returns:** `byte[]` — Clear-signed PGP document. Verify with `PgpVerifyClear`.

---

## PGP Verification

### `bool PgpVerify(byte[] inputBytes, Stream publicKeyStream)`

Verifies a PGP binary signature (produced by `PgpSignFile` or by `PgpEncrypt` with `sign: true`) against a public key.

**Parameters:**
- `inputBytes` (`byte[]`) — Signed PGP data
- `publicKeyStream` (`Stream`) — Stream of the signer's armored PGP public key

**Returns:** `bool` — `true` if the signature is valid; `false` otherwise.

---

### `bool PgpVerifyClear(byte[] inputBytes, Stream publicKeyStream)`

Verifies a PGP clear-text signature (produced by `PgpClearSignFile`) against a public key.

**Parameters:**
- `inputBytes` (`byte[]`) — Clear-signed PGP document
- `publicKeyStream` (`Stream`) — Stream of the signer's armored PGP public key

**Returns:** `bool` — `true` if the signature is valid; `false` otherwise.

---

## PGP Key Generation

### `void PgpGenerateKeyPair(string publicKeyPath, string privateKeyPath, string username, string password)`

Generates a new PGP key pair and writes the armored public and private keys to the specified file paths.

**Parameters:**
- `publicKeyPath` (`string`) — File path where the armored public key is written
- `privateKeyPath` (`string`) — File path where the armored private key is written
- `username` (`string`) — Identity string embedded in the key (e.g., `"Alice <alice@example.com>"`)
- `password` (`string`) — Passphrase protecting the private key

**Returns:** `void`

---

## Enum Reference

### `EncryptionAlgorithm`

Used by `EncryptText`, `DecryptText`, `EncryptFile`, `DecryptFile`.

| Value | Notes |
|-------|-------|
| `AESGCM` | AES-GCM with 96-bit nonce and 128-bit auth tag. AEAD — **recommended for new workflows.** |
| `TripleDES` | 3DES in CBC mode with PKCS7 padding. Supported but weaker than AES-GCM. |
| `AES` | AES in CBC mode. **`[Obsolete]` — no longer considered safe; prefer `AESGCM`.** |
| `DES` | DES in CBC mode. **`[Obsolete]` — no longer safe.** |
| `RC2` | RC2 in CBC mode. **`[Obsolete]` — no longer safe.** |
| `Rijndael` | Rijndael in CBC mode. **`[Obsolete]` — no longer safe; prefer `AESGCM`.** |
| `PGP` | Reserved for PGP. Use the dedicated `PgpEncrypt`/`PgpDecrypt` methods instead. |

### `KeyedHashAlgorithms`

Used by `KeyedHashText`, `KeyedHashFile`.

| Value | Type | Notes |
|-------|------|-------|
| `HMACSHA256` | Keyed HMAC | Recommended for MAC/integrity verification. |
| `HMACSHA384` | Keyed HMAC | |
| `HMACSHA512` | Keyed HMAC | |
| `HMACSHA1` | Keyed HMAC | Weaker; prefer SHA256 or higher for new workflows. |
| `HMACMD5` | Keyed HMAC | Weak; avoid for security-sensitive use cases. |
| `SHA256` | Unkeyed hash | Produces the same result for any key value. |
| `SHA384` | Unkeyed hash | |
| `SHA512` | Unkeyed hash | |
| `SHA1` | Unkeyed hash | Weak; avoid for security-sensitive use cases. |

---

## Common Patterns

### Encrypt and decrypt a string with AES-GCM

```csharp
[Workflow]
public void Execute()
{
    const string key = "MySecretKey123!";

    var ciphertext = cryptography.EncryptText(
        "Sensitive data",
        EncryptionAlgorithm.AESGCM,
        key,
        Encoding.UTF8);

    Log($"Encrypted: {ciphertext}");

    var plaintext = cryptography.DecryptText(
        ciphertext,
        EncryptionAlgorithm.AESGCM,
        key,
        Encoding.UTF8);

    Log($"Decrypted: {plaintext}");
}
```

### Encrypt a file with raw key bytes

```csharp
[Workflow]
public void Execute()
{
    // 32 bytes → AES-256
    byte[] keyBytes = Convert.FromBase64String("your-base64-encoded-32-byte-key==");

    cryptography.EncryptFile(
        inputFilePath:  @"C:\Documents\report.pdf",
        outputFilePath: @"C:\Documents\report.pdf.enc",
        algorithm:      EncryptionAlgorithm.AESGCM,
        keyBytes:       keyBytes,
        overwrite:      true);

    cryptography.DecryptFile(
        inputFilePath:  @"C:\Documents\report.pdf.enc",
        outputFilePath: @"C:\Documents\report_decrypted.pdf",
        algorithm:      EncryptionAlgorithm.AESGCM,
        keyBytes:       keyBytes,
        overwrite:      true);
}
```

### Compute an HMAC-SHA256 for data integrity verification

```csharp
[Workflow]
public void Execute()
{
    byte[] hmacKey = Convert.FromBase64String("your-base64-hmac-key==");

    var digest = cryptography.KeyedHashText(
        "payload to verify",
        KeyedHashAlgorithms.HMACSHA256,
        hmacKey,
        Encoding.UTF8);

    Log($"HMAC-SHA256: {digest}");
}
```

### PGP encrypt a message for a recipient

```csharp
[Workflow]
public void Execute()
{
    byte[] inputBytes = Encoding.UTF8.GetBytes("Confidential message");

    using var publicKeyStream = File.OpenRead(@"C:\Keys\recipient_public.asc");

    byte[] encrypted = cryptography.PgpEncrypt(inputBytes, publicKeyStream);
    File.WriteAllBytes(@"C:\Output\message.pgp", encrypted);
}
```

### PGP encrypt and sign, then decrypt and verify

```csharp
[Workflow]
public void Execute()
{
    byte[] inputBytes = Encoding.UTF8.GetBytes("Signed and encrypted message");

    using var recipientPublicKey = File.OpenRead(@"C:\Keys\recipient_public.asc");
    using var senderPrivateKey   = File.OpenRead(@"C:\Keys\sender_private.asc");

    // Encrypt and sign
    byte[] encrypted = cryptography.PgpEncrypt(
        inputBytes,
        publicKeyStream:  recipientPublicKey,
        privateKeyStream: senderPrivateKey,
        passphrase:       "senderPassphrase",
        sign:             true);

    // Decrypt and verify signature
    using var recipientPrivateKey = File.OpenRead(@"C:\Keys\recipient_private.asc");
    using var senderPublicKey     = File.OpenRead(@"C:\Keys\sender_public.asc");

    byte[] decrypted = cryptography.PgpDecrypt(
        encrypted,
        privateKeyStream:  recipientPrivateKey,
        passphrase:        "recipientPassphrase",
        publicKeyStream:   senderPublicKey,
        verifySignature:   true);

    Log(Encoding.UTF8.GetString(decrypted));
}
```

### Generate a new PGP key pair

```csharp
[Workflow]
public void Execute()
{
    cryptography.PgpGenerateKeyPair(
        publicKeyPath:  @"C:\Keys\my_public.asc",
        privateKeyPath: @"C:\Keys\my_private.asc",
        username:       "Alice <alice@example.com>",
        password:       "StrongPassphrase!");

    Log("Key pair generated.");
}
```
