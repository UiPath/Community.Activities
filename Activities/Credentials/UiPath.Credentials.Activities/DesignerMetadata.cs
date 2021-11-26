using CredentialManagement;
using System;
using System.Activities;
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

            // DisplayNames
            builder.AddCustomAttributes(typeof(GetSecureCredential), new DisplayNameAttribute(Resources.Activity_GetSecureCredential_Property_GetSecureCredentialDisplayName_Name));
            builder.AddCustomAttributes(typeof(AddCredential), new DisplayNameAttribute(Resources.Activity_AddCredential_Property_AddCredentialDisplayName_Name));
            builder.AddCustomAttributes(typeof(DeleteCredential), new DisplayNameAttribute(Resources.Activity_DeleteCredential_Property_DeleteCredentialDisplayName_Name));
            builder.AddCustomAttributes(typeof(RequestCredential), new DisplayNameAttribute(Resources.Activity_RequestCredential_Property_RequestCredentialDisplayName_Name));

            // Descriptions
            builder.AddCustomAttributes(typeof(GetSecureCredential), new DescriptionAttribute(Resources.Activity_GetSecureCredential_Property_GetSecureCredentialDescription_Description));
            builder.AddCustomAttributes(typeof(AddCredential), new DescriptionAttribute(Resources.Activity_AddCredential_Property_AddCredentialDescription_Description));
            builder.AddCustomAttributes(typeof(DeleteCredential), new DescriptionAttribute(Resources.Activity_DeleteCredential_Property_DeleteCredentialDescription_Description));
            builder.AddCustomAttributes(typeof(RequestCredential), new DescriptionAttribute(Resources.Activity_RequestCredential_Property_RequestCredentialDescription_Description));

            // GetSecureCredential Properties
            builder.AddCustomAttributes(typeof(GetSecureCredential), nameof(GetSecureCredential.Result), new DescriptionAttribute(Resources.Result));
            
            // AddCredential Properties
            builder.AddCustomAttributes(typeof(AddCredential), nameof(AddCredential.Result), new DescriptionAttribute(Resources.Result));
            
            // DeleteCredential Properties
            builder.AddCustomAttributes(typeof(DeleteCredential), nameof(DeleteCredential.Result), new DescriptionAttribute(Resources.Result));
            
            // RequestCredential Properties
            builder.AddCustomAttributes(typeof(RequestCredential), nameof(RequestCredential.Result), new DescriptionAttribute(Resources.Result));

            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}
