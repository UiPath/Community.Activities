using System.Activities.DesignViewModels;
using System.Threading.Tasks;
using Moq;
using UiPath.FTP.Activities.NetCore.ViewModels;
using UiPath.FTP.Activities.Properties;
using Xunit;

namespace UiPath.FTP.Tests
{
    /// <summary>
    /// Coverage for <see cref="FileExistsViewModel.InitializeModel"/>: property configuration only,
    /// no rules, no dependencies, no data sources.
    /// </summary>
    public class FileExistsViewModelTests
    {
        private static FileExistsViewModel NewViewModel() =>
            new FileExistsViewModel(Mock.Of<IDesignServices>());

        [Fact]
        public async Task InitializeAsync_ConfiguresRemotePath()
        {
            var vm = NewViewModel();

            await vm.InitializeAsync();

            Assert.True(vm.RemotePath.IsRequired);
            Assert.True(vm.RemotePath.IsPrincipal);
            Assert.Equal(Resources.Input, vm.RemotePath.Category);
            Assert.Equal(1, vm.RemotePath.OrderIndex);
        }

        [Fact]
        public async Task InitializeAsync_ConfiguresContinueOnError()
        {
            var vm = NewViewModel();

            await vm.InitializeAsync();

            Assert.False(vm.ContinueOnError.IsPrincipal);
            Assert.Equal(Resources.Options, vm.ContinueOnError.Category);
            Assert.Equal(2, vm.ContinueOnError.OrderIndex);
        }

        [Fact]
        public async Task InitializeAsync_ConfiguresExistsAsTrailingOutput()
        {
            var vm = NewViewModel();

            await vm.InitializeAsync();

            Assert.False(vm.Exists.IsPrincipal);
            Assert.Equal(Resources.Output, vm.Exists.Category);
            Assert.Equal(3, vm.Exists.OrderIndex);
        }
    }
}
