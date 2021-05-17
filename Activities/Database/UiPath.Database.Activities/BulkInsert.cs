using System;
using System.Activities;
using System.ComponentModel;
using System.Data;
using System.Windows.Markup;
using UiPath.Database.Activities.Properties;
using UiPath.Robot.Activities.Api;

namespace UiPath.Database.Activities
{
    public class BulkInsert : AsyncCodeActivity
    {
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [RequiredArgument]
        [OverloadGroup("New Database Connection")]
        [LocalizedDisplayName(nameof(Resources.ProviderNameDisplayName))]
        public InArgument<string> ProviderName { get; set; }

        [RequiredArgument]
        [DependsOn(nameof(ProviderName))]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [OverloadGroup("New Database Connection")]
        [LocalizedDisplayName(nameof(Resources.ConnectionStringDisplayName))]
        public InArgument<string> ConnectionString { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [OverloadGroup("Existing Database Connection")]
        [LocalizedDisplayName(nameof(Resources.ExistingDbConnectionDisplayName))]
        public InArgument<DatabaseConnection> ExistingDbConnection { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [RequiredArgument]
        [DefaultValue(null)]
        [LocalizedDisplayName(nameof(Resources.TableNameDisplayName))]
        public InArgument<string> TableName { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [DefaultValue(null)]
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.DataTableDisplayName))]
        public InArgument<DataTable> DataTable { get; set; }

        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnErrorDisplayName))]
        public InArgument<bool> ContinueOnError { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.AffectedRecordsDisplayName))]
        public OutArgument<long> AffectedRecords { get; set; }

        private DatabaseConnection DbConnection = null;

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            DataTable dataTable = null;
            string connString = null;
            string provName = null;
            string tableName = null;
            IExecutorRuntime executorRuntime = null;
            try
            {
                DbConnection = ExistingDbConnection.Get(context);
                connString = ConnectionString.Get(context);
                provName = ProviderName.Get(context);
                tableName = TableName.Get(context);
                dataTable = DataTable.Get(context);
                executorRuntime = context.GetExtension<IExecutorRuntime>(); 
            }
            catch (Exception ex)
            {
                HandleException(ex, ContinueOnError.Get(context));
            }
            // create the action for doing the actual work
            Func<long> action = () =>
            {
                DbConnection = DbConnection ?? new DatabaseConnection().Initialize(connString, provName);
                if (DbConnection == null)
                {
                    return 0;
                }
            if (executorRuntime != null && executorRuntime.HasFeature(ExecutorFeatureKeys.LogMessage))
                    return DbConnection.BulkInsertDataTable(tableName, dataTable, connString, executorRuntime);
                else
                    return DbConnection.BulkInsertDataTable(tableName, dataTable, connString);
            };
            context.UserState = action;
            return action.BeginInvoke(callback, state);
        }

        private void HandleException(Exception ex, bool continueOnError)
        {
            if (continueOnError) return;
            throw ex;
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            DatabaseConnection existingConnection = ExistingDbConnection.Get(context);
            try
            {
                Func<long> action = (Func<long>)context.UserState;
                long affectedRecords = action.EndInvoke(result);
                this.AffectedRecords.Set(context, affectedRecords);
            }
            catch (Exception ex)
            {
                HandleException(ex, ContinueOnError.Get(context));
            }
            finally
            {
                if (existingConnection == null)
                {
                    DbConnection?.Dispose();
                }
            }
        }
    }
}