using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Threading.Tasks;
using Moq;
using UiPath.Cryptography.Activities.NetCore.ViewModels;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Widgets;
using Xunit;

namespace UiPath.Cryptography.Activities.Tests
{
    /// <summary>
    /// STUD-80743: file/key path inputs must show the LocalResource picker when the host
    /// advertises support for it, and fall back to a plain text box otherwise — driven by
    /// <see cref="UiPath.Cryptography.Activities.Helpers.WidgetSupportHelper.IsWidgetSupported"/>,
    /// a per-widget host capability check. STUD-64134's platform-dependent default-input
    /// selection was reverted per the ticket's own updated requirement (the default must be the
    /// same on Studio Desktop and Studio Web) — the default is now always the string/path
    /// property, unconditionally, matching pre-STUD-64134 behavior; widget capability has no
    /// bearing on which property is the default.
    /// </summary>
    public class WidgetAdoptionViewModelTests
    {
        private static IDesignServices UnsupportedServices() => Mock.Of<IDesignServices>();

        private static IDesignServices SupportedServices()
        {
            var widgetSupportInfoService = new Mock<IWidgetSupportInfoService>();
            widgetSupportInfoService
                .Setup(s => s.IsWidgetSupported(nameof(ViewModelWidgetType.LocalResource)))
                .Returns(true);

            var workflowDesignApi = new Mock<IWorkflowDesignApi>();
            workflowDesignApi.Setup(s => s.HasFeature(DesignFeatureKeys.WidgetSupportInfoService)).Returns(true);
            workflowDesignApi.Setup(s => s.WidgetSupportInfoService).Returns(widgetSupportInfoService.Object);

            var designServices = new Mock<IDesignServices>();
            designServices.Setup(s => s.GetService<IWorkflowDesignApi>()).Returns(workflowDesignApi.Object);
            return designServices.Object;
        }

        [Fact]
        public async Task DecryptFileViewModel_WidgetSupported_UsesLocalResource()
        {
            var vm = new DecryptFileViewModel(SupportedServices());
            await vm.InitializeAsync();

            Assert.Equal(ViewModelWidgetType.LocalResource, vm.InputFilePath.Widget.Type);
            Assert.Equal(ViewModelWidgetType.LocalResource, vm.InputFile.Widget.Type);
            Assert.Equal(ViewModelWidgetType.LocalResource, vm.OutputFilePath.Widget.Type);
            Assert.Equal(ViewModelWidgetType.LocalResource, vm.OutputFileName.Widget.Type);

            // Every string-typed property carries NoWrap (so the picker returns a raw path, not
            // a LocalResource.FromPath(...)-wrapped value) — regardless of whether it has an
            // IResource sibling (InputFilePath) or not (OutputFilePath, output-only).
            Assert.Equal("true", vm.InputFilePath.Widget.Metadata["NoWrap"]);
            Assert.Equal("true", vm.OutputFilePath.Widget.Metadata["NoWrap"]);

            // The IResource-typed sibling omits NoWrap, since it needs the FromPath(...) wrap.
            Assert.False(vm.InputFile.Widget.Metadata.ContainsKey("NoWrap"));

            // IsPrincipal must stay identical for the paired properties. The default (IsVisible/
            // IsRequired) is always InputFilePath, regardless of widget capability — STUD-64134's
            // platform-dependent default was reverted per the ticket's updated requirement.
            Assert.True(vm.InputFile.IsPrincipal);
            Assert.True(vm.InputFilePath.IsPrincipal);
            Assert.False(vm.InputFile.IsVisible);
            Assert.False(vm.InputFile.IsRequired);
            Assert.True(vm.InputFilePath.IsVisible);
            Assert.True(vm.InputFilePath.IsRequired);
        }

        [Fact]
        public async Task DecryptFileViewModel_WidgetUnsupported_FallsBackToText()
        {
            var vm = new DecryptFileViewModel(UnsupportedServices());
            await vm.InitializeAsync();

            Assert.Equal(ViewModelWidgetType.Text, vm.InputFilePath.Widget.Type);
            Assert.Equal(ViewModelWidgetType.Text, vm.InputFile.Widget.Type);
            Assert.Equal(ViewModelWidgetType.Text, vm.OutputFilePath.Widget.Type);
            Assert.Equal(ViewModelWidgetType.Text, vm.OutputFileName.Widget.Type);

            Assert.True(vm.InputFile.IsPrincipal);
            Assert.True(vm.InputFilePath.IsPrincipal);
            Assert.False(vm.InputFile.IsVisible);
            Assert.False(vm.InputFile.IsRequired);
            Assert.True(vm.InputFilePath.IsVisible);
            Assert.True(vm.InputFilePath.IsRequired);
        }

