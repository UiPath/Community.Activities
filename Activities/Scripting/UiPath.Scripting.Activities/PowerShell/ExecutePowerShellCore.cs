using System;
using System.Activities;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using UiPath.Scripting.Activities.Properties;


namespace UiPath.Scripting.Activities
{
    /// <summary>
    /// Executes the powershell command or the powershell script.
    /// </summary>
    [LocalizedDisplayName(nameof(Resources.Activity_ExecutePowerShellCore_Name))]
    [LocalizedDescription(nameof(Resources.Activity_ExecutePowerShellCore_Description))]
    public partial class ExecutePowerShellCore : AsyncCodeActivity
    {
        private const string PowerShellGlobalVariableNamePrefix = "Global.";
        private const string InputArgumentName = "Input";
        private const string PipelineOutputArgumentName = "PipelineOutput";
        private const string ErrorsArgumentName = "Errors";
        private const string CommandTextArgumentName = "CommandText";
        private const string ContinueOnErrorArgumentName = "ContinueOnError";

        public ExecutePowerShellCore()
            : base()
        {
        }

        /// <summary>
        /// Specifies to continue executing the remaining activities even if the current activity failed. Only boolean values (True, False) are supported.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.Activity_ExecutePowerShellCore_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_ExecutePowerShellCore_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        /// <summary>
        /// The PowerShell command that is to be executed.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_ExecutePowerShellCore_Property_CommandText_Name))]
        [LocalizedDescription(nameof(Resources.Activity_ExecutePowerShellCore_Property_CommandText_Description))]
        public InArgument<string> CommandText { get; set; }

        /// <summary>
        /// A dictionary of PowerShell command parameters.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_ExecutePowerShellCore_Property_Parameters_Name))]
        [LocalizedDescription(nameof(Resources.Activity_ExecutePowerShellCore_Property_Parameters_Description))]
        public IDictionary<string, InArgument> Parameters { get; set; }

        /// <summary>
        /// A collection of PSObjects that are passed to the writer of the pipeline used to execute the command. Can be the output of another InvokePowerShellCore activity.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_ExecutePowerShellCore_Property_Input_Name))]
        [LocalizedDescription(nameof(Resources.Activity_ExecutePowerShellCore_Property_Input_Description))]
        public InArgument<Collection<PSObject>> Input { get; set; }

        /// <summary>
        /// A collection of TypeArguments objects returned by the execution of the command. Can be used to pipe several InvokePowerShellCore activities.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_InvokePowerShellCore_Property_Output_Name))]
        [LocalizedDescription(nameof(Resources.Activity_InvokePowerShellCore_Property_Output_Description))]
        public OutArgument<Collection<PSObject>> PipelineOutput { get; set; }

        /// <summary>
        /// A collection of ErrorRecords from the execution of the command.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Output))]
        public OutArgument<Collection<ErrorRecord>> Errors { get; set; }

        /// <summary>
        /// Specifies if the command text is a script.
        /// </summary>
        [LocalizedDisplayName(nameof(Resources.Activity_ExecutePowerShellCore_Property_IsScript_Name))]
        [LocalizedDescription(nameof(Resources.Activity_ExecutePowerShellCore_Property_IsScript_Description))]
        public bool IsScript { get; set; }

        /// <summary>
        /// A dictionary of named objects that represent variables used within the current session of the command. A PowerShell command can retrieve information from IN and In/Out variables and can set Out variables.
        /// </summary>
        [LocalizedDisplayName(nameof(Resources.Activity_ExecutePowerShellCore_Property_PowerShellVariables_Name))]
        [LocalizedDescription(nameof(Resources.Activity_ExecutePowerShellCore_Property_PowerShellVariables_Description))]
        public IDictionary<string, Argument> PowerShellVariables { get; set; }

        /// <summary>
        /// Called before workflow execution to inform the runtime about arguments.
        /// </summary>
        /// <param name="metadata"></param>
        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            Collection<RuntimeArgument> arguments = new Collection<RuntimeArgument>();

            // Overridding base.OnGetArguments to avoid reflection costs
            RuntimeArgument inputArgument = new RuntimeArgument(InputArgumentName, typeof(Collection<PSObject>), ArgumentDirection.In);
            metadata.Bind(this.Input, inputArgument);
            arguments.Add(inputArgument);

            RuntimeArgument pipelineOutputArgument = new RuntimeArgument(PipelineOutputArgumentName, typeof(Collection<PSObject>), ArgumentDirection.Out);
            metadata.Bind(this.PipelineOutput, pipelineOutputArgument);
            arguments.Add(pipelineOutputArgument);

            RuntimeArgument errorsArgument = new RuntimeArgument(ErrorsArgumentName, typeof(Collection<ErrorRecord>), ArgumentDirection.Out);
            metadata.Bind(this.Errors, errorsArgument);
            arguments.Add(errorsArgument);

            if (this.Parameters is not null &&  this.Parameters?.Count != 0)
            {
                AddDictionaryOfArguments<InArgument>(metadata, arguments, this.Parameters, string.Empty, true);
            }

            if (this.PowerShellVariables is not null && this.PowerShellVariables.Count != 0)
            {
                //Prefix variable name so the name is unique from runtime's point of view,
                //this is to handle the case where user might use the same name for variables and parameters
                AddDictionaryOfArguments<Argument>(metadata, arguments, this.PowerShellVariables, PowerShellGlobalVariableNamePrefix, false);
            }

            arguments.Add(new RuntimeArgument(CommandTextArgumentName, typeof(String), ArgumentDirection.In));
            arguments.Add(new RuntimeArgument(ContinueOnErrorArgumentName, typeof(bool), ArgumentDirection.In));
            metadata.SetArgumentsCollection(arguments);
        }

