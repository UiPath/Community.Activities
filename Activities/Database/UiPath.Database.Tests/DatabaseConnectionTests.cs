﻿
using Microsoft.Activities.UnitTesting;
using Moq;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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
        [Theory]
        [InlineData("System.Data.Odbc")]
        [InlineData("System.Data.Oledb")]
        [InlineData("System.Data.OracleClient")]
        [InlineData("System.Data.SqlClient")]
        [InlineData("Oracle.DataAccess.Client")]
        [InlineData("Oracle.ManagedDataAccess.Client")]
        [InlineData("Mysql.Data.MysqlClient")]
        public void TestSize(string provider)
        {
            var con = new Mock<DbConnection>();
            var cmd = new Mock<DbCommand>();
            var dbParameterCollection = new Mock<DbParameterCollection>();
            var param = new Mock<DbParameter>();
            var dataReader = new Mock<DbDataReader>();

            con.SetReturnsDefault<DbCommand>(cmd.Object);
            con.SetReturnsDefault<string>(provider);

            cmd.SetReturnsDefault<DbParameterCollection>(dbParameterCollection.Object);
            cmd.SetReturnsDefault<DbParameter>(param.Object);
            cmd.SetReturnsDefault<DbDataReader>(dataReader.Object);
            
            param.SetupAllProperties();
            param.SetReturnsDefault<ParameterDirection>(ParameterDirection.InputOutput);

            var databaseConnection = new DatabaseConnection().Initialize(con.Object);
            var parameters = new Dictionary<string, Tuple<object, ArgumentDirection>>() { { "param1", new Tuple<object, ArgumentDirection>("", ArgumentDirection.Out) } };
            databaseConnection.ExecuteQuery("TestProcedure", parameters, 0);
            if(provider.ToLower().Contains("oracle"))
                Assert.True(param.Object.Size == 1000000);
            if (!provider.ToLower().Contains("oracle"))
                Assert.True(param.Object.Size == -1);
        }
    }
}
