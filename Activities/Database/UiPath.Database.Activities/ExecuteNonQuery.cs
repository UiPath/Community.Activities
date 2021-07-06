using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Markup;
using UiPath.Database.Activities.Properties;

namespace UiPath.Database.Activities
{
    public class ExecuteNonQuery : AsyncTaskCodeActivity
    {
        // public arguments
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [RequiredArgument]
        [OverloadGroup("New Database Connection")]
        [LocalizedDisplayName(nameof(Resources.ProviderNameDisplayName))]
        public InArgument<string> ProviderName { get; set; }

        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [DependsOn(nameof(ProviderName))]
        [DefaultValue(null)]
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

        [DefaultValue(null)]
        [LocalizedDisplayName(nameof(Resources.CommandTypeDisplayName))]
        public CommandType CommandType { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        public InArgument<string> Sql { get; set; }

        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnErrorDisplayName))]
        public InArgument<bool> ContinueOnError { get; set; }

        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.TimeoutMSDisplayName))]
        public InArgument<int> TimeoutMS { get; set; }

        private Dictionary<string, Argument> parameters;

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [Browsable(true)]
        [LocalizedDisplayName(nameof(Resources.ParametersDisplayName))]
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
        [LocalizedDisplayName(nameof(Resources.AffectedRecordsDisplayName))]
        public OutArgument<int> AffectedRecords { get; set; }

        private DatabaseConnection DbConnection = null;

        public ExecuteNonQuery()
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
            Dictionary<string, Tuple<object, ArgumentDirection>> parameters = null;
            try
            {
                sql = Sql.Get(context);
                existingConnection = DbConnection = ExistingDbConnection.Get(context);
                connString = ConnectionString.Get(context);
                connSecureString = ConnectionSecureString.Get(context);
                provName = ProviderName.Get(context);

                if (Parameters != null)
                {
                    parameters = new Dictionary<string, Tuple<object, ArgumentDirection>>();
                    foreach (var param in Parameters)
                    {
                        parameters.Add(param.Key, new Tuple<object, ArgumentDirection>(param.Value.Get(context), param.Value.Direction));
                    }
                }

                if (DbConnection == null && connString == null && connSecureString == null)
                {
                    throw new ArgumentNullException(Resources.ConnectionMustBeSet);
                }

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
                HandleException(ex, ContinueOnError.Get(context));
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
                        currentParam.Set(asyncCodeActivityContext, param.Value.Item1);
                    }
                }
            };

        }

        private class DBExecuteCommandResult
        {
            public int Result { get; }
            public Dictionary<string, Tuple<object, ArgumentDirection>> ParametersBind { get; }

            public DBExecuteCommandResult()
            {
                this.Result = 0;
                this.ParametersBind = new Dictionary<string, Tuple<object, ArgumentDirection>>();
            }

            public DBExecuteCommandResult(int result, Dictionary<string, Tuple<object, ArgumentDirection>> parametersBind)
            {
                this.Result = result;
                this.ParametersBind = parametersBind;
            }
        }
    }
}
