using System.Security;
using System.Text;
using UiPath.Cryptography.Enums;

namespace UiPath.Cryptography.Activities.API
{
    /// <summary>
    /// Encapsulates all inputs needed by a cryptographic operation.
    /// </summary>
    /// <remarks>
    /// <para><b>Input data</b> — supply exactly one of <see cref="Input"/>, <see cref="InputSecure"/>,
    /// or <see cref="InputRaw"/> depending on how the data is available.
    /// For file operations <see cref="Input"/> holds the file path.</para>
    /// <para><b>Key material</b> — supply exactly one of <see cref="Key"/>, <see cref="KeySecure"/>,
    /// or <see cref="KeyRaw"/>. Resolution priority: <see cref="KeyRaw"/> → <see cref="KeySecure"/> → <see cref="Key"/>.
    /// Key fields are ignored when <see cref="Algorithm"/> is <see cref="UiPath.Cryptography.EncryptionAlgorithm.PGP"/>.</para>
    /// <para><b>PGP</b> — set <see cref="Algorithm"/> to <see cref="UiPath.Cryptography.EncryptionAlgorithm.PGP"/> and
    /// populate <see cref="PgpConfig"/>. PGP is supported for file operations only.</para>
    /// </remarks>
    public sealed class CryptoOptions
    {
        // ── Input data ────────────────────────────────────────────────────────

        /// <summary>Plain-text input string, ciphertext, or file path.</summary>
        public string Input { get; set; }

        /// <summary>Input data as a <see cref="SecureString"/> (text operations only).</summary>
        public SecureString InputSecure { get; set; }

        /// <summary>Input data as raw bytes (text operations only).</summary>
        public byte[] InputRaw { get; set; }

        // ── Key material ──────────────────────────────────────────────────────

        /// <summary>Plain-text cryptographic key.</summary>
        public string Key { get; set; }

        /// <summary>Cryptographic key as a <see cref="SecureString"/>; zeroed after use where possible.</summary>
        public SecureString KeySecure { get; set; }

        /// <summary>Raw key bytes; used directly without PBKDF2 conversion.</summary>
        public byte[] KeyRaw { get; set; }

        // ── Algorithm / behaviour ─────────────────────────────────────────────

        /// <summary>Symmetric encryption / decryption algorithm.</summary>
        public EncryptionAlgorithm Algorithm { get; set; }

        /// <summary>Keyed-hash algorithm (used only by <c>KeyedHashText</c> / <c>KeyedHashFile</c>).</summary>
        public KeyedHashAlgorithms KeyedHashAlgorithm { get; set; }

        /// <summary>Whether to overwrite the output file if it already exists (file operations only).</summary>
        public bool Overwrite { get; set; }

        /// <summary>Output file path (file operations only).</summary>
        public string OutputFile { get; set; }

        /// <summary>Text encoding used to convert strings to bytes. Defaults to <see cref="Encoding.UTF8"/> when <see langword="null"/>.</summary>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// PGP-specific options. Must be set when <see cref="Algorithm"/> is
        /// <see cref="UiPath.Cryptography.EncryptionAlgorithm.PGP"/>; ignored otherwise.
        /// </summary>
        public PGPOptions PgpConfig { get; set; }
    }
}
