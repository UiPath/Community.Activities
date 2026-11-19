using Org.BouncyCastle.Math.EC.Rfc8032;
using UiPath.FTP.Extensibility;

namespace UiPath.FTP.Extended
{
    /// <summary>The <c>ssh-ed25519</c> host key and public key authentication algorithm.</summary>
    internal sealed class Ed25519SignatureAlgorithm : ISftpSignatureAlgorithm
    {
        public string Name => "ssh-ed25519";

        public int PublicKeyLength => 32;

        public bool Verify(byte[] publicKey, byte[] data, byte[] signature)
        {
            if (publicKey == null || publicKey.Length != 32 || signature == null || signature.Length != 64)
            {
                return false;
            }

            return Ed25519.Verify(signature, 0, publicKey, 0, data, 0, data.Length);
        }

        public byte[] Sign(byte[] privateKey, byte[] data)
        {
            // OpenSSH stores the private key as the seed (32 bytes) followed by the public key (32 bytes).
            var signature = new byte[64];
            Ed25519.Sign(privateKey, 0, privateKey, 32, null, data, 0, data.Length, signature, 0);
            return signature;
        }
    }
}
