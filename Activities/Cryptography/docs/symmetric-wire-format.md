# UiPath.Cryptography symmetric ciphertext wire format

This document describes the on-the-wire format produced by `EncryptText` /
`EncryptFile` and consumed by `DecryptText` / `DecryptFile` when using a
**symmetric** algorithm (`AES`, `AESGCM`, `ChaCha20Poly1305`, `DES`, `RC2`,
`Rijndael`, `TripleDES`). PGP is out of scope — it has its own format defined
by RFC 9580.

The format is **UiPath-specific**. Ciphertext produced by these activities is
**not directly compatible** with raw `openssl enc`, ServiceNow's encryption
APIs, browser-based tools such as `devglan.com`, or any other tool that does
not implement the layout below.

## Byte layout

The bytes returned by `CryptographyHelper.EncryptData` are arranged as:

```
+-------------------+--------------------+----------------------+--------------------+
|       salt        |         IV         |      ciphertext      |  authentication    |
|     (8 bytes)     |     (block size)   |     (variable)       |   tag (AEAD only)  |
+-------------------+--------------------+----------------------+--------------------+
```

`EncryptText` Base64-encodes the result before returning a string.
`EncryptFile` writes the raw bytes to the output file.

### Per-algorithm sizes

| Algorithm           | Mode           | IV size  | Derived key size | Tag size | Padding | Authenticated |
| ------------------- | -------------- | -------- | ---------------- | -------- | ------- | ------------- |
| `AES`               | CBC            | 16 bytes | 32 bytes (256b)  | —        | PKCS7   | No            |
| `DES`               | CBC            | 8 bytes  | 8 bytes (64b)    | —        | PKCS7   | No            |
| `RC2`               | CBC            | 8 bytes  | 16 bytes (128b)  | —        | PKCS7   | No            |
| `Rijndael`          | CBC            | 16 bytes | 32 bytes (256b)  | —        | PKCS7   | No            |
| `TripleDES`         | CBC            | 8 bytes  | 24 bytes (192b)  | —        | PKCS7   | No            |
| `AESGCM`            | GCM            | 12 bytes | 32 bytes (256b)  | 16 bytes | none    | Yes           |
| `ChaCha20Poly1305`  | AEAD-stream    | 12 bytes | 32 bytes (256b)  | 16 bytes | none    | Yes           |

For non-AEAD algorithms there is no tag — ciphertext occupies all remaining
bytes after `salt + IV`. For AEAD algorithms the final 16 bytes are the
authentication tag.

## Key derivation

The key the activity asks for is **not** used directly. It is run through
PBKDF2 to derive the actual algorithm key:

| Parameter     | Value                                              |
| ------------- | -------------------------------------------------- |
| Function      | PBKDF2-HMAC-SHA1 (`Rfc2898DeriveBytes` default)    |
| Iterations    | 10 000                                             |
| Salt          | 8 bytes from `RandomNumberGenerator.Create()`      |
| Output length | Maximum legal key size of the algorithm, in bytes  |

The salt is generated freshly per call, which is **why two encryptions of the
same plaintext with the same key produce different ciphertexts**. This is
intentional — it protects the derived key against pre-computation attacks.

## Encoding

`EncryptText` / `DecryptText` first turn the plaintext string and the key
string into bytes using the activity's `Encoding` property (default UTF-8).
The byte arrays then enter `EncryptData` / `DecryptData` as described above.

## Reference decoder — Python (non-AEAD: TripleDES, AES, DES, RC2, Rijndael)

