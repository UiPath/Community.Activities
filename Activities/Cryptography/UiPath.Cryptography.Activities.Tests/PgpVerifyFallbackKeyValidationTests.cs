using System;
using System.IO;
using System.Text;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Xunit;

namespace UiPath.Cryptography.Activities.Tests
{
    /// <summary>
    /// PR #601 review: the BouncyCastle verify fallback resolves the signature's issuer against
    /// EVERY key in the ring, so it must (1) refuse revoked / expired / unbound / non-signing keys
    /// and (2) handle files signed by more than one key (nested one-pass headers).
    /// Every scenario runs against both the binary and the clear-text fallback, since they are
    /// separate code paths. Binary goes through the public <c>PgpVerify</c> using a COMPRESSED
    /// message (gpg's default), which PgpCore cannot read, so only the fallback can answer. For
    /// clear text, PgpCore can itself verify subkey-signed messages (without any revocation or
    /// expiry checks, as before this PR), so the fallback is called directly to pin ITS behavior.
    /// </summary>
    public class PgpVerifyFallbackKeyValidationTests
    {
        private const long OneYear = 365L * 24 * 3600;
        private static readonly byte[] Payload = Encoding.UTF8.GetBytes("payload for key validation coverage");
        private const string ClearPayload = "clear text for key validation\nsecond line";

        public enum Mode
        {
            /// <summary>Compressed one-pass signature (gpg's default), verified through PgpVerify.</summary>
            Binary,

            /// <summary>Clear-signed text, verified by the clear-text fallback directly.</summary>
            Clear,
        }

        // ---------------------------------------------------------------- helpers

        private static bool SignAndVerify(Mode mode, PgpBcFixture signer, byte[] ring = null, DateTime? signedAtUtc = null)
            => MultiSignAndVerify(mode, ring ?? signer.PublicKeyRing, signedAtUtc, signer);

        private static bool MultiSignAndVerify(Mode mode, byte[] ring, DateTime? signedAtUtc, params PgpBcFixture[] signers)
        {
            using var keyStream = new MemoryStream(ring);
            if (mode == Mode.Binary)
            {
                var signed = PgpBcFixture.CreateMultiSignerMessage(Payload, compress: true, signedAtUtc, signers);
                return CryptographyHelper.PgpVerify(signed, keyStream);
            }

            var clear = PgpBcFixture.CreateMultiSignerClearSignedMessage(ClearPayload, signedAtUtc, signers);
            return CryptographyHelper.BcVerifyClearSignature(clear, ring);
        }

        private static PgpBcFixture Create(Action<PgpBcFixture.Options> configure)
        {
            var options = new PgpBcFixture.Options();
            configure(options);
            return PgpBcFixture.Create(options);
        }

        private static DateTime DaysAgo(int days) => DateTime.UtcNow.AddDays(-days);

        // ---------------------------------------------------------------- key validity: accepted

        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void ValidSigningSubkey_Verifies(Mode mode)
            => Assert.True(SignAndVerify(mode, PgpBcFixture.Create()));

        // Keys from older tools carry no key-flags subpacket on the binding signature; they must
        // keep verifying (only an explicit flag set that omits "sign" is rejected).
        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void Subkey_WithoutKeyFlagsSubpacket_Verifies(Mode mode)
            => Assert.True(SignAndVerify(mode, Create(o => o.SubkeyKeyFlags = null)));

        // The master-key branch of the validity check: a compressed primary-signed message is
        // beyond PgpCore's reach, so it goes through the fallback with IsMasterKey == true.
        [Fact]
        public void PrimaryKeySigner_Compressed_Verifies()
            => Assert.True(SignAndVerify(Mode.Binary, Create(o => o.SignWithPrimary = true)));

        // A key that expired AFTER the signature was made still vouches for that signature.
        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void ExpiredSubkey_SignedBeforeExpiry_Verifies(Mode mode)
        {
            var signer = Create(o =>
            {
                o.MasterCreatedUtc = DaysAgo(730);
                o.SubkeyCreatedUtc = DaysAgo(730);
                o.SubkeyExpirySeconds = OneYear; // valid until ~365 days ago
            });

            Assert.True(SignAndVerify(mode, signer, signedAtUtc: DaysAgo(548)));
        }

