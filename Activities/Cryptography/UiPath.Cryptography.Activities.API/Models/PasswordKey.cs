using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace UiPath.Cryptography.Activities.API
{
    /// <summary>
    /// Password material to be stretched through PBKDF2 to derive the cipher key.
    /// Compatible with <see cref="SymmetricWireFormat.Classic"/>,
    /// <see cref="SymmetricWireFormat.Owasp2026"/>, and <see cref="SymmetricWireFormat.OpenSslEnc"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The password is stored internally as a <see cref="SecureString"/>. Plain bytes are
    /// materialised <b>just-in-time</b> on every <see cref="KeyBytes"/> access — they live
    /// only as long as the caller's local variable (in practice, the duration of a single
    /// <c>CryptographyService</c> operation), never pinned to this instance. The materialiser
    /// also zeroes the unmanaged Unicode buffer and the intermediate <c>char[]</c> in
    /// <c>finally</c>; only the returned <c>byte[]</c> survives, governed by the caller's
    /// stack lifetime.
    /// </para>
    /// <para>
    /// <see cref="Dispose"/> eagerly zeroes the protected SecureString buffer and nulls the
    /// stored reference; subsequent <see cref="KeyBytes"/> access throws
    /// <see cref="ObjectDisposedException"/> so a stale reference cannot silently produce
    /// wrong ciphertext. Disposing is optional — the SecureString finalizer cleans up
    /// eventually — but recommended for sensitive workflows.
    /// </para>
    /// </remarks>
    public sealed class PasswordKey : CryptoKey, IDisposable
    {
        private SecureString _password;
        private readonly Encoding _encoding;

        private PasswordKey(SecureString password, Encoding encoding)
        {
            _password = password;
            _encoding = encoding;
        }

        internal override bool IsRawKey => false;

        internal override KeyBytesFormat BytesFormat => KeyBytesFormat.Encoded;

        internal override byte[] KeyBytes
            => MaterialisePasswordBytes(_password ?? throw new ObjectDisposedException(nameof(PasswordKey)), _encoding);

        // Each KeyBytes access returns a freshly-allocated buffer (see MaterialisePasswordBytes).
        // After the caller has fed it into PBKDF2 / HMAC, zero it eagerly so the password
        // bytes do not survive on the managed heap until the next GC pass.
        internal override void ReleaseMaterialisedBytes(byte[] bytes)
        {
            if (bytes != null && bytes.Length > 0)
                Array.Clear(bytes, 0, bytes.Length);
        }

        public void Dispose()
        {
            if (_password != null)
            {
                _password.Dispose();
                _password = null;
            }
        }

        /// <summary>
        /// Creates a <see cref="PasswordKey"/> from a plain-string password. The string is
        /// copied into a <see cref="SecureString"/> internally — note that the caller's
        /// original string is unaffected and continues to live on the managed heap until GC.
        /// At each encrypt/decrypt operation, PBKDF2 stretches the bytes into the algorithm's
        /// cipher key using the salt embedded in the wire format and the iteration count
        /// from the <see cref="SymmetricEncryptOptions"/> / <see cref="SymmetricDecryptOptions"/> factory.
        /// </summary>
        /// <param name="password">Password material. Any length is accepted — PBKDF2 stretches.</param>
        /// <param name="encoding">Encoding used to transcode <paramref name="password"/> to bytes. Typically <see cref="Encoding.UTF8"/>; must match the encoding used at decrypt time.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="password"/> is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="encoding"/> is null.</exception>
        public static PasswordKey FromPassword(string password, Encoding encoding)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password must not be null or empty.", nameof(password));
            ArgumentNullException.ThrowIfNull(encoding);

            var ss = new SecureString();
            foreach (char c in password)
                ss.AppendChar(c);
            ss.MakeReadOnly();
            return new PasswordKey(ss, encoding);
        }

        /// <summary>
        /// Creates a <see cref="PasswordKey"/> from a <see cref="SecureString"/> password.
        /// The caller's <see cref="SecureString"/> is copied (via <see cref="SecureString.Copy"/>)
        /// so disposing it externally does not affect this instance. Bytes are materialised
        /// just-in-time on each encrypt/decrypt operation and do not persist on the heap
        /// pinned to this object.
        /// </summary>
        /// <param name="password">Password material sourced from a secret store or user input. Any length accepted.</param>
        /// <param name="encoding">Encoding used to transcode <paramref name="password"/> to bytes. Typically <see cref="Encoding.UTF8"/>; must match the encoding used at decrypt time.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="password"/> or <paramref name="encoding"/> is null.</exception>
        public static PasswordKey FromPassword(SecureString password, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(password);
            ArgumentNullException.ThrowIfNull(encoding);
            return new PasswordKey(password.Copy(), encoding);
        }

        // Materialise SecureString contents to bytes via a Marshal-based path. The unmanaged
        // buffer and the intermediate char[] are zeroed in finally; only the returned byte[]
        // persists, on the caller's stack lifetime.
        private static byte[] MaterialisePasswordBytes(SecureString password, Encoding encoding)
        {
            if (password.Length == 0)
                return Array.Empty<byte>();

            IntPtr ptr = IntPtr.Zero;
            char[] chars = null;
            try
            {
                ptr = Marshal.SecureStringToGlobalAllocUnicode(password);
                chars = new char[password.Length];
                Marshal.Copy(ptr, chars, 0, password.Length);
                return encoding.GetBytes(chars);
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr);
                if (chars != null)
                    Array.Clear(chars, 0, chars.Length);
            }
        }
    }
}
