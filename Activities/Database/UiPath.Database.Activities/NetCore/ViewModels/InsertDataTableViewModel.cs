using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Data;
using System.Security;
using System.Threading.Tasks;
using UiPath.Database.Activities.NetCore.ViewModels;

namespace UiPath.Database.Activities
{
    /// <summary>
    /// Inserts a compatible DataTable variable in an existing Table. 
    /// </summary>
    [ViewModelClass(typeof(InsertDataTableViewModel))]
    public partial class InsertDataTable
    {
    }
}

namespace UiPath.Database.Activities.NetCore.ViewModels
{
    public partial class InsertDataTableViewModel : DesignPropertiesViewModel
    {
        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public InsertDataTableViewModel(IDesignServices services) : base(services)
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
        /// An already opened database connection obtained from the Connect or Start Transaction activities.
        /// </summary>
        public DesignInArgument<DatabaseConnection> ExistingDbConnection { get; set; } = new DesignInArgument<DatabaseConnection>();

        /// <summary>
        /// The SQL table in which the data is to be inserted.
        /// </summary>
        public DesignInArgument<string> TableName { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The DataTable variable that will be inserted into the Table. 
        /// </summary>
        public DesignInArgument<DataTable> DataTable { get; set; } = new DesignInArgument<DataTable>();

        /// <summary>
        /// Specifies if the automation should continue even when the activity throws an error.
        /// </summary>
        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        /// <summary>
        /// Stores the number of affected rows into an Int32 variable.
        /// </summary>
        public DesignOutArgument<int> AffectedRecords { get; set; } = new DesignOutArgument<int>();

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
            TableName.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            AffectedRecords.IsPrincipal = true;
            AffectedRecords.OrderIndex = propertyOrderIndex++;
            AffectedRecords.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            ConnectionString.OrderIndex = propertyOrderIndex++;
            ConnectionString.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            ProviderName.OrderIndex = propertyOrderIndex++;
            ProviderName.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            ConnectionSecureString.OrderIndex = propertyOrderIndex++;
            ConnectionSecureString.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

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
