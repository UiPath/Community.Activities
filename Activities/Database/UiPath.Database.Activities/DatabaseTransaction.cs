using System;
using System.Activities;
using System.Activities.Statements;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Database.Activities.Properties;
using UiPath.Shared.Activities;

namespace UiPath.Database.Activities
{
    [LocalizedDescription(nameof(Resources.Activity_DatabaseTransaction_Description))]
    public partial class DatabaseTransaction : AsyncTaskNativeActivity
    {
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
            if (continueOnError) return;
            throw ex;
        }


        protected override async Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext context, CancellationToken cancellationToken)
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

            return (nativeActivityContext) =>
            {
                DatabaseConnection.Set(nativeActivityContext, dbConnection);
                if (Body != null)
                {
                    nativeActivityContext.ScheduleActivity(Body, OnCompletedCallback, OnFaultedCallback);
                }

            };
        
        }

        private void OnCompletedCallback(NativeActivityContext context, ActivityInstance completedInstance)
        {
            DatabaseConnection conn = null;
            try
            {
                conn = DatabaseConnection.Get(context);
                if (UseTransaction && conn.State != System.Data.ConnectionState.Closed)
                {
                    conn.Commit();
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
            }
        }

        private void OnFaultedCallback(NativeActivityFaultContext faultContext, Exception exception, ActivityInstance source)
        {
            faultContext.CancelChildren();
            DatabaseConnection conn = DatabaseConnection.Get(faultContext);
            var continueOnError = ContinueOnError.Get(faultContext);
            if (conn != null)
            {
                try
                {
                    if (UseTransaction && conn.State != System.Data.ConnectionState.Closed)
                    {
                        conn.Rollback();
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.Message);
                    HandleException(ex, continueOnError);
                }
                finally
                {
                    conn.Dispose();
                }
            }

            faultContext.HandleFault();
        }

    }
}