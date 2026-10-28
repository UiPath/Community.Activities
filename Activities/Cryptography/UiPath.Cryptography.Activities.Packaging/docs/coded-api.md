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

The `cryptography` service exposes all operations as **direct method calls** — there is no connection, handle, or scope to open. It is registered as a **stateless singleton**, so the accessor is shared across the workflow and its methods are safe to call concurrently. Call methods on the service accessor directly:

```csharp
var key = PasswordKey.FromPassword("mykey", Encoding.UTF8);
var ciphertext = cryptography.EncryptText("secret", EncryptionAlgorithm.AESGCM, SymmetricEncryptOptions.Classic(key));
```

### Bytes / Text / File matrix

Every logical operation exposes three input/output forms — pick the one that matches the data you already have:

| Form | Suffix | Input → Output | When to use |
|------|--------|---------------|-------------|
| **Bytes** | (base) | `byte[]` → `byte[]` | Binary or already-loaded data |
| **Text**  | `...Text` | `string` → `string` (Base64 / ASCII-armored) | Data arriving as text (HTTP, config, env) |
| **File**  | `...File` | file path → file path | Data lives on disk |

### Key material — `PasswordKey` and `RawKey`

Symmetric and keyed-hash operations take key material as one of two concrete `CryptoKey` subtypes. The class you pick determines which wire formats the key can be used with — and the type system enforces that at compile time via the format factories below.

**`PasswordKey`** — password material to be PBKDF2-stretched into a cipher key. Used with the `Classic`, `Owasp2026`, and `OpenSslEnc` wire formats.

| Factory | Purpose |
|---------|---------|
| `PasswordKey.FromPassword(string password, Encoding encoding)` | Password / passphrase as a `string`. |
| `PasswordKey.FromPassword(SecureString password, Encoding encoding)` | Same, sourced from a secret store or user input. |

`PasswordKey` stores the password internally as a `SecureString` (the `string` factory copies the input characters into one) and materialises the cipher-key bytes **just-in-time** on each encrypt/decrypt operation — the bytes live only on the operation's stack frame, never pinned to the `PasswordKey` instance. The intermediate unmanaged Unicode buffer and `char[]` are zeroed after each materialisation. `PasswordKey` is `IDisposable`: calling `Dispose()` eagerly zeroes the protected SecureString buffer and nulls the stored reference, and subsequent `KeyBytes` access throws `ObjectDisposedException` — recommended for long-lived workflows.

**`RawKey`** — a literal cipher key of the algorithm's exact required length (e.g. 32 bytes for AES-256). Used with the `Raw` wire format. No KDF. `IDisposable`: calling `Dispose()` zeroes the held key bytes in place and subsequent `KeyBytes` access throws `ObjectDisposedException`, so a stale reference cannot silently encrypt with an all-zero key.

| Factory | Purpose |
|---------|---------|
| `RawKey.FromBytes(byte[] keyBytes)` | A key already loaded as bytes. |
| `RawKey.FromHex(string hex)` | A key encoded as hex. |
| `RawKey.FromBase64(string base64)` | A key encoded as Base64. |

Keyed-hash methods accept either subtype (they take `CryptoKey` directly — no wire-format axis).

A `RawKey`'s length is **not** validated by `RawKey.FromBytes` / `FromHex` / `FromBase64`; it is checked when you call the encrypt/decrypt method, and an illegal length for the chosen algorithm throws `ArgumentException` listing the legal byte lengths (e.g. 16/24/32 for AES). Construct the key with exactly the algorithm's required key size.

### Symmetric wire format — `SymmetricEncryptOptions` / `SymmetricDecryptOptions`

The options object bundles the key, wire format, and any format-specific knobs (IV for `Raw`, KDF iterations for `Owasp2026` / `OpenSslEnc`). Construct via a format factory — the factory's key-parameter type enforces the (key kind × wire format) pairing at compile time, so a `PasswordKey` cannot be passed to `Raw(...)` and a `RawKey` cannot be passed to `Classic(...)` etc.

