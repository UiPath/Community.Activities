using System;
using System.Activities;
using System.Activities.Validation;
using System.ComponentModel;
using System.Data;
using System.Security;
using UiPath.Database.Activities.Properties;
using UiPath.Shared.Activities;

namespace UiPath.Database.Activities
{
    public abstract class DatabaseRowActivity : AsyncTaskCodeActivity
    {
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseRowActivity_Property_ProviderName_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseRowActivity_Property_ProviderName_Description))]
        public InArgument<string> ProviderName { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseRowActivity_Property_ConnectionString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseRowActivity_Property_ConnectionString_Description))]
        public InArgument<string> ConnectionString { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseRowActivity_Property_ConnectionSecureString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseRowActivity_Property_ConnectionSecureString_Description))]
        public InArgument<SecureString> ConnectionSecureString { get; set; }

        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseRowActivity_Property_ExistingDbConnection_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseRowActivity_Property_ExistingDbConnection_Description))]
        public InArgument<DatabaseConnection> ExistingDbConnection { get; set; }

        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseRowActivity_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseRowActivity_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        protected static void HandleException(Exception ex, bool continueOnError)
        {
            if (continueOnError) return;
            throw ex;
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

            if (!ExistingDbConnection.IsEmpty())
            {
                return;
            }
            if (ConnectionSecureString.IsEmpty() && ConnectionString.IsEmpty())
            {
                var validationError = new ValidationError(Resources.ValidationError_ConnectionMustNotBeNull, false);
                metadata.AddValidationError(validationError);
                return;
            }
            if (!ConnectionSecureString.IsEmpty() && !ConnectionString.IsEmpty())
            {
                var validationError = new ValidationError(Resources.ValidationError_ConnectionMustBeSet, false);
                metadata.AddValidationError(validationError);
                return;
            }
            if (ProviderName.IsEmpty())
            {
                var validationError = new ValidationError(Resources.ValidationError_ProviderNull, false, nameof(ProviderName));
                metadata.AddValidationError(validationError);
            }
        }
    }
}
