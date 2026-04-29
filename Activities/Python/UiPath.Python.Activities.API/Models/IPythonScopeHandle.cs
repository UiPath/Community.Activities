using System;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Python;

namespace UiPath.Python.Activities.API.Models
{
    /// <summary>
    /// Handle to a Python scope (to be used by the Python coded operations).
    /// </summary>
    public interface IPythonScopeHandle : IDisposable, IAsyncDisposable
    {
    }

    internal class PythonScopeHandle : IPythonScopeHandle
    {
        private int _disposed;

        public IEngine Engine { get; }

        internal PythonScopeHandle(IEngine engine)
        {
            Engine = engine;
        }

        public void Dispose()
        {
            Task.Run(() => DisposeAsyncCore().AsTask()).GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
        }

        private async ValueTask DisposeAsyncCore()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;
            await Engine.Release().ConfigureAwait(false);
        }
    }
}
