using System.Security;
using UiPath.Cryptography.Enums;

namespace UiPath.Cryptography.Activities.API
{
    /// <summary>
    /// Provides cryptography capabilities for coded workflows.
    /// </summary>
    /// <remarks>
    /// <para><b>API shape — Bytes / Text / File matrix</b></para>
    /// <para>
    /// Every logical operation (symmetric encrypt/decrypt, keyed hash, PGP encrypt/decrypt/sign/clearsign/verify)
    /// exposes three input/output forms so callers can use the one that matches the data they already have:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Bytes form</b> — base method name. Bytes in, bytes out.</description></item>
    /// <item><description><b>Text form</b> — <c>...Text</c> suffix. String in, string out (Base64 for symmetric, ASCII-armored for PGP).</description></item>
    /// <item><description><b>File form</b> — <c>...File</c> suffix. File-path in, file-path out.</description></item>
    /// </list>
    /// <para><b>Symmetric wire format</b></para>
    /// <para>
    /// All symmetric methods default to <see cref="SymmetricWireFormat.Classic"/> — the byte-stable
    /// UiPath layout (<c>salt(8) || IV || ct [|| tag(16)]</c>, PBKDF2-HMAC-SHA1 @ 10 000 iterations).
    /// Pass <see cref="SymmetricEncryptOptions"/> / <see cref="SymmetricDecryptOptions"/> to opt into
    /// <see cref="SymmetricWireFormat.Owasp2026"/> (modern KDF iterations),
    /// <see cref="SymmetricWireFormat.Raw"/> (caller-supplied key + IV, third-party interop), or
    /// <see cref="SymmetricWireFormat.OpenSslEnc"/> (<c>openssl enc</c>-compatible). See
    /// <c>docs/symmetric-wire-format.md</c> for the byte layouts.
    /// </para>
    /// <para><b>Key material</b></para>
    /// <para>
    /// Symmetric methods take a <see cref="SymmetricEncryptOptions"/> / <see cref="SymmetricDecryptOptions"/>
    /// object that carries the key together with the wire format. The format factories accept
    /// either a <see cref="PasswordKey"/> (for the PBKDF2-based formats Classic, Owasp2026,
    /// OpenSslEnc) or a <see cref="RawKey"/> (for <see cref="SymmetricWireFormat.Raw"/>), so
    /// mismatched (key kind × format) pairings fail at compile time. Construct keys via
    /// <see cref="PasswordKey.FromPassword(string, System.Text.Encoding)"/> /
    /// <see cref="PasswordKey.FromPassword(SecureString, System.Text.Encoding)"/> /
    /// <see cref="RawKey.FromBytes"/> / <see cref="RawKey.FromHex"/> /
    /// <see cref="RawKey.FromBase64"/>.
    /// </para>
    /// <para>
    /// Keyed-hash methods take a <see cref="CryptoKey"/> directly — they have no wire-format
    /// axis so no options object is required.
    /// </para>
    /// <para><b>PGP keys</b></para>
    /// <para>
    /// PGP methods take <see cref="PgpPublicKey"/> / <see cref="PgpPrivateKey"/> handles. Construct
    /// them from in-memory bytes or a file path via the factory methods. The private key carries
    /// its own passphrase, bound at construction. Passing a <see cref="PgpPrivateKey"/> to an encrypt
    /// method implies signing; passing a <see cref="PgpPublicKey"/> to a decrypt method implies
    /// signature verification — separate <c>bool sign</c> / <c>bool verifySignature</c> flags are
    /// not used.
    /// </para>
    /// </remarks>
    public interface ICryptographyService
    {
        // ── Symmetric ─────────────────────────────────────────────────────────

        /// <summary>
        /// Encrypts arbitrary bytes using the algorithm and wire format selected on
        /// <paramref name="options"/>. Returns the ciphertext — exact byte layout depends on
        /// the format (see <see cref="SymmetricWireFormat"/>).
        /// </summary>
        byte[] EncryptBytes(byte[] input, EncryptionAlgorithm algorithm, SymmetricEncryptOptions options);

