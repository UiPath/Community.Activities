using System;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Security;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Cryptography.Activities.NetCore.ViewModels;
using UiPath.Cryptography.Enums;

namespace UiPath.Cryptography.Activities
{
    /// <summary>
    /// Decrypts text based on a specified key encoding and algorithm.
    /// </summary>
    [ViewModelClass(typeof(DecryptTextViewModel))]
    public partial class DecryptText
    {
    }
}

#pragma warning disable CS0618 // obsolete encryption algorithm

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    public partial class DecryptTextViewModel : DesignPropertiesViewModel
    {
        private readonly DataSource<string> _encodingDataSource;

        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public DecryptTextViewModel(IDesignServices services) : base(services)
        {
            _encodingDataSource = EncodingHelpers.ConfigureEncodingDataSource();
        }

        /// <summary>
        /// A drop-down which enables you to select the decryption algorithm you want to use.
        /// </summary>
        public DesignProperty<EncryptionAlgorithm> Algorithm { get; set; } = new DesignProperty<EncryptionAlgorithm>();

        /// <summary>
        /// The text that you want to decrypt.
        /// </summary>
        public DesignInArgument<string> Input { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The key that you want to use to decrypt the specified file.
        /// </summary>
        public DesignInArgument<string> Key { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The secure string used to decrypt the input string.
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
        /// The decrypted text, stored in a String variable.
        /// </summary>
        public DesignOutArgument<string> Result { get; set; } = new DesignOutArgument<string>();

        /// <summary>
        /// Specifies if the automation should continue even if the activity throws an error.
        /// </summary>
        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        public DesignInArgument<string> PrivateKeyFilePath { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<System.Security.SecureString> Passphrase { get; set; } = new DesignInArgument<System.Security.SecureString>();
        public DesignProperty<bool> VerifySignature { get; set; } = new DesignProperty<bool>();
        public DesignInArgument<string> PublicKeyFilePath { get; set; } = new DesignInArgument<string>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var propertyOrderIndex = 1;

            Input.IsPrincipal = true;
            Input.OrderIndex = propertyOrderIndex++;

            Algorithm.IsPrincipal = true;
            Algorithm.OrderIndex = propertyOrderIndex++;
            Algorithm.DataSource = DataSourceHelper.ForEnum(EncryptionAlgorithm.AES, EncryptionAlgorithm.AESGCM, EncryptionAlgorithm.DES, EncryptionAlgorithm.RC2, EncryptionAlgorithm.Rijndael, EncryptionAlgorithm.TripleDES, EncryptionAlgorithm.PGP);
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

            Result.IsPrincipal = false;
            Result.OrderIndex = propertyOrderIndex++;

            ContinueOnError.IsPrincipal = false;
            ContinueOnError.OrderIndex = propertyOrderIndex++;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.NullableBoolean };
            ContinueOnError.Value = false;

            PrivateKeyFilePath.IsPrincipal = false;
            PrivateKeyFilePath.IsVisible = false;
            PrivateKeyFilePath.OrderIndex = propertyOrderIndex++;

            Passphrase.IsPrincipal = false;
            Passphrase.IsVisible = false;
            Passphrase.OrderIndex = propertyOrderIndex++;

            VerifySignature.IsPrincipal = false;
            VerifySignature.IsVisible = false;
            VerifySignature.OrderIndex = propertyOrderIndex++;
            VerifySignature.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            PublicKeyFilePath.IsPrincipal = false;
            PublicKeyFilePath.IsVisible = false;
            PublicKeyFilePath.OrderIndex = propertyOrderIndex;

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
            Rule(nameof(VerifySignature), VerifySignatureChanged_Action);
        }

        /// <inheritdoc/>
        protected override void ManualRegisterDependencies()
        {
            base.ManualRegisterDependencies();
            RegisterDependency(KeyInputModeSwitch, nameof(KeyInputModeSwitch.Value), nameof(KeyInputModeSwitch));
            RegisterDependency(Algorithm, nameof(Algorithm.Value), nameof(Algorithm));
            RegisterDependency(VerifySignature, nameof(VerifySignature.Value), nameof(VerifySignature));
        }

        /// <summary>
        /// Key input Mode has changed. Set controls visibility based on selection
        /// </summary>
        private void KeyInputModeChanged_Action()
        {
            if (Algorithm.Value == EncryptionAlgorithm.PGP) return;
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

        private void AlgorithmChanged_Action()
        {
            bool isPgp = Algorithm.Value == EncryptionAlgorithm.PGP;

            Key.IsVisible = !isPgp && KeyInputModeSwitch.Value == KeyInputMode.Key;
            KeySecureString.IsVisible = !isPgp && KeyInputModeSwitch.Value == KeyInputMode.SecureKey;
            KeyEncodingString.IsVisible = !isPgp;

            PrivateKeyFilePath.IsVisible = isPgp;
            PrivateKeyFilePath.IsRequired = isPgp;
            Passphrase.IsVisible = isPgp;
            Passphrase.IsRequired = isPgp;
            VerifySignature.IsVisible = isPgp;
            PublicKeyFilePath.IsVisible = isPgp && VerifySignature.Value;
            PublicKeyFilePath.IsRequired = isPgp && VerifySignature.Value;

            if (!isPgp)
            {
                KeyInputModeChanged_Action();
            }
        }

        private void VerifySignatureChanged_Action()
        {
            if (Algorithm.Value != EncryptionAlgorithm.PGP) return;
            PublicKeyFilePath.IsVisible = VerifySignature.Value;
            PublicKeyFilePath.IsRequired = VerifySignature.Value;
        }
    }
}
