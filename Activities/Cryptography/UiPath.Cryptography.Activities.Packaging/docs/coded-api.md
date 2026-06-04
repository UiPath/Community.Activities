# Cryptography — Coded Workflow API

`UiPath.Cryptography.Activities`

Provides coded workflow operations for symmetric encryption/decryption, keyed hashing, and PGP encryption, decryption, signing, clearsigning, verification, and key generation.

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

### Bytes / Text / File matrix

Every logical operation exposes three input/output forms — pick the one that matches the data you already have:

| Form | Suffix | Input → Output | When to use |
|------|--------|---------------|-------------|
| **Bytes** | (base) | `byte[]` → `byte[]` | Binary or already-loaded data |
| **Text**  | `...Text` | `string` → `string` (Base64 / ASCII-armored) | Data arriving as text (HTTP, config, env) |
| **File**  | `...File` | file path → file path | Data lives on disk |

### Key material overloads (symmetric + keyed hash)

Every symmetric and keyed-hash method has three overloads that accept different key formats:

| Overload | Parameter | When to use |
|----------|-----------|-------------|
| String key | `string key, Encoding encoding` | Simple passwords or strings. Note: `string` is immutable and may linger on the heap. |
| SecureString key | `SecureString key, Encoding encoding` | Keys sourced from user input or secret stores. Material is zeroed after use. |
| Raw bytes | `byte[] keyBytes` | Keys already loaded as bytes — no encoding parameter needed. |

### PGP key material

PGP methods accept public and private keys as `byte[]`. The underlying parser auto-detects both ASCII-armored (`-----BEGIN PGP …-----`) and binary OpenPGP encodings. For armored text already held in a string, convert with `Encoding.UTF8.GetBytes(armored)` at the call site.

### Symmetric encryption — IV / salt strategy

All symmetric encrypt methods are **non-deterministic**: a fresh random 8-byte salt (PBKDF2) and IV/nonce are generated on every call and prepended to the ciphertext. Encrypting the same plaintext twice always produces different ciphertext. The matching decrypt method reconstructs the salt and IV from the same prefix automatically.

- **CBC-family** (`AES`, `Rijndael`, `DES`, `TripleDES`, `RC2`): PKCS7 padding, CBC mode, random IV.
- **AES-GCM** (`AESGCM`) — AEAD with random 96-bit nonce and 128-bit auth tag. **Recommended for new workflows.**
- **ChaCha20-Poly1305** (`ChaCha20Poly1305`) — AEAD alternative to AES-GCM.

### PGP passphrase limitation

PGP overloads that accept `SecureString passphrase` must materialise the passphrase to a managed `string` because the underlying BouncyCastle library requires a plain string and offers no `byte[]`-based passphrase API. The managed string cannot be zeroed afterward. For maximum security with PGP, prefer key rings that do not require a passphrase, or accept that the passphrase briefly exists as a managed string.

---

## Symmetric Encryption

### `byte[] EncryptBytes(...)`

Encrypts arbitrary bytes and returns the ciphertext as `byte[]` (salt + IV/nonce prepended).

| Overload | Signature |
|----------|-----------|
| String key | `byte[] EncryptBytes(byte[] inputBytes, EncryptionAlgorithm algorithm, string key, Encoding encoding)` |
| SecureString key | `byte[] EncryptBytes(byte[] inputBytes, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding)` |
| Raw bytes | `byte[] EncryptBytes(byte[] inputBytes, EncryptionAlgorithm algorithm, byte[] keyBytes)` |

**Returns:** `byte[]` — ciphertext with salt and IV/nonce prepended.

---

### `string EncryptText(...)`

Encrypts a string and returns the ciphertext as a Base64-encoded string.

| Overload | Signature |
|----------|-----------|
| String key | `string EncryptText(string input, EncryptionAlgorithm algorithm, string key, Encoding encoding)` |
| SecureString key | `string EncryptText(string input, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding)` |
| Raw bytes | `string EncryptText(string input, EncryptionAlgorithm algorithm, byte[] keyBytes, Encoding encoding)` |

**Returns:** `string` — Base64-encoded ciphertext.

---

### `void EncryptFile(...)`

Reads a file, encrypts it, and writes the result to an output path.