        /// <summary>
        /// Decrypts ciphertext produced by <see cref="EncryptBytes"/>. The wire format on
        /// <paramref name="options"/> must match the format used at encrypt time; mismatches
        /// surface as <see cref="System.Security.Cryptography.CryptographicException"/>
        /// (AEAD tag failure) or garbled output (CBC).
        /// </summary>
        byte[] DecryptBytes(byte[] input, EncryptionAlgorithm algorithm, SymmetricDecryptOptions options);

        /// <summary>
        /// Encrypts a string and returns the ciphertext as Base64. The input string is always
        /// transcoded to bytes via <see cref="System.Text.Encoding.UTF8"/> — there is no
        /// encoding parameter. For non-UTF-8 text or for explicit encoding control, transcode
        /// to bytes at the call site and use <see cref="EncryptBytes"/> instead.
        /// </summary>
        string EncryptText(string input, EncryptionAlgorithm algorithm, SymmetricEncryptOptions options);

        /// <summary>
        /// Decrypts a Base64-encoded ciphertext and returns the plaintext as a UTF-8 string.
        /// The plaintext bytes are always decoded via <see cref="System.Text.Encoding.UTF8"/>
        /// — there is no encoding parameter. For non-UTF-8 text or for explicit encoding
        /// control, use <see cref="DecryptBytes"/> and decode at the call site.
        /// </summary>
        string DecryptText(string input, EncryptionAlgorithm algorithm, SymmetricDecryptOptions options);

        /// <summary>
        /// Reads <paramref name="inputPath"/>, encrypts the contents, and writes the
        /// ciphertext to <paramref name="outputPath"/>. Throws <see cref="System.InvalidOperationException"/>
        /// if the output file exists and <paramref name="overwrite"/> is false.
        /// </summary>
        void EncryptFile(string inputPath, string outputPath, EncryptionAlgorithm algorithm, SymmetricEncryptOptions options, bool overwrite = false);

        /// <summary>
        /// Reads an encrypted file from <paramref name="inputPath"/>, decrypts the contents,
        /// and writes the plaintext to <paramref name="outputPath"/>. Throws
        /// <see cref="System.InvalidOperationException"/> if the output file exists and
        /// <paramref name="overwrite"/> is false.
        /// </summary>
        void DecryptFile(string inputPath, string outputPath, EncryptionAlgorithm algorithm, SymmetricDecryptOptions options, bool overwrite = false);

        // ── Keyed hash ────────────────────────────────────────────────────────

        /// <summary>
        /// Computes a keyed hash (HMAC for the HMAC* algorithms; plain hash with the key
        /// ignored for the SHA*/MD5 algorithms) over <paramref name="input"/> and returns
        /// the digest as an uppercase hex string.
        /// </summary>
        string KeyedHashBytes(byte[] input, KeyedHashAlgorithms algorithm, CryptoKey key);

        /// <summary>
        /// Computes a keyed hash over the UTF-8 bytes of <paramref name="input"/> and returns
        /// the digest as an uppercase hex string.
        /// </summary>
        string KeyedHashText(string input, KeyedHashAlgorithms algorithm, CryptoKey key);

        /// <summary>
        /// Reads the file at <paramref name="inputPath"/> and computes a keyed hash over its
        /// bytes, returning the digest as an uppercase hex string.
        /// </summary>
        string KeyedHashFile(string inputPath, KeyedHashAlgorithms algorithm, CryptoKey key);

        // ── PGP encrypt (signer is optional — when supplied, output is encrypted-and-signed) ─

        /// <summary>
        /// PGP-encrypts <paramref name="input"/> for the holder of <paramref name="recipient"/>.
        /// When <paramref name="signer"/> is supplied, the output is also signed with that
        /// private key — the recipient can verify by passing the matching public key as the
        /// <c>verifier</c> on <see cref="PgpDecryptBytes"/>.
        /// </summary>
        byte[] PgpEncryptBytes(byte[] input, PgpPublicKey recipient, PgpPrivateKey signer = null);

