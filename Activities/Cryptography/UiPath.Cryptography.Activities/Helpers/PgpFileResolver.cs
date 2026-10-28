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

        /// <summary>
        /// Resolves a paired string-path ↔ IResource input for the synchronous activities
        /// (Encrypt/Decrypt Text &amp; File). The string path takes precedence; only when it is empty or
        /// whitespace and a resource is supplied is the resource resolved to a local path. The precedence
        /// logic itself does not throw — it returns the (possibly empty) path so callers keep their own
        /// required/optional validation; resolving a supplied resource can still surface exceptions from
        /// the underlying resource-handling stack. Mirrors the path-first precedence of the design-time
        /// path/resource toggle, matching the whitespace handling of <see cref="ResolveAsync"/>.
        /// </summary>
        public static string ResolveLocalPath(string filePath, IResource resource)
        {
            if (!string.IsNullOrWhiteSpace(filePath) || resource == null)
                return filePath;

            return ResolveResourceLocalPath(resource);
        }

        // Thin sync-over-async adapter over the platform's IResource→local-file conversion.
        private static string ResolveResourceLocalPath(IResource resource)
        {
            var local = resource.ToLocalResource();
            local.ResolveAsync().GetAwaiter().GetResult();
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
