using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Data;
using System.Security;
using System.Threading.Tasks;
using UiPath.Database.Activities.NetCore.ViewModels;

namespace UiPath.Database.Activities
{
    /// <summary>
    /// Updates a compatible DataTable in an existing Table. 
    /// </summary>
    [ViewModelClass(typeof(BulkUpdateViewModel))]
    public partial class BulkUpdate
    {
    }
}

namespace UiPath.Database.Activities.NetCore.ViewModels
{
    public partial class BulkUpdateViewModel : DesignPropertiesViewModel
    {
        /// <summary>
        /// Basic constructor.
        /// </summary>
        /// <param name="services"></param>
        public BulkUpdateViewModel(IDesignServices services) : base(services)
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
        /// Check this box to enable the creation of a temp table using Bulk insert and to update using join between tables.
        /// </summary>
        public DesignProperty<bool> BulkUpdateFlag { get; set; } = new DesignProperty<bool>();

        /// <summary>
        /// The target database table.
        /// </summary>
        public DesignInArgument<string> TableName { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The collection of column names used for row matching.
        /// </summary>
        public DesignInArgument<string[]> ColumnNames { get; set; } = new DesignInArgument<string[]>();

        /// <summary>
        /// The DataTable object that will be used in updating the table.
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

            ColumnNames.IsPrincipal = true;
            ColumnNames.IsRequired = true;
            ColumnNames.OrderIndex = propertyOrderIndex++;
            ColumnNames.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            BulkUpdateFlag.OrderIndex = propertyOrderIndex++;
            BulkUpdateFlag.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            ConnectionString.OrderIndex = propertyOrderIndex++;
            ConnectionString.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            ProviderName.OrderIndex = propertyOrderIndex++;
            ProviderName.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            ConnectionSecureString.OrderIndex = propertyOrderIndex++;
            ConnectionSecureString.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            ContinueOnError.OrderIndex = propertyOrderIndex++;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.NullableBoolean };

            AffectedRecords.OrderIndex = propertyOrderIndex++;
            AffectedRecords.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };
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
