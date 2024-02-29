﻿using System;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Security;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Cryptography.Activities.NetCore.ViewModels;
using UiPath.Cryptography.Enums;
using UiPath.Platform.ResourceHandling;

namespace UiPath.Cryptography.Activities
{
    /// <summary>
    /// Decrypts a file based on a specified key encoding and algorithm.
    /// </summary>
    [ViewModelClass(typeof(DecryptFileViewModel))]
    public partial class DecryptFile
    {
    }
}

#pragma warning disable CS0618 // obsolete encryption algorithm

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    public partial class DecryptFileViewModel : DesignPropertiesViewModel
    {
        private readonly DataSource<string> _encodingDataSource;

        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public DecryptFileViewModel(IDesignServices services) : base(services)
        {
            _encodingDataSource = EncodingHelpers.ConfigureEncodingDataSource();
        }

        /// <summary>
        /// A drop-down which enables you to select the decryption algorithm you want to use.
        /// </summary>
        public DesignProperty<SymmetricAlgorithms> Algorithm { get; set; } = new DesignProperty<SymmetricAlgorithms>();

        /// <summary>
        /// The key that you want to use to decrypt the specified file.
        /// </summary>
        public DesignInArgument<string> Key { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The secure string used to decrypt the input file.
        /// </summary>
        public DesignInArgument<SecureString> KeySecureString { get; set; } = new DesignInArgument<SecureString>();

        /// <summary>
        /// Switches Key as string or secure string 
        /// </summary>
        public DesignProperty<KeyInputMode> KeyInputModeSwitch { get; set; } = new DesignProperty<KeyInputMode>();

        /// <summary>
        /// A drop-down which enables you to select the encoding option you want to use.
        /// </summary>
        public DesignInArgument<string> KeyEncodingString { get; set; } = new() { Name = nameof(KeyEncodingString) };

        /// <summary>
        /// If a file already exists at the path specified in the Output path field, selecting this check box overwrites it. 
        /// </summary>
        public DesignProperty<bool> Overwrite { get; set; } = new DesignProperty<bool>();

        /// <summary>
        /// Specifies if the automation should continue even when the activity throws an error.
        /// </summary>
        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        /// <summary>
        /// The file that you want to decrypt.
        /// </summary>
        public DesignInArgument<IResource> InputFile { get; set; } = new DesignInArgument<IResource>();

        /// <summary>
        /// The path to the file that you want to decrypt.
        /// </summary>
        public DesignInArgument<string> InputFilePath { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// Switches File as IResource or string 
        /// </summary>
        public DesignProperty<FileInputMode> FileInputModeSwitch { get; set; } = new DesignProperty<FileInputMode>();

        /// <summary>
        /// The output path to the file that you want to decrypt.
        /// </summary>
        public DesignInArgument<string> OutputFilePath { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The file that you want to decrypt.
        /// </summary>
        public DesignOutArgument<ILocalResource> DecryptedFile { get; set; } = new DesignOutArgument<ILocalResource>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var propertyOrderIndex = 1;

            InputFile.IsPrincipal = true;
            InputFile.IsVisible = false;
            InputFile.OrderIndex = propertyOrderIndex++;

            InputFilePath.IsPrincipal = true;
            InputFilePath.IsVisible = true;
            InputFilePath.OrderIndex = propertyOrderIndex++;

            FileInputModeSwitch.IsVisible = false;

            Algorithm.IsPrincipal = true;
            Algorithm.OrderIndex = propertyOrderIndex++;
            Algorithm.DataSource = DataSourceHelper.ForEnum(SymmetricAlgorithms.AES, SymmetricAlgorithms.AESGCM, SymmetricAlgorithms.DES, SymmetricAlgorithms.RC2, SymmetricAlgorithms.Rijndael, SymmetricAlgorithms.TripleDES);
            Algorithm.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown };

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

            OutputFilePath.IsPrincipal = false;
            OutputFilePath.IsVisible = true;
            OutputFilePath.IsRequired = false;
            OutputFilePath.OrderIndex = propertyOrderIndex++;

            Overwrite.IsPrincipal = false;
            Overwrite.OrderIndex = propertyOrderIndex++;
            Overwrite.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            ContinueOnError.IsPrincipal = false;
            ContinueOnError.OrderIndex = propertyOrderIndex++;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.NullableBoolean };
            ContinueOnError.Value = false;

            MenuActionsBuilder<KeyInputMode>.WithValueProperty(KeyInputModeSwitch)
                .AddMenuProperty(Key, KeyInputMode.Key)
                .AddMenuProperty(KeySecureString, KeyInputMode.SecureKey)
                .BuildAndInsertMenuActions();

            MenuActionsBuilder<FileInputMode>.WithValueProperty(FileInputModeSwitch)
                .AddMenuProperty(InputFile, FileInputMode.File)
                .AddMenuProperty(InputFilePath, FileInputMode.FilePath)
                .BuildAndInsertMenuActions();

            DecryptedFile.IsPrincipal = false;
            DecryptedFile.OrderIndex = propertyOrderIndex;
        }

        /// <inheritdoc/>
        protected override void InitializeRules()
        {
            base.InitializeRules();
            Rule(nameof(KeyInputModeSwitch), KeyInputModeChanged_Action);
            Rule(nameof(FileInputModeSwitch), FileInputModeChanged_Action);
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
                    KeySecureString.IsVisible = true;
                    KeySecureString.IsRequired = true;
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
                    InputFile.IsRequired = true;
                    InputFile.IsVisible = true;
                    break;
                case FileInputMode.FilePath:
                    InputFilePath.IsVisible = true;
                    InputFilePath.IsRequired = true;
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
            InputFilePath.IsVisible = false;
            InputFilePath.IsRequired = false;
        }
    }
}
