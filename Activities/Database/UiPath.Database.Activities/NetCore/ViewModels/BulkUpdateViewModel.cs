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
        /// An already open database connection. If such a connection is provided, the ConnectionString and SecureConnectionString properties are ignored.
        /// </summary>
        public DesignInArgument<DatabaseConnection> ExistingDbConnection { get; set; } = new DesignInArgument<DatabaseConnection>();

        /// <summary>
        /// The DataTable object that will be used in updating the table.
        /// </summary>
        public DesignInArgument<DataTable> DataTable { get; set; } = new DesignInArgument<DataTable>();

        /// <summary>
        /// The target database table.
        /// </summary>
        public DesignInArgument<string> TableName { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The collection of column names used for row matching.
        /// </summary>
        public DesignInArgument<string[]> ColumnNames { get; set; } = new DesignInArgument<string[]>();


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

            ColumnNames.IsPrincipal = true;
            ColumnNames.IsRequired = true;
            ColumnNames.OrderIndex = propertyOrderIndex++;
            ColumnNames.Widget = new DefaultWidget { Type = ViewModelWidgetType.Collection };

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
