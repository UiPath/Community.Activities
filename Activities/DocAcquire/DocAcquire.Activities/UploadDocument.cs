using DocAcquire.Activities;
using DocAcquire.Activities.Properties;
using DocAcquire.DataContracts;
using System;
using System.Activities;
using System.IO;
using System.Threading.Tasks;

namespace DocAcquire.Activities
{
    public class UploadDocument : AsyncTaskCodeActivity
    {
        private readonly IDocumentUploadService documentService;

        internal UploadDocument(IDocumentUploadService documentService)
        {
            this.documentService = documentService;
        }

        internal UploadDocument()
        {
            this.documentService = new DocumentUploadService();
        }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.ServiceUrl))]
        [RequiredArgument]
        public InArgument<string>    ServiceUrl { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.OAuth20Token))]
        [RequiredArgument]
        public InArgument<string> Token { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.InputDocumentPath))]
        [RequiredArgument]
        public InArgument<string> InputDocumentPath { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.DocumentUploadAsyncResult))]
        public OutArgument<FileUploadResponse> UploadResult { get; set; }

        protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var serviceUrl = ServiceUrl.Get(context);
            var token = Token.Get(context);
            var inputDocumentPAth = InputDocumentPath.Get(context);

            var fileInfo = new FileInfo(inputDocumentPAth);
            var attachment = new AttachmentItem
            {
                Content = File.ReadAllBytes(inputDocumentPAth),
                Name = fileInfo.Name
            };

            var result = await this.documentService.UploadAsync(attachment, token, serviceUrl);

            return (asyncActivityContext) =>
            {
                UploadResult.Set(asyncActivityContext, result);
            };
        }
    }
}
