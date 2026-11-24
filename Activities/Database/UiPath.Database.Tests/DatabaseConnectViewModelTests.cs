#if !WINDOWS // the ViewModels exist only in the portable (Studio Web) build
using System.Activities.DesignViewModels;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using UiPath.Database.Activities.NetCore.ViewModels;
using Xunit;

namespace UiPath.Database.Tests
{
    /// <summary>Property configuration of <see cref="DatabaseConnectViewModel"/>.</summary>
    public class DatabaseConnectViewModelTests
    {
        private static async Task<DatabaseConnectViewModel> InitializedAsync()
        {
            var vm = new DatabaseConnectViewModel(Mock.Of<IDesignServices>());
            await vm.InitializeAsync();
            return vm;
        }

        [Fact]
        public async Task InitializeAsync_DefaultsTheProviderToSqlServerAndOrdersTheProperties()
        {
            var vm = await InitializedAsync();

            Assert.True(vm.ProviderName.IsPrincipal);
            Assert.True(vm.ProviderName.IsRequired);
            Assert.Equal(1, vm.ProviderName.OrderIndex);
            Assert.NotNull(vm.ProviderName.Value);
            Assert.Equal(2, vm.ConnectionString.OrderIndex);
            Assert.Equal(3, vm.ConnectionSecureString.OrderIndex);
            Assert.Equal(4, vm.DatabaseConnection.OrderIndex);
            Assert.True(vm.ConnectionString.IsRequired);
        }

        [Fact]
        public async Task InitializeAsync_OffersTheBuiltInProvidersWithoutDuplicates()
        {
            var vm = await InitializedAsync();

            Assert.NotNull(vm.ProviderName.Widget);
            var data = ((DataSource<string>)vm.ProviderName.DataSource).Data.Select(item => item.ToString()).ToArray();

            Assert.Equal(
                new[] { DatabaseConstants.SqlServerProvider, DatabaseConstants.OleDbProvider, DatabaseConstants.OdbcProvider, "Fake.Provider" },
                data);
        }
    }
}
#endif
