using System.Threading;
using System.Threading.Tasks;
using UiPath.FTP.Activities.API.Models;

namespace UiPath.FTP.Activities.API
{
    /// <summary>
    /// Provides FTP/SFTP capabilities for coded workflows.
    /// </summary>
    public interface IFtpService
    {
        /// <summary>
        /// Opens a connection to an FTP/SFTP server and returns a handle to the active session.
        /// </summary>
        /// <param name="options">Connection options for the FTP/SFTP session.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>An <see cref="IFtpScopeHandle"/> representing the open session.</returns>
        Task<IFtpScopeHandle> UseFtpSession(FtpScopeOptions options, CancellationToken ct = default);
    }
}
