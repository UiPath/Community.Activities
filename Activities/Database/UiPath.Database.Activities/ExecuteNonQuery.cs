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

        private void HandleException(Exception ex, bool continueOnError)
        {
            if (continueOnError) return;
            throw ex;
        }

        protected async override Task<Action<AsyncCodeActivityContext>> ExecuteInternalAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            string connString = null;
            SecureString connSecureString = null;
            string provName = null;
            string sql = string.Empty;
            int commandTimeout = TimeoutMS.Get(context);
            DatabaseConnection existingConnection = null;
            DBExecuteCommandResult affectedRecords = null;
            if (commandTimeout < 0)
            {
                throw new ArgumentException(Resources.TimeoutMSException, "TimeoutMS");
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

                if (Parameters != null)
                {
                    parameters = new Dictionary<string, ParameterInfo>();
                    foreach (var param in Parameters)
                    {
                        parameters.Add(param.Key, new ParameterInfo() { Value = param.Value.Get(context), Direction = param.Value.Direction, Type = param.Value.ArgumentType });
                    }
                }
                ConnectionHelper.ConnectionValidation(existingConnection, connSecureString, connString, provName);
                // create the action for doing the actual work
                affectedRecords = await Task.Run(() =>
                {
                    DBExecuteCommandResult executeResult = new DBExecuteCommandResult();
                    if (DbConnection == null)
                    {
                        DbConnection = new DatabaseConnection().Initialize(connString ?? new NetworkCredential("", connSecureString).Password, provName);
                    }
                    if (DbConnection == null)
                    {
                        return executeResult;
                    }
                    executeResult = new DBExecuteCommandResult(DbConnection.Execute(sql, parameters, commandTimeout, CommandType), parameters);
                    return executeResult;
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
                AffectedRecords.Set(asyncCodeActivityContext, affectedRecords.Result);
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
