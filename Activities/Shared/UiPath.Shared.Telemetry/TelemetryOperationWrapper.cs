using System;
using System.Collections.Generic;
using TelemetryClient.Contracts;
using UiPath.Shared.Activities;
using UiPath.Shared.Telemetry.Contracts;

namespace UiPath.Shared.Telemetry
{
    internal class TelemetryOperationWrapper : ITelemetryOperationWrapper
    {
        private readonly IActivityTelemetryHandler _telemetryHandler;
        private readonly IOperation _operation;

        private bool _isSent;

        public TelemetryOperationWrapper(IActivityTelemetryHandler telemetryHandler, ExecutionOperation executionOperation, IOperation operation)
        {
            _telemetryHandler = telemetryHandler;
            _operation = operation;
            ExecutionOperation = executionOperation;
        }

        public ExecutionOperation ExecutionOperation { get; }

        public void SendWithException(Exception ex)
        {
            if (_isSent)
                return;

            // create exception data to send
            var exceptionData = ActivityExceptionData.From($"{ExecutionOperation.Name}Error", ExecutionOperation, ex);

            // get service & ensure we can send telemetry
            var service = _telemetryHandler.GetRuntimeTelemetryService();
            if (service?.IsEnabled == true)
            {
                // allow handler to pre-process the exception
                _telemetryHandler.PreProcessException(exceptionData);

                // send event exception
                service.Track(exceptionData);
            }

            // ensure we set the Error property accordingly
            ExecutionOperation.Error = exceptionData.Error;

            Send();
        }

        public void Send()
        {
            if (_isSent)
                return;

            _isSent = true;

            if (ExecutionOperation.Error != null)
                _operation.Success = false;

            // dispose will auto-send it to the telemetry
            _operation.Dispose();
        }

        public void SetCustomData(object value)
        {
            ExecutionOperation.Data = value;
        }

        public void SetCustomDataKey(string key, object value)
        {
            if (ExecutionOperation.Data is not Dictionary<string, object> dataDict)
            {
#if DEBUG
                if (ExecutionOperation.Data != null)
                    throw new NotSupportedException("Existing custom data will be lost. Using both SetCustomData and SetCustomDataKey is not supported");
#endif

                dataDict = new Dictionary<string, object>();
                ExecutionOperation.Data = dataDict;
            }

            dataDict[key] = value;
        }
    }
}
