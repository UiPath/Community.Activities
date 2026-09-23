using System;
using System.IO;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace UiPath.Cryptography.Activities.Tests
{
    /// <summary>
    /// Builds PGP test fixtures with BouncyCastle that reproduce the packet structures GnuPG 2.4+
    /// emits by default: signatures issued by a signing-capable SUBKEY (STUD-80429) and one-pass
    /// signatures wrapped in a COMPRESSED packet (STUD-80430). gpg is not available in the build
    /// environment, so these fixtures are generated in-process to exercise the verify fixes.
    /// </summary>
    internal sealed class PgpBcFixture
    {
        private readonly PgpSecretKey _signingSubkeySecret;
        private readonly char[] _passphrase;

        public byte[] PublicKeyRing { get; }

        private PgpBcFixture(PgpSecretKeyRing secretRing, PgpPublicKeyRing publicRing, PgpSecretKey signingSubkeySecret, char[] passphrase)
        {
            _signingSubkeySecret = signingSubkeySecret;
            _passphrase = passphrase;

            using var pubBuffer = new MemoryStream();
            using (var armored = new ArmoredOutputStream(pubBuffer))
            {
                publicRing.Encode(armored);
            }
            PublicKeyRing = pubBuffer.ToArray();
        }

        public static PgpBcFixture Create(string passphrase = "fixturepass")
        {
            var passChars = passphrase.ToCharArray();
            var random = new SecureRandom();

            var masterPair = GenerateRsaKeyPair(random);
            var subPair = GenerateRsaKeyPair(random);

            // Master key: certification only. Subkey: signing-capable (mirrors gpg defaults).
            var masterPgpPair = new PgpKeyPair(PublicKeyAlgorithmTag.RsaGeneral, masterPair, DateTime.UtcNow);
            var subPgpPair = new PgpKeyPair(PublicKeyAlgorithmTag.RsaSign, subPair, DateTime.UtcNow);

            var keyRingGenerator = new PgpKeyRingGenerator(
                PgpSignature.PositiveCertification,
                masterPgpPair,
                "fixture@test.com",
                SymmetricKeyAlgorithmTag.Aes256,
                passChars,
                true,
                null,
                null,
                random);

            keyRingGenerator.AddSubKey(subPgpPair);

            var secretRing = keyRingGenerator.GenerateSecretKeyRing();
            var publicRing = keyRingGenerator.GeneratePublicKeyRing();

            // Select the signing subkey (the second key in the ring; the first is the master).
            PgpSecretKey signingSubkeySecret = null;
            int index = 0;
            foreach (PgpSecretKey secretKey in secretRing.GetSecretKeys())
            {
                if (index == 1)
                {
                    signingSubkeySecret = secretKey;
                    break;
                }
                index++;
            }

            if (signingSubkeySecret is null)
            {
                throw new InvalidOperationException(
                    $"Failed to generate a signing subkey for the fixture. Secret key count: {index + 1}.");
            }

            return new PgpBcFixture(secretRing, publicRing, signingSubkeySecret, passChars);
        }

        /// <summary>
        /// Produce a one-pass signature over <paramref name="data"/> signed by the SUBKEY,
        /// optionally wrapped in a compressed packet (STUD-80430 + STUD-80429 combined).
        /// </summary>
        public byte[] CreateSignedMessage(byte[] data, bool compress)
        {
            var privateKey = _signingSubkeySecret.ExtractPrivateKey(_passphrase);
            var signatureGenerator = new PgpSignatureGenerator(_signingSubkeySecret.PublicKey.Algorithm, HashAlgorithmTag.Sha256);
            signatureGenerator.InitSign(PgpSignature.BinaryDocument, privateKey);

            using var outerBuffer = new MemoryStream();
            using (var armored = new ArmoredOutputStream(outerBuffer))
            {
                Stream signingTarget = armored;
                PgpCompressedDataGenerator compressedGenerator = null;
                if (compress)
                {
                    compressedGenerator = new PgpCompressedDataGenerator(CompressionAlgorithmTag.Zip);
                    signingTarget = compressedGenerator.Open(armored);
                }

                var bcpgOut = new BcpgOutputStream(signingTarget);
                signatureGenerator.GenerateOnePassVersion(false).Encode(bcpgOut);

                var literalGenerator = new PgpLiteralDataGenerator();
                using (var literalOut = literalGenerator.Open(bcpgOut, PgpLiteralData.Binary, "fixture.dat", data.Length, DateTime.UtcNow))
                {
                    literalOut.Write(data, 0, data.Length);
                    signatureGenerator.Update(data);
                }

                signatureGenerator.Generate().Encode(bcpgOut);
                compressedGenerator?.Close();
            }

            return outerBuffer.ToArray();
        }

        /// <summary>
        /// Produce a clear-text signature over <paramref name="text"/> signed by the SUBKEY (STUD-80429).
        /// Mirrors BouncyCastle's canonical ClearSignedFileProcessor line handling so the produced
        /// armor matches what a verifier expects.
        /// </summary>
        public byte[] CreateClearSignedMessage(string text)
        {
            var privateKey = _signingSubkeySecret.ExtractPrivateKey(_passphrase);
            var signatureGenerator = new PgpSignatureGenerator(_signingSubkeySecret.PublicKey.Algorithm, HashAlgorithmTag.Sha256);
            signatureGenerator.InitSign(PgpSignature.CanonicalTextDocument, privateKey);

            var inputBytes = System.Text.Encoding.ASCII.GetBytes(text.Replace("\r\n", "\n"));

            using var outerBuffer = new MemoryStream();
            using (var fIn = new MemoryStream(inputBytes))
            using (var armored = new ArmoredOutputStream(outerBuffer))
            {
                armored.BeginClearText(HashAlgorithmTag.Sha256);

                var lineOut = new MemoryStream();
                int lookAhead = ReadInputLine(lineOut, fIn);
                ProcessLine(armored, signatureGenerator, lineOut.ToArray());

                if (lookAhead != -1)
                {
                    do
                    {
                        lookAhead = ReadInputLine(lineOut, lookAhead, fIn);
                        signatureGenerator.Update((byte)'\r');
                        signatureGenerator.Update((byte)'\n');
                        ProcessLine(armored, signatureGenerator, lineOut.ToArray());
                    }
                    while (lookAhead != -1);
                }

                armored.EndClearText();

                var bcpgOut = new BcpgOutputStream(armored);
                signatureGenerator.Generate().Encode(bcpgOut);
            }

            return outerBuffer.ToArray();
        }

        private static void ProcessLine(Stream aOut, PgpSignatureGenerator sGen, byte[] line)
        {
            int length = GetLengthWithoutWhiteSpace(line);
            if (length > 0)
            {
                sGen.Update(line, 0, length);
            }

            aOut.Write(line, 0, line.Length);
        }

        private static int GetLengthWithoutWhiteSpace(byte[] line)
        {
            int end = line.Length - 1;
            while (end >= 0 && IsWhiteSpace(line[end]))
            {
                end--;
            }

            return end + 1;
        }

        private static bool IsWhiteSpace(byte b)
            => b == '\r' || b == '\n' || b == '\t' || b == ' ';

        private static int ReadInputLine(MemoryStream lineOut, Stream fIn)
        {
            lineOut.SetLength(0);

            int lookAhead = -1;
            int ch;
            while ((ch = fIn.ReadByte()) >= 0)
            {
                lineOut.WriteByte((byte)ch);
                if (ch == '\r' || ch == '\n')
                {
                    lookAhead = ReadPastEol(lineOut, ch, fIn);
                    break;
                }
            }

            return lookAhead;
        }

        private static int ReadInputLine(MemoryStream lineOut, int lookAhead, Stream fIn)
        {
            lineOut.SetLength(0);

            int ch = lookAhead;
            do
            {
                if (ch != '\r' && ch != '\n')
                {
                    lineOut.WriteByte((byte)ch);
                }
                else
                {
                    lookAhead = ReadPastEol(lineOut, ch, fIn);
                    return lookAhead;
                }
            }
            while ((ch = fIn.ReadByte()) >= 0);

            return -1;
        }

        private static int ReadPastEol(MemoryStream lineOut, int lastCh, Stream fIn)
        {
            int lookAhead = fIn.ReadByte();

            if (lastCh == '\r' && lookAhead == '\n')
            {
                lineOut.WriteByte((byte)lookAhead);
                lookAhead = fIn.ReadByte();
            }

            return lookAhead;
        }

        private static AsymmetricCipherKeyPair GenerateRsaKeyPair(SecureRandom random)
        {
            var generator = new RsaKeyPairGenerator();
            generator.Init(new RsaKeyGenerationParameters(
                BigInteger.ValueOf(0x10001), random, 2048, 25));
            return generator.GenerateKeyPair();
        }
    }
}
