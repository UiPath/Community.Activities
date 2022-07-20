using System.Activities.Presentation;
using System.Activities.Presentation.Metadata;
using System.Activities.Presentation.PropertyEditing;
using System.ComponentModel;
using System.Management.Automation;
using UiPath.Scripting.Activities.Design.Properties;
using UiPath.Scripting.Activities.PowerShell;
using UiPath.Shared.Activities.Design.Editors;

namespace UiPath.Scripting.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            AttributeTableBuilder builder = new AttributeTableBuilder();

            builder.AddCustomAttributes(typeof(InvokePowerShellCore<>), new DesignerAttribute(typeof(InvokePowerShellCoreDesigner)));

            var ContinueOnError = new DescriptionAttribute(Resources.ContinueOnError);

            builder.AddCustomAttributes(typeof(InvokePowerShellCore<>), nameof(InvokePowerShellCore<object>.ContinueOnError), ContinueOnError);

            MetadataStore.AddAttributeTable(builder.CreateTable());

            builder.AddCustomAttributes(typeof(InvokePowerShellCore<>),
                new DefaultTypeArgumentAttribute(typeof(PSObject)));

            builder.AddCustomAttributes(typeof(InvokePowerShellCore<>),
                new DisplayNameAttribute(Resources.InvokePowerShellCoreDisplayName));

            //Add Category
            var systemPowerShellCategoryName =
                new CategoryAttribute($"{Resources.CategoryScripting}.{Resources.CategoryPowerShell}");
            builder.AddCustomAttributes(typeof(InvokePowerShellCore<>), systemPowerShellCategoryName);

            // Editors
            var argumentDictionaryEditor =
                new EditorAttribute(typeof(ArgumentDictionaryEditor), typeof(DialogPropertyValueEditor));
            builder.AddCustomAttributes(typeof(InvokePowerShellCore<>), 
                nameof(InvokePowerShellCore<object>.Parameters), argumentDictionaryEditor);
            builder.AddCustomAttributes(typeof(InvokePowerShellCore<>),
                nameof(InvokePowerShellCore<object>.PowerShellVariables), argumentDictionaryEditor);

            builder.AddCustomAttributes(typeof(InvokePowerShellCore<>),
                new DesignerAttribute(typeof(InvokePowerShellCoreDesigner)));
        }
    }
}
