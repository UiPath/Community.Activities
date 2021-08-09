using System;
using System.Activities;
using System.Activities.Statements;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using UiPath.FTP.Activities.Properties;
using UiPath.Shared.Activities;

namespace UiPath.FTP.Activities
{
    [LocalizedDisplayName(nameof(Resources.WithFtpSessionDisplayName))]
    [LocalizedDescription(nameof(Resources.WithFtpSessionDescription))]
    public class WithFtpSession : ContinuableAsyncNativeActivity
    {
        private IFtpSession _ftpSession;

        public static readonly string FtpSessionPropertyName = "FtpSession";

        [Browsable(false)]
        public ActivityAction<IFtpSession> Body { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Server))]
        [LocalizedDisplayName(nameof(Resources.Host))]
        public InArgument<string> Host { get; set; }

        [LocalizedCategory(nameof(Resources.Server))]
        [LocalizedDisplayName(nameof(Resources.Port))]
        public InArgument<int> Port { get; set; }

        [LocalizedCategory(nameof(Resources.Credentials))]
        [LocalizedDisplayName(nameof(Resources.Username))]
        public InArgument<string> Username { get; set; }

        [LocalizedCategory(nameof(Resources.Credentials))]
        [LocalizedDisplayName(nameof(Resources.Password))]
        public InArgument<string> Password { get; set; }

        [DefaultValue(false)]
        [LocalizedCategory(nameof(Resources.Credentials))]
        [LocalizedDisplayName(nameof(Resources.UseAnonymousLogin))]
        public bool UseAnonymousLogin { get; set; }

        [DefaultValue(FtpsMode.None)]
        [LocalizedCategory(nameof(Resources.Security))]
        [LocalizedDisplayName(nameof(Resources.FtpsMode))]
        public FtpsMode FtpsMode { get; set; }

        [DefaultValue(FtpSslProtocols.Default)]
        [LocalizedCategory(nameof(Resources.Security))]
        [LocalizedDisplayName(nameof(Resources.SslProtocols))]
        public FtpSslProtocols SslProtocols { get; set; }

        [DefaultValue(false)]
        [LocalizedCategory(nameof(Resources.Security))]
        [LocalizedDisplayName(nameof(Resources.UseSftp))]
        public bool UseSftp { get; set; }

        [LocalizedCategory(nameof(Resources.Security))]
        [LocalizedDisplayName(nameof(Resources.ClientCertificatePath))]
        [LocalizedDescription(nameof(Resources.ClientCertificatePathDesc))]
        public InArgument<string> ClientCertificatePath { get; set; }

        [LocalizedCategory(nameof(Resources.Security))]
        [LocalizedDisplayName(nameof(Resources.ClientCertificatePassword))]
        public InArgument<string> ClientCertificatePassword { get; set; }

        [DefaultValue(false)]
        [LocalizedCategory(nameof(Resources.Security))]
        [LocalizedDisplayName(nameof(Resources.AcceptAllCertificates))]
        public bool AcceptAllCertificates { get; set; }

        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnError))]
        public InArgument<bool> ContinueOnError { get; set; } = false;

        public WithFtpSession()
        {
            FtpsMode = FtpsMode.None;
            SslProtocols = FtpSslProtocols.Default;
            Body = new ActivityAction<IFtpSession>()
            {
                Argument = new DelegateInArgument<IFtpSession>(FtpSessionPropertyName),
                Handler = new Sequence() { DisplayName = Resources.DefaultBodyName }
            };
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
        }

        protected override async Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext context, CancellationToken cancellationToken)
        {
            IFtpSession ftpSession = null;
            FtpConfiguration ftpConfiguration = new FtpConfiguration(Host.Get(context));
            ftpConfiguration.Port = Port.Expression == null ? null : (int?)Port.Get(context);
            ftpConfiguration.UseAnonymousLogin = UseAnonymousLogin;
            ftpConfiguration.SslProtocols = SslProtocols;
            ftpConfiguration.ClientCertificatePath = ClientCertificatePath.Get(context);
            ftpConfiguration.ClientCertificatePassword = ClientCertificatePassword.Get(context);
            ftpConfiguration.AcceptAllCertificates = AcceptAllCertificates;

            if (ftpConfiguration.UseAnonymousLogin == false)
            {
                ftpConfiguration.Username = Username.Get(context);
                ftpConfiguration.Password = Password.Get(context);

                if (string.IsNullOrWhiteSpace(ftpConfiguration.Username))
                {
                    throw new ArgumentNullException(Resources.EmptyUsernameException);
                }

                if (string.IsNullOrWhiteSpace(ftpConfiguration.Password) && string.IsNullOrWhiteSpace(ftpConfiguration.ClientCertificatePath))
                {
                    throw new ArgumentNullException(Resources.NoValidAuthenticationMethod);
                }
            }

            if (UseSftp)
            {
                ftpSession = new SftpSession(ftpConfiguration);
            }
            else
            {
                ftpSession = new FtpSession(ftpConfiguration, FtpsMode);
            }

            await ftpSession.OpenAsync(cancellationToken);

            return (nativeActivityContext) =>
            {
                if (Body != null)
                {
                    _ftpSession = ftpSession;
                    nativeActivityContext.ScheduleAction(Body, ftpSession, OnCompleted, OnFaulted);
                }
            };
        }

        private void OnCompleted(NativeActivityContext context, ActivityInstance completedInstance)
        {
            if (_ftpSession == null)
            {
                throw new InvalidOperationException(Resources.FTPSessionNotFoundException);
            }

            _ftpSession.Close();
            _ftpSession.Dispose();
        }

        private void OnFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            PropertyDescriptor ftpSessionProperty = faultContext.DataContext.GetProperties()[WithFtpSession.FtpSessionPropertyName];
            IFtpSession ftpSession = ftpSessionProperty?.GetValue(faultContext.DataContext) as IFtpSession;

            ftpSession?.Close();
            ftpSession?.Dispose();
        }
    }
}
