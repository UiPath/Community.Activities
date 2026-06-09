# UiPath.Cryptography symmetric ciphertext wire format

This document describes the on-the-wire format produced by `EncryptText` /
`EncryptFile` and consumed by `DecryptText` / `DecryptFile` for **symmetric**
algorithms (`AES`, `AESGCM`, `ChaCha20Poly1305`, `DES`, `RC2`, `Rijndael`,
`TripleDES`). PGP is out of scope — it has its own format defined by RFC 9580.

Each symmetric activity has a `Format` property selecting one of four wire
formats:

| Format        | KDF                              | Key input                 | Wire layout                                              | Iterop with external tools                 |
| ------------- | -------------------------------- | ------------------------- | -------------------------------------------------------- | ------------------------------------------ |
| `Classic`     | PBKDF2-HMAC-SHA1 @ **10 000**    | Password (Encoding bytes) | `salt(8) ‖ IV ‖ ct [‖ tag]`                              | None — UiPath-specific, backward-compat.   |
| `Owasp2026`   | PBKDF2-HMAC-SHA1 @ caller-set    | Password (Encoding bytes) | `salt(8) ‖ IV ‖ ct [‖ tag]` (same as Classic)            | None — UiPath layout with OWASP 2026 KDF.  |
| `Raw`         | none                             | Raw bytes (Hex or Base64) | `IV ‖ ct [‖ tag]`                                        | Yes — e.g. `openssl enc -K <hex> -iv <hex>`, Python `cryptography`, Java `javax.crypto`. |
| `OpenSslEnc`  | PBKDF2-HMAC-SHA256 @ caller-set  | Password (Encoding bytes) | `Salted__(8) ‖ salt(8) ‖ ct [‖ tag]`                     | Yes — `openssl enc -pbkdf2 -iter <N> -md sha256 -salt -k <pw>`. |