        /// <summary>
        /// PGP-encrypts <paramref name="input"/> and returns ASCII-armored ciphertext
        /// (the <c>-----BEGIN PGP MESSAGE-----</c> / <c>-----END PGP MESSAGE-----</c> envelope).
        /// When <paramref name="signer"/> is supplied, the output is also signed.
        /// </summary>
        string PgpEncryptText(string input, PgpPublicKey recipient, PgpPrivateKey signer = null);

        /// <summary>
        /// Reads <paramref name="inputPath"/>, PGP-encrypts the contents for
        /// <paramref name="recipient"/>, and writes the result to <paramref name="outputPath"/>.
        /// When <paramref name="signer"/> is supplied, the output is also signed.
        /// </summary>
        void PgpEncryptFile(string inputPath, string outputPath, PgpPublicKey recipient, PgpPrivateKey signer = null, bool overwrite = false);

        // ── PGP decrypt (verifier is optional — when supplied, signature is checked too) ─────

        /// <summary>
        /// Decrypts a PGP-encrypted payload using <paramref name="recipient"/>'s private key
        /// (the passphrase is bound to the key at construction). When <paramref name="verifier"/>
        /// is supplied, the embedded signature is verified against that public key;
        /// verification failure throws <see cref="System.InvalidOperationException"/>.
        /// </summary>
        byte[] PgpDecryptBytes(byte[] input, PgpPrivateKey recipient, PgpPublicKey verifier = null);

        /// <summary>
        /// Decrypts an ASCII-armored PGP message using <paramref name="recipient"/>'s private
        /// key. When <paramref name="verifier"/> is supplied, the embedded signature is verified.
        /// </summary>
        string PgpDecryptText(string input, PgpPrivateKey recipient, PgpPublicKey verifier = null);

        /// <summary>
        /// Reads a PGP-encrypted file at <paramref name="inputPath"/>, decrypts it using
        /// <paramref name="recipient"/>, and writes the plaintext to <paramref name="outputPath"/>.
        /// When <paramref name="verifier"/> is supplied, the embedded signature is verified.
        /// </summary>
        void PgpDecryptFile(string inputPath, string outputPath, PgpPrivateKey recipient, PgpPublicKey verifier = null, bool overwrite = false);

        // ── PGP sign (binary) ─────────────────────────────────────────────────

        /// <summary>
        /// Produces a PGP binary signature over <paramref name="input"/> using
        /// <paramref name="signer"/>. The returned payload combines the signature with the
        /// signed content; verify with <see cref="PgpVerifyBytes"/>.
        /// </summary>
        byte[] PgpSignBytes(byte[] input, PgpPrivateKey signer);

        /// <summary>
        /// Produces a PGP binary signature over the UTF-8 bytes of <paramref name="input"/>,
        /// returned as ASCII-armored text. Verify with <see cref="PgpVerifyText"/>.
        /// </summary>
        string PgpSignText(string input, PgpPrivateKey signer);

        /// <summary>
        /// Reads <paramref name="inputPath"/>, produces a PGP binary signature, and writes
        /// the signed payload to <paramref name="outputPath"/>. Verify with
        /// <see cref="PgpVerifyFile"/>.
        /// </summary>
        void PgpSignFile(string inputPath, string outputPath, PgpPrivateKey signer, bool overwrite = false);

        // ── PGP clear-sign (text) ─────────────────────────────────────────────

        /// <summary>
        /// Produces a PGP clear-text signature over <paramref name="input"/>. The original
        /// content remains human-readable, wrapped between
        /// <c>-----BEGIN PGP SIGNED MESSAGE-----</c> and <c>-----END PGP SIGNATURE-----</c>;
        /// verify with <see cref="PgpVerifyClearSignedBytes"/>.
        /// </summary>
        byte[] PgpClearSignBytes(byte[] input, PgpPrivateKey signer);

        /// <summary>
        /// Produces a PGP clear-text signature over <paramref name="input"/> and returns the
        /// ASCII-armored clear-signed envelope. Verify with <see cref="PgpVerifyClearSignedText"/>.
        /// </summary>
        string PgpClearSignText(string input, PgpPrivateKey signer);

