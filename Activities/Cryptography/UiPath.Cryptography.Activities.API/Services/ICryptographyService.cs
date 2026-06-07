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
    /// Symmetric and keyed-hash methods take a <see cref="CryptoKey"/>. Use
    /// <see cref="CryptoKey.FromPassword(string, System.Text.Encoding)"/> /
    /// <see cref="CryptoKey.FromPassword(SecureString, System.Text.Encoding)"/> for the PBKDF2-based
    /// formats (Classic, Owasp2026, OpenSslEnc), or <see cref="CryptoKey.FromRawBytes"/> /
    /// <see cref="CryptoKey.FromHexString"/> / <see cref="CryptoKey.FromBase64String"/> for
    /// <see cref="SymmetricWireFormat.Raw"/>.
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

        byte[] EncryptBytes(byte[] input, EncryptionAlgorithm algorithm, CryptoKey key, SymmetricEncryptOptions options = null);

        byte[] DecryptBytes(byte[] input, EncryptionAlgorithm algorithm, CryptoKey key, SymmetricDecryptOptions options = null);

        /// <summary>
        /// Encrypts a string and returns the ciphertext as Base64. The input string is always
        /// transcoded to bytes via <see cref="System.Text.Encoding.UTF8"/> — there is no
        /// encoding parameter. For non-UTF-8 text or for explicit encoding control, transcode
        /// to bytes at the call site and use <see cref="EncryptBytes"/> instead.
        /// </summary>
        string EncryptText(string input, EncryptionAlgorithm algorithm, CryptoKey key, SymmetricEncryptOptions options = null);

        /// <summary>
        /// Decrypts a Base64-encoded ciphertext and returns the plaintext as a UTF-8 string.
        /// The plaintext bytes are always decoded via <see cref="System.Text.Encoding.UTF8"/>
        /// — there is no encoding parameter. For non-UTF-8 text or for explicit encoding
        /// control, use <see cref="DecryptBytes"/> and decode at the call site.
        /// </summary>
        string DecryptText(string input, EncryptionAlgorithm algorithm, CryptoKey key, SymmetricDecryptOptions options = null);

        void EncryptFile(string inputPath, string outputPath, EncryptionAlgorithm algorithm, CryptoKey key, SymmetricEncryptOptions options = null, bool overwrite = false);

        void DecryptFile(string inputPath, string outputPath, EncryptionAlgorithm algorithm, CryptoKey key, SymmetricDecryptOptions options = null, bool overwrite = false);

        // ── Keyed hash ────────────────────────────────────────────────────────

        string KeyedHashBytes(byte[] input, KeyedHashAlgorithms algorithm, CryptoKey key);

        string KeyedHashText(string input, KeyedHashAlgorithms algorithm, CryptoKey key);

        string KeyedHashFile(string inputPath, KeyedHashAlgorithms algorithm, CryptoKey key);

        // ── PGP encrypt (signer is optional — when supplied, output is encrypted-and-signed) ─

        byte[] PgpEncryptBytes(byte[] input, PgpPublicKey recipient, PgpPrivateKey signer = null);

        string PgpEncryptText(string input, PgpPublicKey recipient, PgpPrivateKey signer = null);

        void PgpEncryptFile(string inputPath, string outputPath, PgpPublicKey recipient, PgpPrivateKey signer = null, bool overwrite = false);

        // ── PGP decrypt (verifier is optional — when supplied, signature is checked too) ─────

        byte[] PgpDecryptBytes(byte[] input, PgpPrivateKey recipient, PgpPublicKey verifier = null);

        string PgpDecryptText(string input, PgpPrivateKey recipient, PgpPublicKey verifier = null);

        void PgpDecryptFile(string inputPath, string outputPath, PgpPrivateKey recipient, PgpPublicKey verifier = null, bool overwrite = false);

        // ── PGP sign (binary) ─────────────────────────────────────────────────

        byte[] PgpSignBytes(byte[] input, PgpPrivateKey signer);

        string PgpSignText(string input, PgpPrivateKey signer);

        void PgpSignFile(string inputPath, string outputPath, PgpPrivateKey signer, bool overwrite = false);

        // ── PGP clear-sign (text) ─────────────────────────────────────────────

        byte[] PgpClearSignBytes(byte[] input, PgpPrivateKey signer);

        string PgpClearSignText(string input, PgpPrivateKey signer);

        void PgpClearSignFile(string inputPath, string outputPath, PgpPrivateKey signer, bool overwrite = false);

        // ── PGP verify (binary signature) ─────────────────────────────────────

        bool PgpVerifyBytes(byte[] input, PgpPublicKey verifier);

        bool PgpVerifyText(string input, PgpPublicKey verifier);

        bool PgpVerifyFile(string inputPath, PgpPublicKey verifier);

        // ── PGP verify (clearsignature) ───────────────────────────────────────

        bool PgpVerifyClearSignedBytes(byte[] input, PgpPublicKey verifier);

        bool PgpVerifyClearSignedText(string input, PgpPublicKey verifier);

        bool PgpVerifyClearSignedFile(string inputPath, PgpPublicKey verifier);

        // ── PGP key generation ────────────────────────────────────────────────

        /// <summary>
        /// Generates an OpenPGP RSA key pair in memory. The public and private halves
        /// returned in the <see cref="PgpKeyPair"/> are mathematically tied — call
        /// <see cref="PgpPublicKey.Save"/> / <see cref="PgpPrivateKey.Save"/> to persist.
        /// </summary>
        PgpKeyPair PgpGenerateKeys(string userId, string passphrase, RsaKeySize keySize = RsaKeySize.Rsa4096);

        PgpKeyPair PgpGenerateKeys(string userId, SecureString passphrase, RsaKeySize keySize = RsaKeySize.Rsa4096);

        /// <summary>
        /// Verifies that the supplied <see cref="PgpPublicKey"/> is a well-formed OpenPGP public key.
        /// </summary>
        bool PgpVerifyPublicKey(PgpPublicKey key);
    }
}
