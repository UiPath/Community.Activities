using System;
using System.Activities;
using System.Activities.Validation;
using System.Linq;
using Shouldly;
using UiPath.Cryptography.Enums;
using Xunit;

#pragma warning disable CS0618 // tests intentionally use obsolete algorithms to fire FIPS warnings.

namespace UiPath.Cryptography.Activities.Tests
{
    /// <summary>
    /// Coverage for the design-time validation warnings emitted by the
    /// EncryptText/DecryptText/EncryptFile/DecryptFile CacheMetadata overrides. The runtime
    /// already enforces these constraints; these tests pin the user-facing design-time
    /// warning surface so a refactor cannot quietly drop a warning the studio author depends on.
    /// </summary>
    public class CacheMetadataWarningTests
    {
        // FIPS warning is emitted for algorithms that have no FIPS-compliant implementation.
        // Today: RC2, Rijndael, ChaCha20Poly1305, PGP are not FIPS-compliant. Pinning a
        // representative non-FIPS algorithm so a future change to the FIPS-compliance map
        // doesn't silently drop the warning.
        [Theory]
        [InlineData(EncryptionAlgorithm.RC2)]
        [InlineData(EncryptionAlgorithm.Rijndael)]
        public void EncryptText_NonFipsAlgorithm_EmitsFipsWarning(EncryptionAlgorithm algorithm)
        {
            var activity = new EncryptText { Algorithm = algorithm };
            ValidationError[] warnings = ValidateAndGetWarnings(activity);

            warnings.Any(w => w.Message.Contains("FIPS", StringComparison.Ordinal)).ShouldBeTrue(
                $"expected a FIPS warning for {algorithm}, got: {Format(warnings)}");
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AESGCM)]    // FIPS-compliant
        [InlineData(EncryptionAlgorithm.AES)]       // FIPS-compliant
        [InlineData(EncryptionAlgorithm.TripleDES)] // FIPS-compliant
        public void EncryptText_FipsAlgorithm_NoFipsWarning(EncryptionAlgorithm algorithm)
        {
            var activity = new EncryptText { Algorithm = algorithm };
            ValidationError[] warnings = ValidateAndGetWarnings(activity);

            warnings.Any(w => w.Message.Contains("FIPS", StringComparison.Ordinal)).ShouldBeFalse(
                $"did not expect FIPS warning for {algorithm}, got: {Format(warnings)}");
        }

        // Iv set on EncryptText with Format == Raw → IV nonce-reuse warning. The IV is only
        // consumed by the Raw wire format, so the warning is gated on it. The warning text
        // mentions "(Key, IV) pair" so the user can self-serve.
        [Fact]
        public void EncryptText_WithExplicitIv_EmitsNonceReuseWarning()
        {
            var activity = new EncryptText
            {
                Algorithm = EncryptionAlgorithm.AESGCM,
                Format = SymmetricWireFormat.Raw,
                Iv = new InArgument<string>("AABBCCDDEEFF00112233445566778899"),
            };
            ValidationError[] warnings = ValidateAndGetWarnings(activity);

            warnings.Any(w => w.Message.Contains("(Key, IV) pair", StringComparison.Ordinal) || w.Message.Contains("explicit IV", StringComparison.Ordinal)).ShouldBeTrue(
                $"expected an IV nonce-reuse warning, got: {Format(warnings)}");
        }

        // Iv set but Format is non-Raw → no warning. Non-Raw formats reject an IV at runtime,
        // so the warning fired in unrelated contexts and desensitized the user. STUD-80532.
        [Theory]
        [InlineData(SymmetricWireFormat.Classic)]
        [InlineData(SymmetricWireFormat.Owasp2026)]
        [InlineData(SymmetricWireFormat.OpenSslEnc)]
        public void EncryptText_ExplicitIv_NonRawFormat_NoNonceReuseWarning(SymmetricWireFormat format)
        {
            var activity = new EncryptText
            {
                Algorithm = EncryptionAlgorithm.AESGCM,
                Format = format,
                Iv = new InArgument<string>("AABBCCDDEEFF00112233445566778899"),
            };
            ValidationError[] warnings = ValidateAndGetWarnings(activity);

            warnings.Any(w => w.Message.Contains("(Key, IV) pair", StringComparison.Ordinal) || w.Message.Contains("nonce", StringComparison.Ordinal)).ShouldBeFalse(
                $"did not expect IV nonce-reuse warning for non-Raw format {format}, got: {Format(warnings)}");
        }