| Factory | Format | Key type accepted | Notes |
|---------|--------|------------------|-------|
| `SymmetricEncryptOptions.Classic(PasswordKey key, Encoding encoding = null)` / `SymmetricDecryptOptions.Classic(PasswordKey key, Encoding encoding = null)` | `Classic` | `PasswordKey` | Default. Frozen wire format for back-compat (PBKDF2-HMAC-SHA1 @ 10 000 iter). |
| `SymmetricEncryptOptions.Owasp2026(PasswordKey key, int kdfIterations = 1_300_000, Encoding encoding = null)` | `Owasp2026` | `PasswordKey` | Same wire layout as Classic with PBKDF2-HMAC-SHA1 at OWASP 2026's recommended iteration count. |
| `SymmetricEncryptOptions.Raw(RawKey key, byte[] iv = null, Encoding encoding = null)` / `SymmetricDecryptOptions.Raw(RawKey key, Encoding encoding = null)` | `Raw` | `RawKey` | Caller-supplied key + IV. Third-party interop. |
| `SymmetricEncryptOptions.OpenSslEnc(PasswordKey key, int kdfIterations = 600_000, Encoding encoding = null, AesKeySize aesKeySize = AesKeySize.Aes256)` | `OpenSslEnc` | `PasswordKey` | `openssl enc`-compatible (`Salted__` magic + PBKDF2-HMAC-SHA256). `aesKeySize` selects AES-128 / -192 / -256 to match the peer's `openssl enc -aes-N-cbc`. |

The decrypt factories take the same shape (no `IV` on the decrypt side — the IV is read from the ciphertext stream automatically). The optional `encoding:` parameter sets `TextEncoding` on the options (defaults to UTF-8) and is only consulted by `EncryptText` / `DecryptText`.

A constructed options object is read-only but **introspectable**: `CryptoOptions` exposes the getters `Key`, `Format`, `KdfIterations`, and `TextEncoding`; `SymmetricEncryptOptions` additionally exposes `IV` and `AesKeySize`, and `SymmetricDecryptOptions` exposes `AesKeySize`. All are set only through the factories (the setters are not public), so an options instance is immutable once built.

See [`docs/symmetric-wire-format.md`](../../docs/symmetric-wire-format.md) for the full byte layouts and third-party interop reference.

### Symmetric encryption — IV / salt strategy

All symmetric encrypt methods are **non-deterministic by default**: a fresh random 8-byte salt (where applicable) and IV/nonce are generated on every call and embedded in the ciphertext stream. Encrypting the same plaintext twice always produces different ciphertext. The matching decrypt method reconstructs the salt and IV from the same stream automatically.

- **CBC-family** (`AES`, `Rijndael`, `DES`, `TripleDES`, `RC2`): PKCS7 padding, CBC mode, random IV.
- **AES-GCM** (`AESGCM`) — AEAD with random 96-bit nonce and 128-bit auth tag. **Recommended for new workflows.**
- **ChaCha20-Poly1305** (`ChaCha20Poly1305`) — AEAD alternative to AES-GCM.

For `Raw`, you may supply an explicit IV via `SymmetricEncryptOptions.Raw(key, iv)`; pass `null` (the factory default) to let the cipher generate one.

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

Each key handle also exposes two instance methods:

| Member | Purpose |
|--------|---------|
| `byte[] ToBytes()` | Returns a copy of the key bytes **as loaded** — ASCII-armored if the key was created from armored input, binary if loaded from binary; `ToBytes()` round-trips the original bytes rather than re-encoding them. Keys returned by `PgpGenerateKeys` are ASCII-armored. |
| `void Save(string filePath, bool overwrite = false)` | Writes the key to disk. **Throws `InvalidOperationException` if the file exists and `overwrite` is `false`** — pass `overwrite: true` to replace an existing file. |

