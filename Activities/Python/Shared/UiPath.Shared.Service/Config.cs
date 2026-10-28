using System;

namespace UiPath.Shared.Service
{
    internal static class Config
    {
        internal const string ServiceBaseAddress = "net.pipe://localhost/UiPath.Service/Host_{0}_{1}";

        internal static readonly TimeSpan DefaultSendTimeout = TimeSpan.FromMinutes(10);

        internal static readonly TimeSpan DefaultReceiveTimeout = TimeSpan.FromHours(1);

        internal static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromHours(1);

        internal static readonly TimeSpan DefaultServiceCreationTimeout = TimeSpan.FromSeconds(20);

        internal static string MakeServiceAddress(Type type, int processId)
        {
            return string.Format(ServiceBaseAddress, type.Name, processId);
        }
    }
}