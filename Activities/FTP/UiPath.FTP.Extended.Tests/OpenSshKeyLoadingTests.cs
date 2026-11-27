using System;
using System.IO;
using System.Linq;
using System.Text;
using Renci.SshNet;
using UiPath.FTP.Extensibility;
using UiPath.FTP.Extended.Internal;
using Xunit;

namespace UiPath.FTP.Extended.Tests
{
    /// <summary>
    /// Encrypted OpenSSH private keys (bcrypt_pbkdf) loaded through the options the SFTP session uses. The fixtures are
    /// throwaway keys made for these tests with <c>ssh-keygen -a 4 -N test-passphrase</c>; they protect nothing.
    /// </summary>
    public class OpenSshKeyLoadingTests
    {
        private const string Passphrase = "test-passphrase";

        private const string Ed25519Key = @"-----BEGIN OPENSSH PRIVATE KEY-----
b3BlbnNzaC1rZXktdjEAAAAACmFlczI1Ni1jdHIAAAAGYmNyeXB0AAAAGAAAABCH6atrs+
H6+7/6r45/UTQ9AAAABAAAAAEAAAAzAAAAC3NzaC1lZDI1NTE5AAAAINT3pe6X1tXbD1XK
eEYg13Lp3X2yepatsb9nUhDj7KPhAAAAkFe/agFmuow5wbDGveesUHTeZOglTbe8poBq+f
l0GybytfYxC0QJSTNGZILqduWmaDnBiKjumqJJUuPeNts5tF9mMMoqcTfDJ1I1q2zhUuIc
aH2FKX2czwgZ+dwWj0Q6yWeEiZzFoAB7AMfXb0Tt/C0RDVdl5d0LSlXiqjIzQkymezFDRG
DtZhHcPByNLHr3sA==
-----END OPENSSH PRIVATE KEY-----";

        private const string RsaKey = @"-----BEGIN OPENSSH PRIVATE KEY-----
b3BlbnNzaC1rZXktdjEAAAAACmFlczI1Ni1jdHIAAAAGYmNyeXB0AAAAGAAAABDmwmA+QF
O0mCHfwmTuDa+DAAAABAAAAAEAAAEXAAAAB3NzaC1yc2EAAAADAQABAAABAQC1uXgWQOmK
q3/TO8VGfKztZ1GBj5YQ/ir75MAt9wU+Xffb7YkHzhj6oaG+FqRk74o/Z0+P6+8My0wuHB
tp+Fdg8+xZphBuJl8hDuJKuhwuaLzJc0fzit7bO/Zglyj5HXOIQ+mwI64nn7lL/erie6iZ
0S++wVkrQjaTrc5yGSjEXrhctv/mk2qI3HI/nVigjJejEsMGJNJnR/OLgdW6QUgI2z6MhS
LLoleDoqcI2a1ucOryjUrqHjeu270TvZSbcfWBABm7hLfWztfGd1PXgbzYEYJ1/pb1AoKR
trR14NAgR6ZHkzQq9M3bVWMWtFZMIQ2g6BwFthEj1o+5AMiAN9nNAAADwL5Br1sUUDkDd4
HapGL53lCC6GcxFsGafQL64U76mT5QR+eVoY8EEQLoNPwP1BnPGONYpOZN88e4/VuK363c
1AoV1qdnHuxkwv4TNxYkgRtAkOOJK2XOdObq7EnX04MW+9vty5MNdHm2MQE9EF8Sb5K4Ss
LXlLB9B1SNQmRrl7MrZjxv/I4GKLbS3G8S4Vxq60xUG85BH5kNTMnaBQjf88QOFGQs9oWD
T16H+W7LPpJHcZppJu5NjAkFLFfFeFLbdZe0Aco7d3NmVXhMHMMnCE2yQNWGjj4WR3EpEI
LSu+FqttpC3z4v9UPUm0AUG2EQW5Yqzgs96FgXo8g7/xWTAVPnkWVIv2oKA76e46HZTodD
UKT06OBIA0oP+p/aZVPAqgy9EOBVF9AXKIronjseDdxtrEjvy8ldQZCTUufUiOUchwA6rX
8UyKjBZxWwX/MlUorx1k02IHvZDNHXzhg+G+EDxmr8roxZKERO11j988slph8nDHEZr+65
/qluamgZfdKIkiw8O5+LHd+6kDtJKwLih6lI3tzNQT40sEYY5Dq+86dosF1vsmlfeo3M/p
VSULLxa0bjnDdNRw+qtP4Yc3EHKvUTbWgX+YLzUbSfk0p8MKUYfFIUPie6R+POR0v8+UyW
5jPbHYk6qnKO1UxpF5fb7If3zBM2A8/vkyRJQpmHL3iY2qg6oeCfHlNBif9/VmwRfsZUnj
7pu3giBtZI4MuvaSlWXJ30sin/T7q7o+DAr/spNGjCDfpXKOd1fYDUfJ1+lCXCfuMz6fnt
t4ktk2SgSadIwwQeme0XNaBhpglRvULl6WMY5sxxSUkp4omSwAx0bzetUKpDRzXQ4AOUjt
vNz7KKgCnMPwXKyzzaZc0hLbG9GwkzzvvDl+oWlISSdqwOtsiXtQj1ND0fBmlWBPXwNk+l
kReKzK1hUNEGnocoRYinN/Rbagl9cm94AnFq8sAOqZ6puh6fqdy8qutNAsMcex50vBcn7Z
xxrBQY02Qmu/Ri+KgdWeuI/HlaZnjQU12MJgUaKq8cldjyYeMWHopOjc/1FD2KCTyxEDfx
pFoSaIB3vba+b0nXmPgwRBknFuXAAnLDlxfmTDJRhsu3WZS4ra4hfJf5omf39Rd3g9qms3
RCWATpOKZpmw3c8+JLD8N86atHgiQfxp2SEshRSDfpXDLCVvgDMzRCeVp/VXIRE/9DFQAI
yMbJmm9FhmpBuojVyFmvYLPbEpvkwzx7pz2ZBhGvRJFyILcs04EYIke/7Kw+oT6x2R6ytd
qgRK9LKg==
-----END OPENSSH PRIVATE KEY-----";

