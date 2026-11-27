using UiPath.FTP.Extended;
using UiPath.FTP.Extensibility;

// Every SFTP connection finds this extension when the assembly is installed (see SftpCryptoExtensionDiscovery).
[assembly: SftpCryptoExtension(typeof(ExtendedSftpCryptoExtension))]
