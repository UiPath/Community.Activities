using System;
using System.Activities;
using System.Activities.Presentation;
using System.Activities.Presentation.Metadata;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UiPath.Python.Activities.Design.Properties;

namespace UiPath.Python.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            AttributeTableBuilder builder = new AttributeTableBuilder();

            // Designers
            builder.AddCustomAttributes(typeof(PythonScope), new DesignerAttribute(typeof(PythonScopeDesigner)));
            builder.AddCustomAttributes(typeof(LoadScript), new DesignerAttribute(typeof(LoadScriptDesigner)));
            builder.AddCustomAttributes(typeof(RunScript), new DesignerAttribute(typeof(RunScriptDesigner)));

            // Browsable false

            // DisplayNames

            //Categories
            CategoryAttribute pythonCategoryAttribute = 
                new CategoryAttribute($"{Resources.CategoryAppInvoker}.{Resources.CategoryPython}");
            builder.AddCustomAttributes(typeof(PythonScope), pythonCategoryAttribute);
            builder.AddCustomAttributes(typeof(RunScript), pythonCategoryAttribute);
            builder.AddCustomAttributes(typeof(LoadScript), pythonCategoryAttribute);
            builder.AddCustomAttributes(typeof(InvokeMethod), pythonCategoryAttribute);
            builder.AddCustomAttributes(typeof(GetObject<>), pythonCategoryAttribute);

            // Generic TypeArgument
            Type attrType = Type.GetType("System.Activities.Presentation.FeatureAttribute, System.Activities.Presentation");
            Type argType = Type.GetType("System.Activities.Presentation.UpdatableGenericArgumentsFeature, System.Activities.Presentation");
            var genericTypeArgument = Activator.CreateInstance(attrType, new object[] { argType }) as Attribute;
            builder.AddCustomAttributes(typeof(GetObject<>), genericTypeArgument);
            builder.AddCustomAttributes(typeof(GetObject<>), new DefaultTypeArgumentAttribute(typeof(object)));

            AddDisplayNameToActivities(builder, typeof(PythonScope).Assembly, nameof(Activity.DisplayName), new DisplayNameAttribute(Resources.DisplayName));

            MetadataStore.AddAttributeTable(builder.CreateTable());
        }

        private void AddDisplayNameToActivities(AttributeTableBuilder builder, Assembly assembly, string propertyName, DisplayNameAttribute attr)
        {
            var activities = assembly.GetExportedTypes().Where(a => typeof(Activity).IsAssignableFrom(a));
            foreach (var activity in activities)
            {
                builder.AddCustomAttributes(activity, propertyName, attr);
            }
        }
    }
}
