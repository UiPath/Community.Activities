using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Threading.Tasks;
using UiPath.FTP.Activities.NetCore.ViewModels;

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

            Username.OrderIndex = propertyOrderIndex++;
            Username.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            Password.OrderIndex = propertyOrderIndex++;
            Password.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            UseAnonymousLogin.OrderIndex = propertyOrderIndex++;
            UseAnonymousLogin.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            AcceptAllCertificates.OrderIndex = propertyOrderIndex++;
            AcceptAllCertificates.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            ClientCertificatePassword.OrderIndex = propertyOrderIndex++;
            ClientCertificatePassword.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            ClientCertificatePath.OrderIndex = propertyOrderIndex++;
            ClientCertificatePath.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            FtpsMode.OrderIndex = propertyOrderIndex++;
            FtpsMode.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown };

            SslProtocols.OrderIndex = propertyOrderIndex++;
            SslProtocols.Widget = new DefaultWidget { Type = ViewModelWidgetType.MultiSelect };

            UseSftp.OrderIndex = propertyOrderIndex++;
            UseSftp.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            Host.IsPrincipal = false;
            Host.OrderIndex = propertyOrderIndex++;
            Host.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            Port.OrderIndex = propertyOrderIndex++;
            Port.Widget = new DefaultWidget { Type = ViewModelWidgetType.Input };

            ContinueOnError.OrderIndex = propertyOrderIndex++;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.NullableBoolean };
        }

        protected override async ValueTask InitializeModelAsync()
        {
            await base.InitializeModelAsync();
        }

        protected override void InitializeRules()
        {
            base.InitializeRules();
        }
    }
}