        // ---------------------------------------------------------------- key validity: rejected

        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void RevokedSubkey_DoesNotVerify(Mode mode)
            => Assert.False(SignAndVerify(mode, Create(o => o.RevokeSubkey = true)));

        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void RevokedPrimary_SubkeySignature_DoesNotVerify(Mode mode)
            => Assert.False(SignAndVerify(mode, Create(o => o.RevokePrimary = true)));

        [Fact]
        public void RevokedPrimary_PrimarySigned_DoesNotVerify()
            => Assert.False(SignAndVerify(Mode.Binary, Create(o =>
            {
                o.SignWithPrimary = true;
                o.RevokePrimary = true;
            })));

        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void ExpiredSubkey_SignedAfterExpiry_DoesNotVerify(Mode mode)
        {
            var signer = Create(o =>
            {
                o.MasterCreatedUtc = DaysAgo(730);
                o.SubkeyCreatedUtc = DaysAgo(730);
                o.SubkeyExpirySeconds = OneYear;
            });

            Assert.False(SignAndVerify(mode, signer)); // signed "now", long after expiry
        }

        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void ExpiredPrimary_SubkeySignature_DoesNotVerify(Mode mode)
        {
            var signer = Create(o =>
            {
                o.MasterCreatedUtc = DaysAgo(730);
                o.MasterExpirySeconds = OneYear;
                o.SubkeyCreatedUtc = DaysAgo(730);
            });

            Assert.False(SignAndVerify(mode, signer));
        }

        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void SignatureCreatedBeforeKeyExisted_DoesNotVerify(Mode mode)
        {
            var signer = Create(o =>
            {
                o.MasterCreatedUtc = DaysAgo(730);
                o.SubkeyCreatedUtc = DaysAgo(730);
            });

            Assert.False(SignAndVerify(mode, signer, signedAtUtc: DaysAgo(900)));
        }

        // Keyserver-poisoning scenario: a subkey from an unrelated key is appended to a real
        // public key. Its binding signature was not made by this ring's primary key.
        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void GraftedSubkey_BoundByAnotherPrimary_DoesNotVerify(Mode mode)
            => Assert.False(SignAndVerify(mode, PgpBcFixture.CreateWithGraftedSubkey()));

        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void Subkey_WithoutAnyBindingSignature_DoesNotVerify(Mode mode)
            => Assert.False(SignAndVerify(mode, Create(o => o.StripSubkeyBinding = true)));

        // Flags are present but omit "sign" (encryption-only subkey): its signatures are not trusted.
        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void Subkey_FlaggedEncryptionOnly_DoesNotVerify(Mode mode)
            => Assert.False(SignAndVerify(mode, Create(o =>
                o.SubkeyKeyFlags = PgpKeyFlags.CanEncryptCommunications | PgpKeyFlags.CanEncryptStorage)));

        // Same checks apply to the text entry points (the coded API's PgpVerifyText).
        [Fact]
        public void RevokedSubkey_PgpVerifyText_DoesNotVerify()
        {
            var signer = Create(o => o.RevokeSubkey = true);
            var armored = Encoding.ASCII.GetString(
                signer.CreateSignedMessage(Payload, compress: true));

            using var keyStream = new MemoryStream(signer.PublicKeyRing);
            Assert.False(CryptographyHelper.PgpVerifyText(armored, keyStream));
        }

        // ---------------------------------------------------------------- embedded back-signature (0x19)

        // GnuPG requires a signing subkey to consent to its binding with an embedded Primary Key
        // Binding signature made by the subkey. A binding by the primary alone is not enough: anyone
        // could otherwise attach somebody else public signing subkey to their own key.
        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void SigningSubkey_WithoutBackSignature_DoesNotVerify(Mode mode)
            => Assert.False(SignAndVerify(mode, Create(o => o.BackSignature = PgpBcFixture.BackSignatureMode.Missing)));

        // The back-signature was made by an unrelated key, not by the subkey itself.
        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void SigningSubkey_BackSignatureMadeByAnotherKey_DoesNotVerify(Mode mode)
            => Assert.False(SignAndVerify(mode, Create(o => o.BackSignature = PgpBcFixture.BackSignatureMode.SignedByWrongKey)));

