using System;
using System.Activities;
using System.Activities.Expressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Shouldly;
using UiPath.Cryptography.Enums;
using Xunit;

#pragma warning disable CS0618 // obsolete algorithms reachable via opt-in formats

namespace UiPath.Cryptography.Activities.Tests
{
    /// <summary>
    /// Live bidirectional interop tests against the OpenSSL CLI. Each test starts by
    /// probing for the <c>openssl</c> executable on <c>PATH</c>; if it is missing the
    /// test no-ops (passes with no assertion). On CI agents that have OpenSSL these
    /// tests prove the spec against the reference implementation, complementing the
    /// in-process tests in <see cref="ExternalInteropInProcessTests"/>.
    /// </summary>
    [Trait("Category", "Interop-CLI")]
    public class ExternalInteropOpenSslCliTests
    {
        private const string Plaintext = "OpenSSL CLI interop round-trip. UTF-8 ăîș 0123";
        private const string Password = "openssl-cli-pwd";

        private static readonly bool OpenSslAvailable = OpenSslCli.Probe();

        // ────────────────────────────────────────────────────────────────────────
        // openssl enc → UiPath DecryptText
        // ────────────────────────────────────────────────────────────────────────

        [Theory]
        [InlineData("aes-128-cbc", 600_000, AesKeySize.Aes128)]
        [InlineData("aes-192-cbc", 600_000, AesKeySize.Aes192)]
        [InlineData("aes-256-cbc", 600_000, AesKeySize.Aes256)]
        [InlineData("aes-256-cbc", 50_000, AesKeySize.Aes256)]
        public void OpenSslCli_Encrypts_UiPathDecrypts(string opensslAlgorithm, int iterations, AesKeySize aesKeySize)
        {
            if (!OpenSslAvailable) return; // no-op when openssl is missing

            string plainPath = NewTempPath();
            string cipherPath = NewTempPath();
            try
            {
                File.WriteAllBytes(plainPath, Encoding.UTF8.GetBytes(Plaintext));

                // Drive openssl: enc -salt -aes-XXX-cbc -pbkdf2 -iter N -k <pwd> -in plainPath -out cipherPath
                OpenSslCli.Run(
                    "enc",
                    $"-{opensslAlgorithm}",
                    "-pbkdf2",
                    $"-iter {iterations}",
                    "-salt",
                    "-md sha256",
                    $"-pass pass:{Password}",
                    $"-in \"{plainPath}\"",
                    $"-out \"{cipherPath}\"");

                byte[] blob = File.ReadAllBytes(cipherPath);
                string base64 = Convert.ToBase64String(blob);

                string decrypted = RunDecryptText(
                    EncryptionAlgorithm.AES, SymmetricWireFormat.OpenSslEnc, KeyBytesFormat.Encoded,
                    password: Password, input: base64,
                    inputEncoding: Encoding.UTF8, iterations: iterations, aesKeySize: aesKeySize);

                decrypted.ShouldBe(Plaintext);
            }
            finally
            {
                Cleanup(plainPath, cipherPath);
            }
        }

        // ────────────────────────────────────────────────────────────────────────
        // UiPath EncryptText → openssl enc -d
        // ────────────────────────────────────────────────────────────────────────

        [Theory]
        [InlineData("aes-128-cbc", 600_000, AesKeySize.Aes128)]
        [InlineData("aes-192-cbc", 600_000, AesKeySize.Aes192)]
        [InlineData("aes-256-cbc", 600_000, AesKeySize.Aes256)]
        [InlineData("aes-256-cbc", 50_000, AesKeySize.Aes256)]
        public void UiPathEncrypts_OpenSslCli_Decrypts(string opensslAlgorithm, int iterations, AesKeySize aesKeySize)
        {
            if (!OpenSslAvailable) return;

            string cipherPath = NewTempPath();
            string outPath = NewTempPath();
            try
            {
                string encrypted = RunEncryptText(
                    EncryptionAlgorithm.AES, SymmetricWireFormat.OpenSslEnc, KeyBytesFormat.Encoded,
                    password: Password, iterations: iterations, inputEncoding: Encoding.UTF8, aesKeySize: aesKeySize);

                File.WriteAllBytes(cipherPath, Convert.FromBase64String(encrypted));

                OpenSslCli.Run(
                    "enc",
                    $"-{opensslAlgorithm}",
                    "-d",
                    "-pbkdf2",
                    $"-iter {iterations}",
                    "-md sha256",
                    $"-pass pass:{Password}",
                    $"-in \"{cipherPath}\"",
                    $"-out \"{outPath}\"");

                string roundTripped = File.ReadAllText(outPath, Encoding.UTF8);
                roundTripped.ShouldBe(Plaintext);
            }
            finally
            {
                Cleanup(cipherPath, outPath);
            }
        }

