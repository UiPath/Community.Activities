using System;
using System.IO;
using System.Net;
using System.Security;
using System.Text;
using UiPath.Cryptography.Enums;

namespace UiPath.Cryptography.Activities.API
{
    internal class CryptographyService : ICryptographyService
    {
        // ── Symmetric: string key ────────────────────────────────────────────

        public string EncryptText(string input, EncryptionAlgorithm algorithm, string key, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(encoding);
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key must not be null or empty.", nameof(key));

            byte[] keyBytes = CryptographyHelper.KeyEncoding(encoding, key, null);
            byte[] inputBytes = encoding.GetBytes(input);
            byte[] encryptedBytes = CryptographyHelper.EncryptData(algorithm, inputBytes, keyBytes);
            return Convert.ToBase64String(encryptedBytes);
        }

        public string DecryptText(string input, EncryptionAlgorithm algorithm, string key, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(encoding);
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key must not be null or empty.", nameof(key));

            byte[] keyBytes = CryptographyHelper.KeyEncoding(encoding, key, null);
            byte[] inputBytes = Convert.FromBase64String(input);
            byte[] decryptedBytes = CryptographyHelper.DecryptData(algorithm, inputBytes, keyBytes);
            return encoding.GetString(decryptedBytes);
        }

        public void EncryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, string key, Encoding encoding, bool overwrite)
        {
            if (string.IsNullOrWhiteSpace(inputFilePath))
                throw new ArgumentException("Input file path must not be null or empty.", nameof(inputFilePath));
            if (string.IsNullOrWhiteSpace(outputFilePath))
                throw new ArgumentException("Output file path must not be null or empty.", nameof(outputFilePath));
            ArgumentNullException.ThrowIfNull(encoding);
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key must not be null or empty.", nameof(key));

            if (!overwrite && File.Exists(outputFilePath))
                throw new InvalidOperationException($"Output file already exists: {outputFilePath}");

            byte[] keyBytes = CryptographyHelper.KeyEncoding(encoding, key, null);
            byte[] inputBytes = File.ReadAllBytes(inputFilePath);
            byte[] encryptedBytes = CryptographyHelper.EncryptData(algorithm, inputBytes, keyBytes);
            File.WriteAllBytes(outputFilePath, encryptedBytes);
        }