        // The stolen-subkey case: the subkey did sign a back-signature, but over its REAL primary key,
        // while this ring attaches it to a different primary.
        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void SigningSubkey_BackSignatureOverAnotherPrimary_DoesNotVerify(Mode mode)
            => Assert.False(SignAndVerify(mode, Create(o => o.BackSignature = PgpBcFixture.BackSignatureMode.WrongPrimary)));

        // A back-signature that does not name its issuer is still accepted when it verifies against the subkey.
        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void SigningSubkey_BackSignatureWithoutIssuer_Verifies(Mode mode)
        {
            var signer = Create(o => o.BackSignature = PgpBcFixture.BackSignatureMode.ValidWithoutIssuer);

            Assert.True(System.Linq.Enumerable.All(BackSignaturesOf(signer), s => s.KeyId == 0), "the fixture must produce an issuer-less back-signature");
            Assert.True(SignAndVerify(mode, signer));
        }

        private static System.Collections.Generic.List<PgpSignature> BackSignaturesOf(PgpBcFixture user)
        {
            var result = new System.Collections.Generic.List<PgpSignature>();
            foreach (PgpSignature binding in user.SigningPublicKey.GetSignaturesOfType(PgpSignature.SubkeyBinding))
            {
                foreach (var subpackets in new[] { binding.GetHashedSubPackets(), binding.GetUnhashedSubPackets() })
                {
                    var embedded = subpackets?.GetEmbeddedSignatures();
                    for (var i = 0; embedded is not null && i < embedded.Count; i++)
                    {
                        result.Add(embedded[i]);
                    }
                }
            }

            Assert.NotEmpty(result);
            return result;
        }

        // GnuPG may place the embedded signature in either subpacket area; both are accepted.
        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void SigningSubkey_BackSignatureInUnhashedArea_Verifies(Mode mode)
            => Assert.True(SignAndVerify(mode, Create(o => o.BackSignatureInUnhashedArea = true)));

        // A primary-key signer has no binding and therefore needs no back-signature.
        [Fact]
        public void PrimaryKeySigner_NeedsNoBackSignature()
            => Assert.True(SignAndVerify(Mode.Binary, Create(o =>
            {
                o.SignWithPrimary = true;
                o.BackSignature = PgpBcFixture.BackSignatureMode.Missing;
            })));

        // Like a subkey, a primary key whose self-signature carries key flags must allow signing: a
        // certify-only primary does not verify data signatures, whichever way they are made.
        [Theory]
        [InlineData(Mode.Binary, PgpKeyFlags.CanCertify, false)]
        [InlineData(Mode.Clear, PgpKeyFlags.CanCertify, false)]
        [InlineData(Mode.Binary, PgpKeyFlags.CanCertify | PgpKeyFlags.CanSign, true)]
        [InlineData(Mode.Clear, PgpKeyFlags.CanCertify | PgpKeyFlags.CanSign, true)]
        public void PrimaryKeySigner_KeyFlagsDecideWhetherItMaySign(Mode mode, int primaryKeyFlags, bool expected)
            => Assert.Equal(expected, SignAndVerify(mode, Create(o =>
            {
                o.SignWithPrimary = true;
                o.PrimaryKeyFlags = primaryKeyFlags;
            })));

        // The governing self-signature of the primary key is the newest one that verifies against it and was in
        // force at signing time. The initial self-signature is dated when the fixture is created; the later ones
        // are dated a day after it and the message is signed two days after it.
        private static DateTime SoonAfterCreation(int days) => DateTime.UtcNow.AddDays(days);

        private static bool PrimarySignsAfterSelfSignatures(Mode mode, int? initialFlags, Action<PgpBcFixture.Options> configure)
            => SignAndVerify(mode, Create(o =>
            {
                o.SignWithPrimary = true;
                o.PrimaryKeyFlags = initialFlags;
                configure(o);
            }), signedAtUtc: SoonAfterCreation(2));

        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void PrimaryKeySigner_NewerSelfSignatureWithSigningFlags_Verifies(Mode mode)
            => Assert.True(PrimarySignsAfterSelfSignatures(mode, PgpKeyFlags.CanCertify,
                o => o.LaterPrimaryCertifications.Add((SoonAfterCreation(1), PgpKeyFlags.CanCertify | PgpKeyFlags.CanSign))));

        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void PrimaryKeySigner_NewerSelfSignatureWithoutSigningFlags_DoesNotVerify(Mode mode)
            => Assert.False(PrimarySignsAfterSelfSignatures(mode, null,
                o => o.LaterPrimaryCertifications.Add((SoonAfterCreation(1), PgpKeyFlags.CanCertify))));

