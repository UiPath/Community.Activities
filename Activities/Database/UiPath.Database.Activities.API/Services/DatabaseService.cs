using System;
using UiPath.Database;
using UiPath.Database.Activities.API.Models;

namespace UiPath.Database.Activities.API
{
    internal class DatabaseService : IDatabaseService
    {
        private readonly IDBConnectionFactory _connectionFactory;

        public DatabaseService()
            : this(new DBConnectionFactory())
        {
        }

        internal DatabaseService(IDBConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public DatabaseConnection UseConnection(DatabaseScopeOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            if (string.IsNullOrWhiteSpace(options.ProviderName))
                throw new ArgumentException("ProviderName must not be null or whitespace.", nameof(options));
            if (string.IsNullOrWhiteSpace(options.ConnectionString))
                throw new ArgumentException("ConnectionString must not be null or whitespace.", nameof(options));

            return _connectionFactory.Create(options.ConnectionString, options.ProviderName);
        }
    }
}
