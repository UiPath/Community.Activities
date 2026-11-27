using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace UiPath.FTP.Extended.Tests
{
    public class TruncatedHmacTests
    {
        private static readonly byte[] Data = Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");

        public static TheoryData<string> Algorithms => new TheoryData<string> { "md5", "sha1", "sha256", "sha512" };

        private static HMAC Full(string algorithm, byte[] key)
        {
            switch (algorithm)
            {
                case "md5": return new HMACMD5(key);
                case "sha1": return new HMACSHA1(key);
                case "sha256": return new HMACSHA256(key);
                default: return new HMACSHA512(key);
            }
        }

        [Theory]
        [MemberData(nameof(Algorithms))]
        public void ComputeHash_IsTheFirst96BitsOfTheFullMac(string algorithm)
        {
            byte[] key = Enumerable.Range(1, 20).Select(i => (byte)i).ToArray();
            byte[] expected = Full(algorithm, key).ComputeHash(Data).Take(12).ToArray();

            using (var truncated = new TruncatedHmac(Full(algorithm, key), 12))
            {
                Assert.Equal(96, truncated.HashSize);
                Assert.Equal(expected, truncated.ComputeHash(Data));
            }
        }

        [Theory]
        [MemberData(nameof(Algorithms))]
        public void TryComputeHash_WithSpans_IsTheFirst96BitsOfTheFullMac(string algorithm)
        {
            // The SSH session hashes with spans; the HMAC base class rejects that unless the subclass handles it.
            byte[] key = Enumerable.Range(1, 20).Select(i => (byte)i).ToArray();
            byte[] expected = Full(algorithm, key).ComputeHash(Data).Take(12).ToArray();
            Span<byte> destination = stackalloc byte[12];

            using (var truncated = new TruncatedHmac(Full(algorithm, key), 12))
            {
                Assert.True(truncated.TryComputeHash(Data, destination, out int written));
                Assert.Equal(12, written);
                Assert.Equal(expected, destination.ToArray());
            }
        }

        [Fact]
        public void TryComputeHash_WithADestinationThatIsTooShort_ReturnsFalse()
        {
            using (var truncated = new TruncatedHmac(new HMACSHA1(new byte[20]), 12))
            {
                Span<byte> destination = stackalloc byte[11];
                Assert.False(truncated.TryComputeHash(Data, destination, out int written));
                Assert.Equal(0, written);
            }
        }

        [Fact]
        public void ComputeHash_CanBeUsedForSeveralPackets()
        {
            byte[] key = new byte[20];
            using (var truncated = new TruncatedHmac(new HMACSHA1(key), 12))
            {
                byte[] first = truncated.ComputeHash(Data);
                byte[] second = truncated.ComputeHash(Data);
                Assert.Equal(first, second);
                Assert.Equal(new HMACSHA1(key).ComputeHash(Data).Take(12).ToArray(), second);
            }
        }

        [Fact]
        public void ComputeHash_WithOffsetAndCount_HashesOnlyThatRange()
        {
            byte[] key = new byte[32];
            byte[] padded = new byte[] { 9, 9 }.Concat(Data).Concat(new byte[] { 7 }).ToArray();

            using (var truncated = new TruncatedHmac(new HMACSHA256(key), 12))
            {
                Assert.Equal(new HMACSHA256(key).ComputeHash(Data).Take(12).ToArray(), truncated.ComputeHash(padded, 2, Data.Length));
            }
        }

        [Fact]
        public void Constructor_RejectsAnInvalidLength()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TruncatedHmac(new HMACSHA1(new byte[20]), 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TruncatedHmac(new HMACSHA1(new byte[20]), 21));
        }
    }
}
