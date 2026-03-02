using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using PgpCore;
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

        public static byte[] EncryptData(EncryptionAlgorithm algorithm, byte[] inputBytes, byte[] key)
        {
            if (algorithm == EncryptionAlgorithm.PGP)
                throw new ArgumentException("Use PGP-specific methods for PGP encryption.", nameof(algorithm));

            byte[] result;

            if (algorithm == EncryptionAlgorithm.AESGCM)
            {
                return EncryptAesGcm(inputBytes, key);
            }
            else
            {
                using (SymmetricAlgorithm symmetricAlgorithm = GetSymmetricAlgorithmProvider(algorithm))
                {
                    byte[] encrypted;
                    byte[] salt = new byte[PBKDF2_SaltSizeBytes];
                    int maxKeySize = GetLegalKeySizes(symmetricAlgorithm).Max();

                    _rng.GetBytes(salt);
                    using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(key, salt, PBKDF2_Iterations))
                    {
                        symmetricAlgorithm.Key = pbkdf2.GetBytes(maxKeySize);
                    }

                    using (ICryptoTransform cryptoTransform = symmetricAlgorithm.CreateEncryptor())
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

                    result = new byte[salt.Length + symmetricAlgorithm.IV.Length + encrypted.Length];
                    Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
                    Buffer.BlockCopy(symmetricAlgorithm.IV, 0, result, salt.Length, symmetricAlgorithm.IV.Length);
                    Buffer.BlockCopy(encrypted, 0, result, salt.Length + symmetricAlgorithm.IV.Length, encrypted.Length);
                }

                return result;
            }
        }

        public static byte[] DecryptData(EncryptionAlgorithm algorithm, byte[] inputBytes, byte[] key)
        {
            if (algorithm == EncryptionAlgorithm.PGP)
                throw new ArgumentException("Use PGP-specific methods for PGP decryption.", nameof(algorithm));

            byte[] decrypted;

            if (algorithm == EncryptionAlgorithm.AESGCM)
            {
                return DecryptAesGcm(inputBytes, key);
            }
            else
            {
                using (SymmetricAlgorithm symmetricAlgorithm = GetSymmetricAlgorithmProvider(algorithm))
                {
                    byte[] salt = new byte[PBKDF2_SaltSizeBytes];
                    byte[] iv = new byte[symmetricAlgorithm.IV.Length];

                    byte[] encryptedData = new byte[inputBytes.Length - salt.Length - iv.Length];

                    int maxKeySize = GetLegalKeySizes(symmetricAlgorithm).Max();

                    Buffer.BlockCopy(inputBytes, 0, salt, 0, salt.Length);
                    Buffer.BlockCopy(inputBytes, salt.Length, iv, 0, iv.Length);
                    Buffer.BlockCopy(inputBytes, salt.Length + iv.Length, encryptedData, 0, encryptedData.Length);

                    symmetricAlgorithm.IV = iv;
                    using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(key, salt, PBKDF2_Iterations))
                    {
                        symmetricAlgorithm.Key = pbkdf2.GetBytes(maxKeySize);
                    }

                    using (ICryptoTransform cryptoTransform = symmetricAlgorithm.CreateDecryptor())
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

        public static bool IsFipsCompliant(EncryptionAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case EncryptionAlgorithm.RC2:
                case EncryptionAlgorithm.Rijndael:
                    return false;

                case EncryptionAlgorithm.PGP:
                    return true; // PGP uses its own key management; FIPS check is not applicable

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

        private static SymmetricAlgorithm GetSymmetricAlgorithmProvider(EncryptionAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case EncryptionAlgorithm.AES:
                    return new AesCryptoServiceProvider(); // kept for backwords compat

                case EncryptionAlgorithm.AESGCM:
                    throw new InvalidOperationException(Resources.UnsupportedSymmetricAlgorithmException); //it's implemented separately.

                case EncryptionAlgorithm.DES:
                    return new DESCryptoServiceProvider();

                case EncryptionAlgorithm.RC2:
                    return new RC2CryptoServiceProvider();

                case EncryptionAlgorithm.Rijndael:
                    return new RijndaelManaged();

                case EncryptionAlgorithm.TripleDES:
                    return new TripleDESCryptoServiceProvider(); // TODO: Use TripleDESCng after upgrading to .NET Framework 4.6.2

                case EncryptionAlgorithm.PGP:
                    // PGP is asymmetric and handled separately; this case is unreachable in production
                    // because callers branch on Algorithm == PGP before calling symmetric methods.
                    throw new InvalidOperationException(Resources.UnsupportedSymmetricAlgorithmException);

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

        #region PGP Methods

        private static void ThrowTranslatedPgpException(Exception ex)
        {
            var message = ex.Message ?? string.Empty;

            // "Checksum mismatch" → wrong passphrase for private key
            if (message.Contains("Checksum mismatch"))
                throw new InvalidOperationException(Resources.PgpInvalidPassphrase, ex);

            // "Secret key for message not found." → wrong private key
            if (message.Contains("Secret key for message not found"))
                throw new InvalidOperationException(Resources.PgpPrivateKeyNotFound, ex);

            // "Failed to verify file." → signature verification failed (wrong public key)
            if (message.Contains("Failed to verify"))
                throw new InvalidOperationException(Resources.PgpSignatureVerificationFailed, ex);

            // No translation — preserve original stack trace
            ExceptionDispatchInfo.Capture(ex).Throw();
        }

        public static byte[] PgpEncrypt(byte[] inputBytes, Stream publicKeyStream, Stream privateKeyStream = null, string passphrase = null, bool sign = false)
        {
            using (var inputStream = new MemoryStream(inputBytes))
            using (var outputStream = new MemoryStream())
            {
                PgpEncryptStream(inputStream, outputStream, publicKeyStream, privateKeyStream, passphrase, sign);
                return outputStream.ToArray();
            }
        }

        public static void PgpEncryptStream(Stream inputStream, Stream outputStream, Stream publicKeyStream, Stream privateKeyStream = null, string passphrase = null, bool sign = false)
        {
            try
            {
                if (sign && (privateKeyStream == null || string.IsNullOrEmpty(passphrase)))
                    throw new ArgumentException(Properties.UiPath_Cryptography.PgpSigningRequiresPrivateKeyAndPassphrase);

                var encryptionKeys = sign
                    ? new EncryptionKeys(publicKeyStream, privateKeyStream, passphrase)
                    : new EncryptionKeys(publicKeyStream);

                using (var pgp = new PGP(encryptionKeys))
                {
                    if (sign)
                        pgp.EncryptAndSign(inputStream, outputStream);
                    else
                        pgp.Encrypt(inputStream, outputStream);
                }
            }
            catch (Exception ex)
            {
                ThrowTranslatedPgpException(ex);
            }
        }

        public static byte[] PgpDecrypt(byte[] inputBytes, Stream privateKeyStream, string passphrase, Stream publicKeyStream = null, bool verifySignature = false)
        {
            using (var inputStream = new MemoryStream(inputBytes))
            using (var outputStream = new MemoryStream())
            {
                PgpDecryptStream(inputStream, outputStream, privateKeyStream, passphrase, publicKeyStream, verifySignature);
                return outputStream.ToArray();
            }
        }

        public static void PgpDecryptStream(Stream inputStream, Stream outputStream, Stream privateKeyStream, string passphrase, Stream publicKeyStream = null, bool verifySignature = false)
        {
            try
            {
                if (verifySignature && publicKeyStream == null)
                    throw new ArgumentException(Properties.UiPath_Cryptography.PgpVerificationRequiresPublicKey);

                var encryptionKeys = verifySignature
                    ? new EncryptionKeys(publicKeyStream, privateKeyStream, passphrase)
                    : new EncryptionKeys(privateKeyStream, passphrase);

                using (var pgp = new PGP(encryptionKeys))
                {
                    if (verifySignature)
                        pgp.DecryptAndVerify(inputStream, outputStream);
                    else
                        pgp.Decrypt(inputStream, outputStream);
                }
            }
            catch (Exception ex)
            {
                ThrowTranslatedPgpException(ex);
            }
        }

        public static string PgpEncryptText(string input, Stream publicKeyStream, Stream privateKeyStream = null, string passphrase = null, bool sign = false)
        {
            try
            {
                if (sign && (privateKeyStream == null || string.IsNullOrEmpty(passphrase)))
                    throw new ArgumentException(Properties.UiPath_Cryptography.PgpSigningRequiresPrivateKeyAndPassphrase);

                var encryptionKeys = sign
                    ? new EncryptionKeys(publicKeyStream, privateKeyStream, passphrase)
                    : new EncryptionKeys(publicKeyStream);

                using (var pgp = new PGP(encryptionKeys))
                {
                    return sign
                        ? pgp.EncryptArmoredStringAndSign(input)
                        : pgp.EncryptArmoredString(input);
                }
            }
            catch (Exception ex)
            {
                ThrowTranslatedPgpException(ex);
                return null; // unreachable
            }
        }

        public static string PgpDecryptText(string input, Stream privateKeyStream, string passphrase, Stream publicKeyStream = null, bool verifySignature = false)
        {
            try
            {
                if (verifySignature && publicKeyStream == null)
                    throw new ArgumentException(Properties.UiPath_Cryptography.PgpVerificationRequiresPublicKey);

                var encryptionKeys = verifySignature
                    ? new EncryptionKeys(publicKeyStream, privateKeyStream, passphrase)
                    : new EncryptionKeys(privateKeyStream, passphrase);

                using (var pgp = new PGP(encryptionKeys))
                {
                    return verifySignature
                        ? pgp.DecryptArmoredStringAndVerify(input)
                        : pgp.DecryptArmoredString(input);
                }
            }
            catch (Exception ex)
            {
                ThrowTranslatedPgpException(ex);
                return null; // unreachable
            }
        }

        public static void PgpGenerateKeyPair(string publicKeyPath, string privateKeyPath, string username, string password)
        {
            try
            {
                using (var pgp = new PGP())
                {
                    pgp.GenerateKey(
                        new FileInfo(publicKeyPath),
                        new FileInfo(privateKeyPath),
                        username,
                        password);
                }
            }
            catch (Exception ex)
            {
                ThrowTranslatedPgpException(ex);
            }
        }

        public static byte[] PgpSign(byte[] inputBytes, Stream privateKeyStream, string passphrase)
            => ExecutePgpSignOperation(inputBytes, privateKeyStream, passphrase, (pgp, input, output) => pgp.Sign(input, output));

        public static byte[] PgpClearSign(byte[] inputBytes, Stream privateKeyStream, string passphrase)
            => ExecutePgpSignOperation(inputBytes, privateKeyStream, passphrase, (pgp, input, output) => pgp.ClearSign(input, output));

        public static bool PgpVerify(byte[] inputBytes, Stream publicKeyStream)
            => ExecutePgpVerifyOperation(inputBytes, publicKeyStream, (pgp, input) => pgp.Verify(input));

        public static bool PgpVerifyClear(byte[] inputBytes, Stream publicKeyStream)
            => ExecutePgpVerifyOperation(inputBytes, publicKeyStream, (pgp, input) => pgp.VerifyClear(input));

        private static byte[] ExecutePgpSignOperation(byte[] inputBytes, Stream privateKeyStream, string passphrase,
            Action<PGP, Stream, Stream> signAction)
        {
            try
            {
                var encryptionKeys = new EncryptionKeys(privateKeyStream, passphrase);
                using (var pgp = new PGP(encryptionKeys))
                using (var inputStream = new MemoryStream(inputBytes))
                using (var outputStream = new MemoryStream())
                {
                    signAction(pgp, inputStream, outputStream);
                    return outputStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                ThrowTranslatedPgpException(ex);
                return null; // unreachable
            }
        }

        private static bool ExecutePgpVerifyOperation(byte[] inputBytes, Stream publicKeyStream,
            Func<PGP, Stream, bool> verifyFunc)
        {
            try
            {
                var encryptionKeys = new EncryptionKeys(publicKeyStream);
                using (var pgp = new PGP(encryptionKeys))
                using (var inputStream = new MemoryStream(inputBytes))
                {
                    return verifyFunc(pgp, inputStream);
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                Trace.TraceWarning("PGP verify operation failed: {0}", ex);
                return false;
            }
        }

        public static bool PgpVerifyPublicKey(Stream publicKeyStream)
        {
            try
            {
                using var decoderStream = Org.BouncyCastle.Bcpg.OpenPgp.PgpUtilities.GetDecoderStream(publicKeyStream);
                var keyRingBundle = new Org.BouncyCastle.Bcpg.OpenPgp.PgpPublicKeyRingBundle(decoderStream);
                return keyRingBundle.Count > 0;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                Trace.TraceWarning("PGP public key verification failed: {0}", ex);
                return false;
            }
        }

        #endregion

        public static byte[] KeyEncoding(Encoding encoding, string key, SecureString keySecureString)
        {
            return key != null ? encoding.GetBytes(key) : encoding.GetBytes(new NetworkCredential("", keySecureString).Password);
        }
    }
}