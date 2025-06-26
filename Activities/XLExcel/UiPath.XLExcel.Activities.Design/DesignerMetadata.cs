using System.Activities.Presentation.Metadata;
using System.ComponentModel;

namespace UiPathTeam.XLExcel.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            AttributeTableBuilder attributeTableBuilder = new AttributeTableBuilder();

            attributeTableBuilder.AddCustomAttributes(typeof(XLExcelApplicationScope), new DesignerAttribute(typeof(XLExcelApplicationScopeDesigner)));
            attributeTableBuilder.AddCustomAttributes(typeof(ReadRange), new DesignerAttribute(typeof(ReadRangeDesigner)));

            MetadataStore.AddAttributeTable(attributeTableBuilder.CreateTable());
        }
    }
}