`PgpKeyPair` (returned by `PgpGenerateKeys`, or constructed directly via `new PgpKeyPair(publicKey, privateKey)`) holds a matched public/private pair. Use `pair.PublicKey` / `pair.PrivateKey`, or deconstruct with `var (pub, priv) = pair;`. Persist with `pair.PublicKey.Save(path)` / `pair.PrivateKey.Save(path)` (add `overwrite: true` to replace existing files) when you need files on disk.

Passing a `PgpPrivateKey` to an encrypt method implies signing; passing a `PgpPublicKey` to a decrypt method implies signature verification — separate `bool sign` / `bool verifySignature` flags are not used.

### PGP passphrase handling

`PgpPrivateKey` stores its passphrase as a `SecureString` for the lifetime of the instance (both the `string` and `SecureString` factories copy the supplied passphrase into one). It is materialised to a managed `string` only for the duration of each cryptographic operation — on the service call's stack frame, not pinned to the key object — because the underlying BouncyCastle library requires a plain `string` and offers no `byte[]`-based passphrase API. That transient string cannot be deterministically zeroed (managed strings are immutable), but its heap residency is bounded by the operation rather than by the object's lifetime.

`PgpPrivateKey` is `IDisposable`: calling `Dispose()` eagerly zeroes the protected `SecureString` buffer. The instance is safe to use without disposing — the `SecureString` finalizer cleans up eventually — but disposing is recommended for sensitive, long-lived workflows. (`PgpPublicKey` holds no secret material and is not `IDisposable`.)

---

## Relationship to the XAML activities

The coded API and the [XAML activities](overview.md) run on the **same cryptographic core**, so they are at full parity on algorithms, wire formats (`Classic` / `Owasp2026` / `Raw` / `OpenSslEnc`), IV / `KdfIterations` / `AesKeySize` / encoding options, keyed-hash algorithms, and every PGP operation (encrypt, decrypt, sign, clearsign, verify in all three modes, generate). Anything you can compute with an activity, you can compute with this service. The differences are about **I/O shape and key-input form**, not capability.

**The coded API does more than the activities:**

| Coded-only capability | Detail |
|----------------------|--------|
| **Bytes I/O on every operation** | `EncryptBytes` / `DecryptBytes`, `KeyedHashBytes`, `PgpEncryptBytes` / `PgpDecryptBytes`, `PgpSignBytes` / `PgpClearSignBytes`, `PgpVerifyBytes` / `PgpVerifyClearSignedBytes`. No activity has a `byte[]` in/out form. |
| **Raw `byte[]` keys** | `RawKey.FromBytes(byte[])` for symmetric `Raw` and keyed hash. Activities can only supply raw keys as hex/base64 **strings** (and keyed-hash activities have no raw-key path at all). |
| **In-memory PGP key material** | `PgpPublicKey.FromBytes` / `PgpPrivateKey.FromBytes` let you encrypt/decrypt/sign/verify without key files on disk. The activities require key **files**. |
| **Text & Bytes Sign / ClearSign / Verify** | `PgpSignText` / `PgpClearSignText`, `PgpVerifyText` / `PgpVerifyClearSignedText` (+ Bytes forms). The activities expose only `PgpSignFile` / `PgpClearSignFile`, and `PgpVerify` is file-only. |
| **In-memory `PgpKeyPair`** | `PgpGenerateKeys` returns a usable key pair without forcing file output; the activity only writes the two key files. |

**The activities do two things the coded API doesn't — neither is a cryptographic capability:**

- **UiPath resource handles** (`IResource` / `ILocalResource`) as inputs and outputs (e.g. Storage-bucket / project resources). The coded API takes plain `string` file paths and `byte[]`; a coded workflow works with local paths and in-memory data, not resource handles.
- **`ContinueOnError`** swallow-and-continue. In a coded workflow you use ordinary `try/catch` instead — every method throws on failure.

