using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Python;
using UiPath.Python.Activities.API.Models;

namespace UiPath.Python.Activities.API
{
    /// <summary>
    /// Extension methods for Python coded workflow operations.
    /// </summary>
    public static class PythonOperations
    {
        /// <summary>
        /// Runs a Python script from a file path.
        /// </summary>
        /// <param name="pythonScope">The Python scope handle.</param>
        /// <param name="scriptFile">Path to the Python script file to run.</param>
        /// <param name="ct">Cancellation token.</param>
        public static async Task RunScript(this IPythonScopeHandle pythonScope, string scriptFile, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(pythonScope);
            if (string.IsNullOrWhiteSpace(scriptFile))
                throw new ArgumentException("scriptFile must not be null or whitespace.", nameof(scriptFile));
            var code = await File.ReadAllTextAsync(scriptFile, ct).ConfigureAwait(false);
            await pythonScope.GetEngine().Execute(code, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Runs inline Python code.
        /// </summary>
        /// <param name="pythonScope">The Python scope handle.</param>
        /// <param name="code">The Python code to execute.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task RunCode(this IPythonScopeHandle pythonScope, string code, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(pythonScope);
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("code must not be null or whitespace.", nameof(code));
            return pythonScope.GetEngine().Execute(code, ct);
        }

        /// <summary>
        /// Loads a Python script from a file and returns a handle to it.
        /// </summary>
        /// <param name="pythonScope">The Python scope handle.</param>
        /// <param name="scriptFile">Path to the Python script file to load.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A <see cref="PythonObject"/> representing the loaded script.</returns>
        public static async Task<PythonObject> LoadScript(this IPythonScopeHandle pythonScope, string scriptFile, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(pythonScope);
            if (string.IsNullOrWhiteSpace(scriptFile))
                throw new ArgumentException("scriptFile must not be null or whitespace.", nameof(scriptFile));
            var code = await File.ReadAllTextAsync(scriptFile, ct).ConfigureAwait(false);
            return await pythonScope.GetEngine().LoadScript(code, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Loads inline Python code and returns a handle to it.
        /// </summary>
        /// <param name="pythonScope">The Python scope handle.</param>
        /// <param name="code">The Python code to load.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A <see cref="PythonObject"/> representing the loaded script.</returns>
        public static Task<PythonObject> LoadCode(this IPythonScopeHandle pythonScope, string code, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(pythonScope);
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("code must not be null or whitespace.", nameof(code));
            return pythonScope.GetEngine().LoadScript(code, ct);
        }

        /// <summary>
        /// Invokes a method on a loaded Python script instance.
        /// </summary>
        /// <param name="pythonScope">The Python scope handle.</param>
        /// <param name="instance">The Python object instance on which to invoke the method.</param>
        /// <param name="methodName">The name of the method to invoke.</param>
        /// <param name="parameters">Optional parameters to pass to the method.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A <see cref="PythonObject"/> representing the result.</returns>
        public static Task<PythonObject> InvokeMethod(this IPythonScopeHandle pythonScope, PythonObject instance, string methodName, IEnumerable<object> parameters = null, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(pythonScope);
            ArgumentNullException.ThrowIfNull(instance);
            if (string.IsNullOrWhiteSpace(methodName))
                throw new ArgumentException("methodName must not be null or whitespace.", nameof(methodName));
            return pythonScope.GetEngine().InvokeMethod(instance, methodName, parameters, ct);
        }

        /// <summary>
        /// Converts a Python object to a .NET type.
        /// </summary>
        /// <typeparam name="T">The target .NET type.</typeparam>
        /// <param name="pythonScope">The Python scope handle.</param>
        /// <param name="pythonObject">The Python object to convert.</param>
        /// <returns>The converted .NET value.</returns>
        public static T GetObject<T>(this IPythonScopeHandle pythonScope, PythonObject pythonObject)
        {
            ArgumentNullException.ThrowIfNull(pythonScope);
            ArgumentNullException.ThrowIfNull(pythonObject);
            return (T)pythonScope.GetEngine().Convert(pythonObject, typeof(T));
        }

        internal static IEngine GetEngine(this IPythonScopeHandle handle) => ((PythonScopeHandle)handle).Engine;
    }
}
