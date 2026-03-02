using System;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using UiPath.FTP.Activities.Properties;
using UiPath.FTP.Enums;
using UiPath.Shared;
using UiPath.Shared.Activities;

namespace UiPath.FTP.Activities.NetCore.ViewModels
{
    internal class WithFtpSessionViewModel : BaseFtpViewModel
    {
        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public WithFtpSessionViewModel(IDesignServices services) : base(services)
        {
            InitializeFtpsModeDataSource();
            InitializeProxyModeDataSource();
        }

        /// <summary>
        /// The URL of the FTP server that you want to connect to.
        /// </summary>
        public DesignInArgument<string> Host { get; set; }

        /// <summary>
        /// The port of the FTP server that you want to connect to.
        /// </summary>
        public DesignInArgument<int> Port { get; set; }

        /// <summary>
        /// The connection timeout in milliseconds.
        /// </summary>
        public DesignInArgument<int> Timeout { get; set; }

        /// <summary>
        /// The username that will be used to connect to the FTP server.
        /// </summary>
        public DesignInArgument<string> Username { get; set; }

        /// <summary>
        /// The password that will be used to connect to the FTP server.
        /// </summary>
        public DesignInArgument<string> Password { get; set; }

        /// <summary>
        /// The secure password that will be used to connect to the FTP server.
        /// </summary>
        public DesignInArgument<SecureString> SecurePassword { get; set; }

        /// <summary>
        /// Switches Password as string or secure string
        /// </summary>
        public DesignProperty<PasswordInputMode> PasswordInputModeSwitch { get; set; }

        /// <summary>
        /// When this box is checked, the username and password fields are ignored, and a standard anonymous user is used instead.
        /// </summary>
        public DesignProperty<bool> UseAnonymousLogin { get; set; }

        /// <summary>
        /// Switches to the FTPS protocol.
        /// </summary>
        public DesignProperty<FtpsMode> FtpsMode { get; set; }

        /// <summary>
        /// Select the SSL protocol to be used for the FTPS connection
        /// </summary>
        public DesignProperty<FtpSslProtocols> SslProtocols { get; set; }

        /// <summary>
        /// Check this box if you want to use the SFTP transfer protocol.
        /// </summary>
        public DesignProperty<bool> UseSftp { get; set; }

        /// <summary>
        /// The path to the certificate used to verify the identity of the client.
        /// </summary>
        public DesignInArgument<string> ClientCertificatePath { get; set; }

        /// <summary>
        /// The password for the client certificate.
        /// </summary>
        public DesignInArgument<string> ClientCertificatePassword { get; set; }

        /// <summary>
        /// The secure password that will be used to connect to the FTP server.
        /// </summary>
        public DesignInArgument<SecureString> ClientCertificateSecurePassword { get; set; }

        /// <summary>
        /// Switches Password as string or secure string
        /// </summary>
        public DesignProperty<PasswordInputMode> CertificatePasswordInputModeSwitch { get; set; }

        /// <summary>
        /// If this box is checked, all certificates will be accepted, including the ones that are expired or not verified.
        /// </summary>
        public DesignProperty<bool> AcceptAllCertificates { get; set; }

        /// <summary>
        /// The type of proxy used
        /// </summary>
        public DesignProperty<FtpProxyType> ProxyType { get; set; }

        /// <summary>
        /// The proxy host
        /// </summary>
        public DesignInArgument<string> ProxyServer { get; set; }

        /// <summary>
        /// The proxy port
        /// </summary>
        public DesignInArgument<int> ProxyPort { get; set; }

        /// <summary>
        /// User used for proxy authentification
        /// </summary>
        public DesignInArgument<string> ProxyUser { get; set; }

        /// <summary>
        /// Password for proxy
        /// </summary>
        public DesignInArgument<string> ProxyPassword { get; set; }

        /// <summary>
        /// Secured password for proxy
        /// </summary>
        public DesignInArgument<SecureString> ProxySecurePassword { get; set; }

        /// <summary>
        /// Switches Proxy Password as string or secure string
        /// </summary>
        public DesignProperty<PasswordInputMode> ProxyPasswordInputModeSwitch { get; set; }

        private static DataSource<FtpSslProtocols> _sslProtocolsDataSource;

        private static IDataSource _proxyTypeDataSource;

