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
        /// An already open database connection. If such a connection is provided, the ConnectionString and SecureConnectionString properties are ignored.
        /// </summary>
        public DesignInArgument<DatabaseConnection> ExistingDbConnection { get; set; } = new DesignInArgument<DatabaseConnection>();

        /// <summary>
        /// The source DataTable for the items to be inserted.
        /// </summary>
        public DesignInArgument<DataTable> DataTable { get; set; } = new DesignInArgument<DataTable>();

        /// <summary>
        /// The destination database table name.
        /// </summary>
        public DesignInArgument<string> TableName { get; set; } = new DesignInArgument<string>();

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

            DataTable.IsPrincipal = true;
            DataTable.IsRequired = true;
            DataTable.OrderIndex = propertyOrderIndex++;
            DataTable.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            TableName.IsPrincipal = true;
            TableName.IsRequired = true;
            TableName.OrderIndex = propertyOrderIndex++;
            TableName.Widget = new DefaultWidget { Type = ViewModelWidgetType.TextComposer };

            ContinueOnError.OrderIndex = propertyOrderIndex++;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.NullableBoolean };

            AffectedRecords.OrderIndex = propertyOrderIndex;
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
