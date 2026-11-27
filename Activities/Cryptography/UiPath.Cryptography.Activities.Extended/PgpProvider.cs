using System.IO;
using UiPath.Cryptography.Enums;

[assembly: UiPath.Cryptography.Pgp.PgpProvider(typeof(UiPath.Cryptography.Pgp.PgpProvider))]

namespace UiPath.Cryptography.Pgp
{
    /// <summary>OpenPGP with PgpCore and BouncyCastle.</summary>
    public sealed class PgpProvider : IPgpProvider
    {
        public byte[] Encrypt(byte[] inputBytes, Stream publicKeyStream, Stream privateKeyStream, string passphrase, bool sign)
            => PgpImplementation.PgpEncrypt(inputBytes, publicKeyStream, privateKeyStream, passphrase, sign);

        public void EncryptStream(Stream inputStream, Stream outputStream, Stream publicKeyStream, Stream privateKeyStream, string passphrase, bool sign)
            => PgpImplementation.PgpEncryptStream(inputStream, outputStream, publicKeyStream, privateKeyStream, passphrase, sign);

        public byte[] Decrypt(byte[] inputBytes, Stream privateKeyStream, string passphrase, Stream publicKeyStream, bool verifySignature)
            => PgpImplementation.PgpDecrypt(inputBytes, privateKeyStream, passphrase, publicKeyStream, verifySignature);

        public void DecryptStream(Stream inputStream, Stream outputStream, Stream privateKeyStream, string passphrase, Stream publicKeyStream, bool verifySignature)
            => PgpImplementation.PgpDecryptStream(inputStream, outputStream, privateKeyStream, passphrase, publicKeyStream, verifySignature);

        public string EncryptText(string input, Stream publicKeyStream, Stream privateKeyStream, string passphrase, bool sign)
            => PgpImplementation.PgpEncryptText(input, publicKeyStream, privateKeyStream, passphrase, sign);

        public string DecryptText(string input, Stream privateKeyStream, string passphrase, Stream publicKeyStream, bool verifySignature)
            => PgpImplementation.PgpDecryptText(input, privateKeyStream, passphrase, publicKeyStream, verifySignature);

        public void GenerateKeys(string publicKeyPath, string privateKeyPath, string username, string password, RsaKeySize keySize)
            => PgpImplementation.PgpGenerateKeys(publicKeyPath, privateKeyPath, username, password, keySize);

        public byte[] Sign(byte[] inputBytes, Stream privateKeyStream, string passphrase)
            => PgpImplementation.PgpSign(inputBytes, privateKeyStream, passphrase);

        public byte[] ClearSign(byte[] inputBytes, Stream privateKeyStream, string passphrase)
            => PgpImplementation.PgpClearSign(inputBytes, privateKeyStream, passphrase);

        public string SignText(string input, Stream privateKeyStream, string passphrase)
            => PgpImplementation.PgpSignText(input, privateKeyStream, passphrase);

        public string ClearSignText(string input, Stream privateKeyStream, string passphrase)
            => PgpImplementation.PgpClearSignText(input, privateKeyStream, passphrase);

        public bool Verify(byte[] inputBytes, Stream publicKeyStream)
            => PgpImplementation.PgpVerify(inputBytes, publicKeyStream);

        public bool VerifyClear(byte[] inputBytes, Stream publicKeyStream)
            => PgpImplementation.PgpVerifyClear(inputBytes, publicKeyStream);

        public bool VerifyText(string input, Stream publicKeyStream)
            => PgpImplementation.PgpVerifyText(input, publicKeyStream);

        public bool VerifyClearText(string input, Stream publicKeyStream)
            => PgpImplementation.PgpVerifyClearText(input, publicKeyStream);

        public bool VerifyPublicKey(Stream publicKeyStream)
            => PgpImplementation.PgpVerifyPublicKey(publicKeyStream);

    }
}