| Overload | Signature |
|----------|-----------|
| String key | `void EncryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, string key, Encoding encoding, bool overwrite = false)` |
| SecureString key | `void EncryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding, bool overwrite = false)` |
| Raw bytes | `void EncryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, byte[] keyBytes, bool overwrite = false)` |

**Returns:** `void` — output file is written to `outputFilePath`.

---

## Symmetric Decryption

### `byte[] DecryptBytes(...)`

Decrypts ciphertext produced by `EncryptBytes` and returns the original bytes.

| Overload | Signature |
|----------|-----------|
| String key | `byte[] DecryptBytes(byte[] inputBytes, EncryptionAlgorithm algorithm, string key, Encoding encoding)` |
| SecureString key | `byte[] DecryptBytes(byte[] inputBytes, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding)` |
| Raw bytes | `byte[] DecryptBytes(byte[] inputBytes, EncryptionAlgorithm algorithm, byte[] keyBytes)` |

**Returns:** `byte[]` — plaintext bytes.

---

### `string DecryptText(...)`

Decrypts a Base64-encoded ciphertext produced by `EncryptText`.

| Overload | Signature |
|----------|-----------|
| String key | `string DecryptText(string input, EncryptionAlgorithm algorithm, string key, Encoding encoding)` |
| SecureString key | `string DecryptText(string input, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding)` |
| Raw bytes | `string DecryptText(string input, EncryptionAlgorithm algorithm, byte[] keyBytes, Encoding encoding)` |

**Returns:** `string` — original plaintext.

---

### `void DecryptFile(...)`

Reads an encrypted file produced by `EncryptFile` and writes the plaintext to an output path.

| Overload | Signature |
|----------|-----------|
| String key | `void DecryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, string key, Encoding encoding, bool overwrite = false)` |
| SecureString key | `void DecryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding, bool overwrite = false)` |
| Raw bytes | `void DecryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, byte[] keyBytes, bool overwrite = false)` |

**Returns:** `void` — output file is written to `outputFilePath`.

---

## Keyed Hashing

Keyed hash methods compute an HMAC (or plain hash for non-HMAC algorithms) and return the result as a lowercase hex string. One-way — no corresponding "unhash" operation.

### `string KeyedHashBytes(...)`

| Overload | Signature |
|----------|-----------|
| String key | `string KeyedHashBytes(byte[] inputBytes, KeyedHashAlgorithms algorithm, string key, Encoding encoding)` |
| SecureString key | `string KeyedHashBytes(byte[] inputBytes, KeyedHashAlgorithms algorithm, SecureString key, Encoding encoding)` |
| Raw bytes | `string KeyedHashBytes(byte[] inputBytes, KeyedHashAlgorithms algorithm, byte[] keyBytes)` |

**Returns:** `string` — lowercase hex-encoded hash digest.

---

### `string KeyedHashText(...)`

| Overload | Signature |
|----------|-----------|
| String key | `string KeyedHashText(string input, KeyedHashAlgorithms algorithm, string key, Encoding encoding)` |
| SecureString key | `string KeyedHashText(string input, KeyedHashAlgorithms algorithm, SecureString key, Encoding encoding)` |
| Raw bytes | `string KeyedHashText(string input, KeyedHashAlgorithms algorithm, byte[] keyBytes, Encoding encoding)` |

**Returns:** `string` — lowercase hex-encoded hash digest.

---

### `string KeyedHashFile(...)`

| Overload | Signature |
|----------|-----------|
| String key | `string KeyedHashFile(string filePath, KeyedHashAlgorithms algorithm, string key, Encoding encoding)` |
| SecureString key | `string KeyedHashFile(string filePath, KeyedHashAlgorithms algorithm, SecureString key, Encoding encoding)` |
| Raw bytes | `string KeyedHashFile(string filePath, KeyedHashAlgorithms algorithm, byte[] keyBytes)` |

**Returns:** `string` — lowercase hex-encoded hash digest.

---

## PGP Encryption

PGP encrypt methods accept the recipient's public key as `byte[]` (armored or binary). When `sign: true`, the sender's private key and passphrase are also required so the encrypted payload is signed.

### `byte[] PgpEncryptBytes(...)`

| Overload | Signature |
|----------|-----------|
| String passphrase | `byte[] PgpEncryptBytes(byte[] inputBytes, byte[] publicKey, byte[] privateKey = null, string passphrase = null, bool sign = false)` |
| SecureString passphrase | `byte[] PgpEncryptBytes(byte[] inputBytes, byte[] publicKey, byte[] privateKey, SecureString passphrase, bool sign = false)` |

