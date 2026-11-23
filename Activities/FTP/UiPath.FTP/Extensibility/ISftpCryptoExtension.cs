using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace UiPath.FTP.Extensibility
{
    /// <summary>
    /// Adds cryptographic algorithms to SFTP connections that the operating system crypto module cannot provide.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The UiPath FTP activities ship only algorithms backed by the operating system crypto module. Algorithms such as
    /// Ed25519 or the bcrypt key derivation used by encrypted OpenSSH private keys are therefore not available out of the
    /// box. A project that needs them implements the interfaces in this namespace (for example in a coded workflow, using a
    /// cryptography library of its choice) and passes an <see cref="ISftpCryptoExtension"/> to the connection through
    /// <see cref="FtpConfiguration.CryptoExtensions"/>.
    /// </para>
    /// <para>
    /// The implementation, and its compliance with any cryptography policy, is the responsibility of whoever supplies it.
    /// Connections that use an extension are no longer limited to operating system crypto.
    /// </para>
    /// </remarks>
    public interface ISftpCryptoExtension
    {
        /// <summary>Gets a short name used in diagnostics.</summary>
        string Name { get; }

        /// <summary>Registers the algorithms this extension provides.</summary>
        /// <param name="registry">The registry to add the algorithms to.</param>
        void Register(ISftpCryptoRegistry registry);
    }

    /// <summary>Receives the algorithms an <see cref="ISftpCryptoExtension"/> provides.</summary>
    public interface ISftpCryptoRegistry
    {
        /// <summary>
        /// Adds a public key signature algorithm, used to verify the server host key and to sign for public key
        /// authentication (for example <c>ssh-ed25519</c>). It also makes OpenSSH-format private keys of that type loadable.
        /// </summary>
        void AddSignatureAlgorithm(ISftpSignatureAlgorithm algorithm);

        /// <summary>Adds a transport cipher.</summary>
        /// <param name="name">The SSH algorithm name, for example <c>chacha20-poly1305@openssh.com</c>.</param>
        /// <param name="keySizeBits">The key size in bits.</param>
        /// <param name="factory">Creates the cipher from the key and the initialization vector.</param>
        void AddCipher(string name, int keySizeBits, Func<byte[], byte[], ISftpCipher> factory);

        /// <summary>Adds a transport cipher, saying whether it is an authenticated (AEAD) cipher.</summary>
        /// <param name="name">The SSH algorithm name, for example <c>chacha20-poly1305@openssh.com</c>.</param>
        /// <param name="keySizeBits">The key size in bits.</param>
        /// <param name="factory">Creates the cipher from the key and the initialization vector.</param>
        /// <param name="isAead">Whether the cipher authenticates the packet itself (its <see cref="ISftpCipher.TagSize"/> is not 0); the packets then have no separate MAC.</param>
        void AddCipher(string name, int keySizeBits, Func<byte[], byte[], ISftpCipher> factory, bool isAead);

        /// <summary>Adds a message authentication code.</summary>
        /// <param name="name">The SSH algorithm name, for example <c>hmac-md5</c>.</param>
        /// <param name="keySizeBits">The key size in bits (the digest size for the standard HMACs).</param>
        /// <param name="factory">Creates the HMAC from the key.</param>
        void AddMac(string name, int keySizeBits, Func<byte[], HMAC> factory);

        /// <summary>Adds a message authentication code, optionally of the encrypt-then-MAC kind.</summary>
        /// <param name="name">The SSH algorithm name, for example <c>hmac-md5-etm@openssh.com</c>.</param>
        /// <param name="keySizeBits">The key size in bits (the digest size for the standard HMACs).</param>
        /// <param name="factory">Creates the HMAC from the key. A truncated MAC (for example <c>hmac-sha1-96</c>) returns an HMAC whose output is the truncated value.</param>
        /// <param name="isEncryptThenMac"><see langword="true"/> for the <c>-etm@openssh.com</c> variants.</param>
        void AddMac(string name, int keySizeBits, Func<byte[], HMAC> factory, bool isEncryptThenMac);

        /// <summary>
        /// Adds a host key algorithm whose public key has several values, such as <c>ssh-dss</c> (p, q, g, y). It verifies
        /// the server's signature only; it cannot be used for public key authentication.
        /// </summary>
        void AddHostKeyAlgorithm(ISftpHostKeyAlgorithm algorithm);

        /// <summary>Sets the bcrypt_pbkdf key derivation function used to open encrypted OpenSSH private keys.</summary>
        void SetBcryptPbkdf(ISftpBcryptPbkdf kdf);

        /// <summary>
        /// Adds a key exchange of the "send a value, receive a value" family: fixed group Diffie-Hellman, curve25519 and
        /// post-quantum hybrids such as <c>mlkem768x25519-sha256</c>. The protocol, the exchange hash and the host key check
        /// stay in the library; only the key agreement math is supplied.
        /// </summary>
        /// <param name="name">The SSH algorithm name, for example <c>curve25519-sha256</c>.</param>
        /// <param name="hashAlgorithm">The hash algorithm of the exchange hash: SHA-1, SHA-256, SHA-384 or SHA-512.</param>
        /// <param name="messageType">The messages the algorithm uses.</param>
        /// <param name="factory">Creates the key agreement for one key exchange.</param>
        /// <exception cref="ArgumentException"><paramref name="hashAlgorithm"/> is not supported.</exception>
        void AddKeyExchange(string name, HashAlgorithmName hashAlgorithm, SftpKeyExchangeMessageType messageType, Func<ISftpKeyAgreement> factory);

        /// <summary>
        /// Adds a Diffie-Hellman group exchange (RFC 4419, for example <c>diffie-hellman-group-exchange-sha256</c>).
        /// </summary>
        /// <param name="name">The SSH algorithm name.</param>
        /// <param name="hashAlgorithm">The hash algorithm of the exchange hash: SHA-1, SHA-256, SHA-384 or SHA-512.</param>
        /// <param name="factory">
        /// Creates the key agreement for the group the server sends. The arguments are the safe prime <c>p</c> and the
        /// generator <c>g</c>, as the content of their mpint encoding (big-endian, with a leading zero byte if needed).
        /// </param>
        /// <exception cref="ArgumentException"><paramref name="hashAlgorithm"/> is not supported.</exception>
        void AddDiffieHellmanGroupExchange(string name, HashAlgorithmName hashAlgorithm, Func<byte[], byte[], ISftpKeyAgreement> factory);
    }

    /// <summary>The messages a key exchange added with <see cref="ISftpCryptoRegistry.AddKeyExchange"/> uses.</summary>
    public enum SftpKeyExchangeMessageType
    {
        /// <summary><c>SSH_MSG_KEXDH_INIT</c> / <c>SSH_MSG_KEXDH_REPLY</c> (RFC 4253): fixed group Diffie-Hellman.</summary>
        DiffieHellman,

        /// <summary><c>SSH_MSG_KEX_ECDH_INIT</c> / <c>SSH_MSG_KEX_ECDH_REPLY</c> (RFC 5656, RFC 8731): for example curve25519-sha256.</summary>
        Ecdh,

        /// <summary><c>SSH_MSG_KEX_HYBRID_INIT</c> / <c>SSH_MSG_KEX_HYBRID_REPLY</c>: for example mlkem768x25519-sha256 and sntrup761x25519-sha512.</summary>
        Hybrid,
    }

    /// <summary>
    /// The key agreement math of one key exchange: a Diffie-Hellman group, X25519, a hybrid of a KEM and X25519, and so on.
    /// An instance serves one key exchange: <see cref="GenerateClientValue"/> is called once, then
    /// <see cref="ComputeSharedKey"/> once. The implementation owns the wire encoding of the exchange values and of the
    /// shared key.
    /// </summary>
    public interface ISftpKeyAgreement : IDisposable
    {
        /// <summary>
        /// Creates the client's key pair (or key encapsulation public key) and returns the value to send to the server,
        /// as it appears on the wire: the mpint encoding of <c>e</c> for Diffie-Hellman (see
        /// <see cref="SftpKeyAgreementEncoding.ToMPInt"/>), the raw public key for ECDH style exchanges (32 bytes for
        /// X25519), the concatenation the algorithm defines for hybrids.
        /// </summary>
        byte[] GenerateClientValue();

        /// <summary>
        /// Computes the shared key from the value received from the server (for Diffie-Hellman the mpint encoding of
        /// <c>f</c>) and returns it exactly as it is hashed into the exchange hash: the mpint encoding of the shared secret
        /// for Diffie-Hellman and ECDH style exchanges, the string value the algorithm defines for hybrids.
        /// </summary>
        /// <exception cref="Exception">The value is not valid; the connection fails with a key exchange error.</exception>
        byte[] ComputeSharedKey(byte[] serverValue);
    }

    /// <summary>Helpers for implementations of <see cref="ISftpKeyAgreement"/>.</summary>
    public static class SftpKeyAgreementEncoding
    {
        /// <summary>
        /// Encodes an unsigned big-endian integer as the content of an SSH <c>mpint</c> (without the length prefix):
        /// leading zero bytes removed, and a zero byte added in front if the high bit of the first byte is set.
        /// </summary>
        public static byte[] ToMPInt(ReadOnlySpan<byte> unsignedBigEndian) =>
            Renci.SshNet.Security.KeyAgreementEncoding.ToMPInt(unsignedBigEndian);
    }

    /// <summary>A public key signature algorithm, for example Ed25519.</summary>
    public interface ISftpSignatureAlgorithm
    {
        /// <summary>Gets the SSH public key algorithm name, for example <c>ssh-ed25519</c>.</summary>
        string Name { get; }

        /// <summary>Gets the length in bytes of the raw public key (32 for Ed25519).</summary>
        int PublicKeyLength { get; }

        /// <summary>Verifies a signature, used to check the server host key.</summary>
        /// <param name="publicKey">The raw public key.</param>
        /// <param name="data">The signed data.</param>
        /// <param name="signature">The raw signature.</param>
        /// <returns><see langword="true"/> when the signature is valid.</returns>
        bool Verify(byte[] publicKey, byte[] data, byte[] signature);

        /// <summary>Signs data, used for public key authentication.</summary>
        /// <param name="privateKey">The private key bytes as stored in the OpenSSH key file (for Ed25519: seed followed by public key).</param>
        /// <param name="data">The data to sign.</param>
        /// <returns>The raw signature.</returns>
        byte[] Sign(byte[] privateKey, byte[] data);
    }

    /// <summary>A host key algorithm whose public key consists of several values, for example <c>ssh-dss</c>.</summary>
    public interface ISftpHostKeyAlgorithm
    {
        /// <summary>Gets the SSH public key algorithm name, for example <c>ssh-dss</c>.</summary>
        string Name { get; }

        /// <summary>Verifies a signature of the server host key.</summary>
        /// <param name="publicKeyValues">The values of the public key in the order of the key blob (for ssh-dss p, q, g, y), each as unsigned big-endian bytes without leading zeros.</param>
        /// <param name="data">The signed data.</param>
        /// <param name="signature">The raw signature (for ssh-dss 40 bytes: r then s).</param>
        /// <returns><see langword="true"/> when the signature is valid.</returns>
        bool Verify(IReadOnlyList<byte[]> publicKeyValues, byte[] data, byte[] signature);
    }

    /// <summary>A transport cipher instance (one direction of one connection).</summary>
    public interface ISftpCipher
    {
        /// <summary>Gets the block size in bytes (8 for 64-bit block ciphers, 16 for AES).</summary>
        int BlockSize { get; }

        /// <summary>Gets the authentication tag size in bytes, or 0 when the cipher is not authenticated.</summary>
        int TagSize { get; }

        /// <summary>Encrypts a region of the input.</summary>
        /// <param name="input">The input buffer.</param>
        /// <param name="offset">The offset of the data in <paramref name="input"/>.</param>
        /// <param name="length">The number of bytes to encrypt.</param>
        /// <returns>The encrypted data.</returns>
        byte[] Encrypt(byte[] input, int offset, int length);

        /// <summary>Decrypts a region of the input.</summary>
        /// <param name="input">The input buffer.</param>
        /// <param name="offset">The offset of the data in <paramref name="input"/>.</param>
        /// <param name="length">The number of bytes to decrypt.</param>
        /// <returns>The decrypted data.</returns>
        byte[] Decrypt(byte[] input, int offset, int length);
    }

    /// <summary>The bcrypt_pbkdf key derivation function of OpenSSH.</summary>
    public interface ISftpBcryptPbkdf
    {
        /// <summary>Derives key material from a passphrase.</summary>
        /// <param name="passphrase">The passphrase, UTF-8 encoded.</param>
        /// <param name="salt">The salt from the key file.</param>
        /// <param name="rounds">The number of rounds from the key file.</param>
        /// <param name="outputLength">The number of bytes to derive.</param>
        /// <returns>The derived bytes.</returns>
        byte[] DeriveKey(byte[] passphrase, byte[] salt, int rounds, int outputLength);
    }
}
