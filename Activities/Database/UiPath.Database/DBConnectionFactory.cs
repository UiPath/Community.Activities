namespace UiPath.Database
{
    public class DBConnectionFactory : IDBConnectionFactory
    {
        public DatabaseConnection Create(string connectionString, string providerName)
        {
            var conn = new DatabaseConnection();
            return conn.Initialize(connectionString, providerName);
        }
    }

    public interface IDBConnectionFactory
    {
        DatabaseConnection Create(string connectionString, string providerName);
    }
}