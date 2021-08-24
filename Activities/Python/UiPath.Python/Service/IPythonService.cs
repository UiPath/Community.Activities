using System;
using System.Collections.Generic;

namespace UiPath.Python.Service
{
    public interface IPythonService
    {
        void Initialize(string path, Version version, string workingFolder);

        void Shutdown();

        void Execute(string code);

        Guid LoadScript(string code);

        Guid InvokeMethod(Guid instance, string method, IEnumerable<Argument> args);

        Argument Convert(Guid obj, string t);
    }
}