using System;
using System.Security.Cryptography;
using Org.BouncyCastle.Math;
using UiPath.FTP.Extended.Internal;
using UiPath.FTP.Extensibility;

namespace UiPath.FTP.Extended
{
    /// <summary>
    /// The SFTP algorithms of UiPath.FTP.Activities 3.1.3 that the operating system crypto module cannot provide, so that
    /// projects upgrading from that version keep reaching the same servers. They are implemented with BouncyCastle (and
    /// the platform DSA for the ssh-dss host key). Connections that use one of them are not limited to operating system
    /// crypto. The algorithms the operating system provides stay preferred: these are added after them, legacy last.
    /// </summary>
    public sealed class ExtendedSftpCryptoExtension : ISftpCryptoExtension
    {
        public string Name => "UiPath.FTP.Activities extended algorithms";

        public void Register(ISftpCryptoRegistry registry)
        {
            ArgumentNullException.ThrowIfNull(registry);

            RegisterKeys(registry);
            RegisterKeyExchanges(registry);
            RegisterMacs(registry);
            RegisterCiphers(registry);
        }

        private static void RegisterKeys(ISftpCryptoRegistry registry)
        {
            registry.AddSignatureAlgorithm(new Ed25519SignatureAlgorithm());
            registry.AddHostKeyAlgorithm(new DsaHostKeyAlgorithm());
            registry.SetBcryptPbkdf(new OpenSshBcryptPbkdf());
        }

        private static void RegisterKeyExchanges(ISftpCryptoRegistry registry)
        {
            registry.AddKeyExchange("curve25519-sha256", HashAlgorithmName.SHA256, SftpKeyExchangeMessageType.Ecdh, () => new X25519KeyAgreement());
            registry.AddKeyExchange("curve25519-sha256@libssh.org", HashAlgorithmName.SHA256, SftpKeyExchangeMessageType.Ecdh, () => new X25519KeyAgreement());

            registry.AddDiffieHellmanGroupExchange("diffie-hellman-group-exchange-sha256", HashAlgorithmName.SHA256, GroupExchange);
            registry.AddKeyExchange("diffie-hellman-group16-sha512", HashAlgorithmName.SHA512, SftpKeyExchangeMessageType.DiffieHellman,
                () => new DiffieHellmanKeyAgreement(DiffieHellmanGroups.Group16Prime, DiffieHellmanGroups.Group16Generator));
            registry.AddKeyExchange("diffie-hellman-group14-sha256", HashAlgorithmName.SHA256, SftpKeyExchangeMessageType.DiffieHellman,
                () => new DiffieHellmanKeyAgreement(DiffieHellmanGroups.Group14Prime, DiffieHellmanGroups.Group14Generator));

            // SHA-1 based exchanges, last
            registry.AddDiffieHellmanGroupExchange("diffie-hellman-group-exchange-sha1", HashAlgorithmName.SHA1, GroupExchange);
            registry.AddKeyExchange("diffie-hellman-group14-sha1", HashAlgorithmName.SHA1, SftpKeyExchangeMessageType.DiffieHellman,
                () => new DiffieHellmanKeyAgreement(DiffieHellmanGroups.Group14Prime, DiffieHellmanGroups.Group14Generator));
            registry.AddKeyExchange("diffie-hellman-group1-sha1", HashAlgorithmName.SHA1, SftpKeyExchangeMessageType.DiffieHellman,
                () => new DiffieHellmanKeyAgreement(DiffieHellmanGroups.Group1Prime, DiffieHellmanGroups.Group1Generator));
        }

        // The arguments are the mpint content of p and g sent by the server.
        private static ISftpKeyAgreement GroupExchange(byte[] p, byte[] g) =>
            new DiffieHellmanKeyAgreement(new BigInteger(1, p), new BigInteger(1, g));

        private static void RegisterMacs(ISftpCryptoRegistry registry)
        {
            // Truncated to 96 bits
            registry.AddMac("hmac-sha2-256-96", 256, key => new TruncatedHmac(new HMACSHA256(key), 12));
            registry.AddMac("hmac-sha2-512-96", 512, key => new TruncatedHmac(new HMACSHA512(key), 12));
            // The SHA-1 and MD5 MACs below are legacy SSH algorithms that older servers still require. They are offered
            // last, only by this extended package (never by the core package), hence the NOSONAR markers.
            registry.AddMac("hmac-sha1-96", 160, key => new TruncatedHmac(new HMACSHA1(key), 12)); // NOSONAR - legacy SSH MAC, offered last
            registry.AddMac("hmac-sha1-96-etm@openssh.com", 160, key => new TruncatedHmac(new HMACSHA1(key), 12), isEncryptThenMac: true); // NOSONAR - legacy SSH MAC, offered last

            // MD5, last
            registry.AddMac("hmac-md5", 128, key => new HMACMD5(key)); // NOSONAR - legacy SSH MAC, offered last
            registry.AddMac("hmac-md5-96", 128, key => new TruncatedHmac(new HMACMD5(key), 12)); // NOSONAR - legacy SSH MAC, offered last
            registry.AddMac("hmac-md5-etm@openssh.com", 128, key => new HMACMD5(key), isEncryptThenMac: true); // NOSONAR - legacy SSH MAC, offered last
            registry.AddMac("hmac-md5-96-etm@openssh.com", 128, key => new TruncatedHmac(new HMACMD5(key), 12), isEncryptThenMac: true); // NOSONAR - legacy SSH MAC, offered last
        }

        private static void RegisterCiphers(ISftpCryptoRegistry registry)
        {
            registry.AddCipher("twofish256-cbc", 256, LegacyCbcCipher.Twofish);
            registry.AddCipher("twofish-cbc", 256, LegacyCbcCipher.Twofish);
            registry.AddCipher("twofish192-cbc", 192, LegacyCbcCipher.Twofish);
            registry.AddCipher("twofish128-cbc", 128, LegacyCbcCipher.Twofish);
            registry.AddCipher("blowfish-cbc", 128, LegacyCbcCipher.Blowfish);
            registry.AddCipher("cast128-cbc", 128, LegacyCbcCipher.Cast128);

            // RC4, last
            registry.AddCipher("arcfour256", 256, (key, iv) => new Rc4Cipher(key, discardBytes: 1536));
            registry.AddCipher("arcfour128", 128, (key, iv) => new Rc4Cipher(key, discardBytes: 1536));
            registry.AddCipher("arcfour", 128, (key, iv) => new Rc4Cipher(key, discardBytes: 0));
        }
    }
}
