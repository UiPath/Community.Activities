using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Security.Cryptography;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Cryptography.Enums;
using UiPath.Platform.ResourceHandling;

#pragma warning disable CS0618 // obsolete keyed-hash algorithms (HMACMD5, HMACSHA1, SHA1) remain referenced for backwards compatibility

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class KeyedHashFileViewModel : DesignPropertiesViewModel
    {
        private readonly DataSource<string> _encodingDataSource;
        private readonly PairedInputToggle<string, SecureString> _keyToggle;
        private readonly PairedInputToggle<string, IResource> _inputFileToggle;

        public KeyedHashFileViewModel(IDesignServices services) : base(services)
        {
            _encodingDataSource = EncodingHelpers.ConfigureEncodingDataSource();

            _keyToggle = new PairedInputToggle<string, SecureString>(
                Key, KeySecureString,
                Resources.MenuAction_UseKey,
                Resources.MenuAction_UseSecureKey)
            {
                AfterSwitch = ApplyKeyInputModeVisibility,
            };

            _inputFileToggle = new PairedInputToggle<string, IResource>(
                FilePath, InputFile,
                Resources.MenuAction_UseFilePath,
                Resources.MenuAction_UseFile)
            {
                AfterSwitch = ApplyInputFileVisibility,
            };
        }

        public DesignInArgument<IResource> InputFile { get; set; } = new DesignInArgument<IResource>();
        public DesignInArgument<string> FilePath { get; set; } = new DesignInArgument<string>();
        public DesignProperty<KeyedHashAlgorithms> Algorithm { get; set; } = new DesignProperty<KeyedHashAlgorithms>();
        public DesignInArgument<string> Key { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<SecureString> KeySecureString { get; set; } = new DesignInArgument<SecureString>();
        public DesignInArgument<string> KeyEncodingString { get; set; } = new() { Name = nameof(KeyEncodingString) };
        public DesignOutArgument<string> Result { get; set; } = new DesignOutArgument<string>();
        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var propertyOrderIndex = 1;

            InputFile.IsPrincipal = true;
            InputFile.OrderIndex = propertyOrderIndex;
            InputFile.Category = Resources.Input;

            FilePath.IsPrincipal = true;
            FilePath.OrderIndex = propertyOrderIndex;
            FilePath.Category = Resources.Input;
            propertyOrderIndex++;

            Algorithm.IsPrincipal = true;
            Algorithm.IsRequired = true;
            Algorithm.OrderIndex = propertyOrderIndex++;
            Algorithm.Category = Resources.Input;
            Algorithm.DataSource = DataSourceHelper.ForEnum(
                // Usable (alphabetical):
                KeyedHashAlgorithms.HMACSHA256,
                KeyedHashAlgorithms.HMACSHA384,
                KeyedHashAlgorithms.HMACSHA512,
                KeyedHashAlgorithms.SHA256,
                KeyedHashAlgorithms.SHA384,
                KeyedHashAlgorithms.SHA512,
                // Deprecated (alphabetical):
                KeyedHashAlgorithms.HMACMD5,
                KeyedHashAlgorithms.HMACSHA1,
                KeyedHashAlgorithms.SHA1);
            Algorithm.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown };

            Key.IsPrincipal = true;
            Key.OrderIndex = propertyOrderIndex;
            Key.Category = Resources.Input;

            KeySecureString.IsPrincipal = true;
            KeySecureString.OrderIndex = propertyOrderIndex;
            KeySecureString.Category = Resources.Input;
            propertyOrderIndex++;

            KeyEncodingString.IsPrincipal = false;
            KeyEncodingString.OrderIndex = propertyOrderIndex++;
            KeyEncodingString.Category = Resources.Category_Options_Name;

            KeyEncodingString.DataSource = _encodingDataSource;
            KeyEncodingString.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown, Metadata = new Dictionary<string, string>() };

            _encodingDataSource.Data = EncodingHelpers.GetAvailableEncodings();

            ContinueOnError.IsPrincipal = false;
            ContinueOnError.OrderIndex = propertyOrderIndex++;
            ContinueOnError.Category = Resources.Category_Options_Name;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle, Metadata = new Dictionary<string, string>() };
            ContinueOnError.Value = false;

            // Output is assigned last so it renders after the Options section (guideline "outputs last").
            // Non-principal, matching every other Cryptography output — it belongs in the Output section
            // of the properties panel, not the collapsed canvas card among the inputs.
            Result.IsPrincipal = false;
            Result.OrderIndex = propertyOrderIndex++;
            Result.Category = Resources.Output;

            _keyToggle.ConfigureMenuActions();
            ApplyKeyInputModeVisibility();

            _inputFileToggle.ConfigureMenuActions();
            ApplyInputFileVisibility();
            ConfigurePropertyTexts();
        }

        private void ConfigurePropertyTexts()
        {
            InputFile.DisplayName = Resources.Activity_KeyedHashFile_Property_InputFile_Name;
            InputFile.Tooltip = Resources.Activity_KeyedHashFile_Property_InputFile_Description;
            FilePath.DisplayName = Resources.Activity_KeyedHashFile_Property_FilePath_Name;
            FilePath.Tooltip = Resources.Activity_KeyedHashFile_Property_FilePath_Description;
            Algorithm.DisplayName = Resources.Activity_KeyedHashFile_Property_Algorithm_Name;
            Algorithm.Tooltip = Resources.Activity_KeyedHashFile_Property_Algorithm_Description;
            Key.DisplayName = Resources.Activity_KeyedHashFile_Property_Key_Name;
            Key.Tooltip = Resources.Activity_KeyedHashFile_Property_Key_Description;
            KeySecureString.DisplayName = Resources.Activity_KeyedHashFile_Property_KeySecureString_Name;
            KeySecureString.Tooltip = Resources.Activity_KeyedHashFile_Property_KeySecureString_Description;
            KeyEncodingString.DisplayName = Resources.Activity_KeyedHashFile_Property_KeyEncodingString_Name;
            KeyEncodingString.Tooltip = Resources.Activity_KeyedHashFile_Property_KeyEncodingString_Description;
            Result.DisplayName = Resources.Activity_KeyedHashFile_Property_Result_Name;
            Result.Tooltip = Resources.Activity_KeyedHashFile_Property_Result_Description;
            ContinueOnError.DisplayName = Resources.Activity_KeyedHashFile_Property_ContinueOnError_Name;
            ContinueOnError.Tooltip = Resources.Activity_KeyedHashFile_Property_ContinueOnError_Description;
        }

        private void ApplyKeyInputModeVisibility()
        {
            bool isHmac = Algorithm.Value.ToString().StartsWith(nameof(HMAC));
            bool useSecure = _keyToggle.UseSecondary;
            Key.IsVisible = isHmac && !useSecure;
            Key.IsRequired = isHmac && !useSecure;
            KeySecureString.IsVisible = isHmac && useSecure;
            KeySecureString.IsRequired = isHmac && useSecure;
        }

        private void ApplyInputFileVisibility()
        {
            bool useResource = _inputFileToggle.UseSecondary;
            InputFile.IsVisible = useResource;
            InputFile.IsRequired = useResource;
            FilePath.IsVisible = !useResource;
            FilePath.IsRequired = !useResource;
        }

        protected override void InitializeRules()
        {
            base.InitializeRules();
            Rule(nameof(Algorithm), AlgorithmChanged_Action);
        }

        private void AlgorithmChanged_Action()
        {
            ApplyKeyInputModeVisibility();
        }
    }
}
