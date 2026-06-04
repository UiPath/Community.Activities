using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Security;
using System.Security.Cryptography;
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

            _keyToggle.ConfigureMenuActions();
            ApplyKeyInputModeVisibility();
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
