using System;
using System.Activities;
using System.Activities.Statements;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Markup;
using UiPath.Database.Activities.Properties;

namespace UiPath.Database.Activities
{
    public class DatabaseTransaction : AsyncNativeActivity
    {
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [RequiredArgument]
        [OverloadGroup("New Database Connection")]
        [LocalizedDisplayName(nameof(Resources.ProviderNameDisplayName))]
        public InArgument<string> ProviderName { get; set; }

        [DependsOn(nameof(ProviderName))]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [DefaultValue(null)]
        [RequiredArgument]
        [OverloadGroup("New Database Connection")]
        [LocalizedDisplayName(nameof(Resources.ConnectionStringDisplayName))]
        public InArgument<string> ConnectionString { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [RequiredArgument]
        [OverloadGroup("Existing Database Connection")]
        [LocalizedDisplayName(nameof(Resources.ExistingDbConnectionDisplayName))]
        public InArgument<DatabaseConnection> ExistingDbConnection { get; set; }

        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnErrorDisplayName))]
        public InArgument<bool> ContinueOnError { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.DatabaseConnectionDisplayName))]
        public OutArgument<DatabaseConnection> DatabaseConnection { get; set; }

        [Browsable(false)]
        public Activity Body { get; set; }

        [LocalizedDisplayName(nameof(Resources.UseTransactionDisplayName))]
        public bool UseTransaction { get; set; }

        private Func<DatabaseConnection> ConnectionInitFunc;

        public DatabaseTransaction()
        {
            UseTransaction = true;
            Body = new Sequence
            {
                DisplayName = "Do"
            };
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddChild(Body);
            base.CacheMetadata(metadata);
        }

        protected override IAsyncResult BeginExecute(NativeActivityContext context, AsyncCallback callback, object state)
        {
            var connString = ConnectionString.Get(context);
            var provName = ProviderName.Get(context);
            var dbConnection = ExistingDbConnection.Get(context) ?? new DatabaseConnection().Initialize(connString, provName);

            ConnectionInitFunc = () => dbConnection;
            return ConnectionInitFunc.BeginInvoke(callback, state);
        }

        protected override void EndExecute(NativeActivityContext context, IAsyncResult result)
        {
            var databaseConn = ConnectionInitFunc.EndInvoke(result);
            if (databaseConn == null) return;

            if (UseTransaction)
            {
                databaseConn.BeginTransaction();
            }

            DatabaseConnection.Set(context, databaseConn);

            if (Body != null)
            {
                context.ScheduleActivity(Body, OnCompletedCallback, OnFaultedCallback);
            }
        }

        private void OnCompletedCallback(NativeActivityContext context, ActivityInstance activityInstance)
        {
            DatabaseConnection conn = null;
            try
            {
                conn = DatabaseConnection.Get(context);
                if (UseTransaction)
                {
                    conn.Commit();
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, ContinueOnError.Get(context));
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
            if (conn != null)
            {
                try
                {
                    if (UseTransaction)
                    {
                        conn.Rollback();
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.Message);
                }
                finally
                {
                    conn.Dispose();
                }
            }

            HandleException(exception, ContinueOnError.Get(faultContext));
            faultContext.HandleFault();
        }

        private void HandleException(Exception ex, bool continueOnError)
        {
            if (continueOnError) return;
            throw ex;
        }
    }
}
