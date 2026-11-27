using System;
using System.IO;
using System.Security.Cryptography;

namespace UiPath.FTP.Extended
{
    /// <summary>
    /// An HMAC whose result is the first bytes of another HMAC, for the truncated MACs of SSH
    /// (<c>hmac-sha1-96</c>, <c>hmac-md5-96</c>, <c>hmac-sha2-256-96</c> and so on keep 96 bits).
    /// </summary>
    /// <remarks>
    /// The data is collected and the inner HMAC computes it in one call: the base class <see cref="HMAC"/> rejects the
    /// span based hashing the SSH session uses unless the subclass handles it, and an SSH packet is small.
    /// </remarks>
    internal sealed class TruncatedHmac : HMAC // NOSONAR - wraps the platform HMAC and only truncates its tag, no algorithm of its own
    {
        private readonly HMAC _inner;
        private readonly int _length;
        private readonly MemoryStream _buffer = new MemoryStream();

        public TruncatedHmac(HMAC inner, int lengthInBytes)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            if (lengthInBytes <= 0 || lengthInBytes > inner.HashSize / 8)
            {
                throw new ArgumentOutOfRangeException(nameof(lengthInBytes));
            }

            _length = lengthInBytes;
            HashSizeValue = lengthInBytes * 8;
        }

        public override byte[] Key
        {
            get => _inner.Key;
            set => _inner.Key = value;
        }

        public override void Initialize()
        {
            _buffer.SetLength(0);
            _inner.Initialize();
        }

        protected override void HashCore(byte[] rgb, int ib, int cb) => _buffer.Write(rgb, ib, cb);

        protected override void HashCore(ReadOnlySpan<byte> source) => _buffer.Write(source);

        protected override byte[] HashFinal()
        {
            byte[] full = _inner.ComputeHash(_buffer.GetBuffer(), 0, (int)_buffer.Length);
            _buffer.SetLength(0);

            var truncated = new byte[_length];
            Buffer.BlockCopy(full, 0, truncated, 0, _length);
            return truncated;
        }

        protected override bool TryHashFinal(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length < _length)
            {
                bytesWritten = 0;
                return false;
            }

            HashFinal().CopyTo(destination);
            bytesWritten = _length;
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
                _buffer.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
