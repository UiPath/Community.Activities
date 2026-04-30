using System;
using System.Activities.DesignViewModels;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using UiPath.Python;
using Resources = UiPath.Python.Activities.Properties.UiPath_Python_Activities;

namespace UiPath.Activities.Python.ViewModels
{
    [ExcludeFromCodeCoverage]
    class InvokeMethodViewModel : DesignPropertiesViewModel
    {
        public InvokeMethodViewModel(IDesignServices services) : base(services)
        {
        }

        public DesignInArgument<PythonObject> Instance { get; set; }

        public DesignInArgument<string> Name { get; set; }

        public DesignInArgument<IEnumerable<object>> Parameters { get; set; }

        public DesignOutArgument<PythonObject> Result { get; set; }

        protected override void InitializeModel()
        {
            base.InitializeModel();

            var orderIndex = 0;
            Instance.OrderIndex = orderIndex++;
            Name.OrderIndex = orderIndex++;
            Parameters.OrderIndex = orderIndex++;
            Result.OrderIndex = orderIndex++;

            Instance.DisplayName = Resources.InstanceNameDisplayName;
            Instance.Tooltip = Resources.InstanceDescription;
            Instance.Category = Resources.Input;

            Name.DisplayName = Resources.NameDisplayName;
            Name.Tooltip = Resources.MethodNameDescription;
            Name.Category = Resources.Input;
            Name.IsRequired = true;
            Name.IsPrincipal = true;

            Parameters.DisplayName = Resources.ParametersNameDisplayName;
            Parameters.Tooltip = Resources.ParametersDescription;
            Parameters.Category = Resources.Input;

            Result.DisplayName = Resources.ResultNameDisplayName;
            Result.Tooltip = Resources.ResultDescription;
            Result.Category = Resources.Output;
        }

    }
}