        protected override void InitializeModel()
        {
            base.InitializeModel();
            PersistValuesChangedDuringInit();

            int propertyOrderIndex = 1;

            Host.OrderIndex = propertyOrderIndex++;
            Username.OrderIndex = propertyOrderIndex++;
            Password.OrderIndex = propertyOrderIndex++;
            SecurePassword.OrderIndex = propertyOrderIndex++;
            Port.OrderIndex = propertyOrderIndex++;
            Timeout.OrderIndex = propertyOrderIndex++;
            UseAnonymousLogin.OrderIndex = propertyOrderIndex++;
            ContinueOnError.OrderIndex = propertyOrderIndex++;
            AcceptAllCertificates.OrderIndex = propertyOrderIndex++;
            ClientCertificatePath.OrderIndex = propertyOrderIndex++;
            ClientCertificatePassword.OrderIndex = propertyOrderIndex++;
            ClientCertificateSecurePassword.OrderIndex = propertyOrderIndex++;
            FtpsMode.OrderIndex = propertyOrderIndex++;
            SslProtocols.OrderIndex = propertyOrderIndex++;
            UseSftp.OrderIndex = propertyOrderIndex;

            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.NullableBoolean };
            SslProtocols.Widget = new DefaultWidget { Type = ViewModelWidgetType.MultiSelect };
            UseSftp.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            SslProtocols.DataSource = _sslProtocolsDataSource;
            ProxyType.DataSource = _proxyTypeDataSource;

            MenuActionsBuilder<PasswordInputMode>.WithValueProperty(PasswordInputModeSwitch)
              .AddMenuProperty(Password, PasswordInputMode.Password)
              .AddMenuProperty(SecurePassword, PasswordInputMode.SecurePassword)
              .BuildAndInsertMenuActions(true);

            MenuActionsBuilder<PasswordInputMode>.WithValueProperty(CertificatePasswordInputModeSwitch)
              .AddMenuProperty(ClientCertificatePassword, PasswordInputMode.Password)
              .AddMenuProperty(ClientCertificateSecurePassword, PasswordInputMode.SecurePassword)
              .BuildAndInsertMenuActions(true);

            MenuActionsBuilder<PasswordInputMode>.WithValueProperty(ProxyPasswordInputModeSwitch)
              .AddMenuProperty(ProxyPassword, PasswordInputMode.Password)
              .AddMenuProperty(ProxySecurePassword, PasswordInputMode.SecurePassword)
              .BuildAndInsertMenuActions(true);
        }

        /// <inheritdoc/>
        protected override void InitializeRules()
        {
            base.InitializeRules();
            Rule(nameof(PasswordInputModeSwitch), PasswordInputModeChanged_Action);
            Rule(nameof(CertificatePasswordInputModeSwitch), CertificatePasswordInputModeChanged_Action);
            Rule(nameof(ProxyPasswordInputModeSwitch), ProxyPasswordInputModeChanged_Action);
            Rule(nameof(FtpsMode), FtpEncryptionModeChanged_Action);
            Rule(nameof(ProxyType), ProxyModeChanged_Action);

        }

        /// <inheritdoc/>
        protected override void ManualRegisterDependencies()
        {
            base.ManualRegisterDependencies();
            RegisterDependency(PasswordInputModeSwitch, nameof(PasswordInputModeSwitch.Value), nameof(PasswordInputModeSwitch));
            RegisterDependency(CertificatePasswordInputModeSwitch, nameof(CertificatePasswordInputModeSwitch.Value), nameof(CertificatePasswordInputModeSwitch));
            RegisterDependency(ProxyPasswordInputModeSwitch, nameof(ProxyPasswordInputModeSwitch.Value), nameof(ProxyPasswordInputModeSwitch));
            RegisterDependency(FtpsMode, nameof(FtpsMode.Value), nameof(FtpsMode));
            RegisterDependency(ProxyType, nameof(ProxyType.Value), nameof(ProxyType));
        }

