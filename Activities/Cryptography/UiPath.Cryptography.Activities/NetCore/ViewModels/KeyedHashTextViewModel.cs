using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Security;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Cryptography.Activities.NetCore.ViewModels;
using UiPath.Cryptography.Activities.Properties;
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
        private readonly DataSource<string> _encodingDataSource;
        private InArgument<string> _persistedKey;
        private InArgument<SecureString> _persistedKeySecureString;
        private bool _useSecureKey;

        public KeyedHashTextViewModel(IDesignServices services) : base(services)
        {
            _encodingDataSource = EncodingHelpers.ConfigureEncodingDataSource();
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
            Algorithm.OrderIndex = propertyOrderIndex++;
            Algorithm.Category = Resources.Input;
            Algorithm.DataSource = DataSourceHelper.ForEnum(KeyedHashAlgorithms.HMACMD5, KeyedHashAlgorithms.HMACSHA1, KeyedHashAlgorithms.HMACSHA256, KeyedHashAlgorithms.HMACSHA384, KeyedHashAlgorithms.HMACSHA512, KeyedHashAlgorithms.SHA1, KeyedHashAlgorithms.SHA256, KeyedHashAlgorithms.SHA384, KeyedHashAlgorithms.SHA512);
            Algorithm.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown };

            Key.IsPrincipal = true;
            Key.OrderIndex = propertyOrderIndex;
            Key.Category = Resources.Input;

            KeySecureString.IsPrincipal = true;
            KeySecureString.OrderIndex = propertyOrderIndex;
            KeySecureString.Category = Resources.Input;
            propertyOrderIndex++;

            KeyEncodingString.IsPrincipal = false;
            KeyEncodingString.IsVisible = true;
            KeyEncodingString.OrderIndex = propertyOrderIndex++;
            KeyEncodingString.Category = Resources.Input;

            KeyEncodingString.DataSource = _encodingDataSource;
            KeyEncodingString.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown, Metadata = new Dictionary<string, string>() };

            _encodingDataSource.Data = EncodingHelpers.GetAvailableEncodings();

            Result.IsPrincipal = false;
            Result.OrderIndex = propertyOrderIndex++;
            Result.Category = Resources.Output;

            ContinueOnError.IsPrincipal = false;
            ContinueOnError.OrderIndex = propertyOrderIndex;
            ContinueOnError.Category = Resources.Category_Options_Name;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle, Metadata = new Dictionary<string, string>() };
            ContinueOnError.Value = false;

            ConfigureKeyInputModeMenuActions();
        }

        private void ConfigureKeyInputModeMenuActions()
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
            ApplyKeyInputModeVisibility();
        }

        private void ApplyKeyInputModeVisibility()
        {
            bool isHmac = Algorithm.Value.ToString().StartsWith(nameof(HMAC));
            Key.IsVisible = isHmac && !_useSecureKey;
            Key.IsRequired = isHmac && !_useSecureKey;
            KeySecureString.IsVisible = isHmac && _useSecureKey;
            KeySecureString.IsRequired = isHmac && _useSecureKey;
        }

        private Task SwitchToKey(MenuAction _)
        {
            if (KeySecureString.Value != null) _persistedKeySecureString = KeySecureString.Value;
            KeySecureString.Value = null;
            Key.Value = _persistedKey;
            _useSecureKey = false;
            ApplyKeyInputModeVisibility();
            return Task.CompletedTask;
        }

        private Task SwitchToKeySecureString(MenuAction _)
        {
            if (Key.Value != null) _persistedKey = Key.Value;
            Key.Value = null;
            KeySecureString.Value = _persistedKeySecureString;
            _useSecureKey = true;
            ApplyKeyInputModeVisibility();
            return Task.CompletedTask;
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
