using System;
using System.IO;
using System.Net;
using System.Security;
using UiPath.Cryptography.Activities.Properties;

namespace UiPath.Cryptography.Activities.Helpers
{
    internal static class PgpStreamHelper
    {
        internal static TResult WithPgpEncryptStreams<TResult>(
            string publicKeyFilePath,
            string privateKeyFilePath,
            SecureString passphrase,
            bool signData,
            Func<Stream, Stream, string, TResult> operation)
        {
            if (string.IsNullOrWhiteSpace(publicKeyFilePath))
                throw new ArgumentNullException(Resources.PublicKeyFilePathDisplayName);
            if (!File.Exists(publicKeyFilePath))
                throw new ArgumentException(Resources.FileDoesNotExistsException, Resources.PublicKeyFilePathDisplayName);

            using (var publicKeyStream = File.OpenRead(publicKeyFilePath))
            {
                Stream privateKeyStream = null;
                try
                {
                    string passphraseString = null;
                    if (signData)
                    {
                        if (string.IsNullOrWhiteSpace(privateKeyFilePath))
                            throw new ArgumentNullException(Resources.PrivateKeyFilePathDisplayName);
                        if (!File.Exists(privateKeyFilePath))
                            throw new ArgumentException(Resources.FileDoesNotExistsException, Resources.PrivateKeyFilePathDisplayName);
                        if (passphrase == null || passphrase.Length == 0)
                            throw new ArgumentNullException(Resources.PassphraseDisplayName);
                        passphraseString = new NetworkCredential("", passphrase).Password;
                        privateKeyStream = File.OpenRead(privateKeyFilePath);
                    }

                    return operation(publicKeyStream, privateKeyStream, passphraseString);
                }
                finally
                {
                    privateKeyStream?.Dispose();
                }
            }
        }

        internal static void WithPgpEncryptStreams(
            string publicKeyFilePath,
            string privateKeyFilePath,
            SecureString passphrase,
            bool signData,
            Action<Stream, Stream, string> operation)
        {
            WithPgpEncryptStreams<object>(publicKeyFilePath, privateKeyFilePath, passphrase, signData,
                (pub, priv, pass) => { operation(pub, priv, pass); return null; });
        }

        internal static TResult WithPgpDecryptStreams<TResult>(
            string privateKeyFilePath,
            SecureString passphrase,
            string publicKeyFilePath,
            bool verifySignature,
            Func<Stream, string, Stream, TResult> operation)
        {
            if (string.IsNullOrWhiteSpace(privateKeyFilePath))
                throw new ArgumentNullException(Resources.PrivateKeyFilePathDisplayName);
            if (!File.Exists(privateKeyFilePath))
                throw new ArgumentException(Resources.FileDoesNotExistsException, Resources.PrivateKeyFilePathDisplayName);
            if (passphrase == null || passphrase.Length == 0)
                throw new ArgumentNullException(Resources.PassphraseDisplayName);

            var passphraseString = new NetworkCredential("", passphrase).Password;

            using (var privateKeyStream = File.OpenRead(privateKeyFilePath))
            {
                Stream publicKeyStream = null;
                try
                {
                    if (verifySignature)
                    {
                        if (string.IsNullOrWhiteSpace(publicKeyFilePath))
                            throw new ArgumentNullException(Resources.PublicKeyFilePathDisplayName);
                        if (!File.Exists(publicKeyFilePath))
                            throw new ArgumentException(Resources.FileDoesNotExistsException, Resources.PublicKeyFilePathDisplayName);
                        publicKeyStream = File.OpenRead(publicKeyFilePath);
                    }

                    return operation(privateKeyStream, passphraseString, publicKeyStream);
                }
                finally
                {
                    publicKeyStream?.Dispose();
                }
            }
        }
        internal static void WithPgpDecryptStreams(
            string privateKeyFilePath,
            SecureString passphrase,
            string publicKeyFilePath,
            bool verifySignature,
            Action<Stream, string, Stream> operation)
        {
            WithPgpDecryptStreams<object>(privateKeyFilePath, passphrase, publicKeyFilePath, verifySignature,
                (priv, pass, pub) => { operation(priv, pass, pub); return null; });
        }
    }
}
