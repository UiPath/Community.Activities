using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using UiPath.Cryptography.Enums;

#pragma warning disable CS0618 // CryptographyHelper is intentionally marked Obsolete to discourage external use; in-package consumers are expected.

namespace UiPath.Cryptography.Activities.API
{
    internal class CryptographyService : ICryptographyService
    {
        // ── Symmetric encrypt ────────────────────────────────────────────────

        public byte[] EncryptBytes(byte[] inputBytes, EncryptionAlgorithm algorithm, string key, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(inputBytes);
            ArgumentNullException.ThrowIfNull(encoding);
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key must not be null or empty.", nameof(key));

            byte[] keyBytes = CryptographyHelper.KeyEncoding(encoding, key, null);
            return CryptographyHelper.EncryptData(algorithm, inputBytes, keyBytes);
        }

        public byte[] EncryptBytes(byte[] inputBytes, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(encoding);
            byte[] keyBytes = SecureStringToBytes(key, encoding);
            try
            {
                return EncryptBytes(inputBytes, algorithm, keyBytes);
            }
            finally
            {
                Array.Clear(keyBytes, 0, keyBytes.Length);
            }
        }

        public byte[] EncryptBytes(byte[] inputBytes, EncryptionAlgorithm algorithm, byte[] keyBytes)
        {
            ArgumentNullException.ThrowIfNull(inputBytes);
            ThrowIfKeyMissing(keyBytes, nameof(keyBytes));
            return CryptographyHelper.EncryptData(algorithm, inputBytes, keyBytes);
        }


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

        public string EncryptText(string input, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(encoding);
            byte[] keyBytes = SecureStringToBytes(key, encoding);
            try
            {
                return EncryptText(input, algorithm, keyBytes, encoding);
            }
            finally
            {
                Array.Clear(keyBytes, 0, keyBytes.Length);
            }
        }

        public string EncryptText(string input, EncryptionAlgorithm algorithm, byte[] keyBytes, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(encoding);
            ThrowIfKeyMissing(keyBytes, nameof(keyBytes));

            byte[] inputBytes = encoding.GetBytes(input);
            byte[] encryptedBytes = CryptographyHelper.EncryptData(algorithm, inputBytes, keyBytes);
            return Convert.ToBase64String(encryptedBytes);
        }


        public void EncryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, string key, Encoding encoding, bool overwrite = false)
        {
            ThrowIfFilePathMissing(inputFilePath, nameof(inputFilePath));
            ThrowIfFilePathMissing(outputFilePath, nameof(outputFilePath));
            ArgumentNullException.ThrowIfNull(encoding);
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key must not be null or empty.", nameof(key));

            byte[] keyBytes = CryptographyHelper.KeyEncoding(encoding, key, null);
            byte[] inputBytes = File.ReadAllBytes(inputFilePath);
            byte[] encryptedBytes = CryptographyHelper.EncryptData(algorithm, inputBytes, keyBytes);
            WriteFile(outputFilePath, encryptedBytes, overwrite);
        }

