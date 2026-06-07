using System;
using System.Security;
using System.Text;

#pragma warning disable CS0618 // CryptographyHelper is intentionally marked Obsolete to discourage external use; in-package consumers are expected.

namespace UiPath.Cryptography.Activities.API
{
    /// <summary>
    /// Represents key material supplied to a symmetric or keyed-hash operation.
    /// Use the factory methods to choose how the value is interpreted:
    /// <see cref="FromPassword(string, Encoding)"/> / <see cref="FromPassword(SecureString, Encoding)"/>
    /// produce password bytes for the PBKDF2-based formats (Classic, Owasp2026, OpenSslEnc),
    /// while <see cref="FromRawBytes"/>, <see cref="FromHexString"/>, and <see cref="FromBase64String"/>
    /// produce a literal key for <see cref="SymmetricWireFormat.Raw"/>.
    /// </summary>
    /// <remarks>
    /// The factory methods resolve key material to <c>byte[]</c> eagerly. The resulting bytes live
    /// on the managed heap for the lifetime of the <see cref="CryptoKey"/> instance.
    /// </remarks>
    public sealed class CryptoKey
    {
        private CryptoKey(byte[] keyBytes, bool isRawKey)
        {
            KeyBytes = keyBytes;
            IsRawKey = isRawKey;
        }

        internal byte[] KeyBytes { get; }

        /// <summary>
        /// True when the bytes are a literal cipher key (compatible with <see cref="SymmetricWireFormat.Raw"/>),
        /// false when they are password bytes for a KDF-based format.
        /// </summary>
        internal bool IsRawKey { get; }

        internal KeyBytesFormat AsKeyBytesFormat() => IsRawKey ? KeyBytesFormat.Hex : KeyBytesFormat.Encoded;

        public static CryptoKey FromPassword(string password, Encoding encoding)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password must not be null or empty.", nameof(password));
            ArgumentNullException.ThrowIfNull(encoding);
            return new CryptoKey(CryptographyHelper.KeyEncoding(encoding, password, null), isRawKey: false);
        }

        public static CryptoKey FromPassword(SecureString password, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(password);
            ArgumentNullException.ThrowIfNull(encoding);
            return new CryptoKey(CryptographyHelper.KeyEncoding(encoding, null, password), isRawKey: false);
        }

        public static CryptoKey FromRawBytes(byte[] keyBytes)
        {
            if (keyBytes is null || keyBytes.Length == 0)
                throw new ArgumentException("Raw key bytes must not be null or empty.", nameof(keyBytes));
            byte[] copy = new byte[keyBytes.Length];
            Buffer.BlockCopy(keyBytes, 0, copy, 0, keyBytes.Length);
            return new CryptoKey(copy, isRawKey: true);
        }

        public static CryptoKey FromHexString(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                throw new ArgumentException("Hex string must not be null or empty.", nameof(hex));
            return new CryptoKey(CryptographyHelper.ParseKeyBytes(hex, null, KeyBytesFormat.Hex, null), isRawKey: true);
        }

        public static CryptoKey FromBase64String(string base64)
        {
            if (string.IsNullOrEmpty(base64))
                throw new ArgumentException("Base64 string must not be null or empty.", nameof(base64));
            return new CryptoKey(CryptographyHelper.ParseKeyBytes(base64, null, KeyBytesFormat.Base64, null), isRawKey: true);
        }
    }
}
