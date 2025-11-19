using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using UiPath.Cryptography.Properties;

#pragma warning disable CS0618 // obsolete encryption algorithm

namespace UiPath.Cryptography
{
    public static class CryptographyHelper
    {
        private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
        private const int PBKDF2_SaltSizeBytes = 8; // Value recommended in literature (64 bit key).
        private const int PBKDF2_Iterations = 10000; // Value recommended in literature.

        public static byte[] HashDataWithKey(KeyedHashAlgorithms keyedHashAlgorithm, byte[] inputBytes, byte[] keyBytes)
        {
            byte[] result;

            using (HashAlgorithm algorithm = GetKeyedHashAlgorithm(keyedHashAlgorithm))
            {
                if (algorithm is KeyedHashAlgorithm hashAlgorithm)
                {
                    hashAlgorithm.Key = keyBytes;
                }
                
                result = algorithm.ComputeHash(inputBytes);

                algorithm.Clear();
            }

            return result;
        }

        public static byte[] EncryptData(SymmetricAlgorithms symmetricAlgorithm, byte[] inputBytes, byte[] key)
        {
            byte[] result;

            if (symmetricAlgorithm == SymmetricAlgorithms.AESGCM)
            {
                return EncryptAesGcm(inputBytes, key);
            }
            else
            {
                using (SymmetricAlgorithm algorithm = GetSymmetricAlgorithm(symmetricAlgorithm))
                {
                    byte[] encrypted;
                    byte[] salt = new byte[PBKDF2_SaltSizeBytes];
                    int maxKeySize = GetLegalKeySizes(algorithm).Max();

                    _rng.GetBytes(salt);
                    using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(key, salt, PBKDF2_Iterations))
                    {
                        algorithm.Key = pbkdf2.GetBytes(maxKeySize);
                    }

                    using (ICryptoTransform cryptoTransform = algorithm.CreateEncryptor())
                    {
                        using (MemoryStream inputStream = new MemoryStream(inputBytes), transformedStream = new MemoryStream())
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(inputStream, cryptoTransform, CryptoStreamMode.Read))
                            {
                                cryptoStream.CopyTo(transformedStream);
                            }

                            encrypted = transformedStream.ToArray();
                        }
                    }

                    result = new byte[salt.Length + algorithm.IV.Length + encrypted.Length];
                    Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
                    Buffer.BlockCopy(algorithm.IV, 0, result, salt.Length, algorithm.IV.Length);
                    Buffer.BlockCopy(encrypted, 0, result, salt.Length + algorithm.IV.Length, encrypted.Length);
                }

