using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Shared.Activities;
using UiPath.Database.Activities.Properties;
using System.Activities.Validation;

namespace UiPath.Database.Activities
{
    public abstract class DatabaseExecute : AsyncTaskCodeActivity
    {
        private const int _defaultTimeout = 30000;

        private Dictionary<string, Argument> parameters;

        protected DatabaseConnection DbConnection = null;

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseExecute_Property_ProviderName_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseExecute_Property_ProviderName_Description))]
        public InArgument<string> ProviderName { get; set; }

        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [DefaultValue(null)]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseExecute_Property_ConnectionString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseExecute_Property_ConnectionString_Description))]
        public InArgument<string> ConnectionString { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseExecute_Property_ConnectionSecureString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseExecute_Property_ConnectionSecureString_Description))]
        public InArgument<SecureString> ConnectionSecureString { get; set; }

        [LocalizedCategory(nameof(Resources.ConnectionConfiguration))]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseExecute_Property_ExistingDbConnection_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseExecute_Property_ExistingDbConnection_Description))]
        public InArgument<DatabaseConnection> ExistingDbConnection { get; set; }

        [DefaultValue(CommandType.Text)]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseExecute_Property_CommandType_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseExecute_Property_CommandType_Description))]
        public CommandType CommandType { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [Browsable(true)]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseExecute_Property_Parameters_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseExecute_Property_Parameters_Description))]
        public Dictionary<string, Argument> Parameters
        {
            get
            {
                if (this.parameters == null)
                {
                    this.parameters = new Dictionary<string, Argument>();
                }
                return this.parameters;
            }
            set
            {
                parameters = value;
            }
        }

        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseExecute_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseExecute_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.Activity_DatabaseExecute_Property_TimeoutMS_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DatabaseExecute_Property_TimeoutMS_Description))]
        public InArgument<int> TimeoutMS { get; set; }

        protected override Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            if(TimeoutMS.Expression is null)
            {
                TimeoutMS.Set(context, _defaultTimeout);
            }

            return ExecuteInternalAsync(context, cancellationToken);
        }
        protected abstract Task<Action<AsyncCodeActivityContext>> ExecuteInternalAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken);

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
            if(ProviderName.IsEmpty())
            {
                var validationError = new ValidationError(Resources.ValidationError_ProviderNull, false, nameof(ProviderName));
                metadata.AddValidationError(validationError);
            }
        }
    }
}