> **Encoding note (shaped differently, not a gap):** the Text activities split *key encoding* and *plaintext encoding* into two properties. In the coded API the password encoding is set on `PasswordKey.FromPassword(..., encoding)` and the plaintext encoding via the options `encoding:` parameter — both axes remain independently controllable.

---

## Migrating from the prior coded API

The coded API surface introduced in the prior release has been **consolidated** in this version. The old call shapes — separate `string` / `SecureString` / `byte[]` key overloads with an `Encoding` parameter, plus a path-based `PgpGenerateKeys` — are replaced by a single options-based shape per operation so that the (key kind × wire format) pairing is enforced at compile time. There are **no `[Obsolete]` shims**: code written against the prior API must be updated to compile against this package.

### One-to-one replacement

| Before | After |
|--------|-------|
| `EncryptBytes(input, algo, string key, Encoding enc)` | `EncryptBytes(input, algo, SymmetricEncryptOptions.Classic(PasswordKey.FromPassword(key, enc)))` |
| `EncryptBytes(input, algo, SecureString key, Encoding enc)` | `EncryptBytes(input, algo, SymmetricEncryptOptions.Classic(PasswordKey.FromPassword(key, enc)))` |
| `EncryptBytes(input, algo, byte[] keyBytes)` | `EncryptBytes(input, algo, SymmetricEncryptOptions.Raw(RawKey.FromBytes(keyBytes)))` |
| `EncryptText(input, algo, key, enc)` | `EncryptText(input, algo, SymmetricEncryptOptions.Classic(PasswordKey.FromPassword(key, enc), enc))` *(see encoding note)* |
| `EncryptFile(in, out, algo, key, enc, overwrite)` | `EncryptFile(in, out, algo, SymmetricEncryptOptions.Classic(PasswordKey.FromPassword(key, enc)), overwrite)` |
| `DecryptBytes` / `DecryptText` / `DecryptFile` | Same shape — `SymmetricDecryptOptions.<Format>(...)` |
| `KeyedHashBytes(input, algo, string key, Encoding enc)` | `KeyedHashBytes(input, algo, PasswordKey.FromPassword(key, enc))` |
| `KeyedHashBytes(input, algo, byte[] keyBytes)` | `KeyedHashBytes(input, algo, RawKey.FromBytes(keyBytes))` |
| `KeyedHashText(input, algo, string key, Encoding enc)` | `KeyedHashText(input, algo, PasswordKey.FromPassword(key, enc), enc)` *(see encoding note — the old `enc` covered both password and input bytes; in the new API the password encoding goes on `PasswordKey.FromPassword`, and the same encoding is also passed as the trailing parameter to drive input transcoding)* |
| `KeyedHashFile(inputPath, algo, string key, Encoding enc)` | `KeyedHashFile(inputPath, algo, PasswordKey.FromPassword(key, enc))` *(the old `enc` only ever applied to the password — file contents are hashed byte-for-byte)* |
| `PgpGenerateKeys(publicKeyPath, privateKeyPath, userId, passphrase, keySize)` *(path-based)* | `var pair = PgpGenerateKeys(userId, passphrase, keySize); pair.PublicKey.Save(publicKeyPath); pair.PrivateKey.Save(privateKeyPath);` |

### Plaintext encoding for `EncryptText` / `DecryptText`

The old text APIs took the plaintext `Encoding` as a positional parameter. In the new shape, **the same encoding is carried on the options object** via the optional trailing `encoding:` parameter on every format factory, and defaults to `Encoding.UTF8` when omitted:

```csharp
// Old
cryptography.EncryptText(input, algo, key, Encoding.Latin1);

// New
var pwKey = PasswordKey.FromPassword(key, Encoding.UTF8);  // password bytes encoding
cryptography.EncryptText(input, algo, SymmetricEncryptOptions.Classic(pwKey, Encoding.Latin1));
//                                                                    plaintext encoding ↑
```

