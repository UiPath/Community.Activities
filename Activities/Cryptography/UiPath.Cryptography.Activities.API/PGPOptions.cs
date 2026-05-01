using System.Security;
using UiPath.Cryptography.Enums;

namespace UiPath.Cryptography.Activities.API
{
    /// <summary>
    /// PGP-specific options used by PGP operations on <see cref="ICryptographyService"/>.
    /// </summary>
    /// <remarks>
    /// For <see cref="ICryptographyService.EncryptFile"/> and <see cref="ICryptographyService.DecryptFile"/>
    /// set <see cref="CryptoOptions.Algorithm"/> to <see cref="UiPath.Cryptography.EncryptionAlgorithm.PGP"/>
    /// and assign an instance to <see cref="CryptoOptions.PgpConfig"/>.
    /// For dedicated PGP operations (<see cref="ICryptographyService.PgpSignFile"/>,
    /// <see cref="ICryptographyService.PgpClearSignFile"/>, <see cref="ICryptographyService.PgpVerify"/>,
    /// <see cref="ICryptographyService.PgpGenerateKeyPair(PGPOptions)"/>) pass the instance directly.
    /// Not all fields are required for every operation — see each property's remarks.
    /// </remarks>
    public sealed class PGPOptions
    {
        // ── Key files ─────────────────────────────────────────────────────────

        /// <summary>
        /// Path to the ASCII-armored or binary PGP public key file.
        /// Required for: <c>EncryptFile</c>, <c>PgpVerify</c> (Signature/ClearSignature/PublicKey modes),
        /// <c>DecryptFile</c> when verifying signature, <c>PgpGenerateKeyPair</c> (output path).
        /// </summary>
        public string PublicKeyFilePath { get; set; }

        /// <summary>
        /// Path to the ASCII-armored or binary PGP private key file.
        /// Required for: <c>DecryptFile</c>, <c>PgpSignFile</c>, <c>PgpClearSignFile</c>,
        /// <c>EncryptFile</c> when <see cref="SignData"/> is <see langword="true"/>,
        /// <c>PgpGenerateKeyPair</c> (output path).
        /// </summary>
        public string PrivateKeyFilePath { get; set; }

        // ── Authentication ────────────────────────────────────────────────────

        /// <summary>
        /// Passphrase protecting the private key.
        /// Required for: <c>DecryptFile</c>, <c>PgpSignFile</c>, <c>PgpClearSignFile</c>,
        /// <c>EncryptFile</c> when <see cref="SignData"/> is <see langword="true"/>,
        /// <c>PgpGenerateKeyPair</c>.
        /// </summary>
        public SecureString Passphrase { get; set; }

        // ── Behaviour flags ───────────────────────────────────────────────────

        /// <summary>
        /// When <see langword="true"/> the encrypted output is also signed using
        /// <see cref="PrivateKeyFilePath"/> and <see cref="Passphrase"/>.
        /// Used by: <c>EncryptFile</c>, <c>DecryptFile</c> (verify signature on decrypt).
        /// </summary>
        public bool SignData { get; set; }

        /// <summary>
        /// Verification mode used by <see cref="ICryptographyService.PgpVerify"/>.
        /// <list type="bullet">
        ///   <item><see cref="PgpVerifyMode.Signature"/> — verify a detached/inline PGP signature.</item>
        ///   <item><see cref="PgpVerifyMode.ClearSignature"/> — verify a clear-signed file.</item>
        ///   <item><see cref="PgpVerifyMode.PublicKey"/> — validate the public key itself (no input file needed).</item>
        /// </list>
        /// </summary>
        public PgpVerifyMode VerifyMode { get; set; }

        // ── Key generation ────────────────────────────────────────────────────

        /// <summary>
        /// PGP user identity string (e.g. <c>"Alice &lt;alice@example.com&gt;"</c>).
        /// Used only by <see cref="ICryptographyService.PgpGenerateKeyPair(PGPOptions)"/>.
        /// </summary>
        public string Username { get; set; }
    }
}
