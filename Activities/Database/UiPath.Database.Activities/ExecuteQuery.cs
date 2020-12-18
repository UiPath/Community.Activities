using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using UiPath.Database.Activities.Properties;

namespace UiPath.Database.Activities
{
    public class ExecuteQuery : AsyncCodeActivity
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
        [LocalizedDisplayName(nameof(Resources.DataTableDisplayName))]
        public OutArgument<DataTable> DataTable { get; set; }

        private DatabaseConnection DbConnection = null;

        public ExecuteQuery()
        {
            CommandType = CommandType.Text;
        }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            try
            {
                var dataTable = DataTable.Get(context);
                string connString = null;
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
                    DbConnection = ExistingDbConnection.Get(context);
                    connString = ConnectionString.Get(context);
                    provName = ProviderName.Get(context);
                    sql = Sql.Get(context);
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

                // create the action for doing the actual work
                Func<DataTable> action = () =>
                    {
                        if (DbConnection == null)
                        {
                            DbConnection = new DatabaseConnection().Initialize(connString, provName);
                        }
                        if (DbConnection == null)
                        {
                            return null;
                        }
                        return DbConnection.ExecuteQuery(sql, parameters, commandTimeout, CommandType);
                    };

                context.UserState = action;

                return action.BeginInvoke(callback, state);
            }
            catch (DbException ex)
            {
                throw new Exception("[Database driver error]: " + ex.Message + " " + ex?.InnerException?.Message, ex);
            }
        }

        private void HandleException(Exception ex, bool continueOnError)
        {
            if (continueOnError) return;
            throw ex;
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            try
            {
                DatabaseConnection existingConnection = ExistingDbConnection.Get(context);
                try
                {
                    Func<DataTable> action = (Func<DataTable>)context.UserState;
                    DataTable dt = action.EndInvoke(result);
                    if (dt == null) return;

                    DataTable.Set(context, dt);
                }
                catch (Exception ex)
                {
                    HandleException(ex, ContinueOnError.Get(context));
                }
                finally
                {
                    if (existingConnection == null)
                    {
                        DbConnection.Dispose();
                    }
                }
            }
            catch (DbException ex)
            {
                throw new Exception("[Database driver error]: " + ex.Message + " " + ex?.InnerException?.Message, ex);
            }
        }
    }
}
