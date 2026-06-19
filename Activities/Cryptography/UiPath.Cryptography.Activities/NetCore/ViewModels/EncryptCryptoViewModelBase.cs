using System;
using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Security;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Cryptography.Enums;
using UiPath.Platform.ResourceHandling;
using AesKeySizeEnum = UiPath.Cryptography.Enums.AesKeySize;

#pragma warning disable CS0618 // obsolete encryption algorithm

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    /// <summary>
    /// Base ViewModel for encrypt activities. Contains shared Algorithm/Key/PGP
    /// properties, their design-time configuration, and rule handlers.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public abstract class EncryptCryptoViewModelBase : DesignPropertiesViewModel
    {
        private readonly DataSource<string> _encodingDataSource;
        private readonly PairedInputToggle<string, SecureString> _keyToggle;
        private readonly PairedInputToggle<string, SecureString> _passphraseToggle;
        private readonly PairedInputToggle<string, IResource> _publicKeyFileToggle;

        protected EncryptCryptoViewModelBase(IDesignServices services) : base(services)
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

        [NotMappedProperty]
        public DesignProperty<string> DeprecatedWarning { get; set; } = new DesignProperty<string>();

        public DesignInArgument<string> PublicKeyFilePath { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<IResource> PublicKeyFile { get; set; } = new DesignInArgument<IResource>();
        public DesignProperty<bool> SignData { get; set; } = new DesignProperty<bool>();
        public DesignInArgument<string> PrivateKeyFilePath { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<string> Passphrase { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<SecureString> PassphraseSecureString { get; set; } = new DesignInArgument<SecureString>();

        public DesignProperty<SymmetricWireFormat> Format { get; set; } = new DesignProperty<SymmetricWireFormat>();
        public DesignProperty<KeyBytesFormat> KeyFormat { get; set; } = new DesignProperty<KeyBytesFormat>();
        public DesignInArgument<string> Iv { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<int> KdfIterations { get; set; } = new DesignInArgument<int>();
        public DesignProperty<AesKeySize> AesKeySize { get; set; } = new DesignProperty<AesKeySize>();

        /// <summary>
        /// Configures Algorithm dropdown, DeprecatedWarning, Key, KeySecureString,
        /// and KeyEncodingString properties.
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

            DeprecatedWarning.OrderIndex = orderIndex++;
            DeprecatedWarning.Category = Resources.Input;
            DeprecatedWarning.Widget = new TextBlockWidget
            {
                Level = TextBlockWidgetLevel.Warning,
                Multiline = true,
            };
            DeprecatedWarning.Value = Resources.Activity_Encrypt_Algorithm_Deprecated_Warning;

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
        /// Configures a code-page encoding dropdown on the supplied design property, mirroring the
        /// <see cref="KeyEncodingString"/> setup. Used by Text activities to surface their plaintext
        /// encoding; File activities operate on raw bytes and do not call this.
        /// </summary>
        protected void ConfigureEncodingDropdown(DesignInArgument<string> encodingProperty, ref int orderIndex)
        {
            var dataSource = EncodingHelpers.ConfigureEncodingDataSource();
            encodingProperty.IsPrincipal = false;
            encodingProperty.OrderIndex = orderIndex++;
            encodingProperty.Category = Resources.Input;
            encodingProperty.DataSource = dataSource;
            encodingProperty.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown, Metadata = new Dictionary<string, string>() };
            dataSource.Data = EncodingHelpers.GetAvailableEncodings();
        }

        /// <summary>
        /// Configures the third-party-compatibility properties (Format, KeyFormat, Iv, KdfIterations).
        /// Format is visible by default; the others are hidden until <see cref="ApplyInteropVisibility"/>
        /// reveals them based on Format/Algorithm.
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

            Iv.IsPrincipal = false;
            Iv.IsVisible = false;
            Iv.OrderIndex = orderIndex++;
            Iv.Category = Resources.Input;

            KdfIterations.IsPrincipal = false;
            KdfIterations.IsVisible = false;
            KdfIterations.OrderIndex = orderIndex++;
            KdfIterations.Category = Resources.Input;

            AesKeySize.IsPrincipal = false;
            AesKeySize.IsVisible = false;
            AesKeySize.OrderIndex = orderIndex++;
            AesKeySize.Category = Resources.Input;
            AesKeySize.DataSource = DataSourceHelper.ForEnum(
                AesKeySizeEnum.Aes128,
                AesKeySizeEnum.Aes192,
                AesKeySizeEnum.Aes256);
            AesKeySize.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown };
            AesKeySize.Value = AesKeySizeEnum.Aes256;
        }

        /// <summary>
        /// Configures ContinueOnError and PGP encrypt properties
        /// (PublicKeyFilePath, SignData, PrivateKeyFilePath, Passphrase — hidden by default).
        /// </summary>
        protected void ConfigureTailProperties(ref int orderIndex)
        {
            ContinueOnError.IsPrincipal = false;
            ContinueOnError.OrderIndex = orderIndex++;
            ContinueOnError.Category = Resources.Category_Options_Name;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };
            ContinueOnError.Value = false;

            PublicKeyFilePath.IsPrincipal = false;
            PublicKeyFilePath.IsVisible = false;
            PublicKeyFilePath.OrderIndex = orderIndex;
            PublicKeyFilePath.Category = Resources.Input;

            PublicKeyFile.IsPrincipal = false;
            PublicKeyFile.IsVisible = false;
            PublicKeyFile.OrderIndex = orderIndex;
            PublicKeyFile.Category = Resources.Input;
            orderIndex++;

            SignData.IsPrincipal = false;
            SignData.IsVisible = false;
            SignData.OrderIndex = orderIndex++;
            SignData.Category = Resources.Category_Options_Name;
            SignData.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

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
        /// Final visibility is set by AlgorithmChanged_Action / SignDataChanged_Action.
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
            bool isPgp = Algorithm.Value == EncryptionAlgorithm.PGP;
            bool useResource = _publicKeyFileToggle.UseSecondary;
            PublicKeyFilePath.IsVisible = isPgp && !useResource;
            PublicKeyFilePath.IsRequired = isPgp && !useResource;
            PublicKeyFile.IsVisible = isPgp && useResource;
            PublicKeyFile.IsRequired = isPgp && useResource;
            PublicKeyFilePath.IsPrincipal = isPgp;
            PublicKeyFile.IsPrincipal = isPgp;
        }

        private void ApplyPassphraseVisibility()
        {
            bool active = Algorithm.Value == EncryptionAlgorithm.PGP && SignData.Value;
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
            Rule(nameof(SignData), SignDataChanged_Action);
            Rule(nameof(Format), FormatChanged_Action);
        }

        protected override void ManualRegisterDependencies()
        {
            base.ManualRegisterDependencies();
            RegisterDependency(Algorithm, nameof(Algorithm.Value), nameof(Algorithm));
            RegisterDependency(SignData, nameof(SignData.Value), nameof(SignData));
            RegisterDependency(Format, nameof(Format.Value), nameof(Format));
        }

        private void AlgorithmChanged_Action()
        {
            UpdateDeprecatedAlgorithmWarning();

            bool isPgp = Algorithm.Value == EncryptionAlgorithm.PGP;
            KeyEncodingString.IsVisible = !isPgp;

            ApplyKeyInputVisibility();
            ApplyPublicKeyVisibility();
            SignData.IsVisible = isPgp;
            SignData.IsPrincipal = isPgp;
            PrivateKeyFilePath.IsVisible = isPgp && SignData.Value;
            PrivateKeyFilePath.IsRequired = isPgp && SignData.Value;
            PrivateKeyFilePath.IsPrincipal = isPgp && SignData.Value;
            ApplyPassphraseVisibility();
            ApplyInteropVisibility();
            OnAlgorithmChanged(isPgp);
        }

        /// <summary>
        /// Hook invoked at the end of <see cref="AlgorithmChanged_Action"/> so derived ViewModels can
        /// react to the active algorithm (e.g. hide Text-only properties when PGP is selected).
        /// </summary>
        protected virtual void OnAlgorithmChanged(bool isPgp) { }

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
            bool isOpenSslAes = Format.Value == SymmetricWireFormat.OpenSslEnc && Algorithm.Value == EncryptionAlgorithm.AES;

            Format.IsVisible = !isPgp;
            KeyFormat.IsVisible = !isPgp && isRaw;
            Iv.IsVisible = !isPgp && isRaw;
            KdfIterations.IsVisible = !isPgp && isOwasp2026OrOpenSsl;
            AesKeySize.IsVisible = !isPgp && isOpenSslAes;

            // Surface the underlying KDF in the visible label so the iteration count's effect is unambiguous.
            if (KdfIterations.IsVisible)
            {
                KdfIterations.DisplayName = Format.Value == SymmetricWireFormat.Owasp2026
                    ? Resources.Activity_KdfIterations_DisplayName_Pbkdf2Sha1
                    : Resources.Activity_KdfIterations_DisplayName_Pbkdf2Sha256;
            }
        }

        private void UpdateDeprecatedAlgorithmWarning()
        {
            try
            {
                var enumName = typeof(EncryptionAlgorithm).GetEnumName(Algorithm.Value);
                var field = typeof(EncryptionAlgorithm).GetField(enumName);
                var obsoleteAttribute = field?.GetCustomAttribute<ObsoleteAttribute>();
                DeprecatedWarning.IsVisible = obsoleteAttribute != null;
            }
            catch
            {
                DeprecatedWarning.IsVisible = false;
            }
        }

        private void SignDataChanged_Action()
        {
            if (Algorithm.Value != EncryptionAlgorithm.PGP) return;

            PrivateKeyFilePath.IsVisible = SignData.Value;
            PrivateKeyFilePath.IsRequired = SignData.Value;
            PrivateKeyFilePath.IsPrincipal = SignData.Value;
            ApplyPassphraseVisibility();
        }
    }
}
