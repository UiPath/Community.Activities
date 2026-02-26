using System;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Cryptography.Enums;

#pragma warning disable CS0618 // obsolete encryption algorithm

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    /// <summary>
    /// Base ViewModel for decrypt activities. Contains shared Algorithm/Key/PGP
    /// properties, their design-time configuration, and rule handlers.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public abstract class DecryptCryptoViewModelBase : DesignPropertiesViewModel
    {
        private readonly DataSource<string> _encodingDataSource;

        protected DecryptCryptoViewModelBase(IDesignServices services) : base(services)
        {
            _encodingDataSource = EncodingHelpers.ConfigureEncodingDataSource();
        }

        public DesignProperty<EncryptionAlgorithm> Algorithm { get; set; } = new DesignProperty<EncryptionAlgorithm>();
        public DesignInArgument<string> Key { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<SecureString> KeySecureString { get; set; } = new DesignInArgument<SecureString>();
        public DesignProperty<KeyInputMode> KeyInputModeSwitch { get; set; } = new DesignProperty<KeyInputMode>();
        public DesignInArgument<string> KeyEncodingString { get; set; } = new() { Name = nameof(KeyEncodingString) };
        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        public DesignInArgument<string> PrivateKeyFilePath { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<SecureString> Passphrase { get; set; } = new DesignInArgument<SecureString>();
        public DesignProperty<bool> VerifySignature { get; set; } = new DesignProperty<bool>();
        public DesignInArgument<string> PublicKeyFilePath { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// Configures Algorithm dropdown, Key, KeySecureString,
        /// KeyInputModeSwitch, and KeyEncodingString properties.
        /// </summary>
        protected void ConfigureAlgorithmAndKeyProperties(ref int orderIndex)
        {
            Algorithm.IsPrincipal = true;
            Algorithm.OrderIndex = orderIndex++;
            Algorithm.DataSource = DataSourceHelper.ForEnum(
                EncryptionAlgorithm.AES, EncryptionAlgorithm.AESGCM,
                EncryptionAlgorithm.DES, EncryptionAlgorithm.RC2,
                EncryptionAlgorithm.Rijndael, EncryptionAlgorithm.TripleDES,
                EncryptionAlgorithm.PGP);
            Algorithm.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown };

            Key.IsPrincipal = true;
            Key.IsVisible = true;
            Key.OrderIndex = orderIndex++;

            KeySecureString.IsPrincipal = true;
            KeySecureString.IsVisible = false;
            KeySecureString.OrderIndex = orderIndex++;

            KeyInputModeSwitch.IsVisible = false;

            KeyEncodingString.IsPrincipal = false;
            KeyEncodingString.IsVisible = true;
            KeyEncodingString.OrderIndex = orderIndex++;

            KeyEncodingString.DataSource = _encodingDataSource;
            KeyEncodingString.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown, Metadata = new Dictionary<string, string>() };

            _encodingDataSource.Data = EncodingHelpers.GetAvailableEncodings();
        }

        /// <summary>
        /// Configures ContinueOnError and PGP decrypt properties
        /// (PrivateKeyFilePath, Passphrase, VerifySignature, PublicKeyFilePath — hidden by default).
        /// </summary>
        protected void ConfigureTailProperties(ref int orderIndex)
        {
            ContinueOnError.IsPrincipal = false;
            ContinueOnError.OrderIndex = orderIndex++;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.NullableBoolean };
            ContinueOnError.Value = false;

            PrivateKeyFilePath.IsPrincipal = false;
            PrivateKeyFilePath.IsVisible = false;
            PrivateKeyFilePath.OrderIndex = orderIndex++;

            Passphrase.IsPrincipal = false;
            Passphrase.IsVisible = false;
            Passphrase.OrderIndex = orderIndex++;

            VerifySignature.IsPrincipal = false;
            VerifySignature.IsVisible = false;
            VerifySignature.OrderIndex = orderIndex++;
            VerifySignature.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            PublicKeyFilePath.IsPrincipal = false;
            PublicKeyFilePath.IsVisible = false;
            PublicKeyFilePath.OrderIndex = orderIndex++;
        }

        /// <summary>
        /// Configures the Key/SecureKey toggle menu action.
        /// </summary>
        protected void ConfigureKeyInputModeMenuActions()
        {
            MenuActionsBuilder<KeyInputMode>.WithValueProperty(KeyInputModeSwitch)
                .AddMenuProperty(Key, KeyInputMode.Key)
                .AddMenuProperty(KeySecureString, KeyInputMode.SecureKey)
                .BuildAndInsertMenuActions();
        }

        protected override void InitializeRules()
        {
            base.InitializeRules();
            Rule(nameof(KeyInputModeSwitch), KeyInputModeChanged_Action);
            Rule(nameof(Algorithm), AlgorithmChanged_Action);
            Rule(nameof(VerifySignature), VerifySignatureChanged_Action);
        }

        protected override void ManualRegisterDependencies()
        {
            base.ManualRegisterDependencies();
            RegisterDependency(KeyInputModeSwitch, nameof(KeyInputModeSwitch.Value), nameof(KeyInputModeSwitch));
            RegisterDependency(Algorithm, nameof(Algorithm.Value), nameof(Algorithm));
            RegisterDependency(VerifySignature, nameof(VerifySignature.Value), nameof(VerifySignature));
        }

        private void KeyInputModeChanged_Action()
        {
            if (Algorithm.Value == EncryptionAlgorithm.PGP) return;

            Key.IsRequired = false;
            Key.IsVisible = false;
            KeySecureString.IsVisible = false;
            KeySecureString.IsRequired = false;

            switch (KeyInputModeSwitch.Value)
            {
                case KeyInputMode.Key:
                    Key.IsVisible = true;
                    Key.IsRequired = true;
                    break;
                case KeyInputMode.SecureKey:
                    KeySecureString.IsVisible = true;
                    KeySecureString.IsRequired = true;
                    break;
                default:
                    throw new NotImplementedException();
            }
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