        /// <summary>
        /// Disposes the pipeline.
        /// </summary>
        /// <param name="pipelineInstance"></param>
        private void DisposePipeline(Pipeline pipelineInstance)
        {
            if (pipelineInstance != null)
            {
                pipelineInstance.Runspace.Close();
                pipelineInstance.Dispose();
            }
            pipelineInstance = null;
        }

        /// <summary>
        /// Called by the runtime to cancel the execution of this asynchronous activity.
        /// </summary>
        /// <param name="context"></param>
        protected override void Cancel(AsyncCodeActivityContext context)
        {
            Pipeline pipeline = context.UserState as Pipeline;
            if (pipeline != null)
            {
                pipeline.Stop();
                DisposePipeline(pipeline);
            }
            base.Cancel(context);
        }

        /// <summary>
        /// Called by the runtime to begin execution of this asynchronous activity.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            Runspace runspace = null;
            Pipeline pipeline = null;

            try
            {
                runspace = RunspaceFactory.CreateRunspace();
                runspace.Open();
                pipeline = runspace.CreatePipeline();
                var cmdTxt = this.CommandText.Get(context);
                Command cmd = new Command(cmdTxt, this.IsScript);
                if (this.Parameters != null)
                {
                    foreach (KeyValuePair<string, InArgument> parameter in this.Parameters)
                    {
                        if (parameter.Value.Expression != null)
                        {
                            cmd.Parameters.Add(parameter.Key, parameter.Value.Get(context));
                        }
                        else
                        {
                            cmd.Parameters.Add(parameter.Key, true);
                        }
                    }
                }
                if (this.PowerShellVariables != null)
                {
                    foreach (KeyValuePair<string, Argument> powerShellVariable in this.PowerShellVariables)
                    {
                        if ((powerShellVariable.Value.Direction == ArgumentDirection.In) || (powerShellVariable.Value.Direction == ArgumentDirection.InOut))
                        {
                            runspace.SessionStateProxy.SetVariable(powerShellVariable.Key, powerShellVariable.Value.Get(context));
                        }
                    }
                }
                pipeline.Commands.Add(cmd);
                IEnumerable pipelineInput = this.Input.Get(context);
                if (pipelineInput != null)
                {
                    foreach (object inputItem in pipelineInput)
                    {
                        pipeline.Input.Write(inputItem);
                    }
                }
                pipeline.Input.Close();

            }
            catch
            {
                if (runspace != null)
                {
                    runspace.Dispose();
                }

                if (pipeline != null)
                {
                    pipeline.Dispose();
                }
                if (!ContinueOnError.Get(context))
                {
                    throw;
                }
            }

            context.UserState = pipeline;
            return new PipelineInvokerAsyncResult(pipeline, callback, state);
        }

        /// <summary>
        /// Called by the runtime after execution of this asynchronous activity.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="result"></param>
        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            PipelineInvokerAsyncResult asyncResult = result as PipelineInvokerAsyncResult;
            Pipeline pipeline = context.UserState as Pipeline;

            if (asyncResult == null || pipeline == null) return;
            try
            {
                if (asyncResult.Exception != null)
                {
                    throw asyncResult.Exception;
                }
                if (asyncResult.ErrorRecords != null && asyncResult.ErrorRecords.Count > 0)
                {
                    if (asyncResult.ErrorRecords.Count == 1)
                    {
                        throw asyncResult.ErrorRecords[0].Exception;
                    }

                    string exceptionMessage = Resources.PowerShellExceptionMessage;
                    foreach (var error in asyncResult.ErrorRecords)
                    {
                        exceptionMessage += System.Environment.NewLine + error.Exception.Message;
                    }

                    throw new Exception(exceptionMessage);
                }
                this.PipelineOutput.Set(context, asyncResult.PipelineOutput);
                this.Errors.Set(context, asyncResult.ErrorRecords);

                foreach (KeyValuePair<string, Argument> entry in this.PowerShellVariables)
                {
                    if ((entry.Value.Direction == ArgumentDirection.Out) || (entry.Value.Direction == ArgumentDirection.InOut))
                    {
                        object value = pipeline.Runspace.SessionStateProxy.GetVariable(entry.Key);
                        entry.Value.Set(context, value);
                    }
                }
            }
            catch
            {
                if (!ContinueOnError.Get(context)) throw;
            }
            finally
            {
                DisposePipeline(pipeline);
            }
        }

        /// <summary>
        /// Adds bindings for dictionary entries
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="metadata"></param>
        /// <param name="arguments"></param>
        /// <param name="dictionary"></param>
        /// <param name="prefix"></param>
        /// <param name="isParameter"></param>
        private void AddDictionaryOfArguments<T>(CodeActivityMetadata metadata, IList<RuntimeArgument> arguments,
            IDictionary<string, T> dictionary, string prefix, bool isParameter) where T : Argument
        {
            foreach (KeyValuePair<string, T> entry in dictionary)
            {
                Argument binding = entry.Value;
                if (binding.Expression != null)
                {
                    RuntimeArgument argument;
                    if (isParameter && binding.ArgumentType == typeof(bool))
                    {
                        //InArgument<bool> with null expression specifies a switch parameter.
                        //Otherwise all the InArguments in Parameters collection cannot have null expression.
                        argument = new RuntimeArgument(prefix + entry.Key, binding.ArgumentType, binding.Direction, false);
                    }
                    else
                    {
                        argument = new RuntimeArgument(prefix + entry.Key, binding.ArgumentType, binding.Direction, true);
                    }
                    metadata.Bind(binding, argument);

                    arguments.Add(argument);
                }
            }
        }
    }
}
