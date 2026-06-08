namespace UiPath.Cryptography.Activities.API
{
    /// <summary>
    /// Options for symmetric decrypt operations. The IV is read from the ciphertext stream on
    /// decrypt, so there is no <c>IV</c> field on this type. Construct via a format factory;
    /// the factory's key parameter type enforces the (key kind × wire format) pairing at
    /// compile time.
    /// </summary>
    public sealed class SymmetricDecryptOptions : CryptoOptions
    {
        private SymmetricDecryptOptions() { }

        /// <summary>Decrypts <see cref="SymmetricWireFormat.Classic"/> ciphertext.</summary>
        public static SymmetricDecryptOptions Classic(PasswordKey key) =>
            new() { Key = key, Format = SymmetricWireFormat.Classic };

        /// <summary>Decrypts <see cref="SymmetricWireFormat.Owasp2026"/> ciphertext.</summary>
        /// <param name="key">Password material; must match the key used at encrypt time.</param>
        /// <param name="kdfIterations">
        /// PBKDF2 iteration count. Must match the value used at encrypt time.
        /// Defaults to <c>1_300_000</c> — the OWASP 2026 recommendation for PBKDF2-HMAC-SHA1.
        /// </param>
        public static SymmetricDecryptOptions Owasp2026(PasswordKey key, int kdfIterations = 1_300_000) =>
            new() { Key = key, Format = SymmetricWireFormat.Owasp2026, KdfIterations = kdfIterations };

        /// <summary>Decrypts <see cref="SymmetricWireFormat.Raw"/> ciphertext (the IV is read from the prefix of the stream).</summary>
        /// <param name="key">Literal cipher key — must be the same bytes used at encrypt time.</param>
        public static SymmetricDecryptOptions Raw(RawKey key) =>
            new() { Key = key, Format = SymmetricWireFormat.Raw };

        /// <summary>Decrypts <see cref="SymmetricWireFormat.OpenSslEnc"/> ciphertext.</summary>
        /// <param name="key">Password material; must match the key used at encrypt time.</param>
        /// <param name="kdfIterations">
        /// PBKDF2 iteration count. Must match the value used at encrypt time.
        /// Defaults to <c>600_000</c> — the OWASP 2026 recommendation for PBKDF2-HMAC-SHA256.
        /// Pass <c>10_000</c> when reading output produced by <c>openssl enc -pbkdf2</c> without an explicit
        /// <c>-iter</c> flag (openssl's back-compat default).
        /// </param>
        public static SymmetricDecryptOptions OpenSslEnc(PasswordKey key, int kdfIterations = 600_000) =>
            new() { Key = key, Format = SymmetricWireFormat.OpenSslEnc, KdfIterations = kdfIterations };
    }
}
