using System;
using System.Activities;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security;
using UiPath.Cryptography.Activities.Models;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Platform.ResourceHandling;
using UiPath.Shared.Activities;
#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif

namespace UiPath.Cryptography.Activities
{
    [LocalizedDisplayName(nameof(Resources.Activity_PgpGenerateKeyPair_Name))]
    [LocalizedDescription(nameof(Resources.Activity_PgpGenerateKeyPair_Description))]
    public partial class PgpGenerateKeyPair : CodeActivity
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpGenerateKeyPair_Property_PublicKeyFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpGenerateKeyPair_Property_PublicKeyFilePath_Description))]
        public InArgument<string> PublicKeyFilePath { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpGenerateKeyPair_Property_PrivateKeyFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpGenerateKeyPair_Property_PrivateKeyFilePath_Description))]
        public InArgument<string> PrivateKeyFilePath { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpGenerateKeyPair_Property_Username_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpGenerateKeyPair_Property_Username_Description))]
        public InArgument<string> Username { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpGenerateKeyPair_Property_Password_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpGenerateKeyPair_Property_Password_Description))]
        public InArgument<SecureString> Password { get; set; }

        [DefaultValue(false)]
        [LocalizedCategory(nameof(Resources.Category_Options_Name))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpGenerateKeyPair_Property_Overwrite_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpGenerateKeyPair_Property_Overwrite_Description))]
        public bool Overwrite { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpGenerateKeyPair_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpGenerateKeyPair_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        [Browsable(false)]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpGenerateKeyPair_Property_PublicKeyFile_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpGenerateKeyPair_Property_PublicKeyFile_Description))]
        public OutArgument<ILocalResource> PublicKeyFile { get; set; }

        [Browsable(false)]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpGenerateKeyPair_Property_PrivateKeyFile_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpGenerateKeyPair_Property_PrivateKeyFile_Description))]
        public OutArgument<ILocalResource> PrivateKeyFile { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
#if ENABLE_DEFAULT_TELEMETRY
            var telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
#endif

            try
            {
                var publicKeyFilePath = PublicKeyFilePath.Get(context);
                var privateKeyFilePath = PrivateKeyFilePath.Get(context);
                var username = Username.Get(context);
                var password = Password.Get(context);

                if (string.IsNullOrWhiteSpace(publicKeyFilePath))
                    throw new ArgumentNullException(Resources.Activity_PgpGenerateKeyPair_Property_PublicKeyFilePath_Name);
                if (string.IsNullOrWhiteSpace(privateKeyFilePath))
                    throw new ArgumentNullException(Resources.Activity_PgpGenerateKeyPair_Property_PrivateKeyFilePath_Name);
                if (string.IsNullOrWhiteSpace(username))
                    throw new ArgumentNullException(Resources.Activity_PgpGenerateKeyPair_Property_Username_Name);
                if (password == null || password.Length == 0)
                    throw new ArgumentNullException(Resources.Activity_PgpGenerateKeyPair_Property_Password_Name);

                if (!Overwrite)
                {
                    if (File.Exists(publicKeyFilePath))
                        throw new ArgumentException(Resources.FileAlreadyExistsException, Resources.Activity_PgpGenerateKeyPair_Property_PublicKeyFilePath_Name);
                    if (File.Exists(privateKeyFilePath))
                        throw new ArgumentException(Resources.FileAlreadyExistsException, Resources.Activity_PgpGenerateKeyPair_Property_PrivateKeyFilePath_Name);
                }

                var passwordString = new NetworkCredential("", password).Password;

                var pubDir = Path.GetDirectoryName(publicKeyFilePath);
                if (!string.IsNullOrEmpty(pubDir))
                    Directory.CreateDirectory(pubDir);

                var privDir = Path.GetDirectoryName(privateKeyFilePath);
                if (!string.IsNullOrEmpty(privDir))
                    Directory.CreateDirectory(privDir);

                CryptographyHelper.PgpGenerateKeyPair(publicKeyFilePath, privateKeyFilePath, username, passwordString);

                var pubItem = new CryptographyLocalItem(Path.GetFileName(publicKeyFilePath), publicKeyFilePath);
                PublicKeyFile.Set(context, pubItem);

                var privItem = new CryptographyLocalItem(Path.GetFileName(privateKeyFilePath), privateKeyFilePath);
                PrivateKeyFile.Set(context, privItem);

#if ENABLE_DEFAULT_TELEMETRY
                telemetryOperation.Send();
#endif
            }
            catch (Exception ex)
            {
#if ENABLE_DEFAULT_TELEMETRY
                telemetryOperation.SendWithException(ex);
#endif
                Trace.TraceError(ex.ToString());
                if (!ContinueOnError.Get(context)) throw;
            }
        }
    }
}
