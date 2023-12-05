using System.Activities.DesignViewModels;
using System.Activities.ViewModels;

namespace UiPath.Examples.Activities.ViewModels
{
    public class MailViewModel : DesignPropertiesViewModel
    {
        public DesignProperty<string> ConnectionId { get; set; }

        public DesignProperty<string> Connector { get; set; }
        /*
         * The result property comes from the activity's base class
         */
        public DesignOutArgument<string> Result { get; set; }

        public MailViewModel(IDesignServices services) : base(services)
        {
        }

        protected override void InitializeModel()
        {
            base.InitializeModel();

            PersistValuesChangedDuringInit();

            var orderIndex = 0;

            Connector.DisplayName = Resources.Connector_DisplayName;
            Connector.Tooltip = Resources.Connector_Tooltip;
            Connector.IsPrincipal = true;
            Connector.OrderIndex = orderIndex++;
            Connector.DataSource = DataSourceBuilder<string>.WithId(e => e).WithLabel(e => e).WithData(new string[]
            {
                ConnectorConstants.Outlook365GraphConnector,
                ConnectorConstants.OneDriveConnector,
                ConnectorConstants.GoogleDocsConnector,
                ConnectorConstants.GmailConnector,
                ConnectorConstants.GoogleDriveConnector,
                ConnectorConstants.GoogleSpreadsheetsConnector,
            }).Build();

            ConnectionId.DisplayName = Resources.ConnectionId_DisplayName;
            ConnectionId.Tooltip = Resources.ConnectionId_Tooltip;
            ConnectionId.IsRequired = true;
            ConnectionId.IsPrincipal = true;
            ConnectionId.OrderIndex = orderIndex++;

            Result.DisplayName = Resources.Calculator_Result_DisplayName;
            Result.Tooltip = Resources.Calculator_Result_Tooltip;
            Result.OrderIndex = orderIndex;
        }

        protected override void InitializeRules()
        {
            base.InitializeRules();
            Rule(nameof(Connector), OnConnectorChanged, runOnInit: false);
        }

        private void OnConnectorChanged()
        {
            if (!string.IsNullOrEmpty(Connector.Value))
            {
                ConnectionId.Widget = new DefaultWidget { Type = ViewModelWidgetType.Connection, Metadata = new Dictionary<string, string> { { nameof(Connector), Connector.Value } } };
            }
        }
    }
}
