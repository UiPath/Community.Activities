using System;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Platform.ResourceHandling;

namespace UiPath.Cryptography.Activities.Helpers
{
    /// <summary>
    /// Resolves paired properties (string path ↔ IResource, string ↔ SecureString) at runtime.
    /// Tries the string side first; falls back to the IResource/SecureString side if the string is empty.
    /// Throws <see cref="ArgumentNullException"/> if both are empty.
    /// </summary>
    internal static class PgpFileResolver
    {
        public static async Task<string> ResolveAsync(
            string filePath,
            IResource resource,
            string paramName,
            string displayName,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(filePath))
                return filePath;

            if (resource == null)
                throw new ArgumentNullException(paramName, displayName);

            var local = resource.ToLocalResource();
            await local.ResolveAsync(ct: cancellationToken);
            if (string.IsNullOrWhiteSpace(local.LocalPath))
                throw new ArgumentNullException(paramName, displayName);

            return local.LocalPath;
        }

        public static string ResolvePassphrase(
            string passphrase,
            SecureString securePassphrase,
            string paramName,
            string displayName)
        {
            if (!string.IsNullOrWhiteSpace(passphrase))
                return passphrase;

            if (securePassphrase == null || securePassphrase.Length == 0)
                throw new ArgumentNullException(paramName, displayName);

            return new NetworkCredential(string.Empty, securePassphrase).Password;
        }
    }
}