**Returns:** `byte[]` — PGP-encrypted (and optionally signed) payload.

---

### `string PgpEncryptText(...)`

| Overload | Signature |
|----------|-----------|
| String passphrase | `string PgpEncryptText(string input, byte[] publicKey, byte[] privateKey = null, string passphrase = null, bool sign = false)` |
| SecureString passphrase | `string PgpEncryptText(string input, byte[] publicKey, byte[] privateKey, SecureString passphrase, bool sign = false)` |

**Returns:** `string` — ASCII-armored PGP ciphertext.

---

### `void PgpEncryptFile(...)`

| Overload | Signature |
|----------|-----------|
| String passphrase | `void PgpEncryptFile(string inputFilePath, string outputFilePath, byte[] publicKey, byte[] privateKey = null, string passphrase = null, bool sign = false, bool overwrite = false)` |
| SecureString passphrase | `void PgpEncryptFile(string inputFilePath, string outputFilePath, byte[] publicKey, byte[] privateKey, SecureString passphrase, bool sign = false, bool overwrite = false)` |

**Returns:** `void` — output file is written to `outputFilePath`.

---

## PGP Decryption

PGP decrypt methods accept the recipient's private key + passphrase. When `verifySignature: true`, the sender's public key is also required to verify the embedded signature.

### `byte[] PgpDecryptBytes(...)`

| Overload | Signature |
|----------|-----------|
| String passphrase | `byte[] PgpDecryptBytes(byte[] inputBytes, byte[] privateKey, string passphrase, byte[] publicKey = null, bool verifySignature = false)` |
| SecureString passphrase | `byte[] PgpDecryptBytes(byte[] inputBytes, byte[] privateKey, SecureString passphrase, byte[] publicKey = null, bool verifySignature = false)` |

**Returns:** `byte[]` — decrypted plaintext bytes.

---

### `string PgpDecryptText(...)`

| Overload | Signature |
|----------|-----------|
| String passphrase | `string PgpDecryptText(string input, byte[] privateKey, string passphrase, byte[] publicKey = null, bool verifySignature = false)` |
| SecureString passphrase | `string PgpDecryptText(string input, byte[] privateKey, SecureString passphrase, byte[] publicKey = null, bool verifySignature = false)` |

**Returns:** `string` — decrypted plaintext.

---

### `void PgpDecryptFile(...)`

| Overload | Signature |
|----------|-----------|
| String passphrase | `void PgpDecryptFile(string inputFilePath, string outputFilePath, byte[] privateKey, string passphrase, byte[] publicKey = null, bool verifySignature = false, bool overwrite = false)` |
| SecureString passphrase | `void PgpDecryptFile(string inputFilePath, string outputFilePath, byte[] privateKey, SecureString passphrase, byte[] publicKey = null, bool verifySignature = false, bool overwrite = false)` |

**Returns:** `void` — output file is written to `outputFilePath`.

---

## PGP Signing (binary signature)

Produces a detached or embedded binary signature. Verify with `PgpVerify*`.

### `byte[] PgpSignBytes(...)`

| Overload | Signature |
|----------|-----------|
| String passphrase | `byte[] PgpSignBytes(byte[] inputBytes, byte[] privateKey, string passphrase)` |
| SecureString passphrase | `byte[] PgpSignBytes(byte[] inputBytes, byte[] privateKey, SecureString passphrase)` |

**Returns:** `byte[]` — signed payload.

---

### `string PgpSignText(...)`

| Overload | Signature |
|----------|-----------|
| String passphrase | `string PgpSignText(string input, byte[] privateKey, string passphrase)` |
| SecureString passphrase | `string PgpSignText(string input, byte[] privateKey, SecureString passphrase)` |

**Returns:** `string` — ASCII-armored signed payload.

---

### `void PgpSignFile(...)`

| Overload | Signature |
|----------|-----------|
| String passphrase | `void PgpSignFile(string inputFilePath, string outputFilePath, byte[] privateKey, string passphrase, bool overwrite = false)` |
| SecureString passphrase | `void PgpSignFile(string inputFilePath, string outputFilePath, byte[] privateKey, SecureString passphrase, bool overwrite = false)` |