The encoding on the options is ignored by `EncryptBytes` / `DecryptBytes` / `EncryptFile` / `DecryptFile` — for those, transcode at the call site if needed.

`KeyedHashText` follows the same default (UTF-8) and exposes an analogous optional trailing `encoding:` parameter directly on the method (keyed-hash methods take no options object). Non-UTF-8 callers of the prior `KeyedHashText(input, algo, key, enc)` overload should pass the same `enc` to the new signature to reproduce identical digests. `KeyedHashBytes` operates on raw bytes and `KeyedHashFile` hashes file content byte-for-byte — neither needs an encoding.

### Behaviours to be aware of

- **Default text encoding is UTF-8.** Code that didn't pass an encoding to the old text APIs will continue to behave identically if the old call also used UTF-8. Calls that relied on the prior default of UTF-8 need no migration changes beyond the options refactor.
- **No silent compile-by-renaming.** Because every key/options type is new, code referencing the old overloads fails to compile rather than silently picking up a different overload. The mapping above gives the one-to-one replacement for each.
- **Path-based `PgpGenerateKeys` returns a key pair, not files.** The old overload wrote files as a side effect. The new overload returns an in-memory `PgpKeyPair`; call `.Save(path)` on each half to persist.

---

## Symmetric Encryption

### `byte[] EncryptBytes(byte[] input, EncryptionAlgorithm algorithm, SymmetricEncryptOptions options)`

Encrypts arbitrary bytes. The `options` parameter carries the key + wire format; construct via a `SymmetricEncryptOptions.<Format>(key, ...)` factory.

**Returns:** `byte[]` — ciphertext per the chosen wire format.

### `string EncryptText(string input, EncryptionAlgorithm algorithm, SymmetricEncryptOptions options)`

Encrypts a string and returns the result as Base64-encoded ciphertext. The plaintext encoding is read from `options.TextEncoding` — defaulting to UTF-8 when the factory's optional `encoding:` parameter is omitted. Pass a different encoding to the format factory (`Classic(key, Encoding.Latin1)`, etc.) when migrating ciphertext produced by non-UTF-8 callers of the prior API.

**Returns:** `string` — Base64-encoded ciphertext.

### `void EncryptFile(string inputPath, string outputPath, EncryptionAlgorithm algorithm, SymmetricEncryptOptions options, bool overwrite = false)`

Reads a file, encrypts it, and writes the result. Throws `InvalidOperationException` if `outputPath` exists and `overwrite` is false.

---

## Symmetric Decryption

### `byte[] DecryptBytes(byte[] input, EncryptionAlgorithm algorithm, SymmetricDecryptOptions options)`

Decrypts ciphertext produced by `EncryptBytes`. `options.Format` must match the format used at encrypt time.

**Returns:** `byte[]` — plaintext bytes.

### `string DecryptText(string input, EncryptionAlgorithm algorithm, SymmetricDecryptOptions options)`

Decrypts a Base64-encoded ciphertext produced by `EncryptText` and returns the plaintext. The plaintext encoding is read from `options.TextEncoding` — defaulting to UTF-8 when the factory's optional `encoding:` parameter is omitted, and must match the encoding used at encrypt time.

### `void DecryptFile(string inputPath, string outputPath, EncryptionAlgorithm algorithm, SymmetricDecryptOptions options, bool overwrite = false)`

Reads an encrypted file and writes the plaintext. Throws `InvalidOperationException` if `outputPath` exists and `overwrite` is false.

---

## Keyed Hashing

Keyed-hash methods compute an HMAC (or plain hash for non-HMAC algorithms) and return the result as an uppercase hex string. One-way — no inverse operation.

### `string KeyedHashBytes(byte[] input, KeyedHashAlgorithms algorithm, CryptoKey key)`
### `string KeyedHashText(string input, KeyedHashAlgorithms algorithm, CryptoKey key, Encoding encoding = null)`
### `string KeyedHashFile(string inputPath, KeyedHashAlgorithms algorithm, CryptoKey key)`

