using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace UiPath.Cryptography.Activities.Tests
{
    internal static class CryptographyHelper
    {
        private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
        private const int PBKDF2_SaltSizeBytes = 8; // Value recommended in literature (64 bit key).
        private const int PBKDF2_Iterations = 10000; // Value recommended in literature.

        public static byte[] HashData(HashAlgorithms hashAlgorithm, byte[] inputBytes)
        {
            byte[] result;

            using (HashAlgorithm algorithm = GetHashAlgorithm(hashAlgorithm))
            {
                result = algorithm.ComputeHash(inputBytes);

                algorithm.Clear();
            }

            return result;
        }

        public static byte[] HashDataWithKey(KeyedHashAlgorithms keyedHashAlgorithm, byte[] inputBytes, byte[] keyBytes)
        {
            byte[] result;

            using (KeyedHashAlgorithm algorithm = GetKeyedHashAlgorithm(keyedHashAlgorithm))
            {
                algorithm.Key = keyBytes;
                result = algorithm.ComputeHash(inputBytes);

                algorithm.Clear();
            }

            return result;
        }

        public static byte[] EncryptData(SymmetricAlgorithms symmetricAlgorithm, byte[] inputBytes, byte[] key)
        {
            byte[] result;

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

        public static byte[] DecryptData(SymmetricAlgorithms symmetricAlgorithm, byte[] inputBytes, byte[] key)
        {
            byte[] decrypted;

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

            return decrypted;
        }
        
        public static bool IsFipsCompliant(HashAlgorithms hashAlgorithm)
        {
            switch (hashAlgorithm)
            {
                case HashAlgorithms.MD5:
                case HashAlgorithms.RIPEMD160:
                    return false;
                default:
                    return true;
            }
        }

        public static bool IsFipsCompliant(KeyedHashAlgorithms keyedHashAlgorithm)
        {
            switch (keyedHashAlgorithm)
            {
                case KeyedHashAlgorithms.HMACMD5:
                case KeyedHashAlgorithms.HMACRIPEMD160:
                    return false;
                default:
                    return true;
            }
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


        private static HashAlgorithm GetHashAlgorithm(HashAlgorithms hashAlgorithm)
        {
            switch (hashAlgorithm)
            {
                case HashAlgorithms.MD5:
                    return new MD5Cng();
                case HashAlgorithms.RIPEMD160:
                    return new RIPEMD160Managed();
                case HashAlgorithms.SHA1:
                    return new SHA1Cng();
                case HashAlgorithms.SHA256:
                    return new SHA256Cng();
                case HashAlgorithms.SHA384:
                    return new SHA384Cng();
                case HashAlgorithms.SHA512:
                    return new SHA512Cng();
                default:
                    throw new InvalidOperationException();
            }
        }

        private static KeyedHashAlgorithm GetKeyedHashAlgorithm(KeyedHashAlgorithms keyedHashAlgorithm)
        {
            switch (keyedHashAlgorithm)
            {
                case KeyedHashAlgorithms.HMACMD5:
                    return new HMACMD5();
                case KeyedHashAlgorithms.HMACRIPEMD160:
                    return new HMACRIPEMD160();
                case KeyedHashAlgorithms.HMACSHA1:
                    return new HMACSHA1();
                case KeyedHashAlgorithms.HMACSHA256:
                    return new HMACSHA256();
                case KeyedHashAlgorithms.HMACSHA384:
                    return new HMACSHA384();
                case KeyedHashAlgorithms.HMACSHA512:
                    return new HMACSHA512();
                case KeyedHashAlgorithms.MACTripleDES: // TODO: What about padding mode?
                    return new MACTripleDES(); // TODO: Use TripleDESCng after upgrading to .NET Framework 4.6.2
                default:
                    throw new InvalidOperationException();
            }
        }

        private static SymmetricAlgorithm GetSymmetricAlgorithm(SymmetricAlgorithms symmetricAlgorithm)
        {
            switch (symmetricAlgorithm)
            {
                case SymmetricAlgorithms.AES:
                    return new AesCryptoServiceProvider(); // TODO: Use AesCng after upgrading to .NET Framework 4.6.2
                case SymmetricAlgorithms.DES:
                    return new DESCryptoServiceProvider();
                case SymmetricAlgorithms.RC2:
                    return new RC2CryptoServiceProvider();
                case SymmetricAlgorithms.Rijndael:
                    return new RijndaelManaged();
                case SymmetricAlgorithms.TripleDES:
                    return new TripleDESCryptoServiceProvider(); // TODO: Use TripleDESCng after upgrading to .NET Framework 4.6.2
                default:
                    throw new InvalidOperationException();
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
    }
}
