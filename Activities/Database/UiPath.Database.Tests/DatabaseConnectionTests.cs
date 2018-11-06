
using Microsoft.Activities.UnitTesting;
using Moq;
using System;
using System.Activities;
using System.Dynamic;
using UiPath.Database.Activities;
using Xunit;

namespace UiPath.Database.Tests
{
    public class DatabaseConnectionTests
    {
        [Fact]
        public void TestConnect()
        {
            var factory = new Mock<IDBConnectionFactory>();
            factory.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<string>()))
                                .Returns(() =>  new DatabaseConnection());
            var connectActivity = new DatabaseConnect(factory.Object)
                                      {
                                        ConnectionString = new InArgument<string>("alpha"),
                                        ProviderName = new InArgument<string>("beta")
                                      };
            var host = new WorkflowInvokerTest(connectActivity);
            var ex = Record.Exception(() => host.TestActivity());
            Assert.Null(ex);
        }

        [Fact]
        public void TestDisconect()
        {
            var dbConnection = new Mock<DatabaseConnection>();
            var executed = false;
            dbConnection.Setup(con => con.Dispose()).Callback(() => executed = true);
            dynamic arguments = new ExpandoObject();
            arguments.DatabaseConnection = dbConnection.Object;
            var host = new WorkflowInvokerTest(new DatabaseDisconnect(), arguments);
            var ex = Record.Exception(() => host.TestActivity());
            Assert.Null(ex);
            Assert.True(executed);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestDatabaseTransactionWithEmptyBody(bool useTransaction)
        {
            var dbConnection = new Mock<DatabaseConnection>();
            var executed = false;
            dbConnection.Setup(con => con.BeginTransaction()).Callback(() => executed = true);
            dynamic arguments = new ExpandoObject();
            arguments.ExistingDbConnection = dbConnection.Object;
            var dbTransactionActivity = new DatabaseTransaction { UseTransaction = useTransaction };
            var host = new WorkflowInvokerTest(dbTransactionActivity, arguments);
            var ex = Record.Exception(() => host.TestActivity());
            Assert.Null(ex);
            Assert.True(executed == useTransaction);

        }
    }
}
