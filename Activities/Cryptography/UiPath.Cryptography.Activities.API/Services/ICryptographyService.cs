using System.Text;

namespace UiPath.Cryptography.Activities.API
{
    /// <summary>
    /// Provides cryptography capabilities for coded workflows.
    /// </summary>
    /// <remarks>
    /// <para><b>Key material</b></para>
    /// <para>
    /// Each method resolves the cryptographic key from <see cref="CryptoOptions"/> in the following
    /// priority order: <see cref="CryptoOptions.KeyRaw"/> (raw bytes, used directly) →
    /// <see cref="CryptoOptions.KeySecure"/> (SecureString, extracted via unmanaged memory, zeroed after use) →
    /// <see cref="CryptoOptions.Key"/> (plain string, encoded to bytes via <see cref="CryptoOptions.Encoding"/>).
    /// </para>
    /// <para><b>Input data</b></para>
    /// <para>
    /// For text operations the input is resolved from <see cref="CryptoOptions.Input"/>,
    /// <see cref="CryptoOptions.InputSecure"/>, or <see cref="CryptoOptions.InputRaw"/> (first non-null wins).
    /// For file operations <see cref="CryptoOptions.Input"/> holds the input file path and
    /// <see cref="CryptoOptions.OutputFile"/> holds the output file path.
    /// </para>
    /// <para><b>PGP</b></para>
    /// <para>
    /// Set <see cref="CryptoOptions.Algorithm"/> to <see cref="UiPath.Cryptography.EncryptionAlgorithm.PGP"/>
    /// and populate <see cref="CryptoOptions.PgpConfig"/> with a <see cref="PGPOptions"/> instance.
    /// PGP is supported for file operations only (<see cref="EncryptFile"/>, <see cref="DecryptFile"/>);
    /// calling <see cref="EncryptText"/> or <see cref="DecryptText"/> with PGP throws <see cref="InvalidOperationException"/>.
    /// Key fields on <see cref="CryptoOptions"/> are ignored when PGP is selected.
    /// </para>
    /// <para><b>IV / salt / nonce strategy (symmetric methods)</b></para>
    /// <para>
    /// All symmetric encrypt methods (<see cref="EncryptText"/>, <see cref="EncryptFile"/>) are
    /// <b>non-deterministic</b>: a fresh random salt (8 bytes, PBKDF2) and IV/nonce are generated
    /// on every call and prepended to the ciphertext.  The paired decrypt methods reconstruct the
    /// salt and IV from the same prefix.
    /// </para>
    /// </remarks>
    public interface ICryptographyService
    {
        /// <summary>
        /// Encrypts a string and returns the Base-64 encoded ciphertext.
        /// The plaintext is read from <see cref="CryptoOptions.Input"/>, <see cref="CryptoOptions.InputSecure"/>,
        /// or <see cref="CryptoOptions.InputRaw"/> (first non-null wins).
        /// </summary>
        /// <param name="options">Cryptographic options including input data, key material, algorithm, and encoding.</param>
        /// <returns>Base-64 encoded ciphertext.</returns>
        string EncryptText(CryptoOptions options);

        /// <summary>
        /// Decrypts a ciphertext and returns the original plaintext.
        /// Supply raw cipher bytes via <see cref="CryptoOptions.InputRaw"/>, or a Base-64 encoded
        /// ciphertext string via <see cref="CryptoOptions.Input"/> (first non-null wins).
        /// </summary>
        /// <param name="options">Cryptographic options including input data, key material, algorithm, and encoding.</param>
        /// <returns>The original plaintext.</returns>
        string DecryptText(CryptoOptions options);

        /// <summary>
        /// Encrypts a file and writes the result to the output path.
        /// The input file path is read from <see cref="CryptoOptions.Input"/> and the output path from <see cref="CryptoOptions.OutputFile"/>.
        /// </summary>
        /// <param name="options">Cryptographic options including input/output file paths, key material, algorithm, encoding, and overwrite flag.</param>
        void EncryptFile(CryptoOptions options);

