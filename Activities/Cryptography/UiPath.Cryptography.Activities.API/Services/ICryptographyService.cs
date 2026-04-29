using System.IO;
using System.Net;
using System.Security;
using System.Text;
using UiPath.Cryptography.Enums;

namespace UiPath.Cryptography.Activities.API
{
    /// <summary>
    /// Provides cryptography capabilities for coded workflows.
    /// </summary>
    /// <remarks>
    /// <para><b>IV / salt / nonce strategy (symmetric methods)</b></para>
    /// <para>
    /// All symmetric encrypt methods (<see cref="EncryptText"/>, <see cref="EncryptFile"/>) are
    /// <b>non-deterministic</b>: a fresh random salt (8 bytes, PBKDF2) and IV/nonce are generated
    /// on every call and prepended to the ciphertext.  Encrypting the same plaintext twice always
    /// produces different ciphertext.  The paired decrypt methods reconstruct the salt and IV from
    /// the same prefix, so the output of Encrypt can always be fed directly to Decrypt without
    /// supplying the IV separately.
    /// </para>
    /// <para>
    /// CBC-family algorithms (AES/Rijndael/DES/3DES/RC2) use PKCS7 padding, CBC mode, and a
    /// randomly-generated IV.  AES-GCM (<see cref="EncryptionAlgorithm.AESGCM"/>) uses a
    /// randomly-generated 96-bit nonce and a 128-bit authentication tag, providing authenticated
    /// encryption with associated data (AEAD) — it is the recommended choice for new workflows.
    /// </para>
    /// <para><b>Key material</b></para>
    /// <para>
    /// Every method that accepts a <c>string key</c> has a paired overload that accepts
    /// <c>byte[] keyBytes</c> (raw key material) or <c>SecureString key</c>.
    /// Prefer the <c>byte[]</c> overload when key material is already loaded into memory as bytes;
    /// prefer the <c>SecureString</c> overload when receiving the key from user input or a secret
    /// store that surfaces <see cref="SecureString"/>.  The plain-string overload remains for
    /// compatibility but note that <see langword="string"/> values are immutable and may be interned,
    /// meaning the secret can linger on the heap until GC.
    /// </para>
    /// </remarks>
    public interface ICryptographyService
    {
        // ── Symmetric: string key ────────────────────────────────────────────

        /// <summary>Encrypts a string using the specified algorithm and key.</summary>
        /// <inheritdoc cref="ICryptographyService" path="/remarks"/>
        string EncryptText(string input, EncryptionAlgorithm algorithm, string key, Encoding encoding);

        /// <summary>Encrypts a string using the specified algorithm and a <see cref="SecureString"/> key.</summary>
        /// <inheritdoc cref="ICryptographyService" path="/remarks"/>
        string EncryptText(string input, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding);

        /// <summary>Encrypts a string using the specified algorithm and raw key bytes.</summary>
        /// <inheritdoc cref="ICryptographyService" path="/remarks"/>
        string EncryptText(string input, EncryptionAlgorithm algorithm, byte[] keyBytes, Encoding encoding);

        /// <summary>Decrypts a string using the specified algorithm and key.</summary>
        string DecryptText(string input, EncryptionAlgorithm algorithm, string key, Encoding encoding);

        /// <summary>Decrypts a string using the specified algorithm and a <see cref="SecureString"/> key.</summary>
        string DecryptText(string input, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding);

        /// <summary>Decrypts a string using the specified algorithm and raw key bytes.</summary>
        string DecryptText(string input, EncryptionAlgorithm algorithm, byte[] keyBytes, Encoding encoding);

        // ── Symmetric: string key ────────────────────────────────────────────

        /// <summary>Encrypts a file and writes the result to the output path.</summary>
        void EncryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, string key, Encoding encoding, bool overwrite);