                return result;
            }
        }

        public static byte[] DecryptData(SymmetricAlgorithms symmetricAlgorithm, byte[] inputBytes, byte[] key)
        {
            byte[] decrypted;

            if (symmetricAlgorithm == SymmetricAlgorithms.AESGCM)
            {
                return DecryptAesGcm(inputBytes, key);
            }
            else
            {
                using (SymmetricAlgorithm algorithm = GetSymmetricAlgorithm(symmetricAlgorithm))
                {
                    byte[] salt = new byte[PBKDF2_SaltSizeBytes];
                    byte[] iv = new byte[algorithm.IV.Length];

                    byte[] encryptedData = new byte[inputBytes.Length - salt.Length - iv.Length];

                    int maxKeySize = GetLegalKeySizes(algorithm).Max();

                    Buffer.BlockCopy(inputBytes, 0, salt, 0, salt.Length);
                    Buffer.BlockCopy(inputBytes, salt.Length, iv, 0, iv.Length);
                    Buffer.BlockCopy(inputBytes, salt.Length + iv.Length, encryptedData, 0, encryptedData.Length);

                    algorithm.IV = iv;
                    using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(key, salt, PBKDF2_Iterations))
                    {
                        algorithm.Key = pbkdf2.GetBytes(maxKeySize);
                    }

                    using (ICryptoTransform cryptoTransform = algorithm.CreateDecryptor())
                    {
                        using (MemoryStream encryptedStream = new MemoryStream(encryptedData))
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(encryptedStream, cryptoTransform, CryptoStreamMode.Read))
                            {
                                decrypted = cryptoStream.ReadToEnd();
                            }
                        }
                    }
                }
            }

            return decrypted;
        }

        public static bool IsFipsCompliant(SymmetricAlgorithms symmetricAlgorithm)
        {
            switch (symmetricAlgorithm)
            {
                case SymmetricAlgorithms.RC2:
                case SymmetricAlgorithms.Rijndael:
                    return false;

                default:
                    return true;
            }
        }

        private static HashAlgorithm GetKeyedHashAlgorithm(KeyedHashAlgorithms keyedHashAlgorithm)
        {
            switch (keyedHashAlgorithm)
            {
                case KeyedHashAlgorithms.HMACMD5:
                    return new HMACMD5();
                case KeyedHashAlgorithms.HMACSHA1:
                    return new HMACSHA1();
                case KeyedHashAlgorithms.HMACSHA256:
                    return new HMACSHA256();
                case KeyedHashAlgorithms.HMACSHA384:
                    return new HMACSHA384();
                case KeyedHashAlgorithms.HMACSHA512:
                    return new HMACSHA512();
                case KeyedHashAlgorithms.SHA1:
                    return SHA1.Create();
                case KeyedHashAlgorithms.SHA256:
                    return SHA256.Create();
                case KeyedHashAlgorithms.SHA384:
                    return SHA384.Create();
                case KeyedHashAlgorithms.SHA512:
                    return SHA512.Create();
                default:
                    throw new InvalidOperationException(Resources.UnsupportedKeyedHashAlgorithmException);
            }
        }

        private static SymmetricAlgorithm GetSymmetricAlgorithm(SymmetricAlgorithms symmetricAlgorithm)
        {
            switch (symmetricAlgorithm)
            {
                case SymmetricAlgorithms.AES:
                    return new AesCryptoServiceProvider(); // kept for backwords compat

                case SymmetricAlgorithms.AESGCM:
                    throw new InvalidOperationException(Resources.UnsupportedSymmetricAlgorithmException); //it's implemented separately.

                case SymmetricAlgorithms.DES:
                    return new DESCryptoServiceProvider();

                case SymmetricAlgorithms.RC2:
                    return new RC2CryptoServiceProvider();

                case SymmetricAlgorithms.Rijndael:
                    return new RijndaelManaged();

                case SymmetricAlgorithms.TripleDES:
                    return new TripleDESCryptoServiceProvider(); // TODO: Use TripleDESCng after upgrading to .NET Framework 4.6.2

                default:
                    throw new InvalidOperationException(Resources.UnsupportedSymmetricAlgorithmException);
            }
        }

        private static int[] GetLegalKeySizes(SymmetricAlgorithm algorithm)
        {
            List<int> keySizes = new List<int>();

            foreach (KeySizes ks in algorithm.LegalKeySizes)
            {
                if (ks.MinSize == ks.MaxSize)
                {
                    keySizes.Add(ks.MinSize / 8); // ks.MinSize represents the size in bits, we want the size in bytes.
                }
                else
                {
                    for (int i = ks.MinSize; i <= ks.MaxSize; i += ks.SkipSize)
                    {
                        keySizes.Add(i / 8); // i represents the size in bits, we want the size in bytes.
                    }
                }
            }

            return keySizes.ToArray();
        }

        private static byte[] EncryptAesGcm(byte[] inputBytes, byte[] key)
        {
            byte[] result;
            byte[] encrypted = new byte[inputBytes.Length];
            InitializeAesGcmEncryption(out byte[] salt, out byte[] tag, out byte[] algorithmIV);

            using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(key, salt, PBKDF2_Iterations))
            {
                var Key = pbkdf2.GetBytes(32); //256 bit key

                var aes = new AesGcm(Key);
                aes.Encrypt(algorithmIV, inputBytes, encrypted, tag);
            }

            result = CreateAesGcmEncryptionResult(encrypted, salt, tag, algorithmIV);

            return result;
        }

        private static byte[] DecryptAesGcm(byte[] inputBytes, byte[] key)
        {
            byte[] decrypted;
            InitializeDecryptAesGcm(inputBytes, out byte[] salt, out byte[] iv, out byte[] tag, out byte[] encryptedData);

            using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(key, salt, PBKDF2_Iterations))
            {
                var Key = pbkdf2.GetBytes(32); //256 bit key

                var aes = new AesGcm(Key);
                decrypted = new byte[encryptedData.Length];
                aes.Decrypt(iv, encryptedData, tag, decrypted);
            }

            return decrypted;
        }

        private static void InitializeAesGcmEncryption(out byte[] salt, out byte[] tag, out byte[] algorithmIV)
        {
            salt = new byte[PBKDF2_SaltSizeBytes];
            tag = new byte[16];
            algorithmIV = new byte[12];
            _rng.GetBytes(salt);
            _rng.GetBytes(algorithmIV);
        }

        private static byte[] CreateAesGcmEncryptionResult(byte[] encrypted, byte[] salt, byte[] tag, byte[] algorithmIV)
        {
            byte[] result = new byte[salt.Length + algorithmIV.Length + encrypted.Length + tag.Length];
            Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
            Buffer.BlockCopy(algorithmIV, 0, result, salt.Length, algorithmIV.Length);
            Buffer.BlockCopy(encrypted, 0, result, salt.Length + algorithmIV.Length, encrypted.Length);
            Buffer.BlockCopy(tag, 0, result, salt.Length + algorithmIV.Length + encrypted.Length, tag.Length);
            return result;
        }

        private static void InitializeDecryptAesGcm(byte[] inputBytes, out byte[] salt, out byte[] iv, out byte[] tag, out byte[] encryptedData)
        {
            salt = new byte[PBKDF2_SaltSizeBytes];
            iv = new byte[12];
            tag = new byte[16];
            encryptedData = new byte[inputBytes.Length - salt.Length - iv.Length - tag.Length];
            Buffer.BlockCopy(inputBytes, 0, salt, 0, salt.Length);
            Buffer.BlockCopy(inputBytes, salt.Length, iv, 0, iv.Length);
            Buffer.BlockCopy(inputBytes, salt.Length + iv.Length, encryptedData, 0, encryptedData.Length);
            Buffer.BlockCopy(inputBytes, salt.Length + iv.Length + encryptedData.Length, tag, 0, tag.Length);
        }

        public static byte[] KeyEncoding(Encoding encoding, string key, SecureString keySecureString)
        {
            return key != null ? encoding.GetBytes(key) : encoding.GetBytes(new NetworkCredential("", keySecureString).Password);
        }
    }
}