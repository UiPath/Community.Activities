using System;
using System.IO;
using System.Net;
using System.Security;
using UiPath.Cryptography.Activities.Models;
using UiPath.Cryptography.Activities.Properties;

namespace UiPath.Cryptography.Activities.Helpers
{
    internal static class PgpFileSignHelper
    {
        internal static CryptographyLocalItem ExecuteSign(
            string inputFilePath,
            string privateKeyFilePath,
            SecureString passphrase,
            string outputFilePath,
            bool overwrite,
            string suffix,
            Func<byte[], Stream, string, byte[]> signFunc)
        {
            if (!File.Exists(inputFilePath))
                throw new ArgumentException(Resources.FileDoesNotExistsException, Resources.InputFilePathDisplayName);
            if (string.IsNullOrWhiteSpace(privateKeyFilePath))
                throw new ArgumentNullException(Resources.PrivateKeyFilePathDisplayName);
            if (!File.Exists(privateKeyFilePath))
                throw new ArgumentException(Resources.FileDoesNotExistsException, Resources.PrivateKeyFilePathDisplayName);
            if (passphrase == null || passphrase.Length == 0)
                throw new ArgumentNullException(Resources.PassphraseDisplayName);

            if (string.IsNullOrEmpty(outputFilePath))
            {
                var dir = Path.GetDirectoryName(inputFilePath);
                var name = Path.GetFileNameWithoutExtension(inputFilePath) + suffix + Path.GetExtension(inputFilePath);
                outputFilePath = Path.Combine(dir, name);
            }

            if (File.Exists(outputFilePath) && !overwrite)
                throw new ArgumentException(Resources.FileAlreadyExistsException, Resources.OutputFilePathDisplayName);

            var passphraseString = new NetworkCredential("", passphrase).Password;
            var inputBytes = File.ReadAllBytes(inputFilePath);

            using (var privateKeyStream = File.OpenRead(privateKeyFilePath))
            {
                var signed = signFunc(inputBytes, privateKeyStream, passphraseString);

                var directory = Path.GetDirectoryName(outputFilePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllBytes(outputFilePath, signed);

                return new CryptographyLocalItem(Path.GetFileName(outputFilePath), outputFilePath);
            }
        }
    }
}
