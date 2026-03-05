using TelemetryClient.Contracts;

namespace UiPath.Shared.Telemetry.Services
{
    internal class TelemetryService : TelemetryServiceBase
    {
        public override bool IsEnabled => TelemetryProvider.IsSupported;

        protected override IOperation TrackOperationInternal(BasicOperation operation)
        {
            if (!TelemetryProvider.IsSupported)
                return null;

            return TelemetryProvider.Client.StartOperation(operation);
        }

        protected override IOperation TrackExecutionOperationInternal(ExecutionOperation data)
        {
            if (!TelemetryProvider.IsSupported)
                return null;

            return TelemetryProvider.Client.StartOperation(data);
        }

        protected override void TrackEventInternal(BasicEvent basicEvent)
        {
            if (!TelemetryProvider.IsSupported)
                return;

            try
            {
                TelemetryProvider.Client.TrackEvent(basicEvent);
            }
            catch
            {
                // do not trace
            }
        }
    }
}
