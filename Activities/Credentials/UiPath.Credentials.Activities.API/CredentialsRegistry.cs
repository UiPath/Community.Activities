using System;
using System.Activities;
using System.Collections.Generic;
using UiPath.CodedWorkflows;
using UiPath.Credentials.Activities.API;

[assembly: CodedWorkflowsServiceRegistryAttribute(typeof(CredentialsRegistry))]
namespace UiPath.Credentials.Activities.API
{
    internal class CredentialsRegistry : ICodedWorkflowsServiceRegistry
    {
        public IDictionary<string, Type> AutoImportedTypes => new Dictionary<string, Type> { { "credentials", typeof(ICredentialsService) } };

        public IEnumerable<string> AutoImportedNamespaces => new[] { "System", "System.Security", "CredentialManagement", "UiPath.Credentials.Activities", "UiPath.Credentials.Activities.API", "UiPath.Credentials.Activities.API.Models" };

        public void Register(ICodedWorkflowsServiceLocator serviceLocator, ActivityContext initialActivityContext)
        {
            serviceLocator.RegisterType<ICredentialsService, CredentialsService>(CodeServiceRegistrationType.Singleton);
        }
    }
}
