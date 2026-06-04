using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Threading.Tasks;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Cryptography.Enums;
using UiPath.Platform.ResourceHandling;

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
        private InArgument<string> _persistedKey;
        private InArgument<SecureString> _persistedKeySecureString;
        private bool _useSecureKey;
        private InArgument<string> _persistedPassphrase;
        private InArgument<SecureString> _persistedPassphraseSecureString;
        private bool _useSecurePassphrase;

        protected DecryptCryptoViewModelBase(IDesignServices services) : base(services)
        {
            _encodingDataSource = EncodingHelpers.ConfigureEncodingDataSource();
        }

        public DesignProperty<EncryptionAlgorithm> Algorithm { get; set; } = new DesignProperty<EncryptionAlgorithm>();
        public DesignInArgument<string> Key { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<SecureString> KeySecureString { get; set; } = new DesignInArgument<SecureString>();
        public DesignInArgument<string> KeyEncodingString { get; set; } = new() { Name = nameof(KeyEncodingString) };
        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        public DesignInArgument<string> PrivateKeyFilePath { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<string> Passphrase { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<SecureString> PassphraseSecureString { get; set; } = new DesignInArgument<SecureString>();
        public DesignProperty<bool> VerifySignature { get; set; } = new DesignProperty<bool>();
        public DesignInArgument<string> PublicKeyFilePath { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<IResource> PublicKeyFile { get; set; } = new DesignInArgument<IResource>();
        private InArgument<string> _persistedPublicKeyFilePath;
        private InArgument<IResource> _persistedPublicKeyFile;
        private bool _usePublicKeyResource;

        /// <summary>
        /// Configures Algorithm dropdown, Key, KeySecureString, and KeyEncodingString properties.
        /// </summary>
        protected void ConfigureAlgorithmAndKeyProperties(ref int orderIndex)
        {
            Algorithm.IsPrincipal = true;
            Algorithm.OrderIndex = orderIndex++;
            Algorithm.Category = Resources.Input;
            Algorithm.DataSource = DataSourceHelper.ForEnum(
                // Usable (alphabetical):
                EncryptionAlgorithm.AESGCM,
                EncryptionAlgorithm.ChaCha20Poly1305,
                EncryptionAlgorithm.PGP,
                // Deprecated (alphabetical):
                EncryptionAlgorithm.AES,
                EncryptionAlgorithm.DES,
                EncryptionAlgorithm.RC2,
                EncryptionAlgorithm.Rijndael,
                EncryptionAlgorithm.TripleDES);
            Algorithm.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown };

            Key.IsPrincipal = true;
            Key.OrderIndex = orderIndex;
            Key.Category = Resources.Input;

            KeySecureString.IsPrincipal = true;
            KeySecureString.OrderIndex = orderIndex;
            KeySecureString.Category = Resources.Input;
            orderIndex++;

            KeyEncodingString.IsPrincipal = false;
            KeyEncodingString.IsVisible = true;
            KeyEncodingString.OrderIndex = orderIndex++;
            KeyEncodingString.Category = Resources.Input;

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
            ContinueOnError.Category = Resources.Category_Options_Name;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };
            ContinueOnError.Value = false;

            PrivateKeyFilePath.IsPrincipal = false;
            PrivateKeyFilePath.IsVisible = false;
            PrivateKeyFilePath.OrderIndex = orderIndex++;
            PrivateKeyFilePath.Category = Resources.Input;

            Passphrase.IsPrincipal = false;
            Passphrase.IsVisible = false;
            Passphrase.OrderIndex = orderIndex;
            Passphrase.Category = Resources.Input;

            PassphraseSecureString.IsPrincipal = false;
            PassphraseSecureString.IsVisible = false;
            PassphraseSecureString.OrderIndex = orderIndex;
            PassphraseSecureString.Category = Resources.Input;
            orderIndex++;

            VerifySignature.IsPrincipal = false;
            VerifySignature.IsVisible = false;
            VerifySignature.OrderIndex = orderIndex++;
            VerifySignature.Category = Resources.Input;
            VerifySignature.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            PublicKeyFilePath.IsPrincipal = false;
            PublicKeyFilePath.IsVisible = false;
            PublicKeyFilePath.OrderIndex = orderIndex;
            PublicKeyFilePath.Category = Resources.Input;

            PublicKeyFile.IsPrincipal = false;
            PublicKeyFile.IsVisible = false;
            PublicKeyFile.OrderIndex = orderIndex;
            PublicKeyFile.Category = Resources.Input;
            orderIndex++;
        }

        /// <summary>
        /// Registers Main-menu actions to toggle between PublicKeyFilePath (string) and PublicKeyFile (IResource).
        /// Initial visibility derives from whichever side has a persisted value.
        /// </summary>
        protected void ConfigurePublicKeyFileMenuActions()
        {
            var useFilePathMenuAction = new MenuAction
            {
                DisplayName = Resources.MenuAction_UseFilePath,
                IsMain = true,
                Handler = SwitchToPublicKeyFilePath,
            };
            var useFileMenuAction = new MenuAction
            {
                DisplayName = Resources.MenuAction_UseFile,
                IsMain = true,
                Handler = SwitchToPublicKeyFile,
            };
            PublicKeyFile.AddMenuAction(useFilePathMenuAction);
            PublicKeyFilePath.AddMenuAction(useFileMenuAction);

            _usePublicKeyResource = PublicKeyFile.HasValue && !PublicKeyFilePath.HasValue;
        }

        private Task SwitchToPublicKeyFilePath(MenuAction _)
        {
            if (PublicKeyFile.Value != null) _persistedPublicKeyFile = PublicKeyFile.Value;
            PublicKeyFile.Value = null;
            PublicKeyFilePath.Value = _persistedPublicKeyFilePath;
            _usePublicKeyResource = false;
            ApplyPublicKeyVisibility();
            return Task.CompletedTask;
        }

        private Task SwitchToPublicKeyFile(MenuAction _)
        {
            if (PublicKeyFilePath.Value != null) _persistedPublicKeyFilePath = PublicKeyFilePath.Value;
            PublicKeyFilePath.Value = null;
            PublicKeyFile.Value = _persistedPublicKeyFile;
            _usePublicKeyResource = true;
            ApplyPublicKeyVisibility();
            return Task.CompletedTask;
        }

        private void ApplyPublicKeyVisibility()
        {
            // PublicKey* is only relevant when Algorithm == PGP AND VerifySignature is true.
            bool show = Algorithm.Value == EncryptionAlgorithm.PGP && VerifySignature.Value;
            PublicKeyFilePath.IsVisible = show && !_usePublicKeyResource;
            PublicKeyFilePath.IsRequired = show && !_usePublicKeyResource;
            PublicKeyFilePath.IsPrincipal = show;
            PublicKeyFile.IsVisible = show && _usePublicKeyResource;
            PublicKeyFile.IsRequired = show && _usePublicKeyResource;
            PublicKeyFile.IsPrincipal = show;
        }

        /// <summary>
        /// Registers Main-menu actions to toggle between Key (string) and KeySecureString (SecureString),
        /// and sets initial visibility based on which side has a persisted value.
        /// </summary>
        protected void ConfigureKeyInputModeMenuActions()
        {
            var useKeyMenuAction = new MenuAction
            {
                DisplayName = Resources.MenuAction_UseKey,
                IsMain = true,
                Handler = SwitchToKey,
            };
            var useSecureKeyMenuAction = new MenuAction
            {
                DisplayName = Resources.MenuAction_UseSecureKey,
                IsMain = true,
                Handler = SwitchToKeySecureString,
            };
            KeySecureString.AddMenuAction(useKeyMenuAction);
            Key.AddMenuAction(useSecureKeyMenuAction);

            _useSecureKey = KeySecureString.HasValue && !Key.HasValue;
            Key.IsVisible = !_useSecureKey;
            Key.IsRequired = !_useSecureKey;
            KeySecureString.IsVisible = _useSecureKey;
            KeySecureString.IsRequired = _useSecureKey;
        }

        private Task SwitchToKey(MenuAction _)
        {
            if (Algorithm.Value == EncryptionAlgorithm.PGP) return Task.CompletedTask;
            if (KeySecureString.Value != null) _persistedKeySecureString = KeySecureString.Value;
            KeySecureString.Value = null;
            Key.Value = _persistedKey;
            Key.IsVisible = true;
            Key.IsRequired = true;
            KeySecureString.IsVisible = false;
            KeySecureString.IsRequired = false;
            _useSecureKey = false;
            return Task.CompletedTask;
        }

        private Task SwitchToKeySecureString(MenuAction _)
        {
            if (Algorithm.Value == EncryptionAlgorithm.PGP) return Task.CompletedTask;
            if (Key.Value != null) _persistedKey = Key.Value;
            Key.Value = null;
            KeySecureString.Value = _persistedKeySecureString;
            KeySecureString.IsVisible = true;
            KeySecureString.IsRequired = true;
            Key.IsVisible = false;
            Key.IsRequired = false;
            _useSecureKey = true;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Registers Main-menu actions to toggle between Passphrase (string) and PassphraseSecureString (SecureString),
        /// and sets initial visibility based on which side has a persisted value.
        /// </summary>
        protected void ConfigurePassphraseInputModeMenuActions()
        {
            var usePassphraseMenuAction = new MenuAction
            {
                DisplayName = Resources.MenuAction_UsePassphrase,
                IsMain = true,
                Handler = SwitchToPassphrase,
            };
            var useSecurePassphraseMenuAction = new MenuAction
            {
                DisplayName = Resources.MenuAction_UseSecurePassphrase,
                IsMain = true,
                Handler = SwitchToPassphraseSecureString,
            };
            PassphraseSecureString.AddMenuAction(usePassphraseMenuAction);
            Passphrase.AddMenuAction(useSecurePassphraseMenuAction);

            _useSecurePassphrase = PassphraseSecureString.HasValue && !Passphrase.HasValue;
            // Final visibility is set by AlgorithmChanged_Action.
        }

        private Task SwitchToPassphrase(MenuAction _)
        {
            if (PassphraseSecureString.Value != null) _persistedPassphraseSecureString = PassphraseSecureString.Value;
            PassphraseSecureString.Value = null;
            Passphrase.Value = _persistedPassphrase;
            _useSecurePassphrase = false;
            ApplyPassphraseVisibility();
            return Task.CompletedTask;
        }

        private Task SwitchToPassphraseSecureString(MenuAction _)
        {
            if (Passphrase.Value != null) _persistedPassphrase = Passphrase.Value;
            Passphrase.Value = null;
            PassphraseSecureString.Value = _persistedPassphraseSecureString;
            _useSecurePassphrase = true;
            ApplyPassphraseVisibility();
            return Task.CompletedTask;
        }

        private void ApplyPassphraseVisibility()
        {
            bool active = Algorithm.Value == EncryptionAlgorithm.PGP;
            Passphrase.IsVisible = active && !_useSecurePassphrase;
            Passphrase.IsRequired = active && !_useSecurePassphrase;
            Passphrase.IsPrincipal = active;
            PassphraseSecureString.IsVisible = active && _useSecurePassphrase;
            PassphraseSecureString.IsRequired = active && _useSecurePassphrase;
            PassphraseSecureString.IsPrincipal = active;
        }

        protected override void InitializeRules()
        {
            base.InitializeRules();
            Rule(nameof(Algorithm), AlgorithmChanged_Action);
            Rule(nameof(VerifySignature), VerifySignatureChanged_Action);
        }

        protected override void ManualRegisterDependencies()
        {
            base.ManualRegisterDependencies();
            RegisterDependency(Algorithm, nameof(Algorithm.Value), nameof(Algorithm));
            RegisterDependency(VerifySignature, nameof(VerifySignature.Value), nameof(VerifySignature));
        }

        private void AlgorithmChanged_Action()
        {
            bool isPgp = Algorithm.Value == EncryptionAlgorithm.PGP;

            Key.IsVisible = !isPgp && !_useSecureKey;
            Key.IsRequired = !isPgp && !_useSecureKey;
            KeySecureString.IsVisible = !isPgp && _useSecureKey;
            KeySecureString.IsRequired = !isPgp && _useSecureKey;
            KeyEncodingString.IsVisible = !isPgp;

            PrivateKeyFilePath.IsVisible = isPgp;
            PrivateKeyFilePath.IsRequired = isPgp;
            PrivateKeyFilePath.IsPrincipal = isPgp;
            ApplyPassphraseVisibility();
            VerifySignature.IsVisible = isPgp;
            VerifySignature.IsPrincipal = isPgp;
            ApplyPublicKeyVisibility();
        }

        private void VerifySignatureChanged_Action()
        {
            if (Algorithm.Value != EncryptionAlgorithm.PGP) return;
            ApplyPublicKeyVisibility();
        }
    }
}
