using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Threading.Tasks;
using UiPath.FTP.Activities.NetCore.ViewModels;
using System.Security;
using System;
using UiPath.FTP.Enums;

namespace UiPath.FTP.Activities
{
    /// <summary>
    /// A container which handles the connection to the FTP server and provides a scope for all the FTP activities.
    /// </summary>
    [ViewModelClass(typeof(WithFtpSessionViewModel))]
    public partial class WithFtpSession
    {
    }
}

namespace UiPath.FTP.Activities.NetCore.ViewModels
{
    public partial class WithFtpSessionViewModel : DesignPropertiesViewModel
    {
        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public WithFtpSessionViewModel(IDesignServices services) : base(services)
        {
        }

        /// <summary>
        /// The URL of the FTP server that you want to connect to.
        /// </summary>
        public DesignInArgument<string> Host { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The port of the FTP server that you want to connect to.
        /// </summary>
        public DesignInArgument<int> Port { get; set; } = new DesignInArgument<int>();

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
        /// Specifies if the automation should continue even when the activity throws an error.
        /// </summary>
        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            int propertyOrderIndex = 1;

            Host.OrderIndex = propertyOrderIndex++;
            Host.IsPrincipal = true;

            Username.OrderIndex = propertyOrderIndex++;
            Username.IsPrincipal = true;

            Password.OrderIndex = propertyOrderIndex++;
            Password.IsVisible = true;
            Password.IsPrincipal = true;

            SecurePassword.OrderIndex = propertyOrderIndex++;
            SecurePassword.IsVisible = false;
            SecurePassword.IsPrincipal = true;

            PasswordInputModeSwitch.IsVisible = false;

            Port.OrderIndex = propertyOrderIndex++;
            Port.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            UseAnonymousLogin.OrderIndex = propertyOrderIndex++;
            UseAnonymousLogin.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            ContinueOnError.OrderIndex = propertyOrderIndex++;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.NullableBoolean };

            AcceptAllCertificates.OrderIndex = propertyOrderIndex++;
            AcceptAllCertificates.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            ClientCertificatePath.OrderIndex = propertyOrderIndex++;

            ClientCertificatePassword.OrderIndex = propertyOrderIndex++;
            ClientCertificatePassword.IsPrincipal = false;
            ClientCertificatePassword.IsVisible = true;

            ClientCertificateSecurePassword.OrderIndex = propertyOrderIndex++;
            ClientCertificateSecurePassword.IsPrincipal = false;
            ClientCertificateSecurePassword.IsVisible = false;

            CertificatePasswordInputModeSwitch.IsVisible = false;

            FtpsMode.OrderIndex = propertyOrderIndex++;
            FtpsMode.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown };

            SslProtocols.OrderIndex = propertyOrderIndex++;
            SslProtocols.Widget = new DefaultWidget { Type = ViewModelWidgetType.MultiSelect };

            UseSftp.OrderIndex = propertyOrderIndex++;
            UseSftp.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            MenuActionsBuilder<PasswordInputMode>.WithValueProperty(PasswordInputModeSwitch)
              .AddMenuProperty(Password, PasswordInputMode.Password)
              .AddMenuProperty(SecurePassword, PasswordInputMode.SecurePassword)
              .BuildAndInsertMenuActions();

            MenuActionsBuilder<PasswordInputMode>.WithValueProperty(CertificatePasswordInputModeSwitch)
              .AddMenuProperty(ClientCertificatePassword, PasswordInputMode.Password)
              .AddMenuProperty(ClientCertificateSecurePassword, PasswordInputMode.SecurePassword)
              .BuildAndInsertMenuActions();
        }

        protected override async ValueTask InitializeModelAsync()
        {
            await base.InitializeModelAsync();
        }

        /// <inheritdoc/>
        protected override void InitializeRules()
        {
            base.InitializeRules();
            Rule(nameof(PasswordInputModeSwitch), PasswordInputModeChanged_Action);
            Rule(nameof(CertificatePasswordInputModeSwitch), CertificatePasswordInputModeChanged_Action);
        }

        /// <inheritdoc/>
        protected override void ManualRegisterDependencies()
        {
            base.ManualRegisterDependencies();
            RegisterDependency(PasswordInputModeSwitch, nameof(PasswordInputModeSwitch.Value), nameof(PasswordInputModeSwitch));
            RegisterDependency(CertificatePasswordInputModeSwitch, nameof(CertificatePasswordInputModeSwitch.Value), nameof(CertificatePasswordInputModeSwitch));
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
    }
}
