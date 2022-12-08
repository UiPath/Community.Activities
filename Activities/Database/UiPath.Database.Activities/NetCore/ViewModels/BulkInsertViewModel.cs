using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Data;
using System.Security;
using System.Threading.Tasks;
using UiPath.Database.Activities.NetCore.ViewModels;

namespace UiPath.Database.Activities
{
    /// <summary>
    /// Updates a table using Bulk operations using the specific database driver implementation.
    /// </summary>
    [ViewModelClass(typeof(BulkInsertViewModel))]
    public partial class BulkInsert
    {
    }
}

namespace UiPath.Database.Activities.NetCore.ViewModels
{
    public partial class BulkInsertViewModel : DesignPropertiesViewModel
    {
        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public BulkInsertViewModel(IDesignServices services) : base(services)
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
        /// An already open database connection. If such a connection is provided, the ConnectionString and SecureConnectionString properties are ignored.
        /// </summary>
        public DesignInArgument<DatabaseConnection> ExistingDbConnection { get; set; } = new DesignInArgument<DatabaseConnection>();

        /// <summary>
        /// The destination database table name.
        /// </summary>
        public DesignInArgument<string> TableName { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The source DataTable for the items to be inserted.
        /// </summary>
        public DesignInArgument<DataTable> DataTable { get; set; } = new DesignInArgument<DataTable>();

        /// <summary>
        /// Specifies if the automation should continue even when the activity throws an error.
        /// </summary>
        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        /// <summary>
        /// The number of updated rows.
        /// </summary>
        public DesignOutArgument<long> AffectedRecords { get; set; } = new DesignOutArgument<long>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            int propertyOrderIndex = 1;

            ExistingDbConnection.IsPrincipal = true;
            ExistingDbConnection.OrderIndex = propertyOrderIndex++;
            ExistingDbConnection.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            TableName.IsPrincipal = true;
            TableName.IsRequired = true;
            TableName.OrderIndex = propertyOrderIndex++;
            TableName.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            DataTable.IsPrincipal = true;
            DataTable.IsRequired = true;
            DataTable.OrderIndex = propertyOrderIndex++;
            DataTable.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            AffectedRecords.IsPrincipal = true;
            AffectedRecords.OrderIndex = propertyOrderIndex++;
            AffectedRecords.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            ConnectionString.OrderIndex = propertyOrderIndex++;
            ConnectionString.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            ProviderName.OrderIndex = propertyOrderIndex++;
            ProviderName.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            ConnectionSecureString.OrderIndex = propertyOrderIndex++;
            ConnectionSecureString.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input};

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