        public void DecryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, string key, Encoding encoding, bool overwrite)
        {
            if (string.IsNullOrWhiteSpace(inputFilePath))
                throw new ArgumentException("Input file path must not be null or empty.", nameof(inputFilePath));
            if (string.IsNullOrWhiteSpace(outputFilePath))
                throw new ArgumentException("Output file path must not be null or empty.", nameof(outputFilePath));
            ArgumentNullException.ThrowIfNull(encoding);
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key must not be null or empty.", nameof(key));

            if (!overwrite && File.Exists(outputFilePath))
                throw new InvalidOperationException($"Output file already exists: {outputFilePath}");

            byte[] keyBytes = CryptographyHelper.KeyEncoding(encoding, key, null);
            byte[] inputBytes = File.ReadAllBytes(inputFilePath);
            byte[] decryptedBytes = CryptographyHelper.DecryptData(algorithm, inputBytes, keyBytes);
            File.WriteAllBytes(outputFilePath, decryptedBytes);
        }

        // ── Symmetric: SecureString key ──────────────────────────────────────

        public string EncryptText(string input, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(key);
            return EncryptText(input, algorithm, SecureStringToString(key), encoding);
        }

        public string DecryptText(string input, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(key);
            return DecryptText(input, algorithm, SecureStringToString(key), encoding);
        }

        public void EncryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding, bool overwrite)
        {
            ArgumentNullException.ThrowIfNull(key);
            EncryptFile(inputFilePath, outputFilePath, algorithm, SecureStringToString(key), encoding, overwrite);
        }

        public void DecryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding, bool overwrite)
        {
            ArgumentNullException.ThrowIfNull(key);
            DecryptFile(inputFilePath, outputFilePath, algorithm, SecureStringToString(key), encoding, overwrite);
        }

        // ── Symmetric: byte[] key ─────────────────────────────────────────────

        public string EncryptText(string input, EncryptionAlgorithm algorithm, byte[] keyBytes, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(encoding);
            if (keyBytes is null || keyBytes.Length == 0)
                throw new ArgumentException("Key bytes must not be null or empty.", nameof(keyBytes));

            byte[] inputBytes = encoding.GetBytes(input);
            byte[] encryptedBytes = CryptographyHelper.EncryptData(algorithm, inputBytes, keyBytes);
            return Convert.ToBase64String(encryptedBytes);
        }

        public string DecryptText(string input, EncryptionAlgorithm algorithm, byte[] keyBytes, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(encoding);
            if (keyBytes is null || keyBytes.Length == 0)
                throw new ArgumentException("Key bytes must not be null or empty.", nameof(keyBytes));

            byte[] inputBytes = Convert.FromBase64String(input);
            byte[] decryptedBytes = CryptographyHelper.DecryptData(algorithm, inputBytes, keyBytes);
            return encoding.GetString(decryptedBytes);
        }

        public void EncryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, byte[] keyBytes, bool overwrite)
        {
            if (string.IsNullOrWhiteSpace(inputFilePath))
                throw new ArgumentException("Input file path must not be null or empty.", nameof(inputFilePath));
            if (string.IsNullOrWhiteSpace(outputFilePath))
                throw new ArgumentException("Output file path must not be null or empty.", nameof(outputFilePath));
            if (keyBytes is null || keyBytes.Length == 0)
                throw new ArgumentException("Key bytes must not be null or empty.", nameof(keyBytes));

            if (!overwrite && File.Exists(outputFilePath))
                throw new InvalidOperationException($"Output file already exists: {outputFilePath}");

            byte[] inputBytes = File.ReadAllBytes(inputFilePath);
            byte[] encryptedBytes = CryptographyHelper.EncryptData(algorithm, inputBytes, keyBytes);
            File.WriteAllBytes(outputFilePath, encryptedBytes);
        }

        public void DecryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, byte[] keyBytes, bool overwrite)
        {
            if (string.IsNullOrWhiteSpace(inputFilePath))
                throw new ArgumentException("Input file path must not be null or empty.", nameof(inputFilePath));
            if (string.IsNullOrWhiteSpace(outputFilePath))
                throw new ArgumentException("Output file path must not be null or empty.", nameof(outputFilePath));
            if (keyBytes is null || keyBytes.Length == 0)
                throw new ArgumentException("Key bytes must not be null or empty.", nameof(keyBytes));

            if (!overwrite && File.Exists(outputFilePath))
                throw new InvalidOperationException($"Output file already exists: {outputFilePath}");

            byte[] inputBytes = File.ReadAllBytes(inputFilePath);
            byte[] decryptedBytes = CryptographyHelper.DecryptData(algorithm, inputBytes, keyBytes);
            File.WriteAllBytes(outputFilePath, decryptedBytes);
        }

        // ── Keyed hash: string key ────────────────────────────────────────────

        public string KeyedHashText(string input, KeyedHashAlgorithms algorithm, string key, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(encoding);
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key must not be null or empty.", nameof(key));

            byte[] keyBytes = CryptographyHelper.KeyEncoding(encoding, key, null);
            byte[] inputBytes = encoding.GetBytes(input);
            byte[] hashBytes = CryptographyHelper.HashDataWithKey(algorithm, inputBytes, keyBytes);
            return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
        }

        public string KeyedHashFile(string filePath, KeyedHashAlgorithms algorithm, string key, Encoding encoding)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path must not be null or empty.", nameof(filePath));
            ArgumentNullException.ThrowIfNull(encoding);
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key must not be null or empty.", nameof(key));

            byte[] keyBytes = CryptographyHelper.KeyEncoding(encoding, key, null);
            byte[] inputBytes = File.ReadAllBytes(filePath);
            byte[] hashBytes = CryptographyHelper.HashDataWithKey(algorithm, inputBytes, keyBytes);
            return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
        }

        // ── Keyed hash: SecureString key ──────────────────────────────────────

        public string KeyedHashText(string input, KeyedHashAlgorithms algorithm, SecureString key, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(key);
            return KeyedHashText(input, algorithm, SecureStringToString(key), encoding);
        }

        public string KeyedHashFile(string filePath, KeyedHashAlgorithms algorithm, SecureString key, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(key);
            return KeyedHashFile(filePath, algorithm, SecureStringToString(key), encoding);
        }

        // ── Keyed hash: byte[] key ────────────────────────────────────────────

        public string KeyedHashText(string input, KeyedHashAlgorithms algorithm, byte[] keyBytes, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(encoding);
            if (keyBytes is null || keyBytes.Length == 0)
                throw new ArgumentException("Key bytes must not be null or empty.", nameof(keyBytes));

            byte[] inputBytes = encoding.GetBytes(input);
            byte[] hashBytes = CryptographyHelper.HashDataWithKey(algorithm, inputBytes, keyBytes);
            return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
        }

        public string KeyedHashFile(string filePath, KeyedHashAlgorithms algorithm, byte[] keyBytes)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path must not be null or empty.", nameof(filePath));
            if (keyBytes is null || keyBytes.Length == 0)
                throw new ArgumentException("Key bytes must not be null or empty.", nameof(keyBytes));

            byte[] inputBytes = File.ReadAllBytes(filePath);
            byte[] hashBytes = CryptographyHelper.HashDataWithKey(algorithm, inputBytes, keyBytes);
            return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
        }

        // ── PGP: string passphrase ─────────────────────────────────────────────

        public byte[] PgpEncrypt(byte[] inputBytes, Stream publicKeyStream, Stream privateKeyStream = null, string passphrase = null, bool sign = false)
        {
            ArgumentNullException.ThrowIfNull(inputBytes);
            ArgumentNullException.ThrowIfNull(publicKeyStream);

            return CryptographyHelper.PgpEncrypt(inputBytes, publicKeyStream, privateKeyStream, passphrase, sign);
        }

        public byte[] PgpDecrypt(byte[] inputBytes, Stream privateKeyStream, string passphrase, Stream publicKeyStream = null, bool verifySignature = false)
        {
            ArgumentNullException.ThrowIfNull(inputBytes);
            ArgumentNullException.ThrowIfNull(privateKeyStream);

            return CryptographyHelper.PgpDecrypt(inputBytes, privateKeyStream, passphrase, publicKeyStream, verifySignature);
        }

        public string PgpEncryptText(string input, Stream publicKeyStream, Stream privateKeyStream = null, string passphrase = null, bool sign = false)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(publicKeyStream);

            return CryptographyHelper.PgpEncryptText(input, publicKeyStream, privateKeyStream, passphrase, sign);
        }

        public string PgpDecryptText(string input, Stream privateKeyStream, string passphrase, Stream publicKeyStream = null, bool verifySignature = false)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(privateKeyStream);

            return CryptographyHelper.PgpDecryptText(input, privateKeyStream, passphrase, publicKeyStream, verifySignature);
        }

        public byte[] PgpSignFile(byte[] inputBytes, Stream privateKeyStream, string passphrase)
        {
            ArgumentNullException.ThrowIfNull(inputBytes);
            ArgumentNullException.ThrowIfNull(privateKeyStream);

            return CryptographyHelper.PgpSign(inputBytes, privateKeyStream, passphrase);
        }

        public byte[] PgpClearSignFile(byte[] inputBytes, Stream privateKeyStream, string passphrase)
        {
            ArgumentNullException.ThrowIfNull(inputBytes);
            ArgumentNullException.ThrowIfNull(privateKeyStream);

            return CryptographyHelper.PgpClearSign(inputBytes, privateKeyStream, passphrase);
        }

        // ── PGP: SecureString passphrase ──────────────────────────────────────

        public byte[] PgpEncrypt(byte[] inputBytes, Stream publicKeyStream, Stream privateKeyStream, SecureString passphrase, bool sign = false)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            return PgpEncrypt(inputBytes, publicKeyStream, privateKeyStream, SecureStringToString(passphrase), sign);
        }

        public byte[] PgpDecrypt(byte[] inputBytes, Stream privateKeyStream, SecureString passphrase, Stream publicKeyStream = null, bool verifySignature = false)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            return PgpDecrypt(inputBytes, privateKeyStream, SecureStringToString(passphrase), publicKeyStream, verifySignature);
        }

        public string PgpEncryptText(string input, Stream publicKeyStream, Stream privateKeyStream, SecureString passphrase, bool sign = false)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            return PgpEncryptText(input, publicKeyStream, privateKeyStream, SecureStringToString(passphrase), sign);
        }

        public string PgpDecryptText(string input, Stream privateKeyStream, SecureString passphrase, Stream publicKeyStream = null, bool verifySignature = false)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            return PgpDecryptText(input, privateKeyStream, SecureStringToString(passphrase), publicKeyStream, verifySignature);
        }

        public byte[] PgpSignFile(byte[] inputBytes, Stream privateKeyStream, SecureString passphrase)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            return PgpSignFile(inputBytes, privateKeyStream, SecureStringToString(passphrase));
        }

        public byte[] PgpClearSignFile(byte[] inputBytes, Stream privateKeyStream, SecureString passphrase)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            return PgpClearSignFile(inputBytes, privateKeyStream, SecureStringToString(passphrase));
        }

        // ── PGP: verify / key-gen ─────────────────────────────────────────────

        public bool PgpVerify(byte[] inputBytes, Stream publicKeyStream)
        {
            ArgumentNullException.ThrowIfNull(inputBytes);
            ArgumentNullException.ThrowIfNull(publicKeyStream);

            return CryptographyHelper.PgpVerify(inputBytes, publicKeyStream);
        }

        public bool PgpVerifyClear(byte[] inputBytes, Stream publicKeyStream)
        {
            ArgumentNullException.ThrowIfNull(inputBytes);
            ArgumentNullException.ThrowIfNull(publicKeyStream);

            return CryptographyHelper.PgpVerifyClear(inputBytes, publicKeyStream);
        }

        public void PgpGenerateKeyPair(string publicKeyPath, string privateKeyPath, string username, string password)
        {
            if (string.IsNullOrWhiteSpace(publicKeyPath))
                throw new ArgumentException("Public key path must not be null or empty.", nameof(publicKeyPath));
            if (string.IsNullOrWhiteSpace(privateKeyPath))
                throw new ArgumentException("Private key path must not be null or empty.", nameof(privateKeyPath));

            CryptographyHelper.PgpGenerateKeyPair(publicKeyPath, privateKeyPath, username, password);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        // Extracts the plain-text value from a SecureString at the call boundary.
        // The string is short-lived: it is used immediately to construct key bytes
        // or pass to the underlying BouncyCastle API and then discarded.
        private static string SecureStringToString(SecureString value)
            => new NetworkCredential(string.Empty, value).Password;
    }
}
