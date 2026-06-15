using System;
using System.IO;
using System.Security;
using System.Text;
using Shouldly;
using UiPath.Cryptography.Activities.API;
using UiPath.Cryptography.Enums;
using Xunit;

namespace UiPath.Cryptography.Activities.API.Tests
{
    /// <summary>
    /// xUnit class fixture: generates a single PGP key pair (RSA-2048 for test speed) once
    /// per test class and exposes the keys + their armored bytes so PGP roundtrip tests don't
    /// pay the multi-second key-generation cost per case.
    /// </summary>
    public sealed class PgpKeyFixture : IDisposable
    {
        public const string Passphrase = "fixture-pass";

        public PgpKeyPair KeyPair { get; }
        public PgpPublicKey PublicKey => KeyPair.PublicKey;
        public PgpPrivateKey PrivateKey => KeyPair.PrivateKey;
#pragma warning disable CA1819 // Test fixture: returning byte[] directly is intentional — tests feed these straight into File.WriteAllBytes / PgpPublicKey.FromBytes.
        public byte[] PublicKeyBytes { get; }
        public byte[] PrivateKeyBytes { get; }
#pragma warning restore CA1819
        public string PublicKeyArmored { get; }
        public string PublicKeyPath { get; }

        public PgpKeyFixture()
        {
            var service = new CryptographyService();
            KeyPair = service.PgpGenerateKeys("Service Tester <svc@test.com>", Passphrase, RsaKeySize.Rsa2048);
            PublicKeyBytes = PublicKey.ToBytes();
            PrivateKeyBytes = PrivateKey.ToBytes();
            PublicKeyArmored = Encoding.UTF8.GetString(PublicKeyBytes);
            PublicKeyPath = Path.Combine(Path.GetTempPath(), $"cryptosvc_pub_{Guid.NewGuid():N}.asc");
            PublicKey.Save(PublicKeyPath, overwrite: true);
        }

        public void Dispose()
        {
            PrivateKey.Dispose();
            if (File.Exists(PublicKeyPath)) File.Delete(PublicKeyPath);
        }
    }

#pragma warning disable CS0618 // Tests intentionally exercise the obsolete AES enum because that's the algorithm the service ships.
    public class CryptographyServiceTests : IClassFixture<PgpKeyFixture>
    {
        private readonly CryptographyService _service = new CryptographyService();
        private readonly PgpKeyFixture _keys;

