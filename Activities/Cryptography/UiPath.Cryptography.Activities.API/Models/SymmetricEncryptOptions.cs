namespace UiPath.Cryptography.Activities.API
{
    /// <summary>
    /// Optional knobs for symmetric encrypt operations. Default-constructed value
    /// produces <see cref="SymmetricWireFormat.Classic"/> output with auto-generated IV
    /// and the format's recommended KDF iterations.
    /// </summary>
    public sealed class SymmetricEncryptOptions
    {
        /// <summary>
        /// Wire format to produce. Defaults to <see cref="SymmetricWireFormat.Classic"/>
        /// — the byte-stable UiPath layout PBKDF2-HMAC-SHA1 @ 10 000 iterations.
        /// </summary>
        public SymmetricWireFormat Format { get; private init; } = SymmetricWireFormat.Classic;

        /// <summary>
        /// Explicit initialization vector for <see cref="SymmetricWireFormat.Raw"/>.
        /// Null (default) means generate a random IV. Must be null for all non-Raw formats.
        /// </summary>
        public byte[] Iv { get; private init; }

        /// <summary>
        /// PBKDF2 iteration count override for <see cref="SymmetricWireFormat.Owasp2026"/>
        /// and <see cref="SymmetricWireFormat.OpenSslEnc"/>. Zero (default) uses the
        /// format's OWASP-recommended value. Must be zero for Classic and Raw.
        /// </summary>
        public int KdfIterations { get; private init; }

        /// <summary>Produces <see cref="SymmetricWireFormat.Classic"/> output (UiPath's frozen, byte-stable layout).</summary>
        public static SymmetricEncryptOptions Classic() =>
            new() { Format = SymmetricWireFormat.Classic };

        /// <summary>Produces <see cref="SymmetricWireFormat.Owasp2026"/> output (Classic layout, OWASP-recommended PBKDF2-HMAC-SHA1 iterations).</summary>
        /// <param name="kdfIterations">Optional override. Zero uses 1,300,000 (OWASP 2026 recommendation).</param>
        public static SymmetricEncryptOptions Owasp2026(int kdfIterations = 0) =>
            new() { Format = SymmetricWireFormat.Owasp2026, KdfIterations = kdfIterations };

        /// <summary>
        /// Produces <see cref="SymmetricWireFormat.Raw"/> output (caller-supplied key + IV, no KDF).
        /// </summary>
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
        public static SymmetricEncryptOptions Raw(byte[] iv = null) =>
            new() { Format = SymmetricWireFormat.Raw, Iv = iv };

        /// <summary>Produces <see cref="SymmetricWireFormat.OpenSslEnc"/> output (<c>openssl enc</c>-compatible, PBKDF2-HMAC-SHA256).</summary>
        /// <param name="kdfIterations">Optional override. Zero uses 600,000 (OWASP 2026 recommendation for SHA-256).</param>
        public static SymmetricEncryptOptions OpenSslEnc(int kdfIterations = 0) =>
            new() { Format = SymmetricWireFormat.OpenSslEnc, KdfIterations = kdfIterations };
    }
}
