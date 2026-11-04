using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Activities.ViewModels.Interfaces;
using Moq;
using UiPath.Activities.Python.ViewModels;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Widgets;
using Xunit;

namespace UiPath.Python.Activities.Tests
{
    /// <summary>
    /// STUD-80745: script/environment path inputs must show the LocalResource picker (with NoWrap,
    /// since the arguments are plain strings) when the host supports it, and fall back to a text box otherwise.
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

        private static void AssertLocalResource(IWidget widget)
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

        private static void AssertText(IWidget widget)
        {
            Assert.Equal(ViewModelWidgetType.Text, widget.Type);
            Assert.False(widget.Metadata.ContainsKey("NoWrap"));
        }

        [Fact]
        public async Task RunScript_WidgetSupported_UsesLocalResource()
        {
            var vm = new RunScriptViewModel(SupportedServices());
            await vm.InitializeAsync();

            AssertLocalResource(vm.ScriptFile.Widget);
        }

        [Fact]
        public async Task RunScript_WidgetUnsupported_FallsBackToText()
        {
            var vm = new RunScriptViewModel(UnsupportedServices());
            await vm.InitializeAsync();

            AssertText(vm.ScriptFile.Widget);
        }

        [Fact]
        public async Task LoadScript_WidgetSupported_UsesLocalResource()
        {
            var vm = new LoadScriptViewModel(SupportedServices());
            await vm.InitializeAsync();

            AssertLocalResource(vm.ScriptFile.Widget);
        }

        [Fact]
        public async Task LoadScript_WidgetUnsupported_FallsBackToText()
        {
            var vm = new LoadScriptViewModel(UnsupportedServices());
            await vm.InitializeAsync();

            AssertText(vm.ScriptFile.Widget);
        }

        [Fact]
        public async Task PythonScope_FolderSelectionSupported_UsesFolderPickerForFolders()
        {
            var vm = new PythonScopeViewModel(SupportedServices(folderSelection: true));
            await vm.InitializeAsync();

            AssertLocalResource(vm.LibraryPath.Widget);
            AssertFolderPicker(vm.Path.Widget);
            AssertFolderPicker(vm.WorkingFolder.Widget);
        }

        [Fact]
        public async Task PythonScope_FolderSelectionNotSupported_FoldersFallBackToText()
        {
            // Older hosts have the file-only widget: a file picker on a folder argument would be wrong.
            var vm = new PythonScopeViewModel(SupportedServices(folderSelection: false));
            await vm.InitializeAsync();

            AssertLocalResource(vm.LibraryPath.Widget);
            AssertText(vm.Path.Widget);
            AssertText(vm.WorkingFolder.Widget);
        }

        [Fact]
        public void BuildLocalResourceWidget_FolderFeatureWithoutLocalResource_FallsBackToText()
        {
            var services = ServicesWith(hasWidgetSupportService: true, localResource: false, folderSelection: true);

            AssertText(WidgetSupportHelper.BuildLocalResourceWidget(services, selectFolder: true));
            AssertText(WidgetSupportHelper.BuildLocalResourceWidget(services));
        }

        [Fact]
        public void BuildLocalResourceWidget_HostWithoutWidgetSupportService_FallsBackToText()
        {
            var services = ServicesWith(hasWidgetSupportService: false, localResource: true, folderSelection: true);

            AssertText(WidgetSupportHelper.BuildLocalResourceWidget(services, selectFolder: true));
            AssertText(WidgetSupportHelper.BuildLocalResourceWidget(services));
        }

        [Fact]
        public void BuildLocalResourceWidget_FolderSelectionSupported_FileWidgetStaysInFileMode()
        {
            var services = ServicesWith(hasWidgetSupportService: true, localResource: true, folderSelection: true);

            AssertLocalResource(WidgetSupportHelper.BuildLocalResourceWidget(services));
            AssertFolderPicker(WidgetSupportHelper.BuildLocalResourceWidget(services, selectFolder: true));
        }

        [Fact]
        public void BuildLocalResourceWidget_NullServices_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => WidgetSupportHelper.BuildLocalResourceWidget(null));
        }

        private static IDesignServices ServicesWith(bool hasWidgetSupportService, bool localResource, bool folderSelection)
        {
            var widgetSupportInfoService = new Mock<IWidgetSupportInfoService>();
            widgetSupportInfoService.Setup(s => s.IsWidgetSupported(ViewModelWidgetType.LocalResource)).Returns(localResource);
            widgetSupportInfoService.Setup(s => s.IsWidgetFeatureSupported("LocalResource.FolderSelection")).Returns(folderSelection);

            var workflowDesignApi = new Mock<IWorkflowDesignApi>();
            workflowDesignApi.Setup(s => s.HasFeature(DesignFeatureKeys.WidgetSupportInfoService)).Returns(hasWidgetSupportService);
            workflowDesignApi.Setup(s => s.WidgetSupportInfoService).Returns(widgetSupportInfoService.Object);

            var designServices = new Mock<IDesignServices>();
            designServices.Setup(s => s.GetService<IWorkflowDesignApi>()).Returns(workflowDesignApi.Object);
            return designServices.Object;
        }

        [Fact]
        public async Task PythonScope_WidgetUnsupported_FallsBackToText()
        {
            var vm = new PythonScopeViewModel(UnsupportedServices());
            await vm.InitializeAsync();

            AssertText(vm.LibraryPath.Widget);
            AssertText(vm.Path.Widget);
            AssertText(vm.WorkingFolder.Widget);
        }
    }
}
