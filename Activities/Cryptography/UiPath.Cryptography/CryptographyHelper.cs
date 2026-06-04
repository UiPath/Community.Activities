using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Bcpg;
using PgpCore;
using UiPath.Cryptography.Enums;
using UiPath.Cryptography.Properties;

#pragma warning disable CS0618 // obsolete encryption algorithm

namespace UiPath.Cryptography
{
    /// <summary>
    /// Internal implementation helper. <b>Not a contract.</b>
    /// External callers should use the activity surface or
    /// <c>UiPath.Cryptography.Activities.API.ICryptographyService</c> instead — those provide
    /// argument validation, the paired File/Text/Bytes overloads, and a stable API.
    /// </summary>
    /// <remarks>
    /// This class is <c>public</c> only because it crosses assembly boundaries inside the
    /// package (the activities and the coded-workflow service consume it). It cannot be
    /// internalised via <c>InternalsVisibleTo</c> because the friend assemblies share a
    /// project with this one and would collide on duplicated types. Treat it as if it were
    /// internal: it may change, gain or lose methods, or be removed without notice in any
    /// minor release.
    /// </remarks>
    [Obsolete("CryptographyHelper is an internal implementation detail. Use UiPath.Cryptography.Activities.API.ICryptographyService for coded workflows, or the activities for XAML. This class may change or be removed without notice.", error: false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
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
            else if (algorithm == EncryptionAlgorithm.ChaCha20Poly1305)
            {
                return EncryptChaCha20Poly1305(inputBytes, key);
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
            else if (algorithm == EncryptionAlgorithm.ChaCha20Poly1305)
            {
                return DecryptChaCha20Poly1305(inputBytes, key);
            }
            else
            {
                using (SymmetricAlgorithm symmetricAlgorithm = GetSymmetricAlgorithmProvider(algorithm))
                {
                    byte[] salt = new byte[PBKDF2_SaltSizeBytes];
                    byte[] iv = new byte[symmetricAlgorithm.IV.Length];

                    int minimumInputLength = salt.Length + iv.Length;
                    if (inputBytes.Length < minimumInputLength)
                    {
                        throw new CryptographicException(string.Format(Resources.SymmetricDecrypt_InputTooShort, minimumInputLength));
                    }

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
                                try
                                {
                                    decrypted = cryptoStream.ReadToEnd();
                                }
                                catch (CryptographicException ex)
                                {
                                    throw new CryptographicException(Resources.SymmetricDecrypt_PaddingHint, ex);
                                }
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
                case EncryptionAlgorithm.ChaCha20Poly1305: // FIPS 140-3 added it (2023) but not yet broadly validated
                    return false;

                case EncryptionAlgorithm.PGP:
                    // PGP runs through PgpCore + BouncyCastle (the open-source edition, not BC-FIPS),
                    // which is not a CMVP-validated cryptographic module. The algorithms we configure
                    // (RSA, SHA-256, AES-256) are FIPS-approved, but FIPS 140 validates the *module*,
                    // not the algorithm choice — so this path is not FIPS-compliant.
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

        private static SymmetricAlgorithm GetSymmetricAlgorithmProvider(EncryptionAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case EncryptionAlgorithm.AES:
                    return new AesCryptoServiceProvider(); // kept for backwards compat

                case EncryptionAlgorithm.AESGCM:
                case EncryptionAlgorithm.ChaCha20Poly1305:
                    throw new InvalidOperationException(Resources.UnsupportedSymmetricAlgorithmException); //implemented separately as AEAD.

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

        // AEAD primitives (AES-GCM, ChaCha20-Poly1305) share an identical wire layout:
        // salt | iv(12) | ciphertext | tag(16). The only thing that varies is the cipher
        // instance; everything else — PBKDF2 key derivation, IV generation, byte packing —
        // is shared via these two helpers below.

        private const int AeadIvSizeBytes = 12;
        private const int AeadTagSizeBytes = 16;
        private const int AeadKeySizeBytes = 32; // 256-bit key

        private delegate void AeadEncryptCore(byte[] key, byte[] iv, byte[] plain, byte[] cipher, byte[] tag);
        private delegate void AeadDecryptCore(byte[] key, byte[] iv, byte[] cipher, byte[] tag, byte[] plain);

        private static byte[] EncryptAead(byte[] inputBytes, byte[] key, AeadEncryptCore encryptCore)
        {
            InitializeAeadEncryption(out byte[] salt, out byte[] tag, out byte[] algorithmIV);
            byte[] encrypted = new byte[inputBytes.Length];

            using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(key, salt, PBKDF2_Iterations))
            {
                var derivedKey = pbkdf2.GetBytes(AeadKeySizeBytes);
                encryptCore(derivedKey, algorithmIV, inputBytes, encrypted, tag);
            }

            return CreateAeadEncryptionResult(encrypted, salt, tag, algorithmIV);
        }

        private static byte[] DecryptAead(byte[] inputBytes, byte[] key, AeadDecryptCore decryptCore)
        {
            InitializeDecryptAead(inputBytes, out byte[] salt, out byte[] iv, out byte[] tag, out byte[] encryptedData);
            byte[] decrypted = new byte[encryptedData.Length];

            using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(key, salt, PBKDF2_Iterations))
            {
                var derivedKey = pbkdf2.GetBytes(AeadKeySizeBytes);
                decryptCore(derivedKey, iv, encryptedData, tag, decrypted);
            }

            return decrypted;
        }

        private static byte[] EncryptAesGcm(byte[] inputBytes, byte[] key) =>
            EncryptAead(inputBytes, key, (k, iv, plain, cipher, tag) =>
            {
                var aes = new AesGcm(k);
                aes.Encrypt(iv, plain, cipher, tag);
            });

        private static byte[] DecryptAesGcm(byte[] inputBytes, byte[] key) =>
            DecryptAead(inputBytes, key, (k, iv, cipher, tag, plain) =>
            {
                var aes = new AesGcm(k);
                aes.Decrypt(iv, cipher, tag, plain);
            });

        private static byte[] EncryptChaCha20Poly1305(byte[] inputBytes, byte[] key) =>
            EncryptAead(inputBytes, key, (k, iv, plain, cipher, tag) =>
            {
                var chacha = new ChaCha20Poly1305(k);
                chacha.Encrypt(iv, plain, cipher, tag);
            });

        private static byte[] DecryptChaCha20Poly1305(byte[] inputBytes, byte[] key) =>
            DecryptAead(inputBytes, key, (k, iv, cipher, tag, plain) =>
            {
                var chacha = new ChaCha20Poly1305(k);
                chacha.Decrypt(iv, cipher, tag, plain);
            });

        private static void InitializeAeadEncryption(out byte[] salt, out byte[] tag, out byte[] algorithmIV)
        {
            salt = new byte[PBKDF2_SaltSizeBytes];
            tag = new byte[AeadTagSizeBytes];
            algorithmIV = new byte[AeadIvSizeBytes];
            _rng.GetBytes(salt);
            _rng.GetBytes(algorithmIV);
        }

        private static byte[] CreateAeadEncryptionResult(byte[] encrypted, byte[] salt, byte[] tag, byte[] algorithmIV)
        {
            byte[] result = new byte[salt.Length + algorithmIV.Length + encrypted.Length + tag.Length];
            Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
            Buffer.BlockCopy(algorithmIV, 0, result, salt.Length, algorithmIV.Length);
            Buffer.BlockCopy(encrypted, 0, result, salt.Length + algorithmIV.Length, encrypted.Length);
            Buffer.BlockCopy(tag, 0, result, salt.Length + algorithmIV.Length + encrypted.Length, tag.Length);
            return result;
        }

        private static void InitializeDecryptAead(byte[] inputBytes, out byte[] salt, out byte[] iv, out byte[] tag, out byte[] encryptedData)
        {
            salt = new byte[PBKDF2_SaltSizeBytes];
            iv = new byte[AeadIvSizeBytes];
            tag = new byte[AeadTagSizeBytes];
            encryptedData = new byte[inputBytes.Length - salt.Length - iv.Length - tag.Length];
            Buffer.BlockCopy(inputBytes, 0, salt, 0, salt.Length);
            Buffer.BlockCopy(inputBytes, salt.Length, iv, 0, iv.Length);
            Buffer.BlockCopy(inputBytes, salt.Length + iv.Length, encryptedData, 0, encryptedData.Length);
            Buffer.BlockCopy(inputBytes, salt.Length + iv.Length + encryptedData.Length, tag, 0, tag.Length);
        }

        #region PGP Methods

        private static Exception TranslatePgpException(Exception ex)
        {
            var message = ex.Message ?? string.Empty;

            // "Checksum mismatch" → wrong passphrase for private key
            if (message.Contains("Checksum mismatch"))
                return new InvalidOperationException(Resources.PgpInvalidPassphrase, ex);

            // "Secret key for message not found." → wrong private key
            if (message.Contains("Secret key for message not found"))
                return new InvalidOperationException(Resources.PgpPrivateKeyNotFound, ex);

            // "Failed to verify file." → signature verification failed (wrong public key)
            if (message.Contains("Failed to verify"))
                return new InvalidOperationException(Resources.PgpSignatureVerificationFailed, ex);

            // No translation — preserve original stack trace
            ExceptionDispatchInfo.Capture(ex).Throw();
            return ex; // unreachable — satisfies compiler
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
                    pgp.HashAlgorithmTag = HashAlgorithmTag.Sha256;
                    pgp.SymmetricKeyAlgorithm = SymmetricKeyAlgorithmTag.Aes256;
                    if (sign)
                        pgp.EncryptAndSign(inputStream, outputStream);
                    else
                        pgp.Encrypt(inputStream, outputStream);
                }
            }
            catch (Exception ex)
            {
                throw TranslatePgpException(ex);
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
                    pgp.HashAlgorithmTag = HashAlgorithmTag.Sha256;
                    pgp.SymmetricKeyAlgorithm = SymmetricKeyAlgorithmTag.Aes256;
                    if (verifySignature)
                        pgp.DecryptAndVerify(inputStream, outputStream);
                    else
                        pgp.Decrypt(inputStream, outputStream);
                }
            }
            catch (Exception ex)
            {
                throw TranslatePgpException(ex);
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
                    pgp.HashAlgorithmTag = HashAlgorithmTag.Sha256;
                    pgp.SymmetricKeyAlgorithm = SymmetricKeyAlgorithmTag.Aes256;
                    return sign
                        ? pgp.EncryptArmoredStringAndSign(input)
                        : pgp.EncryptArmoredString(input);
                }
            }
            catch (Exception ex)
            {
                throw TranslatePgpException(ex);
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
                    pgp.HashAlgorithmTag = HashAlgorithmTag.Sha256;
                    pgp.SymmetricKeyAlgorithm = SymmetricKeyAlgorithmTag.Aes256;
                    return verifySignature
                        ? pgp.DecryptArmoredStringAndVerify(input)
                        : pgp.DecryptArmoredString(input);
                }
            }
            catch (Exception ex)
            {
                throw TranslatePgpException(ex);
            }
        }

        public static void PgpGenerateKeys(string publicKeyPath, string privateKeyPath, string username, string password, RsaKeySize keySize = RsaKeySize.Rsa4096)
        {
            try
            {
                using (var pgp = new PGP())
                {
                    pgp.GenerateKey(
                        new FileInfo(publicKeyPath),
                        new FileInfo(privateKeyPath),
                        username,
                        password,
                        (int)keySize);
                }
            }
            catch (Exception ex)
            {
                throw TranslatePgpException(ex);
            }
        }

        public static byte[] PgpSign(byte[] inputBytes, Stream privateKeyStream, string passphrase)
            => ExecutePgpSignOperation(inputBytes, privateKeyStream, passphrase, (pgp, input, output) => pgp.Sign(input, output));

        public static byte[] PgpClearSign(byte[] inputBytes, Stream privateKeyStream, string passphrase)
            => ExecutePgpSignOperation(inputBytes, privateKeyStream, passphrase, (pgp, input, output) => pgp.ClearSign(input, output));

        public static string PgpSignText(string input, Stream privateKeyStream, string passphrase)
        {
            try
            {
                var encryptionKeys = new EncryptionKeys(privateKeyStream, passphrase);
                using (var pgp = new PGP(encryptionKeys))
                {
                    pgp.HashAlgorithmTag = HashAlgorithmTag.Sha256;
                    return pgp.SignArmoredString(input);
                }
            }
            catch (Exception ex)
            {
                throw TranslatePgpException(ex);
            }
        }

        public static string PgpClearSignText(string input, Stream privateKeyStream, string passphrase)
        {
            try
            {
                var encryptionKeys = new EncryptionKeys(privateKeyStream, passphrase);
                using (var pgp = new PGP(encryptionKeys))
                {
                    pgp.HashAlgorithmTag = HashAlgorithmTag.Sha256;
                    return pgp.ClearSignArmoredString(input);
                }
            }
            catch (Exception ex)
            {
                throw TranslatePgpException(ex);
            }
        }

        public static bool PgpVerify(byte[] inputBytes, Stream publicKeyStream)
            => ExecutePgpVerifyOperation(inputBytes, publicKeyStream, (pgp, input) => pgp.Verify(input));

        public static bool PgpVerifyClear(byte[] inputBytes, Stream publicKeyStream)
            => ExecutePgpVerifyOperation(inputBytes, publicKeyStream, (pgp, input) => pgp.VerifyClear(input));

        public static bool PgpVerifyText(string input, Stream publicKeyStream)
        {
            try
            {
                var encryptionKeys = new EncryptionKeys(publicKeyStream);
                using (var pgp = new PGP(encryptionKeys))
                {
                    return pgp.VerifyArmoredString(input);
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                Trace.TraceWarning("PGP verify-text operation failed: {0}", ex);
                return false;
            }
        }

        public static bool PgpVerifyClearText(string input, Stream publicKeyStream)
        {
            try
            {
                var encryptionKeys = new EncryptionKeys(publicKeyStream);
                using (var pgp = new PGP(encryptionKeys))
                {
                    return pgp.VerifyClearArmoredString(input);
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                Trace.TraceWarning("PGP verify-clear-text operation failed: {0}", ex);
                return false;
            }
        }

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
                throw TranslatePgpException(ex);
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
                // Wrap in a non-closing stream so disposing the decoder doesn't close the caller's stream
                using var wrapper = new NonClosingStreamWrapper(publicKeyStream);
                using var decoderStream = Org.BouncyCastle.Bcpg.OpenPgp.PgpUtilities.GetDecoderStream(wrapper);
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

        /// <summary>
        /// Stream wrapper that delegates all operations to an inner stream but suppresses Close/Dispose,
        /// preventing BouncyCastle's decoder stream from closing a caller-owned stream.
        /// </summary>
        private sealed class NonClosingStreamWrapper : Stream
        {
            private readonly Stream _inner;
            public NonClosingStreamWrapper(Stream inner) => _inner = inner;
            public override bool CanRead => _inner.CanRead;
            public override bool CanSeek => _inner.CanSeek;
            public override bool CanWrite => _inner.CanWrite;
            public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => _inner.Position = value; }
            public override void Flush() => _inner.Flush();
            public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
            public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
            public override void SetLength(long value) => _inner.SetLength(value);
            public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
            protected override void Dispose(bool disposing) { /* intentionally do not dispose _inner */ }
        }
    }
}