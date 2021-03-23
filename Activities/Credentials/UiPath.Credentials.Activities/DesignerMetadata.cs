using System.Activities.Presentation.Metadata;
using System.ComponentModel;
using UiPath.Credentials.Activities.Properties;

namespace UiPath.Credentials.Activities
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            var builder = new AttributeTableBuilder();

            builder.AddCustomAttributes(typeof(AddCredential), Resources.Result, new CategoryAttribute(Resources.Output));
            builder.AddCustomAttributes(typeof(DeleteCredential), Resources.Result, new CategoryAttribute(Resources.Output));
            builder.AddCustomAttributes(typeof(GetCredential), Resources.Result, new CategoryAttribute(Resources.Output));
            builder.AddCustomAttributes(typeof(RequestCredential), Resources.Result, new CategoryAttribute(Resources.Output));

            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}
