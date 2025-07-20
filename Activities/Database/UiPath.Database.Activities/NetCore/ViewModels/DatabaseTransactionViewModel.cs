using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Security;
using System.Threading.Tasks;
using UiPath.Database.Activities.NetCore.ViewModels;

namespace UiPath.Database.Activities
{
    /// <summary>
    /// Connects to a database and features a Sequence which can perform multiple transactions with the database.
    /// </summary>
    [ViewModelClass(typeof(DatabaseTransactionViewModel))]
    public partial class DatabaseTransaction
    {
    }
}

namespace UiPath.Database.Activities.NetCore.ViewModels
{
    public partial class DatabaseTransactionViewModel : DesignPropertiesViewModel
    {
        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public DatabaseTransactionViewModel(IDesignServices services) : base(services)
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
        /// The DatabaseConnection variable you want to use for connecting to a database
        /// </summary>
        public DesignInArgument<DatabaseConnection> ExistingDbConnection { get; set; } = new DesignInArgument<DatabaseConnection>();

        /// <summary>
        /// Specifies if the automation should continue even when the activity throws an error.
        /// </summary>
        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        /// <summary>
        /// The database connection variable returned by the activity. 
        /// </summary>
        public DesignOutArgument<DatabaseConnection> DatabaseConnection { get; set; } = new DesignOutArgument<DatabaseConnection>();

        /// <summary>
        /// Specifies if the database operations within this activity should be wrapped in a database transaction.
        /// </summary>
        public DesignProperty<bool> UseTransaction { get; set; } = new DesignProperty<bool>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            int propertyOrderIndex = 1;

            ExistingDbConnection.IsPrincipal = true;
            ExistingDbConnection.OrderIndex = propertyOrderIndex++;
            ExistingDbConnection.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            DatabaseConnection.IsPrincipal = true;
            DatabaseConnection.OrderIndex = propertyOrderIndex++;
            DatabaseConnection.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            ConnectionString.OrderIndex = propertyOrderIndex++;
            ConnectionString.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            ProviderName.OrderIndex = propertyOrderIndex++;
            ProviderName.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            ConnectionSecureString.OrderIndex = propertyOrderIndex++;
            ConnectionSecureString.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            UseTransaction.OrderIndex = propertyOrderIndex++;
            UseTransaction.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            ContinueOnError.OrderIndex = propertyOrderIndex++;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.NullableBoolean };
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
