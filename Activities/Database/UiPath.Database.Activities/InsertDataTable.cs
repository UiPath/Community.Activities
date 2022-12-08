using System;
using System.Activities;
using System.ComponentModel;
using System.Data;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Database.Activities.Properties;

namespace UiPath.Database.Activities
{
    [LocalizedDescription(nameof(Resources.Activity_InsertDataTable_Description))]
    public partial class InsertDataTable : AsyncTaskCodeActivity
    {
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.Activity_InsertDataTable_Property_ProviderName_Name))]
        [LocalizedDescription(nameof(Resources.Activity_InsertDataTable_Property_ProviderName_Description))]
        public InArgument<string> ProviderName { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.Activity_InsertDataTable_Property_ConnectionString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_InsertDataTable_Property_ConnectionString_Description))]
        public InArgument<string> ConnectionString { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.Activity_InsertDataTable_Property_ConnectionSecureString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_InsertDataTable_Property_ConnectionSecureString_Description))]
        public InArgument<SecureString> ConnectionSecureString { get; set; }

        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.Activity_InsertDataTable_Property_ExistingDbConnection_Name))]
        [LocalizedDescription(nameof(Resources.Activity_InsertDataTable_Property_ExistingDbConnection_Description))]
        public InArgument<DatabaseConnection> ExistingDbConnection { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [RequiredArgument]
        [DefaultValue(null)]
        [LocalizedDisplayName(nameof(Resources.Activity_InsertDataTable_Property_TableName_Name))]
        [LocalizedDescription(nameof(Resources.Activity_InsertDataTable_Property_TableName_Description))]
        public InArgument<string> TableName { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [DefaultValue(null)]
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.Activity_InsertDataTable_Property_DataTable_Name))]
        [LocalizedDescription(nameof(Resources.Activity_InsertDataTable_Property_DataTable_Description))]
        public InArgument<DataTable> DataTable { get; set; }

        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.Activity_InsertDataTable_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_InsertDataTable_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_InsertDataTable_Property_AffectedRecords_Name))]
        [LocalizedDescription(nameof(Resources.Activity_InsertDataTable_Property_AffectedRecords_Description))]
        public OutArgument<int> AffectedRecords { get; set; }

        private DatabaseConnection DbConnection = null;



        private void HandleException(Exception ex, bool continueOnError)
        {
            if (continueOnError) return;
            throw ex;
        }

        protected async override Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            DataTable dataTable = null;
            string connString = null;
            SecureString connSecureString = null;
            string provName = null;
            string tableName = null;
            DatabaseConnection existingConnection = null;
            int affectedRecords = 0;
            var continueOnError = ContinueOnError.Get(context);
            try
            {
                existingConnection = DbConnection = ExistingDbConnection.Get(context);
                connString = ConnectionString.Get(context);
                provName = ProviderName.Get(context);
                tableName = TableName.Get(context);
                dataTable = DataTable.Get(context);

                connSecureString = ConnectionSecureString.Get(context);
                ConnectionHelper.ConnectionValidation(existingConnection, connSecureString, connString, provName);
                // create the action for doing the actual work
                affectedRecords = await Task.Run(() =>
                {
                    DbConnection = DbConnection ?? new DatabaseConnection().Initialize(connString != null ? connString : new NetworkCredential("", connSecureString).Password, provName);
                    if (DbConnection == null)
                    {
                        return 0;
                    }
                    return DbConnection.InsertDataTable(tableName, dataTable);
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
