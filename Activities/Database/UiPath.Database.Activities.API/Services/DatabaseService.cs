using System;
using UiPath.Database;
using UiPath.Database.Activities.API.Models;
using UiPath.Robot.Activities.Api;

namespace UiPath.Database.Activities.API
{
    internal class DatabaseService : IDatabaseService
    {
        private readonly IDBConnectionFactory _connectionFactory;

        public DatabaseService(IExecutorRuntime executorRuntime)
            : this(executorRuntime, new DBConnectionFactory())
        {
        }

        internal DatabaseService(IExecutorRuntime executorRuntime, IDBConnectionFactory connectionFactory)
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