```python
import base64
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
from cryptography.hazmat.primitives.kdf.pbkdf2 import PBKDF2HMAC
from cryptography.hazmat.primitives.padding import PKCS7

# --- parameters: change to match the UiPath activity ---
ALGO     = algorithms.TripleDES   # AES / TripleDES / ... from cryptography.hazmat
KEY_LEN  = 24                     # see the per-algorithm table above (bytes)
IV_LEN   = 8                      # 8 for TripleDES/DES/RC2, 16 for AES/Rijndael
KEY      = "your-key-string".encode("utf-8")
CIPHER_B64 = "..."  # output of EncryptText

raw  = base64.b64decode(CIPHER_B64)
salt, iv, ct = raw[:8], raw[8:8+IV_LEN], raw[8+IV_LEN:]

derived = PBKDF2HMAC(
    algorithm=hashes.SHA1(), length=KEY_LEN, salt=salt, iterations=10_000
).derive(KEY)

dec = Cipher(ALGO(derived), modes.CBC(iv)).decryptor()
padded = dec.update(ct) + dec.finalize()
unpadder = PKCS7(ALGO.block_size).unpadder()
plaintext = unpadder.update(padded) + unpadder.finalize()
print(plaintext.decode("utf-8"))
```

## Reference decoder — Python (AEAD: AES-GCM, ChaCha20-Poly1305)

```python
import base64
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.ciphers.aead import AESGCM, ChaCha20Poly1305
from cryptography.hazmat.primitives.kdf.pbkdf2 import PBKDF2HMAC

# --- parameters: change to match the UiPath activity ---
KEY        = "your-key-string".encode("utf-8")
CIPHER_B64 = "..."  # output of EncryptText
USE_AESGCM = True   # False => ChaCha20-Poly1305

raw = base64.b64decode(CIPHER_B64)
SALT_LEN, IV_LEN, TAG_LEN = 8, 12, 16
salt = raw[:SALT_LEN]
iv   = raw[SALT_LEN:SALT_LEN + IV_LEN]
tag  = raw[-TAG_LEN:]
ct   = raw[SALT_LEN + IV_LEN : -TAG_LEN]

derived = PBKDF2HMAC(
    algorithm=hashes.SHA1(), length=32, salt=salt, iterations=10_000
).derive(KEY)

aead = AESGCM(derived) if USE_AESGCM else ChaCha20Poly1305(derived)
plaintext = aead.decrypt(iv, ct + tag, None)  # cryptography expects ct||tag
print(plaintext.decode("utf-8"))
```

## Reference encoder — Python (non-AEAD)

```python
import base64, os
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
from cryptography.hazmat.primitives.kdf.pbkdf2 import PBKDF2HMAC
from cryptography.hazmat.primitives.padding import PKCS7

ALGO    = algorithms.TripleDES
KEY_LEN = 24
IV_LEN  = 8
KEY     = "your-key-string".encode("utf-8")
PLAIN   = "hello".encode("utf-8")

salt = os.urandom(8)
iv   = os.urandom(IV_LEN)
derived = PBKDF2HMAC(
    algorithm=hashes.SHA1(), length=KEY_LEN, salt=salt, iterations=10_000
).derive(KEY)

padder = PKCS7(ALGO.block_size).padder()
padded = padder.update(PLAIN) + padder.finalize()
enc    = Cipher(ALGO(derived), modes.CBC(iv)).encryptor()
ct     = enc.update(padded) + enc.finalize()

print(base64.b64encode(salt + iv + ct).decode("ascii"))
```

## When you see "Decryption failed" in DecryptText / DecryptFile

The activity raises a `CryptographicException` with one of two hints when the
input cannot be parsed:

- **"too short to be in UiPath wire format"** — the input did not contain
  enough bytes for `salt + IV`. The input is almost certainly from a tool
  that does not embed `salt + IV` in its output, or it has been truncated.
- **"this commonly indicates the input was produced by a different tool"** —
  parsing succeeded, but the derived key did not unpad the ciphertext. The
  most common causes are (a) the input came from a different tool with a
  different wire format, (b) the key string or its encoding does not match
  what was used to encrypt, or (c) the algorithm selected does not match
  what was used to encrypt.

Use the reference decoders above to verify that your input is in the UiPath
wire format with the correct key.
