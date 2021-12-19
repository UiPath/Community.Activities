using System;
using System.Activities;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Markup;
using UiPath.Database.Activities.Properties;

namespace UiPath.Database.Activities
{
    [LocalizedDescription(nameof(Resources.Activity_DatabaseConnect_Description))]
    public partial class DatabaseConnect : AsyncTaskCodeActivity
    {
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseConnect_Property_ProviderName_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseConnect_Property_ProviderName_Description))]
        public InArgument<string> ProviderName { get; set; }

        [DependsOn(nameof(ProviderName))]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseConnect_Property_ConnectionString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseConnect_Property_ConnectionString_Description))]
        public InArgument<string> ConnectionString { get; set; }

        [DefaultValue(null)]
        [DependsOn(nameof(ProviderName))]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseConnect_Property_ConnectionSecureString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseConnect_Property_ConnectionSecureString_Description))]
        public InArgument<SecureString> ConnectionSecureString { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [DependsOn(nameof(ProviderName))]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseConnect_Property_DatabaseConnection_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseConnect_Property_DatabaseConnection_Description))]
        public OutArgument<DatabaseConnection> DatabaseConnection { get; set; }

        private readonly IDBConnectionFactory _connectionFactory;

        public DatabaseConnect()
        {
            _connectionFactory = new DBConnectionFactory();
        }

        internal DatabaseConnect(IDBConnectionFactory factory)
        {
            _connectionFactory = factory;
        }

        protected async override Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            var connString = ConnectionString.Get(context);
            var connSecureString = ConnectionSecureString.Get(context);
            if (connString == null && connSecureString == null)
            {
                throw new ArgumentNullException(Resources.ValidationError_ConnectionStringMustNotBeNull);
            }
            if (connString != null && connSecureString != null)
            {
                throw new ArgumentException(Resources.ValidationError_ConnectionStringMustBeSet);
            }
            var provName = ProviderName.Get(context);
            DatabaseConnection dbConnection = null;
            try
            {
                dbConnection = await Task.Run(() => _connectionFactory.Create(connString ?? new NetworkCredential("", connSecureString).Password, provName));
            }
            catch (Exception e)
            {
                Trace.TraceError($"{e}");
            }

            return asyncCodeActivityContext =>
            {
                DatabaseConnection.Set(asyncCodeActivityContext, dbConnection);
            };

        }

    }
}