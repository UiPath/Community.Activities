using Microsoft.VisualBasic.Activities;
using System;
using System.Activities;
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
    [LocalizedDisplayName(nameof(Resources.KeyedHashFileDisplayName))]
    [LocalizedDescription(nameof(Resources.KeyedHashFileDescription))]
    public class KeyedHashFile : CodeActivity<string>
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.AlgorithmDisplayName))]
        [LocalizedDescription(nameof(Resources.KeyedHashAlgorithmDescription))]
        public KeyedHashAlgorithms Algorithm { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.FilePathDisplayName))]
        [LocalizedDescription(nameof(Resources.HashFilePathDescription))]
        public InArgument<string> FilePath { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.KeyDisplayName))]
        [LocalizedDescription(nameof(Resources.KeyedHashFileKeyDescription))]
        public InArgument<string> Key { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.KeySecureStringDisplayName))]
        [LocalizedDescription(nameof(Resources.KeyedHashFileKeySecureStringDescription))]
        public InArgument<SecureString> KeySecureString { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.EncodingDisplayName))]
        [LocalizedDescription(nameof(Resources.KeyedHashFileEncodingDescription))]
        public InArgument<Encoding> Encoding { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.ResultDisplayName))]
        [LocalizedDescription(nameof(Resources.KeyedHashFileResultDescription))]
        public new OutArgument<string> Result { get => base.Result; set => base.Result = value; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnErrorDisplayName))]
        public InArgument<bool> ContinueOnError { get; set; }

        public KeyedHashFile()
        {
            Algorithm = KeyedHashAlgorithms.HMACSHA256;
            Encoding = new VisualBasicValue<Encoding>(typeof(Encoding).FullName + "." + nameof(System.Text.Encoding.UTF8)); // Kinda ugly.
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
                Encoding encoding = Encoding.Get(context);

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
                if (encoding == null)
                {
                    throw new ArgumentNullException(Resources.Encoding);
                }
                if (!File.Exists(filePath))
                {
                    throw new ArgumentException(Resources.FileDoesNotExistsException, Resources.FilePathDisplayName);
                }


                byte[] hashed = CryptographyHelper.HashDataWithKey(Algorithm, File.ReadAllBytes(filePath), key != null ? encoding.GetBytes(key) : encoding.GetBytes(new NetworkCredential("", keySecureString).Password));

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