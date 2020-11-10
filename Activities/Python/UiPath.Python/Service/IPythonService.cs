using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace UiPath.Python.Service
{
    [ServiceContract(Namespace = "http://UiPath.Python.Host")]
    [ServiceKnownType(typeof(object[]))]
    public interface IPythonService
    {
        [OperationContract]
        void Initialize(string path, string workingFolder);

        [OperationContract]
        void Shutdown();

        [OperationContract]
        void Execute(string code);

        [OperationContract]
        Guid LoadScript(string code);

        [OperationContract]
        Guid InvokeMethod(Guid instance, string method, IEnumerable<Argument> args);

        [OperationContract]
        Argument Convert(Guid obj, string t);
    }
}
