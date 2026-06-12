using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security;
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
        private readonly PairedInputToggle<string, SecureString> _keyToggle;
        private readonly PairedInputToggle<string, SecureString> _passphraseToggle;
        private readonly PairedInputToggle<string, IResource> _publicKeyFileToggle;

        protected DecryptCryptoViewModelBase(IDesignServices services) : base(services)
        {
            _encodingDataSource = EncodingHelpers.ConfigureEncodingDataSource();

            _keyToggle = new PairedInputToggle<string, SecureString>(
                Key, KeySecureString,
                Resources.MenuAction_UseKey,
                Resources.MenuAction_UseSecureKey)
            {
                SwitchGuard = () => Algorithm.Value == EncryptionAlgorithm.PGP,
                AfterSwitch = ApplyKeyInputVisibility,
            };

            _passphraseToggle = new PairedInputToggle<string, SecureString>(
                Passphrase, PassphraseSecureString,
                Resources.MenuAction_UsePassphrase,
                Resources.MenuAction_UseSecurePassphrase)
            {
                AfterSwitch = ApplyPassphraseVisibility,
            };

            _publicKeyFileToggle = new PairedInputToggle<string, IResource>(
                PublicKeyFilePath, PublicKeyFile,
                Resources.MenuAction_UseFilePath,
                Resources.MenuAction_UseFile)
            {
                AfterSwitch = ApplyPublicKeyVisibility,
            };
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

        public DesignProperty<SymmetricWireFormat> Format { get; set; } = new DesignProperty<SymmetricWireFormat>();
        public DesignProperty<KeyBytesFormat> KeyFormat { get; set; } = new DesignProperty<KeyBytesFormat>();
        public DesignInArgument<int> KdfIterations { get; set; } = new DesignInArgument<int>();

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
        /// Configures the third-party-compatibility properties (Format, KeyFormat, KdfIterations).
        /// Format is visible by default; the others are hidden until <see cref="ApplyInteropVisibility"/>
        /// reveals them based on Format/Algorithm. Decrypt has no IV property — the IV is read from
        /// the ciphertext stream at decrypt time.
        /// </summary>
        protected void ConfigureInteropProperties(ref int orderIndex)
        {
            Format.IsPrincipal = false;
            Format.IsVisible = true;
            Format.OrderIndex = orderIndex++;
            Format.Category = Resources.Input;
            Format.DataSource = DataSourceHelper.ForEnum(
                SymmetricWireFormat.Classic,
                SymmetricWireFormat.Owasp2026,
                SymmetricWireFormat.Raw,
                SymmetricWireFormat.OpenSslEnc);
            Format.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown };

            KeyFormat.IsPrincipal = false;
            KeyFormat.IsVisible = false;
            KeyFormat.OrderIndex = orderIndex++;
            KeyFormat.Category = Resources.Input;
            // Encoded is intentionally omitted — the dropdown is visible only when Format = Raw,
            // and Raw rejects Encoded at runtime. FormatChanged_Action keeps the underlying value
            // in sync (Hex when Raw, Encoded otherwise) so non-Raw runtime validation stays clean.
            KeyFormat.DataSource = DataSourceHelper.ForEnum(
                KeyBytesFormat.Hex,
                KeyBytesFormat.Base64);
            KeyFormat.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown };

            KdfIterations.IsPrincipal = false;
            KdfIterations.IsVisible = false;
            KdfIterations.OrderIndex = orderIndex++;
            KdfIterations.Category = Resources.Input;
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
        /// </summary>
        protected void ConfigurePublicKeyFileMenuActions() => _publicKeyFileToggle.ConfigureMenuActions();

        /// <summary>
        /// Registers Main-menu actions to toggle between Key (string) and KeySecureString (SecureString),
        /// and sets initial visibility based on which side has a persisted value.
        /// </summary>
        protected void ConfigureKeyInputModeMenuActions()
        {
            _keyToggle.ConfigureMenuActions();
            ApplyKeyInputVisibility();
        }

        /// <summary>
        /// Registers Main-menu actions to toggle between Passphrase (string) and PassphraseSecureString (SecureString).
        /// Final visibility is set by AlgorithmChanged_Action.
        /// </summary>
        protected void ConfigurePassphraseInputModeMenuActions() => _passphraseToggle.ConfigureMenuActions();

        private void ApplyKeyInputVisibility()
        {
            bool useSecure = _keyToggle.UseSecondary;
            bool isPgp = Algorithm.Value == EncryptionAlgorithm.PGP;
            Key.IsVisible = !isPgp && !useSecure;
            Key.IsRequired = !isPgp && !useSecure;
            KeySecureString.IsVisible = !isPgp && useSecure;
            KeySecureString.IsRequired = !isPgp && useSecure;
        }

        private void ApplyPublicKeyVisibility()
        {
            // PublicKey* is only relevant when Algorithm == PGP AND VerifySignature is true.
            bool show = Algorithm.Value == EncryptionAlgorithm.PGP && VerifySignature.Value;
            bool useResource = _publicKeyFileToggle.UseSecondary;
            PublicKeyFilePath.IsVisible = show && !useResource;
            PublicKeyFilePath.IsRequired = show && !useResource;
            PublicKeyFilePath.IsPrincipal = show;
            PublicKeyFile.IsVisible = show && useResource;
            PublicKeyFile.IsRequired = show && useResource;
            PublicKeyFile.IsPrincipal = show;
        }

        private void ApplyPassphraseVisibility()
        {
            bool active = Algorithm.Value == EncryptionAlgorithm.PGP;
            bool useSecure = _passphraseToggle.UseSecondary;
            Passphrase.IsVisible = active && !useSecure;
            Passphrase.IsRequired = active && !useSecure;
            Passphrase.IsPrincipal = active;
            PassphraseSecureString.IsVisible = active && useSecure;
            PassphraseSecureString.IsRequired = active && useSecure;
            PassphraseSecureString.IsPrincipal = active;
        }

        protected override void InitializeRules()
        {
            base.InitializeRules();
            Rule(nameof(Algorithm), AlgorithmChanged_Action);
            Rule(nameof(VerifySignature), VerifySignatureChanged_Action);
            Rule(nameof(Format), FormatChanged_Action);
        }

        protected override void ManualRegisterDependencies()
        {
            base.ManualRegisterDependencies();
            RegisterDependency(Algorithm, nameof(Algorithm.Value), nameof(Algorithm));
            RegisterDependency(VerifySignature, nameof(VerifySignature.Value), nameof(VerifySignature));
            RegisterDependency(Format, nameof(Format.Value), nameof(Format));
        }

        private void AlgorithmChanged_Action()
        {
            bool isPgp = Algorithm.Value == EncryptionAlgorithm.PGP;
            KeyEncodingString.IsVisible = !isPgp;

            ApplyKeyInputVisibility();
            PrivateKeyFilePath.IsVisible = isPgp;
            PrivateKeyFilePath.IsRequired = isPgp;
            PrivateKeyFilePath.IsPrincipal = isPgp;
            ApplyPassphraseVisibility();
            VerifySignature.IsVisible = isPgp;
            VerifySignature.IsPrincipal = isPgp;
            ApplyPublicKeyVisibility();
            ApplyInteropVisibility();
        }

        private void FormatChanged_Action()
        {
            ApplyInteropVisibility();
            // Snap KdfIterations to a concrete value whenever Format changes — avoids the user seeing 0
            // and having to look up what the format ships with. Customizations made before the format
            // change are discarded by design (they were tied to the previous format's KDF anyway).
            KdfIterations.Value = KdfIterations.IsVisible
                ? CryptographyHelper.GetRecommendedIterations(Format.Value)
                : 0;
            // Snap KeyFormat: Hex when Raw (so the dropdown lands on a valid option),
            // Encoded otherwise (so non-Raw runtime validation passes).
            KeyFormat.Value = Format.Value == SymmetricWireFormat.Raw
                ? KeyBytesFormat.Hex
                : KeyBytesFormat.Encoded;
        }

        private void ApplyInteropVisibility()
        {
            bool isPgp = Algorithm.Value == EncryptionAlgorithm.PGP;
            bool isRaw = Format.Value == SymmetricWireFormat.Raw;
            bool isOwasp2026OrOpenSsl = Format.Value == SymmetricWireFormat.Owasp2026 || Format.Value == SymmetricWireFormat.OpenSslEnc;

            Format.IsVisible = !isPgp;
            KeyFormat.IsVisible = !isPgp && isRaw;
            KdfIterations.IsVisible = !isPgp && isOwasp2026OrOpenSsl;

            // Surface the underlying KDF in the visible label so the iteration count's effect is unambiguous.
            if (KdfIterations.IsVisible)
            {
                KdfIterations.DisplayName = Format.Value == SymmetricWireFormat.Owasp2026
                    ? Resources.Activity_KdfIterations_DisplayName_Pbkdf2Sha1
                    : Resources.Activity_KdfIterations_DisplayName_Pbkdf2Sha256;
            }
        }

        private void VerifySignatureChanged_Action()
        {
            if (Algorithm.Value != EncryptionAlgorithm.PGP) return;
            ApplyPublicKeyVisibility();
        }
    }
}
