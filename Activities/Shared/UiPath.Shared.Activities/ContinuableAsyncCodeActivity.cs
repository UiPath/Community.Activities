using System;
using System.Activities;
using System.Diagnostics;
using System.Threading.Tasks;

namespace UiPath.Shared.Activities
{
    public abstract class ContinuableAsyncCodeActivity : AsyncTaskCodeActivity
    {
        public InArgument<bool> ContinueOnError { get; set; } = false;

        protected sealed override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            try
            {
                return base.BeginExecute(context, callback, state);
            }
            catch (Exception e)
            {
                if (ContinueOnError.Get(context))
                {
                    Trace.TraceError(e.ToString());

                    var taskCompletionSource = new TaskCompletionSource<AsyncCodeActivityContext>(state);
                    taskCompletionSource.TrySetResult(null);
                    callback?.Invoke(taskCompletionSource.Task);

                    return taskCompletionSource.Task;
                }
                else
                {
                    throw;
                }
            }
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            try
            {
                base.EndExecute(context, result);
            }
            catch (Exception e)
            {
                if (ContinueOnError.Get(context))
                {
                    Trace.TraceError(e.ToString());
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