**Default is `Classic`.** Existing workflows with no `Format` property set
behave byte-identically to every prior release of this package. Tracked in
[STUD-64429](https://uipath.atlassian.net/browse/STUD-64429).

> **AEAD over `OpenSslEnc` is a UiPath extension, not a cross-tool standard.**
> `openssl enc` does not officially support GCM/Poly1305 modes; combine
> `AESGCM` / `ChaCha20Poly1305` with `OpenSslEnc` only when both producer and
> consumer are UiPath.

## Choosing a format

| Situation                                                                              | Use            |
| -------------------------------------------------------------------------------------- | -------------- |
| Reading or producing ciphertext that any older UiPath release will need to decrypt     | `Classic`      |
| New UiPath-to-UiPath workflow, want current-best-practice security                     | `Owasp2026`    |
| Peer is `openssl enc -K <hex> -iv <hex>`, Python `cryptography`, Java `javax.crypto`   | `Raw`          |
| Peer is `openssl enc -pbkdf2 -k <password>` (or any tool that emits that layout)       | `OpenSslEnc`   |

## Activity properties added with the four-format feature

| Property         | Type                  | Notes                                                                                                                          |
| ---------------- | --------------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| `Format`         | `SymmetricWireFormat` | Enum, default `Classic`. Selects which of the four wire formats below.                                                         |
| `KeyFormat`      | `KeyBytesFormat`      | Enum, default `Encoded`. Required `Hex` or `Base64` when `Format = Raw`; rejected otherwise.                                   |
| `Iv`             | `InArgument<string>`  | Optional, only valid when `Format = Raw`. Parsed via `KeyFormat`. Empty → random IV generated.                                 |
| `KdfIterations`  | `InArgument<int>`     | Auto-fills with the format's shipped iteration count when `Format` is set to `Owasp2026` or `OpenSslEnc`. Override only for cross-tool interop. Minimum 1 000. Hidden / not used for `Classic` and `Raw`. |

---

## Classic (default)

Wire layout, per-algorithm sizes, and KDF parameters are fixed at the
historical UiPath values. **No changes are accepted to this format**:
backward compatibility with ciphertext produced by every previous release of
the package is non-negotiable.

**Choose `Classic` when** you need to decrypt existing UiPath ciphertext, or
when you need to keep producing ciphertext that older UiPath builds can
decrypt without changes.

```
+-------------------+--------------------+----------------------+--------------------+
|       salt        |         IV         |      ciphertext      |  authentication    |
|     (8 bytes)     |     (block size)   |     (variable)       |   tag (AEAD only)  |
+-------------------+--------------------+----------------------+--------------------+
```

`EncryptText` Base64-encodes the result; `EncryptFile` writes raw bytes.

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

### KDF — Classic

| Parameter     | Value                                              |
| ------------- | -------------------------------------------------- |
| Function      | PBKDF2-HMAC-SHA1 (`Rfc2898DeriveBytes` default)    |
| Iterations    | **10 000** (fixed)                                 |
| Salt          | 8 random bytes per call                            |
| Output length | Maximum legal key size of the algorithm, in bytes  |

> **Security note:** 10 000 PBKDF2-SHA1 iterations is the *floor* of
> acceptable values by current OWASP / NIST guidance, kept here only for
> backward compatibility. For new workflows use `Owasp2026` or `OpenSslEnc` with
> their OWASP-recommended defaults.

### Reference decoder — Python (Classic, non-AEAD)

```python
import base64
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
from cryptography.hazmat.primitives.kdf.pbkdf2 import PBKDF2HMAC
from cryptography.hazmat.primitives.padding import PKCS7

ALGO     = algorithms.TripleDES   # AES / TripleDES / ...
KEY_LEN  = 24                     # see per-algorithm table
IV_LEN   = 8                      # 8 for TripleDES/DES/RC2, 16 for AES/Rijndael
KEY      = "your-key-string".encode("utf-8")
CIPHER_B64 = "..."                # output of EncryptText

raw  = base64.b64decode(CIPHER_B64)
salt, iv, ct = raw[:8], raw[8:8+IV_LEN], raw[8+IV_LEN:]
derived = PBKDF2HMAC(algorithm=hashes.SHA1(), length=KEY_LEN, salt=salt, iterations=10_000).derive(KEY)

dec = Cipher(ALGO(derived), modes.CBC(iv)).decryptor()
padded = dec.update(ct) + dec.finalize()
unpadder = PKCS7(ALGO.block_size).unpadder()
print((unpadder.update(padded) + unpadder.finalize()).decode("utf-8"))
```

### Reference decoder — Python (Classic, AEAD)

```python
import base64
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.ciphers.aead import AESGCM, ChaCha20Poly1305
from cryptography.hazmat.primitives.kdf.pbkdf2 import PBKDF2HMAC

KEY        = "your-key-string".encode("utf-8")
CIPHER_B64 = "..."
USE_AESGCM = True   # False => ChaCha20-Poly1305

raw = base64.b64decode(CIPHER_B64)
SALT_LEN, IV_LEN, TAG_LEN = 8, 12, 16
salt, iv, tag = raw[:8], raw[8:20], raw[-16:]
ct = raw[20:-16]
derived = PBKDF2HMAC(algorithm=hashes.SHA1(), length=32, salt=salt, iterations=10_000).derive(KEY)

aead = AESGCM(derived) if USE_AESGCM else ChaCha20Poly1305(derived)
print(aead.decrypt(iv, ct + tag, None).decode("utf-8"))
```

---

## Owasp2026 (UiPath layout, OWASP's 2026 guidance)

Identical wire layout to `Classic` — only the PBKDF2 iteration count changes.
`KdfIterations` auto-fills with OWASP's 2026 password-storage recommendation
(**1 300 000** for PBKDF2-HMAC-SHA1) when this format is selected. Override
to any value ≥ 1 000.

The year-suffix is intentional. `Owasp2026` is a **snapshot**, not a moving
target: when OWASP revises its guidance, a new enum value such as
`Owasp2030` will be added to `SymmetricWireFormat` with the updated default,
and `Owasp2026` will keep its defaults byte-stable forever. Users migrating
across UiPath releases pin a year explicitly; they never wake up to changed
ciphertext just because the package was upgraded.

**Choose `Owasp2026` when** both endpoints are UiPath and you want
current-best-practice security. Classic and Owasp2026 produce the same wire
format, so an Owasp2026 blob encrypted with `KdfIterations = 10_000` is
byte-identical to a Classic blob — and decryption will succeed regardless of
which Format is selected on the consumer, as long as the iteration count
matches.

The Python reference decoder above works unchanged; substitute
`iterations=<your-value>` for the `10_000` literal.

---

## Raw (caller-supplied key and IV)

No KDF. Caller supplies raw cipher key bytes via `Hex` or `Base64`. IV is
prepended to the stream (random if not supplied, otherwise the caller's
value).

**Choose `Raw` when** the peer is a tool that takes a literal cipher key
(rather than a password): `openssl enc -K <hex> -iv <hex>`, Python's
`cryptography` library, Java's `javax.crypto`, browser SubtleCrypto, etc.
Also the right choice when the key comes from a key-management system and
you don't want a KDF in the loop.

> **⚠ NEVER reuse the same `(Key, IV)` pair across encryptions.**
>
> Nonce reuse with the same key is catastrophic in `Raw` mode:
>
> - **All modes (CTR-derived, including CBC and AEAD):** identical keystream
>   on both messages → `C₁ ⊕ C₂ = P₁ ⊕ P₂` (the classic two-time-pad
>   disaster). Plaintexts with any structure are usually recoverable from
>   the XOR.
> - **AEAD (`AESGCM`, `ChaCha20Poly1305`) only — much worse:** a single
>   nonce collision lets an attacker solve a polynomial over GF(2¹²⁸) and
>   recover the GHASH/Poly1305 authentication subkey `H`. The subkey is
>   derived from the cipher key alone (the nonce never touches it), so once
>   `H` is recovered the attacker can forge a valid tag for **any** message
>   under that key — including messages with fresh, never-reused nonces.
>   The key is permanently compromised for authentication.
>
> The safe default: leave the `Iv` property empty so the cipher generates a
> cryptographically random IV per call. Supply an explicit IV only when the
> peer protocol mandates it, and ensure your producer guarantees nonce
> uniqueness (e.g. an atomic counter persisted across runs, or a 96-bit
> random nonce with documented birthday-bound analysis for your message
> volume).
>
> The `EncryptText` and `EncryptFile` activities raise a design-time
> warning when the `Iv` property is bound. The warning is informational —
> Studio will not block the workflow — but is meant to surface the
> constraint before the ciphertext ever leaves the machine.

Layout:

```
+----------------+----------------------+-------------------+
|       IV       |      ciphertext      |   tag (AEAD only) |
|  (block size)  |     (variable)       |    16 bytes       |
+----------------+----------------------+-------------------+
```

This is what `openssl enc -aes-256-cbc -K <hex> -iv <hex>` outputs, what
Python `cryptography` produces by default for `Cipher(...).encryptor()`, and
what Java `javax.crypto.Cipher` produces with a `SecretKeySpec` and explicit
`IvParameterSpec`.

### openssl interop

```bash
# Encrypt with openssl, decrypt in UiPath (Format = Raw, KeyFormat = Hex)
openssl rand -hex 32 > key.hex
openssl rand -hex 16 > iv.hex
openssl enc -aes-256-cbc -K "$(cat key.hex)" -iv "$(cat iv.hex)" -in plain.txt -out cipher.bin
# Concatenate IV + ciphertext to match UiPath Raw layout:
xxd -r -p iv.hex > stream.bin && cat cipher.bin >> stream.bin
# Base64-encode for DecryptText, or pass stream.bin to DecryptFile.

# Encrypt in UiPath (Format = Raw, Algorithm = AES, KeyFormat = Hex), decrypt with openssl
# UiPath output = base64(IV‖ct). Decode and split:
base64 -d < uipath-output.txt > stream.bin
head -c 16 stream.bin > iv.bin
tail -c +17 stream.bin > cipher.bin
openssl enc -aes-256-cbc -d -K "$(cat key.hex)" -iv "$(xxd -p iv.bin | tr -d '\n')" -in cipher.bin
```

### Reference decoder — Python (Raw, AEAD)

```python
import base64
from cryptography.hazmat.primitives.ciphers.aead import AESGCM

KEY        = bytes.fromhex("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F")
CIPHER_B64 = "..."   # output of EncryptText with Format=Raw

raw = base64.b64decode(CIPHER_B64)
iv, ct_and_tag = raw[:12], raw[12:]
print(AESGCM(KEY).decrypt(iv, ct_and_tag, None).decode("utf-8"))
```

### Reference encoder — Python (Raw, AEAD)

```python
import base64, os
from cryptography.hazmat.primitives.ciphers.aead import AESGCM

KEY   = bytes.fromhex("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F")
PLAIN = "hello".encode("utf-8")

iv = os.urandom(12)
ct_and_tag = AESGCM(KEY).encrypt(iv, PLAIN, None)
print(base64.b64encode(iv + ct_and_tag).decode("ascii"))
```

---

## OpenSslEnc (openssl `enc` compatibility)

**Choose `OpenSslEnc` when** the peer is `openssl enc -pbkdf2 -k <password>`
or any tool that emits the same layout. Match the peer's `-iter <N>` value
via `KdfIterations`.

Wire layout matches the standard openssl 1.1.1+ output for `enc -pbkdf2 -salt`:

```
+----------------+-------------+----------------------+-------------------+
|   Salted__     |    salt     |      ciphertext      |   tag (AEAD only) |
|   (8 bytes)    |  (8 bytes)  |     (variable)       |    16 bytes       |
+----------------+-------------+----------------------+-------------------+
```

`Salted__` is the literal ASCII string `S a l t e d _ _`. The salt is 8
random bytes. PBKDF2-HMAC-SHA256 is run against the password to produce a
`key ‖ iv` block — the key is the first N bytes (algorithm key size), the IV
is the next M bytes (algorithm IV size). There is no separate IV in the
stream; both key and IV come from the KDF.

### KDF — OpenSslEnc

| Parameter     | Value                                                  |
| ------------- | ------------------------------------------------------ |
| Function      | PBKDF2-HMAC-SHA256                                     |
| Iterations    | **600 000** (default — OWASP 2026 value for SHA-256)   |
| Salt          | 8 random bytes per call                                |
| Output length | algorithm key size + IV size                           |

> openssl's *own* default is 10 000 iterations, kept by the openssl project
> for backward compatibility. UiPath defaults to the OWASP 2026 value
> instead. When interoperating with `openssl enc -iter <N>`, set
> `KdfIterations` to match `<N>`. Unlike `Owasp2026`, this format's default
> is *not* pinned to a year by name — future releases may revise the
> default. Pin `KdfIterations` explicitly if cross-version stability matters.

### openssl interop

```bash
# openssl encrypts, UiPath DecryptFile reads (Format = OpenSslEnc, KdfIterations = 600000)
openssl enc -aes-256-cbc -pbkdf2 -iter 600000 -md sha256 -salt -k "your-password" \
    -in plain.txt -out cipher.bin

# UiPath encrypts, openssl reads (matched iter count)
# DecryptText with Format = OpenSslEnc and the same password unpacks an openssl-produced blob.
openssl enc -aes-256-cbc -pbkdf2 -iter 600000 -md sha256 -d -k "your-password" -in cipher.bin
```

### Reference decoder — Python (OpenSslEnc, non-AEAD)

```python
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
from cryptography.hazmat.primitives.kdf.pbkdf2 import PBKDF2HMAC
from cryptography.hazmat.primitives.padding import PKCS7

with open("cipher.bin", "rb") as f: raw = f.read()
assert raw[:8] == b"Salted__"
salt = raw[8:16]
ct   = raw[16:]

KEY_LEN, IV_LEN, ITER = 32, 16, 600_000
derived = PBKDF2HMAC(algorithm=hashes.SHA256(), length=KEY_LEN + IV_LEN,
                    salt=salt, iterations=ITER).derive(b"your-password")
key, iv = derived[:KEY_LEN], derived[KEY_LEN:]

dec = Cipher(algorithms.AES(key), modes.CBC(iv)).decryptor()
padded = dec.update(ct) + dec.finalize()
unpadder = PKCS7(algorithms.AES.block_size).unpadder()
print((unpadder.update(padded) + unpadder.finalize()).decode("utf-8"))
```

---

## KDF iterations and the `KdfIterations` property

`KdfIterations` is exposed only for `Owasp2026` and `OpenSslEnc`. The field
auto-fills with the format's shipped value when you change `Format` (no
sentinel to memorise — what you see is what runs):