        public CryptographyServiceTests(PgpKeyFixture keys)
        {
            _keys = keys;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Symmetric Encrypt / Decrypt — Bytes form, PasswordKey + RawKey
        // ═══════════════════════════════════════════════════════════════════════

        public static TheoryData<EncryptionAlgorithm, string> AlgorithmsForPasswordKey { get; } = new()
        {
            { EncryptionAlgorithm.AES, "string" },
            { EncryptionAlgorithm.AES, "secure" },
            { EncryptionAlgorithm.AESGCM, "string" },
            { EncryptionAlgorithm.AESGCM, "secure" },
        };

        [Theory]
        [MemberData(nameof(AlgorithmsForPasswordKey))]
        public void Encrypt_Decrypt_PasswordKey_RoundTrip(EncryptionAlgorithm algorithm, string keyKind)
        {
            byte[] plain = Encoding.UTF8.GetBytes("Hello bytes form!");
            PasswordKey key = NewPasswordKey("myKey", keyKind);

            // Encrypt twice — pins that:
            //   (a) cipher != plain (encryption actually happened, not a no-op)
            //   (b) cipher1 != cipher2 (fresh salt/IV per call — pins the IV/salt randomness)
            // Without these pins, a no-op `EncryptBytes` that returned its input would still
            // pass every round-trip test in this file.
            byte[] cipher1 = _service.EncryptBytes(plain, algorithm, SymmetricEncryptOptions.Classic(key));
            byte[] cipher2 = _service.EncryptBytes(plain, algorithm, SymmetricEncryptOptions.Classic(key));
            cipher1.ShouldNotBe(plain);
            cipher2.ShouldNotBe(plain);
            cipher1.ShouldNotBe(cipher2);

            byte[] decrypted = _service.DecryptBytes(cipher1, algorithm, SymmetricDecryptOptions.Classic(key));
            decrypted.ShouldBe(plain);
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AES)]
        [InlineData(EncryptionAlgorithm.AESGCM)]
        public void Encrypt_Decrypt_RawKey_Hex_RoundTrip(EncryptionAlgorithm algorithm)
        {
            byte[] plain = Encoding.UTF8.GetBytes("Raw round-trip");
            byte[] rawKey = MakeDeterministicKey(32, offset: 42); // AES-256, deterministic non-RNG seed
            RawKey key = RawKey.FromHex(Convert.ToHexString(rawKey));
            byte[] cipher = _service.EncryptBytes(plain, algorithm, SymmetricEncryptOptions.Raw(key));
            byte[] decrypted = _service.DecryptBytes(cipher, algorithm, SymmetricDecryptOptions.Raw(key));
            decrypted.ShouldBe(plain);
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AES)]
        public void Encrypt_Decrypt_RawKey_Base64_RoundTrip(EncryptionAlgorithm algorithm)
        {
            byte[] plain = Encoding.UTF8.GetBytes("Base64 raw round-trip");
            byte[] rawKey = MakeDeterministicKey(32, offset: 7);
            RawKey key = RawKey.FromBase64(Convert.ToBase64String(rawKey));
            byte[] cipher = _service.EncryptBytes(plain, algorithm, SymmetricEncryptOptions.Raw(key));
            byte[] decrypted = _service.DecryptBytes(cipher, algorithm, SymmetricDecryptOptions.Raw(key));
            decrypted.ShouldBe(plain);
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AES)]
        public void Encrypt_Decrypt_RawKey_Bytes_RoundTrip(EncryptionAlgorithm algorithm)
        {
            byte[] plain = Encoding.UTF8.GetBytes("FromBytes round-trip");
            byte[] rawKey = MakeDeterministicKey(32, offset: 11);
            RawKey key = RawKey.FromBytes(rawKey);
            byte[] cipher = _service.EncryptBytes(plain, algorithm, SymmetricEncryptOptions.Raw(key));
            byte[] decrypted = _service.DecryptBytes(cipher, algorithm, SymmetricDecryptOptions.Raw(key));
            decrypted.ShouldBe(plain);
        }

        [Fact]
        public void Encrypt_Decrypt_RawKey_ExplicitIv_RoundTrip()
        {
            byte[] plain = Encoding.UTF8.GetBytes("Explicit IV roundtrip");
            byte[] rawKey = MakeDeterministicKey(32, offset: 99);
            byte[] iv = MakeDeterministicKey(16, offset: 33);
            RawKey key = RawKey.FromBytes(rawKey);
            byte[] cipher = _service.EncryptBytes(plain, EncryptionAlgorithm.AES, SymmetricEncryptOptions.Raw(key, iv));

            // Raw wire layout: IV ‖ ct [‖ tag]. Pin that the supplied IV is the one written —
            // without this, a regression that ignored `options.IV` and generated a fresh random
            // IV would still round-trip (decrypt reads IV from the stream prefix).
            cipher.Length.ShouldBeGreaterThanOrEqualTo(iv.Length);
            cipher.AsSpan(0, iv.Length).ToArray().ShouldBe(iv);

            byte[] decrypted = _service.DecryptBytes(cipher, EncryptionAlgorithm.AES, SymmetricDecryptOptions.Raw(key));
            decrypted.ShouldBe(plain);
        }

        // Reusing a SymmetricEncryptOptions across calls must not be silently affected by the
        // caller mutating its original IV buffer. Mirrors the RawKey.FromBytes defensive-copy
        // contract (RawKeyTests.FromBytes_DefensiveCopy_CallerMutationDoesNotAffectInstance).
        [Fact]
        public void SymmetricEncryptOptions_Raw_DefensiveCopiesIv_CallerMutationDoesNotAffectEncryption()
        {
            byte[] sourceIv = MakeDeterministicKey(16, offset: 71);
            byte[] rawKey = MakeDeterministicKey(32, offset: 7);
            RawKey key = RawKey.FromBytes(rawKey);

            SymmetricEncryptOptions options = SymmetricEncryptOptions.Raw(key, sourceIv);
            byte[] cipher1 = _service.EncryptBytes(Encoding.UTF8.GetBytes("payload"), EncryptionAlgorithm.AES, options);

            // Mutating the caller's buffer after the options object is constructed must NOT
            // change the IV used by subsequent encryptions reusing the same options — the factory
            // defensively copies. If the factory ever regresses to ref-storage, this clear would
            // zero the stored IV and the next cipher would differ.
            Array.Clear(sourceIv, 0, sourceIv.Length);

            byte[] cipher2 = _service.EncryptBytes(Encoding.UTF8.GetBytes("payload"), EncryptionAlgorithm.AES, options);

            cipher2.ShouldBe(cipher1);
        }

        [Fact]
        public void Encrypt_Decrypt_Owasp2026_RoundTrip()
        {
            byte[] plain = Encoding.UTF8.GetBytes("OWASP roundtrip");
            PasswordKey key = PasswordKey.FromPassword("myKey", Encoding.UTF8);
            byte[] cipher = _service.EncryptBytes(plain, EncryptionAlgorithm.AES, SymmetricEncryptOptions.Owasp2026(key));
            byte[] decrypted = _service.DecryptBytes(cipher, EncryptionAlgorithm.AES, SymmetricDecryptOptions.Owasp2026(key));
            decrypted.ShouldBe(plain);
        }

        [Fact]
        public void Encrypt_Decrypt_Owasp2026_ExplicitKdfIterations_RoundTrip()
        {
            byte[] plain = Encoding.UTF8.GetBytes("OWASP custom iters");
            PasswordKey key = PasswordKey.FromPassword("myKey", Encoding.UTF8);
            byte[] cipher = _service.EncryptBytes(plain, EncryptionAlgorithm.AES, SymmetricEncryptOptions.Owasp2026(key, kdfIterations: 50_000));
            byte[] decrypted = _service.DecryptBytes(cipher, EncryptionAlgorithm.AES, SymmetricDecryptOptions.Owasp2026(key, kdfIterations: 50_000));
            decrypted.ShouldBe(plain);
        }

        [Fact]
        public void Encrypt_Decrypt_OpenSslEnc_RoundTrip()
        {
            byte[] plain = Encoding.UTF8.GetBytes("openssl roundtrip");
            PasswordKey key = PasswordKey.FromPassword("myKey", Encoding.UTF8);
            byte[] cipher = _service.EncryptBytes(plain, EncryptionAlgorithm.AES, SymmetricEncryptOptions.OpenSslEnc(key));
            byte[] decrypted = _service.DecryptBytes(cipher, EncryptionAlgorithm.AES, SymmetricDecryptOptions.OpenSslEnc(key));
            decrypted.ShouldBe(plain);
        }

        // Classic and Owasp2026 share the same wire layout (salt ‖ IV ‖ ct, PBKDF2-HMAC-SHA1).
        // Only the iter source differs — Classic is frozen at 10 000, Owasp2026 is caller-controlled.
        // Encrypting with Owasp2026(10_000) must produce a blob the Classic decrypt can read,
        // and vice versa — proves the dispatch routes through compatible code paths.
        [Fact]
        public void Encrypt_Owasp2026_10000Iter_DecryptableBy_Classic()
        {
            byte[] plain = Encoding.UTF8.GetBytes("cross-format wire compat");
            PasswordKey key = PasswordKey.FromPassword("crossKey", Encoding.UTF8);
            byte[] cipher = _service.EncryptBytes(plain, EncryptionAlgorithm.AES, SymmetricEncryptOptions.Owasp2026(key, kdfIterations: 10_000));
            byte[] decrypted = _service.DecryptBytes(cipher, EncryptionAlgorithm.AES, SymmetricDecryptOptions.Classic(key));
            decrypted.ShouldBe(plain);
        }

        [Fact]
        public void Encrypt_Classic_DecryptableBy_Owasp2026_10000Iter()
        {
            byte[] plain = Encoding.UTF8.GetBytes("cross-format wire compat reverse");
            PasswordKey key = PasswordKey.FromPassword("crossKey", Encoding.UTF8);
            byte[] cipher = _service.EncryptBytes(plain, EncryptionAlgorithm.AES, SymmetricEncryptOptions.Classic(key));
            byte[] decrypted = _service.DecryptBytes(cipher, EncryptionAlgorithm.AES, SymmetricDecryptOptions.Owasp2026(key, kdfIterations: 10_000));
            decrypted.ShouldBe(plain);
        }

        // The iteration count is NOT carried in the ciphertext — encrypt and decrypt must use
        // matching values. AEAD makes this deterministic: wrong key (from wrong iter) fails the
        // tag check rather than silently producing garbage. Documents the cross-version-decrypt
        // warning in docs/symmetric-wire-format.md.
        [Theory]
        [InlineData(EncryptionAlgorithm.AESGCM)]
        [InlineData(EncryptionAlgorithm.ChaCha20Poly1305)]
        public void Decrypt_Owasp2026_MismatchedKdfIterations_Throws(EncryptionAlgorithm algorithm)
        {
            byte[] plain = Encoding.UTF8.GetBytes("iter mismatch");
            PasswordKey key = PasswordKey.FromPassword("myKey", Encoding.UTF8);
            byte[] cipher = _service.EncryptBytes(plain, algorithm, SymmetricEncryptOptions.Owasp2026(key, kdfIterations: 50_000));
            Should.Throw<System.Security.Cryptography.CryptographicException>(
                () => _service.DecryptBytes(cipher, algorithm, SymmetricDecryptOptions.Owasp2026(key, kdfIterations: 60_000)));
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AESGCM)]
        [InlineData(EncryptionAlgorithm.ChaCha20Poly1305)]
        public void Decrypt_OpenSslEnc_MismatchedKdfIterations_Throws(EncryptionAlgorithm algorithm)
        {
            byte[] plain = Encoding.UTF8.GetBytes("openssl iter mismatch");
            PasswordKey key = PasswordKey.FromPassword("myKey", Encoding.UTF8);
            byte[] cipher = _service.EncryptBytes(plain, algorithm, SymmetricEncryptOptions.OpenSslEnc(key, kdfIterations: 50_000));
            Should.Throw<System.Security.Cryptography.CryptographicException>(
                () => _service.DecryptBytes(cipher, algorithm, SymmetricDecryptOptions.OpenSslEnc(key, kdfIterations: 60_000)));
        }

        // AEAD ciphertext carries an authentication tag covering salt + IV + ct. Flipping any
        // bit must surface as a CryptographicException rather than silently returning garbage.
        [Theory]
        [InlineData(EncryptionAlgorithm.AESGCM)]
        [InlineData(EncryptionAlgorithm.ChaCha20Poly1305)]
        public void Decrypt_AeadTamperedCiphertext_Throws(EncryptionAlgorithm algorithm)
        {
            byte[] plain = Encoding.UTF8.GetBytes("aead tamper test — must fail");
            PasswordKey key = PasswordKey.FromPassword("aeadKey", Encoding.UTF8);
            byte[] cipher = _service.EncryptBytes(plain, algorithm, SymmetricEncryptOptions.Classic(key));
            cipher[cipher.Length / 2] ^= 0x01; // flip a bit mid-stream
            Should.Throw<System.Security.Cryptography.CryptographicException>(
                () => _service.DecryptBytes(cipher, algorithm, SymmetricDecryptOptions.Classic(key)));
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AESGCM)]
        [InlineData(EncryptionAlgorithm.ChaCha20Poly1305)]
        public void Decrypt_AeadTamperedTag_Throws(EncryptionAlgorithm algorithm)
        {
            byte[] plain = Encoding.UTF8.GetBytes("aead tag tamper");
            PasswordKey key = PasswordKey.FromPassword("aeadKey", Encoding.UTF8);
            byte[] cipher = _service.EncryptBytes(plain, algorithm, SymmetricEncryptOptions.Classic(key));

            // Classic AEAD wire layout: salt(8) ‖ IV(12) ‖ ct ‖ tag(16). Pin the assumption
            // that the last byte is part of the tag — without this, a future wire-layout tweak
            // (or a degenerate short ciphertext) could silently move the flip target out of
            // the tag region and the test would pass for an unrelated reason.
            const int salt = 8, iv = 12, tag = 16;
            cipher.Length.ShouldBeGreaterThanOrEqualTo(salt + iv + plain.Length + tag);

            cipher[^1] ^= 0x01; // flip a bit in the trailing auth tag
            Should.Throw<System.Security.Cryptography.CryptographicException>(
                () => _service.DecryptBytes(cipher, algorithm, SymmetricDecryptOptions.Classic(key)));
        }

        [Fact]
        public void Encrypt_NullInputBytes_Throws()
        {
            PasswordKey key = PasswordKey.FromPassword("k", Encoding.UTF8);
            Should.Throw<ArgumentNullException>(() => _service.EncryptBytes(null, EncryptionAlgorithm.AES, SymmetricEncryptOptions.Classic(key)));
        }

        [Fact]
        public void Decrypt_NullInputBytes_Throws()
        {
            PasswordKey key = PasswordKey.FromPassword("k", Encoding.UTF8);
            Should.Throw<ArgumentNullException>(() => _service.DecryptBytes(null, EncryptionAlgorithm.AES, SymmetricDecryptOptions.Classic(key)));
        }

        [Fact]
        public void Encrypt_NullOptions_Throws()
        {
            Should.Throw<ArgumentNullException>(() => _service.EncryptBytes(new byte[] { 1 }, EncryptionAlgorithm.AES, (SymmetricEncryptOptions)null));
        }

        [Fact]
        public void Decrypt_NullOptions_Throws()
        {
            Should.Throw<ArgumentNullException>(() => _service.DecryptBytes(new byte[] { 1 }, EncryptionAlgorithm.AES, (SymmetricDecryptOptions)null));
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AESGCM)]
        [InlineData(EncryptionAlgorithm.ChaCha20Poly1305)]
        public void DecryptBytes_AeadShortInput_Throws(EncryptionAlgorithm algorithm)
        {
            byte[] shortInput = new byte[4];
            PasswordKey key = PasswordKey.FromPassword("anyKey", Encoding.UTF8);
            Should.Throw<System.Security.Cryptography.CryptographicException>(
                () => _service.DecryptBytes(shortInput, algorithm, SymmetricDecryptOptions.Classic(key)));
        }

        // Note: the runtime cases `Encrypt_RawFormat_WithPasswordKey_Throws` and
        // `Encrypt_NonRawFormat_WithRawKey_Throws` from the previous design are now
        // unreachable — the (key kind × wire format) pairing is enforced at compile time by
        // the typed factory parameters on SymmetricEncryptOptions / SymmetricDecryptOptions
        // (e.g. `Classic(PasswordKey)`, `Raw(RawKey)`). The runtime validator in
        // SymmetricInteropHelper still runs as defence-in-depth but is no longer reachable
        // from valid C# call sites.

        [Fact]
        public void Encrypt_Owasp2026_KdfIterationsBelowFloor_Throws()
        {
            PasswordKey key = PasswordKey.FromPassword("k", Encoding.UTF8);
            Should.Throw<ArgumentException>(() =>
                _service.EncryptBytes(new byte[] { 1 }, EncryptionAlgorithm.AES, SymmetricEncryptOptions.Owasp2026(key, kdfIterations: 500)));
        }

        [Fact]
        public void Encrypt_Raw_WrongKeyLength_Throws()
        {
            RawKey shortKey = RawKey.FromBytes(new byte[7]); // not a legal AES key size
            Should.Throw<ArgumentException>(() =>
                _service.EncryptBytes(new byte[] { 1 }, EncryptionAlgorithm.AES, SymmetricEncryptOptions.Raw(shortKey)));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Symmetric Encrypt / Decrypt — Text form
        // ═══════════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData(EncryptionAlgorithm.AES)]
        [InlineData(EncryptionAlgorithm.TripleDES)]
        public void EncryptText_ThenDecryptText_RoundTrip(EncryptionAlgorithm algorithm)
        {
            string original = "Hello, coded workflows!";
            PasswordKey key = PasswordKey.FromPassword("mySecretKey", Encoding.UTF8);
            string encrypted = _service.EncryptText(original, algorithm, SymmetricEncryptOptions.Classic(key));
            string decrypted = _service.DecryptText(encrypted, algorithm, SymmetricDecryptOptions.Classic(key));
            decrypted.ShouldBe(original);
        }

        [Fact]
        public void EncryptText_NullInput_Throws()
        {
            PasswordKey key = PasswordKey.FromPassword("key", Encoding.UTF8);
            Should.Throw<ArgumentNullException>(() => _service.EncryptText(null, EncryptionAlgorithm.AES, SymmetricEncryptOptions.Classic(key)));
        }

        [Fact]
        public void DecryptText_NullInput_Throws()
        {
            PasswordKey key = PasswordKey.FromPassword("key", Encoding.UTF8);
            Should.Throw<ArgumentNullException>(() => _service.DecryptText(null, EncryptionAlgorithm.AES, SymmetricDecryptOptions.Classic(key)));
        }

        // Non-UTF-8 encoding flows through the options object end-to-end. Use UTF-16LE so the
        // input characters (including em-dash and Latin accents) all have a faithful representation
        // and the ciphertext bytes are structurally different from the UTF-8 path.
        [Fact]
        public void EncryptText_DecryptText_NonUtf8Encoding_RoundTrip()
        {
            string original = "café — éàü";
            PasswordKey key = PasswordKey.FromPassword("mySecretKey", Encoding.UTF8);
            Encoding utf16 = Encoding.Unicode;

            string utf16Cipher = _service.EncryptText(original, EncryptionAlgorithm.AES, SymmetricEncryptOptions.Classic(key, utf16));
            string utf16Decrypted = _service.DecryptText(utf16Cipher, EncryptionAlgorithm.AES, SymmetricDecryptOptions.Classic(key, utf16));
            utf16Decrypted.ShouldBe(original);

            // Decrypting UTF-16 ciphertext as UTF-8 produces garbage (or mojibake) — proving the
            // option actually drives the decode side, not a hidden UTF-8 default.
            string utf8Decrypted = _service.DecryptText(utf16Cipher, EncryptionAlgorithm.AES, SymmetricDecryptOptions.Classic(key));
            utf8Decrypted.ShouldNotBe(original);
        }

        [Fact]
        public void EncryptText_NullOptions_Throws()
        {
            Should.Throw<ArgumentNullException>(() => _service.EncryptText("hi", EncryptionAlgorithm.AES, options: null));
        }

        [Fact]
        public void DecryptText_NullOptions_Throws()
        {
            Should.Throw<ArgumentNullException>(() => _service.DecryptText("AAAA", EncryptionAlgorithm.AES, options: null));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Symmetric Encrypt / Decrypt — File form
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public void EncryptFile_NullInputPath_Throws()
        {
            PasswordKey key = PasswordKey.FromPassword("key", Encoding.UTF8);
            Should.Throw<ArgumentException>(() =>
                _service.EncryptFile(null, "out.bin", EncryptionAlgorithm.AES, SymmetricEncryptOptions.Classic(key), overwrite: true));
        }

        [Fact]
        public void DecryptFile_NullInputPath_Throws()
        {
            PasswordKey key = PasswordKey.FromPassword("key", Encoding.UTF8);
            Should.Throw<ArgumentException>(() =>
                _service.DecryptFile(null, "out.txt", EncryptionAlgorithm.AES, SymmetricDecryptOptions.Classic(key), overwrite: true));
        }

        [Fact]
        public void EncryptFile_ExistingOutputWithoutOverwrite_Throws()
        {
            string inputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string outputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(inputPath, "test content");
                File.WriteAllText(outputPath, "existing output");
                PasswordKey key = PasswordKey.FromPassword("key", Encoding.UTF8);
                Should.Throw<InvalidOperationException>(() =>
                    _service.EncryptFile(inputPath, outputPath, EncryptionAlgorithm.AES, SymmetricEncryptOptions.Classic(key), overwrite: false));
            }
            finally
            {
                File.Delete(inputPath);
                File.Delete(outputPath);
            }
        }

        [Theory]
        [InlineData("string")]
        [InlineData("secure")]
        public void EncryptFile_ThenDecryptFile_RoundTrip(string keyKind)
        {
            string original = "File round-trip — " + keyKind;
            string inputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string encryptedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string decryptedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(inputPath, original, Encoding.UTF8);
                PasswordKey key = NewPasswordKey("testKey", keyKind);
                _service.EncryptFile(inputPath, encryptedPath, EncryptionAlgorithm.AES, SymmetricEncryptOptions.Classic(key), overwrite: true);
                _service.DecryptFile(encryptedPath, decryptedPath, EncryptionAlgorithm.AES, SymmetricDecryptOptions.Classic(key), overwrite: true);
                File.ReadAllText(decryptedPath, Encoding.UTF8).ShouldBe(original);
            }
            finally
            {
                if (File.Exists(inputPath)) File.Delete(inputPath);
                if (File.Exists(encryptedPath)) File.Delete(encryptedPath);
                if (File.Exists(decryptedPath)) File.Delete(decryptedPath);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Keyed Hash — Bytes / Text / File (takes CryptoKey directly, no options)
        // ═══════════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("string")]
        [InlineData("secure")]
        public void KeyedHashBytes_DeterministicAndHex(string keyKind)
        {
            byte[] input = Encoding.UTF8.GetBytes("hello");
            CryptoKey k1 = NewPasswordKey("key", keyKind);
            CryptoKey k2 = NewPasswordKey("key", keyKind);
            string hash1 = _service.KeyedHashBytes(input, KeyedHashAlgorithms.HMACSHA256, k1);
            string hash2 = _service.KeyedHashBytes(input, KeyedHashAlgorithms.HMACSHA256, k2);
            hash1.ShouldBe(hash2);
            hash1.ShouldMatch("^[0-9A-F]+$");
        }

        [Fact]
        public void KeyedHashBytes_SecureMatchesString()
        {
            byte[] input = Encoding.UTF8.GetBytes("hello");
            string fromString = _service.KeyedHashBytes(input, KeyedHashAlgorithms.HMACSHA256, PasswordKey.FromPassword("k", Encoding.UTF8));
            string fromSecure = _service.KeyedHashBytes(input, KeyedHashAlgorithms.HMACSHA256, PasswordKey.FromPassword(ToSecureString("k"), Encoding.UTF8));
            fromSecure.ShouldBe(fromString);
        }

        [Fact]
        public void KeyedHashBytes_Guards()
        {
            CryptoKey key = PasswordKey.FromPassword("k", Encoding.UTF8);
            Should.Throw<ArgumentNullException>(() => _service.KeyedHashBytes(null, KeyedHashAlgorithms.HMACSHA256, key));
            Should.Throw<ArgumentNullException>(() => _service.KeyedHashBytes(new byte[] { 1 }, KeyedHashAlgorithms.HMACSHA256, null));
        }

        [Fact]
        public void KeyedHashText_Guards()
        {
            CryptoKey key = PasswordKey.FromPassword("k", Encoding.UTF8);
            Should.Throw<ArgumentNullException>(() => _service.KeyedHashText(null, KeyedHashAlgorithms.HMACSHA256, key));
            Should.Throw<ArgumentNullException>(() => _service.KeyedHashText("x", KeyedHashAlgorithms.HMACSHA256, null));
        }

        [Fact]
        public void KeyedHashText_DifferentKeys_DifferentHash()
        {
            string h1 = _service.KeyedHashText("hello", KeyedHashAlgorithms.HMACSHA256, PasswordKey.FromPassword("k1", Encoding.UTF8));
            string h2 = _service.KeyedHashText("hello", KeyedHashAlgorithms.HMACSHA256, PasswordKey.FromPassword("k2", Encoding.UTF8));
            h1.ShouldNotBe(h2);
        }

        [Fact]
        public void KeyedHashFile_RoundTrip()
        {
            string filePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllBytes(filePath, Encoding.UTF8.GetBytes("hello"));
                CryptoKey key = PasswordKey.FromPassword("key", Encoding.UTF8);
                string hashFromFile = _service.KeyedHashFile(filePath, KeyedHashAlgorithms.HMACSHA256, key);
                string hashFromText = _service.KeyedHashText("hello", KeyedHashAlgorithms.HMACSHA256, key);
                hashFromFile.ShouldBe(hashFromText);
            }
            finally
            {
                if (File.Exists(filePath)) File.Delete(filePath);
            }
        }

        [Fact]
        public void KeyedHashFile_Guards()
        {
            CryptoKey key = PasswordKey.FromPassword("k", Encoding.UTF8);
            Should.Throw<ArgumentException>(() => _service.KeyedHashFile(null, KeyedHashAlgorithms.HMACSHA256, key));
            Should.Throw<ArgumentNullException>(() => _service.KeyedHashFile("file.txt", KeyedHashAlgorithms.HMACSHA256, null));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PGP Encrypt / Decrypt — Bytes / Text / File
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public void PgpEncrypt_Decrypt_Bytes_RoundTrip()
        {
            byte[] plain = Encoding.UTF8.GetBytes("PGP bytes round-trip");
            byte[] cipher = _service.PgpEncryptBytes(plain, _keys.PublicKey);
            byte[] decrypted = _service.PgpDecryptBytes(cipher, _keys.PrivateKey);
            decrypted.ShouldBe(plain);
        }

        [Fact]
        public void PgpEncrypt_Decrypt_Bytes_SignedAndVerified_RoundTrip()
        {
            byte[] plain = Encoding.UTF8.GetBytes("Signed PGP roundtrip");
            byte[] cipher = _service.PgpEncryptBytes(plain, _keys.PublicKey, signer: _keys.PrivateKey);
            byte[] decrypted = _service.PgpDecryptBytes(cipher, _keys.PrivateKey, verifier: _keys.PublicKey);
            decrypted.ShouldBe(plain);
        }

        [Fact]
        public void PgpEncrypt_Decrypt_Text_RoundTrip()
        {
            const string plain = "PGP text round-trip";
            string cipher = _service.PgpEncryptText(plain, _keys.PublicKey);
            cipher.ShouldStartWith("-----BEGIN PGP MESSAGE-----");
            string decrypted = _service.PgpDecryptText(cipher, _keys.PrivateKey);
            decrypted.ShouldBe(plain);
        }

        [Fact]
        public void PgpEncryptFile_DecryptFile_RoundTrip()
        {
            const string original = "PGP file round-trip";
            string inputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string encryptedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string decryptedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(inputPath, original, Encoding.UTF8);
                _service.PgpEncryptFile(inputPath, encryptedPath, _keys.PublicKey, overwrite: true);
                _service.PgpDecryptFile(encryptedPath, decryptedPath, _keys.PrivateKey, overwrite: true);
                File.ReadAllText(decryptedPath, Encoding.UTF8).ShouldBe(original);
            }
            finally
            {
                if (File.Exists(inputPath)) File.Delete(inputPath);
                if (File.Exists(encryptedPath)) File.Delete(encryptedPath);
                if (File.Exists(decryptedPath)) File.Delete(decryptedPath);
            }
        }

        [Fact]
        public void PgpEncrypt_Guards()
        {
            Should.Throw<ArgumentNullException>(() => _service.PgpEncryptBytes(null, _keys.PublicKey));
            Should.Throw<ArgumentNullException>(() => _service.PgpEncryptBytes(new byte[] { 1 }, null));
        }

        [Fact]
        public void PgpDecrypt_Guards()
        {
            Should.Throw<ArgumentNullException>(() => _service.PgpDecryptBytes(null, _keys.PrivateKey));
            Should.Throw<ArgumentNullException>(() => _service.PgpDecryptBytes(new byte[] { 1 }, null));
        }

        [Fact]
        public void PgpEncryptText_DecryptText_NullInput_Throws()
        {
            Should.Throw<ArgumentNullException>(() => _service.PgpEncryptText(null, _keys.PublicKey));
            Should.Throw<ArgumentNullException>(() => _service.PgpDecryptText(null, _keys.PrivateKey));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PGP Sign / ClearSign + Verify — Bytes / Text / File
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public void PgpSign_Bytes_Then_PgpVerify_RoundTrip()
        {
            byte[] plain = Encoding.UTF8.GetBytes("Sign me");
            byte[] signed = _service.PgpSignBytes(plain, _keys.PrivateKey);
            _service.PgpVerifyBytes(signed, _keys.PublicKey).ShouldBeTrue();
        }

        [Fact]
        public void PgpSignText_Then_PgpVerifyText_RoundTrip()
        {
            const string plain = "Sign-me-text";
            string signed = _service.PgpSignText(plain, _keys.PrivateKey);
            signed.ShouldStartWith("-----BEGIN PGP MESSAGE-----");
            _service.PgpVerifyText(signed, _keys.PublicKey).ShouldBeTrue();
        }

        [Fact]
        public void PgpSignFile_Then_PgpVerifyFile_RoundTrip()
        {
            string inputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string signedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(inputPath, "Sign-me-file", Encoding.UTF8);
                _service.PgpSignFile(inputPath, signedPath, _keys.PrivateKey, overwrite: true);
                _service.PgpVerifyFile(signedPath, _keys.PublicKey).ShouldBeTrue();
            }
            finally
            {
                if (File.Exists(inputPath)) File.Delete(inputPath);
                if (File.Exists(signedPath)) File.Delete(signedPath);
            }
        }

        [Fact]
        public void PgpClearSign_Bytes_Then_PgpVerifyClearSigned_RoundTrip()
        {
            byte[] plain = Encoding.UTF8.GetBytes("ClearSign me");
            byte[] signed = _service.PgpClearSignBytes(plain, _keys.PrivateKey);
            _service.PgpVerifyClearSignedBytes(signed, _keys.PublicKey).ShouldBeTrue();
        }

        [Fact]
        public void PgpClearSignText_Then_PgpVerifyClearSignedText_RoundTrip()
        {
            const string plain = "ClearSign-me-text";
            string signed = _service.PgpClearSignText(plain, _keys.PrivateKey);
            signed.ShouldStartWith("-----BEGIN PGP SIGNED MESSAGE-----");
            _service.PgpVerifyClearSignedText(signed, _keys.PublicKey).ShouldBeTrue();
        }

        [Fact]
        public void PgpClearSignFile_Then_PgpVerifyClearSignedFile_RoundTrip()
        {
            string inputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string signedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(inputPath, "ClearSign-me-file", Encoding.UTF8);
                _service.PgpClearSignFile(inputPath, signedPath, _keys.PrivateKey, overwrite: true);
                _service.PgpVerifyClearSignedFile(signedPath, _keys.PublicKey).ShouldBeTrue();
            }
            finally
            {
                if (File.Exists(inputPath)) File.Delete(inputPath);
                if (File.Exists(signedPath)) File.Delete(signedPath);
            }
        }

        [Fact]
        public void PgpSign_ClearSign_Guards()
        {
            Should.Throw<ArgumentNullException>(() => _service.PgpSignBytes(null, _keys.PrivateKey));
            Should.Throw<ArgumentNullException>(() => _service.PgpClearSignBytes(null, _keys.PrivateKey));
            Should.Throw<ArgumentNullException>(() => _service.PgpSignBytes(new byte[] { 1 }, null));
            Should.Throw<ArgumentNullException>(() => _service.PgpClearSignBytes(new byte[] { 1 }, null));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PGP Verify — guards + tamper detection
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public void PgpVerify_Guards()
        {
            Should.Throw<ArgumentNullException>(() => _service.PgpVerifyBytes(null, _keys.PublicKey));
            Should.Throw<ArgumentNullException>(() => _service.PgpVerifyClearSignedBytes(null, _keys.PublicKey));
            Should.Throw<ArgumentNullException>(() => _service.PgpVerifyText(null, _keys.PublicKey));
            Should.Throw<ArgumentNullException>(() => _service.PgpVerifyClearSignedText(null, _keys.PublicKey));
            Should.Throw<ArgumentException>(() => _service.PgpVerifyFile(null, _keys.PublicKey));
            Should.Throw<ArgumentException>(() => _service.PgpVerifyClearSignedFile(null, _keys.PublicKey));
        }

        [Fact]
        public void PgpVerify_TamperedBytes_ReturnsFalse()
        {
            byte[] plain = Encoding.UTF8.GetBytes("tamper test");
            byte[] signed = _service.PgpSignBytes(plain, _keys.PrivateKey);
            signed[signed.Length / 2] ^= 0x01;
            _service.PgpVerifyBytes(signed, _keys.PublicKey).ShouldBeFalse();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PGP VerifyPublicKey
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public void PgpVerifyPublicKey_FromFixture_ReturnsTrue()
        {
            _service.PgpVerifyPublicKey(_keys.PublicKey).ShouldBeTrue();
        }

        [Fact]
        public void PgpVerifyPublicKey_LoadedFromFile_ReturnsTrue()
        {
            PgpPublicKey loaded = PgpPublicKey.FromFilePath(_keys.PublicKeyPath);
            _service.PgpVerifyPublicKey(loaded).ShouldBeTrue();
        }

        [Fact]
        public void PgpVerifyPublicKey_GarbageBytes_ReturnsFalse()
        {
            PgpPublicKey junk = PgpPublicKey.FromBytes(Encoding.UTF8.GetBytes("not a key"));
            _service.PgpVerifyPublicKey(junk).ShouldBeFalse();
        }

        [Fact]
        public void PgpVerifyPublicKey_NullKey_Throws()
        {
            Should.Throw<ArgumentNullException>(() => _service.PgpVerifyPublicKey(null));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PGP Generate Keys
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public void PgpGenerateKeys_Guards()
        {
            Should.Throw<ArgumentException>(() => _service.PgpGenerateKeys(null, "pass"));
            Should.Throw<ArgumentException>(() => _service.PgpGenerateKeys(string.Empty, "pass"));
            Should.Throw<ArgumentNullException>(() => _service.PgpGenerateKeys("user", (SecureString)null));
        }

        [Fact]
        public void PgpGenerateKeys_StringPassphrase_ProducesUsableKeyPair()
        {
            PgpKeyPair pair = _service.PgpGenerateKeys("Gen Test <gen@test.com>", "gen-pass", RsaKeySize.Rsa2048);
            _service.PgpVerifyPublicKey(pair.PublicKey).ShouldBeTrue();
            byte[] cipher = _service.PgpEncryptBytes(Encoding.UTF8.GetBytes("ok"), pair.PublicKey);
            byte[] plain = _service.PgpDecryptBytes(cipher, pair.PrivateKey);
            Encoding.UTF8.GetString(plain).ShouldBe("ok");
        }

        [Fact]
        public void PgpGenerateKeys_SecureStringPassphrase_ProducesUsableKeyPair()
        {
            PgpKeyPair pair = _service.PgpGenerateKeys("Sec Gen <gen@test.com>", ToSecureString("sec-pass"), RsaKeySize.Rsa2048);
            _service.PgpVerifyPublicKey(pair.PublicKey).ShouldBeTrue();
            byte[] cipher = _service.PgpEncryptBytes(Encoding.UTF8.GetBytes("sec"), pair.PublicKey);
            byte[] plain = _service.PgpDecryptBytes(cipher, pair.PrivateKey);
            Encoding.UTF8.GetString(plain).ShouldBe("sec");
        }

        [Fact]
        public void PgpGenerateKeys_Deconstruct_YieldsBothHalves()
        {
            PgpKeyPair pair = _service.PgpGenerateKeys("Decon <d@t.com>", "decon-pass", RsaKeySize.Rsa2048);
            (PgpPublicKey pub, PgpPrivateKey priv) = pair;
            pub.ShouldNotBeNull();
            priv.ShouldNotBeNull();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Model-type guards
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public void PasswordKey_From_GuardsEmptyOrNull()
        {
            Should.Throw<ArgumentException>(() => PasswordKey.FromPassword((string)null, Encoding.UTF8));
            Should.Throw<ArgumentException>(() => PasswordKey.FromPassword(string.Empty, Encoding.UTF8));
            Should.Throw<ArgumentNullException>(() => PasswordKey.FromPassword("k", null));
            Should.Throw<ArgumentNullException>(() => PasswordKey.FromPassword((SecureString)null, Encoding.UTF8));
        }

        [Fact]
        public void RawKey_FromBytes_GuardsEmptyOrNull()
        {
            Should.Throw<ArgumentException>(() => RawKey.FromBytes(null));
            Should.Throw<ArgumentException>(() => RawKey.FromBytes(Array.Empty<byte>()));
        }

        [Fact]
        public void RawKey_FromHex_GuardsEmptyOrNull()
        {
            Should.Throw<ArgumentException>(() => RawKey.FromHex(null));
            Should.Throw<ArgumentException>(() => RawKey.FromHex(string.Empty));
        }

        [Fact]
        public void RawKey_FromBase64_GuardsEmptyOrNull()
        {
            Should.Throw<ArgumentException>(() => RawKey.FromBase64(null));
            Should.Throw<ArgumentException>(() => RawKey.FromBase64(string.Empty));
        }

        [Fact]
        public void PgpPublicKey_FromBytes_GuardsEmptyOrNull()
        {
            Should.Throw<ArgumentException>(() => PgpPublicKey.FromBytes(null));
            Should.Throw<ArgumentException>(() => PgpPublicKey.FromBytes(Array.Empty<byte>()));
        }

        [Fact]
        public void PgpPrivateKey_FromBytes_GuardsEmptyOrNull()
        {
            Should.Throw<ArgumentException>(() => PgpPrivateKey.FromBytes(null, "pass"));
            Should.Throw<ArgumentException>(() => PgpPrivateKey.FromBytes(Array.Empty<byte>(), "pass"));
            Should.Throw<ArgumentNullException>(() => PgpPrivateKey.FromBytes(new byte[] { 1 }, (SecureString)null));
        }

        // ───────────────────────────────────────────────────────────────────────
        // helpers
        // ───────────────────────────────────────────────────────────────────────

        private static PasswordKey NewPasswordKey(string value, string keyKind) => keyKind switch
        {
            "string" => PasswordKey.FromPassword(value, Encoding.UTF8),
            "secure" => PasswordKey.FromPassword(ToSecureString(value), Encoding.UTF8),
            _ => throw new ArgumentOutOfRangeException(nameof(keyKind)),
        };

        private static SecureString ToSecureString(string value)
        {
            var ss = new SecureString();
            foreach (char c in value)
                ss.AppendChar(c);
            ss.MakeReadOnly();
            return ss;
        }

        // Build a deterministic test key buffer without invoking a (CA5394-flagged) RNG —
        // tests need stable bytes, not actual randomness. The offset parameter lets different
        // test cases pick different byte sequences so they don't collide on a single key.
        private static byte[] MakeDeterministicKey(int sizeBytes, int offset)
        {
            byte[] bytes = new byte[sizeBytes];
            for (int i = 0; i < sizeBytes; i++) bytes[i] = (byte)(offset + i);
            return bytes;
        }
    }
#pragma warning restore CS0618
}