        // A newer self-signature without a key-flags subpacket governs: it no longer carries the older restriction.
        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void PrimaryKeySigner_NewerSelfSignatureWithoutFlags_ReplacesOlderRestriction(Mode mode)
            => Assert.True(PrimarySignsAfterSelfSignatures(mode, PgpKeyFlags.CanCertify,
                o => o.LaterPrimaryCertifications.Add((SoonAfterCreation(1), null))));

        // Flags that are present but empty grant no capability at all.
        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void PrimaryKeySigner_EmptyKeyFlags_DoesNotVerify(Mode mode)
            => Assert.False(PrimarySignsAfterSelfSignatures(mode, null,
                o => o.LaterPrimaryCertifications.Add((SoonAfterCreation(1), 0))));

        // A self-signature that names the primary key as issuer but was made by another key does not verify, so it
        // cannot lift the restriction of the genuine one.
        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void PrimaryKeySigner_SelfSignatureNotMadeByThePrimaryKey_IsIgnored(Mode mode)
            => Assert.False(PrimarySignsAfterSelfSignatures(mode, PgpKeyFlags.CanCertify,
                o => o.ForgedPrimaryCertifications.Add((SoonAfterCreation(1), PgpKeyFlags.CanCertify | PgpKeyFlags.CanSign))));

        // A user id that is not plain ASCII must not hide the key's restriction (or its permission).
        [Theory]
        [InlineData(Mode.Binary, PgpKeyFlags.CanCertify, false)]
        [InlineData(Mode.Clear, PgpKeyFlags.CanCertify, false)]
        [InlineData(Mode.Binary, PgpKeyFlags.CanCertify | PgpKeyFlags.CanSign, true)]
        [InlineData(Mode.Clear, PgpKeyFlags.CanCertify | PgpKeyFlags.CanSign, true)]
        public void PrimaryKeySigner_NonAsciiUserId_KeyFlagsStillDecide(Mode mode, int primaryKeyFlags, bool expected)
            => Assert.Equal(expected, SignAndVerify(mode, Create(o =>
            {
                o.SignWithPrimary = true;
                o.UserId = "Ștefan Ionescu <stefan@example.com>";
                o.PrimaryKeyFlags = primaryKeyFlags;
            })));

        // Key flags may also sit on a direct-key signature, which governs when it is the newest self-signature.
        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void PrimaryKeySigner_NewerDirectKeySignatureWithoutSigningFlags_DoesNotVerify(Mode mode)
            => Assert.False(PrimarySignsAfterSelfSignatures(mode, null,
                o => o.DirectKeySignatures.Add((SoonAfterCreation(1), PgpKeyFlags.CanCertify))));

        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void PrimaryKeySigner_NewerDirectKeySignatureWithSigningFlags_Verifies(Mode mode)
            => Assert.True(PrimarySignsAfterSelfSignatures(mode, PgpKeyFlags.CanCertify,
                o => o.DirectKeySignatures.Add((SoonAfterCreation(1), PgpKeyFlags.CanCertify | PgpKeyFlags.CanSign))));

        // A revocation of a user-id certification grants nothing: it must not replace the certification's restriction.
        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void PrimaryKeySigner_UserIdRevocation_DoesNotLiftTheRestriction(Mode mode)
            => Assert.False(PrimarySignsAfterSelfSignatures(mode, PgpKeyFlags.CanCertify,
                o => o.UserIdRevocations.Add(SoonAfterCreation(1))));

        // A self-signature that cannot be verified (unknown algorithm) does not count and does not fail the evaluation.
        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void PrimaryKeySigner_UnverifiableSelfSignature_IsSkipped(Mode mode)
            => Assert.True(PrimarySignsAfterSelfSignatures(mode, PgpKeyFlags.CanCertify | PgpKeyFlags.CanSign,
                o => o.UnverifiableSelfCertifications.Add((SoonAfterCreation(1), PgpKeyFlags.CanCertify))));

        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void PrimaryKeySigner_UnverifiableSelfSignature_CannotLiftTheRestriction(Mode mode)
            => Assert.False(PrimarySignsAfterSelfSignatures(mode, PgpKeyFlags.CanCertify,
                o => o.UnverifiableSelfCertifications.Add((SoonAfterCreation(1), PgpKeyFlags.CanCertify | PgpKeyFlags.CanSign))));

