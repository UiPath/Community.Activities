using System;
using System.Activities.Presentation;
using System.Activities.Presentation.Metadata;
using System.Activities.Presentation.PropertyEditing;
using System.ComponentModel;
using UiPath.Script.Activities.AutoHotKey;
using UiPath.Script.Activities.Design.Editors;
using UiPath.Script.Activities.PowerShell;

namespace UiPath.Script.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            var builder = new AttributeTableBuilder();

            var categoryAttribute = new CategoryAttribute("App Scripting");
            var autoHotKeyType = typeof(RunAutoHotKeyScript);

            builder.AddCustomAttributes(autoHotKeyType, categoryAttribute);
            builder.AddCustomAttributes(autoHotKeyType, "Parameters", new EditorAttribute(typeof(CollectionArgumentEditor), typeof(DialogPropertyValueEditor)));
            builder.AddCustomAttributes(autoHotKeyType, "Result", new CategoryAttribute("Output"));
            builder.AddCustomAttributes(autoHotKeyType, new DesignerAttribute(typeof(ScriptActivityDesigner)));

            var psType = typeof(RunPowerShellScript<>);

            builder.AddCustomAttributes(psType, categoryAttribute);
            builder.AddCustomAttributes(psType, "Parameters", new EditorAttribute(typeof(DictionaryArgumentEditor), typeof(DialogPropertyValueEditor)));
            builder.AddCustomAttributes(psType, new DesignerAttribute(typeof(ScriptActivityDesigner)));
            builder.AddCustomAttributes(psType, new DefaultTypeArgumentAttribute(typeof(object)));

            Type attrType = Type.GetType("System.Activities.Presentation.FeatureAttribute, System.Activities.Presentation, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
            Type argType = Type.GetType("System.Activities.Presentation.UpdatableGenericArgumentsFeature, System.Activities.Presentation, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
            var genericTypeArgument = Activator.CreateInstance(attrType, new object[] { argType }) as Attribute;

            builder.AddCustomAttributes(psType, genericTypeArgument);

            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}
