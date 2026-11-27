using System;

#pragma warning disable CS0618 // CryptographyHelper is intentionally marked Obsolete to discourage external use; in-package consumers are expected.

namespace UiPath.Cryptography.Activities.API
{
    /// <summary>
    /// A literal cipher key — exact-length bytes used directly by the algorithm with no KDF.
    /// Compatible only with <see cref="SymmetricWireFormat.Raw"/>. Length must match a legal
    /// key size for the algorithm (e.g. 16/24/32 bytes for AES).
    /// </summary>
    /// <remarks>
    /// <see cref="Dispose"/> zeroes the held key bytes in place. The bytes are the cipher
    /// key itself — disposing eagerly removes them from the managed heap (rather than waiting
    /// for GC). After disposal, <see cref="KeyBytes"/> throws <see cref="ObjectDisposedException"/>
    /// so a stale reference cannot silently encrypt with an all-zero key.
    /// </remarks>
    public sealed class RawKey : CryptoKey, IDisposable
    {
        private byte[] _keyBytes;

        private RawKey(byte[] keyBytes)
        {
            _keyBytes = keyBytes;
        }

        internal override bool IsRawKey => true;

        internal override KeyBytesFormat BytesFormat => KeyBytesFormat.Hex;

        internal override byte[] KeyBytes
            => _keyBytes ?? throw new ObjectDisposedException(nameof(RawKey));

        public void Dispose()
        {
            if (_keyBytes != null)
            {
                Array.Clear(_keyBytes, 0, _keyBytes.Length);
                _keyBytes = null;
            }
        }

        /// <summary>
        /// Creates a <see cref="RawKey"/> from an in-memory byte array. The input is
        /// defensively copied so subsequent mutations of the caller's buffer don't affect
        /// this instance.
        /// </summary>
        /// <param name="keyBytes">
        /// Literal cipher key bytes. Length must match a legal key size for the algorithm
        /// the key will be used with (e.g. 16, 24, or 32 bytes for AES — the runtime
        /// validator surfaces a mismatch as <see cref="ArgumentException"/> at the service
        /// entry, not here).
        /// </param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="keyBytes"/> is null or empty.</exception>
        public static RawKey FromBytes(byte[] keyBytes)
        {
            if (keyBytes is null || keyBytes.Length == 0)
                throw new ArgumentException("Raw key bytes must not be null or empty.", nameof(keyBytes));
            byte[] copy = new byte[keyBytes.Length];
            Buffer.BlockCopy(keyBytes, 0, copy, 0, keyBytes.Length);
            return new RawKey(copy);
        }

        /// <summary>
        /// Creates a <see cref="RawKey"/> by decoding a hex string into bytes. Use when the
        /// key arrives as a hex literal (e.g. from configuration, an HTTP response, or the
        /// output of <c>openssl rand -hex N</c>).
        /// </summary>
        /// <param name="hex">
        /// Hex-encoded key. Must contain only hex digits (0-9, a-f, A-F) and have even
        /// length. The decoded byte length must match a legal key size for the algorithm
        /// (validated at the service entry, not here).
        /// </param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="hex"/> is null or empty.</exception>
        /// <exception cref="FormatException">Thrown when <paramref name="hex"/> is not a valid hex string.</exception>
        public static RawKey FromHex(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                throw new ArgumentException("Hex string must not be null or empty.", nameof(hex));
            return new RawKey(CryptographyHelper.ParseKeyBytes(hex, null, KeyBytesFormat.Hex, null));
        }

        /// <summary>
        /// Creates a <see cref="RawKey"/> by decoding a Base64 string into bytes. Use when
        /// the key arrives Base64-encoded (e.g. JSON config, JWT-style payload, or the
        /// output of <c>openssl rand -base64 N</c>).
        /// </summary>
        /// <param name="base64">
        /// Base64-encoded key, optionally padded with '='. The decoded byte length must
        /// match a legal key size for the algorithm (validated at the service entry, not
        /// here).
        /// </param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="base64"/> is null or empty.</exception>
        /// <exception cref="FormatException">Thrown when <paramref name="base64"/> is not a valid Base64 string.</exception>
        public static RawKey FromBase64(string base64)
        {
            if (string.IsNullOrEmpty(base64))
                throw new ArgumentException("Base64 string must not be null or empty.", nameof(base64));
            return new RawKey(CryptographyHelper.ParseKeyBytes(base64, null, KeyBytesFormat.Base64, null));
        }
    }
}
