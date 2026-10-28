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
    /// Coverage for <see cref="DownloadFilesViewModel.InitializeModel"/>: property configuration
    /// only, no rules, no dependencies, no data sources.
    /// </summary>
    public class DownloadFilesViewModelTests
    {
        private static DownloadFilesViewModel NewViewModel() =>
            new DownloadFilesViewModel(Mock.Of<IDesignServices>());

        [Fact]
        public async Task InitializeAsync_ConfiguresRemotePathBeforeLocalPath()
        {
            var vm = NewViewModel();

            await vm.InitializeAsync();

            Assert.True(vm.RemotePath.IsRequired);
            Assert.True(vm.RemotePath.IsPrincipal);
            Assert.Equal(Resources.Input, vm.RemotePath.Category);
            Assert.Equal(1, vm.RemotePath.OrderIndex);

            Assert.True(vm.LocalPath.IsRequired);
            Assert.True(vm.LocalPath.IsPrincipal);
            Assert.Equal(Resources.Input, vm.LocalPath.Category);
            Assert.Equal(2, vm.LocalPath.OrderIndex);
        }

        [Fact]
        public async Task InitializeAsync_ConfiguresCreateRecursiveOverwriteAsToggles()
        {
            var vm = NewViewModel();

            await vm.InitializeAsync();

            Assert.False(vm.Create.IsPrincipal);
            Assert.Equal(Resources.Options, vm.Create.Category);
            Assert.Equal(ViewModelWidgetType.Toggle, vm.Create.Widget.Type);
            Assert.Equal(3, vm.Create.OrderIndex);

            Assert.False(vm.Recursive.IsPrincipal);
            Assert.Equal(Resources.Options, vm.Recursive.Category);
            Assert.Equal(ViewModelWidgetType.Toggle, vm.Recursive.Widget.Type);
            Assert.Equal(4, vm.Recursive.OrderIndex);

            Assert.False(vm.Overwrite.IsPrincipal);
            Assert.Equal(Resources.Options, vm.Overwrite.Category);
            Assert.Equal(ViewModelWidgetType.Toggle, vm.Overwrite.Widget.Type);
            Assert.Equal(5, vm.Overwrite.OrderIndex);
        }

        [Fact]
        public async Task InitializeAsync_ConfiguresContinueOnErrorLast()
        {
            var vm = NewViewModel();

            await vm.InitializeAsync();

            Assert.False(vm.ContinueOnError.IsPrincipal);
            Assert.Equal(Resources.Options, vm.ContinueOnError.Category);
            Assert.Equal(6, vm.ContinueOnError.OrderIndex);
        }
    }
}
