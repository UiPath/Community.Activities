using System;
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

            builder.AddCustomAttributes(typeof(ExecuteNonQuery), new DesignerAttribute(typeof(GenericDatabaseDesigner)));
            builder.AddCustomAttributes(typeof(ExecuteQuery), new DesignerAttribute(typeof(GenericDatabaseDesigner)));
            builder.AddCustomAttributes(typeof(InsertDataTable), new DesignerAttribute(typeof(InsertDataTableDesigner)));
            builder.AddCustomAttributes(typeof(DatabaseConnect), new DesignerAttribute(typeof(ConnectDatabaseDesigner)));
            builder.AddCustomAttributes(typeof(DatabaseTransaction), new DesignerAttribute(typeof(ConnectDatabaseDesigner)));
            builder.AddCustomAttributes(typeof(DatabaseDisconnect), new DesignerAttribute(typeof(DisconnectDesigner)));

            // Editors
            builder.AddCustomAttributes(typeof(ExecuteQuery), nameof(ExecuteQuery.Parameters), new EditorAttribute(typeof(ArgumentDictionaryEditor), typeof(DialogPropertyValueEditor)));
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), nameof(ExecuteQuery.Parameters), new EditorAttribute(typeof(ArgumentDictionaryEditor), typeof(DialogPropertyValueEditor)));

            //Content attribute
            var contentAttr = new ContentPropertyAttribute(nameof(ExecuteNonQuery.ExistingDbConnection));
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), contentAttr);
            builder.AddCustomAttributes(typeof(ExecuteQuery), contentAttr);
            builder.AddCustomAttributes(typeof(InsertDataTable), contentAttr);

            // DisplayName attribute
            builder.AddCustomAttributes(typeof(DatabaseConnect), new DisplayNameAttribute(Resources.Connect));
            builder.AddCustomAttributes(typeof(InsertDataTable), new DisplayNameAttribute(Resources.Insert));
            builder.AddCustomAttributes(typeof(DatabaseDisconnect), new DisplayNameAttribute(Resources.Disconnect));
            builder.AddCustomAttributes(typeof(DatabaseTransaction), new DisplayNameAttribute(Resources.StartTransaction));
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), new DisplayNameAttribute(Resources.ExecuteNonQuery));
            builder.AddCustomAttributes(typeof(ExecuteQuery), new DisplayNameAttribute(Resources.ExecuteQuery));


            //Categories
            CategoryAttribute appIntegrationDatabaseCategoryAttribute =
                new CategoryAttribute($"{Resources.CategoryAppIntegration}.{Resources.CategoryDatabase}");
            builder.AddCustomAttributes(typeof(DatabaseConnect), appIntegrationDatabaseCategoryAttribute);
            builder.AddCustomAttributes(typeof(DatabaseDisconnect), appIntegrationDatabaseCategoryAttribute);
            builder.AddCustomAttributes(typeof(DatabaseTransaction), appIntegrationDatabaseCategoryAttribute);
            builder.AddCustomAttributes(typeof(InsertDataTable), appIntegrationDatabaseCategoryAttribute);
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), appIntegrationDatabaseCategoryAttribute);
            builder.AddCustomAttributes(typeof(ExecuteQuery), appIntegrationDatabaseCategoryAttribute);

            AddDescription(builder);

            MetadataStore.AddAttributeTable(builder.CreateTable());
        }

        private static void AddDescription(AttributeTableBuilder builder)
        {
            builder.AddCustomAttributes(typeof(DatabaseTransaction), new DescriptionAttribute(Resources.DbTransactionDescription));
            builder.AddCustomAttributes(typeof(ExecuteQuery), new DescriptionAttribute(Resources.ExecuteQueryDescription));
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), new DescriptionAttribute(Resources.ExecuteNonQueryDescription));
            builder.AddCustomAttributes(typeof(InsertDataTable), new DescriptionAttribute(Resources.InsertDataTableDescription));
            builder.AddCustomAttributes(typeof(DatabaseConnect), new DescriptionAttribute(Resources.DatabaseConnectDescription));
            builder.AddCustomAttributes(typeof(DatabaseDisconnect), new DescriptionAttribute(Resources.DatabaseDisconnectDescription));


            var connectionString = new DescriptionAttribute(Resources.ConnectionStringDescription);
            var providerName = new DescriptionAttribute(Resources.ProviderNameDescription);
            var parameters = new DescriptionAttribute(Resources.ParametersDescription);

            builder.AddCustomAttributes(typeof(DatabaseTransaction), nameof(DatabaseTransaction.ConnectionString), connectionString);
            builder.AddCustomAttributes(typeof(DatabaseTransaction), nameof(DatabaseTransaction.ProviderName), providerName);
            builder.AddCustomAttributes(typeof(DatabaseTransaction), nameof(DatabaseTransaction.UseTransaction), new DescriptionAttribute(Resources.UseTransactionDescription));
            builder.AddCustomAttributes(typeof(DatabaseTransaction), nameof(DatabaseTransaction.DatabaseConnection), new DescriptionAttribute(Resources.DatabaseConnectionDescription));

            builder.AddCustomAttributes(typeof(ExecuteQuery), nameof(ExecuteNonQuery.ConnectionString), connectionString);
            builder.AddCustomAttributes(typeof(ExecuteQuery), nameof(ExecuteNonQuery.ProviderName), providerName);
            builder.AddCustomAttributes(typeof(ExecuteQuery), nameof(ExecuteQuery.ExistingDbConnection), new DescriptionAttribute(Resources.ExistingDbConnectionDescription));
            builder.AddCustomAttributes(typeof(ExecuteQuery), nameof(ExecuteQuery.Parameters), parameters);
            builder.AddCustomAttributes(typeof(ExecuteQuery), nameof(ExecuteQuery.CommandType), new DescriptionAttribute(Resources.CommandTypeDescription));
            builder.AddCustomAttributes(typeof(ExecuteQuery), nameof(ExecuteQuery.Sql), new DescriptionAttribute(Resources.SqlDescription));
            builder.AddCustomAttributes(typeof(ExecuteQuery), nameof(ExecuteQuery.DataTable), new DescriptionAttribute(Resources.DataTableDescription));
            builder.AddCustomAttributes(typeof(ExecuteQuery), nameof(ExecuteQuery.TimeoutMS), new DescriptionAttribute(Resources.QueryTimeoutMSDescription));

            builder.AddCustomAttributes(typeof(ExecuteNonQuery), nameof(ExecuteNonQuery.ConnectionString), connectionString);
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), nameof(ExecuteNonQuery.ProviderName), providerName);
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), nameof(ExecuteNonQuery.ExistingDbConnection), new DescriptionAttribute(Resources.ExistingDbConnectionDescription));
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), nameof(ExecuteNonQuery.Parameters), parameters);
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), nameof(ExecuteNonQuery.CommandType), new DescriptionAttribute(Resources.CommandTypeDescription));
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), nameof(ExecuteNonQuery.Sql), new DescriptionAttribute(Resources.SqlDescription));
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), nameof(ExecuteNonQuery.AffectedRecords), new DescriptionAttribute(Resources.AffectedRecordsDescription));
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), nameof(ExecuteNonQuery.TimeoutMS), new DescriptionAttribute(Resources.TimeoutMSDescription));


            builder.AddCustomAttributes(typeof(InsertDataTable), nameof(InsertDataTable.ConnectionString), connectionString);
            builder.AddCustomAttributes(typeof(InsertDataTable), nameof(InsertDataTable.ProviderName), providerName);
            builder.AddCustomAttributes(typeof(InsertDataTable), nameof(InsertDataTable.ExistingDbConnection), new DescriptionAttribute(Resources.ExistingDbConnectionDescription));
            builder.AddCustomAttributes(typeof(InsertDataTable), nameof(InsertDataTable.TableName), new DescriptionAttribute(Resources.TableNameDescription));
            builder.AddCustomAttributes(typeof(InsertDataTable), nameof(InsertDataTable.DataTable), new DescriptionAttribute(Resources.InserDataTableInputDescription));
            builder.AddCustomAttributes(typeof(InsertDataTable), nameof(InsertDataTable.AffectedRecords), new DescriptionAttribute(Resources.AffectedRecordsInsertDescription));

            builder.AddCustomAttributes(typeof(DatabaseConnect), nameof(DatabaseConnect.ConnectionString), connectionString);
            builder.AddCustomAttributes(typeof(DatabaseConnect), nameof(DatabaseConnect.ProviderName), providerName);
            builder.AddCustomAttributes(typeof(DatabaseConnect), nameof(DatabaseConnect.DatabaseConnection), new DescriptionAttribute(Resources.DatabaseConnectionDescription));

            builder.AddCustomAttributes(typeof(DatabaseDisconnect), nameof(DatabaseConnect.DatabaseConnection), new DescriptionAttribute(Resources.DatabaseConnectionDescription));

            AddToAll(builder, typeof(DatabaseConnect).Assembly, nameof(Activity.DisplayName), new DisplayNameAttribute(Resources.DisplayName));
        }

        private static void AddToAll(AttributeTableBuilder builder, Assembly asmb, string propName, DisplayNameAttribute attr)
        {
            var activities = asmb.GetExportedTypes().Where(a => typeof(Activity).IsAssignableFrom(a));
            foreach(var entry in activities)
            {
                builder.AddCustomAttributes(entry, propName, attr);
            }
        }
    }
}
