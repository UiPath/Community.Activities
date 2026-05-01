using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using UiPath.Cryptography;
using UiPath.Cryptography.Enums;

namespace UiPath.Cryptography.Activities.API
{
    internal class CryptographyService : ICryptographyService
    {
        // ── Public API ────────────────────────────────────────────────────────

        public string EncryptText(CryptoOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            if (options.Algorithm == EncryptionAlgorithm.PGP)
                throw new InvalidOperationException("PGP is not supported for text operations. Use EncryptFile with Algorithm = PGP.");
            var encoding = ResolveEncoding(options);
            byte[] inputBytes = ResolveInputBytes(options, encoding);
            byte[] keyBytes = ResolveKeyBytes(options, encoding);
            try
            {
                byte[] encryptedBytes = CryptographyHelper.EncryptData(options.Algorithm, inputBytes, keyBytes);
                return Convert.ToBase64String(encryptedBytes);
            }
            finally
            {
                ClearIfDerived(options, keyBytes);
            }
        }

        public string DecryptText(CryptoOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            if (options.Algorithm == EncryptionAlgorithm.PGP)
                throw new InvalidOperationException("PGP is not supported for text operations. Use DecryptFile with Algorithm = PGP.");
            var encoding = ResolveEncoding(options);
            byte[] inputBytes = ResolveCiphertextBytes(options);
            byte[] keyBytes = ResolveKeyBytes(options, encoding);
            try
            {
                byte[] decryptedBytes = CryptographyHelper.DecryptData(options.Algorithm, inputBytes, keyBytes);
                return encoding.GetString(decryptedBytes);
            }
            finally
            {
                ClearIfDerived(options, keyBytes);
            }
        }

        public void EncryptFile(CryptoOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            if (string.IsNullOrWhiteSpace(options.Input))
                throw new ArgumentException("CryptoOptions.Input must contain the input file path.", nameof(options));
            if (string.IsNullOrWhiteSpace(options.OutputFile))
                throw new ArgumentException("CryptoOptions.OutputFile must contain the output file path.", nameof(options));
            if (!options.Overwrite && File.Exists(options.OutputFile))
                throw new InvalidOperationException($"Output file already exists: {options.OutputFile}");

            if (options.Algorithm == EncryptionAlgorithm.PGP)
            {
                ExecutePgpEncryptFile(options);
                return;
            }

            var encoding = ResolveEncoding(options);
            byte[] keyBytes = ResolveKeyBytes(options, encoding);
            try
            {
                byte[] inputBytes = File.ReadAllBytes(options.Input);
                byte[] encryptedBytes = CryptographyHelper.EncryptData(options.Algorithm, inputBytes, keyBytes);
                File.WriteAllBytes(options.OutputFile, encryptedBytes);
            }
            finally
            {
                ClearIfDerived(options, keyBytes);
            }
        }

        public void DecryptFile(CryptoOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            if (string.IsNullOrWhiteSpace(options.Input))
                throw new ArgumentException("CryptoOptions.Input must contain the input file path.", nameof(options));
            if (string.IsNullOrWhiteSpace(options.OutputFile))
                throw new ArgumentException("CryptoOptions.OutputFile must contain the output file path.", nameof(options));
            if (!options.Overwrite && File.Exists(options.OutputFile))
                throw new InvalidOperationException($"Output file already exists: {options.OutputFile}");

            if (options.Algorithm == EncryptionAlgorithm.PGP)
            {
                ExecutePgpDecryptFile(options);
                return;
            }

            var encoding = ResolveEncoding(options);
            byte[] keyBytes = ResolveKeyBytes(options, encoding);
            try
            {
                byte[] inputBytes = File.ReadAllBytes(options.Input);
                byte[] decryptedBytes = CryptographyHelper.DecryptData(options.Algorithm, inputBytes, keyBytes);
                File.WriteAllBytes(options.OutputFile, decryptedBytes);
            }
            finally
            {
                ClearIfDerived(options, keyBytes);
            }
        }

