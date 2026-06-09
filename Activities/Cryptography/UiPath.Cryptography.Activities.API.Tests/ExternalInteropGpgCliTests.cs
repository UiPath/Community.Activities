using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Shouldly;
using UiPath.Cryptography.Activities.API;
using UiPath.Cryptography.Enums;
using Xunit;

namespace UiPath.Cryptography.Activities.API.Tests
{
    /// <summary>
    /// Per-test-class fixture that builds an isolated <c>--homedir</c>, imports the
    /// shared PGP keypair, and tears down on dispose. Each test runs gpg with this
    /// homedir so the developer's real keyring is untouched.
    /// </summary>
    public sealed class GpgIsolatedHomedir : IDisposable
    {
        private const string PublicKeyFilename = "ext_pub.asc";
        private const string PrivateKeyFilename = "ext_priv.asc";

        public PgpKeyFixture Keys { get; }
        public string HomeDir { get; }
        public string GpgFingerprint { get; private set; }
        public bool PublicKeyImported { get; private set; }
        public bool SecretKeyImported { get; private set; }
        public string LastImportError { get; private set; }

        public GpgIsolatedHomedir()
        {
            Keys = new PgpKeyFixture();
            HomeDir = Path.Combine(Path.GetTempPath(), $"gpg_homedir_{Guid.NewGuid():N}");
            Directory.CreateDirectory(HomeDir);

            if (!GpgCli.Probe()) return;

            // Import public key, then verify via --list-keys. The exit code of `gpg --import`
            // is unreliable on some Windows MSYS builds — it can be 2 because of a gpg-agent
            // hiccup even though the key WAS added to the keyring. So we run import-and-swallow,
            // then list-keys to confirm and grab the fingerprint.
            string pubPath = Path.Combine(HomeDir, PublicKeyFilename);
            File.WriteAllBytes(pubPath, Keys.PublicKeyBytes);
            try { GpgCli.Run(HomeDir, $"--batch --import \"{GpgCli.NormalisePath(pubPath)}\""); }
            catch (Exception ex) { LastImportError = ex.Message; }

            try
            {
                GpgFingerprint = ExtractFirstFingerprint(
                    GpgCli.RunCapture(HomeDir, "--with-colons --list-keys"));
                PublicKeyImported = !string.IsNullOrEmpty(GpgFingerprint);
            }
            catch (Exception ex)
            {
                LastImportError = (LastImportError + " | " + ex.Message).Trim('|', ' ');
            }

            if (!PublicKeyImported) return;

            // Same pattern for the private key — import then verify via --list-secret-keys.
            string privPath = Path.Combine(HomeDir, PrivateKeyFilename);
            File.WriteAllBytes(privPath, Keys.PrivateKeyBytes);
            try { GpgCli.Run(HomeDir, $"--batch --pinentry-mode loopback --passphrase {PgpKeyFixture.Passphrase} --import \"{GpgCli.NormalisePath(privPath)}\""); }
            catch (Exception ex) { LastImportError = (LastImportError + " | " + ex.Message).Trim('|', ' '); }

            try
            {
                string secrets = GpgCli.RunCapture(HomeDir, "--with-colons --list-secret-keys");
                SecretKeyImported = secrets.Contains("sec:");
            }
            catch { SecretKeyImported = false; }
        }

        public void Dispose()
        {
            try { Keys.Dispose(); } catch { }
            try { if (Directory.Exists(HomeDir)) Directory.Delete(HomeDir, recursive: true); } catch { }
        }

        private static string ExtractFirstFingerprint(string colons)
        {
            // gpg --with-colons emits records like "fpr:::::::::ABCDEF1234567890:" — the 10th colon-delimited field is the fingerprint.
            foreach (string line in colons.Split('\n'))
            {
                if (!line.StartsWith("fpr:")) continue;
                string[] parts = line.Split(':');
                if (parts.Length >= 10 && !string.IsNullOrEmpty(parts[9])) return parts[9];
            }
            throw new InvalidOperationException("Could not extract fingerprint from gpg --list-keys output:\n" + colons);
        }
    }

    /// <summary>
    /// Live bidirectional interop tests against the GnuPG CLI. Skip if <c>gpg</c> is
    /// not on PATH. On agents that have GnuPG these tests prove our PGP wire format
    /// against the canonical reference implementation.
    /// </summary>
    [Trait("Category", "Interop-CLI")]
    public class ExternalInteropGpgCliTests : IClassFixture<GpgIsolatedHomedir>
    {
        private const string Plaintext = "GPG CLI interop. UTF-8 ăîș 0123";
        private readonly CryptographyService _service = new CryptographyService();
        private readonly GpgIsolatedHomedir _gpg;

        public ExternalInteropGpgCliTests(GpgIsolatedHomedir gpg) => _gpg = gpg;