        // The binding of a signing subkey that carries empty key flags grants nothing, like for the primary key.
        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void SigningSubkey_EmptyKeyFlags_DoesNotVerify(Mode mode)
            => Assert.False(SignAndVerify(mode, Create(o => o.SubkeyKeyFlags = 0)));

        // The back-signature check is applied to the binding that is in force at signing time: the first
        // binding carries one, the later binding (also signing-capable) does not.
        private static PgpBcFixture WithLaterBindingWithoutBackSignature()
            => Create(o =>
            {
                o.MasterCreatedUtc = DaysAgo(1000);
                o.SubkeyCreatedUtc = DaysAgo(1000);
                o.LaterBindings.Add((DaysAgo(100), PgpKeyFlags.CanSign, PgpBcFixture.BackSignatureMode.Missing));
            });

        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void LaterBinding_WithoutBackSignature_IsNotInForceBeforeItsDate(Mode mode)
            => Assert.True(SignAndVerify(mode, WithLaterBindingWithoutBackSignature(), signedAtUtc: DaysAgo(500)));

        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void LaterBinding_WithoutBackSignature_GovernsSignaturesMadeAfterItsDate(Mode mode)
            => Assert.False(SignAndVerify(mode, WithLaterBindingWithoutBackSignature(), signedAtUtc: DaysAgo(10)));

        // ---------------------------------------------------------------- binding in force at signing time

        // Old bindings stay in the ring. The one that governs a signature is the latest valid
        // binding that existed (and had not lapsed) when the message was signed.
        private static PgpBcFixture WithBindingHistory(int? firstFlags, params (int AgeDays, int? Flags)[] later)
            => Create(o =>
            {
                o.MasterCreatedUtc = DaysAgo(1000);
                o.SubkeyCreatedUtc = DaysAgo(1000);
                o.SubkeyKeyFlags = firstFlags;
                foreach (var (ageDays, flags) in later)
                {
                    o.LaterBindings.Add((DaysAgo(ageDays), flags, null));
                }
            });

        private const int EncryptOnly = PgpKeyFlags.CanEncryptCommunications | PgpKeyFlags.CanEncryptStorage;

        // Signing was authorized, then later restricted to encryption: a signature made BEFORE the
        // restriction still counts; one made after it does not.
        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void LaterRestrictedBinding_SignatureMadeBeforeRestriction_Verifies(Mode mode)
        {
            var signer = WithBindingHistory(PgpKeyFlags.CanSign, (100, EncryptOnly));

            Assert.True(SignAndVerify(mode, signer, signedAtUtc: DaysAgo(500)));
        }

        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void LaterRestrictedBinding_SignatureMadeAfterRestriction_DoesNotVerify(Mode mode)
        {
            var signer = WithBindingHistory(PgpKeyFlags.CanSign, (100, EncryptOnly));

            Assert.False(SignAndVerify(mode, signer, signedAtUtc: DaysAgo(10)));
        }

        // Signing was authorized only LATER: a signature made before that authorization must not
        // be validated by the newer binding; one made after it is fine.
        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void LaterAuthorizedBinding_SignatureMadeBeforeAuthorization_DoesNotVerify(Mode mode)
        {
            var signer = WithBindingHistory(EncryptOnly, (100, PgpKeyFlags.CanSign));

            Assert.False(SignAndVerify(mode, signer, signedAtUtc: DaysAgo(500)));
        }

        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void LaterAuthorizedBinding_SignatureMadeAfterAuthorization_Verifies(Mode mode)
        {
            var signer = WithBindingHistory(EncryptOnly, (100, PgpKeyFlags.CanSign));

            Assert.True(SignAndVerify(mode, signer, signedAtUtc: DaysAgo(10)));
        }

        // The latest binding wins even when an older, signing-capable one is still present.
        [Fact]
        public void SupersededSigningBinding_DoesNotAuthorizeLaterSignature()
        {
            var signer = WithBindingHistory(PgpKeyFlags.CanSign, (400, EncryptOnly), (100, EncryptOnly));

            Assert.False(SignAndVerify(Mode.Binary, signer, signedAtUtc: DaysAgo(10)));
        }

