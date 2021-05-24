using System;
using System.Activities;
using System.ComponentModel;
using System.Data.Common;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security;
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
        [LocalizedDisplayName(nameof(Resources.ConnectionStringDisplayName))]
        public InArgument<string> ConnectionString { get; set; }

        [DefaultValue(null)]
        [DependsOn(nameof(ProviderName))]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.ConnectionSecureStringDisplayName))]
        public InArgument<SecureString> ConnectionSecureString { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [DependsOn(nameof(ProviderName))]
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
            var connString = ConnectionString.Get(context);
            var connSecureString = ConnectionSecureString.Get(context);
            if (connString==null && connSecureString==null)
            {
                throw new ArgumentNullException(Resources.ConnectionMustBeSet);
            }
            var provName = ProviderName.Get(context);
            Func<DatabaseConnection> action = () => _connectionFactory.Create(connString != null ? connString : new NetworkCredential("", connSecureString).Password, provName);
            context.UserState = action;

            return action.BeginInvoke(callback, state);
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            Func<DatabaseConnection> action = (Func<DatabaseConnection>)context.UserState;
            var dbConn = action.EndInvoke(result);
            DatabaseConnection.Set(context, dbConn);
        }
    }
}
