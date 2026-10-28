using System;
using System.Activities;
using System.Activities.Statements;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using TelemetryClient.Contracts.Model;
using UiPath.Database.Activities.Properties;
using UiPath.Shared.Activities;
#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif

namespace UiPath.Database.Activities
{
    [LocalizedDescription(nameof(Resources.Activity_DatabaseTransaction_Description))]
#if NETSTANDARD
    [Browsable(false)]
#endif
    public partial class DatabaseTransaction : AsyncTaskNativeActivity
    {
        private const string _commit = "Commit";
        private const string _rollback = "Rollback";
        private const string _successful = "Successful";
        private const string _failed = "Failed";

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseTransaction_Property_ProviderName_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseTransaction_Property_ProviderName_Description))]
        public InArgument<string> ProviderName { get; set; }

        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [DefaultValue(null)]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseTransaction_Property_ConnectionString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseTransaction_Property_ConnectionString_Description))]
        public InArgument<string> ConnectionString { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseTransaction_Property_ConnectionSecureString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseTransaction_Property_ConnectionSecureString_Description))]
        public InArgument<SecureString> ConnectionSecureString { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseTransaction_Property_ExistingDbConnection_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseTransaction_Property_ExistingDbConnection_Description))]
        public InArgument<DatabaseConnection> ExistingDbConnection { get; set; }

        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseTransaction_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseTransaction_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseTransaction_Property_DatabaseConnection_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseTransaction_Property_DatabaseConnection_Description))]
        public OutArgument<DatabaseConnection> DatabaseConnection { get; set; }

        [Browsable(false)]
        public System.Activities.Activity Body { get; set; }

        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseTransaction_Property_UseTransaction_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseTransaction_Property_UseTransaction_Description))]
        public bool UseTransaction { get; set; } = true;

        public DatabaseTransaction()
        {
            Body = new Sequence
            {
                DisplayName = "Do"
            };
        }

        private void HandleException(Exception ex, bool continueOnError)
        {
            if (continueOnError || ex == null) return;
            throw ex;
        }


        protected override async Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext context, CancellationToken cancellationToken)
        {
            ITelemetryOperationWrapper telemetryOperation = null;
#if ENABLE_DEFAULT_TELEMETRY
            telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
#endif
            try
            {
                var connString = ConnectionString.Get(context);
                SecureString connSecureString = null;
                var provName = ProviderName.Get(context);
                connSecureString = ConnectionSecureString.Get(context);
                DatabaseConnection existingConnection = null;
                existingConnection = ExistingDbConnection.Get(context);
                DatabaseConnection dbConnection = null;


                ConnectionHelper.ConnectionValidation(existingConnection, connSecureString, connString, provName);
                dbConnection = await Task.Run(() => existingConnection ?? new DatabaseConnection().Initialize(connString ?? new NetworkCredential("", connSecureString).Password, provName));
                if (UseTransaction)
                {
                    dbConnection.BeginTransaction();
                }

                var result = new Action<NativeActivityContext>(nativeActivityContext =>
                {
                    DatabaseConnection.Set(nativeActivityContext, dbConnection);
                    if (Body != null)
                    {
                        nativeActivityContext.ScheduleActivity(Body, OnCompletedCallback, OnFaultedCallback);
                    }
                });
                return result;
            }
            catch (Exception ex)
            {
                telemetryOperation?.SendWithException(ex);
                throw;
            }
        }

        private void OnCompletedCallback(NativeActivityContext context, ActivityInstance completedInstance)
        {
            ITelemetryOperationWrapper telemetryOperation = null;
#if ENABLE_DEFAULT_TELEMETRY
            telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
#endif

            DatabaseConnection conn = null;
            Exception ex = null;
            var continueOnError = ContinueOnError.Get(context);
            var existingConnection = ExistingDbConnection.Get(context);
            try
            {
                conn = DatabaseConnection.Get(context);
                if (UseTransaction && conn.State != System.Data.ConnectionState.Closed)
                {
                    conn.Commit();
                    telemetryOperation?.SetCustomDataKey(_commit, _successful);
                    telemetryOperation?.Send();
                }
            }
            catch (Exception e)
            {
                ex = e;
                telemetryOperation?.SetCustomDataKey(_commit, _failed);
                telemetryOperation?.SendWithException(e);
            }
            finally
            {
                // Dispose only a connection this activity created (no ExistingDbConnection) AND that the
                // user did not capture through the DatabaseConnection output. A bound output means the
                // caller intends to reuse the connection after the scope (per the documented output
                // contract "can be subsequently used for other database operations"), so it must stay
                // open; an externally supplied connection is always the caller's to manage.
                if (conn != null && existingConnection == null && DatabaseConnection?.Expression == null)
                {
                    conn.Dispose();
                }
            }

            HandleException(ex, continueOnError);
        }

        private void OnFaultedCallback(NativeActivityFaultContext faultContext, Exception exception, ActivityInstance source)
        {
            ITelemetryOperationWrapper telemetryOperation = null;
#if ENABLE_DEFAULT_TELEMETRY
            telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, faultContext);
#endif

            faultContext.CancelChildren();
            DatabaseConnection conn = DatabaseConnection.Get(faultContext);
            var continueOnError = ContinueOnError.Get(faultContext);
            var existingConnection = ExistingDbConnection.Get(faultContext);
            var primaryException = exception;
            if (conn != null)
            {
                try
                {
                    if (UseTransaction && conn.State != System.Data.ConnectionState.Closed)
                    {
                        conn.Rollback();
                        telemetryOperation?.SetCustomDataKey(_rollback, _successful);
                        telemetryOperation?.Send();
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.Message);
                    //we should trace the original exception if present
                    if (primaryException == null)
                        primaryException = ex;
                    telemetryOperation?.SetCustomDataKey(_rollback, _failed);
                    telemetryOperation?.SendWithException(ex);
                }
                finally
                {
                    // See OnCompletedCallback: keep the connection open when it was supplied externally
                    // or captured via a bound DatabaseConnection output; otherwise the activity owns it.
                    if (existingConnection == null && DatabaseConnection?.Expression == null)
                    {
                        conn.Dispose();
                    }
                }
            }

            faultContext.HandleFault();

            HandleException(primaryException, continueOnError);
        }

    }
}