        // A binding that postdates the signature and is the only one is not in force.
        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void OnlyBindingPostdatesSignature_DoesNotVerify(Mode mode)
        {
            var signer = Create(o =>
            {
                o.MasterCreatedUtc = DaysAgo(1000);
                o.SubkeyCreatedUtc = DaysAgo(100); // binding dated 100 days ago
            });

            Assert.False(SignAndVerify(mode, signer, signedAtUtc: DaysAgo(200)));
        }

        // A binding signature can itself expire; after that it no longer authorizes the subkey.
        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void BindingSignatureExpired_DoesNotVerify(Mode mode)
        {
            var signer = Create(o =>
            {
                o.MasterCreatedUtc = DaysAgo(730);
                o.SubkeyCreatedUtc = DaysAgo(730);
                o.SubkeyBindingExpirySeconds = OneYear; // lapsed ~365 days ago
            });

            Assert.False(SignAndVerify(mode, signer));
        }

        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void BindingSignatureNotYetExpired_Verifies(Mode mode)
        {
            var signer = Create(o =>
            {
                o.MasterCreatedUtc = DaysAgo(730);
                o.SubkeyCreatedUtc = DaysAgo(730);
                o.SubkeyBindingExpirySeconds = OneYear;
            });

            Assert.True(SignAndVerify(mode, signer, signedAtUtc: DaysAgo(548)));
        }

        // ---------------------------------------------------------------- marker packets

