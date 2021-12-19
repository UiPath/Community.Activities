using System.Activities.Presentation.Metadata;
using System.ComponentModel;
using System.ComponentModel.Design;
using UiPath.ZenDesk.Activities.Design.Designers;
using UiPath.ZenDesk.Activities.Design.Properties;

namespace UiPath.ZenDesk.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            var builder = new AttributeTableBuilder();
            builder.ValidateTable();

            var categoryAttribute = new CategoryAttribute($"{Resources.Category}");

            builder.AddCustomAttributes(typeof(ZendeskScope), categoryAttribute);
            builder.AddCustomAttributes(typeof(ZendeskScope), new DesignerAttribute(typeof(ZendeskScopeDesigner)));
            builder.AddCustomAttributes(typeof(ZendeskScope), new HelpKeywordAttribute(""));

            builder.AddCustomAttributes(typeof(GetTicket), categoryAttribute);
            builder.AddCustomAttributes(typeof(GetTicket), new DesignerAttribute(typeof(GetTicketDesigner)));
            builder.AddCustomAttributes(typeof(GetTicket), new HelpKeywordAttribute(""));

            builder.AddCustomAttributes(typeof(UpdateTicket), categoryAttribute);
            builder.AddCustomAttributes(typeof(UpdateTicket), new DesignerAttribute(typeof(UpdateTicketDesigner)));
            builder.AddCustomAttributes(typeof(UpdateTicket), new HelpKeywordAttribute(""));

            builder.AddCustomAttributes(typeof(GetUser), categoryAttribute);
            builder.AddCustomAttributes(typeof(GetUser), new DesignerAttribute(typeof(GetUserDesigner)));
            builder.AddCustomAttributes(typeof(GetUser), new HelpKeywordAttribute(""));

            builder.AddCustomAttributes(typeof(GetUserFields), categoryAttribute);
            builder.AddCustomAttributes(typeof(GetUserFields), new DesignerAttribute(typeof(GetUserFieldsDesigner)));
            builder.AddCustomAttributes(typeof(GetUserFields), new HelpKeywordAttribute(""));

            builder.AddCustomAttributes(typeof(GetTicketFieldOption), categoryAttribute);
            builder.AddCustomAttributes(typeof(GetTicketFieldOption), new DesignerAttribute(typeof(GetTicketFieldOptionDesigner)));
            builder.AddCustomAttributes(typeof(GetTicketFieldOption), new HelpKeywordAttribute(""));


            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}