        [Fact]
        public async Task EncryptFileViewModel_WidgetSupported_UsesLocalResource()
        {
            var vm = new EncryptFileViewModel(SupportedServices());
            await vm.InitializeAsync();

            Assert.Equal(ViewModelWidgetType.LocalResource, vm.InputFilePath.Widget.Type);
            Assert.Equal(ViewModelWidgetType.LocalResource, vm.InputFile.Widget.Type);
            Assert.Equal(ViewModelWidgetType.LocalResource, vm.OutputFilePath.Widget.Type);
            Assert.Equal(ViewModelWidgetType.LocalResource, vm.OutputFileName.Widget.Type);

            // Every string-typed property carries NoWrap, regardless of whether it has an
            // IResource sibling (InputFilePath) or not (OutputFilePath/OutputFileName).
            Assert.Equal("true", vm.InputFilePath.Widget.Metadata["NoWrap"]);
            Assert.Equal("true", vm.OutputFilePath.Widget.Metadata["NoWrap"]);
            Assert.Equal("true", vm.OutputFileName.Widget.Metadata["NoWrap"]);
            Assert.False(vm.InputFile.Widget.Metadata.ContainsKey("NoWrap"));

            // IsPrincipal must stay identical for the paired properties. The default (IsVisible/
            // IsRequired) is always InputFilePath, regardless of widget capability — STUD-64134's
            // platform-dependent default was reverted per the ticket's updated requirement.
            Assert.True(vm.InputFile.IsPrincipal);
            Assert.True(vm.InputFilePath.IsPrincipal);
            Assert.False(vm.InputFile.IsVisible);
            Assert.False(vm.InputFile.IsRequired);
            Assert.True(vm.InputFilePath.IsVisible);
            Assert.True(vm.InputFilePath.IsRequired);
        }

        [Fact]
        public async Task EncryptFileViewModel_WidgetUnsupported_FallsBackToText()
        {
            var vm = new EncryptFileViewModel(UnsupportedServices());
            await vm.InitializeAsync();

            Assert.Equal(ViewModelWidgetType.Text, vm.InputFilePath.Widget.Type);
            Assert.Equal(ViewModelWidgetType.Text, vm.InputFile.Widget.Type);
            Assert.Equal(ViewModelWidgetType.Text, vm.OutputFilePath.Widget.Type);
            Assert.Equal(ViewModelWidgetType.Text, vm.OutputFileName.Widget.Type);

            Assert.True(vm.InputFile.IsPrincipal);
            Assert.True(vm.InputFilePath.IsPrincipal);
            Assert.False(vm.InputFile.IsVisible);
            Assert.False(vm.InputFile.IsRequired);
            Assert.True(vm.InputFilePath.IsVisible);
            Assert.True(vm.InputFilePath.IsRequired);
        }

        [Fact]
        public async Task KeyedHashFileViewModel_WidgetSupported_UsesLocalResource()
        {
            var vm = new KeyedHashFileViewModel(SupportedServices());
            await vm.InitializeAsync();

            Assert.Equal(ViewModelWidgetType.LocalResource, vm.FilePath.Widget.Type);
            Assert.Equal(ViewModelWidgetType.LocalResource, vm.InputFile.Widget.Type);

            // IsPrincipal must stay identical for the paired properties. The default (IsVisible/
            // IsRequired) is always FilePath, regardless of widget capability — STUD-64134's
            // platform-dependent default was reverted per the ticket's updated requirement.
            Assert.True(vm.InputFile.IsPrincipal);
            Assert.True(vm.FilePath.IsPrincipal);
            Assert.False(vm.InputFile.IsVisible);
            Assert.False(vm.InputFile.IsRequired);
            Assert.True(vm.FilePath.IsVisible);
            Assert.True(vm.FilePath.IsRequired);
        }

        [Fact]
        public async Task KeyedHashFileViewModel_WidgetUnsupported_FallsBackToText()
        {
            var vm = new KeyedHashFileViewModel(UnsupportedServices());
            await vm.InitializeAsync();

            Assert.Equal(ViewModelWidgetType.Text, vm.FilePath.Widget.Type);
            Assert.Equal(ViewModelWidgetType.Text, vm.InputFile.Widget.Type);

            Assert.True(vm.InputFile.IsPrincipal);
            Assert.True(vm.FilePath.IsPrincipal);
            Assert.False(vm.InputFile.IsVisible);
            Assert.False(vm.InputFile.IsRequired);
            Assert.True(vm.FilePath.IsVisible);
            Assert.True(vm.FilePath.IsRequired);
        }

        [Fact]
        public async Task PgpVerifyViewModel_WidgetSupported_UsesLocalResource()
        {
            var vm = new PgpVerifyViewModel(SupportedServices());
            await vm.InitializeAsync();

            Assert.Equal(ViewModelWidgetType.LocalResource, vm.InputFilePath.Widget.Type);
            Assert.Equal(ViewModelWidgetType.LocalResource, vm.PublicKeyFilePath.Widget.Type);
        }

