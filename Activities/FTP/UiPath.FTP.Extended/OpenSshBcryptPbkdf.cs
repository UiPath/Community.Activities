using UiPath.FTP.Extended.Internal;
using UiPath.FTP.Extensibility;

namespace UiPath.FTP.Extended
{
    /// <summary>
    /// The bcrypt_pbkdf key derivation function of OpenSSH, used to open encrypted OpenSSH private keys. The
    /// implementation is the one SSH.NET used before it was removed from the operating system only build
    /// (see <c>BcryptPbkdfImplementation.cs</c> for its license).
    /// </summary>
    internal sealed class OpenSshBcryptPbkdf : ISftpBcryptPbkdf
    {
        public byte[] DeriveKey(byte[] passphrase, byte[] salt, int rounds, int outputLength)
        {
            var output = new byte[outputLength];
            new BCrypt().Pbkdf(passphrase, salt, rounds, output);
            return output;
        }
    }
}