        /// <summary>
        /// Decrypts a file and writes the result to the output path.
        /// The input file path is read from <see cref="CryptoOptions.Input"/> and the output path from <see cref="CryptoOptions.OutputFile"/>.
        /// </summary>
        /// <param name="options">Cryptographic options including input/output file paths, key material, algorithm, encoding, and overwrite flag.</param>
        void DecryptFile(CryptoOptions options);

        /// <summary>
        /// Computes a keyed hash of a string and returns the hex-encoded result.
        /// The input string is read from <see cref="CryptoOptions.Input"/>, <see cref="CryptoOptions.InputSecure"/>,
        /// or <see cref="CryptoOptions.InputRaw"/> (first non-null wins).
        /// </summary>
        /// <param name="options">Cryptographic options including input data, key material, keyed-hash algorithm, and encoding.</param>
        /// <returns>Hex-encoded hash string.</returns>
        string KeyedHashText(CryptoOptions options);

        /// <summary>
        /// Computes a keyed hash of a file and returns the hex-encoded result.
        /// The file path is read from <see cref="CryptoOptions.Input"/>.
        /// </summary>
        /// <param name="options">Cryptographic options including input file path, key material, and keyed-hash algorithm.</param>
        /// <returns>Hex-encoded hash string.</returns>
        string KeyedHashFile(CryptoOptions options);

        // ── PGP operations ────────────────────────────────────────────────────

        /// <summary>
        /// Signs a file using a PGP private key and writes the signed output.
        /// <see cref="CryptoOptions.Input"/> holds the input file path,
        /// <see cref="CryptoOptions.OutputFile"/> the output path.
        /// </summary>
        /// <param name="options">Options with <see cref="CryptoOptions.PgpConfig"/> supplying
        /// <see cref="PGPOptions.PrivateKeyFilePath"/> and <see cref="PGPOptions.Passphrase"/>.</param>
        void PgpSignFile(CryptoOptions options);

        /// <summary>
        /// Clear-signs a file using a PGP private key and writes the signed output.
        /// <see cref="CryptoOptions.Input"/> holds the input file path,
        /// <see cref="CryptoOptions.OutputFile"/> the output path.
        /// </summary>
        /// <param name="options">Options with <see cref="CryptoOptions.PgpConfig"/> supplying
        /// <see cref="PGPOptions.PrivateKeyFilePath"/> and <see cref="PGPOptions.Passphrase"/>.</param>
        void PgpClearSignFile(CryptoOptions options);

        /// <summary>
        /// Verifies a PGP signature or validates a public key.
        /// <see cref="CryptoOptions.Input"/> holds the input file path (not required when
        /// <see cref="PGPOptions.VerifyMode"/> is <see cref="UiPath.Cryptography.Enums.PgpVerifyMode.PublicKey"/>).
        /// </summary>
        /// <param name="options">Options with <see cref="CryptoOptions.PgpConfig"/> supplying
        /// <see cref="PGPOptions.PublicKeyFilePath"/> and <see cref="PGPOptions.VerifyMode"/>.</param>
        /// <returns><see langword="true"/> if verification succeeds.</returns>
        bool PgpVerify(CryptoOptions options);

        /// <summary>
        /// Generates a PGP key pair and writes the public and private key files.
        /// Output paths are taken from <see cref="PGPOptions.PublicKeyFilePath"/> and
        /// <see cref="PGPOptions.PrivateKeyFilePath"/>.
        /// </summary>
        /// <param name="options">PGP options supplying <see cref="PGPOptions.PublicKeyFilePath"/>,
        /// <see cref="PGPOptions.PrivateKeyFilePath"/>, <see cref="PGPOptions.Username"/>,
        /// and <see cref="PGPOptions.Passphrase"/>.</param>
        void PgpGenerateKeyPair(PGPOptions options);

        /// <summary>
        /// Generates a PGP key pair using PGP config from <see cref="CryptoOptions.PgpConfig"/>.
        /// Output paths, username, and passphrase are read from <see cref="CryptoOptions.PgpConfig"/>.
        /// <see cref="CryptoOptions.Overwrite"/> controls whether existing key files are overwritten.
        /// </summary>
        /// <param name="options">Crypto options whose <see cref="CryptoOptions.PgpConfig"/> carries
        /// the key generation parameters.</param>
        void PgpGenerateKeyPair(CryptoOptions options);
    }
}