        public void EncryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding, bool overwrite = false)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(encoding);
            byte[] keyBytes = SecureStringToBytes(key, encoding);
            try
            {
                EncryptFile(inputFilePath, outputFilePath, algorithm, keyBytes, overwrite);
            }
            finally
            {
                Array.Clear(keyBytes, 0, keyBytes.Length);
            }
        }

        public void EncryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, byte[] keyBytes, bool overwrite = false)
        {
            ThrowIfFilePathMissing(inputFilePath, nameof(inputFilePath));
            ThrowIfFilePathMissing(outputFilePath, nameof(outputFilePath));
            ThrowIfKeyMissing(keyBytes, nameof(keyBytes));

            byte[] inputBytes = File.ReadAllBytes(inputFilePath);
            byte[] encryptedBytes = CryptographyHelper.EncryptData(algorithm, inputBytes, keyBytes);
            WriteFile(outputFilePath, encryptedBytes, overwrite);
        }

        // ── Symmetric decrypt ────────────────────────────────────────────────

        public byte[] DecryptBytes(byte[] inputBytes, EncryptionAlgorithm algorithm, string key, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(inputBytes);
            ArgumentNullException.ThrowIfNull(encoding);
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key must not be null or empty.", nameof(key));

            byte[] keyBytes = CryptographyHelper.KeyEncoding(encoding, key, null);
            return CryptographyHelper.DecryptData(algorithm, inputBytes, keyBytes);
        }

        public byte[] DecryptBytes(byte[] inputBytes, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(encoding);
            byte[] keyBytes = SecureStringToBytes(key, encoding);
            try
            {
                return DecryptBytes(inputBytes, algorithm, keyBytes);
            }
            finally
            {
                Array.Clear(keyBytes, 0, keyBytes.Length);
            }
        }

        public byte[] DecryptBytes(byte[] inputBytes, EncryptionAlgorithm algorithm, byte[] keyBytes)
        {
            ArgumentNullException.ThrowIfNull(inputBytes);
            ThrowIfKeyMissing(keyBytes, nameof(keyBytes));
            return CryptographyHelper.DecryptData(algorithm, inputBytes, keyBytes);
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

        public string DecryptText(string input, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(encoding);
            byte[] keyBytes = SecureStringToBytes(key, encoding);
            try
            {
                return DecryptText(input, algorithm, keyBytes, encoding);
            }
            finally
            {
                Array.Clear(keyBytes, 0, keyBytes.Length);
            }
        }

        public string DecryptText(string input, EncryptionAlgorithm algorithm, byte[] keyBytes, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(encoding);
            ThrowIfKeyMissing(keyBytes, nameof(keyBytes));

            byte[] inputBytes = Convert.FromBase64String(input);
            byte[] decryptedBytes = CryptographyHelper.DecryptData(algorithm, inputBytes, keyBytes);
            return encoding.GetString(decryptedBytes);
        }


        public void DecryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, string key, Encoding encoding, bool overwrite = false)
        {
            ThrowIfFilePathMissing(inputFilePath, nameof(inputFilePath));
            ThrowIfFilePathMissing(outputFilePath, nameof(outputFilePath));
            ArgumentNullException.ThrowIfNull(encoding);
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key must not be null or empty.", nameof(key));

            byte[] keyBytes = CryptographyHelper.KeyEncoding(encoding, key, null);
            byte[] inputBytes = File.ReadAllBytes(inputFilePath);
            byte[] decryptedBytes = CryptographyHelper.DecryptData(algorithm, inputBytes, keyBytes);
            WriteFile(outputFilePath, decryptedBytes, overwrite);
        }

        public void DecryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding, bool overwrite = false)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(encoding);
            byte[] keyBytes = SecureStringToBytes(key, encoding);
            try
            {
                DecryptFile(inputFilePath, outputFilePath, algorithm, keyBytes, overwrite);
            }
            finally
            {
                Array.Clear(keyBytes, 0, keyBytes.Length);
            }
        }

        public void DecryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, byte[] keyBytes, bool overwrite = false)
        {
            ThrowIfFilePathMissing(inputFilePath, nameof(inputFilePath));
            ThrowIfFilePathMissing(outputFilePath, nameof(outputFilePath));
            ThrowIfKeyMissing(keyBytes, nameof(keyBytes));

            byte[] inputBytes = File.ReadAllBytes(inputFilePath);
            byte[] decryptedBytes = CryptographyHelper.DecryptData(algorithm, inputBytes, keyBytes);
            WriteFile(outputFilePath, decryptedBytes, overwrite);
        }

        // ── Keyed hash ────────────────────────────────────────────────────────

        public string KeyedHashBytes(byte[] inputBytes, KeyedHashAlgorithms algorithm, string key, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(inputBytes);
            ArgumentNullException.ThrowIfNull(encoding);
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key must not be null or empty.", nameof(key));

            byte[] keyBytes = CryptographyHelper.KeyEncoding(encoding, key, null);
            return ComputeHashHex(algorithm, inputBytes, keyBytes);
        }

        public string KeyedHashBytes(byte[] inputBytes, KeyedHashAlgorithms algorithm, SecureString key, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(encoding);
            byte[] keyBytes = SecureStringToBytes(key, encoding);
            try
            {
                return KeyedHashBytes(inputBytes, algorithm, keyBytes);
            }
            finally
            {
                Array.Clear(keyBytes, 0, keyBytes.Length);
            }
        }

        public string KeyedHashBytes(byte[] inputBytes, KeyedHashAlgorithms algorithm, byte[] keyBytes)
        {
            ArgumentNullException.ThrowIfNull(inputBytes);
            ThrowIfKeyMissing(keyBytes, nameof(keyBytes));
            return ComputeHashHex(algorithm, inputBytes, keyBytes);
        }


        public string KeyedHashText(string input, KeyedHashAlgorithms algorithm, string key, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(encoding);
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key must not be null or empty.", nameof(key));

            byte[] keyBytes = CryptographyHelper.KeyEncoding(encoding, key, null);
            byte[] inputBytes = encoding.GetBytes(input);
            return ComputeHashHex(algorithm, inputBytes, keyBytes);
        }

        public string KeyedHashText(string input, KeyedHashAlgorithms algorithm, SecureString key, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(encoding);
            byte[] keyBytes = SecureStringToBytes(key, encoding);
            try
            {
                return KeyedHashText(input, algorithm, keyBytes, encoding);
            }
            finally
            {
                Array.Clear(keyBytes, 0, keyBytes.Length);
            }
        }

        public string KeyedHashText(string input, KeyedHashAlgorithms algorithm, byte[] keyBytes, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(encoding);
            ThrowIfKeyMissing(keyBytes, nameof(keyBytes));

            byte[] inputBytes = encoding.GetBytes(input);
            return ComputeHashHex(algorithm, inputBytes, keyBytes);
        }


        public string KeyedHashFile(string filePath, KeyedHashAlgorithms algorithm, string key, Encoding encoding)
        {
            ThrowIfFilePathMissing(filePath, nameof(filePath));
            ArgumentNullException.ThrowIfNull(encoding);
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key must not be null or empty.", nameof(key));

            byte[] keyBytes = CryptographyHelper.KeyEncoding(encoding, key, null);
            byte[] inputBytes = File.ReadAllBytes(filePath);
            return ComputeHashHex(algorithm, inputBytes, keyBytes);
        }

        public string KeyedHashFile(string filePath, KeyedHashAlgorithms algorithm, SecureString key, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(encoding);
            byte[] keyBytes = SecureStringToBytes(key, encoding);
            try
            {
                return KeyedHashFile(filePath, algorithm, keyBytes);
            }
            finally
            {
                Array.Clear(keyBytes, 0, keyBytes.Length);
            }
        }

        public string KeyedHashFile(string filePath, KeyedHashAlgorithms algorithm, byte[] keyBytes)
        {
            ThrowIfFilePathMissing(filePath, nameof(filePath));
            ThrowIfKeyMissing(keyBytes, nameof(keyBytes));

            byte[] inputBytes = File.ReadAllBytes(filePath);
            return ComputeHashHex(algorithm, inputBytes, keyBytes);
        }

        // ── PGP encrypt ───────────────────────────────────────────────────────
        // SecureString-passphrase overloads materialise to a managed string for the
        // duration of the call. BouncyCastle requires a plain string passphrase and
        // offers no byte[]-based API; the managed string cannot be zeroed afterward.

        public byte[] PgpEncryptBytes(byte[] inputBytes, byte[] publicKey, byte[] privateKey = null, string passphrase = null, bool sign = false)
        {
            ArgumentNullException.ThrowIfNull(inputBytes);
            ThrowIfKeyMissing(publicKey, nameof(publicKey));

            using var pubStream = new MemoryStream(publicKey, writable: false);
            using var privStream = ToReadOnlyStream(privateKey);
            return CryptographyHelper.PgpEncrypt(inputBytes, pubStream, privStream, passphrase, sign);
        }

        public byte[] PgpEncryptBytes(byte[] inputBytes, byte[] publicKey, byte[] privateKey, SecureString passphrase, bool sign = false)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            return PgpEncryptBytes(inputBytes, publicKey, privateKey, SecureStringToManagedString(passphrase), sign);
        }


        public string PgpEncryptText(string input, byte[] publicKey, byte[] privateKey = null, string passphrase = null, bool sign = false)
        {
            ArgumentNullException.ThrowIfNull(input);
            ThrowIfKeyMissing(publicKey, nameof(publicKey));

            using var pubStream = new MemoryStream(publicKey, writable: false);
            using var privStream = ToReadOnlyStream(privateKey);
            return CryptographyHelper.PgpEncryptText(input, pubStream, privStream, passphrase, sign);
        }

        public string PgpEncryptText(string input, byte[] publicKey, byte[] privateKey, SecureString passphrase, bool sign = false)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            return PgpEncryptText(input, publicKey, privateKey, SecureStringToManagedString(passphrase), sign);
        }


        public void PgpEncryptFile(string inputFilePath, string outputFilePath, byte[] publicKey, byte[] privateKey = null, string passphrase = null, bool sign = false, bool overwrite = false)
        {
            ThrowIfFilePathMissing(inputFilePath, nameof(inputFilePath));
            byte[] inputBytes = File.ReadAllBytes(inputFilePath);
            byte[] encrypted = PgpEncryptBytes(inputBytes, publicKey, privateKey, passphrase, sign);
            WriteFile(outputFilePath, encrypted, overwrite);
        }

        public void PgpEncryptFile(string inputFilePath, string outputFilePath, byte[] publicKey, byte[] privateKey, SecureString passphrase, bool sign = false, bool overwrite = false)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            PgpEncryptFile(inputFilePath, outputFilePath, publicKey, privateKey, SecureStringToManagedString(passphrase), sign, overwrite);
        }

        // ── PGP decrypt ───────────────────────────────────────────────────────

        public byte[] PgpDecryptBytes(byte[] inputBytes, byte[] privateKey, string passphrase, byte[] publicKey = null, bool verifySignature = false)
        {
            ArgumentNullException.ThrowIfNull(inputBytes);
            ThrowIfKeyMissing(privateKey, nameof(privateKey));

            using var privStream = new MemoryStream(privateKey, writable: false);
            using var pubStream = ToReadOnlyStream(publicKey);
            return CryptographyHelper.PgpDecrypt(inputBytes, privStream, passphrase, pubStream, verifySignature);
        }

        public byte[] PgpDecryptBytes(byte[] inputBytes, byte[] privateKey, SecureString passphrase, byte[] publicKey = null, bool verifySignature = false)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            return PgpDecryptBytes(inputBytes, privateKey, SecureStringToManagedString(passphrase), publicKey, verifySignature);
        }


        public string PgpDecryptText(string input, byte[] privateKey, string passphrase, byte[] publicKey = null, bool verifySignature = false)
        {
            ArgumentNullException.ThrowIfNull(input);
            ThrowIfKeyMissing(privateKey, nameof(privateKey));

            using var privStream = new MemoryStream(privateKey, writable: false);
            using var pubStream = ToReadOnlyStream(publicKey);
            return CryptographyHelper.PgpDecryptText(input, privStream, passphrase, pubStream, verifySignature);
        }

        public string PgpDecryptText(string input, byte[] privateKey, SecureString passphrase, byte[] publicKey = null, bool verifySignature = false)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            return PgpDecryptText(input, privateKey, SecureStringToManagedString(passphrase), publicKey, verifySignature);
        }


        public void PgpDecryptFile(string inputFilePath, string outputFilePath, byte[] privateKey, string passphrase, byte[] publicKey = null, bool verifySignature = false, bool overwrite = false)
        {
            ThrowIfFilePathMissing(inputFilePath, nameof(inputFilePath));
            byte[] inputBytes = File.ReadAllBytes(inputFilePath);
            byte[] decrypted = PgpDecryptBytes(inputBytes, privateKey, passphrase, publicKey, verifySignature);
            WriteFile(outputFilePath, decrypted, overwrite);
        }

        public void PgpDecryptFile(string inputFilePath, string outputFilePath, byte[] privateKey, SecureString passphrase, byte[] publicKey = null, bool verifySignature = false, bool overwrite = false)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            PgpDecryptFile(inputFilePath, outputFilePath, privateKey, SecureStringToManagedString(passphrase), publicKey, verifySignature, overwrite);
        }

        // ── PGP sign ──────────────────────────────────────────────────────────

        public byte[] PgpSignBytes(byte[] inputBytes, byte[] privateKey, string passphrase)
        {
            ArgumentNullException.ThrowIfNull(inputBytes);
            ThrowIfKeyMissing(privateKey, nameof(privateKey));

            using var privStream = new MemoryStream(privateKey, writable: false);
            return CryptographyHelper.PgpSign(inputBytes, privStream, passphrase);
        }

        public byte[] PgpSignBytes(byte[] inputBytes, byte[] privateKey, SecureString passphrase)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            return PgpSignBytes(inputBytes, privateKey, SecureStringToManagedString(passphrase));
        }


        public string PgpSignText(string input, byte[] privateKey, string passphrase)
        {
            ArgumentNullException.ThrowIfNull(input);
            ThrowIfKeyMissing(privateKey, nameof(privateKey));

            using var privStream = new MemoryStream(privateKey, writable: false);
            return CryptographyHelper.PgpSignText(input, privStream, passphrase);
        }

        public string PgpSignText(string input, byte[] privateKey, SecureString passphrase)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            return PgpSignText(input, privateKey, SecureStringToManagedString(passphrase));
        }


        public void PgpSignFile(string inputFilePath, string outputFilePath, byte[] privateKey, string passphrase, bool overwrite = false)
        {
            byte[] signed = PgpSignBytesFromFile(inputFilePath, privateKey, passphrase, sign: true);
            WriteFile(outputFilePath, signed, overwrite);
        }

        public void PgpSignFile(string inputFilePath, string outputFilePath, byte[] privateKey, SecureString passphrase, bool overwrite = false)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            PgpSignFile(inputFilePath, outputFilePath, privateKey, SecureStringToManagedString(passphrase), overwrite);
        }

        // ── PGP clearsign ─────────────────────────────────────────────────────

        public byte[] PgpClearsignBytes(byte[] inputBytes, byte[] privateKey, string passphrase)
        {
            ArgumentNullException.ThrowIfNull(inputBytes);
            ThrowIfKeyMissing(privateKey, nameof(privateKey));

            using var privStream = new MemoryStream(privateKey, writable: false);
            return CryptographyHelper.PgpClearSign(inputBytes, privStream, passphrase);
        }

        public byte[] PgpClearsignBytes(byte[] inputBytes, byte[] privateKey, SecureString passphrase)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            return PgpClearsignBytes(inputBytes, privateKey, SecureStringToManagedString(passphrase));
        }


        public string PgpClearsignText(string input, byte[] privateKey, string passphrase)
        {
            ArgumentNullException.ThrowIfNull(input);
            ThrowIfKeyMissing(privateKey, nameof(privateKey));

            using var privStream = new MemoryStream(privateKey, writable: false);
            return CryptographyHelper.PgpClearSignText(input, privStream, passphrase);
        }

        public string PgpClearsignText(string input, byte[] privateKey, SecureString passphrase)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            return PgpClearsignText(input, privateKey, SecureStringToManagedString(passphrase));
        }


        public void PgpClearsignFile(string inputFilePath, string outputFilePath, byte[] privateKey, string passphrase, bool overwrite = false)
        {
            byte[] signed = PgpSignBytesFromFile(inputFilePath, privateKey, passphrase, sign: false);
            WriteFile(outputFilePath, signed, overwrite);
        }

        public void PgpClearsignFile(string inputFilePath, string outputFilePath, byte[] privateKey, SecureString passphrase, bool overwrite = false)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            PgpClearsignFile(inputFilePath, outputFilePath, privateKey, SecureStringToManagedString(passphrase), overwrite);
        }

        // ── PGP verify (binary signature) ─────────────────────────────────────

        public bool PgpVerifyBytes(byte[] inputBytes, byte[] publicKey)
        {
            ArgumentNullException.ThrowIfNull(inputBytes);
            ThrowIfKeyMissing(publicKey, nameof(publicKey));

            using var pubStream = new MemoryStream(publicKey, writable: false);
            return CryptographyHelper.PgpVerify(inputBytes, pubStream);
        }


        public bool PgpVerifyText(string input, byte[] publicKey)
        {
            ArgumentNullException.ThrowIfNull(input);
            ThrowIfKeyMissing(publicKey, nameof(publicKey));

            using var pubStream = new MemoryStream(publicKey, writable: false);
            return CryptographyHelper.PgpVerifyText(input, pubStream);
        }


        public bool PgpVerifyFile(string inputFilePath, byte[] publicKey)
        {
            ThrowIfFilePathMissing(inputFilePath, nameof(inputFilePath));
            byte[] inputBytes = File.ReadAllBytes(inputFilePath);
            return PgpVerifyBytes(inputBytes, publicKey);
        }

        // ── PGP verify (clearsignature) ───────────────────────────────────────

        public bool PgpVerifyClearBytes(byte[] inputBytes, byte[] publicKey)
        {
            ArgumentNullException.ThrowIfNull(inputBytes);
            ThrowIfKeyMissing(publicKey, nameof(publicKey));

            using var pubStream = new MemoryStream(publicKey, writable: false);
            return CryptographyHelper.PgpVerifyClear(inputBytes, pubStream);
        }


        public bool PgpVerifyClearText(string input, byte[] publicKey)
        {
            ArgumentNullException.ThrowIfNull(input);
            ThrowIfKeyMissing(publicKey, nameof(publicKey));

            using var pubStream = new MemoryStream(publicKey, writable: false);
            return CryptographyHelper.PgpVerifyClearText(input, pubStream);
        }


        public bool PgpVerifyClearFile(string inputFilePath, byte[] publicKey)
        {
            ThrowIfFilePathMissing(inputFilePath, nameof(inputFilePath));
            byte[] inputBytes = File.ReadAllBytes(inputFilePath);
            return PgpVerifyClearBytes(inputBytes, publicKey);
        }

        // ── PGP verify (public key well-formedness) ───────────────────────────

        public bool PgpVerifyPublicKeyBytes(byte[] publicKey)
        {
            ThrowIfKeyMissing(publicKey, nameof(publicKey));

            using var pubStream = new MemoryStream(publicKey, writable: false);
            return CryptographyHelper.PgpVerifyPublicKey(pubStream);
        }


        public bool PgpVerifyPublicKeyText(string publicKey)
        {
            if (string.IsNullOrEmpty(publicKey))
                throw new ArgumentException("Public key must not be null or empty.", nameof(publicKey));

            byte[] keyBytes = Encoding.UTF8.GetBytes(publicKey);
            return PgpVerifyPublicKeyBytes(keyBytes);
        }


        public bool PgpVerifyPublicKeyFile(string publicKeyFilePath)
        {
            ThrowIfFilePathMissing(publicKeyFilePath, nameof(publicKeyFilePath));
            byte[] keyBytes = File.ReadAllBytes(publicKeyFilePath);
            return PgpVerifyPublicKeyBytes(keyBytes);
        }

        // ── PGP key generation ───────────────────────────────────────────────

        public void PgpGenerateKeys(string publicKeyPath, string privateKeyPath, string userId, string passphrase, RsaKeySize keySize = RsaKeySize.Rsa4096)
        {
            ThrowIfFilePathMissing(publicKeyPath, nameof(publicKeyPath));
            ThrowIfFilePathMissing(privateKeyPath, nameof(privateKeyPath));

            CryptographyHelper.PgpGenerateKeys(publicKeyPath, privateKeyPath, userId, passphrase, keySize);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static string ComputeHashHex(KeyedHashAlgorithms algorithm, byte[] inputBytes, byte[] keyBytes)
        {
            byte[] hashBytes = CryptographyHelper.HashDataWithKey(algorithm, inputBytes, keyBytes);
            return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
        }

        private static byte[] PgpSignBytesFromFile(string inputFilePath, byte[] privateKey, string passphrase, bool sign)
        {
            ThrowIfFilePathMissing(inputFilePath, nameof(inputFilePath));
            ThrowIfKeyMissing(privateKey, nameof(privateKey));

            byte[] inputBytes = File.ReadAllBytes(inputFilePath);
            using var privStream = new MemoryStream(privateKey, writable: false);
            return sign
                ? CryptographyHelper.PgpSign(inputBytes, privStream, passphrase)
                : CryptographyHelper.PgpClearSign(inputBytes, privStream, passphrase);
        }

        private static void ThrowIfKeyMissing(byte[] keyBytes, string paramName)
        {
            if (keyBytes is null || keyBytes.Length == 0)
                throw new ArgumentException("Key bytes must not be null or empty.", paramName);
        }

        private static void ThrowIfFilePathMissing(string path, string paramName)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("File path must not be null or empty.", paramName);
        }

        private static MemoryStream ToReadOnlyStream(byte[] bytes) =>
            bytes is { Length: > 0 } ? new MemoryStream(bytes, writable: false) : null;

        private static void WriteFile(string outputFilePath, byte[] bytes, bool overwrite = false)
        {
            ThrowIfFilePathMissing(outputFilePath, nameof(outputFilePath));
            if (!overwrite && File.Exists(outputFilePath))
                throw new InvalidOperationException($"Output file already exists: {outputFilePath}");
            File.WriteAllBytes(outputFilePath, bytes);
        }

        // Extracts the SecureString content via unmanaged memory, encodes it with the
        // caller-supplied Encoding, then zeros both the unmanaged buffer and the char[]
        // before returning. No managed string is ever created.
        private static byte[] SecureStringToBytes(SecureString value, Encoding encoding)
        {
            IntPtr ptr = IntPtr.Zero;
            char[] chars = null;
            try
            {
                ptr = Marshal.SecureStringToGlobalAllocUnicode(value);
                int length = value.Length;
                chars = new char[length];
                Marshal.Copy(ptr, chars, 0, length);
                return encoding.GetBytes(chars);
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr);
                if (chars != null)
                    Array.Clear(chars, 0, chars.Length);
            }
        }

        // Used only by PGP overloads: BouncyCastle requires a plain string passphrase
        // and offers no byte[]-based API. The managed string cannot be zeroed afterward.
        private static string SecureStringToManagedString(SecureString value)
        {
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(ptr);
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr);
            }
        }
    }
}