        private static PrivateKeyFileOptions Options() =>
            SftpCryptoRegistry.Create(new ISftpCryptoExtension[] { new ExtendedSftpCryptoExtension() }).CreateKeyFileOptions();

        private static PrivateKeyFile Load(string key, string passphrase)
        {
            // Normalize the line endings of the verbatim fixture (the file may be checked out with CRLF).
            string text = key.Replace("\r\n", "\n") + "\n";
            // The same constructor the SFTP session uses: a key file path.
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".key");
            File.WriteAllText(path, text, Encoding.ASCII);
            try
            {
                return new PrivateKeyFile(path, passphrase, certificateFileName: null, options: Options());
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Bcrypt_MatchesThePublishedKnownAnswer()
        {
            // bcrypt_pbkdf("password", "salt", rounds 4, 32 bytes), the reference vector of the OpenBSD implementation.
            byte[] key = new OpenSshBcryptPbkdf().DeriveKey(Encoding.ASCII.GetBytes("password"), Encoding.ASCII.GetBytes("salt"), 4, 32);

            Assert.Equal("5bbf0cc293587f1c3635555c27796598d47e579071bf427e9d8fbe842aba34d9", Convert.ToHexString(key).ToLowerInvariant());
        }

        [Fact]
        public void EncryptedEd25519OpenSshKey_LoadsWithTheRightPassphrase()
        {
            PrivateKeyFile file = Load(Ed25519Key, Passphrase);

            Assert.Contains(file.HostKeyAlgorithms, algorithm => algorithm.Name == "ssh-ed25519");
        }

        [Fact]
        public void EncryptedRsaOpenSshKey_LoadsWithTheRightPassphrase()
        {
            PrivateKeyFile file = Load(RsaKey, Passphrase);

            Assert.Contains(file.HostKeyAlgorithms, algorithm => algorithm.Name.StartsWith("rsa-sha2", StringComparison.Ordinal) || algorithm.Name == "ssh-rsa");
        }

        [Theory]
        [InlineData("wrong-passphrase")]
        [InlineData("")]
        public void EncryptedOpenSshKey_IsRejectedWithAWrongPassphrase(string passphrase)
        {
            Assert.ThrowsAny<Exception>(() => Load(Ed25519Key, passphrase));
            Assert.ThrowsAny<Exception>(() => Load(RsaKey, passphrase));
        }
    }
}
