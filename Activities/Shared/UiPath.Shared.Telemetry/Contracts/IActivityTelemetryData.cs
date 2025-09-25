using Newtonsoft.Json;

namespace UiPath.Shared.Telemetry.Contracts
{
    internal interface IActivityTelemetryData
    {
        string ActivityType { get; }

        [JsonIgnore]
        string ActivityPackage { get; }

        [JsonIgnore]
        string ActivityPackageVersion { get; }
    }
}