        [Fact]
        public async Task PgpVerifyViewModel_WidgetUnsupported_FallsBackToText()
        {
            var vm = new PgpVerifyViewModel(UnsupportedServices());
            await vm.InitializeAsync();

            Assert.Equal(ViewModelWidgetType.Text, vm.InputFilePath.Widget.Type);
            Assert.Equal(ViewModelWidgetType.Text, vm.PublicKeyFilePath.Widget.Type);
        }

        [Fact]
        public async Task PgpSignFileViewModel_WidgetSupported_UsesLocalResource()
        {
            var vm = new PgpSignFileViewModel(SupportedServices());
            await vm.InitializeAsync();

            Assert.Equal(ViewModelWidgetType.LocalResource, vm.InputFilePath.Widget.Type);
            Assert.Equal(ViewModelWidgetType.LocalResource, vm.PrivateKeyFilePath.Widget.Type);
            Assert.Equal(ViewModelWidgetType.LocalResource, vm.OutputFilePath.Widget.Type);

            // Every string-typed property carries NoWrap — including OutputFilePath, which has
            // no IResource sibling at all.
            Assert.Equal("true", vm.InputFilePath.Widget.Metadata["NoWrap"]);
            Assert.Equal("true", vm.PrivateKeyFilePath.Widget.Metadata["NoWrap"]);
            Assert.Equal("true", vm.OutputFilePath.Widget.Metadata["NoWrap"]);
            Assert.False(vm.InputFile.Widget.Metadata.ContainsKey("NoWrap"));
        }

        [Fact]
        public async Task PgpClearSignFileViewModel_WidgetSupported_UsesLocalResource()
        {
            var vm = new PgpClearSignFileViewModel(SupportedServices());
            await vm.InitializeAsync();

            Assert.Equal(ViewModelWidgetType.LocalResource, vm.InputFilePath.Widget.Type);
            Assert.Equal(ViewModelWidgetType.LocalResource, vm.PrivateKeyFilePath.Widget.Type);
            Assert.Equal(ViewModelWidgetType.LocalResource, vm.OutputFilePath.Widget.Type);

            // Pins the NoWrap fix directly for PgpClearSignFile too, rather than relying on it
            // sharing PgpSignViewModelBase.ConfigureFilePathWidgets() with PgpSignFileViewModel.
            Assert.Equal("true", vm.InputFilePath.Widget.Metadata["NoWrap"]);
            Assert.Equal("true", vm.PrivateKeyFilePath.Widget.Metadata["NoWrap"]);
            Assert.Equal("true", vm.OutputFilePath.Widget.Metadata["NoWrap"]);
            Assert.False(vm.InputFile.Widget.Metadata.ContainsKey("NoWrap"));
        }

        [Fact]
        public async Task PgpClearSignFileViewModel_WidgetUnsupported_FallsBackToText()
        {
            var vm = new PgpClearSignFileViewModel(UnsupportedServices());
            await vm.InitializeAsync();

            Assert.Equal(ViewModelWidgetType.Text, vm.InputFilePath.Widget.Type);
            Assert.Equal(ViewModelWidgetType.Text, vm.PrivateKeyFilePath.Widget.Type);
            Assert.Equal(ViewModelWidgetType.Text, vm.OutputFilePath.Widget.Type);
        }

        [Fact]
        public async Task PgpGenerateKeysViewModel_WidgetSupported_UsesLocalResourceOnOutputPaths()
        {
            var vm = new PgpGenerateKeysViewModel(SupportedServices());
            await vm.InitializeAsync();

            Assert.Equal(ViewModelWidgetType.LocalResource, vm.PublicKeyFilePath.Widget.Type);
            Assert.Equal(ViewModelWidgetType.LocalResource, vm.PrivateKeyFilePath.Widget.Type);

            // Both are string-typed with no IResource sibling at all in this ViewModel — NoWrap
            // must still apply, or the picker would wrap the selection as a LocalResource that
            // can't be assigned to a string argument.
            Assert.Equal("true", vm.PublicKeyFilePath.Widget.Metadata["NoWrap"]);
            Assert.Equal("true", vm.PrivateKeyFilePath.Widget.Metadata["NoWrap"]);
        }

        [Fact]
        public async Task PgpGenerateKeysViewModel_WidgetUnsupported_FallsBackToText()
        {
            var vm = new PgpGenerateKeysViewModel(UnsupportedServices());
            await vm.InitializeAsync();

            Assert.Equal(ViewModelWidgetType.Text, vm.PublicKeyFilePath.Widget.Type);
            Assert.Equal(ViewModelWidgetType.Text, vm.PrivateKeyFilePath.Widget.Type);
        }
    }
}
