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
        // ── Symmetric ─────────────────────────────────────────────────────────

        public byte[] EncryptBytes(byte[] input, EncryptionAlgorithm algorithm, SymmetricEncryptOptions options)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(options);
            ValidateSymmetric(algorithm, options, options.IV);
            return options.Key.UseKeyBytes(keyBytes =>
                SymmetricInteropHelper.DispatchEncrypt(algorithm, options.Format, options.KdfIterations, keyBytes, options.IV, input));
        }

        public byte[] DecryptBytes(byte[] input, EncryptionAlgorithm algorithm, SymmetricDecryptOptions options)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(options);
            ValidateSymmetric(algorithm, options, iv: null);
            return options.Key.UseKeyBytes(keyBytes =>
                SymmetricInteropHelper.DispatchDecrypt(algorithm, options.Format, options.KdfIterations, keyBytes, input));
        }

        public string EncryptText(string input, EncryptionAlgorithm algorithm, SymmetricEncryptOptions options)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(options);
            byte[] cipher = EncryptBytes(options.TextEncoding.GetBytes(input), algorithm, options);
            return Convert.ToBase64String(cipher);
        }

        public string DecryptText(string input, EncryptionAlgorithm algorithm, SymmetricDecryptOptions options)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(options);
            byte[] plain = DecryptBytes(Convert.FromBase64String(input), algorithm, options);
            return options.TextEncoding.GetString(plain);
        }

        public void EncryptFile(string inputPath, string outputPath, EncryptionAlgorithm algorithm, SymmetricEncryptOptions options, bool overwrite = false)
        {
            ThrowIfFilePathMissing(inputPath, nameof(inputPath));
            byte[] cipher = EncryptBytes(File.ReadAllBytes(inputPath), algorithm, options);
            WriteFile(outputPath, cipher, overwrite);
        }

        public void DecryptFile(string inputPath, string outputPath, EncryptionAlgorithm algorithm, SymmetricDecryptOptions options, bool overwrite = false)
        {
            ThrowIfFilePathMissing(inputPath, nameof(inputPath));
            byte[] plain = DecryptBytes(File.ReadAllBytes(inputPath), algorithm, options);
            WriteFile(outputPath, plain, overwrite);
        }

        // ── Keyed hash ────────────────────────────────────────────────────────

        public string KeyedHashBytes(byte[] input, KeyedHashAlgorithms algorithm, CryptoKey key)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(key);
            return key.UseKeyBytes(keyBytes => ComputeHashHex(algorithm, input, keyBytes));
        }

        public string KeyedHashText(string input, KeyedHashAlgorithms algorithm, CryptoKey key, Encoding encoding = null)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(key);
            Encoding textEncoding = encoding ?? Encoding.UTF8;
            return key.UseKeyBytes(keyBytes => ComputeHashHex(algorithm, textEncoding.GetBytes(input), keyBytes));
        }

        public string KeyedHashFile(string inputPath, KeyedHashAlgorithms algorithm, CryptoKey key)
        {
            ThrowIfFilePathMissing(inputPath, nameof(inputPath));
            ArgumentNullException.ThrowIfNull(key);
            return key.UseKeyBytes(keyBytes => ComputeHashHex(algorithm, File.ReadAllBytes(inputPath), keyBytes));
        }

        // ── PGP encrypt ───────────────────────────────────────────────────────

        public byte[] PgpEncryptBytes(byte[] input, PgpPublicKey recipient, PgpPrivateKey signer = null)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(recipient);

            using var pubStream = recipient.OpenStream();
            if (signer is null)
                return CryptographyHelper.PgpEncrypt(input, pubStream, null, null, sign: false);

            var (privStream, passphrase) = signer.Open();
            using (privStream)
                return CryptographyHelper.PgpEncrypt(input, pubStream, privStream, passphrase, sign: true);
        }

        public string PgpEncryptText(string input, PgpPublicKey recipient, PgpPrivateKey signer = null)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(recipient);

            using var pubStream = recipient.OpenStream();
            if (signer is null)
                return CryptographyHelper.PgpEncryptText(input, pubStream, null, null, sign: false);

            var (privStream, passphrase) = signer.Open();
            using (privStream)
                return CryptographyHelper.PgpEncryptText(input, pubStream, privStream, passphrase, sign: true);
        }

        public void PgpEncryptFile(string inputPath, string outputPath, PgpPublicKey recipient, PgpPrivateKey signer = null, bool overwrite = false)
        {
            ThrowIfFilePathMissing(inputPath, nameof(inputPath));
            byte[] encrypted = PgpEncryptBytes(File.ReadAllBytes(inputPath), recipient, signer);
            WriteFile(outputPath, encrypted, overwrite);
        }

        // ── PGP decrypt ───────────────────────────────────────────────────────

        public byte[] PgpDecryptBytes(byte[] input, PgpPrivateKey recipient, PgpPublicKey verifier = null)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(recipient);

            var (privStream, passphrase) = recipient.Open();
            using (privStream)
            using (Stream pubStream = verifier?.OpenStream())
                return CryptographyHelper.PgpDecrypt(input, privStream, passphrase, pubStream, verifySignature: verifier != null);
        }

        public string PgpDecryptText(string input, PgpPrivateKey recipient, PgpPublicKey verifier = null)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(recipient);

            var (privStream, passphrase) = recipient.Open();
            using (privStream)
            using (Stream pubStream = verifier?.OpenStream())
                return CryptographyHelper.PgpDecryptText(input, privStream, passphrase, pubStream, verifySignature: verifier != null);
        }

        public void PgpDecryptFile(string inputPath, string outputPath, PgpPrivateKey recipient, PgpPublicKey verifier = null, bool overwrite = false)
        {
            ThrowIfFilePathMissing(inputPath, nameof(inputPath));
            byte[] decrypted = PgpDecryptBytes(File.ReadAllBytes(inputPath), recipient, verifier);
            WriteFile(outputPath, decrypted, overwrite);
        }

        // ── PGP sign ──────────────────────────────────────────────────────────

        public byte[] PgpSignBytes(byte[] input, PgpPrivateKey signer)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(signer);

            var (privStream, passphrase) = signer.Open();
            using (privStream)
                return CryptographyHelper.PgpSign(input, privStream, passphrase);
        }

        public string PgpSignText(string input, PgpPrivateKey signer)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(signer);

            var (privStream, passphrase) = signer.Open();
            using (privStream)
                return CryptographyHelper.PgpSignText(input, privStream, passphrase);
        }

        public void PgpSignFile(string inputPath, string outputPath, PgpPrivateKey signer, bool overwrite = false)
        {
            ThrowIfFilePathMissing(inputPath, nameof(inputPath));
            byte[] signed = PgpSignBytes(File.ReadAllBytes(inputPath), signer);
            WriteFile(outputPath, signed, overwrite);
        }

        // ── PGP clear-sign ────────────────────────────────────────────────────

        public byte[] PgpClearSignBytes(byte[] input, PgpPrivateKey signer)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(signer);

            var (privStream, passphrase) = signer.Open();
            using (privStream)
                return CryptographyHelper.PgpClearSign(input, privStream, passphrase);
        }

        public string PgpClearSignText(string input, PgpPrivateKey signer)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(signer);

            var (privStream, passphrase) = signer.Open();
            using (privStream)
                return CryptographyHelper.PgpClearSignText(input, privStream, passphrase);
        }

        public void PgpClearSignFile(string inputPath, string outputPath, PgpPrivateKey signer, bool overwrite = false)
        {
            ThrowIfFilePathMissing(inputPath, nameof(inputPath));
            byte[] signed = PgpClearSignBytes(File.ReadAllBytes(inputPath), signer);
            WriteFile(outputPath, signed, overwrite);
        }

        // ── PGP verify (binary) ───────────────────────────────────────────────

        public bool PgpVerifyBytes(byte[] input, PgpPublicKey verifier)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(verifier);

            using var pubStream = verifier.OpenStream();
            return CryptographyHelper.PgpVerify(input, pubStream);
        }

        public bool PgpVerifyText(string input, PgpPublicKey verifier)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(verifier);

            using var pubStream = verifier.OpenStream();
            return CryptographyHelper.PgpVerifyText(input, pubStream);
        }

        public bool PgpVerifyFile(string inputPath, PgpPublicKey verifier)
        {
            ThrowIfFilePathMissing(inputPath, nameof(inputPath));
            return PgpVerifyBytes(File.ReadAllBytes(inputPath), verifier);
        }

        // ── PGP verify (clearsignature) ───────────────────────────────────────

        public bool PgpVerifyClearSignedBytes(byte[] input, PgpPublicKey verifier)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(verifier);

            using var pubStream = verifier.OpenStream();
            return CryptographyHelper.PgpVerifyClear(input, pubStream);
        }

        public bool PgpVerifyClearSignedText(string input, PgpPublicKey verifier)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(verifier);

            using var pubStream = verifier.OpenStream();
            return CryptographyHelper.PgpVerifyClearText(input, pubStream);
        }

        public bool PgpVerifyClearSignedFile(string inputPath, PgpPublicKey verifier)
        {
            ThrowIfFilePathMissing(inputPath, nameof(inputPath));
            return PgpVerifyClearSignedBytes(File.ReadAllBytes(inputPath), verifier);
        }

        // ── PGP key generation + public-key verification ──────────────────────

        public PgpKeyPair PgpGenerateKeys(string userId, string passphrase, RsaKeySize keySize = RsaKeySize.Rsa4096)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID must not be null or empty.", nameof(userId));

            string pubPath = Path.Combine(Path.GetTempPath(), $"crypto_pgp_pub_{Guid.NewGuid():N}.asc");
            string privPath = Path.Combine(Path.GetTempPath(), $"crypto_pgp_priv_{Guid.NewGuid():N}.asc");
            try
            {
                CryptographyHelper.PgpGenerateKeys(pubPath, privPath, userId, passphrase, keySize);
                byte[] pubBytes = File.ReadAllBytes(pubPath);
                byte[] privBytes = File.ReadAllBytes(privPath);
                return new PgpKeyPair(PgpPublicKey.FromBytes(pubBytes), PgpPrivateKey.FromBytes(privBytes, passphrase));
            }
            finally
            {
                TryDelete(pubPath);
                TryDelete(privPath);
            }
        }

        public PgpKeyPair PgpGenerateKeys(string userId, SecureString passphrase, RsaKeySize keySize = RsaKeySize.Rsa4096)
        {
            ArgumentNullException.ThrowIfNull(passphrase);
            return PgpGenerateKeys(userId, SecureStringToManagedString(passphrase), keySize);
        }

        public bool PgpVerifyPublicKey(PgpPublicKey key)
        {
            ArgumentNullException.ThrowIfNull(key);
            using var stream = key.OpenStream();
            return CryptographyHelper.PgpVerifyPublicKey(stream);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        // Defence-in-depth runtime validation. The (key kind × wire format) pairing is already
        // enforced at compile time by the typed factory parameters on SymmetricEncryptOptions /
        // SymmetricDecryptOptions; this still catches KDF-iteration-out-of-bounds, raw-key
        // length mismatch, and (encrypt) IV-on-non-Raw-format. iv is null for decrypt.
        private static void ValidateSymmetric(EncryptionAlgorithm algorithm, CryptoOptions options, byte[] iv)
        {
            CryptoKey key = options.Key ?? throw new ArgumentException("Options must carry a key (construct via a format factory).", nameof(options));
            string ivSentinel = iv != null && iv.Length > 0 ? "set" : null;
            int? rawKeyLength = key.IsRawKey ? key.KeyBytes.Length : (int?)null;
            SymmetricInteropHelper.ValidateInteropSettings(algorithm, options.Format, key.BytesFormat, ivSentinel, options.KdfIterations, rawKeyLength);
        }

        private static string ComputeHashHex(KeyedHashAlgorithms algorithm, byte[] inputBytes, byte[] keyBytes)
        {
            byte[] hashBytes = CryptographyHelper.HashDataWithKey(algorithm, inputBytes, keyBytes);
            return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
        }

        private static void ThrowIfFilePathMissing(string path, string paramName)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("File path must not be null or empty.", paramName);
        }

        private static void WriteFile(string outputPath, byte[] bytes, bool overwrite)
        {
            ThrowIfFilePathMissing(outputPath, nameof(outputPath));
            if (!overwrite && File.Exists(outputPath))
                throw new InvalidOperationException($"Output file already exists: {outputPath}");
            File.WriteAllBytes(outputPath, bytes);
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { /* best-effort cleanup */ }
        }

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
