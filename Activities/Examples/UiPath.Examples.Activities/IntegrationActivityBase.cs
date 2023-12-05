
namespace UiPath.Examples.Activities
{
    public class IntegrationActivityBase : BindingsActivityBase
    {
        public string ConnectionId { get; set; }
        //public Connectors Connector { get; set; }

        public IntegrationActivityBase()
        {   
            Type = "Connection";
            Key = GenerateBindingsKey();
        }

        internal virtual string GenerateBindingsKey()
        {
            return $"{ConnectionId}.{DisplayName}";
        }
    }
}