        /// <summary>
        /// Reads <paramref name="inputPath"/> (treated as text), produces a PGP clear-text
        /// signature, and writes the signed envelope to <paramref name="outputPath"/>. Verify
        /// with <see cref="PgpVerifyClearSignedFile"/>.
        /// </summary>
        void PgpClearSignFile(string inputPath, string outputPath, PgpPrivateKey signer, bool overwrite = false);

        // ── PGP verify (binary signature) ─────────────────────────────────────

        /// <summary>
        /// Verifies a PGP binary signature against <paramref name="verifier"/>. Returns
        /// <c>true</c> when the signature is valid and the public key matches the signer;
        /// returns <c>false</c> on signature mismatch or tampered content.
        /// </summary>
        bool PgpVerifyBytes(byte[] input, PgpPublicKey verifier);

        /// <summary>
        /// Verifies an ASCII-armored PGP binary signature against <paramref name="verifier"/>.
        /// Returns <c>true</c> when valid; <c>false</c> on mismatch.
        /// </summary>
        bool PgpVerifyText(string input, PgpPublicKey verifier);

        /// <summary>
        /// Reads a PGP-signed file at <paramref name="inputPath"/> and verifies the binary
        /// signature against <paramref name="verifier"/>. Returns <c>true</c> when valid.
        /// </summary>
        bool PgpVerifyFile(string inputPath, PgpPublicKey verifier);

        // ── PGP verify (clearsignature) ───────────────────────────────────────

        /// <summary>
        /// Verifies a PGP clear-text signature against <paramref name="verifier"/>. Returns
        /// <c>true</c> when the embedded signature is valid; <c>false</c> on mismatch or
        /// tampered content inside the clear-signed envelope.
        /// </summary>
        bool PgpVerifyClearSignedBytes(byte[] input, PgpPublicKey verifier);

        /// <summary>
        /// Verifies a PGP clear-text signature carried in <paramref name="input"/> (ASCII
        /// armored) against <paramref name="verifier"/>. Returns <c>true</c> when valid.
        /// </summary>
        bool PgpVerifyClearSignedText(string input, PgpPublicKey verifier);

        /// <summary>
        /// Reads a clear-signed file at <paramref name="inputPath"/> and verifies the
        /// embedded signature against <paramref name="verifier"/>. Returns <c>true</c> when
        /// valid.
        /// </summary>
        bool PgpVerifyClearSignedFile(string inputPath, PgpPublicKey verifier);

        // ── PGP key generation ────────────────────────────────────────────────

        /// <summary>
        /// Generates an OpenPGP RSA key pair in memory. The public and private halves
        /// returned in the <see cref="PgpKeyPair"/> are mathematically tied — call
        /// <see cref="PgpPublicKey.Save"/> / <see cref="PgpPrivateKey.Save"/> to persist.
        /// </summary>
        /// <param name="userId">OpenPGP User ID, conventionally an RFC 2822 mailbox such as <c>"Alice Doe &lt;alice@example.com&gt;"</c>.</param>
        /// <param name="passphrase">Passphrase that will protect the generated private key. Bound to the returned <see cref="PgpPrivateKey"/>.</param>
        /// <param name="keySize">RSA modulus size; defaults to 4096 bits.</param>
        PgpKeyPair PgpGenerateKeys(string userId, string passphrase, RsaKeySize keySize = RsaKeySize.Rsa4096);

        /// <summary>
        /// Generates an OpenPGP RSA key pair in memory with a <see cref="SecureString"/>
        /// passphrase. Same semantics as the <c>string</c> overload; see that method's docs
        /// for the key-pair atomicity contract.
        /// </summary>
        PgpKeyPair PgpGenerateKeys(string userId, SecureString passphrase, RsaKeySize keySize = RsaKeySize.Rsa4096);

        /// <summary>
        /// Verifies that the supplied <see cref="PgpPublicKey"/> is a well-formed OpenPGP public key.
        /// Returns <c>true</c> when parsing succeeds; <c>false</c> when the bytes are
        /// malformed or not an OpenPGP key.
        /// </summary>
        bool PgpVerifyPublicKey(PgpPublicKey key);
    }
}
