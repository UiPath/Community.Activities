namespace UiPath.Cryptography.Activities.API
{
    /// <summary>
    /// Base for symmetric encrypt and decrypt options. Holds the fields shared between
    /// directions: the key, the wire format, and the KDF iteration count. Construct via the
    /// factory methods on <see cref="SymmetricEncryptOptions"/> / <see cref="SymmetricDecryptOptions"/>;
    /// the factories enforce the (key kind × wire format) pairing at compile time.
    /// </summary>
    public abstract class CryptoOptions
    {
        private protected CryptoOptions() { }

        /// <summary>Key material — a <see cref="PasswordKey"/> for KDF-based formats, a <see cref="RawKey"/> for <see cref="SymmetricWireFormat.Raw"/>.</summary>
        public CryptoKey Key { get; private protected init; }

        /// <summary>Wire format produced / consumed.</summary>
        public SymmetricWireFormat Format { get; private protected init; } = SymmetricWireFormat.Classic;

        /// <summary>
        /// PBKDF2 iteration count for <see cref="SymmetricWireFormat.Owasp2026"/> and
        /// <see cref="SymmetricWireFormat.OpenSslEnc"/>. Set by the format-specific factories.
        /// Zero for Classic and Raw.
        /// </summary>
        public int KdfIterations { get; private protected init; }
    }
}
