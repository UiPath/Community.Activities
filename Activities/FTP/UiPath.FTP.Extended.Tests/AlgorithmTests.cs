using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Math.EC.Rfc8032;
using Org.BouncyCastle.Security;
using UiPath.FTP.Extensibility;
using Xunit;

namespace UiPath.FTP.Extended.Tests
{
    public class AlgorithmTests
    {
        private static byte[] Hex(string hex) => Convert.FromHexString(hex);

        // --- legacy block ciphers: published single block answers (CBC with a zero IV equals ECB for the first block)

        [Fact]
        public void Blowfish_MatchesTheKnownAnswer()
        {
            var cipher = LegacyCbcCipher.Blowfish(new byte[8], new byte[8]);

            Assert.Equal(Hex("4EF997456198DD78"), cipher.Encrypt(new byte[8], 0, 8));
            Assert.Equal(8, cipher.BlockSize);
        }

        [Fact]
        public void Twofish_MatchesTheKnownAnswer()
        {
            var cipher = LegacyCbcCipher.Twofish(new byte[16], new byte[16]);

            Assert.Equal(Hex("9F589F5CF6122C32B6BFEC2F2AE8C35A"), cipher.Encrypt(new byte[16], 0, 16));
            Assert.Equal(16, cipher.BlockSize);
        }

        [Fact]
        public void Cast128_MatchesTheKnownAnswer_Rfc2144()
        {
            var cipher = LegacyCbcCipher.Cast128(Hex("0123456712345678234567893456789A"), new byte[8]);

            Assert.Equal(Hex("238B4FE5847E44B2"), cipher.Encrypt(Hex("0123456789ABCDEF"), 0, 8));
        }

        [Theory]
        [InlineData("blowfish")]
        [InlineData("twofish")]
        [InlineData("cast128")]
        public void CbcCiphers_ChainAcrossPacketsAndDecryptWithAnotherInstance(string name)
        {
            byte[] key = Enumerable.Range(10, 16).Select(i => (byte)i).ToArray();
            byte[] iv = Enumerable.Range(100, 16).Select(i => (byte)i).ToArray();
            Func<byte[], byte[], ISftpCipher> create = (k, v) => name == "blowfish" ? LegacyCbcCipher.Blowfish(k, v) : name == "twofish" ? LegacyCbcCipher.Twofish(k, v) : LegacyCbcCipher.Cast128(k, v);
            byte[] plain = Enumerable.Range(0, 96).Select(i => (byte)(i * 7)).ToArray();

            // two packets one after the other equal one long packet: the chaining value carries over
            ISftpCipher whole = create(key, iv);
            byte[] expected = whole.Encrypt(plain, 0, plain.Length);
            ISftpCipher split = create(key, iv);
            byte[] cipherText = split.Encrypt(plain, 0, 48).Concat(split.Encrypt(plain, 48, 48)).ToArray();
            Assert.Equal(expected, cipherText);

            // the receiving side, a separate instance, decrypts the packets in order
            ISftpCipher receiver = create(key, iv);
            byte[] decrypted = receiver.Decrypt(cipherText, 0, 48).Concat(receiver.Decrypt(cipherText, 48, 48)).ToArray();
            Assert.Equal(plain, decrypted);
        }

        [Fact]
        public void CbcCiphers_RejectDataThatIsNotABlockMultiple()
        {
            var cipher = LegacyCbcCipher.Blowfish(new byte[16], new byte[8]);

            Assert.Throws<ArgumentException>(() => cipher.Encrypt(new byte[12], 0, 12));
        }

        // --- RC4

        [Fact]
        public void Rc4_MatchesTheKnownAnswer()
        {
            var cipher = new Rc4Cipher(Encoding.ASCII.GetBytes("Key"), discardBytes: 0);

            Assert.Equal(Hex("BBF316E8D940AF0AD3"), cipher.Encrypt(Encoding.ASCII.GetBytes("Plaintext"), 0, 9));
        }

        [Fact]
        public void Rc4_Arcfour128DiscardsTheFirst1536BytesOfTheKeyStream()
        {
            byte[] key = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray();
            byte[] data = new byte[32];

            byte[] plainStream = new Rc4Cipher(key, discardBytes: 0).Encrypt(new byte[1536 + 32], 0, 1536 + 32);
            byte[] discarded = new Rc4Cipher(key, discardBytes: 1536).Encrypt(data, 0, data.Length);

            Assert.Equal(plainStream.Skip(1536).ToArray(), discarded);
        }

        [Fact]
        public void Rc4_DecryptsWithAnotherInstanceInPacketOrder()
        {
            byte[] key = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
            byte[] plain = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray();
            var sender = new Rc4Cipher(key, 1536);
            var receiver = new Rc4Cipher(key, 1536);

            byte[] first = sender.Encrypt(plain, 0, 32);
            byte[] second = sender.Encrypt(plain, 32, 32);

            Assert.Equal(plain.Take(32).ToArray(), receiver.Decrypt(first, 0, 32));
            Assert.Equal(plain.Skip(32).ToArray(), receiver.Decrypt(second, 0, 32));
        }

        // --- key agreement

