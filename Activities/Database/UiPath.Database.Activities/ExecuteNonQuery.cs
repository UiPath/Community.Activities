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
    public class ExecuteNonQuery : AsyncCodeActivity
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

        protected override System.IAsyncResult BeginExecute(AsyncCodeActivityContext context, System.AsyncCallback callback, object state)
        {
            string connString = null;
            SecureString connSecureString = null;
            string provName = null;
            string sql = string.Empty;
            int commandTimeout = TimeoutMS.Get(context);
            if (commandTimeout < 0)
            {
                throw new ArgumentException(UiPath.Database.Activities.Properties.Resources.TimeoutMSException, "TimeoutMS");
            }
            Dictionary<string, Tuple<object, ArgumentDirection>> parameters = null;
            try
            {
                sql = Sql.Get(context);
                DbConnection = ExistingDbConnection.Get(context);
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
            Func<DBExecuteCommandResult> action = () =>
            {
                DBExecuteCommandResult executeResult = new DBExecuteCommandResult();
                if (DbConnection == null)
                {
                    DbConnection = new DatabaseConnection().Initialize(connString != null ? connString : new NetworkCredential("", connSecureString).Password, provName);
                }
                if (DbConnection == null)
                {
                    return executeResult;
                }
                executeResult = new DBExecuteCommandResult(DbConnection.Execute(sql, parameters, commandTimeout, CommandType), parameters);
                return executeResult;
            };

            context.UserState = action;

            return action.BeginInvoke(callback, state);
        }

        private void HandleException(Exception ex, bool continueOnError)
        {
            if (continueOnError) return;
            throw ex;
        }

        protected override void EndExecute(AsyncCodeActivityContext context, System.IAsyncResult result)
        {
            DatabaseConnection existingConnection = ExistingDbConnection.Get(context);
            try
            {
                Func<DBExecuteCommandResult> action = (Func<DBExecuteCommandResult>)context.UserState;
                DBExecuteCommandResult commandResult = action.EndInvoke(result);
                this.AffectedRecords.Set(context, commandResult.Result);
                foreach (var param in commandResult.ParametersBind)
                {
                    var currentParam = Parameters[param.Key];
                    if (currentParam.Direction == ArgumentDirection.Out || currentParam.Direction == ArgumentDirection.InOut)
                    {
                        currentParam.Set(context, param.Value.Item1);
                    }
                }
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
