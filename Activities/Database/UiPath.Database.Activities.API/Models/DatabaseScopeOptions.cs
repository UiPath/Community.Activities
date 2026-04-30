namespace UiPath.Database.Activities.API.Models
{
    /// <summary>
    /// Options for configuring a database connection scope.
    /// </summary>
    public class DatabaseScopeOptions
    {
        /// <summary>
        /// The ADO.NET provider name (e.g. <c>System.Data.SqlClient</c>, <c>Oracle.ManagedDataAccess.Client</c>).
        /// </summary>
        public string ProviderName { get; set; }

        /// <summary>
        /// The database connection string.
        /// </summary>
        public string ConnectionString { get; set; }
    }
}
