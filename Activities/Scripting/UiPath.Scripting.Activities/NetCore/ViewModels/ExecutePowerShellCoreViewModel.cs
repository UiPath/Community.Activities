using System;
using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using UiPath.Scripting.Activities.NetCore.ViewModels;

namespace UiPath.Scripting.Activities
{
    /// <summary>
    /// Executes the powershell command or the powershell script.
    /// </summary>
    [ViewModelClass(typeof(ExecutePowerShellCoreViewModel))]
    public partial class ExecutePowerShellCore
    {

    }
}

namespace UiPath.Scripting.Activities.NetCore.ViewModels
{
    internal class ExecutePowerShellCoreViewModel : DesignPropertiesViewModel
    {
        /// <summary>
        /// Specifies to continue executing the remaining activities even if the current activity failed. Only boolean values (True, False) are supported.
        /// </summary>
        public DesignInArgument<bool> ContinueOnError { get; set; }

        /// <summary>
        /// The PowerShell command that is to be executed.
        /// </summary>
        public DesignInArgument<string> CommandText { get; set; }

        /// <summary>
        /// A dictionary of PowerShell command parameters.
        /// </summary>
        public DesignProperty<IDictionary<string, InArgument>> Parameters { get; set; }

        /// <summary>
        /// A collection of PSObjects that are passed to the writer of the pipeline used to execute the command. Can be the output of another InvokePowerShellCore activity.
        /// </summary>
        //public DesignInArgument<Collection<PSObject>> Input { get; set; }

        /// <summary>
        /// A collection of TypeArguments objects returned by the execution of the command. Can be used to pipe several InvokePowerShellCore activities.
        /// </summary>
        //public DesignOutArgument<Collection<PSObject>> PipelineOutput { get; set; }

        /// <summary>
        /// A collection of ErrorRecords from the execution of the command.
        /// </summary>
        //public DesignOutArgument<Collection<ErrorRecord>> Errors { get; set; }

        /// <summary>
        /// Specifies if the command text is a script.
        /// </summary>
        public DesignProperty<bool> IsScript { get; set; }

        /// <summary>
        /// A dictionary of named objects that represent variables used within the current session of the command. A PowerShell command can retrieve information from IN and In/Out variables and can set Out variables.
        /// </summary>
        public DesignProperty<IDictionary<string, Argument>> PowerShellVariables { get; set; }

        public ExecutePowerShellCoreViewModel(IDesignServices services) : base(services)
        {
        }

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var propertyOrderIndex = 1;

            CommandText.IsPrincipal = true;
            CommandText.OrderIndex = propertyOrderIndex++;

            IsScript.OrderIndex = propertyOrderIndex++;

            ContinueOnError.OrderIndex = propertyOrderIndex++;

            Parameters.OrderIndex = propertyOrderIndex++;

            PowerShellVariables.OrderIndex = propertyOrderIndex++;

            //Input.OrderIndex = propertyOrderIndex++;
        }
    }
}
