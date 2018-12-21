using DocAcquire.Activities.Design.Properties;
using System.Activities.Presentation.Metadata;
using System.ComponentModel;
using DocAcquire.Activities;

namespace DocAcquire.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            CategoryAttribute category = new CategoryAttribute(Resources.DocAcquireActivitiesCategory);
            AttributeTableBuilder builder = new AttributeTableBuilder();

            builder.AddCustomAttributes(typeof(ExtractData), category);
            builder.AddCustomAttributes(typeof(UploadDocument), category);
            builder.AddCustomAttributes(typeof(GetVerifiedData), category);

            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}