| Format       | KDF                | Auto-filled value | Minimum when overridden             |
| ------------ | ------------------ | ----------------- | ----------------------------------- |
| `Classic`    | PBKDF2-HMAC-SHA1   | 10 000 (fixed)    | property hidden, override rejected  |
| `Owasp2026`  | PBKDF2-HMAC-SHA1   | 1 300 000         | 1 000 (NIST SP 800-132 floor)       |
| `Raw`        | none               | n/a               | property hidden                     |
| `OpenSslEnc` | PBKDF2-HMAC-SHA256 | 600 000           | 1 000                               |

**The iteration count is not stored in the wire format.** Encrypt and decrypt
sides must agree externally. In particular:

- An `Owasp2026` blob produced with `KdfIterations = 1_300_000` will fail
  to decrypt with `KdfIterations = 10_000`, even though the wire layout is
  identical. Cross-version-decrypt scenarios should set `KdfIterations`
  explicitly on both sides.
- When OWASP revises its recommendation, the new defaults land as a new
  enum value (e.g. `Owasp2030`); `Owasp2026` keeps its values forever.
  Workflows that pin a specific year are immune to package upgrades.

## Validation matrix

The activity rejects inconsistent property combinations at runtime (wrapped
as `InvalidOperationException` with a localized message):

