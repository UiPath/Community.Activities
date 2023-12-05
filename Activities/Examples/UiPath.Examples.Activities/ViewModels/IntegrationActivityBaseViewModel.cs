using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Diagnostics;

namespace UiPath.Examples.Activities.ViewModels
{
    public class IntegrationActivityBaseViewModel : DesignPropertiesViewModel
    {
        DesignProperty<string> ConnectionId { get; set; } = new DesignProperty<string>();
        //DesignProperty<Connectors> Connector { get; set; } = new DesignProperty<Connectors>();

        public IntegrationActivityBaseViewModel(IDesignServices services) : base(services)
        {
        }

        protected override void InitializeModel()
        {
            Debugger.Launch();
            Debugger.Break();

            base.InitializeModel();
            PersistValuesChangedDuringInit();

            var orderIndex = 0;

            ConnectionId.IsPrincipal = true;
            ConnectionId.IsVisible = false;
            ConnectionId.OrderIndex = orderIndex++;

            //Connector.IsVisible = true;
            //Connector.OrderIndex = orderIndex++;
            //Connector.IsPrincipal = true;
            //Connector.DataSource = DataSourceBuilder<string>.WithId(e => e).WithLabel(e => e).WithData(new string[]
            //{
            //    ConnectorConstants.Outlook365GraphConnector,
            //    ConnectorConstants.GmailConnector,
            //}).Build();
        }

        protected override void InitializeRules()
        {
            base.InitializeRules();
            //Rule(nameof(Connector), OnConnectorChanged, runOnInit: false);
        }

        private void OnConnectorChanged()
        {
            //if (!string.IsNullOrEmpty(Connector.Value.ToString()))
            //{
            //    ConnectionId.Widget = new DefaultWidget { Type = ViewModelWidgetType.Connection, Metadata = new Dictionary<string, string> { { nameof(Connector), Connector.Value.ToString() } } };
            //}
        }
    }
}
