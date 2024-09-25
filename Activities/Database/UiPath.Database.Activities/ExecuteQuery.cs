using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Database.Activities.Properties;

namespace UiPath.Database.Activities
{
    [LocalizedDescription(nameof(Resources.Activity_ExecuteQuery_Description))]
    public partial class ExecuteQuery : DatabaseExecute
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_ExecuteQuery_Property_Sql_Name))]
        [LocalizedDescription(nameof(Resources.Activity_ExecuteQuery_Property_Sql_Description))]
        public InArgument<string> Sql { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_ExecuteQuery_Property_DataTable_Name))]
        [LocalizedDescription(nameof(Resources.Activity_ExecuteQuery_Property_DataTable_Description))]
        public OutArgument<DataTable> DataTable { get; set; }

        public ExecuteQuery()
        {
            CommandType = CommandType.Text;
        }

        private void HandleException(Exception ex, bool continueOnError)
        {
            if (continueOnError) return;
            throw ex;
        }

        protected async override Task<Action<AsyncCodeActivityContext>> ExecuteInternalAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            var dataTable = DataTable.Get(context);
            string connString = null;
            SecureString connSecureString = null;
            string provName = null;
            string sql = string.Empty;
            DatabaseConnection existingConnection = null;
            DBExecuteQueryResult affectedRecords = null;
            int commandTimeout = TimeoutMS.Get(context);
            if (commandTimeout < 0)
            {
                throw new ArgumentException(Resources.TimeoutMSException, "TimeoutMS");
            }
            Dictionary<string, ParameterInfo> parameters = null;
            var continueOnError = ContinueOnError.Get(context);
            try
            {
                existingConnection = DbConnection = ExistingDbConnection.Get(context);
                connString = ConnectionString.Get(context);
                provName = ProviderName.Get(context);
                sql = Sql.Get(context);
                connSecureString = ConnectionSecureString.Get(context);
                ConnectionHelper.ConnectionValidation(existingConnection, connSecureString, connString, provName);
                if (Parameters != null)
                {
                    parameters = new Dictionary<string, ParameterInfo>();
                    foreach (var param in Parameters)
                    {
                        parameters.Add(param.Key, new ParameterInfo() { 
                            Value = param.Value.Get(context), 
                            Direction = param.Value.Direction,
                            Type = param.Value.ArgumentType});
                    }
                }

                // create the action for doing the actual work
                affectedRecords = await Task.Run(() =>
                {
                    if (DbConnection == null)
                    {
                        DbConnection = new DatabaseConnection().Initialize(connString != null ? connString : new NetworkCredential("", connSecureString).Password, provName);
                    }
                    if (DbConnection == null)
                    {
                        return null;
                    }
                    return new DBExecuteQueryResult(DbConnection.ExecuteQuery(sql, parameters, commandTimeout, CommandType), parameters);
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
                DataTable dt = affectedRecords?.Result;
                if (dt == null) return;

                DataTable.Set(asyncCodeActivityContext, dt);
                foreach (var param in affectedRecords.ParametersBind)
                {
                    var currentParam = Parameters[param.Key];
                    if (currentParam.Direction == ArgumentDirection.Out || currentParam.Direction == ArgumentDirection.InOut)
                    {
                        currentParam.Set(asyncCodeActivityContext, param.Value.Value);
                    }
                }
            };
        }

        private class DBExecuteQueryResult
        {
            public DataTable Result { get; }
            public Dictionary<string, ParameterInfo> ParametersBind { get; }

            public DBExecuteQueryResult()
            {
                this.Result = new DataTable();
                this.ParametersBind = new Dictionary<string, ParameterInfo>();
            }

            public DBExecuteQueryResult(DataTable result, Dictionary<string, ParameterInfo> parametersBind)
            {
                this.Result = result;
                this.ParametersBind = parametersBind;
            }
        }
    }
}
