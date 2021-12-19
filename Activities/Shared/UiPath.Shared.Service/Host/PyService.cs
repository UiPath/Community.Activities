using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace UiPath.Shared.Service.Host
{
    /// <summary>
    /// Simpe WCF server, using named pipes, with name is based on process id
    /// </summary>
    internal abstract class PyService : IDisposable
    {
        #region Constants

        private static readonly Encoding _utf8Encoding = new UTF8Encoding(false);

        private const int _defaultBufferSize = 1 << 15;

        #endregion Constants

        protected TimeSpan SendTimeout { get; set; } = Config.DefaultSendTimeout;

        protected TimeSpan ReceiveTimeout { get; set; } = Config.DefaultReceiveTimeout;
        protected NamedPipeServerStream pipeServer { get; set; }
        protected bool IncludeExceptionsInFaults { get; set; }

        internal PyService(bool includeExceptionsInFaults = true)
        {
            IncludeExceptionsInFaults = includeExceptionsInFaults;
        }

        internal void Start()
        {
            //string serviceAddress = Config.MakeServiceAddress(typeof(T), Process.GetCurrentProcess().Id);
            string serviceAddress = string.Empty;
            Trace.TraceInformation($"Trying to start PyService service.");
            try
            {
                pipeServer =
                     new NamedPipeServerStream(Process.GetCurrentProcess().Id.ToString(),
                     PipeDirection.InOut, 1);

                // Wait for a client to connect
                pipeServer.WaitForConnection();

                using (var streamReader = new StreamReader(pipeServer, _utf8Encoding, false, _defaultBufferSize,
                                                           leaveOpen: true))
                {
                    var request = PythonRequest.Deserialize(streamReader.ReadLine());
                    Console.WriteLine(request.RequestType.ToString());
                }

                Trace.TraceInformation($"PyService service {serviceAddress} started successfully");
            }
            catch (Exception e)
            {
                Trace.TraceError($"Error starting PyService service: {e}");
                throw;
            }
        }

        internal void Stop()
        {
            //TODO
        }

        public void Dispose()
        {
            Stop();
        }
    }
}