using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Threading.Tasks;
using UiPath.Database.Activities.NetCore.ViewModels;

namespace UiPath.Database.Activities
{
    /// <summary>
    /// Closes a connection to a database.
    /// </summary>
    [ViewModelClass(typeof(DatabaseDisconnectViewModel))]
    public partial class DatabaseDisconnect
    {
    }
}

namespace UiPath.Database.Activities.NetCore.ViewModels
{
    public partial class DatabaseDisconnectViewModel : DesignPropertiesViewModel
    {
        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public DatabaseDisconnectViewModel(IDesignServices services) : base(services)
        {
        }

        /// <summary>
        /// The database connection used for operations within this activity.
        /// </summary>
        public DesignInArgument<DatabaseConnection> DatabaseConnection { get; set; } = new DesignInArgument<DatabaseConnection>();

        protected override void InitializeModel()
        {
            base.InitializeModel();

            DatabaseConnection.IsPrincipal = true;
            DatabaseConnection.IsRequired = true;
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
