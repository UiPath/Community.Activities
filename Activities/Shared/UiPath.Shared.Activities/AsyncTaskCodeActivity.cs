using System;
using System.Activities;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace UiPath.Shared.Activities
{
    public abstract class AsyncTaskCodeActivity : AsyncCodeActivity, IDisposable
    {
        private CancellationTokenSource _cancellationTokenSource;
        private bool _tokenDisposed = false;

        protected override void Cancel(AsyncCodeActivityContext context)
        {
            if (!_tokenDisposed)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();

                _tokenDisposed = true;
            }

            base.Cancel(context);
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
        }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            if (!_tokenDisposed)
            {
                _cancellationTokenSource?.Dispose();
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _tokenDisposed = false;

            TaskCompletionSource<Action<AsyncCodeActivityContext>> taskCompletionSource = new TaskCompletionSource<Action<AsyncCodeActivityContext>>(state);
            Task<Action<AsyncCodeActivityContext>> task = ExecuteAsync(context, _cancellationTokenSource.Token);

            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    taskCompletionSource.TrySetException(t.Exception.InnerException);
                }
                else if (t.IsCanceled || _cancellationTokenSource.IsCancellationRequested)
                {
                    taskCompletionSource.TrySetCanceled();
                }
                else
                {
                    taskCompletionSource.TrySetResult(t.Result);
                }

                callback?.Invoke(taskCompletionSource.Task);
            });

            return taskCompletionSource.Task;
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            Task<Action<AsyncCodeActivityContext>> task = (Task<Action<AsyncCodeActivityContext>>)result;

            if (task.IsFaulted)
            {
                ExceptionDispatchInfo.Capture(task.Exception).Throw();
            }
            if (task.IsCanceled)
            {
                context.MarkCanceled();
            }

            task.Result?.Invoke(context);

            if (!_tokenDisposed)
            {
                _cancellationTokenSource?.Dispose();

                _tokenDisposed = true;
            }
        }

        protected abstract Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken);

        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (!_tokenDisposed)
                    {
                        _cancellationTokenSource.Dispose();

                        _tokenDisposed = true;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                _disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~AsyncTaskCodeActivity() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
