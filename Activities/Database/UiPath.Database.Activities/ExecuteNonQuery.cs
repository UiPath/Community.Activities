using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Database.Activities.Properties;
using UiPath.Shared.Activities;
#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif

namespace UiPath.Database.Activities
{
    [LocalizedDescription(nameof(Resources.Activity_ExecuteNonQuery_Description))]
    public partial class ExecuteNonQuery : DatabaseExecute
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_ExecuteNonQuery_Property_Sql_Name))]
        [LocalizedDescription(nameof(Resources.Activity_ExecuteNonQuery_Property_Sql_Description))]
        public InArgument<string> Sql { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_ExecuteNonQuery_Property_AffectedRecords_Name))]
        [LocalizedDescription(nameof(Resources.Activity_ExecuteNonQuery_Property_AffectedRecords_Description))]
        public OutArgument<int> AffectedRecords { get; set; }

        public ExecuteNonQuery()
        {
            CommandType = CommandType.Text;
        }

        protected async override Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            ITelemetryOperationWrapper telemetryOperation = null;
#if ENABLE_DEFAULT_TELEMETRY
            telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
#endif
            try
            {
                string connString = null;
                SecureString connSecureString = null;
                string provName = null;
                string sql = string.Empty;
                int? commandTimeoutMs = TimeoutMS.Expression is null ? (int?)null : TimeoutMS.Get(context);
                DatabaseConnection existingConnection = null;
                DBExecuteCommandResult affectedRecords = null;
                if (commandTimeoutMs.HasValue && commandTimeoutMs.Value < 0)
                {
                    throw new ArgumentException(Resources.TimeoutMSException, nameof(TimeoutMS));
                }
                Dictionary<string, ParameterInfo> parameters = null;
                var continueOnError = ContinueOnError.Get(context);
                try
                {
                    sql = Sql.Get(context);
                    existingConnection = DbConnection = ExistingDbConnection.Get(context);
                    connString = ConnectionString.Get(context);
                    connSecureString = ConnectionSecureString.Get(context);
                    provName = ProviderName.Get(context);

                    parameters = ConnectionHelper.BuildParameters(Parameters, context);
                    ConnectionHelper.ConnectionValidation(existingConnection, connSecureString, connString, provName);
                    // create the action for doing the actual work
                    affectedRecords = await Task.Run(() => ExecuteCommand(connString, connSecureString, provName, sql, parameters, commandTimeoutMs));
                }
                catch (Exception ex)
                {
                    // telemetryOperation object is made null in order to avoid double sending.
                    telemetryOperation?.SendWithException(ex);
                    telemetryOperation = null;
                    ConnectionHelper.HandleException(ex, continueOnError);
                }
                finally
                {
                    if (existingConnection == null)
                    {
                        DbConnection?.Dispose();
                    }
                }
                var result = new Action<AsyncCodeActivityContext>(asyncCodeActivityContext =>
                {
                    AffectedRecords.Set(asyncCodeActivityContext, affectedRecords.Result);
                    ConnectionHelper.SetOutputParameters(asyncCodeActivityContext, Parameters, affectedRecords.ParametersBind);
                });

                //if exception was caught and sent to telemetry, avoid sending again
                telemetryOperation?.Send();
                return result;
            }
            catch (Exception ex)
            {
                // If any other exception occurs, send it to telemetry
                telemetryOperation?.SendWithException(ex);
                throw;
            }
        }

        private DBExecuteCommandResult ExecuteCommand(string connString, SecureString connSecureString, string provName, string sql, Dictionary<string, ParameterInfo> parameters, int? commandTimeoutMs)
        {
            DbConnection = ConnectionHelper.EnsureConnection(DbConnection, connString, connSecureString, provName);
            if (DbConnection == null)
            {
                return new DBExecuteCommandResult();
            }
            return new DBExecuteCommandResult(DbConnection.Execute(sql, parameters, commandTimeoutMs, CommandType), parameters);
        }

        private class DBExecuteCommandResult
        {
            public int Result { get; }
            public Dictionary<string, ParameterInfo> ParametersBind { get; }

            public DBExecuteCommandResult()
            {
                this.Result = 0;
                this.ParametersBind = new Dictionary<string, ParameterInfo>();
            }

            public DBExecuteCommandResult(int result, Dictionary<string, ParameterInfo> parametersBind)
            {
                this.Result = result;
                this.ParametersBind = parametersBind;
            }
        }
    }
}