        [Fact]
        public void EncryptText_WithoutIv_NoNonceReuseWarning()
        {
            var activity = new EncryptText { Algorithm = EncryptionAlgorithm.AESGCM };
            ValidationError[] warnings = ValidateAndGetWarnings(activity);

            warnings.Any(w => w.Message.Contains("(Key, IV) pair", StringComparison.Ordinal) || w.Message.Contains("nonce", StringComparison.Ordinal)).ShouldBeFalse(
                $"did not expect IV nonce-reuse warning, got: {Format(warnings)}");
        }

        // The EncryptFile activity has the same CacheMetadata logic — pin it independently
        // since both activities will need to stay aligned.
        [Fact]
        public void EncryptFile_NonFipsAlgorithm_EmitsFipsWarning()
        {
            var activity = new EncryptFile { Algorithm = EncryptionAlgorithm.RC2 };
            ValidationError[] warnings = ValidateAndGetWarnings(activity);

            warnings.Any(w => w.Message.Contains("FIPS", StringComparison.Ordinal)).ShouldBeTrue();
        }

        [Fact]
        public void EncryptFile_WithExplicitIv_EmitsNonceReuseWarning()
        {
            var activity = new EncryptFile
            {
                Algorithm = EncryptionAlgorithm.AES,
                Format = SymmetricWireFormat.Raw,
                Iv = new InArgument<string>("AABBCCDDEEFF00112233445566778899"),
            };
            ValidationError[] warnings = ValidateAndGetWarnings(activity);

            warnings.Any(w => w.Message.Contains("(Key, IV) pair", StringComparison.Ordinal) || w.Message.Contains("explicit IV", StringComparison.Ordinal)).ShouldBeTrue();
        }

        // Companion to EncryptText_ExplicitIv_NonRawFormat_NoNonceReuseWarning: both activities
        // share the same CacheMetadata gate and must stay aligned. STUD-80532.
        [Theory]
        [InlineData(SymmetricWireFormat.Classic)]
        [InlineData(SymmetricWireFormat.Owasp2026)]
        [InlineData(SymmetricWireFormat.OpenSslEnc)]
        public void EncryptFile_ExplicitIv_NonRawFormat_NoNonceReuseWarning(SymmetricWireFormat format)
        {
            var activity = new EncryptFile
            {
                Algorithm = EncryptionAlgorithm.AES,
                Format = format,
                Iv = new InArgument<string>("AABBCCDDEEFF00112233445566778899"),
            };
            ValidationError[] warnings = ValidateAndGetWarnings(activity);

            warnings.Any(w => w.Message.Contains("(Key, IV) pair", StringComparison.Ordinal) || w.Message.Contains("nonce", StringComparison.Ordinal)).ShouldBeFalse(
                $"did not expect IV nonce-reuse warning for non-Raw format {format}, got: {Format(warnings)}");
        }

        // DecryptText/DecryptFile emit FIPS + ChaCha warnings but NOT the IV warning
        // (no Iv property on decrypt). Pin that asymmetry — the IV warning is only relevant
        // when the user can choose the IV (encryption).
        [Fact]
        public void DecryptText_NonFipsAlgorithm_EmitsFipsWarning()
        {
            var activity = new DecryptText { Algorithm = EncryptionAlgorithm.RC2 };
            ValidationError[] warnings = ValidateAndGetWarnings(activity);

            warnings.Any(w => w.Message.Contains("FIPS", StringComparison.Ordinal)).ShouldBeTrue();
        }

        // ────────────────────────────────────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────────────────────────────────────

        // ActivityValidationServices.Validate runs CacheMetadata on the activity tree and
        // collects every error/warning into the returned ValidationResults. Then we filter
        // to warnings only (isWarning=true entries).
        private static ValidationError[] ValidateAndGetWarnings(Activity activity) =>
            ActivityValidationServices.Validate(activity).Warnings.ToArray();

        private static string Format(ValidationError[] warnings) =>
            warnings.Length == 0 ? "(none)" : string.Join("; ", warnings.Select(w => w.Message));
    }
}

#pragma warning restore CS0618
