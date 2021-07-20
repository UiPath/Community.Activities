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
            builder.AddCustomAttributes(typeof(MoveItem), new DesignerAttribute(typeof(MoveItemsDesigner)));

            builder.AddCustomAttributes(typeof(WithFtpSession), nameof(WithFtpSession.Body), hideFromOutlineAttribute);

            // Category
            CategoryAttribute FTPCategoryAttribute =
               new CategoryAttribute($"{Resources.CategoryAppIntegration}.{Resources.CategoryFTP}");
            builder.AddCustomAttributes(typeof(Delete), FTPCategoryAttribute);
            builder.AddCustomAttributes(typeof(DirectoryExists), FTPCategoryAttribute);
            builder.AddCustomAttributes(typeof(DownloadFiles), FTPCategoryAttribute);
            builder.AddCustomAttributes(typeof(EnumerateObjects), FTPCategoryAttribute);
            builder.AddCustomAttributes(typeof(FileExists), FTPCategoryAttribute);
            builder.AddCustomAttributes(typeof(MoveItem), FTPCategoryAttribute);
            builder.AddCustomAttributes(typeof(UploadFiles), FTPCategoryAttribute);
            builder.AddCustomAttributes(typeof(WithFtpSession), FTPCategoryAttribute);

            // Activities DisplayName
            builder.AddCustomAttributes(typeof(Delete), new DisplayNameAttribute(Resources.DeleteDisplayName));
            builder.AddCustomAttributes(typeof(DirectoryExists), new DisplayNameAttribute(Resources.DirectoryExistsDisplayName));
            builder.AddCustomAttributes(typeof(DownloadFiles), new DisplayNameAttribute(Resources.DownloadFilesDisplayName));
            builder.AddCustomAttributes(typeof(EnumerateObjects), new DisplayNameAttribute(Resources.EnumerateObjectsDisplayName));
            builder.AddCustomAttributes(typeof(FileExists), new DisplayNameAttribute(Resources.FileExistsDisplayName));
            builder.AddCustomAttributes(typeof(MoveItem), new DisplayNameAttribute(Resources.MoveItemDisplayName));
            builder.AddCustomAttributes(typeof(UploadFiles), new DisplayNameAttribute(Resources.UploadFilesDisplayName));
            builder.AddCustomAttributes(typeof(WithFtpSession), new DisplayNameAttribute(Resources.WithFtpSessionDisplayName));

            // Properties and Descriptions
            var ContinueOnError = new DescriptionAttribute(Resources.ContinueOnError);
            var UsernameDescription = new DescriptionAttribute(Resources.UsernameDescription);
            var PasswordDescription = new DescriptionAttribute(Resources.PasswordDescription);
            var ClientCertificatePasswordDescription = new DescriptionAttribute(Resources.ClientCertificatePasswordDescription);
            var HostDescription = new DescriptionAttribute(Resources.HostDescription);
            var PortDescription = new DescriptionAttribute(Resources.PortDescription);
            var RemotePathDescription = new DescriptionAttribute(Resources.RemotePathDescription);
            var DirectoryExistsDescription = new DescriptionAttribute(Resources.DirectoryExistsDescription);
            var FileExistsDescription = new DescriptionAttribute(Resources.FileExistsDescription);
            var FilesDescription = new DescriptionAttribute(Resources.FilesDescription);
            var MoveItemNewPathDescription = new DescriptionAttribute(Resources.MoveItemNewPathDescription);
            var LocalPathDescription = new DescriptionAttribute(Resources.LocalPathDescription);

            //Delete properties
            builder.AddCustomAttributes(typeof(Delete), nameof(Delete.ContinueOnError), ContinueOnError);
            builder.AddCustomAttributes(typeof(Delete), nameof(Delete.RemotePath), RemotePathDescription);

            //DirectoryExists properties
            builder.AddCustomAttributes(typeof(DirectoryExists), nameof(DirectoryExists.ContinueOnError), ContinueOnError);
            builder.AddCustomAttributes(typeof(DirectoryExists), nameof(DirectoryExists.RemotePath), RemotePathDescription);
            builder.AddCustomAttributes(typeof(DirectoryExists), nameof(DirectoryExists.Exists), DirectoryExistsDescription);

            //File properties
            builder.AddCustomAttributes(typeof(FileExists), nameof(FileExists.ContinueOnError), ContinueOnError);
            builder.AddCustomAttributes(typeof(FileExists), nameof(FileExists.RemotePath), RemotePathDescription);
            builder.AddCustomAttributes(typeof(FileExists), nameof(FileExists.Exists), FileExistsDescription);

            //MoveItem properties
            builder.AddCustomAttributes(typeof(MoveItem), nameof(MoveItem.RemotePath), RemotePathDescription);
            builder.AddCustomAttributes(typeof(MoveItem), nameof(MoveItem.NewPath), MoveItemNewPathDescription);

            //DownloadFiles properties
            builder.AddCustomAttributes(typeof(DownloadFiles), nameof(DownloadFiles.ContinueOnError), ContinueOnError);
            builder.AddCustomAttributes(typeof(DownloadFiles), nameof(DownloadFiles.RemotePath), RemotePathDescription);
            builder.AddCustomAttributes(typeof(DownloadFiles), nameof(DownloadFiles.LocalPath), LocalPathDescription);

            //EnumerateObjects properties
            builder.AddCustomAttributes(typeof(EnumerateObjects), nameof(EnumerateObjects.ContinueOnError), ContinueOnError);
            builder.AddCustomAttributes(typeof(EnumerateObjects), nameof(EnumerateObjects.RemotePath), RemotePathDescription);
            builder.AddCustomAttributes(typeof(EnumerateObjects), nameof(EnumerateObjects.Files), FilesDescription);

            //UploadFiles properties
            builder.AddCustomAttributes(typeof(UploadFiles), nameof(UploadFiles.ContinueOnError), ContinueOnError);
            builder.AddCustomAttributes(typeof(UploadFiles), nameof(UploadFiles.LocalPath), LocalPathDescription);
            builder.AddCustomAttributes(typeof(UploadFiles), nameof(UploadFiles.RemotePath), RemotePathDescription);

            // WithFTPSession properties
            builder.AddCustomAttributes(typeof(WithFtpSession), nameof(WithFtpSession.ContinueOnError), ContinueOnError);
            builder.AddCustomAttributes(typeof(WithFtpSession), nameof(WithFtpSession.Username), UsernameDescription);
            builder.AddCustomAttributes(typeof(WithFtpSession), nameof(WithFtpSession.Password), PasswordDescription);
            builder.AddCustomAttributes(typeof(WithFtpSession), nameof(WithFtpSession.ClientCertificatePassword), ClientCertificatePasswordDescription);
            builder.AddCustomAttributes(typeof(WithFtpSession), nameof(WithFtpSession.Host), HostDescription);
            builder.AddCustomAttributes(typeof(WithFtpSession), nameof(WithFtpSession.Port), PortDescription);


            builder.AddCustomAttributes(typeof(WithFtpSession), nameof(WithFtpSession.UseAnonymousLogin), new DescriptionAttribute(Resources.UseAnonymousLoginDescription));


            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}
