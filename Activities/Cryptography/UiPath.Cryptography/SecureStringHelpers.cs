using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace UiPath.Cryptography
{
    /// <summary>
    /// Materialises a <see cref="SecureString"/> to bytes without ever producing a managed
    /// <see cref="string"/>. The secret is copied to an unmanaged Unicode buffer and an
    /// intermediate <c>char[]</c>; both are zeroed in <c>finally</c> so the only thing that
    /// survives is the returned <c>byte[]</c>. Zeroing that buffer is the caller's
    /// responsibility: the symmetric path scrubs it via
    /// <see cref="SymmetricInteropHelper.ClearKeyBytes"/>, while other callers
    /// (e.g. the keyed-hash path through <see cref="CryptographyHelper.KeyEncoding"/>) do not.
    /// </summary>
    /// <remarks>
    /// Modelled on <c>PasswordKey.MaterialisePasswordBytes</c>. Replaces the
    /// <c>new NetworkCredential(string.Empty, secret).Password</c> idiom, which left an
    /// uncontrolled copy of the secret on the GC heap.
    /// </remarks>
    internal static class SecureStringHelpers
    {
        private static long _materialisationCount;

        /// <summary>
        /// Debug/test seam: number of times a <see cref="SecureString"/> has been materialised to a
        /// <c>char[]</c> via <see cref="WithSecureChars"/>. Tests assert this increments to prove the
        /// secret takes the string-free path rather than the old <c>NetworkCredential.Password</c> one.
        /// Exposed as an atomic read-only accessor (via <see cref="Interlocked.Read"/>) so 64-bit reads
        /// never tear on 32-bit runtimes and the backing field cannot be overwritten from elsewhere.
        /// </summary>
        internal static long MaterialisationCount => Interlocked.Read(ref _materialisationCount);

        /// <summary>
        /// Copies <paramref name="secret"/> into a transient <c>char[]</c>, invokes
        /// <paramref name="convert"/> to produce the result bytes, and zeroes both the unmanaged
        /// buffer and the <c>char[]</c> in <c>finally</c>. Returns an empty array when the secret is
        /// null or empty.
        /// </summary>
        internal static byte[] WithSecureChars(SecureString secret, Func<char[], byte[]> convert)
        {
            if (convert == null) throw new ArgumentNullException(nameof(convert));
            if (secret == null || secret.Length == 0)
                return Array.Empty<byte>();

            IntPtr ptr = IntPtr.Zero;
            char[] chars = null;
            try
            {
                ptr = Marshal.SecureStringToGlobalAllocUnicode(secret);
                chars = new char[secret.Length];
                Marshal.Copy(ptr, chars, 0, secret.Length);
                // Count only once the char[] is fully populated, so a failed materialisation
                // (e.g. OOM in the marshal/copy above) does not overcount the seam.
                Interlocked.Increment(ref _materialisationCount);
                return convert(chars);
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
