using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Activities.ViewModels.Interfaces;
using System.Threading.Tasks;
using Moq;
using UiPath.FTP.Activities.NetCore.ViewModels;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Widgets;
using Xunit;

namespace UiPath.FTP.Tests
{
    /// <summary>
    /// STUD-80744: local-side path inputs must show the LocalResource picker (with NoWrap, since the
    /// arguments are plain strings) when the host supports it, and keep the host's default widget otherwise.
    /// Remote paths are FTP server paths and never get the picker.
    /// </summary>
    public class WidgetAdoptionViewModelTests
    {
        private static IDesignServices UnsupportedServices() => Mock.Of<IDesignServices>();

        private static IDesignServices SupportedServices()
        {
            var widgetSupportInfoService = new Mock<IWidgetSupportInfoService>();
            widgetSupportInfoService
                .Setup(s => s.IsWidgetSupported(ViewModelWidgetType.LocalResource))
                .Returns(true);

            var workflowDesignApi = new Mock<IWorkflowDesignApi>();
            workflowDesignApi.Setup(s => s.HasFeature(DesignFeatureKeys.WidgetSupportInfoService)).Returns(true);
            workflowDesignApi.Setup(s => s.WidgetSupportInfoService).Returns(widgetSupportInfoService.Object);

            var designServices = new Mock<IDesignServices>();
            designServices.Setup(s => s.GetService<IWorkflowDesignApi>()).Returns(workflowDesignApi.Object);
            return designServices.Object;
        }

        private static void AssertLocalResource(IWidget widget)
        {
            Assert.Equal(ViewModelWidgetType.LocalResource, widget.Type);
            Assert.Equal("true", widget.Metadata["NoWrap"]);
        }

        // Fallback: no widget is set, so the host's default widget for the argument type is used.
        private static void AssertDefaultWidget(IWidget widget) => Assert.Null(widget);

        [Fact]
        public async Task DownloadFiles_WidgetSupported_LocalPathUsesLocalResource()
        {
            var vm = new DownloadFilesViewModel(SupportedServices());
            await vm.InitializeAsync();

            AssertLocalResource(vm.LocalPath.Widget);
            AssertDefaultWidget(vm.RemotePath.Widget);
        }

        [Fact]
        public async Task DownloadFiles_WidgetUnsupported_LocalPathFallsBackToDefaultWidget()
        {
            var vm = new DownloadFilesViewModel(UnsupportedServices());
            await vm.InitializeAsync();

            AssertDefaultWidget(vm.LocalPath.Widget);
        }

        [Fact]
        public async Task UploadFiles_WidgetSupported_LocalPathUsesLocalResource()
        {
            var vm = new UploadFilesViewModel(SupportedServices());
            await vm.InitializeAsync();

            AssertLocalResource(vm.LocalPath.Widget);
            AssertDefaultWidget(vm.RemotePath.Widget);
        }

        [Fact]
        public async Task UploadFiles_WidgetUnsupported_LocalPathFallsBackToDefaultWidget()
        {
            var vm = new UploadFilesViewModel(UnsupportedServices());
            await vm.InitializeAsync();

            AssertDefaultWidget(vm.LocalPath.Widget);
        }

        [Fact]
        public async Task WithFtpSession_WidgetSupported_ClientCertificatePathUsesLocalResource()
        {
            var vm = new WithFtpSessionViewModel(SupportedServices());
            await vm.InitializeAsync();

            AssertLocalResource(vm.ClientCertificatePath.Widget);
        }

        [Fact]
        public async Task WithFtpSession_WidgetUnsupported_ClientCertificatePathFallsBackToDefaultWidget()
        {
            var vm = new WithFtpSessionViewModel(UnsupportedServices());
            await vm.InitializeAsync();

            AssertDefaultWidget(vm.ClientCertificatePath.Widget);
        }
    }
}
