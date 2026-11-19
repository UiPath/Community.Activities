using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using UiPath.FTP.Extensibility;

namespace UiPath.FTP.Extended
{
    /// <summary>curve25519-sha256 (RFC 8731) with BouncyCastle.</summary>
    internal sealed class X25519KeyAgreement : ISftpKeyAgreement
    {
        private readonly X25519Agreement _agreement = new X25519Agreement();

        public byte[] GenerateClientValue()
        {
            var generator = new X25519KeyPairGenerator();
            generator.Init(new X25519KeyGenerationParameters(new SecureRandom()));
            var pair = generator.GenerateKeyPair();
            _agreement.Init(pair.Private);
            return ((X25519PublicKeyParameters)pair.Public).GetEncoded();
        }

        public byte[] ComputeSharedKey(byte[] serverValue)
        {
            var secret = new byte[_agreement.AgreementSize];
            _agreement.CalculateAgreement(new X25519PublicKeyParameters(serverValue, 0), secret, 0);
            return SftpKeyAgreementEncoding.ToMPInt(secret);
        }

        public void Dispose()
        {
        }
    }
}
