using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UiPath.Database
{
    internal class DBConnectionFactory : IDBConnectionFactory
    {
        public DatabaseConnection Create(string connectionString, string providerName)
        {
            var conn = new DatabaseConnection();
            return conn.Initialize(connectionString, providerName);
        }
    }

    internal interface IDBConnectionFactory
    {
        DatabaseConnection Create(string connectionString, string providerName);
    }
}
