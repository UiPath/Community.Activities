using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Management.Automation;
using UiPath.Scripting.Activities.Properties;

namespace UiPath.Scripting.Activities
{
    /// <summary>
    /// Invokes the powershell by sending the propeties to the ExecutePowershellCore.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    [LocalizedDisplayName(nameof(Resources.Activity_InvokePowerShellCore_Name))]
    [LocalizedDescription(nameof(Resources.Activity_InvokePowerShellCore_Description))]
    public partial class InvokePowerShellCore<T> : Activity
    {
        private Dictionary<string, InArgument> parameters;
        private Dictionary<string, Argument> powerShellVariables;
        private Dictionary<string, InArgument> childParameters;
        private Dictionary<string, Argument> childPowerShellVariables;

        /// <summary>
        /// A dictionary of PowerShell command parameters.
        /// </summary>
        [Browsable(true)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_InvokePowerShellCore_Property_Parameters_Name))]
        [LocalizedDescription(nameof(Resources.Activity_InvokePowerShellCore_Property_Parameters_Description))]
        public Dictionary<string, InArgument> Parameters
        {
            get
            {
                if (this.parameters == null)
                {
                    this.parameters = new Dictionary<string, InArgument>();
                }
                return this.parameters;
            }
        }

        /// <summary>
        /// A dictionary of named objects that represent variables used within the current session of the command. A PowerShell command can retrieve information from IN and In/Out variables and can set Out variables.
        /// </summary>
        [Browsable(true)]
        [LocalizedDisplayName(nameof(Resources.Activity_InvokePowerShellCore_Property_PowerShellVariables_Name))]
        [LocalizedDescription(nameof(Resources.Activity_InvokePowerShellCore_Property_PowerShellVariables_Description))]
        public Dictionary<string, Argument> PowerShellVariables
        {
            get
            {
                if (this.powerShellVariables == null)
                {
                    this.powerShellVariables = new Dictionary<string, Argument>();
                }
                return this.powerShellVariables;
            }
            set => this.powerShellVariables = value;
        }

        /// <summary>
        /// The PowerShell command or script that is to be executed
        /// </summary>
        [LocalizedCategory(nameof(Resources.Input))]
        [DefaultValue(null)]
        [LocalizedDisplayName(nameof(Resources.Activity_InvokePowerShellCore_Property_CommandText_Name))]
        [LocalizedDescription(nameof(Resources.Activity_InvokePowerShellCore_Property_CommandText_Description))]
        public InArgument<string> CommandText { get; set; }

        /// <summary>
        /// A collection of PSObjects that are passed to the writer of the pipeline used to execute the command. Can be the output of another InvokePowerShellCore activity.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Input))]
        [DefaultValue(null)]
        [LocalizedDisplayName(nameof(Resources.Activity_InvokePowerShellCore_Property_Input_Name))]
        [LocalizedDescription(nameof(Resources.Activity_InvokePowerShellCore_Property_Input_Description))]
        public InArgument<Collection<PSObject>> Input { get; set; }

        /// <summary>
        /// A collection of ErrorRecords from the execution of the command.
        /// </summary>
        [Category("Output")]
        [DefaultValue(null)]
        private OutArgument<Collection<ErrorRecord>> Errors { get; set; }

        /// <summary>
        /// Specifies if the command text is a script.
        /// </summary>
        [DefaultValue(false)]
        [LocalizedDisplayName(nameof(Resources.Activity_InvokePowerShellCore_Property_IsScript_Name))]
        [LocalizedDescription(nameof(Resources.Activity_InvokePowerShellCore_Property_IsScript_Description))]
        public bool IsScript { get; set; }

        /// <summary>
        /// A collection of TypeArguments objects returned by the execution of the command. Can be used to pipe several InvokePowerShellCore activities.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Output))]
        [DefaultValue(null)]
        [LocalizedDisplayName(nameof(Resources.Activity_InvokePowerShellCore_Property_Output_Name))]
        [LocalizedDescription(nameof(Resources.Activity_InvokePowerShellCore_Property_Output_Description))]
        public OutArgument<Collection<T>> Output { get; set; }

        /// <summary>
        /// Specifies to continue executing the remaining activities even if the current activity failed. Only boolean values (True, False) are supported.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.Activity_InvokePowerShellCore_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_InvokePowerShellCore_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        public InvokePowerShellCore()
            : base()
        {
            DelegateOutArgument<T> result = new DelegateOutArgument<T>() { Name = "result" };
            DelegateInArgument<PSObject> psObject = new DelegateInArgument<PSObject>() { Name = "psObject" };

            this.childParameters = new Dictionary<string, InArgument>();
            this.childPowerShellVariables = new Dictionary<string, Argument>();
            this.Implementation = new Func<Activity>(this.CreateBody);
        }

        /// <summary>
        /// Binds the activity arguments.
        /// </summary>
        /// <param name="metadata"></param>
        protected override void CacheMetadata(ActivityMetadata metadata)
        {
            IPSHelper.CacheMetadataHelper(metadata, this.Input, this.Errors, this.CommandText, GetType().Name,
                this.DisplayName, this.PowerShellVariables, this.Parameters, this.childPowerShellVariables, this.childParameters);

            RuntimeArgument outputArgument = new RuntimeArgument("Output", typeof(Collection<T>), ArgumentDirection.Out, false);
            metadata.Bind(this.Output, outputArgument);
            metadata.AddArgument(new RuntimeArgument("CommandText", typeof(String), ArgumentDirection.In));
            metadata.AddArgument(new RuntimeArgument("ContinueOnError", typeof(bool), ArgumentDirection.In));
            metadata.AddArgument(outputArgument);
        }

        /// <summary>
        /// Creates the body for ExecutePowerShellCore.
        /// </summary>
        /// <returns></returns>
        private Activity CreateBody()
        {
            Variable<Collection<PSObject>> psObjects = new Variable<Collection<PSObject>>();
            Variable<T> outputObject = new Variable<T>();
            DelegateInArgument<PSObject> psObject = new DelegateInArgument<PSObject>();
            Variable<Collection<T>> outputObjects = new Variable<Collection<T>>() { Default = new New<Collection<T>>(), };
            DelegateInArgument<Exception> exception = new DelegateInArgument<Exception>();

            return new TryCatch
            {
                Try = new Sequence
                {
                    Variables = { psObjects, outputObjects },
                    Activities =
                    {
                        new ExecutePowerShellCore
                        {
                            CommandText = new ArgumentValue<string> { ArgumentName = "CommandText" },
                            Parameters = this.childParameters,
                            PowerShellVariables = this.childPowerShellVariables,
                            //PipelineOutput = psObjects,
                            //Errors = new ArgumentReference<Collection<ErrorRecord>> { ArgumentName = "Errors" },
                            IsScript = this.IsScript,
                            //Input = new ArgumentValue<Collection<PSObject>> { ArgumentName = "Input" },
                            ContinueOnError = new ArgumentValue<bool> { ArgumentName = "ContinueOnError" }
                        },
                        new If
                        {
                            Condition = psObjects != null,
                            Then = new Sequence
                            {
                                Activities =
                                {
                                    new ForEach<PSObject>
                                    {
                                        Values = psObjects,
                                        Body = new ActivityAction<PSObject>
                                        {
                                            Argument = psObject,
                                            Handler = new AddToCollection<T>
                                            {
                                                Collection = outputObjects,
                                                Item = new InArgument<T>(ctx => (T) psObject.Get(ctx).BaseObject)
                                            }
                                        }
                                    },
                                    new Assign<Collection<T>>
                                    {
                                        To = new OutArgument<Collection<T>>(ctx => this.Output.Get(ctx)),
                                        Value = outputObjects
                                    }
                                }
                            }
                        }
                    }
                },
                Catches =
                {
                    new Catch<Exception>
                    {
                        Action = new ActivityAction<Exception>
                        {
                            Argument = exception,
                            Handler = new If
                            {
                               Condition = ExpressionServices.Convert<bool>(context => ContinueOnError.Get(context)),
                               Else = new Throw
                                {
                                    Exception =  new LambdaValue<Exception>(context => exception.Get(context))
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
