using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Validation;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security;
using System.Text;
using UiPath.Cryptography.Activities.Properties;

namespace UiPath.Cryptography.Activities
{
    [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashFile_Name))]
    [LocalizedDescription(nameof(Resources.Activity_KeyedHashFile_Description))]
    public partial class KeyedHashFile : CodeActivity<string>
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashFile_Property_Algorithm_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashFile_Property_Algorithm_Description))]
        public KeyedHashAlgorithms Algorithm { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashFile_Property_FilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashFile_Property_FilePath_Description))]
        public InArgument<string> FilePath { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashFile_Property_Key_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashFile_Property_Key_Description))]
        public InArgument<string> Key { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashFile_Property_KeySecureString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashFile_Property_KeySecureString_Description))]
        public InArgument<SecureString> KeySecureString { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashFile_Property_Encoding_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashFile_Property_Encoding_Description))]
        public InArgument<Encoding> Encoding { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashFile_Property_Result_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashFile_Property_Result_Description))]
        public new OutArgument<string> Result { get => base.Result; set => base.Result = value; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashFile_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashFile_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        public KeyedHashFile()
        {
            Algorithm = KeyedHashAlgorithms.HMACSHA256;
            Encoding = new InArgument<Encoding>(ExpressionServices.Convert((env) => System.Text.Encoding.UTF8));
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

            if (!CryptographyHelper.IsFipsCompliant(Algorithm))
            {
                ValidationError error = new ValidationError(Resources.FipsComplianceWarning, true, nameof(Algorithm));
                metadata.AddValidationError(error);
            }
#if NET461
            if (Algorithm == KeyedHashAlgorithms.MACTripleDES)
            {
                ValidationError keySizeWarning = new ValidationError(Resources.MacTripleDesKeySizeWarning, true, nameof(Algorithm));
                metadata.AddValidationError(keySizeWarning);
            }
#endif
        }

        protected override string Execute(CodeActivityContext context)
        {
            string result = null;

            try
            {
                string filePath = FilePath.Get(context);
                string key = Key.Get(context);
                SecureString keySecureString = KeySecureString.Get(context);
                Encoding keyEncoding = Encoding.Get(context);

                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentNullException(Resources.FilePathDisplayName);
                }
                if (string.IsNullOrWhiteSpace(key) && keySecureString == null)
                {
                    throw new ArgumentNullException(Resources.KeyAndSecureStringNull);
                }
                if (key != null && keySecureString != null)
                {
                    throw new ArgumentNullException(Resources.KeyAndSecureStringNotNull);
                }
                if (keyEncoding == null)
                {
                    throw new ArgumentNullException(Resources.Encoding);
                }
                if (!File.Exists(filePath))
                {
                    throw new ArgumentException(Resources.FileDoesNotExistsException, Resources.FilePathDisplayName);
                }


                byte[] hashed = CryptographyHelper.HashDataWithKey(Algorithm, File.ReadAllBytes(filePath), CryptographyHelper.KeyEncoding(keyEncoding, key, keySecureString));

                result = BitConverter.ToString(hashed).Replace("-", string.Empty);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());

                if (!ContinueOnError.Get(context))
                {
                    throw;
                }
            }

            return result;
        }
    }
}