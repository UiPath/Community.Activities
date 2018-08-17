using System.Activities.Presentation.Metadata;
using System.ComponentModel;

namespace UiPath.Credentials.Activities
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            var builder = new AttributeTableBuilder();

            builder.AddCustomAttributes(typeof(AddCredential), "Result", new CategoryAttribute("Output"));
            builder.AddCustomAttributes(typeof(DeleteCredential), "Result", new CategoryAttribute("Output"));
            builder.AddCustomAttributes(typeof(GetCredential), "Result", new CategoryAttribute("Output"));
            builder.AddCustomAttributes(typeof(RequestCredential), "Result", new CategoryAttribute("Output"));

            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}