**Returns:** `string` — uppercase hex-encoded hash digest.

The optional `encoding` parameter on `KeyedHashText` controls how the input string is transcoded to bytes before hashing (defaults to UTF-8). Pass a non-UTF-8 encoding to match digests produced by the prior API when the caller passed a non-UTF-8 encoding for the input. `KeyedHashBytes` operates on raw bytes, and `KeyedHashFile` hashes the file contents byte-for-byte — neither has an encoding axis.

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
| `Rijndael` | Rijndael in CBC mode. **`[Obsolete]` — weak; avoid.** |
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
    var key = PasswordKey.FromPassword("MySecretKey123!", Encoding.UTF8);

    var ciphertext = cryptography.EncryptText("Sensitive data", EncryptionAlgorithm.AESGCM, SymmetricEncryptOptions.Classic(key));
    Log($"Encrypted: {ciphertext}");

    var plaintext = cryptography.DecryptText(ciphertext, EncryptionAlgorithm.AESGCM, SymmetricDecryptOptions.Classic(key));
    Log($"Decrypted: {plaintext}");
}
```

### Encrypt with caller-supplied raw key + IV (third-party interop)

```csharp
[Workflow]
public void Execute()
{
    // 32 bytes → AES-256
    byte[] rawKeyBytes = Convert.FromBase64String("your-base64-encoded-32-byte-key==");
    byte[] iv          = Convert.FromHexString("a3f1b2c4d5e6f70819a0b1c2d3e4f506");

    var key = RawKey.FromBytes(rawKeyBytes);

    byte[] cipher = cryptography.EncryptBytes(
        Encoding.UTF8.GetBytes("payload"),
        EncryptionAlgorithm.AESGCM,
        SymmetricEncryptOptions.Raw(key, iv));

    // Decrypt — IV is read from the ciphertext stream prefix; no need to pass it again.
    byte[] plain = cryptography.DecryptBytes(
        cipher,
        EncryptionAlgorithm.AESGCM,
        SymmetricDecryptOptions.Raw(key));
}
```

### Decrypt a file produced by `openssl enc`

```csharp
[Workflow]
public void Execute()
{
    // openssl enc -aes-256-cbc -pbkdf2 -iter 600000 -md sha256 -salt -k password -in plain.txt -out cipher.bin
    var key = PasswordKey.FromPassword("password", Encoding.UTF8);

    cryptography.DecryptFile(
        inputPath:  @"C:\Documents\cipher.bin",
        outputPath: @"C:\Documents\plain.txt",
        algorithm:  EncryptionAlgorithm.AES,
        options:    SymmetricDecryptOptions.OpenSslEnc(key),
        overwrite:  true);
}
```

### Use a stronger KDF iteration count (`Owasp2026`)

```csharp
[Workflow]
public void Execute()
{
    var key = PasswordKey.FromPassword("MySecretKey", Encoding.UTF8);

    // Owasp2026(key) defaults to kdfIterations = 1_300_000 (the OWASP 2026 recommendation, inlined in the factory signature).
    var ciphertext = cryptography.EncryptBytes(
        Encoding.UTF8.GetBytes("payload"),
        EncryptionAlgorithm.AESGCM,
        SymmetricEncryptOptions.Owasp2026(key));

    // Decrypt must use the same iteration count — Owasp2026 does not store it in the wire format.
    byte[] plain = cryptography.DecryptBytes(
        ciphertext,
        EncryptionAlgorithm.AESGCM,
        SymmetricDecryptOptions.Owasp2026(key));
}
```

### Compute an HMAC-SHA256 for data integrity verification

```csharp
[Workflow]
public void Execute()
{
    byte[] hmacKey = Convert.FromBase64String("your-base64-hmac-key==");
    var key = RawKey.FromBytes(hmacKey);

    // Keyed-hash methods take a CryptoKey directly — no options object (no wire-format axis).
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