**Returns:** `void` — signed output file is written to `outputFilePath`.

---

## PGP Clearsigning

Clearsignatures keep the original content human-readable with the signature appended. Verify with `PgpVerifyClear*`.

### `byte[] PgpClearsignBytes(...)`

| Overload | Signature |
|----------|-----------|
| String passphrase | `byte[] PgpClearsignBytes(byte[] inputBytes, byte[] privateKey, string passphrase)` |
| SecureString passphrase | `byte[] PgpClearsignBytes(byte[] inputBytes, byte[] privateKey, SecureString passphrase)` |

**Returns:** `byte[]` — clearsigned payload.

---

### `string PgpClearsignText(...)`

| Overload | Signature |
|----------|-----------|
| String passphrase | `string PgpClearsignText(string input, byte[] privateKey, string passphrase)` |
| SecureString passphrase | `string PgpClearsignText(string input, byte[] privateKey, SecureString passphrase)` |

**Returns:** `string` — ASCII-armored clearsigned text.

---

### `void PgpClearsignFile(...)`

| Overload | Signature |
|----------|-----------|
| String passphrase | `void PgpClearsignFile(string inputFilePath, string outputFilePath, byte[] privateKey, string passphrase, bool overwrite = false)` |
| SecureString passphrase | `void PgpClearsignFile(string inputFilePath, string outputFilePath, byte[] privateKey, SecureString passphrase, bool overwrite = false)` |

**Returns:** `void` — clearsigned output file is written to `outputFilePath`.

---

## PGP Verification

### Binary signatures

Verify payloads produced by `PgpSign*` (or `PgpEncrypt*` with `sign: true`).

| Method | Signature |
|--------|-----------|
| Bytes | `bool PgpVerifyBytes(byte[] inputBytes, byte[] publicKey)` |
| Text  | `bool PgpVerifyText(string input, byte[] publicKey)` |
| File  | `bool PgpVerifyFile(string inputFilePath, byte[] publicKey)` |

**Returns:** `bool` — `true` when the signature is valid; `false` otherwise.

---

### Clearsignatures

Verify payloads produced by `PgpClearsign*`.

| Method | Signature |
|--------|-----------|
| Bytes | `bool PgpVerifyClearBytes(byte[] inputBytes, byte[] publicKey)` |
| Text  | `bool PgpVerifyClearText(string input, byte[] publicKey)` |
| File  | `bool PgpVerifyClearFile(string inputFilePath, byte[] publicKey)` |

**Returns:** `bool` — `true` when the clearsignature is valid; `false` otherwise.

---

### Public-key well-formedness

Confirms that the supplied material is a well-formed OpenPGP public key. Mirrors the `PgpVerify` activity's `Mode = PublicKey`.

| Method | Signature |
|--------|-----------|
| Bytes | `bool PgpVerifyPublicKeyBytes(byte[] publicKey)` |
| Text  | `bool PgpVerifyPublicKeyText(string publicKey)` |
| File  | `bool PgpVerifyPublicKeyFile(string publicKeyFilePath)` |

**Returns:** `bool` — `true` when the input parses as a valid OpenPGP public key.

---

## PGP Key-Pair Generation

### `void PgpGenerateKeys(string publicKeyPath, string privateKeyPath, string userId, string passphrase, RsaKeySize keySize = RsaKeySize.Rsa4096)`

Generates an OpenPGP RSA key pair and writes both keys to the specified paths.

**Parameters:**
- `publicKeyPath` (`string`) — Path where the ASCII-armored public key is written.
- `privateKeyPath` (`string`) — Path where the ASCII-armored private key is written.
- `userId` (`string`) — OpenPGP User ID; conventionally an RFC 2822 mailbox such as `Alice Doe <alice@example.com>`.
- `passphrase` (`string`) — Passphrase that protects the generated private key.
- `keySize` (`RsaKeySize`) — RSA key size. Default `Rsa4096`. `Rsa3072` and `Rsa2048` are accepted for interop with legacy systems.

**Returns:** `void`

---

## Enum Reference

### `EncryptionAlgorithm`

Used by `EncryptBytes`/`EncryptText`/`EncryptFile` and `DecryptBytes`/`DecryptText`/`DecryptFile`.

