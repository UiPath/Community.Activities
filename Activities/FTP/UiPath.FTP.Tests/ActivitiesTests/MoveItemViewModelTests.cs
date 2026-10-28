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
    /// Coverage for <see cref="MoveItemViewModel.InitializeModel"/>: property configuration only, no
    /// rules, no dependencies, no data sources.
    /// </summary>
    public class MoveItemViewModelTests
    {
        private static MoveItemViewModel NewViewModel() =>
            new MoveItemViewModel(Mock.Of<IDesignServices>());

        [Fact]
        public async Task InitializeAsync_ConfiguresRemotePathBeforeNewPath()
        {
            var vm = NewViewModel();

            await vm.InitializeAsync();

            Assert.True(vm.RemotePath.IsRequired);
            Assert.True(vm.RemotePath.IsPrincipal);
            Assert.Equal(Resources.Input, vm.RemotePath.Category);
            Assert.Equal(1, vm.RemotePath.OrderIndex);

            Assert.True(vm.NewPath.IsRequired);
            Assert.True(vm.NewPath.IsPrincipal);
            Assert.Equal(Resources.Input, vm.NewPath.Category);
            Assert.Equal(2, vm.NewPath.OrderIndex);
        }

        [Fact]
        public async Task InitializeAsync_ConfiguresOverwriteAsToggle()
        {
            var vm = NewViewModel();

            await vm.InitializeAsync();

            Assert.False(vm.Overwrite.IsPrincipal);
            Assert.Equal(Resources.Options, vm.Overwrite.Category);
            Assert.Equal(ViewModelWidgetType.Toggle, vm.Overwrite.Widget.Type);
            Assert.Equal(3, vm.Overwrite.OrderIndex);
        }

        [Fact]
        public async Task InitializeAsync_ConfiguresContinueOnErrorLast()
        {
            var vm = NewViewModel();

            await vm.InitializeAsync();

            Assert.False(vm.ContinueOnError.IsPrincipal);
            Assert.Equal(Resources.Options, vm.ContinueOnError.Category);
            Assert.Equal(4, vm.ContinueOnError.OrderIndex);
        }
    }
}
