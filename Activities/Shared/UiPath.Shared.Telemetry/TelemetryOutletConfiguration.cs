using TelemetryClient.Contracts.Outlets;
using TelemetryClient.Implementations.Configuration.Model;

namespace UiPath.Shared.Telemetry
{
    [OutletType(typeof(TelemetryOutlet))]
    internal class TelemetryOutletConfiguration : OutletConfiguration
    {
    }
}