        /// <summary>Decrypts a file and writes the result to the output path.</summary>
        void DecryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, string key, Encoding encoding, bool overwrite);

        // ── Symmetric: SecureString key ──────────────────────────────────────

        /// <summary>Encrypts a file and writes the result to the output path using a <see cref="SecureString"/> key.</summary>
        void EncryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding, bool overwrite);

        /// <summary>Decrypts a file and writes the result to the output path using a <see cref="SecureString"/> key.</summary>
        void DecryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding, bool overwrite);

        // ── Symmetric: byte[] key (raw material — no PBKDF2 string conversion) ──

        /// <summary>Encrypts a file and writes the result to the output path using raw key bytes.</summary>
        void EncryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, byte[] keyBytes, bool overwrite);

        /// <summary>Decrypts a file and writes the result to the output path using raw key bytes.</summary>
        void DecryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, byte[] keyBytes, bool overwrite);

        // ── Keyed hash: string key ───────────────────────────────────────────

        /// <summary>Computes a keyed hash of a string and returns the hex-encoded result.</summary>
        string KeyedHashText(string input, KeyedHashAlgorithms algorithm, string key, Encoding encoding);

        /// <summary>Computes a keyed hash of a file and returns the hex-encoded result.</summary>
        string KeyedHashFile(string filePath, KeyedHashAlgorithms algorithm, string key, Encoding encoding);

        // ── Keyed hash: SecureString key ─────────────────────────────────────

        /// <summary>Computes a keyed hash of a string using a <see cref="SecureString"/> key and returns the hex-encoded result.</summary>
        string KeyedHashText(string input, KeyedHashAlgorithms algorithm, SecureString key, Encoding encoding);

        /// <summary>Computes a keyed hash of a file using a <see cref="SecureString"/> key and returns the hex-encoded result.</summary>
        string KeyedHashFile(string filePath, KeyedHashAlgorithms algorithm, SecureString key, Encoding encoding);

        // ── Keyed hash: byte[] key ────────────────────────────────────────────

        /// <summary>Computes a keyed hash of a string using raw key bytes and returns the hex-encoded result.</summary>
        string KeyedHashText(string input, KeyedHashAlgorithms algorithm, byte[] keyBytes, Encoding encoding);

        /// <summary>Computes a keyed hash of a file using raw key bytes and returns the hex-encoded result.</summary>
        string KeyedHashFile(string filePath, KeyedHashAlgorithms algorithm, byte[] keyBytes);

        // ── PGP: string passphrase ────────────────────────────────────────────

        /// <summary>Encrypts bytes using PGP with the provided public key stream.</summary>
        byte[] PgpEncrypt(byte[] inputBytes, Stream publicKeyStream, Stream privateKeyStream = null, string passphrase = null, bool sign = false);

        /// <summary>Decrypts PGP-encrypted bytes using the provided private key stream.</summary>
        byte[] PgpDecrypt(byte[] inputBytes, Stream privateKeyStream, string passphrase, Stream publicKeyStream = null, bool verifySignature = false);

        /// <summary>Encrypts a string using PGP with the provided public key stream.</summary>
        string PgpEncryptText(string input, Stream publicKeyStream, Stream privateKeyStream = null, string passphrase = null, bool sign = false);

        /// <summary>Decrypts a PGP-encrypted string using the provided private key stream.</summary>
        string PgpDecryptText(string input, Stream privateKeyStream, string passphrase, Stream publicKeyStream = null, bool verifySignature = false);

        /// <summary>Signs bytes using PGP with the provided private key stream.</summary>
        byte[] PgpSignFile(byte[] inputBytes, Stream privateKeyStream, string passphrase);

        /// <summary>Creates a PGP clear-text signature for the given bytes.</summary>
        byte[] PgpClearSignFile(byte[] inputBytes, Stream privateKeyStream, string passphrase);

        // ── PGP: SecureString passphrase ──────────────────────────────────────

        /// <summary>Encrypts bytes using PGP with the provided public key stream and a <see cref="SecureString"/> passphrase.</summary>
        byte[] PgpEncrypt(byte[] inputBytes, Stream publicKeyStream, Stream privateKeyStream, SecureString passphrase, bool sign = false);

        /// <summary>Decrypts PGP-encrypted bytes using the provided private key stream and a <see cref="SecureString"/> passphrase.</summary>
        byte[] PgpDecrypt(byte[] inputBytes, Stream privateKeyStream, SecureString passphrase, Stream publicKeyStream = null, bool verifySignature = false);

        /// <summary>Encrypts a string using PGP with the provided public key stream and a <see cref="SecureString"/> passphrase.</summary>
        string PgpEncryptText(string input, Stream publicKeyStream, Stream privateKeyStream, SecureString passphrase, bool sign = false);

        /// <summary>Decrypts a PGP-encrypted string using the provided private key stream and a <see cref="SecureString"/> passphrase.</summary>
        string PgpDecryptText(string input, Stream privateKeyStream, SecureString passphrase, Stream publicKeyStream = null, bool verifySignature = false);

        /// <summary>Signs bytes using PGP with the provided private key stream and a <see cref="SecureString"/> passphrase.</summary>
        byte[] PgpSignFile(byte[] inputBytes, Stream privateKeyStream, SecureString passphrase);

        /// <summary>Creates a PGP clear-text signature using a <see cref="SecureString"/> passphrase.</summary>
        byte[] PgpClearSignFile(byte[] inputBytes, Stream privateKeyStream, SecureString passphrase);

        // ── PGP: verify / key-gen ─────────────────────────────────────────────

        /// <summary>Verifies a PGP signature against the provided public key stream.</summary>
        bool PgpVerify(byte[] inputBytes, Stream publicKeyStream);

        /// <summary>Verifies a PGP clear-text signature against the provided public key stream.</summary>
        bool PgpVerifyClear(byte[] inputBytes, Stream publicKeyStream);

        /// <summary>Generates a PGP key pair and writes the keys to the specified paths.</summary>
        void PgpGenerateKeyPair(string publicKeyPath, string privateKeyPath, string username, string password);
    }
}
