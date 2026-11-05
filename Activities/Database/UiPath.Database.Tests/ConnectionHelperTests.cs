using System;
using System.Activities;
using Moq;
using Shouldly;
using UiPath.Database.Activities;
using UiPath.Database.Activities.Properties;
using Xunit;

namespace UiPath.Database.Tests
{
    public class ConnectionHelperTests
    {
        [Fact]
        public void ConnectionValidation_NoConnectionAndNoSettings_KeepsMessageAndParamNameSeparate()
        {
            var ex = Should.Throw<ArgumentNullException>(() => ConnectionHelper.ConnectionValidation(null));

            AssertMessageAndParamNameSeparate(ex, Resources.ValidationError_ConnectionNotValid, "ExistingDbConnection");
        }

        [Fact]
        public void ConnectionValidation_NoProvider_KeepsMessageAndParamNameSeparate()
        {
            var ex = Should.Throw<ArgumentNullException>(() => ConnectionHelper.ConnectionValidation(null, null, "Data Source=x", null));

            AssertMessageAndParamNameSeparate(ex, Resources.ValidationError_ProviderNull, "ProviderName");
        }

        [Fact]
        public void ConnectionValidation_NoConnectionString_KeepsMessageAndParamNameSeparate()
        {
            var ex = Should.Throw<ArgumentNullException>(() => ConnectionHelper.ConnectionValidation(null, null, null, "System.Data.SqlClient"));

            AssertMessageAndParamNameSeparate(ex, Resources.ValidationError_ConnectionStringMustNotBeNull, "ConnectionString");
        }

        [Fact]
        public void DatabaseConnect_NoConnectionString_KeepsMessageAndParamNameSeparate()
        {
            var factory = new Mock<IDBConnectionFactory>();
            var activity = new DatabaseConnect(factory.Object)
            {
                ConnectionString = new InArgument<string>((string)null),
                ProviderName = new InArgument<string>("System.Data.SqlClient")
            };

            var ex = Should.Throw<ArgumentNullException>(() => WorkflowInvoker.Invoke(activity, TimeSpan.FromSeconds(30)));

            AssertMessageAndParamNameSeparate(ex, Resources.ValidationError_ConnectionStringMustNotBeNull, "ConnectionString");
        }

        private static void AssertMessageAndParamNameSeparate(ArgumentNullException ex, string expectedMessage, string expectedParamName)
        {
            ex.Message.ShouldStartWith(expectedMessage);
            ex.Message.ShouldNotStartWith("Value cannot be null");
            ex.ParamName.ShouldBe(expectedParamName);
        }
    }
}
