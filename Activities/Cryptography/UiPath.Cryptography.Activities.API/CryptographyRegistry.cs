using System;
using System.Activities;
using System.Collections.Generic;
using UiPath.CodedWorkflows;
using UiPath.Cryptography.Activities.API;

[assembly: CodedWorkflowsServiceRegistryAttribute(typeof(CryptographyRegistry))]
namespace UiPath.Cryptography.Activities.API
{
    internal class CryptographyRegistry : ICodedWorkflowsServiceRegistry
    {
        public IDictionary<string, Type> AutoImportedTypes => new Dictionary<string, Type> { { "cryptography", typeof(ICryptographyService) } };

        public IEnumerable<string> AutoImportedNamespaces => new[] { "System", "System.IO", "System.Text", "UiPath.Cryptography", "UiPath.Cryptography.Activities", "UiPath.Cryptography.Activities.API", "UiPath.Cryptography.Enums" };

        public void Register(ICodedWorkflowsServiceLocator serviceLocator, ActivityContext initialActivityContext)
        {
            serviceLocator.RegisterType<ICryptographyService, CryptographyService>(CodeServiceRegistrationType.Singleton);
        }
    }
}
