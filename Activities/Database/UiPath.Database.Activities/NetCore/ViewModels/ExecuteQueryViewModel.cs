using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Data;
using System.Security;
using System.Threading.Tasks;
using UiPath.Database.Activities.NetCore.ViewModels;

namespace UiPath.Database.Activities
{
    /// <summary>
    /// Executes an non query statement on a database.
    /// </summary>
    [ViewModelClass(typeof(ExecuteQueryViewModel))]
    public partial class ExecuteQuery
    {
    }
}

namespace UiPath.Database.Activities.NetCore.ViewModels
{
    public partial class ExecuteQueryViewModel : DesignPropertiesViewModel
    {
        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public ExecuteQueryViewModel(IDesignServices services) : base(services)
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
        /// An already open database connection.
        /// </summary>
        public DesignInArgument<DatabaseConnection> ExistingDbConnection { get; set; } = new DesignInArgument<DatabaseConnection>();

        /// <summary>
        /// Specifies how a command string is interpreted.
        /// </summary>
        public DesignProperty<CommandType> CommandType { get; set; } = new DesignProperty<CommandType>();

        /// <summary>
        /// An sql command to be executed.
        /// </summary>
        public DesignInArgument<string> Sql { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// Specifies if the automation should continue even when the activity throws an error.
        /// </summary>
        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        /// <summary>
        /// Specifies the amount of time (in millisecond) to wait for the sql command to run before an error is thrown.
        /// </summary>
        public DesignInArgument<int> TimeoutMS { get; set; } = new DesignInArgument<int>();

        /// <summary>
        /// A dictionary of named parameters that are bound to the sql command.
        /// </summary>
        public DesignProperty<Dictionary<string, Argument>> Parameters { get; set; } = new DesignProperty<Dictionary<string, Argument>>();

        /// <summary>
        /// The result of the execution of the sql command.
        /// </summary>
        public DesignOutArgument<int> DataTable { get; set; } = new DesignOutArgument<int>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            int propertyOrderIndex = 1;

            ExistingDbConnection.IsPrincipal = true;
            ExistingDbConnection.OrderIndex = propertyOrderIndex++;
            ExistingDbConnection.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            Sql.IsPrincipal = true;
            Sql.IsRequired = true;
            Sql.OrderIndex = propertyOrderIndex++;
            Sql.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            ConnectionString.OrderIndex = propertyOrderIndex++;
            ConnectionString.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            ProviderName.OrderIndex = propertyOrderIndex++;
            ProviderName.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            ConnectionSecureString.OrderIndex = propertyOrderIndex++;
            ConnectionSecureString.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            CommandType.OrderIndex = propertyOrderIndex++;
            CommandType.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown };

            TimeoutMS.OrderIndex = propertyOrderIndex++;
            TimeoutMS.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            Parameters.OrderIndex = propertyOrderIndex++;
            Parameters.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dictionary };

            DataTable.OrderIndex = propertyOrderIndex++;
            DataTable.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

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
