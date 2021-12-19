using System.Activities.Presentation.Metadata;
using System.ComponentModel;
using System.ComponentModel.Design;
using UiPath.WebhookHandler.Activities.Design.Designers;
using UiPath.WebhookHandler.Activities.Design.Properties;

namespace UiPath.WebhookHandler.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            var builder = new AttributeTableBuilder();
            builder.ValidateTable();

            var categoryAttribute = new CategoryAttribute($"{Resources.Category}");

            builder.AddCustomAttributes(typeof(WebhookHandler), categoryAttribute);
            builder.AddCustomAttributes(typeof(WebhookHandler), new DesignerAttribute(typeof(WebhookHandler)));
            builder.AddCustomAttributes(typeof(WebhookHandler), new HelpKeywordAttribute(""));

            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}