        public string KeyedHashText(CryptoOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            var encoding = ResolveEncoding(options);
            byte[] inputBytes = ResolveInputBytes(options, encoding);
            byte[] keyBytes = ResolveKeyBytes(options, encoding);
            try
            {
                byte[] hashBytes = CryptographyHelper.HashDataWithKey(options.KeyedHashAlgorithm, inputBytes, keyBytes);
                return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
            }
            finally
            {
                ClearIfDerived(options, keyBytes);
            }
        }

        public string KeyedHashFile(CryptoOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            if (string.IsNullOrWhiteSpace(options.Input))
                throw new ArgumentException("CryptoOptions.Input must contain the file path.", nameof(options));

            var encoding = ResolveEncoding(options);
            byte[] keyBytes = ResolveKeyBytes(options, encoding);
            try
            {
                byte[] inputBytes = File.ReadAllBytes(options.Input);
                byte[] hashBytes = CryptographyHelper.HashDataWithKey(options.KeyedHashAlgorithm, inputBytes, keyBytes);
                return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
            }
            finally
            {
                ClearIfDerived(options, keyBytes);
            }
        }

        public void PgpSignFile(CryptoOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            var pgp = ResolvePgpConfig(options);
            if (string.IsNullOrWhiteSpace(options.Input))
                throw new ArgumentException("CryptoOptions.Input must contain the input file path.", nameof(options));
            if (string.IsNullOrWhiteSpace(options.OutputFile))
                throw new ArgumentException("CryptoOptions.OutputFile must contain the output file path.", nameof(options));
            if (string.IsNullOrWhiteSpace(pgp.PrivateKeyFilePath))
                throw new ArgumentException("PGPOptions.PrivateKeyFilePath is required for signing.", nameof(options));
            if (!options.Overwrite && File.Exists(options.OutputFile))
                throw new InvalidOperationException($"Output file already exists: {options.OutputFile}");

            string passphrase = ResolvePassphrase(pgp);
            byte[] inputBytes = File.ReadAllBytes(options.Input);
            using var privateKeyStream = File.OpenRead(pgp.PrivateKeyFilePath);
            byte[] signed = CryptographyHelper.PgpSign(inputBytes, privateKeyStream, passphrase);
            File.WriteAllBytes(options.OutputFile, signed);
        }

        public void PgpClearSignFile(CryptoOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            var pgp = ResolvePgpConfig(options);
            if (string.IsNullOrWhiteSpace(options.Input))
                throw new ArgumentException("CryptoOptions.Input must contain the input file path.", nameof(options));
            if (string.IsNullOrWhiteSpace(options.OutputFile))
                throw new ArgumentException("CryptoOptions.OutputFile must contain the output file path.", nameof(options));
            if (string.IsNullOrWhiteSpace(pgp.PrivateKeyFilePath))
                throw new ArgumentException("PGPOptions.PrivateKeyFilePath is required for clear-signing.", nameof(options));
            if (!options.Overwrite && File.Exists(options.OutputFile))
                throw new InvalidOperationException($"Output file already exists: {options.OutputFile}");

            string passphrase = ResolvePassphrase(pgp);
            byte[] inputBytes = File.ReadAllBytes(options.Input);
            using var privateKeyStream = File.OpenRead(pgp.PrivateKeyFilePath);
            byte[] signed = CryptographyHelper.PgpClearSign(inputBytes, privateKeyStream, passphrase);
            File.WriteAllBytes(options.OutputFile, signed);
        }

        public bool PgpVerify(CryptoOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            var pgp = ResolvePgpConfig(options);
            if (string.IsNullOrWhiteSpace(pgp.PublicKeyFilePath))
                throw new ArgumentException("PGPOptions.PublicKeyFilePath is required for verification.", nameof(options));

            using var publicKeyStream = File.OpenRead(pgp.PublicKeyFilePath);

            if (pgp.VerifyMode == PgpVerifyMode.PublicKey)
                return CryptographyHelper.PgpVerifyPublicKey(publicKeyStream);

            if (string.IsNullOrWhiteSpace(options.Input))
                throw new ArgumentException("CryptoOptions.Input must contain the input file path.", nameof(options));

            byte[] inputBytes = File.ReadAllBytes(options.Input);
            return pgp.VerifyMode == PgpVerifyMode.ClearSignature
                ? CryptographyHelper.PgpVerifyClear(inputBytes, publicKeyStream)
                : CryptographyHelper.PgpVerify(inputBytes, publicKeyStream);
        }

