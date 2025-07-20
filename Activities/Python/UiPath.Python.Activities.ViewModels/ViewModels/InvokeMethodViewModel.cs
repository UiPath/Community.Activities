using System;
using System.Activities.DesignViewModels;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using UiPath.Python;

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
    }
}
