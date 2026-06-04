using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Cryptography.Enums;
using UiPath.Platform.ResourceHandling;

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class DecryptFileViewModel : DecryptCryptoViewModelBase
    {
        private InArgument<IResource> _persistedInputFile;
        private InArgument<string> _persistedInputFilePath;

        public DecryptFileViewModel(IDesignServices services) : base(services)
        {
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
            var useFileMenuAction = new MenuAction
            {
                DisplayName = Resources.MenuAction_UseFile,
                IsMain = true,
                Handler = SwitchToInputFile,
            };
            var useFilePathMenuAction = new MenuAction
            {
                DisplayName = Resources.MenuAction_UseFilePath,
                IsMain = true,
                Handler = SwitchToInputFilePath,
            };
            InputFilePath.AddMenuAction(useFileMenuAction);
            InputFile.AddMenuAction(useFilePathMenuAction);

            bool useFile = InputFile.HasValue && !InputFilePath.HasValue;
            InputFile.IsVisible = useFile;
            InputFile.IsRequired = useFile;
            InputFilePath.IsVisible = !useFile;
            InputFilePath.IsRequired = !useFile;
        }

        private Task SwitchToInputFile(MenuAction _)
        {
            if (InputFilePath.Value != null) _persistedInputFilePath = InputFilePath.Value;
            InputFilePath.Value = null;
            InputFile.Value = _persistedInputFile;
            InputFile.IsVisible = true;
            InputFile.IsRequired = true;
            InputFilePath.IsVisible = false;
            InputFilePath.IsRequired = false;
            return Task.CompletedTask;
        }

        private Task SwitchToInputFilePath(MenuAction _)
        {
            if (InputFile.Value != null) _persistedInputFile = InputFile.Value;
            InputFile.Value = null;
            InputFilePath.Value = _persistedInputFilePath;
            InputFilePath.IsVisible = true;
            InputFilePath.IsRequired = true;
            InputFile.IsVisible = false;
            InputFile.IsRequired = false;
            return Task.CompletedTask;
        }
    }
}