        [Theory]
        [InlineData(true, false)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        public void MarkerPacketBeforeSignedPayload_Verifies(bool compress, bool markerInsideCompression)
        {
            var marker = markerInsideCompression ? PgpBcFixture.MarkerPlacement.Inside : PgpBcFixture.MarkerPlacement.Outside;
            var signer = PgpBcFixture.Create();
            var signed = PgpBcFixture.CreateMultiSignerMessage(Payload, compress, null, marker, signer);

            using var keyStream = new MemoryStream(signer.PublicKeyRing);
            Assert.True(CryptographyHelper.PgpVerify(signed, keyStream));
        }

        [Fact]
        public void MarkerPacketBeforeSignedPayload_PgpVerifyText_Verifies()
        {
            var signer = PgpBcFixture.Create();
            var armored = Encoding.ASCII.GetString(
                PgpBcFixture.CreateMultiSignerMessage(Payload, compress: true, null, PgpBcFixture.MarkerPlacement.Outside, signer));

            using var keyStream = new MemoryStream(signer.PublicKeyRing);
            Assert.True(CryptographyHelper.PgpVerifyText(armored, keyStream));
        }

        // Skipping markers must not weaken anything: a marker-prefixed message from an unusable key
        // is still rejected, as is one from a key that is not in the ring.
        [Fact]
        public void MarkerPacket_RevokedSubkey_DoesNotVerify()
        {
            var signer = Create(o => o.RevokeSubkey = true);
            var signed = PgpBcFixture.CreateMultiSignerMessage(Payload, compress: true, null, PgpBcFixture.MarkerPlacement.Inside, signer);

            using var keyStream = new MemoryStream(signer.PublicKeyRing);
            Assert.False(CryptographyHelper.PgpVerify(signed, keyStream));
        }

        [Fact]
        public void MarkerPacket_UnknownSigner_DoesNotVerify()
        {
            var signer = PgpBcFixture.Create();
            var stranger = PgpBcFixture.Create();
            var signed = PgpBcFixture.CreateMultiSignerMessage(Payload, compress: true, null, PgpBcFixture.MarkerPlacement.Outside, signer);

            using var keyStream = new MemoryStream(stranger.PublicKeyRing);
            Assert.False(CryptographyHelper.PgpVerify(signed, keyStream));
        }

        [Fact]
        public void MarkerPacketBeforeClearTextSignature_Verifies()
        {
            var signer = PgpBcFixture.Create();
            var clear = PgpBcFixture.CreateMultiSignerClearSignedMessage(ClearPayload, null, markerBeforeSignature: true, signer);

            Assert.True(CryptographyHelper.BcVerifyClearSignature(clear, signer.PublicKeyRing));
        }

        // ---------------------------------------------------------------- multiple signers

        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void TwoSigners_BothKeysInRing_Verifies(Mode mode)
        {
            var a = PgpBcFixture.Create();
            var b = PgpBcFixture.Create();

            Assert.True(MultiSignAndVerify(mode, PgpBcFixture.CombinePublicKeyRings(a, b), null, a, b));
        }

        // OPS(A) OPS(B) literal SIG(B) SIG(A): with only B's key present, B's signature is the
        // FIRST one in the signature list, but A's header is the first one-pass packet.
        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void TwoSigners_OnlySecondSignersKeyInRing_Verifies(Mode mode)
        {
            var a = PgpBcFixture.Create();
            var b = PgpBcFixture.Create();

            Assert.True(MultiSignAndVerify(mode, b.PublicKeyRing, null, a, b));
        }

        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void TwoSigners_OnlyFirstSignersKeyInRing_Verifies(Mode mode)
        {
            var a = PgpBcFixture.Create();
            var b = PgpBcFixture.Create();

            Assert.True(MultiSignAndVerify(mode, a.PublicKeyRing, null, a, b));
        }

        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void TwoSigners_NeitherKeyInRing_DoesNotVerify(Mode mode)
        {
            var a = PgpBcFixture.Create();
            var b = PgpBcFixture.Create();
            var stranger = PgpBcFixture.Create();

            Assert.False(MultiSignAndVerify(mode, stranger.PublicKeyRing, null, a, b));
        }

        // One acceptable signer is enough, even if another signer's key is untrustworthy.
        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void TwoSigners_OneValidOneRevoked_BothKeysInRing_Verifies(Mode mode)
        {
            var valid = PgpBcFixture.Create();
            var revoked = Create(o => o.RevokeSubkey = true);

            Assert.True(MultiSignAndVerify(mode, PgpBcFixture.CombinePublicKeyRings(revoked, valid), null, revoked, valid));
        }

        // ...but the untrustworthy signer's key on its own must not carry the message.
        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void TwoSigners_OneValidOneRevoked_OnlyRevokedKeyInRing_DoesNotVerify(Mode mode)
        {
            var valid = PgpBcFixture.Create();
            var revoked = Create(o => o.RevokeSubkey = true);

            Assert.False(MultiSignAndVerify(mode, revoked.PublicKeyRing, null, valid, revoked));
        }

        [Theory]
        [InlineData(Mode.Binary)]
        [InlineData(Mode.Clear)]
        public void TwoSigners_GraftedSubkeyOnly_DoesNotVerify(Mode mode)
        {
            var valid = PgpBcFixture.Create();
            var grafted = PgpBcFixture.CreateWithGraftedSubkey();

            Assert.False(MultiSignAndVerify(mode, grafted.PublicKeyRing, null, valid, grafted));
        }

        // Three signers exercise the bracket pairing beyond the symmetric two-signer case.
        [Fact]
        public void ThreeSigners_OnlyMiddleKeyInRing_Verifies()
        {
            var a = PgpBcFixture.Create();
            var b = PgpBcFixture.Create();
            var c = PgpBcFixture.Create();

            Assert.True(MultiSignAndVerify(Mode.Binary, b.PublicKeyRing, null, a, b, c));
        }

        [Fact]
        public void TwoSigners_PgpVerifyText_OnlySecondSignersKeyInRing_Verifies()
        {
            var a = PgpBcFixture.Create();
            var b = PgpBcFixture.Create();
            var armored = Encoding.ASCII.GetString(
                PgpBcFixture.CreateMultiSignerMessage(Payload, compress: true, null, a, b));

            using var keyStream = new MemoryStream(b.PublicKeyRing);
            Assert.True(CryptographyHelper.PgpVerifyText(armored, keyStream));
        }

        [Fact]
        public void TwoSigners_Uncompressed_BothKeysInRing_Verifies()
        {
            var a = PgpBcFixture.Create();
            var b = PgpBcFixture.Create();
            var signed = PgpBcFixture.CreateMultiSignerMessage(Payload, compress: false, null, a, b);

            using var keyStream = new MemoryStream(PgpBcFixture.CombinePublicKeyRings(a, b));
            Assert.True(CryptographyHelper.PgpVerify(signed, keyStream));
        }
    }
}
