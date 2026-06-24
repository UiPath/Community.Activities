using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Diagnostics.CodeAnalysis;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Cryptography.Enums;
using UiPath.Platform.ResourceHandling;

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class DecryptFileViewModel : DecryptCryptoViewModelBase
    {
        private readonly PairedInputToggle<string, IResource> _inputFileToggle;

        public DecryptFileViewModel(IDesignServices services) : base(services)
        {
            _inputFileToggle = new PairedInputToggle<string, IResource>(
                InputFilePath, InputFile,
                Resources.MenuAction_UseFilePath,
                Resources.MenuAction_UseFile)
            {
                AfterSwitch = ApplyInputFileVisibility,
            };
        }

        public DesignInArgument<IResource> InputFile { get; set; } = new DesignInArgument<IResource>();
        public DesignInArgument<string> InputFilePath { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<string> OutputFilePath { get; set; } = new DesignInArgument<string>();
        public DesignProperty<bool> Overwrite { get; set; } = new DesignProperty<bool>();
        public DesignOutArgument<ILocalResource> DecryptedFile { get; set; } = new DesignOutArgument<ILocalResource>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var orderIndex = 1;

            InputFile.IsPrincipal = true;
            InputFile.OrderIndex = orderIndex;
            InputFile.Category = Resources.Input;

            InputFilePath.IsPrincipal = true;
            InputFilePath.OrderIndex = orderIndex;
            InputFilePath.Category = Resources.Input;
            orderIndex++;

            ConfigureAlgorithmAndKeyProperties(ref orderIndex);
            ConfigureInteropProperties(ref orderIndex);

            OutputFilePath.IsPrincipal = false;
            OutputFilePath.IsVisible = true;
            OutputFilePath.IsRequired = false;
            OutputFilePath.OrderIndex = orderIndex++;
            OutputFilePath.Category = Resources.Input;

            Overwrite.IsPrincipal = false;
            Overwrite.OrderIndex = orderIndex++;
            Overwrite.Category = Resources.Category_Options_Name;
            Overwrite.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            ConfigureTailProperties(ref orderIndex);
            ConfigureKeyInputModeMenuActions();
            ConfigurePublicKeyFileMenuActions();
            ConfigureInputFileMenuActions();
            ConfigurePassphraseInputModeMenuActions();

            DecryptedFile.IsPrincipal = false;
            DecryptedFile.OrderIndex = orderIndex;
            DecryptedFile.Category = Resources.Output;
        }

        private void ConfigureInputFileMenuActions()
        {
            _inputFileToggle.ConfigureMenuActions();
            ApplyInputFileVisibility();
        }

        private void ApplyInputFileVisibility()
        {
            bool useResource = _inputFileToggle.UseSecondary;
            InputFile.IsVisible = useResource;
            InputFile.IsRequired = useResource;
            InputFilePath.IsVisible = !useResource;
            InputFilePath.IsRequired = !useResource;
        }
    }
}
