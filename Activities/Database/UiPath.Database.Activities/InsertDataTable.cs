using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Windows.Markup;
using UiPath.Database.Activities.Properties;

namespace UiPath.Database.Activities
{
    public class InsertDataTable : AsyncCodeActivity
    {
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [RequiredArgument]
        [OverloadGroup("New Database Connection")]
        [LocalizedDisplayName(nameof(Resources.ProviderNameDisplayName))]
        public InArgument<string> ProviderName { get; set; }

        [DependsOn(nameof(ProviderName))]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [OverloadGroup("New Database Connection")]
        [LocalizedDisplayName(nameof(Resources.ConnectionStringDisplayName))]
        public InArgument<string> ConnectionString { get; set; }

        [DefaultValue(null)]
        [DependsOn(nameof(ProviderName))]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [OverloadGroup("New Database Connection")]
        [LocalizedDisplayName(nameof(Resources.ConnectionSecureStringDisplayName))]
        public InArgument<SecureString> ConnectionSecureString { get; set; }

        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [RequiredArgument]
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
        public OutArgument<int> AffectedRecords { get; set; }

        private DatabaseConnection DbConnection = null;

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            DataTable dataTable = null;
            string connString = null;
            SecureString connSecureString = null;
            string provName = null;
            string tableName = null;
            try
            {
                DbConnection = ExistingDbConnection.Get(context);
                connString = ConnectionString.Get(context);
                provName = ProviderName.Get(context);
                tableName = TableName.Get(context); 
                dataTable = DataTable.Get(context);

                connSecureString = ConnectionSecureString.Get(context);
            }
            catch (Exception ex)
            {
                HandleException(ex, ContinueOnError.Get(context));
            }
            if (DbConnection == null && connString == null && connSecureString == null)
            {
                throw new ArgumentNullException(Resources.ConnectionMustBeSet);
            }
            // create the action for doing the actual work
            Func<int> action = () =>
            {
                DbConnection = DbConnection ?? new DatabaseConnection().Initialize(connString != null ? connString : new NetworkCredential("", connSecureString).Password, provName);
                if (DbConnection == null)
                {
                    return 0;
                }
                return DbConnection.InsertDataTable(tableName, dataTable);
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
                Func<int> action = (Func<int>)context.UserState;
                int affectedRecords = action.EndInvoke(result);
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
