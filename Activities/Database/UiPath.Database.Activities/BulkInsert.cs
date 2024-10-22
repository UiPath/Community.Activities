using System;
using System.Activities;
using System.ComponentModel;
using System.Data;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Database.Activities.Properties;
using UiPath.Robot.Activities.Api;

namespace UiPath.Database.Activities
{
    [LocalizedDescription(nameof(Resources.Activity_BulkInsert_Description))]
    public partial class BulkInsert : DatabaseRowActivity
    {
        [LocalizedCategory(nameof(Resources.Input))]
        [RequiredArgument]
        [DefaultValue(null)]
        [LocalizedDisplayName(nameof(Resources.Activity_BulkInsert_Property_TableName_Name))]
        [LocalizedDescription(nameof(Resources.Activity_BulkInsert_Property_TableName_Description))]
        public InArgument<string> TableName { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [DefaultValue(null)]
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.Activity_BulkInsert_Property_DataTable_Name))]
        [LocalizedDescription(nameof(Resources.Activity_BulkInsert_Property_DataTable_Description))]
        public InArgument<DataTable> DataTable { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_BulkInsert_Property_AffectedRecords_Name))]
        [LocalizedDescription(nameof(Resources.Activity_BulkInsert_Property_AffectedRecords_Description))]
        public OutArgument<long> AffectedRecords { get; set; }

        private DatabaseConnection DbConnection = null;

        protected async override Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            DataTable dataTable = null;
            SecureString connSecureString = null;
            string connString = null;
            string provName = null;
            string tableName = null;
            DatabaseConnection existingConnection = null;
            long affectedRecords = 0;
            IExecutorRuntime executorRuntime = null;
            var continueOnError = ContinueOnError.Get(context);
            try
            {
                existingConnection = DbConnection = ExistingDbConnection.Get(context);
                connString = ConnectionString.Get(context);
                provName = ProviderName.Get(context);
                tableName = TableName.Get(context);
                dataTable = DataTable.Get(context);
                executorRuntime = context.GetExtension<IExecutorRuntime>();
                connSecureString = ConnectionSecureString.Get(context);
                ConnectionHelper.ConnectionValidation(existingConnection, connSecureString, connString, provName);
                // create the action for doing the actual work
                affectedRecords = await Task.Run(() =>
            {
                DbConnection = DbConnection ?? new DatabaseConnection().Initialize(connString ?? new NetworkCredential("", connSecureString).Password, provName);
                if (DbConnection == null)
                {
                    return 0;
                }
                if (executorRuntime != null && executorRuntime.HasFeature(ExecutorFeatureKeys.LogMessage))
                    return DbConnection.BulkInsertDataTable(tableName, dataTable, executorRuntime);
                else
                    return DbConnection.BulkInsertDataTable(tableName, dataTable);
            });

            }
            catch (Exception ex)
            {
                HandleException(ex, continueOnError);
            }
            finally
            {
                if (existingConnection == null)
                {
                    DbConnection?.Dispose();
                }
            }

            return asyncCodeActivityContext =>
            {
                AffectedRecords.Set(asyncCodeActivityContext, affectedRecords);
            };
        }
    }
}