        [Fact]
        public void DiffieHellmanGroups_AreTheStandardGroups()
        {
            Assert.Equal(1024, DiffieHellmanGroups.Group1Prime.BitLength);
            Assert.Equal(2048, DiffieHellmanGroups.Group14Prime.BitLength);
            Assert.Equal(4096, DiffieHellmanGroups.Group16Prime.BitLength);
            Assert.All(new[] { DiffieHellmanGroups.Group1Prime, DiffieHellmanGroups.Group14Prime, DiffieHellmanGroups.Group16Prime },
                p => Assert.True(p.TestBit(0) && p.TestBit(p.BitLength - 1)));
            Assert.Equal(Org.BouncyCastle.Math.BigInteger.Two, DiffieHellmanGroups.Group14Generator);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(14)]
        [InlineData(16)]
        public void DiffieHellman_BothSidesComputeTheSameSharedKey(int group)
        {
            Org.BouncyCastle.Math.BigInteger p = group == 1 ? DiffieHellmanGroups.Group1Prime : group == 14 ? DiffieHellmanGroups.Group14Prime : DiffieHellmanGroups.Group16Prime;
            var client = new DiffieHellmanKeyAgreement(p, DiffieHellmanGroups.Group14Generator);
            var server = new DiffieHellmanKeyAgreement(p, DiffieHellmanGroups.Group14Generator);

            byte[] e = client.GenerateClientValue();
            byte[] f = server.GenerateClientValue();

            byte[] clientKey = client.ComputeSharedKey(f);
            byte[] serverKey = server.ComputeSharedKey(e);
            Assert.Equal(serverKey, clientKey);
            Assert.True(clientKey.Length > 64);
        }

        [Fact]
        public void DiffieHellman_RejectsInvalidServerValues()
        {
            var p = DiffieHellmanGroups.Group14Prime;
            foreach (byte[] invalid in new[] { new byte[] { 0 }, new byte[] { 1 }, p.Subtract(Org.BouncyCastle.Math.BigInteger.One).ToByteArrayUnsigned(), p.ToByteArrayUnsigned() })
            {
                var client = new DiffieHellmanKeyAgreement(p, DiffieHellmanGroups.Group14Generator);
                client.GenerateClientValue();
                Assert.ThrowsAny<Exception>(() => client.ComputeSharedKey(invalid));
            }
        }

        [Fact]
        public void DiffieHellman_RejectsATooSmallPrime()
        {
            Assert.Throws<ArgumentException>(() => new DiffieHellmanKeyAgreement(new Org.BouncyCastle.Math.BigInteger("23"), Org.BouncyCastle.Math.BigInteger.Two));
        }

        [Fact]
        public void DiffieHellman_ComputeBeforeGenerateThrows()
        {
            var agreement = new DiffieHellmanKeyAgreement(DiffieHellmanGroups.Group14Prime, DiffieHellmanGroups.Group14Generator);

            Assert.Throws<InvalidOperationException>(() => agreement.ComputeSharedKey(new byte[] { 5 }));
        }

        [Fact]
        public void X25519_BothSidesComputeTheSameSharedKey()
        {
            var client = new X25519KeyAgreement();
            var server = new X25519KeyAgreement();

            byte[] clientValue = client.GenerateClientValue();
            byte[] serverValue = server.GenerateClientValue();

            Assert.Equal(32, clientValue.Length);
            Assert.Equal(server.ComputeSharedKey(clientValue), client.ComputeSharedKey(serverValue));
        }

        // --- signatures

        [Fact]
        public void Ed25519_SignsAndVerifies()
        {
            var seed = new byte[32];
            var publicKey = new byte[32];
            Ed25519.GeneratePrivateKey(new SecureRandom(), seed);
            Ed25519.GeneratePublicKey(seed, 0, publicKey, 0);
            byte[] privateKey = seed.Concat(publicKey).ToArray();
            byte[] data = Encoding.UTF8.GetBytes("data to sign");
            var algorithm = new Ed25519SignatureAlgorithm();

            byte[] signature = algorithm.Sign(privateKey, data);

            Assert.Equal(64, signature.Length);
            Assert.True(algorithm.Verify(publicKey, data, signature));
            Assert.False(algorithm.Verify(publicKey, Encoding.UTF8.GetBytes("other data"), signature));
            Assert.False(algorithm.Verify(publicKey, data, signature.Take(63).ToArray()));
            Assert.False(algorithm.Verify(new byte[31], data, signature));
            Assert.Equal("ssh-ed25519", algorithm.Name);
            Assert.Equal(32, algorithm.PublicKeyLength);
        }

        private static byte[] Trim(byte[] value) => value.SkipWhile(b => b == 0).ToArray();

        [Fact]
        public void Dsa_VerifiesAHostKeySignature()
        {
            using (DSA dsa = DSA.Create(1024))
            {
                DSAParameters parameters = dsa.ExportParameters(false);
                byte[][] values = { Trim(parameters.P), Trim(parameters.Q), Trim(parameters.G), Trim(parameters.Y) };
                byte[] data = Encoding.UTF8.GetBytes("session hash");
                byte[] signature = dsa.SignData(data, HashAlgorithmName.SHA1, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
                var algorithm = new DsaHostKeyAlgorithm();

                Assert.Equal(40, signature.Length);
                Assert.True(algorithm.Verify(values, data, signature));
                Assert.False(algorithm.Verify(values, Encoding.UTF8.GetBytes("another"), signature));
                Assert.False(algorithm.Verify(values, data, signature.Take(39).ToArray()));
                Assert.False(algorithm.Verify(values.Take(3).ToArray(), data, signature));
                Assert.Equal("ssh-dss", algorithm.Name);
            }
        }

        [Fact]
        public void Bcrypt_DerivesTheSameKeyForTheSameInputAndDifferentKeysOtherwise()
        {
            var kdf = new OpenSshBcryptPbkdf();
            byte[] salt = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray();

            byte[] a = kdf.DeriveKey(Encoding.UTF8.GetBytes("pw123"), salt, 16, 48);
            byte[] b = kdf.DeriveKey(Encoding.UTF8.GetBytes("pw123"), salt, 16, 48);
            byte[] c = kdf.DeriveKey(Encoding.UTF8.GetBytes("pw124"), salt, 16, 48);

            Assert.Equal(48, a.Length);
            Assert.Equal(a, b);
            Assert.NotEqual(a, c);
        }
    }
}
