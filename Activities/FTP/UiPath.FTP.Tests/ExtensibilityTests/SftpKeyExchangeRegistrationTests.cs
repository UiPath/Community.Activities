using System;
using System.Linq;
using System.Security.Cryptography;
using Renci.SshNet;
using Renci.SshNet.Security;
using UiPath.FTP.Extensibility;
using Xunit;

namespace UiPath.FTP.Tests
{
    public class SftpKeyExchangeRegistrationTests
    {
        private static readonly string[] ExpectedExchanges = { "ecdh-sha2-nistp256", "ecdh-sha2-nistp384", "ecdh-sha2-nistp521", "curve25519-sha256", "diffie-hellman-group-exchange-sha256" };

        private static ConnectionInfo NewConnectionInfo() =>
            new ConnectionInfo("localhost", 22, "user", new PasswordAuthenticationMethod("user", "password"));

        private sealed class FakeAgreement : ISftpKeyAgreement
        {
            public byte[] GenerateClientValue() => new byte[] { 1 };

            public byte[] ComputeSharedKey(byte[] serverValue) => new byte[] { 2 };

            public void Dispose()
            {
            }
        }

        private sealed class KeyExchangeExtension : ISftpCryptoExtension
        {
            private readonly Action<ISftpCryptoRegistry> _register;

            public KeyExchangeExtension(Action<ISftpCryptoRegistry> register) => _register = register;

            public string Name => "test key exchanges";

            public void Register(ISftpCryptoRegistry registry) => _register(registry);
        }

        private static ConnectionInfo Apply(Action<ISftpCryptoRegistry> register)
        {
            SftpCryptoRegistry registry = SftpCryptoRegistry.Create(new[] { new KeyExchangeExtension(register) });
            ConnectionInfo info = NewConnectionInfo();
            registry.ApplyTo(info);
            return info;
        }

        [Fact]
        public void ApplyTo_AddsKeyExchangesAfterTheOsBackedDefaults()
        {
            ConnectionInfo info = Apply(r =>
            {
                r.AddKeyExchange("curve25519-sha256", HashAlgorithmName.SHA256, SftpKeyExchangeMessageType.Ecdh, () => new FakeAgreement());
                r.AddDiffieHellmanGroupExchange("diffie-hellman-group-exchange-sha256", HashAlgorithmName.SHA256, (p, g) => new FakeAgreement());
            });

            Assert.Equal(
                ExpectedExchanges,
                info.KeyExchangeAlgorithms.Keys.ToArray());
        }

        [Fact]
        public void Factories_CreateTheLibraryKeyExchangeClasses()
        {
            ConnectionInfo info = Apply(r =>
            {
                r.AddKeyExchange("diffie-hellman-group14-sha256", HashAlgorithmName.SHA256, SftpKeyExchangeMessageType.DiffieHellman, () => new FakeAgreement());
                r.AddKeyExchange("curve25519-sha256", HashAlgorithmName.SHA256, SftpKeyExchangeMessageType.Ecdh, () => new FakeAgreement());
                r.AddKeyExchange("mlkem768x25519-sha256", HashAlgorithmName.SHA256, SftpKeyExchangeMessageType.Hybrid, () => new FakeAgreement());
                r.AddDiffieHellmanGroupExchange("diffie-hellman-group-exchange-sha512", HashAlgorithmName.SHA512, (p, g) => new FakeAgreement());
            });

            foreach (string name in new[] { "diffie-hellman-group14-sha256", "curve25519-sha256", "mlkem768x25519-sha256" })
            {
                IKeyExchange exchange = info.KeyExchangeAlgorithms[name]();
                Assert.IsType<KeyExchangeKeyAgreement>(exchange);
                Assert.Equal(name, exchange.Name);
            }

            IKeyExchange groupExchange = info.KeyExchangeAlgorithms["diffie-hellman-group-exchange-sha512"]();
            Assert.IsType<KeyExchangeDiffieHellmanGroupExchange>(groupExchange);
            Assert.Equal("diffie-hellman-group-exchange-sha512", groupExchange.Name);
        }

