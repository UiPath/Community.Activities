using System;
using System.Threading;
using System.Threading.Tasks;
using UiPath.FTP;

namespace UiPath.FTP.Activities.API.Models
{
    /// <summary>
    /// Handle to an active FTP/SFTP session scope (to be used by the FTP coded operations).
    /// </summary>
    public interface IFtpScopeHandle : IDisposable, IAsyncDisposable
    {
        /// <summary>The underlying FTP session.</summary>
        IFtpSession Session { get; }
    }

    internal class FtpScopeHandle : IFtpScopeHandle
    {
        private int _disposed;

        public IFtpSession Session { get; }

        internal FtpScopeHandle(IFtpSession session)
        {
            Session = session;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;
            Session.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
