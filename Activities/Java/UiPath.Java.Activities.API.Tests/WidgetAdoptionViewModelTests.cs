using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Activities.ViewModels.Interfaces;
using Moq;
using UiPath.Activities.Java.ViewModels;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Widgets;
using Xunit;

namespace UiPath.Java.Activities.API.Tests
{
    /// <summary>
    /// STUD-80746: the jar and JDK path inputs must show the LocalResource picker (with NoWrap, since the
    /// arguments are plain strings) when the host supports it, and keep the host's default widget otherwise.
    /// </summary>
    public class WidgetAdoptionViewModelTests
    {
        private static IDesignServices UnsupportedServices() => Mock.Of<IDesignServices>();

        /// <summary>Host with the LocalResource widget; <paramref name="folderSelection"/> adds the FolderSelection feature.</summary>
        private static IDesignServices SupportedServices(bool folderSelection = false)
        {
            var widgetSupportInfoService = new Mock<IWidgetSupportInfoService>();
            widgetSupportInfoService
                .Setup(s => s.IsWidgetSupported(ViewModelWidgetType.LocalResource))
                .Returns(true);
            widgetSupportInfoService
                .Setup(s => s.IsWidgetFeatureSupported("LocalResource.FolderSelection"))
                .Returns(folderSelection);

            var workflowDesignApi = new Mock<IWorkflowDesignApi>();
            workflowDesignApi.Setup(s => s.HasFeature(DesignFeatureKeys.WidgetSupportInfoService)).Returns(true);
            workflowDesignApi.Setup(s => s.WidgetSupportInfoService).Returns(widgetSupportInfoService.Object);

            var designServices = new Mock<IDesignServices>();
            designServices.Setup(s => s.GetService<IWorkflowDesignApi>()).Returns(workflowDesignApi.Object);
            return designServices.Object;
        }

        private static void AssertFilePicker(IWidget widget)
        {
            Assert.Equal(ViewModelWidgetType.LocalResource, widget.Type);
            Assert.Equal("true", widget.Metadata["NoWrap"]);
            Assert.False(widget.Metadata.ContainsKey("SelectFolder"));
        }

        private static void AssertFolderPicker(IWidget widget)
        {
            Assert.Equal(ViewModelWidgetType.LocalResource, widget.Type);
            Assert.Equal("true", widget.Metadata["NoWrap"]);
            Assert.Equal("true", widget.Metadata["SelectFolder"]);
        }

        // Fallback: no widget is set, so the host's default widget for the argument type is used.
        private static void AssertDefaultWidget(IWidget widget) => Assert.Null(widget);

        [Fact]
        public async Task LoadJar_WidgetSupported_UsesFilePicker()
        {
            var vm = new LoadJarViewModel(SupportedServices());
            await vm.InitializeAsync();

            AssertFilePicker(vm.JarPath.Widget);
        }

        [Fact]
        public async Task LoadJar_WidgetUnsupported_FallsBackToDefaultWidget()
        {
            var vm = new LoadJarViewModel(UnsupportedServices());
            await vm.InitializeAsync();

            AssertDefaultWidget(vm.JarPath.Widget);
        }

        [Fact]
        public async Task JavaScope_FolderSelectionSupported_UsesFolderPicker()
        {
            var vm = new JavaScopeViewModel(SupportedServices(folderSelection: true));
            await vm.InitializeAsync();

            AssertFolderPicker(vm.JavaPath.Widget);
        }

        [Fact]
        public async Task JavaScope_FolderSelectionUnsupported_FallsBackToDefaultWidget()
        {
            // Hosts without folder selection would open a file dialog for a folder argument.
            var vm = new JavaScopeViewModel(SupportedServices(folderSelection: false));
            await vm.InitializeAsync();

            AssertDefaultWidget(vm.JavaPath.Widget);
        }

        [Fact]
        public async Task JavaScope_WidgetUnsupported_FallsBackToDefaultWidget()
        {
            var vm = new JavaScopeViewModel(UnsupportedServices());
            await vm.InitializeAsync();

            AssertDefaultWidget(vm.JavaPath.Widget);
        }

        [Fact]
        public void BuildLocalResourceWidget_NullServices_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => WidgetSupportHelper.BuildLocalResourceWidget(null));
        }
    }
}
