using System;
using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Security.Cryptography;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Cryptography.Enums;
using UiPath.Platform.ResourceHandling;

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class KeyedHashFileViewModel : DesignPropertiesViewModel
    {
        private readonly DataSource<string> _encodingDataSource;
        private InArgument<IResource> _backupInputFile;
        private InArgument<string> _backupInputFilePath;

        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public KeyedHashFileViewModel(IDesignServices services) : base(services)
        {
            _encodingDataSource = EncodingHelpers.ConfigureEncodingDataSource();
        }

        /// <summary>
        /// The file that you want to hash.
        /// </summary>
        public DesignInArgument<IResource> InputFile { get; set; } = new DesignInArgument<IResource>();

        /// <summary>
        /// The path to the file you want to hash.
        /// </summary>
        public DesignInArgument<string> FilePath { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// Switches Key as string or secure string
        /// </summary>
        public DesignProperty<FileInputMode> FileInputModeSwitch { get; set; } = new DesignProperty<FileInputMode>();

        /// <summary>
        /// A drop-down which enables you to select the keyed hashing algorithm you want to use.
        /// </summary>
        public DesignProperty<KeyedHashAlgorithms> Algorithm { get; set; } = new DesignProperty<KeyedHashAlgorithms>();

        /// <summary>
        /// The key that you want to use to hash the specified file.
        /// </summary>
        public DesignInArgument<string> Key { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The secure string used to hash the provided file.
        /// </summary>
        public DesignInArgument<SecureString> KeySecureString { get; set; } = new DesignInArgument<SecureString>();

        /// <summary>
        /// A drop-down which enables you to select the encoding option you want to use.
        /// </summary>
        public DesignInArgument<string> KeyEncodingString { get; set; } = new() { Name = nameof(KeyEncodingString) };

        /// <summary>
        /// Switches Key as string or secure string
        /// </summary>
        public DesignProperty<KeyInputMode> KeyInputModeSwitch { get; set; } = new DesignProperty<KeyInputMode>();

        /// <summary>
        /// The hashed file, stored in a String variable.
        /// </summary>
        public DesignOutArgument<string> Result { get; set; } = new DesignOutArgument<string>();

        /// <summary>
        /// Specifies if the automation should continue even when the activity throws an error.
        /// </summary>
        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var propertyOrderIndex = 1;

            InputFile.IsPrincipal = true;
            InputFile.IsVisible = false;
            InputFile.OrderIndex = propertyOrderIndex++;

            FilePath.IsPrincipal = true;
            FilePath.IsVisible = true;
            FilePath.OrderIndex = propertyOrderIndex++;

            Algorithm.IsPrincipal = true;
            Algorithm.OrderIndex = propertyOrderIndex++;
            Algorithm.DataSource = DataSourceHelper.ForEnum(KeyedHashAlgorithms.HMACMD5, KeyedHashAlgorithms.HMACSHA1, KeyedHashAlgorithms.HMACSHA256, KeyedHashAlgorithms.HMACSHA384, KeyedHashAlgorithms.HMACSHA512, KeyedHashAlgorithms.SHA1, KeyedHashAlgorithms.SHA256, KeyedHashAlgorithms.SHA384, KeyedHashAlgorithms.SHA512);
            Algorithm.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown };


            FileInputModeSwitch.IsVisible = false;

            Key.IsPrincipal = true;
            Key.IsVisible = true;
            Key.OrderIndex = propertyOrderIndex++;

            KeySecureString.IsPrincipal = true;
            KeySecureString.IsVisible = false;
            KeySecureString.OrderIndex = propertyOrderIndex++;

            KeyInputModeSwitch.IsVisible = false;

            KeyEncodingString.IsPrincipal = false;
            KeyEncodingString.IsVisible = true;
            KeyEncodingString.OrderIndex = propertyOrderIndex++;

            KeyEncodingString.DataSource = _encodingDataSource;
            KeyEncodingString.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown, Metadata = new Dictionary<string, string>() };

            _encodingDataSource.Data = EncodingHelpers.GetAvailableEncodings();

            Result.IsPrincipal = false;
            Result.OrderIndex = propertyOrderIndex++;

            ContinueOnError.IsPrincipal = false;
            ContinueOnError.OrderIndex = propertyOrderIndex;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.NullableBoolean, Metadata = new Dictionary<string, string>() };
            ContinueOnError.Value = false;

            MenuActionsBuilder<KeyInputMode>.WithValueProperty(KeyInputModeSwitch)
                .AddMenuProperty(Key, KeyInputMode.Key)
                .AddMenuProperty(KeySecureString, KeyInputMode.SecureKey)
                .BuildAndInsertMenuActions();

            MenuActionsBuilder<FileInputMode>.WithValueProperty(FileInputModeSwitch)
                .AddMenuProperty(InputFile, FileInputMode.File)
                .AddMenuProperty(FilePath, FileInputMode.FilePath)
                .BuildAndInsertMenuActions();

            _backupInputFile = InputFile.Value;
            _backupInputFilePath = FilePath.Value;
        }
        /// <inheritdoc/>
        protected override void InitializeRules()
        {
            base.InitializeRules();
            Rule(nameof(KeyInputModeSwitch), KeyInputModeChanged_Action);
            Rule(nameof(FileInputModeSwitch), FileInputModeChanged_Action);
            Rule(nameof(Algorithm), AlgorithmChanged_Action);

        }

        /// <inheritdoc/>
        protected override void ManualRegisterDependencies()
        {
            base.ManualRegisterDependencies();
            RegisterDependency(KeyInputModeSwitch, nameof(KeyInputModeSwitch.Value), nameof(KeyInputModeSwitch));
            RegisterDependency(FileInputModeSwitch, nameof(FileInputModeSwitch.Value), nameof(FileInputModeSwitch));
        }

        /// <summary>
        /// Key input Mode has changed. Set controls visibility based on selection
        /// </summary>
        private void KeyInputModeChanged_Action()
        {
            ResetAllKeyInputMode();
            switch (KeyInputModeSwitch.Value)
            {
                case KeyInputMode.Key:
                    Key.IsRequired = true;
                    Key.IsVisible = true;
                    break;
                case KeyInputMode.SecureKey:
                    Key.IsRequired = false;
                    Key.IsVisible = false;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// File input Mode has changed. Set controls visibility based on selection
        /// </summary>
        private void FileInputModeChanged_Action()
        {
            ResetAllInputFile();
            switch (FileInputModeSwitch.Value)
            {
                case FileInputMode.File:
                    _backupInputFilePath = FilePath.Value;
                    FilePath.Value = null;

                    InputFile.IsRequired = true;
                    InputFile.IsVisible = true;
                    InputFile.Value = _backupInputFile;

                    break;

                case FileInputMode.FilePath:
                    _backupInputFile = InputFile.Value;
                    InputFile.Value = null;

                    FilePath.IsVisible = true;
                    FilePath.IsRequired = true;
                    FilePath.Value = _backupInputFilePath;

                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void ResetAllKeyInputMode()
        {
            Key.IsRequired = false;
            Key.IsVisible = false;
            KeySecureString.IsVisible = false;
            KeySecureString.IsRequired = false;
        }

        private void ResetAllInputFile()
        {
            InputFile.IsRequired = false;
            InputFile.IsVisible = false;
            FilePath.IsVisible = false;
            FilePath.IsRequired = false;
        }

        /// <summary>
        /// Algorithm has changed. Set controls visibility based on selection
        /// </summary>
        private void AlgorithmChanged_Action()
        {
            switch (Algorithm.Value.ToString().StartsWith(nameof(HMAC)))
            {
                case true:
                    if (KeyInputModeSwitch.Value == KeyInputMode.Key)
                    {
                        Key.IsVisible = true;
                        Key.IsRequired = true;
                        KeySecureString.IsVisible = false;
                    }
                    else
                    {
                        Key.IsVisible = false;
                        KeySecureString.IsRequired = true;
                        KeySecureString.IsVisible = true;
                    }
                    break;
                case false:
                    Key.IsRequired = false;
                    Key.IsVisible = false;
                    KeySecureString.IsVisible = false;
                    KeySecureString.IsRequired = false;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
