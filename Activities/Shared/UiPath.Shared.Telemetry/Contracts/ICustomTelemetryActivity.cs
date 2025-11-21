using System;
using System.Activities;
using UiPath.Shared.Telemetry.Contracts;

namespace UiPath.Shared.Telemetry
{
    internal interface ICustomTelemetryActivity
    {
        ExecutionOperation GetExecutionOperationData(IActivityTelemetryHandler handler, ActivityContext context);
    }

    public interface ITelemetryAliasActivity
    {
        Type TelemetryActivityType { get; }
    }
}