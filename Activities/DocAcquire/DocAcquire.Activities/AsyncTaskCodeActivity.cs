using System;
using System.Activities;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace DocAcquire.Activities
{
    public abstract class AsyncTaskCodeActivity : AsyncCodeActivity
    {
        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            TaskCompletionSource<Action<AsyncCodeActivityContext>> taskCompletionSource = new TaskCompletionSource<Action<AsyncCodeActivityContext>>(state);
            Task<Action<AsyncCodeActivityContext>> task = ExecuteAsync(context);

            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    taskCompletionSource.TrySetException(t.Exception.InnerException);
                }
                else if (t.IsCanceled)
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

        }

        protected abstract Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context);
       
    }
}
