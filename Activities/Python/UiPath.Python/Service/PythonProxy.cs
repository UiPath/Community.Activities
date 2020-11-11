using System;
using System.Collections.Generic;
using System.ServiceModel;
using UiPath.Shared.Service.Client;

namespace UiPath.Python.Service
{
    [ServiceContract(Namespace = "http://UiPath.Python.Host")]
    internal class PythonProxy : Proxy<IPythonService>, IPythonService
    {
        internal PythonProxy(string endpoint) : base(endpoint)
        {
        }

        #region Service methods
        public Argument Convert(Guid obj, string t)
        {
            return _channel.Convert(obj, t);
        }

        public void Execute(string code)
        {
            _channel.Execute(code);
        }

        public void Initialize(Version version, string path, string workingFolder)
        {
            _channel.Initialize(version, path, workingFolder);
        }

        public Guid InvokeMethod(Guid instance, string method, IEnumerable<Argument> args)
        {
            return _channel.InvokeMethod(instance, method, args);
        }

        public Guid LoadScript(string code)
        {
            return _channel.LoadScript(code);
        }

        public void Shutdown()
        {
            _channel.Shutdown();
        }
        #endregion
    }
}
