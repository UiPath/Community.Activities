using System;
using System.Security.Cryptography;
using Renci.SshNet;
using UiPath.FTP.Extensibility;
using Xunit;

namespace UiPath.FTP.Tests
{
    public class SftpCryptoRegistryTests
    {
        private static readonly string[] EcdhNames = { "ecdh-sha2-nistp256", "ecdh-sha2-nistp384", "ecdh-sha2-nistp521" };

        private static ConnectionInfo NewConnectionInfo() =>
            new ConnectionInfo("localhost", 22, "user", new PasswordAuthenticationMethod("user", "password"));

        [Fact]
        public void Defaults_OfferOnlyOsBackedAlgorithms()
        {
            ConnectionInfo info = NewConnectionInfo();

            Assert.Equal(EcdhNames, info.KeyExchangeAlgorithms.Keys);
            Assert.DoesNotContain("ssh-ed25519", info.HostKeyAlgorithms.Keys);
            Assert.DoesNotContain("ssh-dss", info.HostKeyAlgorithms.Keys);
            Assert.DoesNotContain("chacha20-poly1305@openssh.com", info.Encryptions.Keys);
            Assert.DoesNotContain("blowfish-cbc", info.Encryptions.Keys);
            Assert.DoesNotContain("hmac-md5", info.HmacAlgorithms.Keys);
        }

        [Fact]
        public void ApplyTo_AddsAlgorithmsFromExtensions()
        {
            var extension = new FakeExtension();
            SftpCryptoRegistry registry = SftpCryptoRegistry.Create(new[] { extension });
            ConnectionInfo info = NewConnectionInfo();

            registry.ApplyTo(info);

            Assert.Contains("ssh-fake", info.HostKeyAlgorithms.Keys);
            Assert.Contains("fake-cbc", info.Encryptions.Keys);
            Assert.Equal(256, info.Encryptions["fake-cbc"].KeySize);
            Assert.Contains("hmac-md5", info.HmacAlgorithms.Keys);
        }

        [Fact]
        public void CreateKeyFileOptions_ReturnsBcryptKdfAndOpenSshKeyFactory()
        {
            var extension = new FakeExtension();
            SftpCryptoRegistry registry = SftpCryptoRegistry.Create(new[] { extension });

            PrivateKeyFileOptions options = registry.CreateKeyFileOptions();

            Assert.NotNull(options.BcryptPbkdf);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, options.BcryptPbkdf(Array.Empty<byte>(), Array.Empty<byte>(), 1, 4));
            Assert.True(options.KeyFactories.ContainsKey("ssh-fake"));
            Assert.Equal(8, options.KeyFactories["ssh-fake"](new byte[8], new byte[8]).FixedPublicKeyLength);
        }

        [Fact]
        public void CreateKeyFileOptions_IsPerRegistry_NothingIsShared()
        {
            PrivateKeyFileOptions withExtension = SftpCryptoRegistry.Create(new[] { new FakeExtension() }).CreateKeyFileOptions();
            PrivateKeyFileOptions without = SftpCryptoRegistry.Create(null).CreateKeyFileOptions();

            Assert.True(withExtension.KeyFactories.ContainsKey("ssh-fake"));
            Assert.Empty(without.KeyFactories);
            Assert.Null(without.BcryptPbkdf);
        }

        [Fact]
        public void Create_WithoutExtensions_ChangesNothing()
        {
            SftpCryptoRegistry registry = SftpCryptoRegistry.Create(null);
            ConnectionInfo info = NewConnectionInfo();
            int hostKeys = info.HostKeyAlgorithms.Count;
            int ciphers = info.Encryptions.Count;
            int macs = info.HmacAlgorithms.Count;

            registry.ApplyTo(info);

            Assert.Equal(hostKeys, info.HostKeyAlgorithms.Count);
            Assert.Equal(ciphers, info.Encryptions.Count);
            Assert.Equal(macs, info.HmacAlgorithms.Count);
        }

        [Fact]
        public void FtpConfiguration_HasNoCryptoExtensionsByDefault()
        {
            Assert.Empty(new FtpConfiguration("host").CryptoExtensions);
        }

        private sealed class FakeExtension : ISftpCryptoExtension
        {
            public string Name => "fake";

            public void Register(ISftpCryptoRegistry registry)
            {
                registry.AddSignatureAlgorithm(new FakeSignature());
                registry.AddCipher("fake-cbc", 256, (key, iv) => new FakeCipher());
                registry.AddMac("hmac-md5", 128, key => new HMACMD5(key));
                registry.SetBcryptPbkdf(new FakeKdf());
            }
        }

        private sealed class FakeSignature : ISftpSignatureAlgorithm
        {
            public string Name => "ssh-fake";
            public int PublicKeyLength => 8;
            public bool Verify(byte[] publicKey, byte[] data, byte[] signature) => true;
            public byte[] Sign(byte[] privateKey, byte[] data) => new byte[8];
        }

        private sealed class FakeCipher : ISftpCipher
        {
            public int BlockSize => 16;
            public int TagSize => 0;
            public byte[] Encrypt(byte[] input, int offset, int length) => new byte[length];
            public byte[] Decrypt(byte[] input, int offset, int length) => new byte[length];
        }

        private sealed class FakeKdf : ISftpBcryptPbkdf
        {
            public byte[] DeriveKey(byte[] passphrase, byte[] salt, int rounds, int outputLength) => new byte[] { 1, 2, 3, 4 };
        }
    }
}
