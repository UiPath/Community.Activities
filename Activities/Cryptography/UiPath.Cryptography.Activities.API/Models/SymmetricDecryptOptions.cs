namespace UiPath.Cryptography.Activities.API
{
    /// <summary>
    /// Optional knobs for symmetric decrypt operations. The IV is read from the
    /// ciphertext stream on decrypt, so there is no <c>Iv</c> field on this type.
    /// </summary>
    public sealed class SymmetricDecryptOptions
    {
        /// <summary>
        /// Wire format the ciphertext was produced in. Defaults to
        /// <see cref="SymmetricWireFormat.Classic"/>.
        /// </summary>
        public SymmetricWireFormat Format { get; private init; } = SymmetricWireFormat.Classic;

        /// <summary>
        /// PBKDF2 iteration count override for <see cref="SymmetricWireFormat.Owasp2026"/>
        /// and <see cref="SymmetricWireFormat.OpenSslEnc"/>. Zero (default) uses the
        /// format's OWASP-recommended value. Must match the value used at encrypt time.
        /// </summary>
        public int KdfIterations { get; private init; }

        /// <summary>Decrypts <see cref="SymmetricWireFormat.Classic"/> ciphertext (UiPath's frozen, byte-stable layout).</summary>
        public static SymmetricDecryptOptions Classic() =>
            new() { Format = SymmetricWireFormat.Classic };

        /// <summary>Decrypts <see cref="SymmetricWireFormat.Owasp2026"/> ciphertext.</summary>
        /// <param name="kdfIterations">Must match the value used at encrypt time. Zero uses the OWASP default.</param>
        public static SymmetricDecryptOptions Owasp2026(int kdfIterations = 0) =>
            new() { Format = SymmetricWireFormat.Owasp2026, KdfIterations = kdfIterations };

        /// <summary>Decrypts <see cref="SymmetricWireFormat.Raw"/> ciphertext (the IV is read from the prefix of the stream).</summary>
        public static SymmetricDecryptOptions Raw() =>
            new() { Format = SymmetricWireFormat.Raw };

        /// <summary>Decrypts <see cref="SymmetricWireFormat.OpenSslEnc"/> ciphertext.</summary>
        /// <param name="kdfIterations">Must match the value used at encrypt time. Zero uses the OWASP default.</param>
        public static SymmetricDecryptOptions OpenSslEnc(int kdfIterations = 0) =>
            new() { Format = SymmetricWireFormat.OpenSslEnc, KdfIterations = kdfIterations };
    }
}
