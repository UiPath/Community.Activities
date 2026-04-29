using System;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Java;

namespace UiPath.Java.Activities.API.Models
{
    /// <summary>
    /// Handle to a Java scope (to be used by the Java coded operations).
    /// </summary>
    public interface IJavaScopeHandle : IDisposable, IAsyncDisposable
    {
        /// <summary>The underlying Java invoker.</summary>
        IInvoker Invoker { get; }
    }

    internal class JavaScopeHandle : IJavaScopeHandle
    {
        private int _disposed;

        public IInvoker Invoker { get; }

        internal JavaScopeHandle(IInvoker invoker)
        {
            Invoker = invoker;
        }

        public void Dispose()
        {
            DisposeAsyncCore().AsTask().GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
        }

        private async ValueTask DisposeAsyncCore()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;
            await Invoker.ReleaseAsync().ConfigureAwait(false);
        }
    }
}
