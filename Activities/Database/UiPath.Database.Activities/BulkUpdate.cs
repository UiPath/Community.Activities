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
    [LocalizedDescription(nameof(Resources.Activity_BulkUpdate_Description))]
    public partial class BulkUpdate : DatabaseRowActivity
    {
        [LocalizedCategory(nameof(Resources.Input))]
        [RequiredArgument]
        [DefaultValue(null)]
        [LocalizedDisplayName(nameof(Resources.Activity_BulkUpdate_Property_TableName_Name))]
        [LocalizedDescription(nameof(Resources.Activity_BulkUpdate_Property_TableName_Description))]
        public InArgument<string> TableName { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [DefaultValue(null)]
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.Activity_BulkUpdate_Property_DataTable_Name))]
        [LocalizedDescription(nameof(Resources.Activity_BulkUpdate_Property_DataTable_Description))]
        public InArgument<DataTable> DataTable { get; set; }

        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.Activity_BulkUpdate_Property_BulkUpdateFlag_Name))]
        [LocalizedDescription(nameof(Resources.Activity_BulkUpdate_Property_BulkUpdateFlag_Description))]
        [DefaultValue(true)]
        public bool BulkUpdateFlag { get; set; } = true;

        [LocalizedCategory(nameof(Resources.Input))]
        [RequiredArgument]
        [DefaultValue(null)]
        [LocalizedDisplayName(nameof(Resources.Activity_BulkUpdate_Property_ColumnNames_Name))]
        [LocalizedDescription(nameof(Resources.Activity_BulkUpdate_Property_ColumnNames_Description))]
        public InArgument<string[]> ColumnNames { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_BulkUpdate_Property_AffectedRecords_Name))]
        [LocalizedDescription(nameof(Resources.Activity_BulkUpdate_Property_AffectedRecords_Description))]
        public OutArgument<long> AffectedRecords { get; set; }

        private DatabaseConnection DbConnection = null;

        protected async override Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            DataTable dataTable = null;
            string connString = null;
            SecureString connSecureString = null;
            string provName = null;
            string tableName = null;
            string[] columnNames = null;
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
                columnNames = ColumnNames.Get(context);
                executorRuntime = context.GetExtension<IExecutorRuntime>();
                connSecureString = ConnectionSecureString.Get(context);
                ConnectionHelper.ConnectionValidation(existingConnection, connSecureString, connString, provName);
                affectedRecords = await Task.Run(() =>
                {
                    DbConnection = DbConnection ?? new DatabaseConnection().Initialize(connString != null ? connString : new NetworkCredential("", connSecureString).Password, provName);
                    if (DbConnection == null)
                    {
                        return 0;
                    }
                    if (executorRuntime != null && executorRuntime.HasFeature(ExecutorFeatureKeys.LogMessage))
                        return DbConnection.BulkUpdateDataTable(BulkUpdateFlag, tableName, dataTable, columnNames, executorRuntime);
                    else
                        return DbConnection.BulkUpdateDataTable(BulkUpdateFlag, tableName, dataTable, columnNames);
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
