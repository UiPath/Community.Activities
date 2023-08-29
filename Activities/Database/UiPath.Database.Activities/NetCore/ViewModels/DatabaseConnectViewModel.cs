using System;
using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using UiPath.Database.Activities.NetCore.ViewModels;
using UiPath.Database.Activities.NetCore.ViewModels.Helpers;
using UiPath.Database.Activities.Properties;

namespace UiPath.Database.Activities
{
    /// <summary>
    /// Connects to a database by using a standard connection string.
    /// </summary>
    [ViewModelClass(typeof(DatabaseConnectViewModel))]
    public partial class DatabaseConnect
    {
    }
}

namespace UiPath.Database.Activities.NetCore.ViewModels
{
    public partial class DatabaseConnectViewModel : DesignPropertiesViewModel
    {
        private const string DefaultProviderNameValue = "Microsoft.Data.SqlClient";

        private static string[] ProviderNameDataSourceItems { get => new string[] { "Microsoft.Data.SqlClient", "System.Data.OleDb", "System.Data.Odbc", "Oracle.ManagedDataAccess.Client" }; }

        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public DatabaseConnectViewModel(IDesignServices services) : base(services)
        {
        }

        /// <summary>
        /// The name of the database provider used to access the database.
        /// </summary>
        public DesignInArgument<string> ProviderName { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The connection string used to establish a database connection.
        /// </summary>
        public DesignInArgument<string> ConnectionString { get; set; } = new DesignInArgument<string>() { Name = nameof(ConnectionString)};

        /// <summary>
        /// The connection string used to establish a database connection as Secure String.
        /// </summary>
        public DesignInArgument<SecureString> ConnectionSecureString { get; set; } = new DesignInArgument<SecureString>() { Name = nameof(ConnectionSecureString)};

        /// <summary>
        /// The database connection used for the operations within this activity.
        /// </summary>
        public DesignOutArgument<DatabaseConnection> DatabaseConnection { get; set; } = new DesignOutArgument<DatabaseConnection>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            InitializeConnectionFields();
            PersistValuesChangedDuringInit();

            int propertyOrderIndex = 1;

            ProviderName.IsPrincipal = true;
            ProviderName.IsRequired = true;
            ProviderName.OrderIndex = propertyOrderIndex++;
            ProviderName.Widget = new DefaultWidget
            {
                Type = ViewModelWidgetType.AutoCompleteForExpression,
            };
            ProviderName.DataSource = DataSourceBuilder<string>
                                            .WithId(p => p)
                                            .WithLabel(d => d)
                                            .WithData(ProviderNameDataSourceItems)
                                            .Build();
            ProviderName.Value ??= new InArgument<string>(DefaultProviderNameValue);

            ConnectionString.IsPrincipal = true;
            ConnectionString.IsRequired = true;
            ConnectionString.OrderIndex = propertyOrderIndex++;

            ConnectionSecureString.IsPrincipal = true;
            ConnectionSecureString.OrderIndex = propertyOrderIndex++;

            DatabaseConnection.OrderIndex = propertyOrderIndex;
            DatabaseConnection.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            var useConnectionStringMenuAction = new MenuAction
            {
                DisplayName = Resources.ConnectionStringMenuAction,
                Handler = (_) => DesignPropertyHelpers.ToggleDesignProperties(ConnectionString, ConnectionSecureString)
            };
            var useConnectionSecureStringMenuAction = new MenuAction
            {
                DisplayName = Resources.ConnectionSecureStringMenuAction,
                Handler = (_) => DesignPropertyHelpers.ToggleDesignProperties(ConnectionSecureString, ConnectionString)
            };

            ConnectionString.AddMenuAction(useConnectionSecureStringMenuAction);
            ConnectionSecureString.AddMenuAction(useConnectionStringMenuAction);
        }

        private void InitializeConnectionFields()
        {
            if(ConnectionSecureString.Value != null)
            {
                DesignPropertyHelpers.ToggleDesignProperties(ConnectionSecureString, ConnectionString);
            }
            else
            {
                DesignPropertyHelpers.ToggleDesignProperties(ConnectionString, ConnectionSecureString);
            }
        }
    }
}
