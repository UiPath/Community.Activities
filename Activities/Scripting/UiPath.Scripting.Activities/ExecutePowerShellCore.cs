using System.Activities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace UiPath.Scripting.Activities
{
    public sealed class ExecutePowerShellCore : CodeActivity
    {
        public OutArgument<int> ProcessesCount { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var count = CountProcesses();

            ProcessesCount.Set(context, count);
        }

        private int CountProcesses()
        {
            var command = "(ps).Count";

            var result = Execute(command, true, null, null);

            return (int)result[0].BaseObject;
        }

        private Collection<PSObject> Execute(string commandText, bool isScript, IDictionary<string, object> parameters, IDictionary<string, object> variables)
        {
            Pipeline pipeline = null;

            try
            {
                var runspace = RunspaceFactory.CreateRunspace();

                runspace.Open();

                pipeline = runspace.CreatePipeline();

                var command = new Command(commandText, isScript);

                if (parameters != null)
                {
                    foreach (var parameter in parameters)
                    {
                        command.Parameters.Add(parameter.Key, parameter.Value);
                    }
                }

                if (variables != null)
                {
                    foreach (var variable in variables)
                    {
                        runspace.SessionStateProxy.SetVariable(variable.Key, variable.Value);
                    }
                }

                pipeline.Commands.Add(command);

                var result = pipeline.Invoke();

                return result;
            }
            finally
            {
                if (pipeline != null)
                {
                    pipeline.Runspace.Close();
                    pipeline.Dispose();
                }
            }
        }
    }
}