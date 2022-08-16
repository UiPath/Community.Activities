using System;
using System.Activities.Presentation;
using System.Activities.Presentation.Metadata;
using System.Activities.Presentation.PropertyEditing;
using System.ComponentModel;
using System.Linq;
using System.Management.Automation;
using UiPath.Scripting.Activities.Design.Properties;
using UiPath.Scripting.Activities.PowerShell;

namespace UiPath.Scripting.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        private void AddDefaultTypeArgumentAttribute(AttributeTableBuilder builder)
        {
            var presentationDll = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "System.Activities.Presentation");
            Type attrType = presentationDll.GetType("System.Activities.Presentation.FeatureAttribute");
            Type argType = presentationDll.GetType("System.Activities.Presentation.UpdatableGenericArgumentsFeature");

            var genericTypeArgument = Activator.CreateInstance(attrType, new object[] { argType }) as Attribute;

            builder.AddCustomAttributes(typeof(InvokePowerShellCore<>), genericTypeArgument);
            builder.AddCustomAttributes(typeof(InvokePowerShellCore<>),
                new DefaultTypeArgumentAttribute(typeof(PSObject)));
        }

        public void Register()
        {
            var systemPowerShellCategoryName =
                new CategoryAttribute($"{Resources.CategoryScripting}.{Resources.CategoryPowerShell}");
            var argumentDictionaryEditor =
                new EditorAttribute(typeof(ArgumentDictionaryEditor), typeof(DialogPropertyValueEditor));
            AttributeTableBuilder builder = new AttributeTableBuilder();
            var hiddenActivityAttribute = new BrowsableAttribute(false);

            MetadataStore.AddAttributeTable(builder.CreateTable());

            builder.AddCustomAttributes(typeof(InvokePowerShellCore<>), new DesignerAttribute(typeof(InvokePowerShellCoreDesigner)));
            builder.AddCustomAttributes(typeof(InvokePowerShellCore<>), new DefaultTypeArgumentAttribute(typeof(PSObject)));
            builder.AddCustomAttributes(typeof(InvokePowerShellCore<>), new DisplayNameAttribute(Resources.InvokePowerShellCoreDisplayName));
            builder.AddCustomAttributes(typeof(InvokePowerShellCore<>), systemPowerShellCategoryName);
            builder.AddCustomAttributes(typeof(InvokePowerShellCore<>), nameof(InvokePowerShellCore<object>.Parameters), argumentDictionaryEditor);
            builder.AddCustomAttributes(typeof(InvokePowerShellCore<>), nameof(InvokePowerShellCore<object>.PowerShellVariables), argumentDictionaryEditor);
            builder.AddCustomAttributes(typeof(ExecutePowerShellCore), hiddenActivityAttribute);
            AddDefaultTypeArgumentAttribute(builder);
        }
    }
}
