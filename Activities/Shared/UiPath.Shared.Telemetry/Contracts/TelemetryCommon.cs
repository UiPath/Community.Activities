namespace UiPath.Shared.Telemetry
{
    internal static class TelemetryCommon
    {
        internal const string DefaultContainer = "DefaultContainer";
        internal const string UiPathProxy = nameof(UiPathProxy);

        internal static readonly string Assembly;
        internal static readonly string AssemblyVersion;

        static TelemetryCommon()
        {
            var assemblyName = typeof(TelemetryCommon).Assembly.GetName();
            Assembly = assemblyName.Name;
            AssemblyVersion = assemblyName.Version.ToString();
        }
    }
}
