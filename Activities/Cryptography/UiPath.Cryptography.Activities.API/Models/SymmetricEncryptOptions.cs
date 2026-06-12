using System.Text;

namespace UiPath.Cryptography.Activities.API
{
    /// <summary>
    /// Options for symmetric encrypt operations. Bundles the key, wire format, and any
    /// format-specific knobs (IV for <see cref="SymmetricWireFormat.Raw"/>, KDF iterations
    /// for <see cref="SymmetricWireFormat.Owasp2026"/> / <see cref="SymmetricWireFormat.OpenSslEnc"/>).
    /// Construct via a format factory; the factory's key parameter type enforces the
    /// (key kind × wire format) pairing at compile time.
    /// </summary>
    public sealed class SymmetricEncryptOptions : CryptoOptions
    {
        private SymmetricEncryptOptions() { }

        /// <summary>
        /// Explicit initialization vector for <see cref="SymmetricWireFormat.Raw"/>.
        /// Null (default) means generate a random IV. Set only by the
        /// <see cref="Raw(RawKey, byte[], Encoding)"/> factory.
        /// </summary>
        public byte[] IV { get; private init; }

        /// <summary>Produces <see cref="SymmetricWireFormat.Classic"/> output — UiPath's frozen, byte-stable layout (PBKDF2-HMAC-SHA1 @ 10 000 iter).</summary>
        /// <param name="key">Password material; PBKDF2 stretches it to the cipher key.</param>
        /// <param name="encoding">Plaintext encoding for <c>EncryptText</c>. Null defaults to <see cref="Encoding.UTF8"/>; ignored by <c>EncryptBytes</c> / <c>EncryptFile</c>.</param>
        public static SymmetricEncryptOptions Classic(PasswordKey key, Encoding encoding = null) =>
            new() { Key = key, Format = SymmetricWireFormat.Classic, TextEncoding = encoding ?? Encoding.UTF8 };

        /// <summary>Produces <see cref="SymmetricWireFormat.Owasp2026"/> output (Classic wire layout, PBKDF2-HMAC-SHA1).</summary>
        /// <param name="key">Password material; PBKDF2 stretches it to the cipher key.</param>
        /// <param name="kdfIterations">
        /// PBKDF2 iteration count. Defaults to <c>1_300_000</c> — the OWASP 2026 recommendation
        /// for PBKDF2-HMAC-SHA1. The literal is part of the year-snapshot contract: if OWASP
        /// revises the recommendation, this package adds a new <see cref="SymmetricWireFormat"/>
        /// entry (e.g. <c>Owasp2030</c>) rather than changing this default.
        /// </param>
        /// <param name="encoding">Plaintext encoding for <c>EncryptText</c>. Null defaults to <see cref="Encoding.UTF8"/>; ignored by <c>EncryptBytes</c> / <c>EncryptFile</c>.</param>
        public static SymmetricEncryptOptions Owasp2026(PasswordKey key, int kdfIterations = 1_300_000, Encoding encoding = null) =>
            new() { Key = key, Format = SymmetricWireFormat.Owasp2026, KdfIterations = kdfIterations, TextEncoding = encoding ?? Encoding.UTF8 };

        /// <summary>
        /// Produces <see cref="SymmetricWireFormat.Raw"/> output (caller-supplied key + IV, no KDF).
        /// </summary>
        /// <param name="key">Literal cipher key of the algorithm's required size (e.g. 32 bytes for AES-256).</param>
        /// <param name="iv">
        /// Optional explicit IV. Null (the default) lets the cipher generate one.
        /// <para>
        /// <b>⚠ NEVER reuse the same (Key, IV) pair across encryptions.</b> Doing so destroys
        /// confidentiality (CTR keystream reuse) and — under AEAD modes (AES-GCM,
        /// ChaCha20-Poly1305) — additionally lets an attacker recover the authentication key
        /// and forge arbitrary messages under that key. Prefer leaving <paramref name="iv"/>
        /// null so a fresh random IV is generated per call; only supply an explicit IV when a
        /// third-party protocol mandates it, and ensure your producer guarantees uniqueness.
        /// </para>
        /// </param>
        /// <param name="encoding">Plaintext encoding for <c>EncryptText</c>. Null defaults to <see cref="Encoding.UTF8"/>; ignored by <c>EncryptBytes</c> / <c>EncryptFile</c>.</param>
        public static SymmetricEncryptOptions Raw(RawKey key, byte[] iv = null, Encoding encoding = null) =>
            new() { Key = key, Format = SymmetricWireFormat.Raw, IV = iv, TextEncoding = encoding ?? Encoding.UTF8 };

        /// <summary>Produces <see cref="SymmetricWireFormat.OpenSslEnc"/> output (<c>openssl enc</c>-compatible, PBKDF2-HMAC-SHA256).</summary>
        /// <param name="key">Password material; PBKDF2-SHA256 stretches it to key+IV.</param>
        /// <param name="kdfIterations">
        /// PBKDF2 iteration count. Defaults to <c>600_000</c> — the OWASP 2026 recommendation
        /// for PBKDF2-HMAC-SHA256. (Note: <c>openssl enc</c>'s own default is 10 000 for
        /// back-compat; the OWASP-aligned default produces stronger output but is still
        /// decryptable by <c>openssl enc -pbkdf2 -iter 600000 -md sha256</c>.) Pass <c>10_000</c>
        /// to match the openssl back-compat default explicitly.
        /// </param>
        /// <param name="encoding">Plaintext encoding for <c>EncryptText</c>. Null defaults to <see cref="Encoding.UTF8"/>; ignored by <c>EncryptBytes</c> / <c>EncryptFile</c>.</param>
        public static SymmetricEncryptOptions OpenSslEnc(PasswordKey key, int kdfIterations = 600_000, Encoding encoding = null) =>
            new() { Key = key, Format = SymmetricWireFormat.OpenSslEnc, KdfIterations = kdfIterations, TextEncoding = encoding ?? Encoding.UTF8 };
    }
}
