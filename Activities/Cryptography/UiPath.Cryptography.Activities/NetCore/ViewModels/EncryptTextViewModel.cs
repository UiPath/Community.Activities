using System;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Reflection;
using System.Security;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Cryptography.Activities.NetCore.ViewModels;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Cryptography.Enums;

namespace UiPath.Cryptography.Activities
{
    /// <summary>
    /// Encrypts a string with a key based on a specified key encoding and algorithm.
    /// </summary>
    [ViewModelClass(typeof(EncryptTextViewModel))]
    public partial class EncryptText
    {
    }
}

#pragma warning disable CS0618 // obsolete encryption algorithm

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    public partial class EncryptTextViewModel : DesignPropertiesViewModel
    {
        private readonly DataSource<string> _encodingDataSource;

        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public EncryptTextViewModel(IDesignServices services) : base(services)
        {
            _encodingDataSource = EncodingHelpers.ConfigureEncodingDataSource();
        }

        /// <summary>
        /// A drop-down which enables you to select the encryption algorithm you want to use.
        /// </summary>
        public DesignProperty<SymmetricAlgorithms> Algorithm { get; set; } = new DesignProperty<SymmetricAlgorithms>();

        /// <summary>
        /// The text that you want to encrypt.
        /// </summary>
        public DesignInArgument<string> Input { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The key that you want to use to encrypt the specified file.
        /// </summary>
        public DesignInArgument<string> Key { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The secure string used to encrypt the input string.
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
        /// The encrypted text, stored in a String variable.
        /// </summary>
        public DesignOutArgument<string> Result { get; set; } = new DesignOutArgument<string>();

        /// <summary>
        /// Specifies if the automation should continue even when the activity throws an error.
        /// </summary>
        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        /// <summary>
        /// A warning about using a deprecated encryption algorithm, due to be removed in the future
        /// </summary>
        [NotMappedProperty]
        public DesignProperty<string> DeprecatedWarning { get; set; } = new DesignProperty<string>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var propertyOrderIndex = 1;

            Input.IsPrincipal = true;
            Input.OrderIndex = propertyOrderIndex++;

            Algorithm.IsPrincipal = true;
            Algorithm.OrderIndex = propertyOrderIndex++;
            Algorithm.DataSource = DataSourceHelper.ForEnum(SymmetricAlgorithms.AES, SymmetricAlgorithms.AESGCM, SymmetricAlgorithms.DES, SymmetricAlgorithms.RC2, SymmetricAlgorithms.Rijndael, SymmetricAlgorithms.TripleDES);
            Algorithm.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown };

            DeprecatedWarning.OrderIndex = propertyOrderIndex++;
            DeprecatedWarning.Widget = new TextBlockWidget
            {
                Level = TextBlockWidgetLevel.Warning,
                Multiline = true
            };
            DeprecatedWarning.Value = Resources.Activity_Encrypt_Algorithm_Deprecated_Warning;

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
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.NullableBoolean };
            ContinueOnError.Value = false;

            MenuActionsBuilder<KeyInputMode>.WithValueProperty(KeyInputModeSwitch)
                .AddMenuProperty(Key, KeyInputMode.Key)
                .AddMenuProperty(KeySecureString, KeyInputMode.SecureKey)
                .BuildAndInsertMenuActions();
        }

        /// <inheritdoc/>
        protected override void InitializeRules()
        {
            base.InitializeRules();
            Rule(nameof(KeyInputModeSwitch), KeyInputModeChanged_Action);
            Rule(nameof(Algorithm), DeprecatedAlgorithmWarning_Action);
        }

        /// <inheritdoc/>
        protected override void ManualRegisterDependencies()
        {
            base.ManualRegisterDependencies();
            RegisterDependency(KeyInputModeSwitch, nameof(KeyInputModeSwitch.Value), nameof(KeyInputModeSwitch));
            RegisterDependency(Algorithm, nameof(Algorithm.Value), nameof(Algorithm));
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

        private void ResetAllKeyInputMode()
        {
            Key.IsRequired = false;
            Key.IsVisible = false;
            KeySecureString.IsVisible = false;
            KeySecureString.IsRequired = false;
        }

        /// <summary>
        /// Checks if the selected encryption algorithm is obsolete 
        /// </summary>
        private void DeprecatedAlgorithmWarning_Action()
        {
            try
            {
                var enumName = typeof(SymmetricAlgorithms).GetEnumName(Algorithm.Value);
                var field = typeof(SymmetricAlgorithms).GetField(enumName);

                var obsoleteAttribute = field?.GetCustomAttribute<ObsoleteAttribute>();
                if (obsoleteAttribute != null)
                {
                    DeprecatedWarning.IsVisible = true;
                }
                else
                {
                    DeprecatedWarning.IsVisible = false;
                }
            }
            catch
            {
                DeprecatedWarning.IsVisible = false;
            }
        }
    }
}
