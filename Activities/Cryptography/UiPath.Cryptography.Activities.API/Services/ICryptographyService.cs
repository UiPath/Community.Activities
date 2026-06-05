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
    /// <para><b>API shape — Bytes / Text / File matrix</b></para>
    /// <para>
    /// Every logical operation (symmetric encrypt/decrypt, keyed hash, PGP encrypt/decrypt/sign/clearsign/verify)
    /// exposes three input/output forms so callers can use the one that matches the data they already have:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Bytes form</b> — base method name. Bytes in, bytes out. The most flexible; binary or text data both work as <c>byte[]</c>.</description></item>
    /// <item><description><b>Text form</b> — <c>...Text</c> suffix. String in, string out (Base64 for symmetric, ASCII-armored for PGP). Use when the data arrives as text from HTTP/config/env-vars.</description></item>
    /// <item><description><b>File form</b> — <c>...File</c> suffix. File-path in, file-path out. Use when the data lives on disk.</description></item>
    /// </list>
    /// <para><b>IV / salt / nonce strategy (symmetric methods)</b></para>
    /// <para>
    /// All symmetric encrypt methods are <b>non-deterministic</b>: a fresh random salt (8 bytes, PBKDF2) and IV/nonce
    /// are generated on every call and prepended to the ciphertext. Encrypting the same plaintext twice always
    /// produces different ciphertext. The paired decrypt methods reconstruct the salt and IV from the same prefix,
    /// so the output of Encrypt can always be fed directly to Decrypt without supplying the IV separately.
    /// </para>
    /// <para>
    /// CBC-family algorithms (AES/Rijndael/DES/3DES/RC2) use PKCS7 padding, CBC mode, and a randomly-generated IV.
    /// AES-GCM (<see cref="EncryptionAlgorithm.AESGCM"/>) uses a randomly-generated 96-bit nonce and a 128-bit
    /// authentication tag, providing authenticated encryption with associated data (AEAD) — it is the recommended
    /// choice for new workflows.
    /// </para>
    /// <para><b>Key material</b></para>
    /// <para>
    /// Every symmetric / keyed-hash method accepts a <c>string key</c>, <c>SecureString key</c>, or
    /// <c>byte[] keyBytes</c> overload. Prefer the <c>byte[]</c> overload when key material is already loaded into
    /// memory as bytes. The <c>SecureString</c> overload is supported for symmetric operations and keyed hashes:
    /// the key is extracted via unmanaged memory, encoded to bytes, used, and then zeroed — no managed
    /// <see langword="string"/> is created. The plain-string overload remains for compatibility; note that
    /// <see langword="string"/> values are immutable and may be interned, meaning the secret can linger on the
    /// heap until GC.
    /// </para>
    /// <para><b>PGP keys</b></para>
    /// <para>
    /// PGP methods accept the public/private key as a <c>byte[]</c>. Both ASCII-armored (text starting with
    /// <c>-----BEGIN PGP …-----</c>) and binary OpenPGP encodings are supported — the underlying parser
    /// auto-detects the form. For armored text that already lives in a <see langword="string"/> (HTTP responses,
    /// configuration files, environment variables), convert with <c>Encoding.UTF8.GetBytes(armored)</c> at the
    /// call site.
    /// </para>
    /// <para><b>PGP passphrase limitation</b></para>
    /// <para>
    /// PGP overloads that accept <c>SecureString passphrase</c> must materialise the passphrase to a managed
    /// <see langword="string"/> because the underlying BouncyCastle library requires a plain string and offers no
    /// byte[]-based passphrase API. The managed string cannot be zeroed afterward. For maximum security with PGP,
    /// prefer passing a key ring that does not require a passphrase, or accept that the passphrase will briefly
    /// exist as a managed string.
    /// </para>
    /// </remarks>
    public interface ICryptographyService
    {
        // ── Symmetric encrypt ────────────────────────────────────────────────

        /// <summary>Encrypts bytes using the specified algorithm and a string key.</summary>
        byte[] EncryptBytes(byte[] inputBytes, EncryptionAlgorithm algorithm, string key, Encoding encoding);

        /// <summary>Encrypts bytes using the specified algorithm and a <see cref="SecureString"/> key.</summary>
        byte[] EncryptBytes(byte[] inputBytes, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding);

        /// <summary>Encrypts bytes using the specified algorithm and raw key bytes.</summary>
        byte[] EncryptBytes(byte[] inputBytes, EncryptionAlgorithm algorithm, byte[] keyBytes);


        /// <summary>Encrypts a string using the specified algorithm and key; returns Base64-encoded ciphertext.</summary>
        string EncryptText(string input, EncryptionAlgorithm algorithm, string key, Encoding encoding);

        /// <summary>Encrypts a string using the specified algorithm and a <see cref="SecureString"/> key; returns Base64-encoded ciphertext.</summary>
        string EncryptText(string input, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding);

        /// <summary>Encrypts a string using the specified algorithm and raw key bytes; returns Base64-encoded ciphertext.</summary>
        string EncryptText(string input, EncryptionAlgorithm algorithm, byte[] keyBytes, Encoding encoding);


        /// <summary>Encrypts a file and writes the result to the output path.</summary>
        void EncryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, string key, Encoding encoding, bool overwrite = false);

        /// <summary>Encrypts a file and writes the result to the output path using a <see cref="SecureString"/> key.</summary>
        void EncryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding, bool overwrite = false);

        /// <summary>Encrypts a file and writes the result to the output path using raw key bytes.</summary>
        void EncryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, byte[] keyBytes, bool overwrite = false);


        // ── Symmetric decrypt ────────────────────────────────────────────────

        /// <summary>Decrypts bytes using the specified algorithm and a string key.</summary>
        byte[] DecryptBytes(byte[] inputBytes, EncryptionAlgorithm algorithm, string key, Encoding encoding);

        /// <summary>Decrypts bytes using the specified algorithm and a <see cref="SecureString"/> key.</summary>
        byte[] DecryptBytes(byte[] inputBytes, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding);

        /// <summary>Decrypts bytes using the specified algorithm and raw key bytes.</summary>
        byte[] DecryptBytes(byte[] inputBytes, EncryptionAlgorithm algorithm, byte[] keyBytes);


        /// <summary>Decrypts a Base64-encoded string using the specified algorithm and key.</summary>
        string DecryptText(string input, EncryptionAlgorithm algorithm, string key, Encoding encoding);

        /// <summary>Decrypts a Base64-encoded string using the specified algorithm and a <see cref="SecureString"/> key.</summary>
        string DecryptText(string input, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding);

        /// <summary>Decrypts a Base64-encoded string using the specified algorithm and raw key bytes.</summary>
        string DecryptText(string input, EncryptionAlgorithm algorithm, byte[] keyBytes, Encoding encoding);


        /// <summary>Decrypts a file and writes the result to the output path.</summary>
        void DecryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, string key, Encoding encoding, bool overwrite = false);

        /// <summary>Decrypts a file and writes the result to the output path using a <see cref="SecureString"/> key.</summary>
        void DecryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, SecureString key, Encoding encoding, bool overwrite = false);

        /// <summary>Decrypts a file and writes the result to the output path using raw key bytes.</summary>
        void DecryptFile(string inputFilePath, string outputFilePath, EncryptionAlgorithm algorithm, byte[] keyBytes, bool overwrite = false);


        // ── Keyed hash ────────────────────────────────────────────────────────

        /// <summary>Computes a keyed hash of bytes and returns the hex-encoded result.</summary>
        string KeyedHashBytes(byte[] inputBytes, KeyedHashAlgorithms algorithm, string key, Encoding encoding);

        /// <summary>Computes a keyed hash of bytes using a <see cref="SecureString"/> key and returns the hex-encoded result.</summary>
        string KeyedHashBytes(byte[] inputBytes, KeyedHashAlgorithms algorithm, SecureString key, Encoding encoding);

        /// <summary>Computes a keyed hash of bytes using raw key bytes and returns the hex-encoded result.</summary>
        string KeyedHashBytes(byte[] inputBytes, KeyedHashAlgorithms algorithm, byte[] keyBytes);


        /// <summary>Computes a keyed hash of a string and returns the hex-encoded result.</summary>
        string KeyedHashText(string input, KeyedHashAlgorithms algorithm, string key, Encoding encoding);

        /// <summary>Computes a keyed hash of a string using a <see cref="SecureString"/> key and returns the hex-encoded result.</summary>
        string KeyedHashText(string input, KeyedHashAlgorithms algorithm, SecureString key, Encoding encoding);

        /// <summary>Computes a keyed hash of a string using raw key bytes and returns the hex-encoded result.</summary>
        string KeyedHashText(string input, KeyedHashAlgorithms algorithm, byte[] keyBytes, Encoding encoding);


        /// <summary>Computes a keyed hash of a file and returns the hex-encoded result.</summary>
        string KeyedHashFile(string filePath, KeyedHashAlgorithms algorithm, string key, Encoding encoding);

        /// <summary>Computes a keyed hash of a file using a <see cref="SecureString"/> key and returns the hex-encoded result.</summary>
        string KeyedHashFile(string filePath, KeyedHashAlgorithms algorithm, SecureString key, Encoding encoding);

        /// <summary>Computes a keyed hash of a file using raw key bytes and returns the hex-encoded result.</summary>
        string KeyedHashFile(string filePath, KeyedHashAlgorithms algorithm, byte[] keyBytes);


        // ── PGP encrypt ───────────────────────────────────────────────────────

        /// <summary>Encrypts bytes using PGP with the provided public key.</summary>
        byte[] PgpEncryptBytes(byte[] inputBytes, byte[] publicKey, byte[] privateKey = null, string passphrase = null, bool sign = false);

        /// <summary>Encrypts bytes using PGP with the provided public key and a <see cref="SecureString"/> passphrase.</summary>
        byte[] PgpEncryptBytes(byte[] inputBytes, byte[] publicKey, byte[] privateKey, SecureString passphrase, bool sign = false);


        /// <summary>Encrypts a string using PGP with the provided public key; returns ASCII-armored ciphertext.</summary>
        string PgpEncryptText(string input, byte[] publicKey, byte[] privateKey = null, string passphrase = null, bool sign = false);

        /// <summary>Encrypts a string using PGP with the provided public key and a <see cref="SecureString"/> passphrase; returns ASCII-armored ciphertext.</summary>
        string PgpEncryptText(string input, byte[] publicKey, byte[] privateKey, SecureString passphrase, bool sign = false);


        /// <summary>Encrypts a file using PGP and writes the result to the output path.</summary>
        void PgpEncryptFile(string inputFilePath, string outputFilePath, byte[] publicKey, byte[] privateKey = null, string passphrase = null, bool sign = false, bool overwrite = false);

        /// <summary>Encrypts a file using PGP with a <see cref="SecureString"/> passphrase and writes the result to the output path.</summary>
        void PgpEncryptFile(string inputFilePath, string outputFilePath, byte[] publicKey, byte[] privateKey, SecureString passphrase, bool sign = false, bool overwrite = false);


        // ── PGP decrypt ───────────────────────────────────────────────────────

        /// <summary>Decrypts PGP-encrypted bytes using the provided private key.</summary>
        byte[] PgpDecryptBytes(byte[] inputBytes, byte[] privateKey, string passphrase, byte[] publicKey = null, bool verifySignature = false);

        /// <summary>Decrypts PGP-encrypted bytes using the provided private key and a <see cref="SecureString"/> passphrase.</summary>
        byte[] PgpDecryptBytes(byte[] inputBytes, byte[] privateKey, SecureString passphrase, byte[] publicKey = null, bool verifySignature = false);


        /// <summary>Decrypts a PGP-encrypted ASCII-armored string using the provided private key.</summary>
        string PgpDecryptText(string input, byte[] privateKey, string passphrase, byte[] publicKey = null, bool verifySignature = false);

        /// <summary>Decrypts a PGP-encrypted ASCII-armored string using the provided private key and a <see cref="SecureString"/> passphrase.</summary>
        string PgpDecryptText(string input, byte[] privateKey, SecureString passphrase, byte[] publicKey = null, bool verifySignature = false);


        /// <summary>Decrypts a PGP-encrypted file and writes the result to the output path.</summary>
        void PgpDecryptFile(string inputFilePath, string outputFilePath, byte[] privateKey, string passphrase, byte[] publicKey = null, bool verifySignature = false, bool overwrite = false);

        /// <summary>Decrypts a PGP-encrypted file with a <see cref="SecureString"/> passphrase and writes the result to the output path.</summary>
        void PgpDecryptFile(string inputFilePath, string outputFilePath, byte[] privateKey, SecureString passphrase, byte[] publicKey = null, bool verifySignature = false, bool overwrite = false);


        // ── PGP sign ──────────────────────────────────────────────────────────

        /// <summary>Signs bytes using PGP with the provided private key and returns the signed payload.</summary>
        byte[] PgpSignBytes(byte[] inputBytes, byte[] privateKey, string passphrase);

        /// <summary>Signs bytes using PGP with the provided private key and a <see cref="SecureString"/> passphrase.</summary>
        byte[] PgpSignBytes(byte[] inputBytes, byte[] privateKey, SecureString passphrase);


        /// <summary>Signs a string using PGP with the provided private key; returns the signed payload as ASCII-armored text.</summary>
        string PgpSignText(string input, byte[] privateKey, string passphrase);

        /// <summary>Signs a string using PGP with a <see cref="SecureString"/> passphrase; returns the signed payload as ASCII-armored text.</summary>
        string PgpSignText(string input, byte[] privateKey, SecureString passphrase);


        /// <summary>Signs a file with PGP and writes the signed output to the destination path.</summary>
        void PgpSignFile(string inputFilePath, string outputFilePath, byte[] privateKey, string passphrase, bool overwrite = false);

        /// <summary>Signs a file with PGP using a <see cref="SecureString"/> passphrase and writes the signed output to the destination path.</summary>
        void PgpSignFile(string inputFilePath, string outputFilePath, byte[] privateKey, SecureString passphrase, bool overwrite = false);


        // ── PGP clearsign ─────────────────────────────────────────────────────

        /// <summary>Creates a PGP clear-text signature for the given bytes and returns the signed payload.</summary>
        byte[] PgpClearsignBytes(byte[] inputBytes, byte[] privateKey, string passphrase);

        /// <summary>Creates a PGP clear-text signature using a <see cref="SecureString"/> passphrase.</summary>
        byte[] PgpClearsignBytes(byte[] inputBytes, byte[] privateKey, SecureString passphrase);


        /// <summary>Creates a PGP clear-text signature for the given string; returns the clearsigned ASCII-armored text.</summary>
        string PgpClearsignText(string input, byte[] privateKey, string passphrase);

        /// <summary>Creates a PGP clear-text signature for the given string with a <see cref="SecureString"/> passphrase.</summary>
        string PgpClearsignText(string input, byte[] privateKey, SecureString passphrase);


        /// <summary>Creates a PGP clear-text signature of a file and writes the result to the destination path.</summary>
        void PgpClearsignFile(string inputFilePath, string outputFilePath, byte[] privateKey, string passphrase, bool overwrite = false);

        /// <summary>Creates a PGP clear-text signature of a file using a <see cref="SecureString"/> passphrase and writes the result to the destination path.</summary>
        void PgpClearsignFile(string inputFilePath, string outputFilePath, byte[] privateKey, SecureString passphrase, bool overwrite = false);


        // ── PGP verify (binary signature) ─────────────────────────────────────

        /// <summary>Verifies a PGP binary signature against the provided public key.</summary>
        bool PgpVerifyBytes(byte[] inputBytes, byte[] publicKey);


        /// <summary>Verifies a PGP ASCII-armored signed string against the provided public key.</summary>
        bool PgpVerifyText(string input, byte[] publicKey);


        /// <summary>Verifies a PGP-signed file against the provided public key.</summary>
        bool PgpVerifyFile(string inputFilePath, byte[] publicKey);


        // ── PGP verify (clearsignature) ───────────────────────────────────────

        /// <summary>Verifies a PGP clear-text signature against the provided public key.</summary>
        bool PgpVerifyClearBytes(byte[] inputBytes, byte[] publicKey);


        /// <summary>Verifies a PGP clearsigned ASCII-armored string against the provided public key.</summary>
        bool PgpVerifyClearText(string input, byte[] publicKey);


        /// <summary>Verifies a PGP clearsigned file against the provided public key.</summary>
        bool PgpVerifyClearFile(string inputFilePath, byte[] publicKey);


        // ── PGP verify (public key well-formedness) ───────────────────────────

        /// <summary>
        /// Verifies that the supplied bytes contain a well-formed OpenPGP public key.
        /// Mirrors the <c>PgpVerify</c> activity's <c>Mode = PublicKey</c>.
        /// </summary>
        bool PgpVerifyPublicKeyBytes(byte[] publicKey);


        /// <summary>Verifies that the supplied ASCII-armored string is a well-formed OpenPGP public key.</summary>
        bool PgpVerifyPublicKeyText(string publicKey);


        /// <summary>Verifies that the file at the supplied path is a well-formed OpenPGP public key.</summary>
        bool PgpVerifyPublicKeyFile(string publicKeyFilePath);


        // ── PGP key pair generation ────────────────────────────────────────────────

        /// <summary>
        /// Generates an OpenPGP RSA key pair and writes the keys to the specified paths.
        /// </summary>
        /// <param name="publicKeyPath">Path where the ASCII-armored public key will be written.</param>
        /// <param name="privateKeyPath">Path where the ASCII-armored private key will be written.</param>
        /// <param name="userId">OpenPGP User ID; conventionally an RFC 2822 mailbox such as <c>Alice Doe &lt;alice@example.com&gt;</c>.</param>
        /// <param name="passphrase">Passphrase that protects the generated private key.</param>
        /// <param name="keySize">RSA key size. Defaults to 4096-bit; 3072 and 2048 are accepted for interop with legacy systems.</param>
        void PgpGenerateKeys(string publicKeyPath, string privateKeyPath, string userId, string passphrase, RsaKeySize keySize = RsaKeySize.Rsa4096);
    }
}
