using System;
using System.Activities;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Database.Activities.Properties;
using UiPath.Shared.Activities;
#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif

namespace UiPath.Database.Activities
{
    [LocalizedDescription(nameof(Resources.Activity_DatabaseDisconnect_Description))]
    public partial class DatabaseDisconnect : AsyncTaskCodeActivity
    {
        [LocalizedCategory(nameof(Resources.Connection))]
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseDisconnect_Property_DatabaseConnection_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseDisconnect_Property_DatabaseConnection_Description))]
        public InArgument<DatabaseConnection> DatabaseConnection { get; set; }

        protected async override Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            ITelemetryOperationWrapper telemetryOperation = null;
#if ENABLE_DEFAULT_TELEMETRY
            telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
#endif

            var dbConnection = DatabaseConnection.Get(context);
            // create the action for doing the actual work
            try
            {
                await Task.Run(() => dbConnection?.Dispose());
                telemetryOperation?.Send();
            }
            catch (Exception e)
            {
                telemetryOperation?.SendWithException(e);
                Trace.TraceError($"{e}");
            }

            var result = new Action<AsyncCodeActivityContext>(asyncCodeActivityContext =>
            {
                //no OutArgument
            });
                
            return result;
        }
    }
}
