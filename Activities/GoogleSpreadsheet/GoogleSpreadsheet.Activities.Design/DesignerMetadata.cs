using GoogleSpreadsheet.Activities;
using System;
using System.Activities.Presentation.Metadata;
using System.ComponentModel;

namespace GoogleSpreadsheet.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            var builder = new AttributeTableBuilder();

            // Designers for Excel application integration
            builder.AddCustomAttributes(typeof(GoogleSheetApplicationScope), new DesignerAttribute(typeof(GoogleSheetApplicationDesigner)));

            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}
