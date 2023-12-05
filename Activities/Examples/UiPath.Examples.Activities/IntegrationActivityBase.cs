namespace UiPath.Examples.Activities
{
    public abstract class IntegrationActivityBase<T> : BindingsActivityBase<T>
    {
        public string ConnectionId { get; set; }
        public string Connector { get; set; }

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
