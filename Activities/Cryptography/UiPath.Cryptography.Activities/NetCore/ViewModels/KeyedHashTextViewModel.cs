using System;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using UiPath.Cryptography.Activities.NetCore.ViewModels;
using UiPath.Cryptography.Enums;

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
    public partial class KeyedHashTextViewModel : DesignPropertiesViewModel
    {
        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public KeyedHashTextViewModel(IDesignServices services) : base(services)
        {
        }

        /// <summary>
        /// A drop-down which enables you to select the keyed hashing algorithm you want to use.
        /// </summary>
        public DesignProperty<KeyedHashAlgorithms> Algorithm { get; set; } = new DesignProperty<KeyedHashAlgorithms>();

        /// <summary>
        /// The text that you want to hash.
        /// </summary>
        public DesignInArgument<string> Input { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The key that you want to use to hash the specified file.
        /// </summary>
        public DesignInArgument<string> Key { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The secure string used to hash the input string.
        /// </summary>
        public DesignInArgument<SecureString> KeySecureString { get; set; } = new DesignInArgument<SecureString>();

        /// <summary>
        /// Switches Key as string or secure string  
        /// </summary>
        public DesignProperty<KeyInputMode> KeyInputModeSwitch { get; set; } = new DesignProperty<KeyInputMode>();

        /// <summary>
        /// The hashed text, stored in a String variable.
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

            Algorithm.IsPrincipal = true;
            Algorithm.OrderIndex = propertyOrderIndex++;
            Algorithm.DataSource = DataSourceHelper.ForEnum(KeyedHashAlgorithms.HMACMD5, KeyedHashAlgorithms.HMACSHA1, KeyedHashAlgorithms.HMACSHA256, KeyedHashAlgorithms.HMACSHA384, KeyedHashAlgorithms.HMACSHA512, KeyedHashAlgorithms.SHA1, KeyedHashAlgorithms.SHA256, KeyedHashAlgorithms.SHA384, KeyedHashAlgorithms.SHA512);
            Algorithm.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown };

            Input.IsPrincipal = true;
            Input.IsRequired = true;
            Input.OrderIndex = propertyOrderIndex++;

            Key.IsPrincipal = true;
            Key.IsVisible = true;
            Key.OrderIndex = propertyOrderIndex++;

            KeySecureString.IsPrincipal = true;
            KeySecureString.IsVisible = false;
            KeySecureString.OrderIndex = propertyOrderIndex++;

            KeyInputModeSwitch.IsVisible = false;

            Result.IsPrincipal = false;
            Result.OrderIndex = propertyOrderIndex++;

            ContinueOnError.IsPrincipal = false;
            ContinueOnError.OrderIndex = propertyOrderIndex++;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.NullableBoolean, Metadata = new Dictionary<string, string>() };
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
            Rule(nameof(Algorithm), AlgorithmChanged_Action);
        }

        /// <inheritdoc/>
        protected override void ManualRegisterDependencies()
        {
            base.ManualRegisterDependencies();
            RegisterDependency(KeyInputModeSwitch, nameof(KeyInputModeSwitch.Value), nameof(KeyInputModeSwitch));
        }

        /// <summary>
        /// Key input Mode has changed. Set controls visibility based on selection
        /// </summary>
        private void KeyInputModeChanged_Action()
        {
            switch (KeyInputModeSwitch.Value)
            {
                case KeyInputMode.Key:
                    Key.IsRequired = true;
                    Key.IsVisible = true;
                    KeySecureString.IsVisible = false;
                    KeySecureString.IsRequired = false;
                    break;
                case KeyInputMode.SecureKey:
                    Key.IsRequired = false;
                    Key.IsVisible = false;
                    KeySecureString.IsVisible = true;
                    KeySecureString.IsRequired = true;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Algorithm has changed. Set controls visibility based on selection
        /// </summary>
        private void AlgorithmChanged_Action()
        {
            switch (Algorithm.Value.ToString().StartsWith(nameof(HMAC)))
            {
                case true:
                    if(KeyInputModeSwitch.Value == KeyInputMode.Key)
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
