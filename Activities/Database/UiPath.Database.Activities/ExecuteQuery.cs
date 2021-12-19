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
    public partial class ExecuteQuery : AsyncTaskCodeActivity
    {
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.Activity_ExecuteQuery_Property_ProviderName_Name))]
        [LocalizedDescription(nameof(Resources.Activity_ExecuteQuery_Property_ProviderName_Description))]
        public InArgument<string> ProviderName { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.Activity_ExecuteQuery_Property_ConnectionString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_ExecuteQuery_Property_ConnectionString_Description))]
        public InArgument<string> ConnectionString { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.Activity_ExecuteQuery_Property_ConnectionSecureString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_ExecuteQuery_Property_ConnectionSecureString_Description))]
        public InArgument<SecureString> ConnectionSecureString { get; set; }

        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.Activity_ExecuteQuery_Property_ExistingDbConnection_Name))]
        [LocalizedDescription(nameof(Resources.Activity_ExecuteQuery_Property_ExistingDbConnection_Description))]
        public InArgument<DatabaseConnection> ExistingDbConnection { get; set; }

        [DefaultValue(null)]
        [LocalizedDisplayName(nameof(Resources.Activity_ExecuteQuery_Property_CommandType_Name))]
        [LocalizedDescription(nameof(Resources.Activity_ExecuteQuery_Property_CommandType_Description))]
        public CommandType CommandType { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_ExecuteQuery_Property_Sql_Name))]
        [LocalizedDescription(nameof(Resources.Activity_ExecuteQuery_Property_Sql_Description))]
        public InArgument<string> Sql { get; set; }

        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.Activity_ExecuteQuery_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_ExecuteQuery_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.Activity_ExecuteQuery_Property_TimeoutMS_Name))]
        [LocalizedDescription(nameof(Resources.Activity_ExecuteQuery_Property_TimeoutMS_Description))]
        public InArgument<int> TimeoutMS { get; set; }

        private Dictionary<string, Argument> parameters;

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [Browsable(true)]
        [LocalizedDisplayName(nameof(Resources.Activity_ExecuteQuery_Property_Parameters_Name))]
        [LocalizedDescription(nameof(Resources.Activity_ExecuteQuery_Property_Parameters_Description))]
        public Dictionary<string, Argument> Parameters
        {
            get
            {
                if (this.parameters == null)
                {
                    this.parameters = new Dictionary<string, Argument>();
                }
                return this.parameters;
            }
            set
            {
                parameters = value;
            }
        }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_ExecuteQuery_Property_DataTable_Name))]
        [LocalizedDescription(nameof(Resources.Activity_ExecuteQuery_Property_DataTable_Description))]
        public OutArgument<DataTable> DataTable { get; set; }

        private DatabaseConnection DbConnection = null;

        public ExecuteQuery()
        {
            CommandType = CommandType.Text;
        }

        private void HandleException(Exception ex, bool continueOnError)
        {
            if (continueOnError) return;
            throw ex;
        }

        protected async override Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
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
            Dictionary<string, Tuple<object, ArgumentDirection>> parameters = null;
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
                    parameters = new Dictionary<string, Tuple<object, ArgumentDirection>>();
                    foreach (var param in Parameters)
                    {
                        parameters.Add(param.Key, new Tuple<object, ArgumentDirection>(param.Value.Get(context), param.Value.Direction));
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
                DataTable dt = affectedRecords.Result;
                if (dt == null) return;

                DataTable.Set(asyncCodeActivityContext, dt);
                foreach (var param in affectedRecords.ParametersBind)
                {
                    var currentParam = Parameters[param.Key];
                    if (currentParam.Direction == ArgumentDirection.Out || currentParam.Direction == ArgumentDirection.InOut)
                    {
                        currentParam.Set(asyncCodeActivityContext, param.Value.Item1);
                    }
                }
            };
        }

        private class DBExecuteQueryResult
        {
            public DataTable Result { get; }
            public Dictionary<string, Tuple<object, ArgumentDirection>> ParametersBind { get; }

            public DBExecuteQueryResult()
            {
                this.Result = new DataTable();
                this.ParametersBind = new Dictionary<string, Tuple<object, ArgumentDirection>>();
            }

            public DBExecuteQueryResult(DataTable result, Dictionary<string, Tuple<object, ArgumentDirection>> parametersBind)
            {
                this.Result = result;
                this.ParametersBind = parametersBind;
            }
        }
    }
}
