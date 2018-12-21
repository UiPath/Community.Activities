using DocAcquire;
using DocAcquire.Activities;
using DocAcquire.Activities.Properties;
using DocAcquire.DataContracts;
using System;
using System.Activities;
using System.IO;
using System.Threading.Tasks;

namespace DocAcquire.Activities
{
    public class ExtractData : AsyncTaskCodeActivity
    {
        private readonly IDocumentExtractionService documentExtractionService;

        public ExtractData(IDocumentExtractionService documentExtractionService)
        {
            this.documentExtractionService = documentExtractionService;
        }

        public ExtractData()
        {
            this.documentExtractionService = new DocumentExtractionService();
        }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.ServiceUrl))]
        [RequiredArgument]
        public InArgument<string> ServiceUrl { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.OAuth20Token))]
        [RequiredArgument]
        public InArgument<string> Token { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.DocumentPath))]
        [RequiredArgument]
        public InArgument<string> DocumentPath { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.DataExtractionResult))]
        public OutArgument<DocumentExtractResponse> DataExtractionResult { get; set; }


        protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var serviceUrl = ServiceUrl.Get(context);
            var token = Token.Get(context);
            var filePath = DocumentPath.Get(context);

            var fileInfo = new FileInfo(filePath);
            var attachment = new AttachmentItem
            {
                Content = File.ReadAllBytes(filePath),
                Name = fileInfo.Name
            };

            var result = await this.documentExtractionService.ExtractAsync(attachment, token, serviceUrl);

            return (asyncActivityContext) =>
            {
                DataExtractionResult.Set(asyncActivityContext, result);
            };
        }
    }
}
