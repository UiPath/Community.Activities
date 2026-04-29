using System;
using Moq;
using Shouldly;
using UiPath.Database;
using UiPath.Database.Activities.API.Models;
using Xunit;

namespace UiPath.Database.Activities.API.Tests
{
    public class DatabaseServiceTests
    {
        private readonly Mock<IDBConnectionFactory> _factoryMock;
        private readonly DatabaseService _databaseService;

        public DatabaseServiceTests()
        {
            _factoryMock = new Mock<IDBConnectionFactory>();
            _databaseService = new DatabaseService(executorRuntime: null, _factoryMock.Object);
        }

        [Fact]
        public void UseConnection_NullOptions_Throws()
        {
            Should.Throw<ArgumentNullException>(() => _databaseService.UseConnection(null));
        }

        [Fact]
        public void UseConnection_NullProviderName_Throws()
        {
            var options = new DatabaseScopeOptions { ProviderName = null, ConnectionString = "Data Source=test" };

            Should.Throw<ArgumentException>(() => _databaseService.UseConnection(options));
        }

        [Fact]
        public void UseConnection_EmptyProviderName_Throws()
        {
            var options = new DatabaseScopeOptions { ProviderName = "   ", ConnectionString = "Data Source=test" };

            Should.Throw<ArgumentException>(() => _databaseService.UseConnection(options));
        }

        [Fact]
        public void UseConnection_NullConnectionString_Throws()
        {
            var options = new DatabaseScopeOptions { ProviderName = "System.Data.SqlClient", ConnectionString = null };

            Should.Throw<ArgumentException>(() => _databaseService.UseConnection(options));
        }

        [Fact]
        public void UseConnection_EmptyConnectionString_Throws()
        {
            var options = new DatabaseScopeOptions { ProviderName = "System.Data.SqlClient", ConnectionString = "  " };

            Should.Throw<ArgumentException>(() => _databaseService.UseConnection(options));
        }

        [Fact]
        public void UseConnection_ValidOptions_CallsFactoryAndReturnsConnection()
        {
            var options = new DatabaseScopeOptions { ProviderName = "System.Data.SqlClient", ConnectionString = "Data Source=." };
            var mockConnection = new Mock<DatabaseConnection>().Object;
            _factoryMock.Setup(f => f.Create(options.ConnectionString, options.ProviderName)).Returns(mockConnection);

            using var connection = _databaseService.UseConnection(options);

            connection.ShouldBeSameAs(mockConnection);
            _factoryMock.Verify(f => f.Create(options.ConnectionString, options.ProviderName), Times.Once);
        }
    }
}
