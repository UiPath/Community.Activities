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
                    pgp.HashAlgorithmTag = HashAlgorithmTag.Sha256;
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
                        return Convert.FromBase64String(keyString);
                    if (keySecureString != null && keySecureString.Length > 0)
                        return SecureStringHelpers.WithSecureChars(keySecureString, chars => Convert.FromBase64CharArray(chars, 0, chars.Length));
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