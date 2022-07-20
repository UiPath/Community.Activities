using System;
using System.Activities;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using UiPath.Scripting.Activities.Properties;

namespace UiPath.Scripting.Activities.PowerShell
{
    [LocalizedDisplayName(nameof(Resources.Activity_InvokePowerShellCore_Name))]
    [LocalizedDescription(nameof(Resources.Activity_InvokePowerShellCore_Description))]
    public partial class InvokePowerShellCore<TResult> : CodeActivity
    {
        private const string PowerShellGlobalVariableNamePrefix = "Global.";

        #region Properties
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_InvokePowerShellCore_Property_CommandText_Name))]
        [LocalizedDescription(nameof(Resources.Activity_InvokePowerShellCore_Property_CommandText_Description))]
        public InArgument<string> CommandText { get; set; }

        [DefaultValue(false)]
        [LocalizedCategory(nameof(Resources.Misc))]
        [LocalizedDisplayName(nameof(Resources.Activity_InvokePowerShellCore_Property_IsScript_Name))]
        [LocalizedDescription(nameof(Resources.Activity_InvokePowerShellCore_Property_IsScript_Description))]
        public bool IsScript { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_InvokePowerShellCore_Property_Parameters_Name))]
        [LocalizedDescription(nameof(Resources.Activity_InvokePowerShellCore_Property_Parameters_Description))]
        public IDictionary<string, InArgument> Parameters { get; set; }

        [LocalizedCategory(nameof(Resources.Misc))]
        [LocalizedDisplayName(nameof(Resources.Activity_InvokePowerShellCore_Property_PowerShellVariables_Name))]
        [LocalizedDescription(nameof(Resources.Activity_InvokePowerShellCore_Property_PowerShellVariables_Description))]
        public InArgument<IDictionary<string, Argument>> PowerShellVariables { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_InvokePowerShellCore_Property_Input_Name))]
        [LocalizedDescription(nameof(Resources.Activity_InvokePowerShellCore_Property_Input_Description))]
        public InArgument<Collection<PSObject>> Input { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_InvokePowerShellCore_Property_Output_Name))]
        [LocalizedDescription(nameof(Resources.Activity_InvokePowerShellCore_Property_Output_Description))]
        public OutArgument<string> Output { get; set; }

        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.Activity_InvokePowerShellCore_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_InvokePowerShellCore_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }
        #endregion

        public InvokePowerShellCore() : base()
        {

        }

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

        /// <summary>
        /// Called before workflow execution to inform the runtime about arguments.
        /// </summary>
        /// <param name="metadata"></param>
        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

            Collection<RuntimeArgument> arguments = new Collection<RuntimeArgument>();

            // Overridding base.OnGetArguments to avoid reflection costs
            RuntimeArgument inputArgument = new RuntimeArgument("Input", typeof(Collection<PSObject>), ArgumentDirection.In);
            metadata.Bind(this.Input, inputArgument);
            arguments.Add(inputArgument);

            if (this.Parameters.Count != 0)
            {
                AddDictionaryOfArguments<InArgument>(metadata, arguments, this.Parameters, string.Empty, true);
            }

            //if (this.PowerShellVariables.Count != 0)
            //{
            //    //Prefix variable name so the name is unique from runtime's point of view,
            //    //this is to handle the case where user might use the same name for variables and parameters
            //    AddDictionaryOfArguments<Argument>(metadata, arguments, this.PowerShellVariables, PowerShellGlobalVariableNamePrefix, false);
            //}

            arguments.Add(new RuntimeArgument("CommandText", typeof(String), ArgumentDirection.In));
            arguments.Add(new RuntimeArgument("ContinueOnError", typeof(bool), ArgumentDirection.In));
            metadata.SetArgumentsCollection(arguments);
        }

        /// <summary>
        /// // Called by the runtime to begin execution of this asynchronous activity.
        /// </summary>
        /// <param name="context"></param>
        protected override void Execute(CodeActivityContext context)
        {
            try
            {
                var command = CommandText.Get(context);
                var isScript = IsScript;
                var parameters = Parameters;
                var powerShellVariables = PowerShellVariables.Get(context);
                var input = Input.Get(context);

                var result = Execute(context, command, isScript, parameters, powerShellVariables);

                Output.Set(context, result[0]?.BaseObject?.ToString());
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                if (!ContinueOnError.Get(context)) throw;
            }
        }

        private Collection<PSObject> Execute(CodeActivityContext context, string commandText, bool isScript, IDictionary<string, InArgument> parameters, IDictionary<string, Argument> variables)
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
                    foreach (KeyValuePair<string, InArgument> parameter in parameters)
                    {
                        if (parameter.Value.Expression != null)
                        {
                            command.Parameters.Add(parameter.Key, parameter.Value.Get(context));
                        }
                        else
                        {
                            command.Parameters.Add(parameter.Key, true);
                        }
                    }
                }

                if (variables != null)
                {
                    foreach (KeyValuePair<string, Argument> variable in variables)
                    {
                        if ((variable.Value.Direction == ArgumentDirection.In) || (variable.Value.Direction == ArgumentDirection.InOut))
                        {
                            runspace.SessionStateProxy.SetVariable(variable.Key, variable.Value.Get(context));
                        }
                    }
                }

                pipeline.Commands.Add(command);

                IEnumerable pipelineInput = this.Input.Get(context);
                if (pipelineInput != null)
                {
                    foreach (object inputItem in pipelineInput)
                    {
                        pipeline.Input.Write(inputItem);
                    }
                }
                pipeline.Input.Close();

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