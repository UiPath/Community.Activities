# Cryptography — Coded Workflow API

`UiPath.Cryptography.Activities`

Provides coded workflow operations for symmetric encryption/decryption, keyed hashing, and PGP encryption, decryption, signing, clear-signing, verification, and key generation.

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
var key = CryptoKey.FromPassword("mykey", Encoding.UTF8);
var ciphertext = cryptography.EncryptText("secret", EncryptionAlgorithm.AESGCM, key);
```

### Bytes / Text / File matrix

Every logical operation exposes three input/output forms — pick the one that matches the data you already have:

| Form | Suffix | Input → Output | When to use |
|------|--------|---------------|-------------|
| **Bytes** | (base) | `byte[]` → `byte[]` | Binary or already-loaded data |
| **Text**  | `...Text` | `string` → `string` (Base64 / ASCII-armored) | Data arriving as text (HTTP, config, env) |
| **File**  | `...File` | file path → file path | Data lives on disk |

### Key material — `CryptoKey`

Every symmetric and keyed-hash method takes a `CryptoKey`. Construct one via a factory method depending on what you have:

| Factory | Purpose |
|---------|---------|
| `CryptoKey.FromPassword(string password, Encoding encoding)` | Password / passphrase as a `string`. PBKDF2 derives the cipher key. |
| `CryptoKey.FromPassword(SecureString password, Encoding encoding)` | Same as above, sourced from a secret store or user input. |
| `CryptoKey.FromRawBytes(byte[] keyBytes)` | A literal cipher key already loaded as bytes. Use with `SymmetricWireFormat.Raw`. |
| `CryptoKey.FromHexString(string hex)` | A literal cipher key encoded as hex. Use with `SymmetricWireFormat.Raw`. |
| `CryptoKey.FromBase64String(string base64)` | A literal cipher key encoded as Base64. Use with `SymmetricWireFormat.Raw`. |

`FromPassword` is required for the password-based formats (`Classic`, `Owasp2026`, `OpenSslEnc`). `FromRawBytes` / `FromHexString` / `FromBase64String` are required for `Raw`. The service rejects the wrong combination with an `ArgumentException`.

### Symmetric wire format — `SymmetricEncryptOptions` / `SymmetricDecryptOptions`

All symmetric methods default to `SymmetricWireFormat.Classic` — the byte-stable UiPath layout (`salt(8) ‖ IV ‖ ct [‖ tag(16)]`, PBKDF2-HMAC-SHA1 @ 10 000 iterations). Pass an options instance to opt into a different format:

| Factory | Format | Notes |
|---------|--------|-------|
| `SymmetricEncryptOptions.Classic()` / `SymmetricDecryptOptions.Classic()` | `Classic` | Default. Frozen wire format for back-compat. |
| `SymmetricEncryptOptions.Owasp2026(int kdfIterations = 0)` | `Owasp2026` | Same wire layout as Classic; caller-controlled iter count (default: 1 300 000). |
| `SymmetricEncryptOptions.Raw(byte[] iv = null)` / `SymmetricDecryptOptions.Raw()` | `Raw` | Caller-supplied key + IV. Third-party interop. |
| `SymmetricEncryptOptions.OpenSslEnc(int kdfIterations = 0)` | `OpenSslEnc` | `openssl enc`-compatible (`Salted__` magic + PBKDF2-HMAC-SHA256). Default iter: 600 000. |

See [`docs/symmetric-wire-format.md`](../../docs/symmetric-wire-format.md) for the full byte layouts and third-party interop reference.

### Symmetric encryption — IV / salt strategy

All symmetric encrypt methods are **non-deterministic by default**: a fresh random 8-byte salt (where applicable) and IV/nonce are generated on every call and embedded in the ciphertext stream. Encrypting the same plaintext twice always produces different ciphertext. The matching decrypt method reconstructs the salt and IV from the same stream automatically.

- **CBC-family** (`AES`, `Rijndael`, `DES`, `TripleDES`, `RC2`): PKCS7 padding, CBC mode, random IV.
- **AES-GCM** (`AESGCM`) — AEAD with random 96-bit nonce and 128-bit auth tag. **Recommended for new workflows.**
- **ChaCha20-Poly1305** (`ChaCha20Poly1305`) — AEAD alternative to AES-GCM.

For `Raw`, you may supply an explicit IV via `SymmetricEncryptOptions.Raw(iv)`; pass `null` (the factory default) to let the cipher generate one.

### PGP key material — `PgpPublicKey` / `PgpPrivateKey` / `PgpKeyPair`

PGP methods take strongly-typed key handles. Construct them once and reuse them across calls:

| Factory | Purpose |
|---------|---------|
| `PgpPublicKey.FromBytes(byte[] keyBytes)` | Public key from in-memory bytes (ASCII-armored or binary). |
| `PgpPublicKey.FromFilePath(string path)` | Public key loaded from a `.asc` / `.gpg` file. |
| `PgpPrivateKey.FromBytes(byte[] keyBytes, string passphrase)` | Private key + passphrase, bound together. |
| `PgpPrivateKey.FromBytes(byte[] keyBytes, SecureString passphrase)` | Same, with `SecureString` passphrase. |
| `PgpPrivateKey.FromFilePath(string path, string passphrase)` | Private key from file. |
| `PgpPrivateKey.FromFilePath(string path, SecureString passphrase)` | Private key from file with `SecureString`. |

`PgpKeyPair` (returned by `PgpGenerateKeys`) holds a matched public/private pair. Use `pair.PublicKey` / `pair.PrivateKey`, or deconstruct with `var (pub, priv) = pair;`. Persist with `pair.PublicKey.Save(path)` / `pair.PrivateKey.Save(path)` when you need files on disk.

Passing a `PgpPrivateKey` to an encrypt method implies signing; passing a `PgpPublicKey` to a decrypt method implies signature verification — separate `bool sign` / `bool verifySignature` flags are not used.

### PGP passphrase limitation

`PgpPrivateKey` carries its passphrase as a managed `string` because the underlying BouncyCastle library requires a plain string and offers no `byte[]`-based passphrase API. The `SecureString` factories materialise to a string at construction and cannot zero it afterward. For maximum security with PGP, prefer key rings without passphrase protection, or accept that the passphrase briefly exists as a managed string.

---

## Symmetric Encryption

### `byte[] EncryptBytes(byte[] input, EncryptionAlgorithm algorithm, CryptoKey key, SymmetricEncryptOptions options = null)`

Encrypts arbitrary bytes. With the default `options`, produces a `Classic` blob (salt + IV + ct prepended).

**Returns:** `byte[]` — ciphertext per the chosen wire format.

### `string EncryptText(string input, EncryptionAlgorithm algorithm, CryptoKey key, SymmetricEncryptOptions options = null)`

Encrypts a string and returns the result as Base64-encoded ciphertext. **The input is always transcoded with UTF-8** — there is no encoding parameter. For non-UTF-8 text, transcode at the call site and use `EncryptBytes`.

**Returns:** `string` — Base64-encoded ciphertext.

### `void EncryptFile(string inputPath, string outputPath, EncryptionAlgorithm algorithm, CryptoKey key, SymmetricEncryptOptions options = null, bool overwrite = false)`

Reads a file, encrypts it, and writes the result. Throws `InvalidOperationException` if `outputPath` exists and `overwrite` is false.

---

## Symmetric Decryption

### `byte[] DecryptBytes(byte[] input, EncryptionAlgorithm algorithm, CryptoKey key, SymmetricDecryptOptions options = null)`

Decrypts ciphertext produced by `EncryptBytes`. `options.Format` must match the format used at encrypt time.

**Returns:** `byte[]` — plaintext bytes.

### `string DecryptText(string input, EncryptionAlgorithm algorithm, CryptoKey key, SymmetricDecryptOptions options = null)`

Decrypts a Base64-encoded ciphertext produced by `EncryptText` and returns the plaintext. **The plaintext bytes are always decoded with UTF-8** — there is no encoding parameter. For non-UTF-8 text, use `DecryptBytes` and decode at the call site.

### `void DecryptFile(string inputPath, string outputPath, EncryptionAlgorithm algorithm, CryptoKey key, SymmetricDecryptOptions options = null, bool overwrite = false)`

Reads an encrypted file and writes the plaintext. Throws `InvalidOperationException` if `outputPath` exists and `overwrite` is false.

---

## Keyed Hashing

Keyed-hash methods compute an HMAC (or plain hash for non-HMAC algorithms) and return the result as an uppercase hex string. One-way — no inverse operation.

### `string KeyedHashBytes(byte[] input, KeyedHashAlgorithms algorithm, CryptoKey key)`
### `string KeyedHashText(string input, KeyedHashAlgorithms algorithm, CryptoKey key)`
### `string KeyedHashFile(string inputPath, KeyedHashAlgorithms algorithm, CryptoKey key)`

**Returns:** `string` — uppercase hex-encoded hash digest.

---

## PGP Encryption

If `signer` is supplied, the encrypted payload is also signed with that private key.

### `byte[] PgpEncryptBytes(byte[] input, PgpPublicKey recipient, PgpPrivateKey signer = null)`
### `string PgpEncryptText(string input, PgpPublicKey recipient, PgpPrivateKey signer = null)`
### `void PgpEncryptFile(string inputPath, string outputPath, PgpPublicKey recipient, PgpPrivateKey signer = null, bool overwrite = false)`

---

## PGP Decryption

If `verifier` is supplied, the embedded signature is verified during decrypt.

### `byte[] PgpDecryptBytes(byte[] input, PgpPrivateKey recipient, PgpPublicKey verifier = null)`
### `string PgpDecryptText(string input, PgpPrivateKey recipient, PgpPublicKey verifier = null)`
### `void PgpDecryptFile(string inputPath, string outputPath, PgpPrivateKey recipient, PgpPublicKey verifier = null, bool overwrite = false)`

---

## PGP Signing (binary signature)

Produces a binary-signed payload. Verify with `PgpVerify*`.

### `byte[] PgpSignBytes(byte[] input, PgpPrivateKey signer)`
### `string PgpSignText(string input, PgpPrivateKey signer)`
### `void PgpSignFile(string inputPath, string outputPath, PgpPrivateKey signer, bool overwrite = false)`

---

## PGP Clear-Signing

Clear-signatures keep the original content human-readable with the signature appended. Verify with `PgpVerifyClearSigned*`.

### `byte[] PgpClearSignBytes(byte[] input, PgpPrivateKey signer)`
### `string PgpClearSignText(string input, PgpPrivateKey signer)`
### `void PgpClearSignFile(string inputPath, string outputPath, PgpPrivateKey signer, bool overwrite = false)`

---

## PGP Verification

### Binary signatures

Verify payloads produced by `PgpSign*` (or `PgpEncrypt*` with a signer).

| Method | Signature |
|--------|-----------|
| Bytes | `bool PgpVerifyBytes(byte[] input, PgpPublicKey verifier)` |
| Text  | `bool PgpVerifyText(string input, PgpPublicKey verifier)` |
| File  | `bool PgpVerifyFile(string inputPath, PgpPublicKey verifier)` |

**Returns:** `bool` — `true` when the signature is valid; `false` otherwise.

### Clear-signatures

Verify payloads produced by `PgpClearSign*`.

| Method | Signature |
|--------|-----------|
| Bytes | `bool PgpVerifyClearSignedBytes(byte[] input, PgpPublicKey verifier)` |
| Text  | `bool PgpVerifyClearSignedText(string input, PgpPublicKey verifier)` |
| File  | `bool PgpVerifyClearSignedFile(string inputPath, PgpPublicKey verifier)` |

### Public-key well-formedness

Confirms that a `PgpPublicKey` instance parses as a well-formed OpenPGP public key. Mirrors the `PgpVerify` activity's `Mode = PublicKey`.

`bool PgpVerifyPublicKey(PgpPublicKey key)`

**Returns:** `bool` — `true` when the key is valid.

---

## PGP Key-Pair Generation

Generates an OpenPGP RSA key pair **in memory** and returns both halves as a matched `PgpKeyPair`. Persist by calling `Save(path)` on each half.

### `PgpKeyPair PgpGenerateKeys(string userId, string passphrase, RsaKeySize keySize = RsaKeySize.Rsa4096)`
### `PgpKeyPair PgpGenerateKeys(string userId, SecureString passphrase, RsaKeySize keySize = RsaKeySize.Rsa4096)`

**Parameters:**
- `userId` (`string`) — OpenPGP User ID; conventionally an RFC 2822 mailbox such as `Alice Doe <alice@example.com>`.
- `passphrase` — Passphrase that protects the generated private key. Bound to the returned `PgpPrivateKey`.
- `keySize` (`RsaKeySize`) — RSA key size. Default `Rsa4096`. `Rsa3072` and `Rsa2048` are accepted for interop with legacy systems.

**Returns:** `PgpKeyPair` — `pair.PublicKey` and `pair.PrivateKey` (also accessible via deconstruction).

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

### `SymmetricWireFormat`

Used by `SymmetricEncryptOptions.Format` / `SymmetricDecryptOptions.Format`.

| Value | Notes |
|-------|-------|
| `Classic` | UiPath's byte-stable layout, PBKDF2-HMAC-SHA1 @ 10 000 iter. Default. Frozen for back-compat. |
| `Owasp2026` | Classic layout with OWASP-recommended iter count (1 300 000). Caller can override via `kdfIterations`. |
| `Raw` | `IV ‖ ct [‖ tag]` — caller supplies the literal key (and optionally IV). Third-party interop. |
| `OpenSslEnc` | `Salted__ ‖ salt(8) ‖ ct [‖ tag]`, PBKDF2-HMAC-SHA256 @ 600 000 iter (default). Compatible with `openssl enc -pbkdf2`. |

### `KeyedHashAlgorithms`

Used by `KeyedHashBytes`/`KeyedHashText`/`KeyedHashFile`.

| Value | Type | Notes |
|-------|------|-------|
| `HMACSHA256` | Keyed HMAC | Recommended for MAC/integrity verification. |
| `HMACSHA384` | Keyed HMAC | |
| `HMACSHA512` | Keyed HMAC | |
| `SHA256` | Unkeyed hash | Key is ignored — equivalent to a plain SHA hash. |
| `SHA384` | Unkeyed hash | Key is ignored. |
| `SHA512` | Unkeyed hash | Key is ignored. |
| `HMACSHA1` | Keyed HMAC | **`[Obsolete]` — SHA-1 is deprecated by NIST; prefer SHA256 or higher.** |
| `HMACMD5` | Keyed HMAC | **`[Obsolete]` — MD5 is broken; avoid for any security-sensitive use.** |
| `SHA1` | Unkeyed hash | **`[Obsolete]` — collision attacks demonstrated; do not use.** |

### `RsaKeySize`

Used by `PgpGenerateKeys`.

| Value | Bits |
|-------|------|
| `Rsa2048` | 2048 |
| `Rsa3072` | 3072 |
| `Rsa4096` | 4096 (default) |

---

## Common Patterns

### Encrypt and decrypt a string with AES-GCM (Classic, the default)

```csharp
[Workflow]
public void Execute()
{
    var key = CryptoKey.FromPassword("MySecretKey123!", Encoding.UTF8);

    var ciphertext = cryptography.EncryptText("Sensitive data", EncryptionAlgorithm.AESGCM, key);
    Log($"Encrypted: {ciphertext}");

    var plaintext = cryptography.DecryptText(ciphertext, EncryptionAlgorithm.AESGCM, key);
    Log($"Decrypted: {plaintext}");
}
```

### Encrypt with caller-supplied raw key + IV (third-party interop)

```csharp
[Workflow]
public void Execute()
{
    // 32 bytes → AES-256
    byte[] rawKey = Convert.FromBase64String("your-base64-encoded-32-byte-key==");
    byte[] iv     = Convert.FromHexString("a3f1b2c4d5e6f70819a0b1c2d3e4f506");

    var key = CryptoKey.FromRawBytes(rawKey);

    byte[] cipher = cryptography.EncryptBytes(
        Encoding.UTF8.GetBytes("payload"),
        EncryptionAlgorithm.AESGCM,
        key,
        SymmetricEncryptOptions.Raw(iv));

    // Decrypt — IV is read from the ciphertext stream prefix; no need to pass it again.
    byte[] plain = cryptography.DecryptBytes(
        cipher,
        EncryptionAlgorithm.AESGCM,
        key,
        SymmetricDecryptOptions.Raw());
}
```

### Decrypt a file produced by `openssl enc`

```csharp
[Workflow]
public void Execute()
{
    // openssl enc -aes-256-cbc -pbkdf2 -iter 600000 -md sha256 -salt -k password -in plain.txt -out cipher.bin
    var key = CryptoKey.FromPassword("password", Encoding.UTF8);

    cryptography.DecryptFile(
        inputPath:  @"C:\Documents\cipher.bin",
        outputPath: @"C:\Documents\plain.txt",
        algorithm:  EncryptionAlgorithm.AES,
        key:        key,
        options:    SymmetricDecryptOptions.OpenSslEnc(),
        overwrite:  true);
}
```

### Use a stronger KDF iteration count (`Owasp2026`)

```csharp
[Workflow]
public void Execute()
{
    var key = CryptoKey.FromPassword("MySecretKey", Encoding.UTF8);

    // Owasp2026() with default kdfIterations = 0 uses the OWASP recommendation (1,300,000).
    var ciphertext = cryptography.EncryptBytes(
        Encoding.UTF8.GetBytes("payload"),
        EncryptionAlgorithm.AESGCM,
        key,
        SymmetricEncryptOptions.Owasp2026());

    // Decrypt must use the same iteration count — Owasp2026 does not store it in the wire format.
    byte[] plain = cryptography.DecryptBytes(
        ciphertext,
        EncryptionAlgorithm.AESGCM,
        key,
        SymmetricDecryptOptions.Owasp2026());
}
```

### Compute an HMAC-SHA256 for data integrity verification

```csharp
[Workflow]
public void Execute()
{
    byte[] hmacKey = Convert.FromBase64String("your-base64-hmac-key==");
    var key = CryptoKey.FromRawBytes(hmacKey);

    var digest = cryptography.KeyedHashText("payload to verify", KeyedHashAlgorithms.HMACSHA256, key);
    Log($"HMAC-SHA256: {digest}");
}
```

### PGP encrypt a message for a recipient

```csharp
[Workflow]
public void Execute()
{
    var recipient = PgpPublicKey.FromFilePath(@"C:\Keys\recipient_public.asc");

    byte[] encrypted = cryptography.PgpEncryptBytes(
        Encoding.UTF8.GetBytes("Confidential message"),
        recipient);

    File.WriteAllBytes(@"C:\Output\message.pgp", encrypted);
}
```

### PGP encrypt + sign, then decrypt + verify

```csharp
[Workflow]
public void Execute()
{
    var recipientPublic  = PgpPublicKey.FromFilePath(@"C:\Keys\recipient_public.asc");
    var senderPrivate    = PgpPrivateKey.FromFilePath(@"C:\Keys\sender_private.asc", "senderPassphrase");

    // Passing a signer to PgpEncrypt* implies sign-and-encrypt.
    byte[] encrypted = cryptography.PgpEncryptBytes(
        Encoding.UTF8.GetBytes("Signed and encrypted message"),
        recipientPublic,
        signer: senderPrivate);

    var recipientPrivate = PgpPrivateKey.FromFilePath(@"C:\Keys\recipient_private.asc", "recipientPassphrase");
    var senderPublic     = PgpPublicKey.FromFilePath(@"C:\Keys\sender_public.asc");

    // Passing a verifier to PgpDecrypt* implies verify-while-decrypting.
    byte[] decrypted = cryptography.PgpDecryptBytes(
        encrypted,
        recipientPrivate,
        verifier: senderPublic);

    Log(Encoding.UTF8.GetString(decrypted));
}
```

### Generate a new PGP key pair

```csharp
[Workflow]
public void Execute()
{
    PgpKeyPair pair = cryptography.PgpGenerateKeys(
        userId:     "Alice <alice@example.com>",
        passphrase: "StrongPassphrase!",
        keySize:    RsaKeySize.Rsa4096);

    pair.PublicKey.Save(@"C:\Keys\my_public.asc");
    pair.PrivateKey.Save(@"C:\Keys\my_private.asc");

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
    var candidate = PgpPublicKey.FromBytes(Encoding.UTF8.GetBytes(armoredPublicKey));

    if (!cryptography.PgpVerifyPublicKey(candidate))
    {
        throw new InvalidOperationException("Supplied content is not a valid OpenPGP public key.");
    }

    candidate.Save(@"C:\Keys\trusted_public.asc");
}
```
