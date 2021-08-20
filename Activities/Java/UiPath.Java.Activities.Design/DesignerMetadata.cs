using System;
using System.Activities.Presentation;
using System.Activities.Presentation.Metadata;
using System.ComponentModel;
using System.Reflection;
using System.Linq;
using System.Activities;
using UiPath.Java.Activities.Design.Properties;
using System.Activities.Presentation.PropertyEditing;

namespace UiPath.Java.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            AttributeTableBuilder builder = new AttributeTableBuilder();


            // Designers
            builder.AddCustomAttributes(typeof(JavaScope), new DesignerAttribute(typeof(JavaScopeDesigner)));
            builder.AddCustomAttributes(typeof(LoadJar), new DesignerAttribute(typeof(LoadJarDesigner)));
            builder.AddCustomAttributes(typeof(InvokeJavaMethod), new DesignerAttribute(typeof(InvokeJavaMethodDesigner)));
            builder.AddCustomAttributes(typeof(CreateJavaObject), new DesignerAttribute(typeof(CreateJavaObjectDesigner)));
            builder.AddCustomAttributes(typeof(ConvertJavaObject<>), new DesignerAttribute(typeof(ConvertJavaObjectDesigner)));
            builder.AddCustomAttributes(typeof(GetJavaField), new DesignerAttribute(typeof(GetJavaFieldDesigner)));

            var argumentCollectionEditor = new EditorAttribute(typeof(ArgumentCollectionEditor), typeof(DialogPropertyValueEditor));
            builder.AddCustomAttributes(typeof(JavaActivityWithParameters), nameof(JavaActivityWithParameters.Parameters), argumentCollectionEditor);

            // DisplayNames

            //Categories
            CategoryAttribute javaCategoryAttribute = 
                new CategoryAttribute($"{Resources.AppInvokerCategory}.{Resources.JavaCategory}");
            builder.AddCustomAttributes(typeof(JavaScope), javaCategoryAttribute);
            builder.AddCustomAttributes(typeof(LoadJar), javaCategoryAttribute);
            builder.AddCustomAttributes(typeof(InvokeJavaMethod), javaCategoryAttribute);
            builder.AddCustomAttributes(typeof(ConvertJavaObject<>), javaCategoryAttribute);
            builder.AddCustomAttributes(typeof(CreateJavaObject), javaCategoryAttribute);
            builder.AddCustomAttributes(typeof(GetJavaField), javaCategoryAttribute);


            // Generic TypeArgument
            Type attrType = Type.GetType("System.Activities.Presentation.FeatureAttribute, System.Activities.Presentation");
            Type argType = Type.GetType("System.Activities.Presentation.UpdatableGenericArgumentsFeature, System.Activities.Presentation");
            var genericTypeArgument = Activator.CreateInstance(attrType, new object[] { argType }) as Attribute;
            builder.AddCustomAttributes(typeof(Type), new DisplayNameAttribute(Resources.Activity_ConvertJavaObject_Property_TypeArgument_Name));
            builder.AddCustomAttributes(typeof(ConvertJavaObject<>), genericTypeArgument);
            builder.AddCustomAttributes(typeof(ConvertJavaObject<>), new DefaultTypeArgumentAttribute(typeof(object)));

            AddDisplayNameToActivities(builder, typeof(JavaScope).Assembly, nameof(Activity.DisplayName), new DisplayNameAttribute(Resources.DisplayName));

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
