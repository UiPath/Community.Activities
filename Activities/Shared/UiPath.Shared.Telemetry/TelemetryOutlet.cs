using System;
using System.Diagnostics;
using TelemetryClient.Contracts;
using TelemetryClient.Contracts.Outlets;
using TelemetryClient.Implementations.Serialization;
using UiPath.Activities.Contracts;
using UiPath.Activities.Contracts.Telemetry;
using UiPath.Shared.Contracts.Private;


//uipath.platform

namespace UiPath.Shared.Telemetry
{
    internal class TelemetryOutlet : IOutlet
    {
        private readonly TelemetrySerializer _telemetrySerializer = new TelemetrySerializer();
        private readonly ITelemetryProxy _runtimeTelemetryProxy;
        private readonly ITelemetryProxy _designTelemetryProxy;

        public TelemetryOutlet(TelemetryOutletConfiguration configuration)
        {
            _designTelemetryProxy = new LegacyDesignerContract().TryInvoke(c => c.UnSafe().TelemetryProxy);

            PrivateRuntimeContract contract;
            try
            {
                var api = Platform.ServiceResolver.Default?.Resolve<IWorkflowRuntime>();
                contract = new PrivateRuntimeContract(api);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                contract = new PrivateRuntimeContract(null);
            }
            _runtimeTelemetryProxy = contract.TryInvoke(c => c.UnSafe().TelemetryProxy);
        }

        public string Name => nameof(TelemetryOutlet);

        public Outlet<IOperation> PushOperation => Export;

        public Outlet<IEvent> PushEvent => Export;

        public void Export(ITelemetry telemetry)
        {
            var serializedTelemetry = _telemetrySerializer.Serialize(telemetry);

            _designTelemetryProxy?.Send(serializedTelemetry);
            _runtimeTelemetryProxy?.Send(serializedTelemetry);
        }
    }
}