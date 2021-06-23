using System;
using System.Activities;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Database.Activities.Properties;

namespace UiPath.Database.Activities
{
    public class DatabaseDisconnect : AsyncTaskCodeActivity
    {
        [LocalizedCategory(nameof(Resources.Connection))]
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.DatabaseConnectionDisplayName))]
        public InArgument<DatabaseConnection> DatabaseConnection { get; set; }



        protected async override Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            var dbConnection = DatabaseConnection.Get(context);
            // create the action for doing the actual work
            try
            {
                await Task.Run(() => dbConnection?.Dispose());
            }
            catch (Exception e)
            {
                Trace.TraceError($"{e}");
            }

            return asyncCodeActivityContext =>
            {
                //no OutArgument
            };
        }
    }
}
