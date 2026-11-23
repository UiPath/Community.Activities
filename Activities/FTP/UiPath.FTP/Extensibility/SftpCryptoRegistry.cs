using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using Renci.SshNet;
using Renci.SshNet.Security;
using Renci.SshNet.Security.Cryptography;

namespace UiPath.FTP.Extensibility
{
    /// <summary>Collects the registrations of the extensions of one connection and applies them to SSH.NET.</summary>
    internal sealed class SftpCryptoRegistry : ISftpCryptoRegistry
    {
        private readonly List<ISftpSignatureAlgorithm> _signatures = new List<ISftpSignatureAlgorithm>();
        private readonly List<(string Name, int KeySizeBits, Func<byte[], byte[], ISftpCipher> Factory, bool IsAead)> _ciphers = new List<(string, int, Func<byte[], byte[], ISftpCipher>, bool)>();
        private readonly List<(string Name, int KeySizeBits, Func<byte[], HMAC> Factory, bool IsEncryptThenMac)> _macs = new List<(string, int, Func<byte[], HMAC>, bool)>();
        private readonly List<ISftpHostKeyAlgorithm> _hostKeys = new List<ISftpHostKeyAlgorithm>();
        private readonly List<(string Name, Func<IKeyExchange> Factory)> _keyExchanges = new List<(string, Func<IKeyExchange>)>();
        private ISftpBcryptPbkdf _kdf;

        public static SftpCryptoRegistry Create(IEnumerable<ISftpCryptoExtension> extensions)
        {
            var registry = new SftpCryptoRegistry();
            if (extensions != null)
            {
                foreach (var extension in extensions)
                {
                    extension?.Register(registry);
                }
            }

            return registry;
        }

        public void AddSignatureAlgorithm(ISftpSignatureAlgorithm algorithm) => _signatures.Add(algorithm ?? throw new ArgumentNullException(nameof(algorithm)));

        public void AddCipher(string name, int keySizeBits, Func<byte[], byte[], ISftpCipher> factory) => AddCipher(name, keySizeBits, factory, isAead: false);

        public void AddCipher(string name, int keySizeBits, Func<byte[], byte[], ISftpCipher> factory, bool isAead) =>
            _ciphers.Add((name ?? throw new ArgumentNullException(nameof(name)), keySizeBits, factory ?? throw new ArgumentNullException(nameof(factory)), isAead));

        public void AddMac(string name, int keySizeBits, Func<byte[], HMAC> factory) => AddMac(name, keySizeBits, factory, isEncryptThenMac: false);

        public void AddMac(string name, int keySizeBits, Func<byte[], HMAC> factory, bool isEncryptThenMac) =>
            _macs.Add((name ?? throw new ArgumentNullException(nameof(name)), keySizeBits, factory ?? throw new ArgumentNullException(nameof(factory)), isEncryptThenMac));

        public void AddHostKeyAlgorithm(ISftpHostKeyAlgorithm algorithm) => _hostKeys.Add(algorithm ?? throw new ArgumentNullException(nameof(algorithm)));

        public void SetBcryptPbkdf(ISftpBcryptPbkdf kdf) => _kdf = kdf ?? throw new ArgumentNullException(nameof(kdf));

        public void AddKeyExchange(string name, HashAlgorithmName hashAlgorithm, SftpKeyExchangeMessageType messageType, Func<ISftpKeyAgreement> factory)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(factory);

            var messages = ToMessageType(messageType);
            Func<IKeyAgreement> agreementFactory = () => new KeyAgreementAdapter(factory());

            // The constructor checks the hash algorithm; do it now, not when a server selects the exchange.
            _ = new KeyExchangeKeyAgreement(name, hashAlgorithm, messages, agreementFactory);

            _keyExchanges.Add((name, () => new KeyExchangeKeyAgreement(name, hashAlgorithm, messages, agreementFactory)));
        }

        public void AddDiffieHellmanGroupExchange(string name, HashAlgorithmName hashAlgorithm, Func<byte[], byte[], ISftpKeyAgreement> factory)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(factory);

