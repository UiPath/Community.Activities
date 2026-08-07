using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Threading.Tasks;
using Moq;
using UiPath.FTP.Activities.NetCore.ViewModels;
using UiPath.FTP.Activities.Properties;
using Xunit;

namespace UiPath.FTP.Tests
{
    /// <summary>
    /// Coverage for <see cref="EnumerateObjectsViewModel.InitializeModel"/>: property configuration
    /// and the Filter data source built in the constructor. No rules, no dependencies.
    /// </summary>
    public class EnumerateObjectsViewModelTests
    {
        private static EnumerateObjectsViewModel NewViewModel() =>
            new EnumerateObjectsViewModel(Mock.Of<IDesignServices>());

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
        public async Task InitializeAsync_ConfiguresRecursiveAsToggle()
        {
            var vm = NewViewModel();

            await vm.InitializeAsync();

            Assert.False(vm.Recursive.IsPrincipal);
            Assert.Equal(Resources.Options, vm.Recursive.Category);
            Assert.Equal(ViewModelWidgetType.Toggle, vm.Recursive.Widget.Type);
            Assert.Equal(2, vm.Recursive.OrderIndex);
        }

        [Fact]
        public async Task InitializeAsync_ConfiguresFilterWithDataSource()
        {
            var vm = NewViewModel();

            await vm.InitializeAsync();

            Assert.False(vm.Filter.IsPrincipal);
            Assert.Equal(Resources.Options, vm.Filter.Category);
            Assert.Equal(ViewModelWidgetType.MultiSelect, vm.Filter.Widget.Type);
            Assert.Equal(3, vm.Filter.OrderIndex);
            Assert.NotNull(vm.Filter.DataSource);
        }

        [Fact]
        public async Task InitializeAsync_ConfiguresContinueOnError()
        {
            var vm = NewViewModel();

            await vm.InitializeAsync();

            Assert.False(vm.ContinueOnError.IsPrincipal);
            Assert.Equal(Resources.Options, vm.ContinueOnError.Category);
            Assert.Equal(4, vm.ContinueOnError.OrderIndex);
        }

        [Fact]
        public async Task InitializeAsync_ConfiguresFilesAsTrailingOutput()
        {
            var vm = NewViewModel();

            await vm.InitializeAsync();

            Assert.False(vm.Files.IsPrincipal);
            Assert.Equal(Resources.Output, vm.Files.Category);
            Assert.Equal(5, vm.Files.OrderIndex);
        }
    }
}