        [Fact]
        public void Factories_CreateAnIndependentInstanceForEachConnectionAttempt()
        {
            ConnectionInfo info = Apply(r =>
                r.AddKeyExchange("curve25519-sha256", HashAlgorithmName.SHA256, SftpKeyExchangeMessageType.Ecdh, () => new FakeAgreement()));

            Assert.NotSame(info.KeyExchangeAlgorithms["curve25519-sha256"](), info.KeyExchangeAlgorithms["curve25519-sha256"]());
        }

        [Fact]
        public void Registrations_AreNotSharedBetweenRegistries()
        {
            ConnectionInfo withExchange = Apply(r =>
                r.AddKeyExchange("curve25519-sha256", HashAlgorithmName.SHA256, SftpKeyExchangeMessageType.Ecdh, () => new FakeAgreement()));
            ConnectionInfo without = NewConnectionInfo();
            SftpCryptoRegistry.Create(null).ApplyTo(without);

            Assert.Contains("curve25519-sha256", withExchange.KeyExchangeAlgorithms.Keys);
            Assert.DoesNotContain("curve25519-sha256", without.KeyExchangeAlgorithms.Keys);
        }

        [Fact]
        public void AddKeyExchange_UnsupportedHash_ThrowsWhenRegistered()
        {
            var registry = SftpCryptoRegistry.Create(null);

            Assert.Throws<ArgumentException>(() =>
                registry.AddKeyExchange("x", HashAlgorithmName.MD5, SftpKeyExchangeMessageType.Ecdh, () => new FakeAgreement()));
            Assert.Throws<ArgumentException>(() =>
                registry.AddDiffieHellmanGroupExchange("x", HashAlgorithmName.MD5, (p, g) => new FakeAgreement()));
        }

        [Fact]
        public void AddKeyExchange_NullArguments_Throw()
        {
            var registry = SftpCryptoRegistry.Create(null);

            Assert.Throws<ArgumentNullException>(() =>
                registry.AddKeyExchange(null, HashAlgorithmName.SHA256, SftpKeyExchangeMessageType.Ecdh, () => new FakeAgreement()));
            Assert.Throws<ArgumentNullException>(() =>
                registry.AddKeyExchange("x", HashAlgorithmName.SHA256, SftpKeyExchangeMessageType.Ecdh, null));
            Assert.Throws<ArgumentNullException>(() =>
                registry.AddDiffieHellmanGroupExchange(null, HashAlgorithmName.SHA256, (p, g) => new FakeAgreement()));
            Assert.Throws<ArgumentNullException>(() =>
                registry.AddDiffieHellmanGroupExchange("x", HashAlgorithmName.SHA256, null));
        }

        [Fact]
        public void AddKeyExchange_UnknownMessageType_Throws()
        {
            var registry = SftpCryptoRegistry.Create(null);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                registry.AddKeyExchange("x", HashAlgorithmName.SHA256, (SftpKeyExchangeMessageType)99, () => new FakeAgreement()));
        }

        [Theory]
        [InlineData(new byte[] { 0x7f }, new byte[] { 0x7f })]
        [InlineData(new byte[] { 0x80 }, new byte[] { 0x00, 0x80 })]
        [InlineData(new byte[] { 0x00, 0x00, 0x01 }, new byte[] { 0x01 })]
        [InlineData(new byte[] { 0x00, 0xff, 0x00 }, new byte[] { 0x00, 0xff, 0x00 })]
        [InlineData(new byte[] { 0x00, 0x00 }, new byte[0])]
        [InlineData(new byte[0], new byte[0])]
        public void ToMPInt_EncodesAnUnsignedBigEndianInteger(byte[] input, byte[] expected)
        {
            Assert.Equal(expected, SftpKeyAgreementEncoding.ToMPInt(input));
        }
    }
}
