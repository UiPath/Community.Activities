using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using UiPath.Scripting.Activities.NetCore.ViewModels;

namespace UiPath.Scripting.Activities
{
    [ViewModelClass(typeof(InvokePowerShellCoreViewModel))]
    public sealed partial class InvokePowerShellCore<T>
    {

    }
}

namespace UiPath.Scripting.Activities.NetCore.ViewModels
{
    internal class InvokePowerShellCoreViewModel : DesignPropertiesViewModel
    {
        /// <summary>
        /// Basic Constructor
        /// </summary>
        /// <param name="services"></param>
        public InvokePowerShellCoreViewModel(IDesignServices services) : base(services)
        {

        }

        /// <summary>
        /// A dictionary of PowerShell command parameters.
        /// </summary>
        //public DesignProperty<Dictionary<string, DesignInArgument<string>>> Parameters = new DesignProperty<Dictionary<string, DesignInArgument<string>>>();

        /// <summary>
        /// A dictionary of named objects that represent variables used within the current session of the command. A PowerShell command can retrieve information from IN and In/Out variables and can set Out variables.
        /// </summary>
        //public DesignProperty<Dictionary<string, DesignInOutArgument<string>>> PowerShellVariables = new DesignProperty<Dictionary<string, DesignInOutArgument<string>>>();

        /// <summary>
        /// The PowerShell command or script that is to be executed
        /// </summary>
        public DesignInArgument<string> CommandText { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// A collection of PSObjects that are passed to the writer of the pipeline used to execute the command. Can be the output of another InvokePowerShellCore activity.
        /// </summary>
        //public DesignInArgument<Collection<PSObject>> Input { get; set; } = new DesignInArgument<Collection<PSObject>>();


        /// <summary>
        /// A collection of ErrorRecords from the execution of the command.
        /// </summary>
        //private DesignOutArgument<Collection<ErrorRecord>> Errors { get; set; } = new DesignOutArgument<Collection<ErrorRecord>>();

        /// <summary>
        /// Specifies if the command text is a script.
        /// </summary>
        //public DesignProperty<bool> IsScript { get; set; } = new DesignProperty<bool>();

        /// <summary>
        /// A collection of TypeArguments objects returned by the execution of the command. Can be used to pipe several InvokePowerShellCore activities.
        /// </summary>
        //public DesignOutArgument<Collection<T>> Output { get; set; } = new DesignOutArgument<Collection<T>>();

        /// <summary>
        /// Specifies to continue executing the remaining activities even if the current activity failed. Only boolean values (True, False) are supported.
        /// </summary>
        //public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var propertyOrderIndex = 1;

            //CommandText.IsPrincipal = true;
            CommandText.OrderIndex = propertyOrderIndex++;
            //CommandText.Widget = new DefaultWidget { Type = ViewModelWidgetType.Text };

            //Input.IsPrincipal = true;
            //Input.OrderIndex = propertyOrderIndex++;
            //Input.Widget = new DefaultWidget { Type = ViewModelWidgetType.Collection };

            //Parameters.IsPrincipal = true;
            //Parameters.OrderIndex = propertyOrderIndex++;

            //IsScript.IsPrincipal = true;
            //IsScript.OrderIndex = propertyOrderIndex++;
            //IsScript.Widget = new DefaultWidget { Type = ViewModelWidgetType.NullableBoolean };

            //PowerShellVariables.IsPrincipal = true;
            //PowerShellVariables.OrderIndex = propertyOrderIndex++;

            //Output.IsPrincipal = true;
            //Output.OrderIndex = propertyOrderIndex++;
            //Output.Widget = new DefaultWidget { Type = ViewModelWidgetType.Collection };
        }

        protected override void InitializeRules()
        {
            base.InitializeRules();
        }

        protected override void ManualRegisterDependencies()
        {
            base.ManualRegisterDependencies();
        }
    }
}
