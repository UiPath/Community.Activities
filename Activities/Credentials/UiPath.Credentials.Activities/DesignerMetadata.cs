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

            // Categories
            CategoryAttribute CredentialsCategoryAttribute =
                new CategoryAttribute($"{Resources.CategorySystem}.{Resources.CategoryCredentials}");
            builder.AddCustomAttributes(typeof(AddCredential), CredentialsCategoryAttribute);
            builder.AddCustomAttributes(typeof(DeleteCredential), CredentialsCategoryAttribute);
            builder.AddCustomAttributes(typeof(GetSecureCredential), CredentialsCategoryAttribute);
            builder.AddCustomAttributes(typeof(RequestCredential), CredentialsCategoryAttribute);

            builder.AddCustomAttributes(typeof(AddCredential), Resources.Result, new CategoryAttribute(Resources.Output));
            builder.AddCustomAttributes(typeof(DeleteCredential), Resources.Result, new CategoryAttribute(Resources.Output));
            builder.AddCustomAttributes(typeof(GetSecureCredential), Resources.Result, new CategoryAttribute(Resources.Output));
            builder.AddCustomAttributes(typeof(RequestCredential), Resources.Result, new CategoryAttribute(Resources.Output));

            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}
