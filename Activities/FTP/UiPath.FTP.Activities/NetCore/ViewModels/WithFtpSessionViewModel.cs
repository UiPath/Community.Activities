using System;
using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using UiPath.FTP.Activities.Properties;
using UiPath.FTP.Enums;
using UiPath.Shared;
using UiPath.Shared.Activities;
using FtpsModeEnum = UiPath.FTP.FtpsMode;

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
            InitializeSslProtocolsDataSource();
            InitializeFtpsModeDataSource();
            InitializeProxyModeDataSource();
        }

        /// <summary>
        /// The child activities that run against the open session. It is rendered as the scope's
        /// drop area on the canvas, never as a property row — see <see cref="InitializeModel"/>.
        /// </summary>
        public DesignProperty<ActivityAction<IFtpSession>> Body { get; set; } = new DesignProperty<ActivityAction<IFtpSession>>();

        /// <summary>
        /// The URL of the FTP server that you want to connect to.
        /// </summary>
        public DesignInArgument<string> Host { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The port of the FTP server that you want to connect to.
        /// </summary>
        public DesignInArgument<int> Port { get; set; } = new DesignInArgument<int>();

        /// <summary>
        /// The connection timeout in milliseconds.
        /// </summary>
        public DesignInArgument<int> Timeout { get; set; } = new DesignInArgument<int>();

        /// <summary>
        /// The username that will be used to connect to the FTP server.
        /// </summary>
        public DesignInArgument<string> Username { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The password that will be used to connect to the FTP server.
        /// </summary>
        public DesignInArgument<string> Password { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The secure password that will be used to connect to the FTP server.
        /// </summary>
        public DesignInArgument<SecureString> SecurePassword { get; set; } = new DesignInArgument<SecureString>();

        /// <summary>
        /// Switches Password as string or secure string
        /// </summary>
        public DesignProperty<PasswordInputMode> PasswordInputModeSwitch { get; set; } = new DesignProperty<PasswordInputMode>();

        /// <summary>
        /// When this box is checked, the username and password fields are ignored, and a standard anonymous user is used instead.
        /// </summary>
        public DesignProperty<bool> UseAnonymousLogin { get; set; } = new DesignProperty<bool>();

        /// <summary>
        /// Switches to the FTPS protocol.
        /// </summary>
        public DesignProperty<FtpsMode> FtpsMode { get; set; } = new DesignProperty<FtpsMode>();

        /// <summary>
        /// Select the SSL protocol to be used for the FTPS connection
        /// </summary>
        public DesignProperty<FtpSslProtocols> SslProtocols { get; set; } = new DesignProperty<FtpSslProtocols>();

        /// <summary>
        /// Check this box if you want to use the SFTP transfer protocol.
        /// </summary>
        public DesignProperty<bool> UseSftp { get; set; } = new DesignProperty<bool>();

        /// <summary>
        /// The path to the certificate used to verify the identity of the client.
        /// </summary>
        public DesignInArgument<string> ClientCertificatePath { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The password for the client certificate.
        /// </summary>
        public DesignInArgument<string> ClientCertificatePassword { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The secure password that will be used to connect to the FTP server.
        /// </summary>
        public DesignInArgument<SecureString> ClientCertificateSecurePassword { get; set; } = new DesignInArgument<SecureString>();

        /// <summary>
        /// Switches Password as string or secure string
        /// </summary>
        public DesignProperty<PasswordInputMode> CertificatePasswordInputModeSwitch { get; set; } = new DesignProperty<PasswordInputMode>();

        /// <summary>
        /// If this box is checked, all certificates will be accepted, including the ones that are expired or not verified.
        /// </summary>
        public DesignProperty<bool> AcceptAllCertificates { get; set; } = new DesignProperty<bool>();

        /// <summary>
        /// The type of proxy used
        /// </summary>
        public DesignProperty<FtpProxyType> ProxyType { get; set; } = new DesignProperty<FtpProxyType>();

        /// <summary>
        /// The proxy host
        /// </summary>
        public DesignInArgument<string> ProxyServer { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The proxy port
        /// </summary>
        public DesignInArgument<int> ProxyPort { get; set; } = new DesignInArgument<int>();

        /// <summary>
        /// User used for proxy authentification
        /// </summary>
        public DesignInArgument<string> ProxyUser { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// Password for proxy
        /// </summary>
        public DesignInArgument<string> ProxyPassword { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// Secured password for proxy
        /// </summary>
        public DesignInArgument<SecureString> ProxySecurePassword { get; set; } = new DesignInArgument<SecureString>();

        /// <summary>
        /// Switches Proxy Password as string or secure string
        /// </summary>
        public DesignProperty<PasswordInputMode> ProxyPasswordInputModeSwitch { get; set; } = new DesignProperty<PasswordInputMode>();

        private static DataSource<FtpSslProtocols> _sslProtocolsDataSource;

        private static IDataSource _proxyTypeDataSource;

        private static IDataSource _ftpsModeDataSource;

        protected override void InitializeModel()
        {
            base.InitializeModel();

            int orderIndex = 1;

            // The scope renders its children as a drop area, not as a property row.
            Body.IsVisible = false;

            ConfigureConnectionProperties(ref orderIndex);
            ConfigureOptionProperties(ref orderIndex);
            ConfigureSecurityProperties(ref orderIndex);
            ConfigureProxyProperties(ref orderIndex);

            // the input-mode switches only back the menu actions below, they are never rendered
            PasswordInputModeSwitch.IsVisible = false;
            CertificatePasswordInputModeSwitch.IsVisible = false;
            ProxyPasswordInputModeSwitch.IsVisible = false;

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

        /// <summary>
        /// The three fields needed for every connection: server, user and password.
        /// They form the primary section and are the only principal properties of the activity.
        /// </summary>
        private void ConfigureConnectionProperties(ref int orderIndex)
        {
            Host.DisplayName = Resources.Activity_WithFtpSession_Property_Host_Name;
            Host.Tooltip = Resources.Activity_WithFtpSession_Property_Host_Description;
            Host.EditPlaceholder = Resources.Activity_WithFtpSession_Property_Host_Placeholder;
            Host.IsRequired = true;
            Host.IsPrincipal = true;
            Host.OrderIndex = orderIndex++;
            Host.Category = Resources.Input;

            Username.DisplayName = Resources.Activity_WithFtpSession_Property_Username_Name;
            Username.Tooltip = Resources.Activity_WithFtpSession_Property_Username_Description;
            Username.IsPrincipal = true;
            Username.OrderIndex = orderIndex++;
            Username.Category = Resources.Input;

            // Password and SecurePassword are two renderings of the same field, only one is ever
            // visible, so they share an order index.
            Password.DisplayName = Resources.Activity_WithFtpSession_Property_Password_Name;
            Password.Tooltip = Resources.Activity_WithFtpSession_Property_Password_Description;
            Password.IsPrincipal = true;
            Password.OrderIndex = orderIndex;
            Password.Category = Resources.Input;

            SecurePassword.DisplayName = Resources.Activity_WithFtpSession_Property_SecurePassword_Name;
            SecurePassword.Tooltip = Resources.Activity_WithFtpSession_Property_SecurePassword_Description;
            SecurePassword.IsPrincipal = true;
            SecurePassword.IsVisible = false;
            SecurePassword.OrderIndex = orderIndex++;
            SecurePassword.Category = Resources.Input;
        }

        /// <summary>
        /// Connection knobs that have a working default: anonymous login, port, timeout and
        /// ContinueOnError. None of them belong in the primary section.
        /// </summary>
        private void ConfigureOptionProperties(ref int orderIndex)
        {
            UseAnonymousLogin.DisplayName = Resources.Activity_WithFtpSession_Property_UseAnonymousLogin_Name;
            UseAnonymousLogin.Tooltip = Resources.Activity_WithFtpSession_Property_UseAnonymousLogin_Description;
            UseAnonymousLogin.IsPrincipal = false;
            UseAnonymousLogin.OrderIndex = orderIndex++;
            UseAnonymousLogin.Category = Resources.Options;
            UseAnonymousLogin.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            Port.DisplayName = Resources.Activity_WithFtpSession_Property_Port_Name;
            Port.Tooltip = Resources.Activity_WithFtpSession_Property_Port_Description;
            Port.EditPlaceholder = Resources.Activity_WithFtpSession_Property_Port_Placeholder;
            Port.IsPrincipal = false;
            Port.OrderIndex = orderIndex++;
            Port.Category = Resources.Options;
            Port.Widget = new DefaultWidget { Type = ViewModelWidgetType.Number };

            Timeout.DisplayName = Resources.Activity_WithFtpSession_Property_Timeout_Name;
            Timeout.Tooltip = Resources.Activity_WithFtpSession_Property_Timeout_Description;
            Timeout.EditPlaceholder = Resources.Activity_WithFtpSession_Property_Timeout_Placeholder;
            Timeout.IsPrincipal = false;
            Timeout.OrderIndex = orderIndex++;
            Timeout.Category = Resources.Options;
            Timeout.Widget = new DefaultWidget { Type = ViewModelWidgetType.Number };

            ConfigureContinueOnError(ref orderIndex);
        }

        /// <summary>
        /// Transport and certificate configuration. UseSftp comes first because it decides which of
        /// the fields below apply.
        /// </summary>
        private void ConfigureSecurityProperties(ref int orderIndex)
        {
            UseSftp.DisplayName = Resources.Activity_WithFtpSession_Property_UseSftp_Name;
            UseSftp.Tooltip = Resources.Activity_WithFtpSession_Property_UseSftp_Description;
            UseSftp.IsPrincipal = false;
            UseSftp.OrderIndex = orderIndex++;
            UseSftp.Category = Resources.Security;
            UseSftp.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            FtpsMode.DisplayName = Resources.Activity_WithFtpSession_Property_FtpsMode_Name;
            FtpsMode.Tooltip = Resources.Activity_WithFtpSession_Property_FtpsMode_Description;
            FtpsMode.IsPrincipal = false;
            FtpsMode.OrderIndex = orderIndex++;
            FtpsMode.Category = Resources.Security;
            FtpsMode.DataSource = _ftpsModeDataSource;
            FtpsMode.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown };

            SslProtocols.DisplayName = Resources.Activity_WithFtpSession_Property_SslProtocols_Name;
            SslProtocols.Tooltip = Resources.Activity_WithFtpSession_Property_SslProtocols_Description;
            SslProtocols.EditPlaceholder = Resources.Activity_WithFtpSession_Property_SslProtocols_Placeholder;
            SslProtocols.IsPrincipal = false;
            SslProtocols.OrderIndex = orderIndex++;
            SslProtocols.Category = Resources.Security;
            SslProtocols.DataSource = _sslProtocolsDataSource;
            SslProtocols.Widget = new DefaultWidget { Type = ViewModelWidgetType.MultiSelect };

            ClientCertificatePath.DisplayName = Resources.Activity_WithFtpSession_Property_ClientCertificatePath_Name;
            ClientCertificatePath.Tooltip = Resources.Activity_WithFtpSession_Property_ClientCertificatePath_Description;
            ClientCertificatePath.EditPlaceholder = Resources.Activity_WithFtpSession_Property_ClientCertificatePath_Placeholder;
            ClientCertificatePath.IsPrincipal = false;
            ClientCertificatePath.OrderIndex = orderIndex++;
            ClientCertificatePath.Category = Resources.Security;

            ClientCertificatePassword.DisplayName = Resources.Activity_WithFtpSession_Property_ClientCertificatePassword_Name;
            ClientCertificatePassword.Tooltip = Resources.Activity_WithFtpSession_Property_ClientCertificatePassword_Description;
            ClientCertificatePassword.IsPrincipal = false;
            ClientCertificatePassword.OrderIndex = orderIndex;
            ClientCertificatePassword.Category = Resources.Security;

            ClientCertificateSecurePassword.DisplayName = Resources.Activity_WithFtpSession_Property_ClientCertificateSecurePassword_Name;
            ClientCertificateSecurePassword.Tooltip = Resources.Activity_WithFtpSession_Property_ClientCertificateSecurePassword_Description;
            ClientCertificateSecurePassword.IsPrincipal = false;
            ClientCertificateSecurePassword.IsVisible = false;
            ClientCertificateSecurePassword.OrderIndex = orderIndex++;
            ClientCertificateSecurePassword.Category = Resources.Security;

            AcceptAllCertificates.DisplayName = Resources.Activity_WithFtpSession_Property_AcceptAllCertificates_Name;
            AcceptAllCertificates.Tooltip = Resources.Activity_WithFtpSession_Property_AcceptAllCertificates_Description;
            AcceptAllCertificates.IsPrincipal = false;
            AcceptAllCertificates.OrderIndex = orderIndex++;
            AcceptAllCertificates.Category = Resources.Security;
            AcceptAllCertificates.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };
        }

        /// <summary>
        /// Proxy configuration. Everything below ProxyType is revealed by
        /// <see cref="ProxyModeChanged_Action"/> once a proxy type is picked.
        /// </summary>
        private void ConfigureProxyProperties(ref int orderIndex)
        {
            ProxyType.DisplayName = Resources.Activity_WithFtpSession_Property_ProxyType_Name;
            ProxyType.Tooltip = Resources.Activity_WithFtpSession_Property_ProxyType_Description;
            ProxyType.IsPrincipal = false;
            ProxyType.OrderIndex = orderIndex++;
            ProxyType.Category = Resources.Proxy;
            ProxyType.DataSource = _proxyTypeDataSource;
            ProxyType.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown };

            ProxyServer.DisplayName = Resources.Activity_WithFtpSession_Property_ProxyServer_Name;
            ProxyServer.Tooltip = Resources.Activity_WithFtpSession_Property_ProxyServer_Description;
            ProxyServer.EditPlaceholder = Resources.Activity_WithFtpSession_Property_ProxyServer_Placeholder;
            ProxyServer.IsPrincipal = false;
            ProxyServer.OrderIndex = orderIndex++;
            ProxyServer.Category = Resources.Proxy;

            ProxyPort.DisplayName = Resources.Activity_WithFtpSession_Property_ProxyPort_Name;
            ProxyPort.Tooltip = Resources.Activity_WithFtpSession_Property_ProxyPort_Description;
            ProxyPort.IsPrincipal = false;
            ProxyPort.OrderIndex = orderIndex++;
            ProxyPort.Category = Resources.Proxy;
            ProxyPort.Widget = new DefaultWidget { Type = ViewModelWidgetType.Number };

            ProxyUser.DisplayName = Resources.Activity_WithFtpSession_Property_ProxyUser_Name;
            ProxyUser.Tooltip = Resources.Activity_WithFtpSession_Property_ProxyUser_Description;
            ProxyUser.IsPrincipal = false;
            ProxyUser.OrderIndex = orderIndex++;
            ProxyUser.Category = Resources.Proxy;

            ProxyPassword.DisplayName = Resources.Activity_WithFtpSession_Property_ProxyPassword_Name;
            ProxyPassword.Tooltip = Resources.Activity_WithFtpSession_Property_ProxyPassword_Description;
            ProxyPassword.IsPrincipal = false;
            ProxyPassword.OrderIndex = orderIndex;
            ProxyPassword.Category = Resources.Proxy;

            ProxySecurePassword.DisplayName = Resources.Activity_WithFtpSession_Property_ProxySecurePassword_Name;
            ProxySecurePassword.Tooltip = Resources.Activity_WithFtpSession_Property_ProxySecurePassword_Description;
            ProxySecurePassword.IsPrincipal = false;
            ProxySecurePassword.IsVisible = false;
            ProxySecurePassword.OrderIndex = orderIndex++;
            ProxySecurePassword.Category = Resources.Proxy;
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
        internal void PasswordInputModeChanged_Action()
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
        internal void CertificatePasswordInputModeChanged_Action()
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
        internal void ProxyPasswordInputModeChanged_Action()
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

        internal void FtpEncryptionModeChanged_Action()
        {
            if (FtpsMode.Value == FTP.FtpsMode.None)
            {
                SslProtocols.IsVisible = false;
                return;
            }

            SslProtocols.IsVisible = true;
        }

        internal void ProxyModeChanged_Action()
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
            _ftpsModeDataSource ??= DataSourceBuilder<FtpsModeEnum>
                .WithId(m => Enum.GetName(m))
                .WithLabel(m => LocalizedEnum.GetLocalizedValue(typeof(FtpsModeEnum), m).Name)
                .WithData(new List<FtpsModeEnum>
                {
                    FtpsModeEnum.None,
                    FtpsModeEnum.Explicit,
                    FtpsModeEnum.Implicit,
                })
                .Build();
        }

        private static void InitializeSslProtocolsDataSource()
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
