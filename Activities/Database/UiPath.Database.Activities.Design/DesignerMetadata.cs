using System.Activities;
using System.Activities.Presentation.Metadata;
using System.Activities.Presentation.PropertyEditing;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Markup;
using UiPath.Activities.Presentation.Editors;
using UiPath.Database.Activities.Design.Properties;
using UiPath.Studio.Activities.Api;

namespace UiPath.Database.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        private IWorkflowDesignApi _wfDesignApi;

        private void InitializeInternal(object api)
        {
            if (api is IWorkflowDesignApi wfDesignApi)
            {
                _wfDesignApi = wfDesignApi;

                new ActivitySynonymApiRegistration().Initialize(wfDesignApi);
            }
        }

        public void Initialize(object api)
        {
            if (api == null)
            {
                return;
            }
            InitializeInternal(api);
        }

        public void Register()
        {
            var builder = new AttributeTableBuilder();

            builder.AddCustomAttributes(typeof(DatabaseConnect), new DesignerAttribute(typeof(ConnectDatabaseDesigner)));
            builder.AddCustomAttributes(typeof(DatabaseDisconnect), new DesignerAttribute(typeof(DisconnectDesigner)));
            builder.AddCustomAttributes(typeof(DatabaseTransaction), new DesignerAttribute(typeof(ConnectDatabaseDesigner)));
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), new DesignerAttribute(typeof(ExecuteNonQueryDesigner)));
            builder.AddCustomAttributes(typeof(ExecuteQuery), new DesignerAttribute(typeof(ExecuteQueryDesigner)));
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
            builder.AddCustomAttributes(typeof(DatabaseConnect), new DisplayNameAttribute(SharedResources.Activity_DatabaseConnect_Name));
            builder.AddCustomAttributes(typeof(DatabaseDisconnect), new DisplayNameAttribute(SharedResources.Activity_DatabaseDisconnect_Name));
            builder.AddCustomAttributes(typeof(DatabaseTransaction), new DisplayNameAttribute(SharedResources.Activity_DatabaseTransaction_Name));
            builder.AddCustomAttributes(typeof(ExecuteNonQuery), new DisplayNameAttribute(SharedResources.Activity_ExecuteNonQuery_Name));
            builder.AddCustomAttributes(typeof(ExecuteQuery), new DisplayNameAttribute(SharedResources.Activity_ExecuteQuery_Name));
            builder.AddCustomAttributes(typeof(InsertDataTable), new DisplayNameAttribute(SharedResources.Activity_InsertDataTable_Name));
            builder.AddCustomAttributes(typeof(BulkInsert), new DisplayNameAttribute(SharedResources.Activity_BulkInsert_Name));
            builder.AddCustomAttributes(typeof(BulkUpdate), new DisplayNameAttribute(SharedResources.Activity_BulkUpdate_Name));

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

            AddToAll(builder, typeof(DatabaseConnect).Assembly, nameof(Activity.DisplayName), new DisplayNameAttribute(Resources.DisplayName));

            MetadataStore.AddAttributeTable(builder.CreateTable());
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