            Func<byte[], byte[], IKeyAgreement> agreementFactory = (p, g) => new KeyAgreementAdapter(factory(p, g));

            // The constructor checks the hash algorithm; do it now, not when a server selects the exchange.
            _ = new KeyExchangeDiffieHellmanGroupExchange(name, hashAlgorithm, agreementFactory);

            _keyExchanges.Add((name, () => new KeyExchangeDiffieHellmanGroupExchange(name, hashAlgorithm, agreementFactory)));
        }

        private static KeyExchangeMessageType ToMessageType(SftpKeyExchangeMessageType messageType)
        {
            switch (messageType)
            {
                case SftpKeyExchangeMessageType.DiffieHellman: return KeyExchangeMessageType.DiffieHellman;
                case SftpKeyExchangeMessageType.Ecdh: return KeyExchangeMessageType.Ecdh;
                case SftpKeyExchangeMessageType.Hybrid: return KeyExchangeMessageType.Hybrid;
                default: throw new ArgumentOutOfRangeException(nameof(messageType));
            }
        }

        /// <summary>
        /// Creates the options for reading a private key file: the key factories and the bcrypt KDF of the extensions.
        /// The options belong to the key file they are passed to; nothing is shared between connections.
        /// </summary>
        /// <returns>The options for <see cref="PrivateKeyFile"/>.</returns>
        public PrivateKeyFileOptions CreateKeyFileOptions()
        {
            var options = new PrivateKeyFileOptions();

            foreach (var algorithm in _signatures)
            {
                var captured = algorithm;
                options.KeyFactories[captured.Name] = (publicKey, privateKey) => new ExternalKey(captured, publicKey, privateKey);
            }

            if (_kdf != null)
            {
                var kdf = _kdf;
                options.BcryptPbkdf = (passphrase, salt, rounds, length) => kdf.DeriveKey(passphrase, salt, rounds, length);
            }

            return options;
        }

        /// <summary>Applies the algorithms to the connection.</summary>
        /// <param name="connectionInfo">The connection to add the algorithms to.</param>
        public void ApplyTo(ConnectionInfo connectionInfo)
        {
            foreach (var algorithm in _signatures)
            {
                var captured = algorithm;
                connectionInfo.HostKeyAlgorithms[captured.Name] = data =>
                {
                    var keyData = new SshKeyData(data);
                    var publicKey = keyData.Keys[0].ToByteArray(isUnsigned: true, isBigEndian: true);
                    var key = new ExternalKey(captured, PadTo(publicKey, captured.PublicKeyLength), privateKey: null);
                    return new KeyHostAlgorithm(captured.Name, key);
                };
            }

            foreach (var (name, keySizeBits, factory, isAead) in _ciphers)
            {
                var captured = factory;
                connectionInfo.Encryptions[name] = new CipherInfo(keySizeBits, (key, iv) => new ExternalCipher(captured(key, iv)), isAead);
            }

            foreach (var (name, keySizeBits, factory, isEncryptThenMac) in _macs)
            {
                var captured = factory;
                connectionInfo.HmacAlgorithms[name] = new HashInfo(keySizeBits, key => captured(key), isEncryptThenMac);
            }

            foreach (var algorithm in _hostKeys)
            {
                var captured = algorithm;
                connectionInfo.HostKeyAlgorithms[captured.Name] = data =>
                {
                    var keyData = new SshKeyData(data);
                    return new KeyHostAlgorithm(captured.Name, new ExternalMultiValueKey(captured, keyData.Keys));
                };
            }

            // Added after the operating system backed exchanges, so those stay preferred when the server offers them.
            foreach (var (name, factory) in _keyExchanges)
            {
                connectionInfo.KeyExchangeAlgorithms[name] = factory;
            }
        }

        private static byte[] PadTo(byte[] value, int length)
        {
            if (value.Length >= length)
            {
                return value;
            }

            var padded = new byte[length];
            Buffer.BlockCopy(value, 0, padded, length - value.Length, value.Length);
            return padded;
        }

        private sealed class ExternalMultiValueKey : Key
        {
            private readonly BigInteger[] _values;
            private readonly MultiValueSignature _signature;

            public ExternalMultiValueKey(ISftpHostKeyAlgorithm algorithm, BigInteger[] values)
            {
                _values = values;
                _signature = new MultiValueSignature(algorithm, values);
            }

            protected override DigitalSignature DigitalSignature => _signature;

            public override BigInteger[] Public => _values;

            public override int KeyLength => _values.Length == 0 ? 0 : (int)_values[0].GetBitLength();
        }

        private sealed class MultiValueSignature : DigitalSignature
        {
            private readonly ISftpHostKeyAlgorithm _algorithm;
            private readonly byte[][] _values;

            public MultiValueSignature(ISftpHostKeyAlgorithm algorithm, BigInteger[] values)
            {
                _algorithm = algorithm;
                _values = values.Select(v => v.ToByteArray(isUnsigned: true, isBigEndian: true)).ToArray();
            }

            public override bool Verify(byte[] input, byte[] signature) => _algorithm.Verify(_values, input, signature);

            public override byte[] Sign(byte[] input) => throw new InvalidOperationException("This host key algorithm verifies signatures only.");
        }

        private sealed class KeyAgreementAdapter : IKeyAgreement
        {
            private readonly ISftpKeyAgreement _inner;

            public KeyAgreementAdapter(ISftpKeyAgreement inner)
            {
                _inner = inner ?? throw new InvalidOperationException("The key agreement factory returned null.");
            }

            public byte[] GenerateClientValue() => _inner.GenerateClientValue();

            public byte[] ComputeSharedKey(byte[] serverValue) => _inner.ComputeSharedKey(serverValue);

            public void Dispose() => _inner.Dispose();
        }

        private sealed class ExternalKey : Key
        {
            private readonly ISftpSignatureAlgorithm _algorithm;
            private readonly byte[] _publicKey;
            private readonly ExternalSignature _signature;

            public ExternalKey(ISftpSignatureAlgorithm algorithm, byte[] publicKey, byte[] privateKey)
            {
                _algorithm = algorithm;
                _publicKey = publicKey;
                _signature = new ExternalSignature(algorithm, publicKey, privateKey);
            }

            protected override DigitalSignature DigitalSignature => _signature;

            public override int? FixedPublicKeyLength => _algorithm.PublicKeyLength;

            public override BigInteger[] Public => new[] { new BigInteger(_publicKey, isUnsigned: true, isBigEndian: true) };

            public override int KeyLength => _publicKey.Length * 8;

            public override string ToString() => _algorithm.Name;
        }

        private sealed class ExternalSignature : DigitalSignature
        {
            private readonly ISftpSignatureAlgorithm _algorithm;
            private readonly byte[] _publicKey;
            private readonly byte[] _privateKey;

            public ExternalSignature(ISftpSignatureAlgorithm algorithm, byte[] publicKey, byte[] privateKey)
            {
                _algorithm = algorithm;
                _publicKey = publicKey;
                _privateKey = privateKey;
            }

            public override bool Verify(byte[] input, byte[] signature) => _algorithm.Verify(_publicKey, input, signature);

            public override byte[] Sign(byte[] input) =>
                _privateKey == null
                    ? throw new InvalidOperationException("This key has no private part and cannot sign.")
                    : _algorithm.Sign(_privateKey, input);
        }

        private sealed class ExternalCipher : SymmetricCipher
        {
            private readonly ISftpCipher _cipher;

            public ExternalCipher(ISftpCipher cipher)
                : base(new byte[1])
            {
                _cipher = cipher;
            }

            public override byte MinimumSize => (byte)_cipher.BlockSize;

            public override int TagSize => _cipher.TagSize;

            public override byte[] Encrypt(byte[] input, int offset, int length) => _cipher.Encrypt(input, offset, length);

            public override byte[] Decrypt(byte[] input, int offset, int length) => _cipher.Decrypt(input, offset, length);
        }
    }
}
