using System;
using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Diagnostics.CodeAnalysis;
using UiPath.Cryptography.Enums;
using UiPath.Platform.ResourceHandling;

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class DecryptFileViewModel : DecryptCryptoViewModelBase
    {
        private InArgument<IResource> _backupInputFile;
        private InArgument<string> _backupInputFilePath;

        public DecryptFileViewModel(IDesignServices services) : base(services)
        {
        }

        public DesignInArgument<IResource> InputFile { get; set; } = new DesignInArgument<IResource>();
        public DesignInArgument<string> InputFilePath { get; set; } = new DesignInArgument<string>();
        public DesignProperty<FileInputMode> FileInputModeSwitch { get; set; } = new DesignProperty<FileInputMode>();
        public DesignInArgument<string> OutputFilePath { get; set; } = new DesignInArgument<string>();
        public DesignProperty<bool> Overwrite { get; set; } = new DesignProperty<bool>();
        public DesignOutArgument<ILocalResource> DecryptedFile { get; set; } = new DesignOutArgument<ILocalResource>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var orderIndex = 1;

            InputFile.IsPrincipal = true;
            InputFile.IsVisible = false;
            InputFile.OrderIndex = orderIndex++;

            InputFilePath.IsPrincipal = true;
            InputFilePath.IsVisible = true;
            InputFilePath.OrderIndex = orderIndex++;

            FileInputModeSwitch.IsVisible = false;

            ConfigureAlgorithmAndKeyProperties(ref orderIndex);

            OutputFilePath.IsPrincipal = false;
            OutputFilePath.IsVisible = true;
            OutputFilePath.IsRequired = false;
            OutputFilePath.OrderIndex = orderIndex++;

            Overwrite.IsPrincipal = false;
            Overwrite.OrderIndex = orderIndex++;
            Overwrite.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            ConfigureTailProperties(ref orderIndex);
            ConfigureKeyInputModeMenuActions();

            MenuActionsBuilder<FileInputMode>.WithValueProperty(FileInputModeSwitch)
                .AddMenuProperty(InputFile, FileInputMode.File)
                .AddMenuProperty(InputFilePath, FileInputMode.FilePath)
                .BuildAndInsertMenuActions();

            DecryptedFile.OrderIndex = orderIndex++;

            _backupInputFile = InputFile.Value;
            _backupInputFilePath = InputFilePath.Value;
        }

        protected override void InitializeRules()
        {
            base.InitializeRules();
            Rule(nameof(FileInputModeSwitch), FileInputModeChanged_Action);
        }

        protected override void ManualRegisterDependencies()
        {
            base.ManualRegisterDependencies();
            RegisterDependency(FileInputModeSwitch, nameof(FileInputModeSwitch.Value), nameof(FileInputModeSwitch));
        }

        private void FileInputModeChanged_Action()
        {
            InputFile.IsRequired = false;
            InputFile.IsVisible = false;
            InputFilePath.IsRequired = false;
            InputFilePath.IsVisible = false;

            switch (FileInputModeSwitch.Value)
            {
                case FileInputMode.File:
                    _backupInputFilePath = InputFilePath.Value;
                    InputFilePath.Value = null;

                    InputFile.IsRequired = true;
                    InputFile.IsVisible = true;
                    InputFile.Value = _backupInputFile;

                    break;

                case FileInputMode.FilePath:
                    _backupInputFile = InputFile.Value;
                    InputFile.Value = null;

                    InputFilePath.IsVisible = true;
                    InputFilePath.IsRequired = true;
                    InputFilePath.Value = _backupInputFilePath;

                    break;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
