using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UiPath.Java.Service
{
    internal class JavaService : IDisposable
    {

        #region Constants

        private static readonly Encoding _utf8Encoding = new UTF8Encoding(false);

        private const int _defaultBufferSize = 1 << 15;
        
        #endregion

        #region Private Members

        private string _pipeName;

        private NamedPipeServerStream _serverPipe;

        private Process _javaProcess;

        #endregion

        #region Constructor

        public JavaService(string pipeName)
        {
            _pipeName = pipeName;
        }

        #endregion Constructor

        #region Service Async Methods

        /// <summary>
        /// Starts the java program that will try to connect to the named pipe.
        /// </summary>
        /// <param name="java">Path to java.exe</param>
        /// <param name="javaInvokerPath">Path to the java invoker program</param>
        public void StartJavaProcess(string java, string javaInvokerPath)
        {
            // Use ProcessStartInfo class, if you want to see java process console set WindowStyle to Normal
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = java,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = $"-jar {javaInvokerPath} {_pipeName}"
            };

            // Start the process with the info we specified.
            try
            {
                Trace.TraceInformation("Java process has started");
                _javaProcess = Process.Start(startInfo);
            }

            catch (Exception e)
            {
                Trace.TraceError($"Java process has stopped: {e.ToString()}");
                throw;
            }
        }

        /// <summary>
        /// Creates a pipe with the specified name and waits for the java program to connect to the named pipe.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>

        public async Task StartServiceAsync(CancellationToken ct)
        {
            using (CancellationTokenRegistration ctr = ct.Register(() => OnCancellationRequested()))
            {
                _serverPipe = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
                                                        PipeOptions.Asynchronous);
                try
                {
                    await Task.Factory.FromAsync((cb, state) => _serverPipe.BeginWaitForConnection(cb, state),
                                       ar => _serverPipe.EndWaitForConnection(ar), TaskCreationOptions.DenyChildAttach);
                }
                catch (Exception)
                {
                    if (ct.IsCancellationRequested)
                        throw new NullReferenceException(UiPath.Java.Properties.Resources.JavaTimeoutException);
                    throw;
                }
            }
        }

        /// <summary>
        /// Writes the serialized java request to the named pipe. Waits from the request to be read on java side.
        /// Then reads the response from java(json) an deserializes it to JavaRespnse
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<JavaResponse> RequestAsync(JavaRequest request, CancellationToken ct)
        {
            using (CancellationTokenRegistration ctr = ct.Register(() => OnCancellationRequested()))
            {
                using (var streamWriter = new StreamWriter(_serverPipe, _utf8Encoding, _defaultBufferSize,
                                                           leaveOpen: true) { AutoFlush = true })
                {
                    Trace.TraceInformation("Sending information to Java.");
                    await streamWriter.WriteLineAsync(request.Serialize());
                }

                ct.ThrowIfCancellationRequested();
                _serverPipe.WaitForPipeDrain();

                using (var streamReader = new StreamReader(_serverPipe, _utf8Encoding, false, _defaultBufferSize,
                                                           leaveOpen: true))
                {
                    return JavaResponse.Deserialize(await streamReader.ReadLineAsync());
                }
            }
        }

        #endregion

        #region Dispose Methods

        private void OnCancellationRequested()
        {
            OnPipeDisposed();
            OnProcessDisposed();         
        }

        public void Dispose()
        {
            OnPipeDisposed();
            OnProcessDisposed();
            GC.SuppressFinalize(this);
        }

        public void OnPipeDisposed()
        {
            if (_serverPipe != null && _serverPipe.IsConnected)
            {
                _serverPipe?.Disconnect();
            }
            _serverPipe?.Close();
            _serverPipe?.Dispose();
            _serverPipe = null;
        }

        public void OnProcessDisposed()
        {
            if (_javaProcess != null && !_javaProcess.HasExited)
            {
                _javaProcess.Kill();
            }
            _javaProcess?.Close();
            _javaProcess?.Dispose();
            _javaProcess = null;
        }

        ~JavaService()
        {
            Dispose();
        }

        #endregion

    }
}
