using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
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
    public static class CryptographyHelper
    {
        private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
        private const int PBKDF2_SaltSizeBytes = 8; // Value recommended in literature (64 bit key).
        private const int PBKDF2_Iterations = 10000; // Frozen baseline for SymmetricWireFormat.Classic.

        // OWASP-recommended PBKDF2 iteration counts as of 2026. Used as defaults for the
        // Owasp2026 and OpenSslEnc formats. Snapshot semantics: when OWASP revises these,
        // we add a new SymmetricWireFormat entry (e.g. Owasp2030) rather than mutating these
        // constants, so existing workflows keep producing byte-stable output.
        private const int OwaspIterations_Sha1 = 1_300_000;
        private const int OwaspIterations_Sha256 = 600_000;

        // "Salted__" magic prefix used by openssl enc.
        private static readonly byte[] OpenSslMagic = new byte[] { 0x53, 0x61, 0x6C, 0x74, 0x65, 0x64, 0x5F, 0x5F };

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

        // Public entry — Classic format (frozen at PBKDF2_Iterations = 10000, PBKDF2-HMAC-SHA1).
        // Wire layout: salt(8) || IV || ciphertext, or salt(8) || IV(12) || ciphertext || tag(16) for AEAD.
        public static byte[] EncryptData(EncryptionAlgorithm algorithm, byte[] inputBytes, byte[] key)
            => EncryptDataCore(algorithm, inputBytes, key, PBKDF2_Iterations);

        public static byte[] DecryptData(EncryptionAlgorithm algorithm, byte[] inputBytes, byte[] key)
            => DecryptDataCore(algorithm, inputBytes, key, PBKDF2_Iterations);

        // Public entry — UiPath layout with caller-supplied PBKDF2-HMAC-SHA1 iteration count.
        // Used by SymmetricWireFormat.Owasp2026 (default 1,300,000) and any future year-versioned
        // snapshots that share the Classic wire layout. Output is byte-identical to Classic when iterations match.
        public static byte[] EncryptDataWithIterations(EncryptionAlgorithm algorithm, byte[] inputBytes, byte[] passwordBytes, int iterations)
            => EncryptDataCore(algorithm, inputBytes, passwordBytes, iterations);

        public static byte[] DecryptDataWithIterations(EncryptionAlgorithm algorithm, byte[] inputBytes, byte[] passwordBytes, int iterations)
            => DecryptDataCore(algorithm, inputBytes, passwordBytes, iterations);

        private static byte[] EncryptDataCore(EncryptionAlgorithm algorithm, byte[] inputBytes, byte[] passwordBytes, int iterations)
        {
            if (algorithm == EncryptionAlgorithm.PGP)
                throw new ArgumentException("Use PGP-specific methods for PGP encryption.", nameof(algorithm));

            if (algorithm == EncryptionAlgorithm.AESGCM)
                return EncryptAesGcm(inputBytes, passwordBytes, iterations);
            if (algorithm == EncryptionAlgorithm.ChaCha20Poly1305)
                return EncryptChaCha20Poly1305(inputBytes, passwordBytes, iterations);

            byte[] result;
            using (SymmetricAlgorithm symmetricAlgorithm = GetSymmetricAlgorithmProvider(algorithm))
            {
                byte[] encrypted;
                byte[] salt = new byte[PBKDF2_SaltSizeBytes];
                int maxKeySize = GetLegalKeySizes(symmetricAlgorithm).Max();

                _rng.GetBytes(salt);
                using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(passwordBytes, salt, iterations))
                {
                    symmetricAlgorithm.Key = pbkdf2.GetBytes(maxKeySize);
                }

                using (ICryptoTransform cryptoTransform = symmetricAlgorithm.CreateEncryptor())
                using (MemoryStream inputStream = new MemoryStream(inputBytes), transformedStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(inputStream, cryptoTransform, CryptoStreamMode.Read))
                    {
                        cryptoStream.CopyTo(transformedStream);
                    }
                    encrypted = transformedStream.ToArray();
                }

                result = new byte[salt.Length + symmetricAlgorithm.IV.Length + encrypted.Length];
                Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
                Buffer.BlockCopy(symmetricAlgorithm.IV, 0, result, salt.Length, symmetricAlgorithm.IV.Length);
                Buffer.BlockCopy(encrypted, 0, result, salt.Length + symmetricAlgorithm.IV.Length, encrypted.Length);
            }
            return result;
        }

        private static byte[] DecryptDataCore(EncryptionAlgorithm algorithm, byte[] inputBytes, byte[] passwordBytes, int iterations)
        {
            if (algorithm == EncryptionAlgorithm.PGP)
                throw new ArgumentException("Use PGP-specific methods for PGP decryption.", nameof(algorithm));

            if (algorithm == EncryptionAlgorithm.AESGCM)
                return DecryptAesGcm(inputBytes, passwordBytes, iterations);
            if (algorithm == EncryptionAlgorithm.ChaCha20Poly1305)
                return DecryptChaCha20Poly1305(inputBytes, passwordBytes, iterations);

            byte[] decrypted;
            using (SymmetricAlgorithm symmetricAlgorithm = GetSymmetricAlgorithmProvider(algorithm))
            {
                byte[] salt = new byte[PBKDF2_SaltSizeBytes];
                byte[] iv = new byte[symmetricAlgorithm.IV.Length];

                int minimumInputLength = salt.Length + iv.Length;
                if (inputBytes.Length < minimumInputLength)
                    throw new CryptographicException(string.Format(Resources.SymmetricDecrypt_InputTooShort, minimumInputLength));

                byte[] encryptedData = new byte[inputBytes.Length - salt.Length - iv.Length];
                int maxKeySize = GetLegalKeySizes(symmetricAlgorithm).Max();

                Buffer.BlockCopy(inputBytes, 0, salt, 0, salt.Length);
                Buffer.BlockCopy(inputBytes, salt.Length, iv, 0, iv.Length);
                Buffer.BlockCopy(inputBytes, salt.Length + iv.Length, encryptedData, 0, encryptedData.Length);

                symmetricAlgorithm.IV = iv;
                using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(passwordBytes, salt, iterations))
                {
                    symmetricAlgorithm.Key = pbkdf2.GetBytes(maxKeySize);
                }

                using (ICryptoTransform cryptoTransform = symmetricAlgorithm.CreateDecryptor())
                using (MemoryStream encryptedStream = new MemoryStream(encryptedData))
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

        [SuppressMessage("Security", "CA5350:Do not use Weak Cryptographic Algorithms",
            Justification = "DES/TripleDES/RC2 and Rijndael are exposed only via the [Obsolete] members of EncryptionAlgorithm so that workflows authored before this guidance landed continue to roundtrip. AEAD (AES-GCM / ChaCha20-Poly1305) is the recommended path for new workflows.")]
        [SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms",
            Justification = "Same backward-compatibility rationale as CA5350: DES/TripleDES selection is opt-in via the [Obsolete] enum value, not the default.")]
        private static SymmetricAlgorithm GetSymmetricAlgorithmProvider(EncryptionAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case EncryptionAlgorithm.AES:
                    return new AesCryptoServiceProvider(); // kept for backwards compatibility

                case EncryptionAlgorithm.AESGCM:
                case EncryptionAlgorithm.ChaCha20Poly1305:
                    throw new InvalidOperationException(Resources.UnsupportedSymmetricAlgorithmException); //implemented separately as AEAD.

                case EncryptionAlgorithm.DES:
                    return new DESCryptoServiceProvider(); // kept for backwards compatibility

                case EncryptionAlgorithm.RC2:
                    return new RC2CryptoServiceProvider(); // kept for backwards compatibility

                case EncryptionAlgorithm.Rijndael:
                    return new RijndaelManaged(); // kept for backwards compatibility

                case EncryptionAlgorithm.TripleDES:
                    return new TripleDESCryptoServiceProvider(); // kept for backwards compatibility

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

        private static byte[] EncryptAead(byte[] inputBytes, byte[] key, int iterations, AeadEncryptCore encryptCore)
        {
            InitializeAeadEncryption(out byte[] salt, out byte[] tag, out byte[] algorithmIV);
            byte[] encrypted = new byte[inputBytes.Length];

            using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(key, salt, iterations))
            {
                var derivedKey = pbkdf2.GetBytes(AeadKeySizeBytes);
                encryptCore(derivedKey, algorithmIV, inputBytes, encrypted, tag);
            }

            return CreateAeadEncryptionResult(encrypted, salt, tag, algorithmIV);
        }

        private static byte[] DecryptAead(byte[] inputBytes, byte[] key, int iterations, AeadDecryptCore decryptCore)
        {
            const int aeadMinimumInputLength = PBKDF2_SaltSizeBytes + AeadIvSizeBytes + AeadTagSizeBytes;
            if (inputBytes == null || inputBytes.Length < aeadMinimumInputLength)
                throw new CryptographicException(string.Format(Resources.SymmetricDecrypt_InputTooShort, aeadMinimumInputLength));

            InitializeDecryptAead(inputBytes, out byte[] salt, out byte[] iv, out byte[] tag, out byte[] encryptedData);
            byte[] decrypted = new byte[encryptedData.Length];

            using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(key, salt, iterations))
            {
                var derivedKey = pbkdf2.GetBytes(AeadKeySizeBytes);
                decryptCore(derivedKey, iv, encryptedData, tag, decrypted);
            }

            return decrypted;
        }

        private static byte[] EncryptAesGcm(byte[] inputBytes, byte[] key, int iterations) =>
            EncryptAead(inputBytes, key, iterations, (k, iv, plain, cipher, tag) =>
            {
                using var aes = new AesGcm(k);
                aes.Encrypt(iv, plain, cipher, tag);
            });

        private static byte[] DecryptAesGcm(byte[] inputBytes, byte[] key, int iterations) =>
            DecryptAead(inputBytes, key, iterations, (k, iv, cipher, tag, plain) =>
            {
                using var aes = new AesGcm(k);
                aes.Decrypt(iv, cipher, tag, plain);
            });

        private static byte[] EncryptChaCha20Poly1305(byte[] inputBytes, byte[] key, int iterations) =>
            EncryptAead(inputBytes, key, iterations, (k, iv, plain, cipher, tag) =>
            {
                if (!ChaCha20Poly1305.IsSupported)
                    throw new PlatformNotSupportedException(Resources.ChaCha20Poly1305NotSupported);
                using var chacha = new ChaCha20Poly1305(k);
                chacha.Encrypt(iv, plain, cipher, tag);
            });

        private static byte[] DecryptChaCha20Poly1305(byte[] inputBytes, byte[] key, int iterations) =>
            DecryptAead(inputBytes, key, iterations, (k, iv, cipher, tag, plain) =>
            {
                if (!ChaCha20Poly1305.IsSupported)
                    throw new PlatformNotSupportedException(Resources.ChaCha20Poly1305NotSupported);
                using var chacha = new ChaCha20Poly1305(k);
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

        #region Third-party-compatible formats (Raw, OpenSslEnc)

        /// <summary>
        /// Returns the OWASP-recommended PBKDF2 iteration count for the given format.
        /// Throws for formats that do not run a KDF (Classic uses a fixed 10 000; Raw skips the KDF entirely).
        /// </summary>
        public static int GetRecommendedIterations(SymmetricWireFormat format) => format switch
        {
            SymmetricWireFormat.Owasp2026 => OwaspIterations_Sha1,
            SymmetricWireFormat.OpenSslEnc => OwaspIterations_Sha256,
            _ => throw new ArgumentException(
                $"GetRecommendedIterations is undefined for {format}: Classic is frozen at 10 000 iterations and Raw skips the KDF.",
                nameof(format))
        };

        /// <summary>
        /// Legal key sizes (in bytes) for the algorithm when supplied as a raw key.
        /// AEAD algorithms accept only 32-byte (256-bit) keys; non-AEAD algorithms forward to the underlying SymmetricAlgorithm.
        /// </summary>
        public static int[] GetRawKeySizes(EncryptionAlgorithm algorithm)
        {
            if (algorithm == EncryptionAlgorithm.AESGCM || algorithm == EncryptionAlgorithm.ChaCha20Poly1305)
                return new[] { AeadKeySizeBytes };
            using var symmetricAlgorithm = GetSymmetricAlgorithmProvider(algorithm);
            return GetLegalKeySizes(symmetricAlgorithm);
        }

        /// <summary>
        /// IV size (in bytes) for the algorithm. AEAD algorithms always use 12-byte IVs;
        /// non-AEAD algorithms use the SymmetricAlgorithm's natural block size.
        /// </summary>
        public static int GetIvSize(EncryptionAlgorithm algorithm)
        {
            if (algorithm == EncryptionAlgorithm.AESGCM || algorithm == EncryptionAlgorithm.ChaCha20Poly1305)
                return AeadIvSizeBytes;
            using var symmetricAlgorithm = GetSymmetricAlgorithmProvider(algorithm);
            return symmetricAlgorithm.IV.Length;
        }

        /// <summary>
        /// Raw-key encrypt: caller supplies the literal cipher key bytes and (optionally) the IV.
        /// Output layout: <c>IV || ciphertext</c> for non-AEAD; <c>IV(12) || ciphertext || tag(16)</c> for AEAD.
        /// If <paramref name="iv"/> is <c>null</c>, a fresh random IV is generated.
        /// </summary>
        public static byte[] EncryptDataRaw(EncryptionAlgorithm algorithm, byte[] inputBytes, byte[] keyBytes, byte[] iv)
        {
            if (algorithm == EncryptionAlgorithm.PGP)
                throw new ArgumentException("Use PGP-specific methods for PGP encryption.", nameof(algorithm));

            if (algorithm == EncryptionAlgorithm.AESGCM || algorithm == EncryptionAlgorithm.ChaCha20Poly1305)
            {
                byte[] effectiveIv = iv;
                if (effectiveIv == null)
                {
                    effectiveIv = new byte[AeadIvSizeBytes];
                    _rng.GetBytes(effectiveIv);
                }
                if (effectiveIv.Length != AeadIvSizeBytes)
                    throw new ArgumentException($"AEAD IV must be exactly {AeadIvSizeBytes} bytes; got {effectiveIv.Length}.", nameof(iv));

                byte[] cipherText = new byte[inputBytes.Length];
                byte[] tag = new byte[AeadTagSizeBytes];

                if (algorithm == EncryptionAlgorithm.AESGCM)
                {
                    using var aes = new AesGcm(keyBytes);
                    aes.Encrypt(effectiveIv, inputBytes, cipherText, tag);
                }
                else
                {
                    if (!ChaCha20Poly1305.IsSupported)
                        throw new PlatformNotSupportedException(Resources.ChaCha20Poly1305NotSupported);
                    using var chacha = new ChaCha20Poly1305(keyBytes);
                    chacha.Encrypt(effectiveIv, inputBytes, cipherText, tag);
                }

                byte[] result = new byte[effectiveIv.Length + cipherText.Length + tag.Length];
                Buffer.BlockCopy(effectiveIv, 0, result, 0, effectiveIv.Length);
                Buffer.BlockCopy(cipherText, 0, result, effectiveIv.Length, cipherText.Length);
                Buffer.BlockCopy(tag, 0, result, effectiveIv.Length + cipherText.Length, tag.Length);
                return result;
            }

            using var symmetric = GetSymmetricAlgorithmProvider(algorithm);
            symmetric.Key = keyBytes;
            if (iv != null)
            {
                if (iv.Length != symmetric.IV.Length)
                    throw new ArgumentException($"IV for {algorithm} must be {symmetric.IV.Length} bytes; got {iv.Length}.", nameof(iv));
                symmetric.IV = iv;
            }

            byte[] encrypted;
            using (ICryptoTransform transform = symmetric.CreateEncryptor())
            using (MemoryStream inputStream = new MemoryStream(inputBytes), outStream = new MemoryStream())
            {
                using (CryptoStream cryptoStream = new CryptoStream(inputStream, transform, CryptoStreamMode.Read))
                    cryptoStream.CopyTo(outStream);
                encrypted = outStream.ToArray();
            }

            byte[] rawResult = new byte[symmetric.IV.Length + encrypted.Length];
            Buffer.BlockCopy(symmetric.IV, 0, rawResult, 0, symmetric.IV.Length);
            Buffer.BlockCopy(encrypted, 0, rawResult, symmetric.IV.Length, encrypted.Length);
            return rawResult;
        }

        /// <summary>
        /// Raw-key decrypt: parses <c>IV || ciphertext [|| tag]</c>, using the caller-supplied raw key.
        /// </summary>
        public static byte[] DecryptDataRaw(EncryptionAlgorithm algorithm, byte[] inputBytes, byte[] keyBytes)
        {
            if (algorithm == EncryptionAlgorithm.PGP)
                throw new ArgumentException("Use PGP-specific methods for PGP decryption.", nameof(algorithm));

            if (algorithm == EncryptionAlgorithm.AESGCM || algorithm == EncryptionAlgorithm.ChaCha20Poly1305)
            {
                int minLen = AeadIvSizeBytes + AeadTagSizeBytes;
                if (inputBytes == null || inputBytes.Length < minLen)
                    throw new CryptographicException(string.Format(Resources.SymmetricDecrypt_InputTooShort, minLen));

                byte[] iv = new byte[AeadIvSizeBytes];
                byte[] tag = new byte[AeadTagSizeBytes];
                byte[] cipher = new byte[inputBytes.Length - iv.Length - tag.Length];
                Buffer.BlockCopy(inputBytes, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(inputBytes, iv.Length, cipher, 0, cipher.Length);
                Buffer.BlockCopy(inputBytes, iv.Length + cipher.Length, tag, 0, tag.Length);

                byte[] plain = new byte[cipher.Length];
                if (algorithm == EncryptionAlgorithm.AESGCM)
                {
                    using var aes = new AesGcm(keyBytes);
                    aes.Decrypt(iv, cipher, tag, plain);
                }
                else
                {
                    if (!ChaCha20Poly1305.IsSupported)
                        throw new PlatformNotSupportedException(Resources.ChaCha20Poly1305NotSupported);
                    using var chacha = new ChaCha20Poly1305(keyBytes);
                    chacha.Decrypt(iv, cipher, tag, plain);
                }
                return plain;
            }

            using var symmetric = GetSymmetricAlgorithmProvider(algorithm);
            int ivLen = symmetric.IV.Length;
            if (inputBytes == null || inputBytes.Length < ivLen)
                throw new CryptographicException(string.Format(Resources.SymmetricDecrypt_InputTooShort, ivLen));

            byte[] symIv = new byte[ivLen];
            byte[] symCipher = new byte[inputBytes.Length - ivLen];
            Buffer.BlockCopy(inputBytes, 0, symIv, 0, ivLen);
            Buffer.BlockCopy(inputBytes, ivLen, symCipher, 0, symCipher.Length);

            symmetric.IV = symIv;
            symmetric.Key = keyBytes;

            byte[] decrypted;
            using (ICryptoTransform transform = symmetric.CreateDecryptor())
            using (MemoryStream encStream = new MemoryStream(symCipher))
            using (CryptoStream cryptoStream = new CryptoStream(encStream, transform, CryptoStreamMode.Read))
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
            return decrypted;
        }

        /// <summary>
        /// OpenSSL <c>enc</c>-compatible encrypt: layout <c>Salted__(8) || salt(8) || ciphertext [|| tag]</c>,
        /// key (and IV, for non-AEAD and AEAD alike) derived via PBKDF2-HMAC-SHA256.
        /// AEAD layout is a UiPath extension — see <c>docs/symmetric-wire-format.md</c>.
        /// </summary>
        public static byte[] EncryptDataOpenSslEnc(EncryptionAlgorithm algorithm, byte[] inputBytes, byte[] passwordBytes, int iterations, AesKeySize aesKeySize = AesKeySize.Aes256)
        {
            if (algorithm == EncryptionAlgorithm.PGP)
                throw new ArgumentException("Use PGP-specific methods for PGP encryption.", nameof(algorithm));

            byte[] salt = new byte[PBKDF2_SaltSizeBytes];
            _rng.GetBytes(salt);

            if (algorithm == EncryptionAlgorithm.AESGCM || algorithm == EncryptionAlgorithm.ChaCha20Poly1305)
            {
                var (aeadKey, aeadIv) = DeriveOpenSslKeyAndIv(passwordBytes, salt, iterations, AeadKeySizeBytes, AeadIvSizeBytes);
                byte[] cipher = new byte[inputBytes.Length];
                byte[] tag = new byte[AeadTagSizeBytes];

                if (algorithm == EncryptionAlgorithm.AESGCM)
                {
                    using var aes = new AesGcm(aeadKey);
                    aes.Encrypt(aeadIv, inputBytes, cipher, tag);
                }
                else
                {
                    if (!ChaCha20Poly1305.IsSupported)
                        throw new PlatformNotSupportedException(Resources.ChaCha20Poly1305NotSupported);
                    using var chacha = new ChaCha20Poly1305(aeadKey);
                    chacha.Encrypt(aeadIv, inputBytes, cipher, tag);
                }

                byte[] aeadResult = new byte[OpenSslMagic.Length + salt.Length + cipher.Length + tag.Length];
                Buffer.BlockCopy(OpenSslMagic, 0, aeadResult, 0, OpenSslMagic.Length);
                Buffer.BlockCopy(salt, 0, aeadResult, OpenSslMagic.Length, salt.Length);
                Buffer.BlockCopy(cipher, 0, aeadResult, OpenSslMagic.Length + salt.Length, cipher.Length);
                Buffer.BlockCopy(tag, 0, aeadResult, OpenSslMagic.Length + salt.Length + cipher.Length, tag.Length);
                return aeadResult;
            }

            using var symmetric = GetSymmetricAlgorithmProvider(algorithm);
            int symKeySize = (algorithm == EncryptionAlgorithm.AES)
                ? (int)aesKeySize / 8
                : GetLegalKeySizes(symmetric).Max();
            int symIvSize = symmetric.IV.Length;
            var (symKey, symIv) = DeriveOpenSslKeyAndIv(passwordBytes, salt, iterations, symKeySize, symIvSize);
            symmetric.Key = symKey;
            symmetric.IV = symIv;

            byte[] encrypted;
            using (ICryptoTransform transform = symmetric.CreateEncryptor())
            using (MemoryStream inputStream = new MemoryStream(inputBytes), outStream = new MemoryStream())
            {
                using (CryptoStream cryptoStream = new CryptoStream(inputStream, transform, CryptoStreamMode.Read))
                    cryptoStream.CopyTo(outStream);
                encrypted = outStream.ToArray();
            }

            byte[] result = new byte[OpenSslMagic.Length + salt.Length + encrypted.Length];
            Buffer.BlockCopy(OpenSslMagic, 0, result, 0, OpenSslMagic.Length);
            Buffer.BlockCopy(salt, 0, result, OpenSslMagic.Length, salt.Length);
            Buffer.BlockCopy(encrypted, 0, result, OpenSslMagic.Length + salt.Length, encrypted.Length);
            return result;
        }

        /// <summary>
        /// OpenSSL <c>enc</c>-compatible decrypt: parses <c>Salted__(8) || salt(8) || ciphertext [|| tag]</c>.
        /// </summary>
        public static byte[] DecryptDataOpenSslEnc(EncryptionAlgorithm algorithm, byte[] inputBytes, byte[] passwordBytes, int iterations, AesKeySize aesKeySize = AesKeySize.Aes256)
        {
            if (algorithm == EncryptionAlgorithm.PGP)
                throw new ArgumentException("Use PGP-specific methods for PGP decryption.", nameof(algorithm));

            int prefixLen = OpenSslMagic.Length + PBKDF2_SaltSizeBytes;
            if (inputBytes == null || inputBytes.Length < prefixLen)
                throw new CryptographicException(string.Format(Resources.SymmetricDecrypt_InputTooShort, prefixLen));

            for (int i = 0; i < OpenSslMagic.Length; i++)
            {
                if (inputBytes[i] != OpenSslMagic[i])
                    throw new CryptographicException(Resources.OpenSslEnc_MissingMagic);
            }

            byte[] salt = new byte[PBKDF2_SaltSizeBytes];
            Buffer.BlockCopy(inputBytes, OpenSslMagic.Length, salt, 0, PBKDF2_SaltSizeBytes);

            if (algorithm == EncryptionAlgorithm.AESGCM || algorithm == EncryptionAlgorithm.ChaCha20Poly1305)
            {
                int minAead = prefixLen + AeadTagSizeBytes;
                if (inputBytes.Length < minAead)
                    throw new CryptographicException(string.Format(Resources.SymmetricDecrypt_InputTooShort, minAead));

                var (aeadKey, aeadIv) = DeriveOpenSslKeyAndIv(passwordBytes, salt, iterations, AeadKeySizeBytes, AeadIvSizeBytes);
                int cipherLen = inputBytes.Length - prefixLen - AeadTagSizeBytes;
                byte[] cipher = new byte[cipherLen];
                byte[] tag = new byte[AeadTagSizeBytes];
                byte[] plain = new byte[cipherLen];
                Buffer.BlockCopy(inputBytes, prefixLen, cipher, 0, cipherLen);
                Buffer.BlockCopy(inputBytes, prefixLen + cipherLen, tag, 0, AeadTagSizeBytes);

                if (algorithm == EncryptionAlgorithm.AESGCM)
                {
                    using var aes = new AesGcm(aeadKey);
                    aes.Decrypt(aeadIv, cipher, tag, plain);
                }
                else
                {
                    if (!ChaCha20Poly1305.IsSupported)
                        throw new PlatformNotSupportedException(Resources.ChaCha20Poly1305NotSupported);
                    using var chacha = new ChaCha20Poly1305(aeadKey);
                    chacha.Decrypt(aeadIv, cipher, tag, plain);
                }
                return plain;
            }

            using var symmetric = GetSymmetricAlgorithmProvider(algorithm);
            int symKeySize = (algorithm == EncryptionAlgorithm.AES)
                ? (int)aesKeySize / 8
                : GetLegalKeySizes(symmetric).Max();
            int symIvSize = symmetric.IV.Length;
            var (symKey, symIv) = DeriveOpenSslKeyAndIv(passwordBytes, salt, iterations, symKeySize, symIvSize);
            symmetric.Key = symKey;
            symmetric.IV = symIv;

            byte[] encrypted = new byte[inputBytes.Length - prefixLen];
            Buffer.BlockCopy(inputBytes, prefixLen, encrypted, 0, encrypted.Length);

            byte[] decrypted;
            using (ICryptoTransform transform = symmetric.CreateDecryptor())
            using (MemoryStream encStream = new MemoryStream(encrypted))
            using (CryptoStream cryptoStream = new CryptoStream(encStream, transform, CryptoStreamMode.Read))
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
            return decrypted;
        }

        private static (byte[] key, byte[] iv) DeriveOpenSslKeyAndIv(byte[] password, byte[] salt, int iterations, int keySize, int ivSize)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            byte[] derived = pbkdf2.GetBytes(keySize + ivSize);
            byte[] key = new byte[keySize];
            byte[] iv = new byte[ivSize];
            Buffer.BlockCopy(derived, 0, key, 0, keySize);
            Buffer.BlockCopy(derived, keySize, iv, 0, ivSize);
            return (key, iv);
        }

        #endregion

        #region PGP Methods

        private static Exception TranslatePgpException(Exception ex)
        {
            // PgpCore 8 reports these cases with typed exceptions; they map to the same messages as the
            // BouncyCastle texts matched below, which stay for the errors BouncyCastle still raises itself.
            switch (ex)
            {
                case IncorrectPassphraseException:
                    return new InvalidOperationException(Resources.PgpInvalidPassphrase, ex);
                case NoDecryptionKeyException:
                    return new InvalidOperationException(Resources.PgpPrivateKeyNotFound, ex);
                case UnsupportedAeadException:
                    return new InvalidOperationException(Resources.PgpAeadNotSupported, ex);
                case NoEncryptionKeyException:
                    return new InvalidOperationException(Resources.PgpEncryptionKeyNotUsable, ex);
                case NoSigningKeyException:
                    return new InvalidOperationException(Resources.PgpSigningKeyNotUsable, ex);
                case NotEncryptedDataException:
                    return new InvalidOperationException(Resources.PgpInputNotEncrypted, ex);
                case MessageIntegrityException:
                    return new InvalidOperationException(Resources.PgpMessageIntegrityFailed, ex);
            }

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

            // GnuPG 2.4+ AEAD encryption (tag 20, OCB cipher) → BouncyCastle's PgpObjectFactory
            // rejects the top-level packet with "unknown object in stream 20" (Bcpg's lower-level
            // packet reader uses a differently worded "unknown packet type encountered: <tag>" for
            // a similar failure elsewhere; both are matched since either could surface here).
            // STUD-80428: GnuPG 2.4+ creates AEAD encrypted packets which BouncyCastle doesn't support
            // The tag is rendered through PacketTag so the match survives a BouncyCastle upgrade that
            // gives value 20 a name (RFC 9580 AEAD packet) instead of printing the bare number.
            var aeadTag = ((PacketTag)20).ToString();
            if (message.Contains($"unknown object in stream {aeadTag}") || message.Contains($"unknown packet type encountered: {aeadTag}"))
                return new InvalidOperationException(Resources.PgpAeadNotSupported, ex);

            // BouncyCastle 2.7+ no longer knows tag 20 (reserved in RFC 9580): the packet reaches PgpUnknown
            // and ReadPacket fails with "invalid header encountered". Matching the PgpUnknown frame keeps a
            // damaged header inside a known packet from being reported as AEAD.
            if (ex is IOException && message.Contains("invalid header encountered") && (ex.StackTrace?.Contains("PgpUnknown") ?? false))
                return new InvalidOperationException(Resources.PgpAeadNotSupported, ex);

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
                    // PgpCore 8 compresses by default; keep the uncompressed output earlier versions produced.
                    pgp.CompressionAlgorithm = CompressionAlgorithmTag.Uncompressed;
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

                if (verifySignature)
                {
                    DecryptAndVerifyWithFallback(inputStream, outputStream, privateKeyStream, passphrase, publicKeyStream);
                    return;
                }

                var encryptionKeys = new EncryptionKeys(privateKeyStream, passphrase);
                using (var pgp = new PGP(encryptionKeys))
                {
                    pgp.HashAlgorithmTag = HashAlgorithmTag.Sha256;
                    pgp.CompressionAlgorithm = CompressionAlgorithmTag.Uncompressed;
                    pgp.SymmetricKeyAlgorithm = SymmetricKeyAlgorithmTag.Aes256;
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
                    pgp.CompressionAlgorithm = CompressionAlgorithmTag.Uncompressed;
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

                if (verifySignature)
                {
                    return DecryptTextAndVerifyWithFallback(input, privateKeyStream, passphrase, publicKeyStream);
                }

                var encryptionKeys = new EncryptionKeys(privateKeyStream, passphrase);
                using (var pgp = new PGP(encryptionKeys))
                {
                    pgp.HashAlgorithmTag = HashAlgorithmTag.Sha256;
                    pgp.CompressionAlgorithm = CompressionAlgorithmTag.Uncompressed;
                    pgp.SymmetricKeyAlgorithm = SymmetricKeyAlgorithmTag.Aes256;
                    return pgp.DecryptArmoredString(input);
                }
            }
            catch (Exception ex)
            {
                throw TranslatePgpException(ex);
            }
        }

        // STUD-81645: decrypt + verify. PgpCore's DecryptAndVerify is the first attempt, but only for its
        // exceptions: what the caller receives is the plaintext our own BouncyCastle evaluation hashed and
        // vetted (signer key revocation, expiry, subkey binding, message layout), so the released bytes are
        // the verified bytes by construction.
        //  - when PgpCore fails (for example a multi-signer message whose first signer is not in the ring)
        //    and the evaluation does not verify, the ORIGINAL PgpCore exception is thrown, so wrong
        //    passphrase / missing private key / failed verification keep their translated messages;
        //  - when PgpCore succeeds and the evaluation does not verify, verification failed.
        // Plaintext is only released after verification: it is buffered and copied to the caller's stream.
        private static void DecryptAndVerifyWithFallback(Stream inputStream, Stream outputStream,
            Stream privateKeyStream, string passphrase, Stream publicKeyStream)
        {
            var inputBytes = ReadAllBytes(inputStream);
            var privateKeyBytes = ReadAllBytes(privateKeyStream);
            var publicKeyBytes = ReadAllBytes(publicKeyStream);

            var pgpCoreFailure = TryPgpCoreDecryptAndVerify(
                privateKeyBytes, publicKeyBytes, passphrase,
                pgp => pgp.DecryptAndVerify(new MemoryStream(inputBytes), Stream.Null));

            using var verifiedPlaintext = new MemoryStream();
            ThrowUnlessVerified(
                TryBcEvaluateDecryptAndVerify(inputBytes, privateKeyBytes, passphrase, publicKeyBytes, verifiedPlaintext),
                pgpCoreFailure);

            verifiedPlaintext.Position = 0;
            verifiedPlaintext.CopyTo(outputStream);
        }

        private static string DecryptTextAndVerifyWithFallback(string input, Stream privateKeyStream,
            string passphrase, Stream publicKeyStream)
        {
            var privateKeyBytes = ReadAllBytes(privateKeyStream);
            var publicKeyBytes = ReadAllBytes(publicKeyStream);

            var pgpCoreFailure = TryPgpCoreDecryptAndVerify(
                privateKeyBytes, publicKeyBytes, passphrase,
                pgp => pgp.DecryptArmoredStringAndVerify(input));

            using var verifiedPlaintext = new MemoryStream();
            ThrowUnlessVerified(
                TryBcEvaluateDecryptAndVerify(ToUtf8Bytes(input), privateKeyBytes, passphrase, publicKeyBytes, verifiedPlaintext),
                pgpCoreFailure);

            return ReadPlaintextText(verifiedPlaintext);
        }

        private static void ThrowUnlessVerified(BcVerdict verdict, Exception pgpCoreFailure)
        {
            if (verdict == BcVerdict.Verified)
            {
                return;
            }

            if (pgpCoreFailure is not null)
            {
                ExceptionDispatchInfo.Capture(pgpCoreFailure).Throw();
            }

            throw new InvalidOperationException(Resources.PgpSignatureVerificationFailed);
        }

        // Decodes like PgpCore's string API: a byte-order mark selects the encoding (UTF-8 when there is none) and is not part of the text.
        internal static string ReadPlaintextText(Stream plaintext)
        {
            plaintext.Position = 0;
            using var reader = new StreamReader(plaintext);
            return reader.ReadToEnd();
        }

        private static Exception TryPgpCoreDecryptAndVerify(byte[] privateKeyBytes, byte[] publicKeyBytes,
            string passphrase, Action<PGP> decryptAndVerify)
        {
            try
            {
                var encryptionKeys = new EncryptionKeys(
                    new MemoryStream(publicKeyBytes), new MemoryStream(privateKeyBytes), passphrase);
                using (var pgp = new PGP(encryptionKeys))
                {
                    pgp.HashAlgorithmTag = HashAlgorithmTag.Sha256;
                    pgp.CompressionAlgorithm = CompressionAlgorithmTag.Uncompressed;
                    pgp.SymmetricKeyAlgorithm = SymmetricKeyAlgorithmTag.Aes256;
                    decryptAndVerify(pgp);
                }

                return null;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                return ex;
            }
        }

        private static BcVerdict TryBcEvaluateDecryptAndVerify(byte[] inputBytes, byte[] privateKeyBytes, string passphrase,
            byte[] publicKeyBytes, Stream verifiedPlaintext)
        {
            try
            {
                return BcEvaluateDecryptAndVerify(inputBytes, privateKeyBytes, passphrase, publicKeyBytes, verifiedPlaintext);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                Trace.TraceWarning("PGP decrypt-and-verify (BouncyCastle evaluation) failed: {0}", ex);
                return BcVerdict.Rejected;
            }
        }

        private static byte[] ReadAllBytes(Stream stream)
        {
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            return buffer.ToArray();
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
                    pgp.CompressionAlgorithm = CompressionAlgorithmTag.Uncompressed;
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
                    pgp.CompressionAlgorithm = CompressionAlgorithmTag.Uncompressed;
                    return pgp.ClearSignArmoredString(input);
                }
            }
            catch (Exception ex)
            {
                throw TranslatePgpException(ex);
            }
        }

        public static bool PgpVerify(byte[] inputBytes, Stream publicKeyStream)
            => ExecutePgpVerifyOperation(publicKeyStream, () => inputBytes, BcEvaluateBinarySignature);

        public static bool PgpVerifyClear(byte[] inputBytes, Stream publicKeyStream)
            => ExecutePgpVerifyOperation(publicKeyStream, () => inputBytes, BcEvaluateClearSignature);

        // Text entry points share ExecutePgpVerifyOperation with the byte[] entry points, so armored
        // subkey-signed / compressed messages follow the same rules (STUD-80429/80430).
        public static bool PgpVerifyText(string input, Stream publicKeyStream)
            => ExecutePgpVerifyOperation(publicKeyStream, () => ToUtf8Bytes(input), BcEvaluateBinarySignature);

        public static bool PgpVerifyClearText(string input, Stream publicKeyStream)
            => ExecutePgpVerifyOperation(publicKeyStream, () => ToUtf8Bytes(input), BcEvaluateClearSignature);

        // A null input must keep meaning "verification failed" (false) rather than throwing.
        private static byte[] ToUtf8Bytes(string input)
            => input is null ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(input);

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
                    pgp.HashAlgorithmTag = HashAlgorithmTag.Sha256;
                    pgp.CompressionAlgorithm = CompressionAlgorithmTag.Uncompressed;
                    signAction(pgp, inputStream, outputStream);
                    return outputStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw TranslatePgpException(ex);
            }
        }

        private static bool ExecutePgpVerifyOperation(Stream publicKeyStream,
            Func<byte[]> fallbackInput, Func<byte[], byte[], BcVerdict> bcEvaluate)
        {
            // Buffer the public key: the stream is forward-only and the evaluation needs it as bytes.
            byte[] publicKeyBytes;
            try
            {
                using var keyBuffer = new MemoryStream();
                publicKeyStream.CopyTo(keyBuffer);
                publicKeyBytes = keyBuffer.ToArray();
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                // Preserve the pre-existing contract: an unreadable/disposed key stream is a
                // non-fatal verification failure (returns false), not a thrown exception.
                Trace.TraceWarning("PGP verify operation (key buffering) failed: {0}", ex);
                return false;
            }

            // The result comes from our own evaluation only. It resolves the signature's issuer against every
            // key in the ring (subkeys included), unwraps compression, and vets the signer key and the message
            // layout, so the same rules apply to every entry point (STUD-80429/80430). PgpCore's verify is not
            // consulted: it cannot change the outcome.
            return TryBcEvaluate(bcEvaluate, fallbackInput, publicKeyBytes) == BcVerdict.Verified;
        }

        private static BcVerdict TryBcEvaluate(Func<byte[], byte[], BcVerdict> bcEvaluate, Func<byte[]> input, byte[] publicKeyBytes)
        {
            try
            {
                return bcEvaluate(input(), publicKeyBytes);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                Trace.TraceWarning("PGP verify operation (BouncyCastle evaluation) failed: {0}", ex);
                return BcVerdict.Rejected;
            }
        }

        /// <summary>
        /// STUD-80430/80429: Verify an embedded (one-pass) binary signature using BouncyCastle — detached
        /// signatures are not supported, since they carry no literal-data packet —
        /// descending through any <see cref="PgpCompressedData"/> layer and resolving each
        /// signature's issuer key ID against all public keys in the provided keyring. Files
        /// with several signers are supported: the message verifies if at least one signer's
        /// signature is valid AND its key passes <see cref="IsUsableSigningKey"/>.
        /// </summary>
        internal static bool BcVerifyBinarySignature(byte[] inputBytes, byte[] publicKeyBytes)
            => BcEvaluateBinarySignature(inputBytes, publicKeyBytes) == BcVerdict.Verified;

        internal static BcVerdict BcEvaluateBinarySignature(byte[] inputBytes, byte[] publicKeyBytes)
        {
            using var inputStream = new MemoryStream(inputBytes);
            using var decoderStream = PgpUtilities.GetDecoderStream(inputStream);

            var publicKeyRings = ReadPublicKeyRings(publicKeyBytes);
            var verdict = BcVerifySignedPayload(new PgpObjectFactory(decoderStream), publicKeyRings, literalSink: null);
            if (verdict == BcVerdict.Verified && HasSecondArmorBlock(inputBytes))
            {
                return BcVerdict.Rejected;
            }

            return verdict;
        }

        /// <summary>Outcome of our own (key-vetting) signature evaluation.</summary>
        internal enum BcVerdict
        {
            /// <summary>
            /// The message did not verify: it is not in a shape the evaluation accepts, no signer key was available,
            /// or no signature was both valid and from a usable key.
            /// </summary>
            Rejected,

            /// <summary>At least one signature is valid and comes from a usable key.</summary>
            Verified,
        }

        /// <summary>
        /// Verifies a one-pass-signed payload read from <paramref name="pgpFactory"/> (optionally
        /// wrapped in a compressed packet). When <paramref name="literalSink"/> is given, the signed
        /// literal data is copied there while it is hashed; the caller must discard it unless this
        /// returns true.
        /// </summary>
        private static BcVerdict BcVerifySignedPayload(PgpObjectFactory pgpFactory, PgpPublicKeyRingBundle publicKeyRings, Stream literalSink)
        {
            var outerFactory = pgpFactory;
            var pgpObject = NextSignificantObject(pgpFactory);

            // Descend through the compression layer (GnuPG's default for gpg --sign) if present.
            // The compressed data stream must stay open for the rest of the verification, so it is
            // disposed via the method-scoped variable rather than an inner using block.
            Stream compressedStream = null;
            try
            {
                if (pgpObject is PgpCompressedData compressedData)
                {
                    compressedStream = compressedData.GetDataStream();
                    pgpFactory = new PgpObjectFactory(compressedStream);
                    pgpObject = NextSignificantObject(pgpFactory);
                }

                // Signature-first layout (RFC 4880 section 11.3): the signatures precede the literal data they cover.
                if (pgpObject is PgpSignatureList leadingSignatures)
                {
                    return BcVerifySignatureFirstPayload(pgpFactory, outerFactory, leadingSignatures, publicKeyRings, literalSink);
                }

                if (pgpObject is not PgpOnePassSignatureList onePassList)
                {
                    return BcVerdict.Rejected;
                }

                if (NextSignificantObject(pgpFactory) is not PgpLiteralData literalData)
                {
                    return BcVerdict.Rejected;
                }

                var hashing = InitVerifyKnownSigners(onePassList, publicKeyRings);
                if (Array.IndexOf(hashing, true) < 0)
                {
                    return BcVerdict.Rejected;
                }

                CopyLiteralData(literalData, (buffer, count) => UpdateOnePassSignatures(onePassList, hashing, buffer, count), literalSink);

                if (NextSignificantObject(pgpFactory) is not PgpSignatureList signatureList || signatureList.Count != onePassList.Count)
                {
                    return BcVerdict.Rejected;
                }

                if (!HasValidSigner(onePassList, signatureList, hashing, publicKeyRings))
                {
                    return BcVerdict.Rejected;
                }

                // Only the first message is evaluated, so anything after it (another message, a second compressed packet) is refused.
                return HasNoFurtherObjects(pgpFactory, outerFactory) ? BcVerdict.Verified : BcVerdict.Rejected;
            }
            finally
            {
                compressedStream?.Dispose();
            }
        }

        // An armored message is one armor block. A second block would not be looked at by the evaluation, so it is
        // refused instead of being ignored. Binary input has no such marker (its payload may even contain armor-looking
        // text): further messages show up as further objects in the packet stream, which the callers check.
        private static bool HasSecondArmorBlock(byte[] input)
        {
            // Same test as PgpUtilities.GetDecoderStream: a binary packet starts with a byte whose high bit is set.
            if (input.Length == 0 || (input[0] & 0x80) != 0)
            {
                return false;
            }

            var text = Encoding.ASCII.GetString(input);
            var first = text.IndexOf("-----BEGIN PGP", StringComparison.Ordinal);
            return first >= 0 && text.IndexOf("-----BEGIN PGP", first + 1, StringComparison.Ordinal) >= 0;
        }

        private static bool HasNoFurtherObjects(PgpObjectFactory first, PgpObjectFactory second)
            => NextSignificantObject(first) is null && (ReferenceEquals(first, second) || NextSignificantObject(second) is null);

        // Only signers whose key is in the ring are hashed; the rest are skipped, not fatal,
        // so a valid signature from a second signer is still tried.
        private static bool[] InitVerifyKnownSigners(PgpOnePassSignatureList onePassList, PgpPublicKeyRingBundle publicKeyRings)
        {
            var hashing = new bool[onePassList.Count];
            for (var i = 0; i < hashing.Length; i++)
            {
                var signingKey = publicKeyRings.GetPublicKey(onePassList[i].KeyId);
                if (signingKey is null)
                {
                    continue;
                }

                onePassList[i].InitVerify(signingKey);
                hashing[i] = true;
            }

            return hashing;
        }

        // Feeds the literal data to the signatures while copying it to the sink (if any).
        private static void CopyLiteralData(PgpLiteralData literalData, Action<byte[], int> update, Stream literalSink)
        {
            using var literalStream = literalData.GetInputStream();
            var buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = literalStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                update(buffer, bytesRead);
                literalSink?.Write(buffer, 0, bytesRead);
            }
        }

        private static void UpdateOnePassSignatures(PgpOnePassSignatureList onePassList, bool[] hashing, byte[] buffer, int count)
        {
            for (var i = 0; i < hashing.Length; i++)
            {
                if (hashing[i])
                {
                    onePassList[i].Update(buffer, 0, count);
                }
            }
        }

        private static void UpdateSignatures(PgpSignatureList signatures, bool[] hashing, byte[] buffer, int count)
        {
            for (var i = 0; i < hashing.Length; i++)
            {
                if (hashing[i])
                {
                    signatures[i].Update(buffer, 0, count);
                }
            }
        }

        // Signatures followed by the literal data they cover, without one-pass headers. The same rules apply as for
        // the one-pass layout: only binary or canonical-text signatures count, the signer's key must be usable,
        // and nothing may follow the message.
        private static BcVerdict BcVerifySignatureFirstPayload(PgpObjectFactory pgpFactory, PgpObjectFactory outerFactory,
            PgpSignatureList signatures, PgpPublicKeyRingBundle publicKeyRings, Stream literalSink)
        {
            if (NextSignificantObject(pgpFactory) is not PgpLiteralData literalData)
            {
                return BcVerdict.Rejected;
            }

            var hashing = InitVerifySignatureFirstSigners(signatures, publicKeyRings, out var anySignerKnown);
            if (!anySignerKnown)
            {
                return BcVerdict.Rejected;
            }

            CopyLiteralData(literalData, (buffer, count) => UpdateSignatures(signatures, hashing, buffer, count), literalSink);

            return HasValidSignatureFirstSigner(signatures, hashing, publicKeyRings) && HasNoFurtherObjects(pgpFactory, outerFactory)
                ? BcVerdict.Verified
                : BcVerdict.Rejected;
        }

        // Signers whose key is in the ring are known; only those with a binary or canonical-text signature are hashed.
        private static bool[] InitVerifySignatureFirstSigners(PgpSignatureList signatures, PgpPublicKeyRingBundle publicKeyRings, out bool anySignerKnown)
        {
            anySignerKnown = false;
            var hashing = new bool[signatures.Count];
            for (var i = 0; i < hashing.Length; i++)
            {
                var signingKey = publicKeyRings.GetPublicKey(signatures[i].KeyId);
                if (signingKey is null)
                {
                    continue;
                }

                anySignerKnown = true;
                if (signatures[i].SignatureType is PgpSignature.BinaryDocument or PgpSignature.CanonicalTextDocument)
                {
                    signatures[i].InitVerify(signingKey);
                    hashing[i] = true;
                }
            }

            return hashing;
        }

        private static bool HasValidSignatureFirstSigner(PgpSignatureList signatures, bool[] hashing, PgpPublicKeyRingBundle publicKeyRings)
        {
            for (var i = 0; i < hashing.Length; i++)
            {
                if (hashing[i] && signatures[i].Verify() && IsUsableSigningKey(publicKeyRings, signatures[i].KeyId, signatures[i].CreationTime))
                {
                    return true;
                }
            }

            return false;
        }

        // The header decides how the data is canonicalized while it is hashed (text mode normalizes line
        // endings) and the signature packet supplies the trailer, so the two must describe the same signature.
        private static bool HeaderMatchesSignature(PgpOnePassSignature header, PgpSignature signature)
            => header.KeyId == signature.KeyId
               && header.SignatureType == signature.SignatureType
               && header.HashAlgorithm == signature.HashAlgorithm
               && header.KeyAlgorithm == signature.KeyAlgorithm
               && (signature.SignatureType == PgpSignature.BinaryDocument || signature.SignatureType == PgpSignature.CanonicalTextDocument);

        // RFC 4880 §5.4: one-pass headers and signatures nest like brackets
        // (OPS(A) OPS(B) literal SIG(B) SIG(A)), so header i pairs with signature n-1-i.
        private static bool HasValidSigner(PgpOnePassSignatureList onePassList, PgpSignatureList signatureList, bool[] hashing, PgpPublicKeyRingBundle publicKeyRings)
        {
            for (var i = 0; i < hashing.Length; i++)
            {
                if (!hashing[i])
                {
                    continue;
                }

                var signature = signatureList[hashing.Length - 1 - i];
                if (HeaderMatchesSignature(onePassList[i], signature) &&
                    onePassList[i].Verify(signature) &&
                    IsUsableSigningKey(publicKeyRings, signature.KeyId, signature.CreationTime))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// STUD-81645: decrypt a public-key-encrypted message with BouncyCastle (private key looked up
        /// across the whole secret ring, subkeys included) and verify its embedded signature(s) with
        /// the same rules as <see cref="BcVerifyBinarySignature"/>. Writes the plaintext to
        /// <paramref name="verifiedPlaintext"/> and returns true only when decryption succeeded, the
        /// integrity check (if any) passed and a signature from a usable key verified; on false the
        /// stream content must be discarded. Exceptions (e.g. a wrong passphrase) propagate to the caller.
        /// </summary>
        internal static bool BcDecryptAndVerify(byte[] inputBytes, byte[] privateKeyBytes, string passphrase,
            byte[] publicKeyBytes, Stream verifiedPlaintext)
            => BcEvaluateDecryptAndVerify(inputBytes, privateKeyBytes, passphrase, publicKeyBytes, verifiedPlaintext) == BcVerdict.Verified;

        internal static BcVerdict BcEvaluateDecryptAndVerify(byte[] inputBytes, byte[] privateKeyBytes, string passphrase,
            byte[] publicKeyBytes, Stream verifiedPlaintext)
        {
            using var inputStream = new MemoryStream(inputBytes);
            using var decoderStream = PgpUtilities.GetDecoderStream(inputStream);

            var factory = new PgpObjectFactory(decoderStream);
            if (NextSignificantObject(factory) is not PgpEncryptedDataList encryptedList)
            {
                return BcVerdict.Rejected;
            }

            var secretKeys = ReadSecretKeyRings(privateKeyBytes);

            PgpPublicKeyEncryptedData encryptedData = null;
            PgpPrivateKey privateKey = null;
            foreach (PgpEncryptedData candidate in encryptedList.GetEncryptedDataObjects())
            {
                if (candidate is not PgpPublicKeyEncryptedData publicKeyEncrypted)
                {
                    continue;
                }

                var secretKey = secretKeys.GetSecretKey(publicKeyEncrypted.KeyId);
                if (secretKey is null)
                {
                    continue;
                }

                // The passphrase is encoded as UTF-8, as PgpCore does (ExtractPrivateKey(char[]) would use a legacy encoding).
                var extracted = secretKey.ExtractPrivateKeyRaw(Encoding.UTF8.GetBytes(passphrase ?? string.Empty));
                if (extracted is null)
                {
                    continue; // empty stub, for example an offline or card key: try the next recipient
                }

                privateKey = extracted;
                encryptedData = publicKeyEncrypted;
                break;
            }

            if (encryptedData is null)
            {
                return BcVerdict.Rejected;
            }

            var publicKeyRings = ReadPublicKeyRings(publicKeyBytes);

            using var clearStream = encryptedData.GetDataStream(privateKey);
            var verdict = BcVerifySignedPayload(new PgpObjectFactory(clearStream), publicKeyRings, verifiedPlaintext);
            if (verdict != BcVerdict.Verified)
            {
                return verdict;
            }

            // Verify() drains the rest of the stream and checks the modification-detection code.
            if (encryptedData.IsIntegrityProtected() && !encryptedData.Verify())
            {
                return BcVerdict.Rejected;
            }

            // Only the first message is evaluated, so a further message after it is refused instead of released unvetted.
            return NextSignificantObject(factory) is null && !HasSecondArmorBlock(inputBytes) ? BcVerdict.Verified : BcVerdict.Rejected;
        }

        /// <summary>
        /// STUD-80429: Verify a clear-text signature using BouncyCastle, resolving each
        /// signature's issuer key ID against all public keys in the provided keyring so that
        /// signatures issued by signing-capable subkeys verify correctly. Uses the canonical
        /// OpenPGP clear-text line processing (trailing whitespace trimmed, lines joined by CRLF,
        /// no trailing line separator) so the recomputed hash matches the signer's. With several
        /// signatures, the message verifies if any one is valid and its key passes
        /// <see cref="IsUsableSigningKey"/>.
        /// </summary>
        internal static bool BcVerifyClearSignature(byte[] inputBytes, byte[] publicKeyBytes)
            => BcEvaluateClearSignature(inputBytes, publicKeyBytes) == BcVerdict.Verified;

        internal static BcVerdict BcEvaluateClearSignature(byte[] inputBytes, byte[] publicKeyBytes)
        {
            if (!IsSingleClearSignedBlock(inputBytes))
            {
                return BcVerdict.Rejected;
            }

            using var inputStream = new MemoryStream(inputBytes);
            using var armoredInput = new ArmoredInputStream(inputStream);

            // Copy the clear-text section verbatim so it can be re-canonicalized line-by-line.
            using var clearTextBuffer = new MemoryStream();
            int ch;
            while ((ch = armoredInput.ReadByte()) >= 0 && armoredInput.IsClearText())
            {
                clearTextBuffer.WriteByte((byte)ch);
            }

            var signatureFactory = new PgpObjectFactory(armoredInput);
            if (NextSignificantObject(signatureFactory) is not PgpSignatureList signatureList)
            {
                return BcVerdict.Rejected;
            }

            var publicKeyRings = ReadPublicKeyRings(publicKeyBytes);
            var clearText = clearTextBuffer.ToArray();

            for (var i = 0; i < signatureList.Count; i++)
            {
                var signature = signatureList[i];
                var signingKey = publicKeyRings.GetPublicKey(signature.KeyId);
                if (signingKey is null)
                {
                    continue;
                }

                if (signature.SignatureType != PgpSignature.CanonicalTextDocument)
                {
                    continue;
                }

                signature.InitVerify(signingKey);
                UpdateSignatureWithClearText(signature, clearText);

                if (signature.Verify() && IsUsableSigningKey(publicKeyRings, signature.KeyId, signature.CreationTime))
                {
                    return BcVerdict.Verified;
                }
            }

            return BcVerdict.Rejected;
        }

        // A key file may list the same key more than once (an export concatenated twice, an older export followed by a
        // newer one). The BouncyCastle bundles throw on a repeated key id, which turned a file that PgpCore handles
        // into a failed verification. Copies of a public key are merged, so a revocation or a newer binding in a later
        // copy survives and an identical repeat changes nothing. Only the first copy of a secret key is used, as PgpCore does.
        private static PgpPublicKeyRingBundle ReadPublicKeyRings(byte[] keyBytes)
        {
            var rings = new List<PgpPublicKeyRing>();
            var factory = new PgpObjectFactory(PgpUtilities.GetDecoderStream(new MemoryStream(keyBytes)));
            for (var next = factory.NextPgpObject(); next is not null; next = factory.NextPgpObject())
            {
                var ring = next as PgpPublicKeyRing ?? throw new PgpException(next.GetType().FullName + " found where PgpPublicKeyRing expected");
                var seen = rings.FindIndex(existing => existing.GetPublicKey().KeyId == ring.GetPublicKey().KeyId);
                if (seen < 0)
                {
                    rings.Add(ring);
                }
                else
                {
                    rings[seen] = MergeKeyLevelSignatures(PgpPublicKeyRing.Join(rings[seen], ring), ring);
                }
            }

            return new PgpPublicKeyRingBundle(rings);
        }

        // PgpPublicKeyRing.Join stops adding the later copy's key-level signatures (direct-key signatures, key
        // revocations) at the first one both copies share, so a revocation behind a shared signature would be lost.
        // Every key-level signature of the later copy that the joined key lacks is added explicitly.
        private static PgpPublicKeyRing MergeKeyLevelSignatures(PgpPublicKeyRing joined, PgpPublicKeyRing later)
        {
            var master = joined.GetPublicKey();
            var known = master.GetKeySignatures().Cast<PgpSignature>().Select(signature => Convert.ToBase64String(signature.GetEncoded())).ToList();
            foreach (PgpSignature signature in later.GetPublicKey().GetKeySignatures())
            {
                if (!known.Contains(Convert.ToBase64String(signature.GetEncoded())))
                {
                    master = PgpPublicKey.AddCertification(master, signature);
                }
            }

            return PgpPublicKeyRing.InsertPublicKey(joined, master);
        }

        private static PgpSecretKeyRingBundle ReadSecretKeyRings(byte[] keyBytes)
        {
            var rings = new List<PgpSecretKeyRing>();
            var factory = new PgpObjectFactory(PgpUtilities.GetDecoderStream(new MemoryStream(keyBytes)));
            for (var next = factory.NextPgpObject(); next is not null; next = factory.NextPgpObject())
            {
                var ring = next as PgpSecretKeyRing ?? throw new PgpException(next.GetType().FullName + " found where PgpSecretKeyRing expected");
                if (!rings.Exists(existing => existing.GetSecretKey().KeyId == ring.GetSecretKey().KeyId))
                {
                    rings.Add(ring);
                }
            }

            return new PgpSecretKeyRingBundle(rings);
        }

        // The signature covers only the text inside the block, so a result of "verified" must not hide unsigned text
        // anywhere else: the whole input is exactly one clear-signed message, laid out as RFC 4880 section 7 describes
        // it, with only whitespace around it. The armor reader that does the verifying is lenient about what follows
        // the footer marker and about stray lines in the signature block, so this scan is stricter than the reader:
        //   BEGIN PGP SIGNED MESSAGE, "Hash:" headers, blank line, clear text (a line that starts with a dash is
        //   dash-escaped, so only the signature header can end the clear text), BEGIN PGP SIGNATURE (or MESSAGE),
        //   armor headers, blank line, base64 lines with an optional checksum line, the matching END line.
        private static readonly Regex ArmorHeaderLine = new Regex(@"^[A-Za-z][A-Za-z0-9-]*:( .*)?$", RegexOptions.CultureInvariant);
        private static readonly Regex ArmorDataLine = new Regex(@"^([A-Za-z0-9+/]+={0,2}|=[A-Za-z0-9+/]{4})$", RegexOptions.CultureInvariant);
        private static readonly Regex SignatureBeginLine = new Regex(@"^-----BEGIN PGP (SIGNATURE|MESSAGE)-----$", RegexOptions.CultureInvariant);

        internal static bool IsSingleClearSignedBlock(byte[] input)
        {
            var lines = Encoding.UTF8.GetString(input).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')
                .Select(line => line.TrimEnd()).ToArray();

            var at = 0;
            SkipLines(lines, ref at, line => line.Length == 0);
            if (at >= lines.Length || lines[at++] != "-----BEGIN PGP SIGNED MESSAGE-----")
            {
                return false;
            }

            SkipLines(lines, ref at, line => line.StartsWith("Hash:", StringComparison.Ordinal));
            if (!SkipBlankLine(lines, ref at))
            {
                return false;
            }

            SkipLines(lines, ref at, IsClearTextLine);
            var signatureBegin = at < lines.Length ? SignatureBeginLine.Match(lines[at]) : Match.Empty;
            if (!signatureBegin.Success)
            {
                return false;
            }

            at++;
            return HasValidSignatureArmor(lines, ref at, "-----END PGP " + signatureBegin.Groups[1].Value + "-----")
                && lines.Skip(at).All(line => line.Length == 0);
        }

        // A generator dash-escapes every clear-text line that starts with a dash. The armor reader ends the clear text
        // at any line that starts with two dashes, so such a line may only be the signature header.
        private static bool IsClearTextLine(string line)
            => !line.StartsWith("-", StringComparison.Ordinal) || line.StartsWith("- ", StringComparison.Ordinal);

        // Armor headers, a blank line, base64 data (checksum line included) and then exactly the footer.
        private static bool HasValidSignatureArmor(string[] lines, ref int at, string footer)
        {
            SkipLines(lines, ref at, ArmorHeaderLine.IsMatch);
            if (!SkipBlankLine(lines, ref at))
            {
                return false;
            }

            SkipLines(lines, ref at, ArmorDataLine.IsMatch);
            return at < lines.Length && lines[at++] == footer;
        }

        private static void SkipLines(string[] lines, ref int at, Func<string, bool> accept)
        {
            while (at < lines.Length && accept(lines[at]))
            {
                at++;
            }
        }

        private static bool SkipBlankLine(string[] lines, ref int at)
        {
            if (at >= lines.Length || lines[at].Length != 0)
            {
                return false;
            }

            at++;
            return true;
        }

        // RFC 4880 section 5.8: a Marker packet is ignorable and may appear before any other packet.
        private static PgpObject NextSignificantObject(PgpObjectFactory factory)
        {
            PgpObject next;
            do
            {
                next = factory.NextPgpObject();
            }
            while (next is PgpMarker);

            return next;
        }

        private static void UpdateSignatureWithClearText(PgpSignature signature, byte[] clearText)
        {
            using var lineIn = new MemoryStream(clearText);
            var lineOut = new MemoryStream();
            int lookAhead = ReadInputLine(lineOut, lineIn);
            ProcessSignatureLine(signature, lineOut.ToArray());

            if (lookAhead != -1)
            {
                do
                {
                    lookAhead = ReadInputLine(lineOut, lookAhead, lineIn);
                    signature.Update((byte)'\r');
                    signature.Update((byte)'\n');
                    ProcessSignatureLine(signature, lineOut.ToArray());
                }
                while (lookAhead != -1);
            }
        }

        /// <summary>
        /// The BouncyCastle fallback resolves the issuer against EVERY key in the file, and BouncyCastle
        /// loads keys without validating them, so a key must be vetted before its signature is trusted:
        /// it (and its primary key) must not be revoked, both must have existed and not yet expired
        /// at <paramref name="signedAtUtc"/>, and a subkey needs a valid binding signature from the
        /// primary key, in force at that time, that allows signing, and the subkey must have
        /// consented with an embedded back-signature. A subkey whose binding signature does not
        /// carry a key-flags subpacket is accepted (older keys), but one whose flags omit "sign" is not.
        /// This keeps the fallback from trusting more than PgpCore's single-key lookup did, so a
        /// revoked or grafted-on subkey cannot start verifying.
        /// </summary>
        private static bool IsUsableSigningKey(PgpPublicKeyRingBundle bundle, long keyId, DateTime signedAtUtc)
        {
            var ring = bundle.GetPublicKeyRing(keyId);
            var key = ring?.GetPublicKey(keyId);
            if (key is null)
            {
                return false;
            }

            var primary = ring.GetPublicKey();
            if (primary.IsRevoked() || key.IsRevoked())
            {
                return false;
            }

            if (!WasValidAt(primary, signedAtUtc) || !WasValidAt(key, signedAtUtc))
            {
                return false;
            }

            return key.IsMasterKey ? PrimaryKeyAllowsSigning(primary, signedAtUtc) : HasValidSigningSubkeyBinding(primary, key, signedAtUtc);
        }

        // The primary key's usage flags sit on its self-signatures (user-id certifications or a direct-key
        // signature). The governing one is the newest verified self-signature in force at signing time. Like for
        // subkeys, no key-flags subpacket means "unrestricted" (older keys), but flags without "sign" (for example
        // certify-only, or none at all) must not verify data.
        private static bool PrimaryKeyAllowsSigning(PgpPublicKey primary, DateTime signedAtUtc)
        {
            PgpSignature governing = null;
            foreach (PgpSignature selfSignature in GetVerifiedSelfSignatures(primary))
            {
                if (IsBindingActiveAt(selfSignature, signedAtUtc) &&
                    (governing is null || selfSignature.CreationTime >= governing.CreationTime))
                {
                    governing = selfSignature;
                }
            }

            var hashed = governing?.GetHashedSubPackets();
            return hashed is null || !hashed.HasSubpacket(SignatureSubpacketTag.KeyFlags) || (hashed.GetKeyFlags() & PgpKeyFlags.CanSign) != 0;
        }

        // Authenticate first, then select: only signatures that verify against the primary key count, whatever they
        // claim about their issuer. Candidates are the direct-key signatures and the user-id certifications (types 0x10
        // to 0x13); a user-id revocation (0x30) never grants anything. User ids are read raw, because a user id that is
        // not plain ASCII does not round-trip through its string form. A candidate that cannot be verified (a damaged
        // packet, an algorithm BouncyCastle does not know) simply does not count.
        private static IEnumerable<PgpSignature> GetVerifiedSelfSignatures(PgpPublicKey primary)
        {
            foreach (PgpSignature signature in primary.GetSignaturesOfType(PgpSignature.DirectKey))
            {
                if (IsVerifiedDirectKeySignature(primary, signature))
                {
                    yield return signature;
                }
            }

            foreach (byte[] rawUserId in primary.GetRawUserIds())
            {
                foreach (PgpSignature signature in primary.GetSignaturesForId(rawUserId) ?? Enumerable.Empty<PgpSignature>())
                {
                    if (signature.SignatureType is >= PgpSignature.DefaultCertification and <= PgpSignature.PositiveCertification &&
                        IsVerifiedUserIdCertification(primary, signature, rawUserId))
                    {
                        yield return signature;
                    }
                }
            }
        }

        private static bool IsVerifiedUserIdCertification(PgpPublicKey primary, PgpSignature signature, byte[] rawUserId)
        {
            try
            {
                signature.InitVerify(primary);
                return signature.VerifyCertification(Encoding.UTF8.GetString(rawUserId), primary);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                return false;
            }
        }

        // A direct-key signature covers the key alone (RFC 4880 section 5.2.4): 0x99, the two-octet length and the
        // key packet body. PgpSignature.VerifyCertification(PgpPublicKey) only accepts revocation types.
        private static bool IsVerifiedDirectKeySignature(PgpPublicKey primary, PgpSignature signature)
        {
            try
            {
                signature.InitVerify(primary);
                var keyBody = primary.PublicKeyPacket.GetEncodedContents();
                signature.Update(0x99);
                signature.Update((byte)(keyBody.Length >> 8));
                signature.Update((byte)keyBody.Length);
                signature.Update(keyBody);
                return signature.Verify();
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                return false;
            }
        }

        private static bool WasValidAt(PgpPublicKey key, DateTime atUtc)
        {
            if (atUtc < key.CreationTime)
            {
                return false;
            }

            var validSeconds = key.GetValidSeconds();
            return validSeconds <= 0 || atUtc <= key.CreationTime.AddSeconds(validSeconds);
        }

        /// <summary>
        /// A subkey keeps every binding signature it ever received, so the one that governs a
        /// signature is the LATEST valid binding that already existed and had not expired at
        /// <paramref name="signedAtUtc"/>; older or later bindings must not decide it (a later
        /// authorization cannot retroactively allow, nor a later restriction retroactively forbid).
        /// </summary>
        private static bool HasValidSigningSubkeyBinding(PgpPublicKey primary, PgpPublicKey subkey, DateTime signedAtUtc)
        {
            var effective = GetBindingInForce(primary, subkey, signedAtUtc);
            if (effective is null)
            {
                return false;
            }

            // Same rule as for the primary key: no key-flags subpacket means unrestricted, but present flags (even
            // empty ones) must include "sign".
            var hashed = effective.GetHashedSubPackets();
            if (hashed is not null && hashed.HasSubpacket(SignatureSubpacketTag.KeyFlags) && (hashed.GetKeyFlags() & PgpKeyFlags.CanSign) == 0)
            {
                return false;
            }

            // A binding by the primary only proves the primary CLAIMS the subkey. GnuPG additionally
            // requires the signing subkey to consent: an embedded back-signature (Primary Key
            // Binding, 0x19) made by the subkey over (primary, subkey). Without it, anyone could attach
            // somebody else's public signing subkey to their own key and have that person's
            // signatures verify as theirs.
            return HasValidBackSignature(primary, subkey, effective);
        }

        // The latest verified binding that existed, and had not lapsed, when the message was signed.
        private static PgpSignature GetBindingInForce(PgpPublicKey primary, PgpPublicKey subkey, DateTime signedAtUtc)
        {
            PgpSignature effective = null;
            foreach (PgpSignature binding in subkey.GetSignaturesOfType(PgpSignature.SubkeyBinding))
            {
                if (IsVerifiedBinding(primary, subkey, binding) &&
                    IsBindingActiveAt(binding, signedAtUtc) &&
                    (effective is null || binding.CreationTime >= effective.CreationTime))
                {
                    effective = binding;
                }
            }

            return effective;
        }

        private static bool IsVerifiedBinding(PgpPublicKey primary, PgpPublicKey subkey, PgpSignature binding)
        {
            try
            {
                binding.InitVerify(primary);
                return binding.VerifyCertification(primary, subkey);
            }
            catch (Exception ex) when (ex is PgpException or InvalidOperationException or IOException)
            {
                return false;
            }
        }

        private static bool IsBindingActiveAt(PgpSignature binding, DateTime signedAtUtc)
        {
            if (binding.CreationTime > signedAtUtc)
            {
                return false; // not yet in force when the message was signed
            }

            var expirySeconds = binding.GetHashedSubPackets()?.GetSignatureExpirationTime() ?? 0;
            return expirySeconds <= 0 || signedAtUtc <= binding.CreationTime.AddSeconds(expirySeconds); // else the binding itself had lapsed
        }

        private static bool HasValidBackSignature(PgpPublicKey primary, PgpPublicKey subkey, PgpSignature binding)
        {
            foreach (var subpackets in new[] { binding.GetHashedSubPackets(), binding.GetUnhashedSubPackets() })
            {
                var embedded = GetEmbeddedSignatures(subpackets);
                if (embedded is null)
                {
                    continue;
                }

                for (var i = 0; i < embedded.Count; i++)
                {
                    if (IsValidBackSignature(embedded[i], primary, subkey))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static PgpSignatureList GetEmbeddedSignatures(PgpSignatureSubpacketVector subpackets)
        {
            if (subpackets is null)
            {
                return null;
            }

            try
            {
                return subpackets.GetEmbeddedSignatures();
            }
            catch (Exception ex) when (ex is PgpException or IOException)
            {
                return null;
            }
        }

        private static bool IsValidBackSignature(PgpSignature backSignature, PgpPublicKey primary, PgpPublicKey subkey)
        {
            // A back-signature without an Issuer Key ID subpacket reports key id 0; the verification below proves the issuer.
            if (backSignature.SignatureType != PgpSignature.PrimaryKeyBinding ||
                (backSignature.KeyId != 0 && backSignature.KeyId != subkey.KeyId))
            {
                return false;
            }

            try
            {
                backSignature.InitVerify(subkey);
                return backSignature.VerifyCertification(primary, subkey);
            }
            catch (Exception ex) when (ex is PgpException or InvalidOperationException or IOException)
            {
                // An unusable back-signature is the same as none.
                return false;
            }
        }

        private static int ReadInputLine(MemoryStream lineOut, Stream fIn)
        {
            lineOut.SetLength(0);

            int lookAhead = -1;
            int ch;
            while ((ch = fIn.ReadByte()) >= 0)
            {
                lineOut.WriteByte((byte)ch);
                if (ch == '\r' || ch == '\n')
                {
                    lookAhead = ReadPastEol(lineOut, ch, fIn);
                    break;
                }
            }

            return lookAhead;
        }

        private static int ReadInputLine(MemoryStream lineOut, int lookAhead, Stream fIn)
        {
            lineOut.SetLength(0);

            int ch = lookAhead;
            do
            {
                if (ch != '\r' && ch != '\n')
                {
                    lineOut.WriteByte((byte)ch);
                }
                else
                {
                    lookAhead = ReadPastEol(lineOut, ch, fIn);
                    return lookAhead;
                }
            }
            while ((ch = fIn.ReadByte()) >= 0);

            return -1;
        }

        private static int ReadPastEol(MemoryStream lineOut, int lastCh, Stream fIn)
        {
            int lookAhead = fIn.ReadByte();

            if (lastCh == '\r' && lookAhead == '\n')
            {
                lineOut.WriteByte((byte)lookAhead);
                lookAhead = fIn.ReadByte();
            }

            return lookAhead;
        }

        private static void ProcessSignatureLine(PgpSignature signature, byte[] line)
        {
            int length = GetLengthWithoutWhiteSpace(line);
            if (length > 0)
            {
                signature.Update(line, 0, length);
            }
        }

        private static int GetLengthWithoutWhiteSpace(byte[] line)
        {
            int end = line.Length - 1;
            while (end >= 0 && IsWhiteSpace(line[end]))
            {
                end--;
            }

            return end + 1;
        }

        private static bool IsWhiteSpace(byte b)
            => b == '\r' || b == '\n' || b == '\t' || b == ' ';

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
            return key != null
                ? encoding.GetBytes(key)
                : SecureStringHelpers.WithSecureChars(keySecureString, chars => encoding.GetBytes(chars));
        }

        /// <summary>
        /// Parse a key (or IV) string per the chosen <see cref="KeyBytesFormat"/>. <c>Encoded</c>
        /// reuses the existing password-via-Encoding path; <c>Hex</c> and <c>Base64</c> are required
        /// when supplying a literal raw key (Format = Raw).
        /// </summary>
        public static byte[] ParseKeyBytes(string keyString, SecureString keySecureString, KeyBytesFormat format, Encoding encoding)
        {
            switch (format)
            {
                case KeyBytesFormat.Encoded:
                    if (encoding == null)
                        throw new ArgumentNullException(nameof(encoding), "Encoding is required when KeyFormat = Encoded.");
                    return KeyEncoding(encoding, keyString, keySecureString);

                case KeyBytesFormat.Hex:
                {
                    if (!string.IsNullOrEmpty(keyString))
                        return FromHexString(keyString);
                    if (keySecureString != null && keySecureString.Length > 0)
                        return SecureStringHelpers.WithSecureChars(keySecureString, chars => FromHexString(chars));
                    throw new ArgumentException("Hex key/IV string is empty.");
                }

                case KeyBytesFormat.Base64:
                {
                    if (!string.IsNullOrEmpty(keyString))
                        return FromBase64String(keyString);
                    if (keySecureString != null && keySecureString.Length > 0)
                        return SecureStringHelpers.WithSecureChars(keySecureString, chars => FromBase64String(chars));
                    throw new ArgumentException("Base64 key/IV string is empty.");
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown KeyBytesFormat.");
            }
        }

        // Takes ReadOnlySpan<char> (not string) so a SecureString-derived char[] can be parsed without
        // ever producing a managed string. The cleaned scratch buffer is stackalloc'd for the common
        // (short key/IV) case and heap-allocated for the oversized fallback; either way it holds a copy
        // of the secret hex digits, so it is zeroed in finally before the frame unwinds.
        private static byte[] FromHexString(ReadOnlySpan<char> hex)
        {
            // Tolerate "0x" prefix and any embedded whitespace/colons typical of hex dumps.
            int start = (hex.Length >= 2 && hex[0] == '0' && (hex[1] == 'x' || hex[1] == 'X')) ? 2 : 0;
            char[] heapBuffer = hex.Length > 512 ? new char[hex.Length] : null;
            Span<char> cleaned = heapBuffer ?? stackalloc char[hex.Length];
            try
            {
                int n = 0;
                for (int i = start; i < hex.Length; i++)
                {
                    char c = hex[i];
                    if (c == ' ' || c == ':' || c == '-' || c == '\t' || c == '\r' || c == '\n') continue;
                    cleaned[n++] = c;
                }
                if ((n & 1) != 0)
                    throw new ArgumentException("Hex string has an odd number of digits.");
                byte[] bytes = new byte[n / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = byte.Parse(cleaned.Slice(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
                }
                return bytes;
            }
            finally
            {
                // Zero the secret-bearing scratch buffer (covers both the stackalloc and heap-allocated cases).
                cleaned.Clear();
            }
        }

        // Takes ReadOnlySpan<char> (not string) so a SecureString-derived char[] can be parsed
        // without ever producing a managed string — mirroring FromHexString and preserving the
        // no-managed-string guarantee of the SecureString key path (STUD-80531). The cleaned
        // scratch buffer holds a copy of the secret key material, so it is zeroed in finally.
        // It is a char[] (not stackalloc) because Convert.FromBase64CharArray needs an array;
        // a key/IV is short, so the allocation is negligible.
        private static byte[] FromBase64String(ReadOnlySpan<char> s)
        {
            // Tolerate whitespace/line wraps, the URL-safe alphabet, and missing padding —
            // common forms a third-party tool (openssl, JWT/OAuth secrets) produces. The final
            // Convert.FromBase64CharArray call still enforces strict validation on the cleaned
            // text: characters outside the alphabet and a mod-4==1 length both surface as
            // FormatException, consistent with the rest of the Base64 contract.
            char[] cleaned = new char[s.Length + 2]; // +2 head-room for the padding we may append
            try
            {
                int n = 0;
                foreach (char c in s)
                {
                    if (c == ' ' || c == '\t' || c == '\r' || c == '\n') continue;
                    if (c == '-') cleaned[n++] = '+';        // URL-safe alphabet → standard
                    else if (c == '_') cleaned[n++] = '/';
                    else cleaned[n++] = c;
                }
                int rem = n % 4;
                if (rem == 2) { cleaned[n++] = '='; cleaned[n++] = '='; }
                else if (rem == 3) { cleaned[n++] = '='; }
                // rem == 1 can never be valid Base64; leave it for Convert.FromBase64CharArray
                // to reject as FormatException, consistent with the rest of the Base64 contract.
                return Convert.FromBase64CharArray(cleaned, 0, n);
            }
            finally
            {
                // Zero the secret-bearing scratch buffer before the frame unwinds.
                Array.Clear(cleaned, 0, cleaned.Length);
            }
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