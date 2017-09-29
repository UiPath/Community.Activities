using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace UiPath.Script.Powershell
{
    public class PowerShellExecutor : IDisposable
    {
        private Runspace runspace;

        private Pipeline pipeline;

        public PipelineInvokerAsyncResult ExecuteScript(string scriptPath, List<KeyValuePair<string, object>> parameters, AsyncCallback callback, object state)
        {
            // create Powershell runspace
            try

            {
                runspace = RunspaceFactory.CreateRunspace();
                runspace.Open();

                RunspaceInvoke runSpaceInvoker = new RunspaceInvoke(runspace);

                //runSpaceInvoker.Invoke("Set-ExecutionPolicy Unrestricted");

                // create a pipeline and feed it the script text
                pipeline = runspace.CreatePipeline();
                var scriptContent = File.ReadAllText(scriptPath);
                Command command = new Command(scriptContent, true);
                foreach (var param in parameters)
                {
                    command.Parameters.Add(param.Key, param.Value);
                }
                pipeline.Commands.Add(command);
            }
            catch
            {
                Dispose();
                throw;
            }

            return new PipelineInvokerAsyncResult(pipeline, callback, state);
        }

        public void Stop()
        {
            if (pipeline != null)
            {
                pipeline.Stop();
            }
        }

        public void Dispose()
        {
            if (pipeline != null)
            {
                pipeline.Stop();
                pipeline.Dispose();
                pipeline = null;
            }
            if (runspace != null)
            {
                runspace.Close();
                runspace.Dispose();
                runspace = null;
            }
        }
    }
}
