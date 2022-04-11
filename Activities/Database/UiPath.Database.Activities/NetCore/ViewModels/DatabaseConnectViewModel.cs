using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Security;
using System.Threading.Tasks;
using UiPath.Database.Activities.NetCore.ViewModels;

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
        public DesignInArgument<string> ConnectionString { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The connection string used to establish a database connection as Secure String.
        /// </summary>
        public DesignInArgument<SecureString> ConnectionSecureString { get; set; } = new DesignInArgument<SecureString>();

        /// <summary>
        /// The database connection used for the operations within this activity.
        /// </summary>
        public DesignOutArgument<DatabaseConnection> DatabaseConnection { get; set; } = new DesignOutArgument<DatabaseConnection>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            int propertyOrderIndex = 1;

            ConnectionString.IsPrincipal = true;
            ConnectionString.OrderIndex = propertyOrderIndex++;
            ConnectionString.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            ProviderName.IsPrincipal = true;
            ProviderName.IsRequired = true;
            ProviderName.OrderIndex = propertyOrderIndex++;
            ProviderName.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            ConnectionSecureString.IsPrincipal = true;
            ConnectionSecureString.OrderIndex = propertyOrderIndex++;
            ConnectionSecureString.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            DatabaseConnection.OrderIndex = propertyOrderIndex++;
            DatabaseConnection.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

        }

        protected override async ValueTask InitializeModelAsync()
        {
            await base.InitializeModelAsync();
        }

        protected override void InitializeRules()
        {
            base.InitializeRules();
        }
    }
}
