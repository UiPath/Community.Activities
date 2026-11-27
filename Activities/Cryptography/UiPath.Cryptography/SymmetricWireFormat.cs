using UiPath.Cryptography.Properties;

namespace UiPath.Cryptography
{
    /// <summary>
    /// Selects the byte layout and key-derivation strategy used by the symmetric
    /// encrypt/decrypt activities. See <c>docs/symmetric-wire-format.md</c> for the
    /// full reference, including third-party interop notes.
    /// </summary>
    public enum SymmetricWireFormat
    {
        /// <summary>
        /// UiPath's frozen, byte-stable layout: <c>salt(8) ‖ IV ‖ ciphertext [‖ tag(16)]</c>,
        /// PBKDF2-HMAC-SHA1 @ 10 000 iterations. Default. Required for back-compat with
        /// ciphertext produced by every prior release of this package. Not directly
        /// interoperable with <c>openssl enc</c> or other standard tools.
        /// </summary>
        [LocalizedDescription(nameof(Resources.SymmetricWireFormat_Classic))]
        Classic = 0,

        /// <summary>
        /// Same wire layout as <see cref="Classic"/> but with the OWASP-recommended PBKDF2-HMAC-SHA1
        /// iteration count (1 300 000 by default; caller-overridable). Choose for new
        /// UiPath-to-UiPath workflows that want current-best-practice security. The iteration
        /// count is <b>not</b> carried in the ciphertext — encrypt and decrypt sides must use
        /// matching values. <c>Owasp2026</c> is a year-snapshot: if OWASP revises the
        /// recommendation, this package adds a new enum entry (e.g. <c>Owasp2030</c>) rather
        /// than changing the constant.
        /// </summary>
        [LocalizedDescription(nameof(Resources.SymmetricWireFormat_Owasp2026))]
        Owasp2026 = 1,

        /// <summary>
        /// Caller-supplied raw cipher key and (optionally) IV, no KDF. Wire layout:
        /// <c>IV ‖ ciphertext [‖ tag(16)]</c>. Use for interop with tools that take a literal
        /// cipher key — <c>openssl enc -K &lt;hex&gt; -iv &lt;hex&gt;</c>, Python
        /// <c>cryptography</c>, Java <c>javax.crypto</c>, browser SubtleCrypto, KMS-managed keys.
        /// <b>NEVER reuse the same (Key, IV) pair across encryptions</b>: under AEAD modes
        /// (AES-GCM, ChaCha20-Poly1305) a single nonce collision lets an attacker recover the
        /// authentication subkey and forge arbitrary messages under that key forever.
        /// </summary>
        [LocalizedDescription(nameof(Resources.SymmetricWireFormat_Raw))]
        Raw = 2,

        /// <summary>
        /// <c>openssl enc</c>-compatible layout: <c>Salted__(8) ‖ salt(8) ‖ ciphertext [‖ tag(16)]</c>,
        /// PBKDF2-HMAC-SHA256 with caller-overridable iteration count (default 600 000 — OWASP's
        /// recommendation for SHA-256). Decryptable by <c>openssl enc -d -pbkdf2 -iter &lt;N&gt; -md sha256</c>.
        /// AEAD over <c>OpenSslEnc</c> is a UiPath extension, not a cross-tool standard — combine
        /// <c>AESGCM</c> / <c>ChaCha20Poly1305</c> with this format only when both producer and
        /// consumer are UiPath.
        /// </summary>
        [LocalizedDescription(nameof(Resources.SymmetricWireFormat_OpenSslEnc))]
        OpenSslEnc = 3,
    }
}