        // ────────────────────────────────────────────────────────────────────────
        // Test infrastructure
        // ────────────────────────────────────────────────────────────────────────

        private static class OpenSslCli
        {
#pragma warning disable CA1031 // Probe is a yes/no detector; any failure (missing exe, IO denied) means "openssl unavailable, skip the CLI tests".
            public static bool Probe()
            {
                try
                {
                    var psi = new ProcessStartInfo("openssl", "version")
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                    };
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
#pragma warning restore CA1031

            public static void Run(params string[] args)
            {
                var psi = new ProcessStartInfo("openssl", string.Join(" ", args))
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                using var p = Process.Start(psi);
                if (p == null) throw new InvalidOperationException("Could not start openssl");
                if (!p.WaitForExit(10_000))
                {
#pragma warning disable CA1031 // Best-effort kill of a hung child; any failure is swallowed before re-throwing the timeout.
                    try { p.Kill(); } catch { }
#pragma warning restore CA1031
                    throw new TimeoutException("openssl did not exit within 10s");
                }
                if (p.ExitCode != 0)
                {
                    string stderr = p.StandardError.ReadToEnd();
                    throw new InvalidOperationException($"openssl {string.Join(' ', args)} → exit {p.ExitCode}: {stderr}");
                }
            }
        }

        // ────────────────────────────────────────────────────────────────────────
        // Activity-surface helpers
        // ────────────────────────────────────────────────────────────────────────

        private static InArgument<Encoding> MakeEncodingArg(Encoding e)
        {
            if (e == Encoding.UTF8) return new InArgument<Encoding>(ExpressionServices.Convert((env) => Encoding.UTF8));
            if (e == Encoding.Unicode || e == null) return new InArgument<Encoding>(ExpressionServices.Convert((env) => Encoding.Unicode));
            throw new ArgumentException($"Test helper only supports UTF-8 and Unicode; got {e.WebName}");
        }

        private static string RunEncryptText(EncryptionAlgorithm algorithm, SymmetricWireFormat format, KeyBytesFormat keyFormat,
            string password = null, int iterations = 0, Encoding inputEncoding = null, AesKeySize aesKeySize = AesKeySize.Aes256)
        {
            var activity = new EncryptText
            {
                Algorithm = algorithm,
                Format = format,
                KeyFormat = keyFormat,
                Encoding = MakeEncodingArg(inputEncoding),
                KeyEncodingString = null,
                AesKeySize = aesKeySize,
            };
            var args = new Dictionary<string, object>
            {
                [nameof(EncryptText.Input)] = Plaintext,
                [nameof(EncryptText.Key)] = password,
            };
            if (iterations != 0) args[nameof(EncryptText.KdfIterations)] = iterations;
            try { return (string)new WorkflowInvoker(activity).Invoke(args)[nameof(activity.Result)]; }
            catch (System.Reflection.TargetInvocationException tie) when (tie.InnerException != null) { throw tie.InnerException; }
        }

        private static string RunDecryptText(EncryptionAlgorithm algorithm, SymmetricWireFormat format, KeyBytesFormat keyFormat,
            string input, string password = null, int iterations = 0, Encoding inputEncoding = null, AesKeySize aesKeySize = AesKeySize.Aes256)
        {
            var activity = new DecryptText
            {
                Algorithm = algorithm,
                Format = format,
                KeyFormat = keyFormat,
                Encoding = MakeEncodingArg(inputEncoding),
                KeyEncodingString = null,
                AesKeySize = aesKeySize,
            };
            var args = new Dictionary<string, object>
            {
                [nameof(DecryptText.Input)] = input,
                [nameof(DecryptText.Key)] = password,
            };
            if (iterations != 0) args[nameof(DecryptText.KdfIterations)] = iterations;
            try { return (string)new WorkflowInvoker(activity).Invoke(args)[nameof(activity.Result)]; }
            catch (System.Reflection.TargetInvocationException tie) when (tie.InnerException != null) { throw tie.InnerException; }
        }

        private static string NewTempPath() => Path.Combine(Path.GetTempPath(), $"openssl_cli_{Guid.NewGuid():N}.bin");

        private static void Cleanup(params string[] paths)
        {
            foreach (string p in paths)
                if (File.Exists(p)) File.Delete(p);
        }
    }
}

#pragma warning restore CS0618
