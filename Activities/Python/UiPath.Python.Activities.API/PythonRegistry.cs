using System;
using System.Activities;
using System.Collections.Generic;
using UiPath.CodedWorkflows;
using UiPath.Python.Activities.API;

[assembly: CodedWorkflowsServiceRegistryAttribute(typeof(PythonRegistry))]
namespace UiPath.Python.Activities.API
{
    internal class PythonRegistry : ICodedWorkflowsServiceRegistry
    {
        public IDictionary<string, Type> AutoImportedTypes => new Dictionary<string, Type> { { "python", typeof(IPythonService) } };

        public IEnumerable<string> AutoImportedNamespaces => new[] { "System", "System.Collections.Generic", "UiPath.Python", "UiPath.Python.Activities", "UiPath.Python.Activities.API", "UiPath.Python.Activities.API.Models" };

        public void Register(ICodedWorkflowsServiceLocator serviceLocator, ActivityContext initialActivityContext)
        {
            serviceLocator.RegisterType<IPythonService, PythonService>(CodeServiceRegistrationType.Singleton);
        }
    }
}