        // Visible diagnostic: surface why GPG tests no-op on this machine. Failing this test
        // when neither half imported flags a setup problem worth investigating (rather than
        // silently skipping all the real tests).
        [Fact]
        public void Fixture_AtLeastPublicKeyImported_OrSetupNotApplicable()
        {
            // No GPG on PATH → not a setup problem, nothing to do.
            if (!GpgCli.Probe()) return;

            // GPG present but neither key made it onto the keyring → surface the diagnostic.
            if (!_gpg.PublicKeyImported && !_gpg.SecretKeyImported)
            {
                Assert.Fail($"gpg is on PATH but no keys were imported. Last error: {_gpg.LastImportError ?? "(none)"}");
            }
        }

        // ────────────────────────────────────────────────────────────────────────
        // UiPath encrypts → gpg --decrypt verifies plaintext
        // ────────────────────────────────────────────────────────────────────────

        [Fact]
        public void UiPathEncrypts_GpgDecrypts_RoundTrips()
        {
            if (!_gpg.SecretKeyImported) return; // gpg side decrypts — needs private key on keyring

            byte[] cipherBytes = _service.PgpEncryptBytes(Encoding.UTF8.GetBytes(Plaintext), _gpg.Keys.PublicKey);

            string cipherPath = NewTempPath();
            string outPath = NewTempPath();
            try
            {
                File.WriteAllBytes(cipherPath, cipherBytes);

                GpgCli.Run(_gpg.HomeDir,
                    "--batch --pinentry-mode loopback",
                    $"--passphrase {PgpKeyFixture.Passphrase}",
                    "--decrypt",
                    $"--output \"{GpgCli.NormalisePath(outPath)}\"",
                    $"\"{GpgCli.NormalisePath(cipherPath)}\"");

                Encoding.UTF8.GetString(File.ReadAllBytes(outPath)).ShouldBe(Plaintext);
            }
            finally { Cleanup(cipherPath, outPath); }
        }

        // ────────────────────────────────────────────────────────────────────────
        // gpg --encrypt → UiPath PgpDecryptBytes recovers plaintext
        // ────────────────────────────────────────────────────────────────────────

        [Fact]
        public void GpgEncrypts_UiPathDecrypts_RoundTrips()
        {
            if (!_gpg.PublicKeyImported) return; // gpg side encrypts to the imported public key

            string plainPath = NewTempPath();
            string cipherPath = NewTempPath();
            try
            {
                File.WriteAllBytes(plainPath, Encoding.UTF8.GetBytes(Plaintext));

                GpgCli.Run(_gpg.HomeDir,
                    "--batch --yes",
                    "--trust-model always",
                    $"--recipient {_gpg.GpgFingerprint}",
                    "--encrypt",
                    $"--output \"{GpgCli.NormalisePath(cipherPath)}\"",
                    $"\"{GpgCli.NormalisePath(plainPath)}\"");

                byte[] cipher = File.ReadAllBytes(cipherPath);
                byte[] plain = _service.PgpDecryptBytes(cipher, _gpg.Keys.PrivateKey);
                Encoding.UTF8.GetString(plain).ShouldBe(Plaintext);
            }
            finally { Cleanup(plainPath, cipherPath); }
        }

        // ────────────────────────────────────────────────────────────────────────
        // UiPath signs → gpg --verify confirms the signature
        // ────────────────────────────────────────────────────────────────────────

        [Fact]
        public void UiPathSigns_GpgVerifies()
        {
            if (!_gpg.PublicKeyImported) return; // gpg verify only needs the public key on keyring

            byte[] signedBytes = _service.PgpSignBytes(Encoding.UTF8.GetBytes(Plaintext), _gpg.Keys.PrivateKey);

            string signedPath = NewTempPath();
            try
            {
                File.WriteAllBytes(signedPath, signedBytes);

                // gpg --verify exits 0 on a good signature; throws on non-zero.
                GpgCli.Run(_gpg.HomeDir,
                    "--batch --pinentry-mode loopback",
                    $"--passphrase {PgpKeyFixture.Passphrase}",
                    "--verify",
                    $"\"{GpgCli.NormalisePath(signedPath)}\"");
            }
            finally { Cleanup(signedPath); }
        }

        // ────────────────────────────────────────────────────────────────────────
        // UiPath clear-signs → gpg --verify on the cleartext signature
        // (Writes the armored text WITHOUT a UTF-8 BOM — gpg can't parse the
        // "-----BEGIN PGP SIGNED MESSAGE-----" header line if it starts with 0xEF 0xBB 0xBF.)
        // ────────────────────────────────────────────────────────────────────────

