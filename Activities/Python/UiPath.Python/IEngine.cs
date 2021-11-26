using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UiPath.Python
{
    /// <summary>
    /// Python target platform
    /// </summary>
    public enum TargetPlatform
    {
        x86,
        x64
    }

    /// <summary>
    /// Python operations interface
    /// </summary>
    public interface IEngine : IDisposable
    {
        Version  Version { get; }
        #region Lifecycle
        Task Initialize(string workingFolder, CancellationToken ct, double timeout = 3600);

        Task Release();
        #endregion

        #region Operations
        Task Execute(string code, CancellationToken ct);

        Task<PythonObject> LoadScript(string code, CancellationToken ct);

        Task<PythonObject> InvokeMethod(PythonObject instance, string method, IEnumerable<object> args, CancellationToken ct);

        object Convert(PythonObject obj, Type t);
        #endregion
    }
}