| Constraint                                                       | Cause                                  |
| ---------------------------------------------------------------- | -------------------------------------- |
| `Format = Raw` + `KeyFormat = Encoded`                           | Raw mode requires literal bytes (Hex or Base64). |
| `Format ∈ {Classic, Owasp2026, OpenSslEnc}` + `KeyFormat ≠ Encoded` | Password-based formats use Encoding.   |
| `Iv` set + `Format ≠ Raw`                                        | Other formats embed IV in the stream.  |
| `KdfIterations ≠ 0` + `Format ∈ {Classic, Raw}`                  | Classic's iteration count is fixed; Raw has no KDF. |
| `KdfIterations < 0`, or `0 < KdfIterations < 1000`               | Below NIST floor. Only `0` (the sentinel for "use the format's shipped value") is accepted under the floor. |
| `Format = Raw` + raw key length not legal for the algorithm      | E.g. AES needs 16/24/32 bytes.         |

## Obsolete algorithms

`AES`, `DES`, `RC2`, `Rijndael`, and `TripleDES` are marked `[Obsolete]` in
the `EncryptionAlgorithm` enum (the compiler raises CS0618). They remain
fully functional in every format — including for *encryption*, not just
decryption — to support customers whose legacy systems require interop with
e.g. TripleDES. The `[Obsolete]` warning is the user-facing security signal;
the activity does not impose a second runtime gate.

## When you see "Decryption failed" in DecryptText / DecryptFile

The activity raises a `CryptographicException` with a hint when the input
cannot be parsed:

- **"too short to be in UiPath wire format"** — input is shorter than the
  minimum prefix (salt+IV for Classic/Owasp2026, IV for Raw, Salted__+salt for
  OpenSslEnc). Almost certainly produced by a tool with a different layout
  or truncated in transit.
- **"does not start with the openssl 'Salted__' magic prefix"** — input is
  not in OpenSslEnc format. Check the producer used `openssl enc -salt` and
  the bytes were not transcoded.
- **"this commonly indicates the input was produced by a different tool"** —
  parsing succeeded but padding/tag verification failed. Common causes:
  wrong Format selected, wrong key/encoding, wrong algorithm, or wrong
  `KdfIterations` for Owasp2026/OpenSslEnc.

Use the reference decoders above to verify your input matches the declared
format.
