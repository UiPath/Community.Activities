using System.Activities.Presentation.Metadata;
using System.Activities.Presentation.View.OutlineView;
using System.ComponentModel;
using UiPath.FTP.Activities.Design.Properties;

namespace UiPath.FTP.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            AttributeTableBuilder builder = new AttributeTableBuilder();
            ShowPropertyInOutlineViewAttribute hideFromOutlineAttribute = new ShowPropertyInOutlineViewAttribute() { CurrentPropertyVisible = false, DuplicatedChildNodesVisible = false };

            builder.AddCustomAttributes(typeof(WithFtpSession), new DesignerAttribute(typeof(WithFtpSessionDesigner)));

            builder.AddCustomAttributes(typeof(WithFtpSession), nameof(WithFtpSession.Body), hideFromOutlineAttribute);

            builder.AddCustomAttributes(typeof(Delete), new CategoryAttribute(Resources.FTPActivitiesCategory));
            builder.AddCustomAttributes(typeof(DirectoryExists), new CategoryAttribute(Resources.FTPActivitiesCategory));
            builder.AddCustomAttributes(typeof(DownloadFiles), new CategoryAttribute(Resources.FTPActivitiesCategory));
            builder.AddCustomAttributes(typeof(EnumerateObjects), new CategoryAttribute(Resources.FTPActivitiesCategory));
            builder.AddCustomAttributes(typeof(FileExists), new CategoryAttribute(Resources.FTPActivitiesCategory));
            builder.AddCustomAttributes(typeof(UploadFiles), new CategoryAttribute(Resources.FTPActivitiesCategory));
            builder.AddCustomAttributes(typeof(WithFtpSession), new CategoryAttribute(Resources.FTPActivitiesCategory));

            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}