        [Fact]
        public void UiPathClearSigns_GpgVerifies()
        {
            if (!_gpg.PublicKeyImported) return;

            string clearSigned = _service.PgpClearSignText(Plaintext, _gpg.Keys.PrivateKey);

            string signedPath = NewTempPath();
            try
            {
                // CRITICAL: write without UTF-8 BOM. The default `Encoding.UTF8` instance
                // emits a BOM (0xEF 0xBB 0xBF) at the start of the file, which makes gpg
                // fail to recognise the "-----BEGIN PGP SIGNED MESSAGE-----" header line
                // ("gpg: no signed data" / "can't hash datafile: No data").
                File.WriteAllText(signedPath, clearSigned, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                GpgCli.Run(_gpg.HomeDir,
                    "--batch --pinentry-mode loopback",
                    $"--passphrase {PgpKeyFixture.Passphrase}",
                    "--verify",
                    $"\"{GpgCli.NormalisePath(signedPath)}\"");
            }
            finally { Cleanup(signedPath); }
        }

        // ────────────────────────────────────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────────────────────────────────────

        private static string NewTempPath() => Path.Combine(Path.GetTempPath(), $"gpg_cli_{Guid.NewGuid():N}.bin");

        private static void Cleanup(params string[] paths)
        {
            foreach (string p in paths)
                if (File.Exists(p)) File.Delete(p);
        }
    }

    internal static class GpgCli
    {
        public static bool Probe()
        {
            try
            {
                var psi = MakeStartInfo("gpg", "--version");
                using var p = Process.Start(psi);
                if (p == null) return false;
                bool exited = p.WaitForExit(5000);
                return exited && p.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        public static void Run(string homeDir, params string[] args)
        {
            var psi = MakeStartInfo("gpg", BuildArgs(homeDir, args));
            using var p = Process.Start(psi)
                ?? throw new InvalidOperationException("Could not start gpg");
            if (!p.WaitForExit(20_000))
            {
                try { p.Kill(); } catch { }
                throw new TimeoutException("gpg did not exit within 20s");
            }
            if (p.ExitCode != 0)
            {
                string stderr = p.StandardError.ReadToEnd();
                throw new InvalidOperationException($"gpg {string.Join(' ', args)} → exit {p.ExitCode}: {stderr}");
            }
        }

        public static string RunCapture(string homeDir, params string[] args)
        {
            var psi = MakeStartInfo("gpg", BuildArgs(homeDir, args));
            using var p = Process.Start(psi)
                ?? throw new InvalidOperationException("Could not start gpg");
            string stdout = p.StandardOutput.ReadToEnd();
            if (!p.WaitForExit(20_000))
            {
                try { p.Kill(); } catch { }
                throw new TimeoutException("gpg did not exit within 20s");
            }
            if (p.ExitCode != 0)
            {
                string stderr = p.StandardError.ReadToEnd();
                throw new InvalidOperationException($"gpg {string.Join(' ', args)} → exit {p.ExitCode}: {stderr}");
            }
            return stdout;
        }

        private static ProcessStartInfo MakeStartInfo(string fileName, string args) => new(fileName, args)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        private static string BuildArgs(string homeDir, string[] args)
        {
            // Always isolate the homedir so tests can't pollute the user's keyring.
            // The --homedir flag must come before subcommand arguments. Normalise to
            // forward-slash paths because GnuPG-on-Windows-via-MSYS expects POSIX-style.
            return $"--homedir \"{NormalisePath(homeDir)}\" {string.Join(' ', args)}";
        }

        // Some gpg builds on Windows (MSYS2 / Git-for-Windows) treat "C:\..." or "C:/..."
        // as a RELATIVE path with a colon in it — they want Cygwin-style absolute paths
        // like "/c/Users/...". Native Gpg4win accepts the regular Windows form. To work
        // with both, we probe once: import an empty file from a known temp homedir and
        // see whether gpg can find the keyring there. If the "/c/..." form succeeds where
        // "C:/..." fails, use cygpath conversion.
        private static readonly Lazy<bool> _useCygPath = new(DetectCygPathRequirement);

        private static bool DetectCygPathRequirement()
        {
            if (!OperatingSystem.IsWindows()) return false;
            string probe = Path.Combine(Path.GetTempPath(), $"gpg_probe_{Guid.NewGuid():N}");
            try
            {
                Directory.CreateDirectory(probe);
                // gpg --list-keys with a fresh homedir will create the keyring lazily — the
                // SIDE EFFECT here is that gpg actually touches the homedir filesystem. If
                // it ends up looking at "<cwd>/C:/..." we know it didn't understand the path.
                var psi = MakeStartInfo("gpg", $"--homedir \"{probe}\" --list-keys");
                using var p = Process.Start(psi);
                p.WaitForExit(5000);
                if (p.ExitCode == 0 && Directory.GetFiles(probe).Length + Directory.GetDirectories(probe).Length > 0)
                    return false; // Windows path worked.
                return true;
            }
            catch { return true; }
            finally { try { Directory.Delete(probe, recursive: true); } catch { } }
        }

        public static string NormalisePath(string p)
        {
            if (string.IsNullOrEmpty(p)) return p;
            if (!_useCygPath.Value) return p.Replace('\\', '/');

            // Convert "C:\foo\bar" → "/c/foo/bar" for MSYS-based gpg.
            string forward = p.Replace('\\', '/');
            if (forward.Length >= 2 && forward[1] == ':')
                return $"/{char.ToLowerInvariant(forward[0])}{forward.Substring(2)}";
            return forward;
        }
    }
}
