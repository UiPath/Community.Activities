using System;
using System.IO;

namespace UiPath.FTP
{
    public sealed class FtpConfiguration
    {
        public static readonly char DirectorySeparator = '/';

        public string Host { get; set; }
        public int? Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool UseAnonymousLogin { get; set; }
        public string ClientCertificatePath { get; set; }
        public string ClientCertificatePassword { get; set; }
        public bool AcceptAllCertificates { get; set; }

        public FtpConfiguration(string host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            Host = host;
        }

        // TODO: investigate
        public static string CombinePaths(params string[] paths)
        {
            if (paths == null || paths.Length == 0)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            string combined = paths[0].TrimEnd(DirectorySeparator);

            for (int i = 1; i < paths.Length; i++)
            {
                combined += DirectorySeparator + paths[i].TrimStart(DirectorySeparator);
            }

            return combined;
        }

        // TODO: investigate
        public static string GetDirectoryPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            int fileNameStart = path.LastIndexOf(Path.GetFileName(path));

            if (fileNameStart == -1)
            {
                throw new ArgumentException(nameof(path));
            }

            return path.Substring(0, fileNameStart);
        }
    }
}
