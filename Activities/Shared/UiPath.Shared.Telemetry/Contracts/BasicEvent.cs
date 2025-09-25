using TelemetryClient.Contracts.Model;

namespace UiPath.Shared.Telemetry
{
    internal class BasicEvent : TelemetryEvent
    {
        public BasicEvent(string name) : base(name, TelemetryCommon.Assembly)
        {
            ActivityPackage = TelemetryCommon.Assembly;
            ActivityPackageVersion = TelemetryCommon.AssemblyVersion;
        }

        /// <summary>
        /// Activity package name. Defaults to <see cref="TelemetryCommon.Assembly"/>
        /// </summary>
        public string ActivityPackage { get; set; }

        /// <summary>
        /// Activity package version. Defaults to <see cref="TelemetryCommon.AssemblyVersion"/>
        /// </summary>
        public string ActivityPackageVersion { get; set; }
    }

    internal class BasicEvent<T> : BasicEvent
    {
        public BasicEvent(string name, T data) : base(name)
        {
            Data = data;
        }

        public T Data { get; set; }
    }
}
