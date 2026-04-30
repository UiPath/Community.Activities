using System;
using System.IO;
using System.Runtime.InteropServices;
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

        public void EncryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding, bool overwrite)
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

        public void DecryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding, bool overwrite)
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
        // BouncyCastle's PGP API requires a plain string passphrase. The SecureString
        // is materialised to a managed string for the duration of the call and cannot
        // be zeroed afterward because strings are immutable in .NET.
        // This is a known limitation documented on ICryptographyService.

        public byte[] PgpEncrypt(byte[] inputBytes, Stream publicKeyStream, Stream privateKeyStream, SecureString passphrase, bool sign = false)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            return PgpEncrypt(inputBytes, publicKeyStream, privateKeyStream, SecureStringToManagedString(passphrase), sign);
        }

        public byte[] PgpDecrypt(byte[] inputBytes, Stream privateKeyStream, SecureString passphrase, Stream publicKeyStream = null, bool verifySignature = false)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            return PgpDecrypt(inputBytes, privateKeyStream, SecureStringToManagedString(passphrase), publicKeyStream, verifySignature);
        }

        public string PgpEncryptText(string input, Stream publicKeyStream, Stream privateKeyStream, SecureString passphrase, bool sign = false)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            return PgpEncryptText(input, publicKeyStream, privateKeyStream, SecureStringToManagedString(passphrase), sign);
        }

        public string PgpDecryptText(string input, Stream privateKeyStream, SecureString passphrase, Stream publicKeyStream = null, bool verifySignature = false)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            return PgpDecryptText(input, privateKeyStream, SecureStringToManagedString(passphrase), publicKeyStream, verifySignature);
        }

        public byte[] PgpSignFile(byte[] inputBytes, Stream privateKeyStream, SecureString passphrase)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            return PgpSignFile(inputBytes, privateKeyStream, SecureStringToManagedString(passphrase));
        }

        public byte[] PgpClearSignFile(byte[] inputBytes, Stream privateKeyStream, SecureString passphrase)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            return PgpClearSignFile(inputBytes, privateKeyStream, SecureStringToManagedString(passphrase));
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
