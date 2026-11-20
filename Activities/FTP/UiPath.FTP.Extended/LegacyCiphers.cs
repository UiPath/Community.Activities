using System;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using UiPath.FTP.Extensibility;

namespace UiPath.FTP.Extended
{
    /// <summary>
    /// A block cipher in CBC mode whose chaining value carries over from one packet to the next, as SSH requires
    /// (blowfish-cbc, twofish*-cbc, cast128-cbc), with BouncyCastle. The instance is used for one direction.
    /// </summary>
    internal sealed class LegacyCbcCipher : ISftpCipher
    {
        private readonly BufferedBlockCipher _encryptor;
        private readonly BufferedBlockCipher _decryptor;

        public LegacyCbcCipher(Func<IBlockCipher> engineFactory, byte[] key, byte[] iv)
        {
            BlockSize = engineFactory().GetBlockSize();
            if (iv.Length < BlockSize)
            {
                throw new ArgumentException("The initialization vector is shorter than the block size.", nameof(iv));
            }

            var parameters = new ParametersWithIV(new KeyParameter(key), iv, 0, BlockSize);

            // Each direction chains on its own; the SSH session uses one instance per direction.
            _encryptor = new BufferedBlockCipher(new CbcBlockCipher(engineFactory()));
            _encryptor.Init(true, parameters);
            _decryptor = new BufferedBlockCipher(new CbcBlockCipher(engineFactory()));
            _decryptor.Init(false, parameters);
        }

        public int BlockSize { get; }

        public int TagSize => 0;

        public byte[] Encrypt(byte[] input, int offset, int length) => Process(_encryptor, input, offset, length);

        public byte[] Decrypt(byte[] input, int offset, int length) => Process(_decryptor, input, offset, length);

        public static LegacyCbcCipher Blowfish(byte[] key, byte[] iv) => new LegacyCbcCipher(() => new BlowfishEngine(), key, iv);

        public static LegacyCbcCipher Twofish(byte[] key, byte[] iv) => new LegacyCbcCipher(() => new TwofishEngine(), key, iv);

        public static LegacyCbcCipher Cast128(byte[] key, byte[] iv) => new LegacyCbcCipher(() => new Cast5Engine(), key, iv);

        private byte[] Process(BufferedBlockCipher cipher, byte[] input, int offset, int length)
        {
            if (length % BlockSize != 0)
            {
                throw new ArgumentException("The data is not a multiple of the block size.", nameof(length));
            }

            var output = new byte[length];
            int produced = cipher.ProcessBytes(input, offset, length, output, 0);
            if (produced != length)
            {
                throw new InvalidOperationException("The block cipher produced " + produced + " bytes for " + length + ".");
            }

            return output;
        }
    }

    /// <summary>The RC4 stream ciphers of SSH (arcfour, arcfour128, arcfour256), with BouncyCastle.</summary>
    internal sealed class Rc4Cipher : ISftpCipher
    {
        private readonly RC4Engine _engine = new RC4Engine();

        public Rc4Cipher(byte[] key, int discardBytes)
        {
            _engine.Init(true, new KeyParameter(key));
            if (discardBytes > 0)
            {
                // arcfour128 and arcfour256 (RFC 4345) discard the first 1536 bytes of the key stream.
                var discard = new byte[discardBytes];
                _engine.ProcessBytes(discard, 0, discard.Length, discard, 0);
            }
        }

        // SSH treats a stream cipher as having a block size of 8 for the packet padding.
        public int BlockSize => 8;

        public int TagSize => 0;

        public byte[] Encrypt(byte[] input, int offset, int length) => Process(input, offset, length);

        // RC4 is symmetric: the key stream, advanced in packet order, decrypts. The session uses one instance per direction.
        public byte[] Decrypt(byte[] input, int offset, int length) => Process(input, offset, length);

        private byte[] Process(byte[] input, int offset, int length)
        {
            var output = new byte[length];
            _engine.ProcessBytes(input, offset, length, output, 0);
            return output;
        }
    }
}
