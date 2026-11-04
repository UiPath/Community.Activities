using System;
using System.IO;
using System.Text;

namespace UiPath.Cryptography.Activities.Tests
{
    /// <summary>
    /// A real message produced by GnuPG 2.4.9 with --force-aead for a throwaway RSA 3072 key: a public-key
    /// encrypted session key packet (tag 1) followed by an AEAD Encrypted Data packet (tag 20, version 1,
    /// AES-256, OCB). The packet is only reached after BouncyCastle finds the recipient's secret key, so the
    /// matching private key is part of the sample (passphrase <see cref="Passphrase"/>). Nothing here protects
    /// anything: the key was generated for these tests.
    /// </summary>
    internal static class GnuPgAeadSample
    {
        public const string Passphrase = "pw123";

        public static byte[] Message() => Convert.FromBase64String(string.Concat(
            "hQGMAw3mZrt035b+AQv/TFyZsOIyebBeh82ajImrBbzN86Nwz1oRFB8T17Fq7l8q+ZJNh67Tsy5sQqT8nZjlrounOZvuwLA/tYBa",
            "7ZliJg5mLdLim0JINFXy+aiLd2O3edf3V/9Zf6PrWaTb8QXcYpFeBETqSbT0jQe2b4/oB1VJqu3u49HVCcQpTjy8Qi3wNTyFPet9",
            "TTSVVw8+0crnm9y6z+5U7zzv8EMFct+78Fdm8/bBa77aajHZJy+pS9zRMsdaBIvNa9/Cd7m8O4SGgVGw6bTzQJqBanN/6fTRTFmD",
            "mW10AJ9PPSf12bKCs5QNRkL4ivAT7ueisItGAPgUK0U7MpmI7rZ7rdt1XbF0+dDxrOUOs7gBvmltlM9YmE6gFf/QBD7u18o/NHxD",
            "fNLyv72YTF7lVgT9ZFWezF4hVGaAqKoippvxrrF0WMKjFMdFYX7zfWSbajxy9zdbRuQw8OrJcB9Qd50TECyAm8fepjBVd7ytZ15d",
            "jOHTESQObUu+eZM0eqjMDUPTMh8u2aQT1FYBCQIQY0eLJVYneykoQj3FGVr3XqmMnoqC8jdmMOTzz4vLCT7x3vKoURGQHy/hTL9Y",
            "0y5EFy7DT/vmTJVLd0eqR6JOGEPt0yhymwsuUBZV9RQOKuWeaQ=="));

        public static string ArmoredPrivateKey() => string.Join("\n", new[]
        {
            "-----BEGIN PGP PRIVATE KEY BLOCK-----",
            "",
            "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",
            "UWTX2fFFApxk1w8c0g==",
            "=alTj",
            "-----END PGP PRIVATE KEY BLOCK-----"
        });

        public static MemoryStream PrivateKeyStream() => new MemoryStream(Encoding.ASCII.GetBytes(ArmoredPrivateKey()));

        public static string WritePrivateKeyFile()
        {
            var path = Path.Combine(Path.GetTempPath(), $"pgp_aead_key_{Guid.NewGuid()}.asc");
            File.WriteAllText(path, ArmoredPrivateKey());
            return path;
        }
    }
}
