using System;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using UiPath.FTP.Extensibility;

namespace UiPath.FTP.Extended
{
    /// <summary>
    /// Diffie-Hellman over a fixed group (diffie-hellman-group1/14/16) or a group the server sends (group exchange),
    /// with BouncyCastle. Exchange values and the shared key are mpint encoded.
    /// </summary>
    internal sealed class DiffieHellmanKeyAgreement : ISftpKeyAgreement
    {
        private readonly DHParameters _parameters;
        private DHBasicAgreement _agreement;

        // The private exponent size: 512 bits give far more strength than the groups provide (OpenSSH uses the same
        // order of magnitude), and avoid the very slow full-size exponents for the 4096 bit and larger groups.
        private const int PrivateExponentBits = 512;

        public DiffieHellmanKeyAgreement(BigInteger prime, BigInteger generator)
        {
            if (prime == null || prime.BitLength < 512)
            {
                throw new ArgumentException("The Diffie-Hellman prime is too small.", nameof(prime));
            }

            _parameters = new DHParameters(prime, generator, null, 0, Math.Min(PrivateExponentBits, prime.BitLength - 1));
        }

        public byte[] GenerateClientValue()
        {
            var generator = new DHKeyPairGenerator();
            generator.Init(new DHKeyGenerationParameters(new SecureRandom(), _parameters));
            var pair = generator.GenerateKeyPair();

            _agreement = new DHBasicAgreement();
            _agreement.Init(pair.Private);
            return SftpKeyAgreementEncoding.ToMPInt(((DHPublicKeyParameters)pair.Public).Y.ToByteArrayUnsigned());
        }

        public byte[] ComputeSharedKey(byte[] serverValue)
        {
            if (_agreement == null)
            {
                throw new InvalidOperationException("GenerateClientValue must be called first.");
            }

            // The public key parameters check the value is in the valid range.
            var serverPublicKey = new DHPublicKeyParameters(new BigInteger(1, serverValue), _parameters);
            return SftpKeyAgreementEncoding.ToMPInt(_agreement.CalculateAgreement(serverPublicKey).ToByteArrayUnsigned());
        }

        public void Dispose()
        {
        }
    }
}
