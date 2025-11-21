using TelemetryClient.Contracts.Attributes;
using TelemetryClient.Contracts.Model;

namespace UiPath.Shared.Telemetry
{
    internal class BasicOperation : TelemetryOperation
    {
        public BasicOperation(string name) : base(name, TelemetryCommon.Assembly)
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

    internal class BasicOperation<T> : BasicOperation
    {
        public BasicOperation(string name, T data) : base(name)
        {
            Data = data;
        }

        [TelemetryProperty]
        public T Data { get; set; }
    }
}
