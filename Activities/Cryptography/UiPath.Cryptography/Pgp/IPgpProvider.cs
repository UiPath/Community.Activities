using System.IO;
using UiPath.Cryptography.Enums;

namespace UiPath.Cryptography.Pgp
{
    /// <summary>
    /// The OpenPGP operations of the Cryptography activities. OpenPGP has no operating system equivalent, so the implementation is
    /// not part of this package: the extended package (<c>UiPath.Cryptography.Activities</c>) provides it and is found
    /// automatically (see <see cref="PgpProviderAttribute"/>).
    /// </summary>
    public interface IPgpProvider
    {
        byte[] Encrypt(byte[] inputBytes, Stream publicKeyStream, Stream privateKeyStream, string passphrase, bool sign);
        void EncryptStream(Stream inputStream, Stream outputStream, Stream publicKeyStream, Stream privateKeyStream, string passphrase, bool sign);
        byte[] Decrypt(byte[] inputBytes, Stream privateKeyStream, string passphrase, Stream publicKeyStream, bool verifySignature);
        void DecryptStream(Stream inputStream, Stream outputStream, Stream privateKeyStream, string passphrase, Stream publicKeyStream, bool verifySignature);
        string EncryptText(string input, Stream publicKeyStream, Stream privateKeyStream, string passphrase, bool sign);
        string DecryptText(string input, Stream privateKeyStream, string passphrase, Stream publicKeyStream, bool verifySignature);
        void GenerateKeys(string publicKeyPath, string privateKeyPath, string username, string password, RsaKeySize keySize);
        byte[] Sign(byte[] inputBytes, Stream privateKeyStream, string passphrase);
        byte[] ClearSign(byte[] inputBytes, Stream privateKeyStream, string passphrase);
        string SignText(string input, Stream privateKeyStream, string passphrase);
        string ClearSignText(string input, Stream privateKeyStream, string passphrase);
        bool Verify(byte[] inputBytes, Stream publicKeyStream);
        bool VerifyClear(byte[] inputBytes, Stream publicKeyStream);
        bool VerifyText(string input, Stream publicKeyStream);
        bool VerifyClearText(string input, Stream publicKeyStream);
        bool VerifyPublicKey(Stream publicKeyStream);
    }
}
