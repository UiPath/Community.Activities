﻿using System;
using System.Collections.Generic;
using System.ServiceModel;
using UiPath.Shared.Service.Client;

namespace UiPath.Python.Service
{
    [ServiceContract(Namespace = "http://UiPath.Python.Host")]
    internal class PythonProxy : Proxy<IPythonService>, IPythonService
    {
        internal PythonProxy(string endpoint, double timeout) : base(endpoint,
                                                                     timeout)
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

        public void Initialize(string path, Version version, string workingFolder)
        {
            _channel.Initialize(path, version, workingFolder);
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
