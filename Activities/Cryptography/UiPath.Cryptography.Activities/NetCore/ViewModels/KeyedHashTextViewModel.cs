using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Security.Cryptography;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Cryptography.Activities.NetCore.ViewModels;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Cryptography.Enums;

#pragma warning disable CS0618 // obsolete keyed-hash algorithms (HMACMD5, HMACSHA1, SHA1) remain referenced for backwards compatibility

namespace UiPath.Cryptography.Activities
{
    /// <summary>
    /// Hashes a string with a key using a specified algorithm and returns
    /// the hexadecimal string representation of the resulting hash.
    /// </summary>
    [ViewModelClass(typeof(KeyedHashTextViewModel))]
    public partial class KeyedHashText
    {
    }

    /// <summary>
    /// Hashes a string with a key using a specified algorithm and returns
    /// the hexadecimal string representation of the resulting hash.
    /// </summary>
    [ViewModelClass(typeof(KeyedHashTextViewModel))]
    public partial class HashText
    {
    }
}

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    [ExcludeFromCodeCoverage]
    public partial class KeyedHashTextViewModel : DesignPropertiesViewModel
    {
        private readonly DataSource<string> _encodingDataSource;
        private readonly PairedInputToggle<string, SecureString> _keyToggle;

        public KeyedHashTextViewModel(IDesignServices services) : base(services)
        {
            _encodingDataSource = EncodingHelpers.ConfigureEncodingDataSource();

            _keyToggle = new PairedInputToggle<string, SecureString>(
                Key, KeySecureString,
                Resources.MenuAction_UseKey,
                Resources.MenuAction_UseSecureKey)
            {
                AfterSwitch = ApplyKeyInputModeVisibility,
            };
        }

        public DesignProperty<KeyedHashAlgorithms> Algorithm { get; set; } = new DesignProperty<KeyedHashAlgorithms>();
        public DesignInArgument<string> Input { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<string> Key { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<string> KeyEncodingString { get; set; } = new() { Name = nameof(KeyEncodingString) };
        public DesignInArgument<SecureString> KeySecureString { get; set; } = new DesignInArgument<SecureString>();
        public DesignOutArgument<string> Result { get; set; } = new DesignOutArgument<string>();
        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var propertyOrderIndex = 1;

            Input.IsPrincipal = true;
            Input.IsRequired = true;
            Input.OrderIndex = propertyOrderIndex++;
            Input.Category = Resources.Input;

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
            KeyEncodingString.Category = Resources.Category_Encoding_Name;

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
            ConfigurePropertyTexts();
        }

        private void ConfigurePropertyTexts()
        {
            Input.DisplayName = Resources.Activity_KeyedHashText_Property_Input_Name;
            Input.Tooltip = Resources.Activity_KeyedHashText_Property_Input_Description;
            Algorithm.DisplayName = Resources.Activity_KeyedHashText_Property_Algorithm_Name;
            Algorithm.Tooltip = Resources.Activity_KeyedHashText_Property_Algorithm_Description;
            Key.DisplayName = Resources.Activity_KeyedHashText_Property_Key_Name;
            Key.Tooltip = Resources.Activity_KeyedHashText_Property_Key_Description;
            KeySecureString.DisplayName = Resources.Activity_KeyedHashText_Property_KeySecureString_Name;
            KeySecureString.Tooltip = Resources.Activity_KeyedHashText_Property_KeySecureString_Description;
            KeyEncodingString.DisplayName = Resources.Activity_KeyedHashText_Property_KeyEncodingString_Name;
            KeyEncodingString.Tooltip = Resources.Activity_KeyedHashText_Property_KeyEncodingString_Description;
            Result.DisplayName = Resources.Activity_KeyedHashText_Property_Result_Name;
            Result.Tooltip = Resources.Activity_KeyedHashText_Property_Result_Description;
            ContinueOnError.DisplayName = Resources.Activity_KeyedHashText_Property_ContinueOnError_Name;
            ContinueOnError.Tooltip = Resources.Activity_KeyedHashText_Property_ContinueOnError_Description;
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
