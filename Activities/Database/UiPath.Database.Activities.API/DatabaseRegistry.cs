using System;
using System.Activities;
using System.Collections.Generic;
using UiPath.CodedWorkflows;
using UiPath.Database.Activities.API;

[assembly: CodedWorkflowsServiceRegistryAttribute(typeof(DatabaseRegistry))]
namespace UiPath.Database.Activities.API
{
    internal class DatabaseRegistry : ICodedWorkflowsServiceRegistry
    {
        public IDictionary<string, Type> AutoImportedTypes => new Dictionary<string, Type> { { "database", typeof(IDatabaseService) } };

        public IEnumerable<string> AutoImportedNamespaces => new[] { "System", "System.Collections.Generic", "System.Data", "UiPath.Database", "UiPath.Database.Activities", "UiPath.Database.Activities.API", "UiPath.Database.Activities.API.Models" };

        public void Register(ICodedWorkflowsServiceLocator serviceLocator, ActivityContext initialActivityContext)
        {
            serviceLocator.RegisterType<IDatabaseService, DatabaseService>(CodeServiceRegistrationType.Singleton);
        }
    }
}
