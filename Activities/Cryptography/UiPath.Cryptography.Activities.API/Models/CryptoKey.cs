namespace UiPath.Cryptography.Activities.API
{
    /// <summary>
    /// Base type for key material supplied to a symmetric or keyed-hash operation. Choose a
    /// concrete subclass based on what the bytes represent:
    /// <list type="bullet">
    /// <item><description><see cref="PasswordKey"/> — low-entropy password material to be PBKDF2-stretched (used with <see cref="SymmetricWireFormat.Classic"/>, <see cref="SymmetricWireFormat.Owasp2026"/>, <see cref="SymmetricWireFormat.OpenSslEnc"/>).</description></item>
    /// <item><description><see cref="RawKey"/> — a literal cipher key of the exact size required by the algorithm (used with <see cref="SymmetricWireFormat.Raw"/>).</description></item>
    /// </list>
    /// The factory on the corresponding <c>SymmetricEncryptOptions</c> / <c>SymmetricDecryptOptions</c>
    /// format takes the matching key type, so mismatched pairings fail at compile time.
    /// </summary>
    public abstract class CryptoKey
    {
        private protected CryptoKey() { }

        /// <summary>
        /// Bytes the cipher will operate on. Each derived type owns its storage strategy —
        /// see <see cref="PasswordKey.KeyBytes"/> for the lazy SecureString-backed path and
        /// <see cref="RawKey.KeyBytes"/> for the literal-key path.
        /// </summary>
        internal abstract byte[] KeyBytes { get; }

        internal abstract bool IsRawKey { get; }

        internal abstract KeyBytesFormat BytesFormat { get; }

        /// <summary>
        /// Release a buffer that <see cref="KeyBytes"/> handed out for a single operation. Default
        /// is a no-op: <see cref="RawKey"/> returns its instance-owned storage, and clearing it
        /// per call would corrupt the key. <see cref="PasswordKey"/> overrides this to zero the
        /// returned buffer eagerly, so freshly-materialised password bytes do not linger on the
        /// managed heap between operations.
        /// </summary>
        internal virtual void ReleaseMaterialisedBytes(byte[] bytes) { }
    }
}