| Value | Notes |
|-------|-------|
| `AESGCM` | AES-GCM with 96-bit nonce and 128-bit auth tag. AEAD — **recommended for new workflows.** |
| `ChaCha20Poly1305` | ChaCha20-Poly1305 AEAD. Non-FIPS. Alternative to AES-GCM. |
| `AES` | AES in CBC mode. |
| `Rijndael` | Rijndael in CBC mode. |
| `DES` | DES in CBC mode. **`[Obsolete]` — weak; avoid.** |
| `TripleDES` | 3DES in CBC mode. **`[Obsolete]` — weak; avoid.** |
| `RC2` | RC2 in CBC mode. **`[Obsolete]` — weak; avoid.** |
| `PGP` | Reserved. Use the dedicated `PgpEncrypt*`/`PgpDecrypt*` methods instead. |

### `KeyedHashAlgorithms`

Used by `KeyedHashBytes`/`KeyedHashText`/`KeyedHashFile`.

| Value | Type | Notes |
|-------|------|-------|
| `HMACSHA256` | Keyed HMAC | Recommended for MAC/integrity verification. |
| `HMACSHA384` | Keyed HMAC | |
| `HMACSHA512` | Keyed HMAC | |
| `HMACSHA1` | Keyed HMAC | Weaker; prefer SHA256 or higher. |
| `HMACMD5` | Keyed HMAC | Weak; avoid for security-sensitive use cases. |
| `SHA256` | Unkeyed hash | Key is ignored — equivalent to a plain SHA hash. |
| `SHA384` | Unkeyed hash | Key is ignored. |
| `SHA512` | Unkeyed hash | Key is ignored. |
| `SHA1` | Unkeyed hash | Key is ignored. Weak; avoid for security-sensitive use cases. |

### `RsaKeySize`

Used by `PgpGenerateKeys`.

| Value | Bits |
|-------|------|
| `Rsa2048` | 2048 |
| `Rsa3072` | 3072 |
| `Rsa4096` | 4096 (default) |

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
    byte[] publicKey  = File.ReadAllBytes(@"C:\Keys\recipient_public.asc");

    byte[] encrypted = cryptography.PgpEncryptBytes(inputBytes, publicKey);
    File.WriteAllBytes(@"C:\Output\message.pgp", encrypted);
}
```

### PGP encrypt and sign, then decrypt and verify

```csharp
[Workflow]
public void Execute()
{
    byte[] inputBytes        = Encoding.UTF8.GetBytes("Signed and encrypted message");
    byte[] recipientPublic   = File.ReadAllBytes(@"C:\Keys\recipient_public.asc");
    byte[] senderPrivate     = File.ReadAllBytes(@"C:\Keys\sender_private.asc");

    // Encrypt and sign
    byte[] encrypted = cryptography.PgpEncryptBytes(
        inputBytes,
        publicKey:  recipientPublic,
        privateKey: senderPrivate,
        passphrase: "senderPassphrase",
        sign:       true);

    // Decrypt and verify signature
    byte[] recipientPrivate = File.ReadAllBytes(@"C:\Keys\recipient_private.asc");
    byte[] senderPublic     = File.ReadAllBytes(@"C:\Keys\sender_public.asc");

    byte[] decrypted = cryptography.PgpDecryptBytes(
        encrypted,
        privateKey:      recipientPrivate,
        passphrase:      "recipientPassphrase",
        publicKey:       senderPublic,
        verifySignature: true);

    Log(Encoding.UTF8.GetString(decrypted));
}
```

### Generate a new PGP key pair

```csharp
[Workflow]
public void Execute()
{
    cryptography.PgpGenerateKeys(
        publicKeyPath:  @"C:\Keys\my_public.asc",
        privateKeyPath: @"C:\Keys\my_private.asc",
        userId:         "Alice <alice@example.com>",
        passphrase:     "StrongPassphrase!",
        keySize:        RsaKeySize.Rsa4096);

    Log("Key pair generated.");
}
```

### Validate an inbound public key before storing it

```csharp
[Workflow]
public void Execute()
{
    // armored public key arriving as text from an HTTP response or config
    string armoredPublicKey = LoadFromInbox();

    if (!cryptography.PgpVerifyPublicKeyText(armoredPublicKey))
    {
        throw new InvalidOperationException("Supplied content is not a valid OpenPGP public key.");
    }

    File.WriteAllText(@"C:\Keys\trusted_public.asc", armoredPublicKey);
}
```