        /// <summary>
        /// Password input Mode has changed. Set controls visibility based on selection
        /// </summary>
        private void PasswordInputModeChanged_Action()
        {
            switch (PasswordInputModeSwitch.Value)
            {
                case PasswordInputMode.Password:
                    Password.IsVisible = true;
                    SecurePassword.IsVisible = false;
                    break;
                case PasswordInputMode.SecurePassword:
                    Password.IsVisible = false;
                    SecurePassword.IsVisible = true;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// CertificatePassword input Mode has changed. Set controls visibility based on selection
        /// </summary>
        private void CertificatePasswordInputModeChanged_Action()
        {
            switch (CertificatePasswordInputModeSwitch.Value)
            {
                case PasswordInputMode.Password:
                    ClientCertificatePassword.IsVisible = true;
                    ClientCertificateSecurePassword.IsVisible = false;
                    break;
                case PasswordInputMode.SecurePassword:
                    ClientCertificatePassword.IsVisible = false;
                    ClientCertificateSecurePassword.IsVisible = true;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// CertificatePassword input Mode has changed. Set controls visibility based on selection
        /// </summary>
        private void ProxyPasswordInputModeChanged_Action()
        {
            switch (ProxyPasswordInputModeSwitch.Value)
            {
                case PasswordInputMode.Password:
                    ProxyPassword.IsVisible = true;
                    ProxySecurePassword.IsVisible = false;
                    break;
                case PasswordInputMode.SecurePassword:
                    ProxyPassword.IsVisible = false;
                    ProxySecurePassword.IsVisible = true;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void FtpEncryptionModeChanged_Action()
        {
            if (FtpsMode.Value == FTP.FtpsMode.None)
            {
                SslProtocols.IsVisible = false;
                return;
            }

            SslProtocols.IsVisible = true;
        }

        private void ProxyModeChanged_Action()
        {
            bool proxyConfigVisible = ProxyType.Value != FtpProxyType.None;
            ProxyServer.IsVisible = proxyConfigVisible;
            ProxyPort.IsVisible = proxyConfigVisible;
            ProxyUser.IsVisible = proxyConfigVisible;
            ProxyPassword.IsVisible = proxyConfigVisible && ProxyPasswordInputModeSwitch.Value == PasswordInputMode.Password;
            ProxySecurePassword.IsVisible = proxyConfigVisible && ProxyPasswordInputModeSwitch.Value == PasswordInputMode.SecurePassword;
            ProxyServer.IsVisible = proxyConfigVisible;
        }

        private static void InitializeFtpsModeDataSource()
        {
            if (_sslProtocolsDataSource is not null)
            {
                return;
            }

            var protocols = Enum.GetValues<FtpSslProtocols>()
                .Where(s => s != FtpSslProtocols.Auto && s != FtpSslProtocols.Default)
                .OrderBy(s => s)
                .Reverse() // newer, non-obsolete protocols first
                .ToList();
            protocols.Add(FtpSslProtocols.Default); // add Default as the last option, by default it would have been between Tls and Tls11

            _sslProtocolsDataSource = DataSourceBuilder<FtpSslProtocols>
                .WithId(s => Enum.GetName(s))
                .WithLabel(s => GetFtpSslProtocolLabel(s))
                .WithMultipleSelection(
                    selectionToValue: s =>
                    {
                        if (s.Count == 0)
                        {
                            return FtpSslProtocols.Auto;
                        }

                        return s.Aggregate((a, b) => a | b);
                    },
                    valueToSelection: s => Decompose(s).ToArray()
                    )
                .WithData(protocols)
                .Build();
        }

        private static void InitializeProxyModeDataSource()
        {
            _proxyTypeDataSource ??= ProxyTypeHelper.GetProxyTypeDataSource();   
        }

        private static List<FtpSslProtocols> Decompose(FtpSslProtocols value)
        {
            var bits = EnumExtensions<FtpSslProtocols>.Decompose(value);
            bits.Remove(FtpSslProtocols.Auto); // FtpSslProtocols.Auto is not displayed
            return bits;
        }

        // gets the value of a localized enum and appends Obsolete if necessary
        private static string GetFtpSslProtocolLabel(FtpSslProtocols value)
        {
            List<string> labels = new();
            foreach (var flag in Decompose(value))
            {
                labels.Add(GetFtpSslProtocolFlagLabel(flag));
            }

            return string.Join(" | ", labels);
        }

        /// <summary>
        /// Returns the localized name of an enum flag (i.e. a single enum value, not a combination).
        ///  Localized names of obsolete values are marked appropriately.
        /// </summary>
        /// <param name="flag"></param>
        /// <returns>The localized name</returns>
        private static string GetFtpSslProtocolFlagLabel(FtpSslProtocols flag)
        {
            var localizedName = LocalizedEnum.GetLocalizedValue(typeof(FtpSslProtocols), flag).Name;

            // get the attributes of the enum value
            var field = typeof(FtpSslProtocols).GetField(flag.ToString());
            if (field.GetCustomAttributesData().Any(a => a.AttributeType == typeof(ObsoleteAttribute)))
            {
                return string.Format(Resources.ObsoleteEnumValue, localizedName);
            }

            return localizedName;
        }

    }

    internal static class ProxyTypeHelper
    {
        internal static IDataSource GetProxyTypeDataSource()
        => DataSourceBuilder<FtpProxyType>
            .WithId(t => t.ToString())
            .WithLabel(GetLocalizedFtpObjectTypeDisplayName)
            .WithData(GetOrderedProxyTypeList())
            .Build();

        internal static IReadOnlyList<FtpProxyType> GetOrderedProxyTypeList()
        => new List<FtpProxyType>()
        {
            FtpProxyType.None,
            FtpProxyType.Socks4,
            FtpProxyType.Socks5,
            FtpProxyType.Http,
        };

        internal static string GetLocalizedFtpObjectTypeDisplayName(FtpProxyType type)
        => LocalizedEnum.GetLocalizedValue(typeof(FtpProxyType), type).Name;   
    }

}
