using System;
using System.Linq;
using Renci.SshNet;
using UiPath.FTP.Extensibility;
using Xunit;

namespace UiPath.FTP.Extended.Tests
{
    public class ExtensionRegistrationTests
    {
        private static readonly string[] EcdhNames = { "ecdh-sha2-nistp256", "ecdh-sha2-nistp384", "ecdh-sha2-nistp521" };
        private static readonly string[] Sha1ExchangeNames = { "diffie-hellman-group-exchange-sha1", "diffie-hellman-group14-sha1", "diffie-hellman-group1-sha1" };
        private static readonly string[] ArcfourNames = { "arcfour256", "arcfour128", "arcfour" };

        // What UiPath.FTP.Activities 3.1.3 (SSH.NET 2024.1.0) offered by default, read from its assembly. Core (operating
        // system crypto) plus the extended package must still offer all of it, so projects that upgrade keep reaching
        // the same servers. Certificate host keys are Core's.
        private static readonly string[] Ciphers =
        {
            "aes128-ctr", "aes192-ctr", "aes256-ctr", "aes128-gcm@openssh.com", "aes256-gcm@openssh.com", "aes128-cbc", "aes192-cbc", "aes256-cbc", "3des-cbc",
            "blowfish-cbc", "twofish-cbc", "twofish128-cbc", "twofish192-cbc", "twofish256-cbc", "cast128-cbc", "arcfour", "arcfour128", "arcfour256",
        };

        private static readonly string[] Macs =
        {
            "hmac-sha2-256", "hmac-sha2-512", "hmac-sha1", "hmac-sha2-256-etm@openssh.com", "hmac-sha2-512-etm@openssh.com", "hmac-sha1-etm@openssh.com",
            "hmac-md5", "hmac-md5-96", "hmac-md5-etm@openssh.com", "hmac-md5-96-etm@openssh.com", "hmac-sha1-96", "hmac-sha1-96-etm@openssh.com",
            "hmac-sha2-256-96", "hmac-sha2-512-96",
        };

        private static readonly string[] KeyExchanges =
        {
            "ecdh-sha2-nistp256", "ecdh-sha2-nistp384", "ecdh-sha2-nistp521", "curve25519-sha256", "curve25519-sha256@libssh.org",
            "diffie-hellman-group-exchange-sha256", "diffie-hellman-group16-sha512", "diffie-hellman-group14-sha256",
            "diffie-hellman-group-exchange-sha1", "diffie-hellman-group14-sha1", "diffie-hellman-group1-sha1",
        };

        private static readonly string[] HostKeys =
        {
            "ecdsa-sha2-nistp256", "ecdsa-sha2-nistp384", "ecdsa-sha2-nistp521", "rsa-sha2-256", "rsa-sha2-512", "ssh-rsa", "ssh-ed25519", "ssh-dss",
        };

        private static ConnectionInfo ConnectionInfoWithTheExtension()
        {
            var info = new ConnectionInfo("localhost", 22, "user", new PasswordAuthenticationMethod("user", "password"));
            SftpCryptoRegistry.Create(new ISftpCryptoExtension[] { new ExtendedSftpCryptoExtension() }).ApplyTo(info);
            return info;
        }

        [Fact]
        public void CorePlusExtended_OfferEverythingVersion313Offered()
        {
            ConnectionInfo info = ConnectionInfoWithTheExtension();

            Assert.Empty(Ciphers.Except(info.Encryptions.Keys));
            Assert.Empty(Macs.Except(info.HmacAlgorithms.Keys));
            Assert.Empty(KeyExchanges.Except(info.KeyExchangeAlgorithms.Keys));
            Assert.Empty(HostKeys.Except(info.HostKeyAlgorithms.Keys));
        }

        [Fact]
        public void KeyExchanges_KeepTheOperatingSystemBackedOnesFirstAndTheSha1OnesLast()
        {
            string[] names = ConnectionInfoWithTheExtension().KeyExchangeAlgorithms.Keys.ToArray();

            Assert.Equal(EcdhNames, names.Take(3));
            Assert.Equal(
                Sha1ExchangeNames,
                names.Skip(names.Length - 3));
        }

        [Fact]
        public void Ciphers_KeepTheOperatingSystemBackedOnesFirstAndRc4Last()
        {
            string[] names = ConnectionInfoWithTheExtension().Encryptions.Keys.ToArray();

            Assert.Equal("aes128-ctr", names.First());
            Assert.Equal(ArcfourNames, names.Skip(names.Length - 3));
        }

        [Theory]
        [InlineData("hmac-md5-etm@openssh.com", true)]
        [InlineData("hmac-md5-96-etm@openssh.com", true)]
        [InlineData("hmac-sha1-96-etm@openssh.com", true)]
        [InlineData("hmac-md5", false)]
        [InlineData("hmac-md5-96", false)]
        [InlineData("hmac-sha1-96", false)]
        [InlineData("hmac-sha2-256-96", false)]
        [InlineData("hmac-sha2-512-96", false)]
        public void Macs_HaveTheRightKindAndLength(string name, bool isEncryptThenMac)
        {
            HashInfo info = ConnectionInfoWithTheExtension().HmacAlgorithms[name];

            Assert.Equal(isEncryptThenMac, info.IsEncryptThenMAC);
            using (var mac = info.HashAlgorithm(new byte[64]))
            {
                Assert.Equal(name.Contains("-96") ? 96 : 128, mac.HashSize);
            }
        }

        [Fact]
        public void KeyFileOptions_LetEncryptedOpenSshAndEd25519KeysOpen()
        {
            var options = SftpCryptoRegistry.Create(new ISftpCryptoExtension[] { new ExtendedSftpCryptoExtension() }).CreateKeyFileOptions();

            Assert.NotNull(options.BcryptPbkdf);
            Assert.True(options.KeyFactories.ContainsKey("ssh-ed25519"));
        }

        [Fact]
        public void TheExtensionIsDiscoveredByTheAssemblyAttributeAndByItsWellKnownName()
        {
            var assembly = typeof(ExtendedSftpCryptoExtension).Assembly;

            var found = SftpCryptoExtensionDiscovery.Discover(new[] { assembly }, Array.Empty<string>(), name => throw new InvalidOperationException(name));

            Assert.IsType<ExtendedSftpCryptoExtension>(Assert.Single(found));
            Assert.Contains(assembly.GetName().Name, SftpCryptoExtensionDiscovery.WellKnownAssemblies);
        }

        [Fact]
        public void Register_RejectsANullRegistry()
        {
            Assert.Throws<ArgumentNullException>(() => new ExtendedSftpCryptoExtension().Register(null));
        }
    }
}
