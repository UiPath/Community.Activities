using System;
using System.Activities;
using System.ComponentModel;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Windows.Markup;
using UiPath.Database.Activities.Properties;

namespace UiPath.Database.Activities
{
    public class DatabaseConnect : AsyncCodeActivity
    {
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.ProviderNameDisplayName))]
        public InArgument<string> ProviderName { get; set; }

        [DependsOn(nameof(ProviderName))]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.ConnectionStringDisplayName))]
        public InArgument<string> ConnectionString { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.DatabaseConnectionDisplayName))]
        public OutArgument<DatabaseConnection> DatabaseConnection { get; set; }

        private readonly IDBConnectionFactory  _connectionFactory;

        public DatabaseConnect()
        {
            _connectionFactory = new DBConnectionFactory();
        }

        internal DatabaseConnect(IDBConnectionFactory factory)
        {
            _connectionFactory = factory;
        }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            try
            {
                var connString = ConnectionString.Get(context);
                var provName = ProviderName.Get(context);
                Func<DatabaseConnection> action = () => _connectionFactory.Create(connString, provName);
                context.UserState = action;

                return action.BeginInvoke(callback, state);
            }
            catch (DbException ex)
            {
                throw new Exception("[Database driver error]: " + ex.Message + " " + ex?.InnerException?.Message, ex);
            }
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            try
            {
                Func<DatabaseConnection> action = (Func<DatabaseConnection>)context.UserState;
                var dbConn = action.EndInvoke(result);
                DatabaseConnection.Set(context, dbConn);
            }
            catch (DbException ex)
            {
                throw new Exception("[Database driver error]: " + ex.Message + " " + ex?.InnerException?.Message, ex);
            }
        }
    }
}
