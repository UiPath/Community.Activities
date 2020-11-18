using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Java.Service;

namespace UiPath.Java
{
    public class JavaInvoker : IInvoker
    {
        #region Constants

        private const string _javaFolder = "java_files";

        private const string _javaProgram = "InvokeJava.jar";

        private const string _defaultJava = "java";

        private const string _pipePrefix = "dotnet_java_pipe_";

        private static readonly string _defaultJavaInvokerPath = GetPathToJavaProgram();

        private int _timeout = 15000;

        #endregion

        #region Private Members

        private string _javaPath;

        private string _javaInvokerPath;

        private JavaService _javaService;

        #endregion

        #region Constructor

        public JavaInvoker(string javaPath = null, string javaInvokerPath = null)
        {
            _javaPath = javaPath ?? _defaultJava;
            _javaInvokerPath = javaInvokerPath ?? _defaultJavaInvokerPath;
            _javaService = new JavaService(GetNewPipeName());
        }

        #endregion Construcotr

        #region Start/Stop Java Service

        public async Task StartJavaService(int timeout)
        {
            try
            {
                _timeout = timeout;
                using (var cts = new CancellationTokenSource(_timeout))
                {
                    var startServiceTask = _javaService.StartServiceAsync(cts.Token);
                    _javaService.StartJavaProcess(_javaPath, _javaInvokerPath);
                    await startServiceTask;
                }
            }
            catch (Exception e)
            {
                Trace.TraceError($"Java listner could not be started: {e.ToString()}");
                throw;
            }
        }

        public async Task StopJavaService()
        {
            var request = new JavaRequest { RequestType = RequestType.StopConnection };
            using (var cts = new CancellationTokenSource(_timeout))
            {
                await _javaService.RequestAsync(request, cts.Token);
            }
        }

        public async Task ReleaseAsync()
        {
            try
            {
                await StopJavaService();
            }
            catch (Exception e)
            {
                Trace.TraceError($"Error stopping Java process: {e.ToString()}");
            }
            _javaService?.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Java Invoker Methods

        public async Task LoadJar(string jarPath, CancellationToken ct)
        {
            var request = new JavaRequest() { RequestType = RequestType.LoadJar, JarPath = jarPath };

            JavaResponse response = await _javaService.RequestAsync(request, ct);
            ct.ThrowIfCancellationRequested();
            response.ThrowExceptionIfNeeded();
        }

        public async Task<JavaObject> InvokeMethod(string methodName, string className, JavaObject javaObject, List<object> parameters, List<Type> parametersTypes,
                                                   CancellationToken ct)
        {
            return await SendJavaRequest(className != null ? RequestType.InvokeStaticMethod : RequestType.InvokeMethod, ct, methodName: methodName,
                                         className: className, javaObject: javaObject, parameters: parameters, parametersTypes: parametersTypes);
        }

        public async Task<JavaObject> InvokeConstructor(string className, List<object> parameters, List<Type> parametersTypes, CancellationToken ct)
        {
            return await SendJavaRequest(RequestType.InvokeConstructor, ct, className: className, parameters: parameters, parametersTypes: parametersTypes);
        }

        public async Task<JavaObject> InvokeGetField(JavaObject javaObject, string fieldName, string className, CancellationToken ct)
        {
            return await SendJavaRequest(RequestType.GetField, ct, fieldName: fieldName, className: className, javaObject: javaObject);
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Sends a request to the java process with one of the invoke options.
        /// Receives the response from the java process and it encapsulates it in JavaObject.
        /// Throws an exception if the result state is not Successful.
        /// </summary>
        /// <param name="requestType"></param>
        /// <param name="ct"></param>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <param name="fieldName"></param>
        /// <param name="javaObject"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private async Task<JavaObject> SendJavaRequest(RequestType requestType, CancellationToken ct,
                                                     string className = null, string methodName = null,
                                                     string fieldName = null, JavaObject javaObject = null,
                                                     List<object> parameters = null, List<Type> parametersTypes = null)
        {
            var request = new JavaRequest()
            {
                RequestType = requestType,
                MethodName = methodName,
                ClassName = className,
                FieldName = fieldName,
                Instance = javaObject?.Instance,
            };
            request.AddParametersToRequest(parameters);

            JavaResponse response = await _javaService.RequestAsync(request, ct);

            ct.ThrowIfCancellationRequested();
            response.ThrowExceptionIfNeeded();

            return new JavaObject() { Instance = response.Result };
        }

        private static string GetNewPipeName()
        {
            return _pipePrefix + Guid.NewGuid();
        }

        private static string GetPathToJavaProgram()
        {
            var assemblyPath = Assembly.GetAssembly(typeof(JavaInvoker)).Location;
            var parentDir = Directory.GetParent(assemblyPath).Parent.FullName;
            return Path.Combine(parentDir, _javaFolder, _javaProgram);
        }

        #endregion

    }
}