        public void PgpGenerateKeyPair(PGPOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            if (string.IsNullOrWhiteSpace(options.PublicKeyFilePath))
                throw new ArgumentException("PGPOptions.PublicKeyFilePath is required.", nameof(options));
            if (string.IsNullOrWhiteSpace(options.PrivateKeyFilePath))
                throw new ArgumentException("PGPOptions.PrivateKeyFilePath is required.", nameof(options));
            if (string.IsNullOrWhiteSpace(options.Username))
                throw new ArgumentException("PGPOptions.Username is required.", nameof(options));

            string passphrase = ResolvePassphrase(options);
            CryptographyHelper.PgpGenerateKeyPair(options.PublicKeyFilePath, options.PrivateKeyFilePath, options.Username, passphrase);
        }

        public void PgpGenerateKeyPair(CryptoOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            var pgp = ResolvePgpConfig(options);
            if (string.IsNullOrWhiteSpace(pgp.PublicKeyFilePath))
                throw new ArgumentException("PGPOptions.PublicKeyFilePath is required.", nameof(options));
            if (string.IsNullOrWhiteSpace(pgp.PrivateKeyFilePath))
                throw new ArgumentException("PGPOptions.PrivateKeyFilePath is required.", nameof(options));
            if (string.IsNullOrWhiteSpace(pgp.Username))
                throw new ArgumentException("PGPOptions.Username is required.", nameof(options));
            if (!options.Overwrite && File.Exists(pgp.PublicKeyFilePath))
                throw new InvalidOperationException($"Public key file already exists: {pgp.PublicKeyFilePath}");
            if (!options.Overwrite && File.Exists(pgp.PrivateKeyFilePath))
                throw new InvalidOperationException($"Private key file already exists: {pgp.PrivateKeyFilePath}");

            string passphrase = ResolvePassphrase(pgp);
            CryptographyHelper.PgpGenerateKeyPair(pgp.PublicKeyFilePath, pgp.PrivateKeyFilePath, pgp.Username, passphrase);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        // Validates that PgpConfig is set; throws a clear ArgumentException otherwise.
        private static PGPOptions ResolvePgpConfig(CryptoOptions options) =>
            options.PgpConfig
                ?? throw new ArgumentException("CryptoOptions.PgpConfig must be set for PGP operations.", nameof(options));

        private static void ExecutePgpEncryptFile(CryptoOptions options)
        {
            var pgp = options.PgpConfig
                ?? throw new ArgumentException("CryptoOptions.PgpConfig must be set when Algorithm is PGP.", nameof(options));
            if (string.IsNullOrWhiteSpace(pgp.PublicKeyFilePath))
                throw new ArgumentException("PGPOptions.PublicKeyFilePath is required.", nameof(options));

            string passphrase = pgp.SignData ? ResolvePassphrase(pgp) : null;

            using var publicKeyStream = File.OpenRead(pgp.PublicKeyFilePath);
            Stream privateKeyStream = pgp.SignData ? File.OpenRead(pgp.PrivateKeyFilePath) : null;
            try
            {
                byte[] inputBytes = File.ReadAllBytes(options.Input);
                byte[] encryptedBytes = CryptographyHelper.PgpEncrypt(inputBytes, publicKeyStream, privateKeyStream, passphrase, pgp.SignData);
                File.WriteAllBytes(options.OutputFile, encryptedBytes);
            }
            finally
            {
                privateKeyStream?.Dispose();
            }
        }

        private static void ExecutePgpDecryptFile(CryptoOptions options)
        {
            var pgp = options.PgpConfig
                ?? throw new ArgumentException("CryptoOptions.PgpConfig must be set when Algorithm is PGP.", nameof(options));
            if (string.IsNullOrWhiteSpace(pgp.PrivateKeyFilePath))
                throw new ArgumentException("PGPOptions.PrivateKeyFilePath is required for decryption.", nameof(options));

            string passphrase = ResolvePassphrase(pgp);

            using var privateKeyStream = File.OpenRead(pgp.PrivateKeyFilePath);
            Stream publicKeyStream = pgp.SignData ? File.OpenRead(pgp.PublicKeyFilePath) : null;
            try
            {
                byte[] inputBytes = File.ReadAllBytes(options.Input);
                byte[] decryptedBytes = CryptographyHelper.PgpDecrypt(inputBytes, privateKeyStream, passphrase, publicKeyStream, pgp.SignData);
                File.WriteAllBytes(options.OutputFile, decryptedBytes);
            }
            finally
            {
                publicKeyStream?.Dispose();
            }
        }

        // Extracts the passphrase from PGPOptions.Passphrase (SecureString) to a plain string.
        // NetworkCredential is used to avoid creating a managed string via Marshal — it is the
        // same pattern used in the existing activity layer (PgpStreamHelper).
        private static string ResolvePassphrase(PGPOptions pgp) =>
            pgp.Passphrase is { Length: > 0 }
                ? new System.Net.NetworkCredential(string.Empty, pgp.Passphrase).Password
                : null;

        private static Encoding ResolveEncoding(CryptoOptions options) =>
            options.Encoding ?? Encoding.UTF8;

        /// <summary>
        /// Resolves plain-text input bytes from <see cref="CryptoOptions"/>.
        /// Priority: <see cref="CryptoOptions.InputRaw"/> → <see cref="CryptoOptions.InputSecure"/> → <see cref="CryptoOptions.Input"/>.
        /// </summary>
        private static byte[] ResolveInputBytes(CryptoOptions options, Encoding encoding)
        {
            if (options.InputRaw is { Length: > 0 })
                return options.InputRaw;

            if (options.InputSecure is not null)
                return SecureStringToBytes(options.InputSecure, encoding);

            if (!string.IsNullOrEmpty(options.Input))
                return encoding.GetBytes(options.Input);

            throw new ArgumentException("CryptoOptions must supply input data via Input, InputSecure, or InputRaw.", nameof(options));
        }

        /// <summary>
        /// Resolves ciphertext bytes for decryption.
        /// <see cref="CryptoOptions.InputRaw"/> is used directly (raw cipher bytes);
        /// otherwise <see cref="CryptoOptions.Input"/> is treated as a Base-64 string and decoded.
        /// </summary>
        private static byte[] ResolveCiphertextBytes(CryptoOptions options)
        {
            if (options.InputRaw is { Length: > 0 })
                return options.InputRaw;

            if (!string.IsNullOrEmpty(options.Input))
                return Convert.FromBase64String(options.Input);

            throw new ArgumentException("CryptoOptions must supply ciphertext via Input (Base-64) or InputRaw.", nameof(options));
        }

        /// <summary>
        /// Resolves key bytes from <see cref="CryptoOptions"/>.
        /// Priority: <see cref="CryptoOptions.KeyRaw"/> → <see cref="CryptoOptions.KeySecure"/> → <see cref="CryptoOptions.Key"/>.
        /// </summary>
        private static byte[] ResolveKeyBytes(CryptoOptions options, Encoding encoding)
        {
            if (options.KeyRaw is { Length: > 0 })
                return options.KeyRaw;

            if (options.KeySecure is not null)
                return SecureStringToBytes(options.KeySecure, encoding);

            if (!string.IsNullOrEmpty(options.Key))
                return CryptographyHelper.KeyEncoding(encoding, options.Key, null);

            throw new ArgumentException("CryptoOptions must supply key material via Key, KeySecure, or KeyRaw.", nameof(options));
        }

        // Zero derived key bytes only when they were not passed in directly as KeyRaw
        // (caller owns KeyRaw and is responsible for clearing it).
        private static void ClearIfDerived(CryptoOptions options, byte[] keyBytes)
        {
            if (!ReferenceEquals(keyBytes, options.KeyRaw))
                Array.Clear(keyBytes, 0, keyBytes.Length);
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
    }
}
