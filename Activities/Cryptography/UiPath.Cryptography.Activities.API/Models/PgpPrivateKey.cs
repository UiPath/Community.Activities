using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace UiPath.Cryptography.Activities.API
{
    /// <summary>
    /// Holds an OpenPGP private key plus its passphrase. The passphrase is bound to the
    /// key at construction so callers cannot accidentally pair the wrong passphrase with
    /// a private key elsewhere. Use the factory methods to load from bytes or a file path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The passphrase is stored as a <see cref="SecureString"/> for the lifetime of this
    /// instance. It is materialised to a managed <see cref="string"/> only when needed for a
    /// cryptographic operation (BouncyCastle's API requires a plain string and offers no
    /// byte[]-based passphrase API). The materialised string lives on the stack frame of the
    /// service call — not on this object — so its heap residency is bounded by the operation
    /// rather than the lifetime of the <see cref="PgpPrivateKey"/>.
    /// </para>
    /// <para>
    /// The materialised string still cannot be deterministically zeroed (it's a managed
    /// immutable string passed into BouncyCastle). The improvement over storing a managed
    /// string field is per-operation lifetime instead of per-object.
    /// </para>
    /// <para>
    /// <see cref="Dispose"/> zeroes the protected passphrase buffer eagerly. The instance is
    /// safe to use without disposing — the <see cref="SecureString"/> finalizer cleans up
    /// eventually — but calling <see cref="Dispose"/> is recommended for sensitive workflows.
    /// </para>
    /// </remarks>
    public sealed class PgpPrivateKey : IDisposable
    {
        private readonly byte[] _keyBytes;
        private readonly SecureString _passphrase;

        private PgpPrivateKey(byte[] keyBytes, SecureString passphrase)
        {
            _keyBytes = keyBytes;
            _passphrase = passphrase;
        }

        public static PgpPrivateKey FromBytes(byte[] keyBytes, string passphrase)
        {
            ValidateBytes(keyBytes);
            return new PgpPrivateKey(CopyBytes(keyBytes), StringToSecureString(passphrase));
        }

        public static PgpPrivateKey FromBytes(byte[] keyBytes, SecureString passphrase)
        {
            ValidateBytes(keyBytes);
            ArgumentNullException.ThrowIfNull(passphrase);
            return new PgpPrivateKey(CopyBytes(keyBytes), passphrase.Copy());
        }

        public static PgpPrivateKey FromFilePath(string path, string passphrase)
        {
            ValidatePath(path);
            return new PgpPrivateKey(File.ReadAllBytes(path), StringToSecureString(passphrase));
        }

        public static PgpPrivateKey FromFilePath(string path, SecureString passphrase)
        {
            ValidatePath(path);
            ArgumentNullException.ThrowIfNull(passphrase);
            return new PgpPrivateKey(File.ReadAllBytes(path), passphrase.Copy());
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

        public void Dispose() => _passphrase?.Dispose();

        // Materialise the passphrase to a managed string just-in-time. The returned string
        // lives in the caller's stack frame and falls out of scope at the end of the
        // cryptographic operation — not pinned to this instance.
        internal (Stream stream, string passphrase) Open() =>
            (new MemoryStream(_keyBytes, writable: false), MaterialiseSecureString(_passphrase));

        private static void ValidateBytes(byte[] keyBytes)
        {
            if (keyBytes is null || keyBytes.Length == 0)
                throw new ArgumentException("Private key bytes must not be null or empty.", nameof(keyBytes));
        }

        private static void ValidatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Private key file path must not be null or empty.", nameof(path));
        }

        private static byte[] CopyBytes(byte[] src)
        {
            byte[] copy = new byte[src.Length];
            Buffer.BlockCopy(src, 0, copy, 0, src.Length);
            return copy;
        }

        private static SecureString StringToSecureString(string value)
        {
            var ss = new SecureString();
            if (!string.IsNullOrEmpty(value))
            {
                foreach (char c in value)
                    ss.AppendChar(c);
            }
            ss.MakeReadOnly();
            return ss;
        }

        private static string MaterialiseSecureString(SecureString value)
        {
            if (value is null || value.Length == 0)
                return null;
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(ptr);
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr);
            }
        }
    }
}
