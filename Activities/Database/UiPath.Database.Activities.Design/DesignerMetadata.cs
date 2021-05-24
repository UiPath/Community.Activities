using System.Activities;
using System.Activities.Presentation.Metadata;
using System.Activities.Presentation.PropertyEditing;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Markup;
using UiPath.Activities.Presentation.Editors;
using UiPath.Database.Activities.Design.Properties;

namespace UiPath.Database.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            var builder = new AttributeTableBuilder();

            builder.AddCustomAttributes(typeof(DatabaseConnect), new DesignerAttribute(typeof(ConnectDatabaseDesigner)));
            builder.AddCustomAttributes(typeof(DatabaseDisconnect), new DesignerAttribute(typeof(DisconnectDesigner)));
            builder.AddCustomAttributes(typeof(DatabaseTransaction), new DesignerAttribute(typeof(ConnectDatabaseDesigner)));
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), new DesignerAttribute(typeof(GenericDatabaseDesigner)));
            builder.AddCustomAttributes(typeof(ExecuteQuery), new DesignerAttribute(typeof(GenericDatabaseDesigner)));
            builder.AddCustomAttributes(typeof(InsertDataTable), new DesignerAttribute(typeof(InsertDataTableDesigner)));
            builder.AddCustomAttributes(typeof(BulkInsert), new DesignerAttribute(typeof(BulkInsertDesigner)));
            builder.AddCustomAttributes(typeof(BulkUpdate), new DesignerAttribute(typeof(BulkUpdateDesigner)));

            // Editors
            var EditorAttributeType = new EditorAttribute(typeof(ArgumentDictionaryEditor), typeof(DialogPropertyValueEditor));
            builder.AddCustomAttributes(typeof(ExecuteQuery), nameof(ExecuteQuery.Parameters), EditorAttributeType);
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), nameof(ExecuteNonQuery.Parameters), EditorAttributeType);

            // Content attribute
            var contentAttr = new ContentPropertyAttribute(nameof(ExecuteNonQuery.ExistingDbConnection));
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), contentAttr);
            builder.AddCustomAttributes(typeof(ExecuteQuery), contentAttr);
            builder.AddCustomAttributes(typeof(InsertDataTable), contentAttr);
            builder.AddCustomAttributes(typeof(BulkInsert), contentAttr);
            builder.AddCustomAttributes(typeof(BulkUpdate), contentAttr);

            // DisplayName attribute
            builder.AddCustomAttributes(typeof(DatabaseConnect), new DisplayNameAttribute(Resources.Connect));
            builder.AddCustomAttributes(typeof(DatabaseDisconnect), new DisplayNameAttribute(Resources.Disconnect));
            builder.AddCustomAttributes(typeof(DatabaseTransaction), new DisplayNameAttribute(Resources.StartTransaction));
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), new DisplayNameAttribute(Resources.ExecuteNonQuery));
            builder.AddCustomAttributes(typeof(ExecuteQuery), new DisplayNameAttribute(Resources.ExecuteQuery));
            builder.AddCustomAttributes(typeof(InsertDataTable), new DisplayNameAttribute(Resources.Insert));
            builder.AddCustomAttributes(typeof(BulkInsert), new DisplayNameAttribute(Resources.BulkInsert));
            builder.AddCustomAttributes(typeof(BulkUpdate), new DisplayNameAttribute(Resources.BulkUpdate));

            // Categories
            CategoryAttribute appIntegrationDatabaseCategoryAttribute =
                new CategoryAttribute($"{Resources.CategoryAppIntegration}.{Resources.CategoryDatabase}");
            builder.AddCustomAttributes(typeof(DatabaseConnect), appIntegrationDatabaseCategoryAttribute);
            builder.AddCustomAttributes(typeof(DatabaseDisconnect), appIntegrationDatabaseCategoryAttribute);
            builder.AddCustomAttributes(typeof(DatabaseTransaction), appIntegrationDatabaseCategoryAttribute);
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), appIntegrationDatabaseCategoryAttribute);
            builder.AddCustomAttributes(typeof(ExecuteQuery), appIntegrationDatabaseCategoryAttribute);
            builder.AddCustomAttributes(typeof(InsertDataTable), appIntegrationDatabaseCategoryAttribute);
            builder.AddCustomAttributes(typeof(BulkInsert), appIntegrationDatabaseCategoryAttribute);
            builder.AddCustomAttributes(typeof(BulkUpdate), appIntegrationDatabaseCategoryAttribute);

            AddDescription(builder);

            MetadataStore.AddAttributeTable(builder.CreateTable());
        }

        private static void AddDescription(AttributeTableBuilder builder)
        {
            // Activities
            builder.AddCustomAttributes(typeof(DatabaseConnect), new DescriptionAttribute(Resources.DatabaseConnectDescription));
            builder.AddCustomAttributes(typeof(DatabaseDisconnect), new DescriptionAttribute(Resources.DatabaseDisconnectDescription));
            builder.AddCustomAttributes(typeof(DatabaseTransaction), new DescriptionAttribute(Resources.DbTransactionDescription));
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), new DescriptionAttribute(Resources.ExecuteNonQueryDescription));
            builder.AddCustomAttributes(typeof(ExecuteQuery), new DescriptionAttribute(Resources.ExecuteQueryDescription));
            builder.AddCustomAttributes(typeof(InsertDataTable), new DescriptionAttribute(Resources.InsertDataTableDescription));
            builder.AddCustomAttributes(typeof(BulkInsert), new DescriptionAttribute(Resources.BulkInsertDescription));
            builder.AddCustomAttributes(typeof(BulkUpdate), new DescriptionAttribute(Resources.BulkUpdateDescription));

            // Properties and Descriptions
            var AffectedRecordsDescription = new DescriptionAttribute(Resources.AffectedRecordsDescription);
            var AffectedRecordsInsertDescription = new DescriptionAttribute(Resources.AffectedRecordsInsertDescription);
            var CommandTypeDescription = new DescriptionAttribute(Resources.CommandTypeDescription);
            var connectionString = new DescriptionAttribute(Resources.ConnectionStringDescription);
            var connectionSecureString = new DescriptionAttribute(Resources.ConnectionSecureStringDescription);
            var ContinueOnError = new DescriptionAttribute(Resources.ContinueOnError);
            var DatabaseConnectionDescription = new DescriptionAttribute(Resources.DatabaseConnectionDescription);
            var DataTableDescription = new DescriptionAttribute(Resources.DataTableDescription);
            var ExistingDbConnectionDescription = new DescriptionAttribute(Resources.ExistingDbConnectionDescription);
            var InserDataTableInputDescription = new DescriptionAttribute(Resources.InserDataTableInputDescription);
            var parameters = new DescriptionAttribute(Resources.ParametersDescription);
            var providerName = new DescriptionAttribute(Resources.ProviderNameDescription);
            var QueryTimeoutMSDescription = new DescriptionAttribute(Resources.QueryTimeoutMSDescription);
            var SqlDescription = new DescriptionAttribute(Resources.SqlDescription);
            var TableNameDescription = new DescriptionAttribute(Resources.TableNameDescription);
            var TimeoutMSDescription = new DescriptionAttribute(Resources.TimeoutMSDescription);
            var UseTransactionDescription = new DescriptionAttribute(Resources.UseTransactionDescription);

            builder.AddCustomAttributes(typeof(DatabaseTransaction), nameof(DatabaseTransaction.ConnectionString), connectionString);
            builder.AddCustomAttributes(typeof(DatabaseTransaction), nameof(DatabaseTransaction.ConnectionSecureString), connectionSecureString);
            builder.AddCustomAttributes(typeof(DatabaseTransaction), nameof(DatabaseTransaction.ContinueOnError), ContinueOnError);
            builder.AddCustomAttributes(typeof(DatabaseTransaction), nameof(DatabaseTransaction.DatabaseConnection), DatabaseConnectionDescription);
            builder.AddCustomAttributes(typeof(DatabaseTransaction), nameof(DatabaseTransaction.ExistingDbConnection), ExistingDbConnectionDescription);
            builder.AddCustomAttributes(typeof(DatabaseTransaction), nameof(DatabaseTransaction.ProviderName), providerName);
            builder.AddCustomAttributes(typeof(DatabaseTransaction), nameof(DatabaseTransaction.UseTransaction), UseTransactionDescription);

            builder.AddCustomAttributes(typeof(ExecuteQuery), nameof(ExecuteNonQuery.ConnectionString), connectionString);
            builder.AddCustomAttributes(typeof(ExecuteQuery), nameof(ExecuteNonQuery.ConnectionSecureString), connectionSecureString);
            builder.AddCustomAttributes(typeof(ExecuteQuery), nameof(ExecuteNonQuery.ProviderName), providerName);
            builder.AddCustomAttributes(typeof(ExecuteQuery), nameof(ExecuteQuery.CommandType), CommandTypeDescription);
            builder.AddCustomAttributes(typeof(ExecuteQuery), nameof(ExecuteQuery.ContinueOnError), ContinueOnError);
            builder.AddCustomAttributes(typeof(ExecuteQuery), nameof(ExecuteQuery.DataTable), DataTableDescription);
            builder.AddCustomAttributes(typeof(ExecuteQuery), nameof(ExecuteQuery.ExistingDbConnection), ExistingDbConnectionDescription);
            builder.AddCustomAttributes(typeof(ExecuteQuery), nameof(ExecuteQuery.Parameters), parameters);
            builder.AddCustomAttributes(typeof(ExecuteQuery), nameof(ExecuteQuery.Sql), SqlDescription);
            builder.AddCustomAttributes(typeof(ExecuteQuery), nameof(ExecuteQuery.TimeoutMS), QueryTimeoutMSDescription);

            builder.AddCustomAttributes(typeof(ExecuteNonQuery), nameof(ExecuteNonQuery.AffectedRecords), AffectedRecordsDescription);
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), nameof(ExecuteNonQuery.CommandType), CommandTypeDescription);
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), nameof(ExecuteNonQuery.ConnectionString), connectionString);
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), nameof(ExecuteNonQuery.ConnectionSecureString), connectionSecureString);
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), nameof(ExecuteNonQuery.ContinueOnError), ContinueOnError);
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), nameof(ExecuteNonQuery.ExistingDbConnection), ExistingDbConnectionDescription);
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), nameof(ExecuteNonQuery.Parameters), parameters);
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), nameof(ExecuteNonQuery.ProviderName), providerName);
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), nameof(ExecuteNonQuery.Sql), SqlDescription);
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), nameof(ExecuteNonQuery.TimeoutMS), TimeoutMSDescription);

            builder.AddCustomAttributes(typeof(InsertDataTable), nameof(InsertDataTable.AffectedRecords), AffectedRecordsInsertDescription);
            builder.AddCustomAttributes(typeof(InsertDataTable), nameof(InsertDataTable.ConnectionString), connectionString);
            builder.AddCustomAttributes(typeof(InsertDataTable), nameof(InsertDataTable.ConnectionSecureString), connectionSecureString);
            builder.AddCustomAttributes(typeof(InsertDataTable), nameof(InsertDataTable.ContinueOnError), ContinueOnError);
            builder.AddCustomAttributes(typeof(InsertDataTable), nameof(InsertDataTable.DataTable), InserDataTableInputDescription);
            builder.AddCustomAttributes(typeof(InsertDataTable), nameof(InsertDataTable.ExistingDbConnection), ExistingDbConnectionDescription);
            builder.AddCustomAttributes(typeof(InsertDataTable), nameof(InsertDataTable.ProviderName), providerName);
            builder.AddCustomAttributes(typeof(InsertDataTable), nameof(InsertDataTable.TableName), TableNameDescription);

            builder.AddCustomAttributes(typeof(DatabaseConnect), nameof(DatabaseConnect.ConnectionString), connectionString);
            builder.AddCustomAttributes(typeof(DatabaseConnect), nameof(DatabaseConnect.ConnectionSecureString), connectionSecureString);
            builder.AddCustomAttributes(typeof(DatabaseConnect), nameof(DatabaseConnect.DatabaseConnection), DatabaseConnectionDescription);
            builder.AddCustomAttributes(typeof(DatabaseConnect), nameof(DatabaseConnect.ProviderName), providerName);

            builder.AddCustomAttributes(typeof(DatabaseDisconnect), nameof(DatabaseConnect.DatabaseConnection), DatabaseConnectionDescription);

            AddToAll(builder, typeof(DatabaseConnect).Assembly, nameof(Activity.DisplayName), new DisplayNameAttribute(Resources.DisplayName));
        }

        private static void AddToAll(AttributeTableBuilder builder, Assembly asmb, string propName, DisplayNameAttribute attr)
        {
            var activities = asmb.GetExportedTypes().Where(a => typeof(Activity).IsAssignableFrom(a));
            foreach (var entry in activities)
            {
                builder.AddCustomAttributes(entry, propName, attr);
            }
        }
    }
}