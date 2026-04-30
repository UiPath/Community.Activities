using System;
using System.Activities;
using System.Collections.Generic;
using UiPath.CodedWorkflows;
using UiPath.FTP.Activities.API;

[assembly: CodedWorkflowsServiceRegistryAttribute(typeof(FtpRegistry))]
namespace UiPath.FTP.Activities.API
{
    internal class FtpRegistry : ICodedWorkflowsServiceRegistry
    {
        public IDictionary<string, Type> AutoImportedTypes => new Dictionary<string, Type> { { "ftp", typeof(IFtpService) } };

        public IEnumerable<string> AutoImportedNamespaces => new[] { "System", "System.Collections.Generic", "UiPath.FTP", "UiPath.FTP.Activities", "UiPath.FTP.Activities.API", "UiPath.FTP.Activities.API.Models" };

        public void Register(ICodedWorkflowsServiceLocator serviceLocator, ActivityContext initialActivityContext)
        {
            serviceLocator.RegisterType<IFtpService, FtpService>(CodeServiceRegistrationType.Singleton);
        }
    }
}
