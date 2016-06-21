using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using UiPath.Script.Powershell;

namespace UiPath.Script.Activities.PowerShell
{
    public class RunPowerShellScript<TResult> : AsyncCodeActivity
    {
        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> ScriptPath { get; set; }

        [Category("Input")]
        [Browsable(true)]
        public Dictionary<string, InArgument> Parameters { get; private set; } = new Dictionary<string, InArgument>();

        [Category("Output")]
        public OutArgument<IEnumerable<TResult>> Output { get; set; }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            var scriptPath = ScriptPath.Get(context);
            if (Path.GetExtension(scriptPath) != ".ps1")
                throw new ArgumentException($"'{Path.GetExtension(scriptPath)}' is not a valid PowerShell file type.");

            var parameters = Parameters.Select(x => new KeyValuePair<string, object>(x.Key, x.Value.Get(context))).ToList();
            var psExec = new PowerShellExecutor();
            context.UserState = psExec;
            return psExec.ExecuteScript(scriptPath, parameters, callback, state);
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            PipelineInvokerAsyncResult asyncResult = result as PipelineInvokerAsyncResult;
            var executor = context.UserState as PowerShellExecutor;

            if (asyncResult == null || executor == null) return;
            try
            {
                if (asyncResult.Exception != null)
                {
                    throw asyncResult.Exception;
                }
                if (asyncResult.Exception != null)
                {
                    throw asyncResult.Exception.Flatten().InnerException;
                }

                this.Output.Set(context, asyncResult.Output.Cast<TResult>().ToList());
            }
            finally
            {
                executor.Dispose();
            }
        }

        protected override void Cancel(AsyncCodeActivityContext context)
        {
            var executor = context.UserState as PowerShellExecutor;
            if (executor != null)
            {
                executor.Stop();
                executor.Dispose();
            }
            base.Cancel(context);
        }
    }
}
