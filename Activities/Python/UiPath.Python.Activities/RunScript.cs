using System;
using System.Activities;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Python.Activities.Properties;

namespace UiPath.Python.Activities
{
    /// <summary>
    /// Activity for executing a Python script
    /// </summary>
    [LocalizedDisplayName(nameof(Resources.RunScriptNameDisplayName))]
    [LocalizedDescription(nameof(Resources.RunScriptDescription))]
    public class RunScript : PythonActivity
    {
        [RequiredArgument]
        [OverloadGroup("Script File")]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.ScriptFileNameDisplayName))]
        [LocalizedDescription(nameof(Resources.ScriptFileDescription))]
        public InArgument<string> ScriptFile { get; set; }

        [RequiredArgument]
        [OverloadGroup("Code")]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.CodeNameDisplayName))]
        [LocalizedDescription(nameof(Resources.CodeDescription))]
        public InArgument<string> Code { get; set; }

        protected async override Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            IEngine pythonEngine = PythonScope.GetPythonEngine(context);

            string scriptFile = ScriptFile.Get(context);
            string scriptCode = Code.Get(context);
            //IEnumerable<object> parameters = Parameters.Get(context);

            // safeguard checks
            if (scriptFile.IsNullOrEmpty() && scriptCode.IsNullOrEmpty())
            {
                throw new InvalidOperationException(Resources.NoScriptSpecifiedException);
            }
            if (!scriptFile.IsNullOrEmpty() && !File.Exists(scriptFile))
            {
                throw new FileNotFoundException(Resources.ScriptFileNotFoundException, scriptFile);
            }

            // load script from file if not specified
            if (scriptCode.IsNullOrEmpty())
            {
                scriptCode = File.ReadAllText(ScriptFile.Get(context));
            }

            try
            {
                await pythonEngine.Execute(scriptCode, cancellationToken);
            }
            catch (Exception e)
            {
                Trace.TraceError($"Error running Python script: {e.ToString()}");
                throw new InvalidOperationException(Resources.RunScriptException, e);
            }

            return (asyncCodeActivityContext) => 
            {
            };
        }
    }
}
