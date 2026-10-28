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
    /// Coverage for <see cref="DeleteViewModel.InitializeModel"/>: the only design-time behavior this
    /// ViewModel has is the property configuration below (no rules, no dependencies, no data sources).
    /// </summary>
    public class DeleteViewModelTests
    {
        private static DeleteViewModel NewViewModel() =>
            new DeleteViewModel(Mock.Of<IDesignServices>());

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
            Assert.False(vm.ContinueOnError.IsRequired);
            Assert.Equal(Resources.Options, vm.ContinueOnError.Category);
            Assert.Equal(ViewModelWidgetType.NullableBoolean, vm.ContinueOnError.Widget.Type);
            Assert.Equal(2, vm.ContinueOnError.OrderIndex);
        }
    }
}
