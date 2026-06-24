using System;
using System.Activities;
using System.Collections.Generic;
using UiPath.CodedWorkflows;
using UiPath.Java.Activities.API;

[assembly: CodedWorkflowsServiceRegistryAttribute(typeof(JavaRegistry))]
namespace UiPath.Java.Activities.API
{
    internal class JavaRegistry : ICodedWorkflowsServiceRegistry
    {
        public IDictionary<string, Type> AutoImportedTypes => new Dictionary<string, Type> { { "java", typeof(IJavaService) } };

        public IEnumerable<string> AutoImportedNamespaces => new[] { "System", "System.Collections.Generic", "UiPath.Java", "UiPath.Java.Activities", "UiPath.Java.Activities.API", "UiPath.Java.Activities.API.Models" };

        public void Register(ICodedWorkflowsServiceLocator serviceLocator, ActivityContext initialActivityContext)
        {
            serviceLocator.RegisterType<IJavaService, JavaService>(CodeServiceRegistrationType.Singleton);
        }
    }
}
