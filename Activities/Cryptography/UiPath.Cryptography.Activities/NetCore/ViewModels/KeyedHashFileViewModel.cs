using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Cryptography.Enums;
using UiPath.Platform.ResourceHandling;

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class KeyedHashFileViewModel : DesignPropertiesViewModel
    {
        private readonly DataSource<string> _encodingDataSource;
        private InArgument<IResource> _persistedInputFile;
        private InArgument<string> _persistedInputFilePath;
        private InArgument<string> _persistedKey;
        private InArgument<SecureString> _persistedKeySecureString;
        private bool _useSecureKey;

        public KeyedHashFileViewModel(IDesignServices services) : base(services)
        {
            _encodingDataSource = EncodingHelpers.ConfigureEncodingDataSource();
        }

        public DesignInArgument<IResource> InputFile { get; set; } = new DesignInArgument<IResource>();
        public DesignInArgument<string> FilePath { get; set; } = new DesignInArgument<string>();
        public DesignProperty<KeyedHashAlgorithms> Algorithm { get; set; } = new DesignProperty<KeyedHashAlgorithms>();
        public DesignInArgument<string> Key { get; set; } = new DesignInArgument<string>();
        public DesignInArgument<SecureString> KeySecureString { get; set; } = new DesignInArgument<SecureString>();
        public DesignInArgument<string> KeyEncodingString { get; set; } = new() { Name = nameof(KeyEncodingString) };
        public DesignOutArgument<string> Result { get; set; } = new DesignOutArgument<string>();
        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var propertyOrderIndex = 1;

            InputFile.IsPrincipal = true;
            InputFile.OrderIndex = propertyOrderIndex;
            InputFile.Category = Resources.Input;

            FilePath.IsPrincipal = true;
            FilePath.OrderIndex = propertyOrderIndex;
            FilePath.Category = Resources.Input;
            propertyOrderIndex++;

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
            ConfigureInputFileMenuActions();
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

        private void ConfigureInputFileMenuActions()
        {
            var useFileMenuAction = new MenuAction
            {
                DisplayName = Resources.MenuAction_UseFile,
                IsMain = true,
                Handler = SwitchToInputFile,
            };
            var useFilePathMenuAction = new MenuAction
            {
                DisplayName = Resources.MenuAction_UseFilePath,
                IsMain = true,
                Handler = SwitchToInputFilePath,
            };
            FilePath.AddMenuAction(useFileMenuAction);
            InputFile.AddMenuAction(useFilePathMenuAction);

            bool useFile = InputFile.HasValue && !FilePath.HasValue;
            InputFile.IsVisible = useFile;
            InputFile.IsRequired = useFile;
            FilePath.IsVisible = !useFile;
            FilePath.IsRequired = !useFile;
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

        private Task SwitchToInputFile(MenuAction _)
        {
            if (FilePath.Value != null) _persistedInputFilePath = FilePath.Value;
            FilePath.Value = null;
            InputFile.Value = _persistedInputFile;
            InputFile.IsVisible = true;
            InputFile.IsRequired = true;
            FilePath.IsVisible = false;
            FilePath.IsRequired = false;
            return Task.CompletedTask;
        }

        private Task SwitchToInputFilePath(MenuAction _)
        {
            if (InputFile.Value != null) _persistedInputFile = InputFile.Value;
            InputFile.Value = null;
            FilePath.Value = _persistedInputFilePath;
            FilePath.IsVisible = true;
            FilePath.IsRequired = true;
            InputFile.IsVisible = false;
            InputFile.IsRequired = false;
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
