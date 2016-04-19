using System;
using System.Activities.Presentation.Metadata;
using System.Activities.Presentation.PropertyEditing;
using System.ComponentModel;
using UiPath.Script.Activities.AutoHotKey;
using UiPath.Script.Activities.Design.Editors;

namespace UiPath.Script.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            var builder = new AttributeTableBuilder();
            var autoHotKeyType = typeof(ExecuteAutoHotKey);

            builder.AddCustomAttributes(autoHotKeyType, new CategoryAttribute("App Scripting"));

            builder.AddCustomAttributes(autoHotKeyType, "Parameters", new EditorAttribute(typeof(ArgumentCollectionEditor), typeof(DialogPropertyValueEditor)));

            builder.AddCustomAttributes(autoHotKeyType, "Result", new CategoryAttribute("Output"));

            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}
