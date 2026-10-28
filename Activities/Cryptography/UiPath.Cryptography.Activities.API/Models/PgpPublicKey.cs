using System;
using System.IO;

namespace UiPath.Cryptography.Activities.API
{
    /// <summary>
    /// Holds an OpenPGP public key in memory. Use the factory methods to load from
    /// bytes or a file path; the same instance can be reused across multiple PGP
    /// operations.
    /// </summary>
    public sealed class PgpPublicKey
    {
        private readonly byte[] _keyBytes;

        private PgpPublicKey(byte[] keyBytes)
        {
            _keyBytes = keyBytes;
        }

        public static PgpPublicKey FromBytes(byte[] keyBytes)
        {
            if (keyBytes is null || keyBytes.Length == 0)
                throw new ArgumentException("Public key bytes must not be null or empty.", nameof(keyBytes));
            byte[] copy = new byte[keyBytes.Length];
            Buffer.BlockCopy(keyBytes, 0, copy, 0, keyBytes.Length);
            return new PgpPublicKey(copy);
        }

        public static PgpPublicKey FromFilePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Public key file path must not be null or empty.", nameof(path));
            return new PgpPublicKey(File.ReadAllBytes(path));
        }

        public byte[] ToBytes()
        {
            byte[] copy = new byte[_keyBytes.Length];
            Buffer.BlockCopy(_keyBytes, 0, copy, 0, _keyBytes.Length);
            return copy;
        }

        public void Save(string filePath, bool overwrite = false)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path must not be null or empty.", nameof(filePath));
            if (!overwrite && File.Exists(filePath))
                throw new InvalidOperationException($"Output file already exists: {filePath}");
            File.WriteAllBytes(filePath, _keyBytes);
        }

        internal Stream OpenStream() => new MemoryStream(_keyBytes, writable: false);
    }
}
