using System;
using System.Activities;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UiPath.Script.AutoHotKey;

namespace UiPath.Script.Activities.AutoHotKey
{
    public sealed class RunAutoHotKeyScript : ScriptActivity<string>
    {
        private AutoHotkeyExecutor Engine;
                
        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {            
            var inputParams = Parameters ?? new List<InArgument<string>>();

            if (inputParams.Count > 10)
                throw new ArgumentException("Only 10 parameters are allowed.");

            var fileInfo = new FileInfo(ScriptPath.Get(context));
            var fullName = fileInfo.FullName;
            var functionName = FunctionName.Get(context);

            if (!fileInfo.Exists)
                throw new ArgumentException($"'{fullName}' does not exist.");

            if (string.Compare(fileInfo.Extension, ".ahk", true) != 0)
                throw new ArgumentException($"'{fileInfo.Extension}' is not a valid AutoHotKey file type.");

            var paramList = inputParams.Select(x => x.Get(context)).ToList();

            var tcs = new TaskCompletionSource<string>(state);
            var cts = new CancellationTokenSource();
            context.UserState = cts;

            var task = Task.Factory.StartNew(() => {
                Engine = new AutoHotkeyExecutor();
                if (string.IsNullOrWhiteSpace(functionName))
                {
                    return Engine.ExecuteRaw(fileInfo);
                }
                else
                {
                    Engine.Load(fullName);

                    if (!Engine.FunctionExists(functionName))
                        throw new ArgumentException($"The function '{functionName}' does not exist in the provided file: '{fullName}'.");

                    return Engine.ExecuteFunction(functionName, paramList.ToArray());
                }
            }, cts.Token);

            task.ContinueWith(t => {
                if (t.IsFaulted) tcs.TrySetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled) tcs.TrySetCanceled();
                if (callback != null) callback(tcs.Task);
                tcs.TrySetResult(t.Result);
            });

            return tcs.Task;
        }

        protected override string EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            var task = result as Task<string>;

            if (task.IsFaulted) throw task.Exception.InnerException;
            if (task.IsCanceled || context.IsCancellationRequested)
            {
                context.MarkCanceled();
                return string.Empty;
            }

            return task.Result;
        }

        protected override void Cancel(AsyncCodeActivityContext context)
        {
            Engine.Terminate();
            var cts = (CancellationTokenSource)context.UserState;
            cts.Cancel();
        }
    }
}
