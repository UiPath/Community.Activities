using DocAcquire;
using DocAcquire.Activities;
using DocAcquire.Activities.Properties;
using DocAcquire.DataContracts;
using System;
using System.Activities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DocAcquire.Activities
{
    public class GetVerifiedData : AsyncTaskCodeActivity
    {
        private readonly IVerificationService verificationService;

        public GetVerifiedData(IVerificationService verificationService)
        {
            this.verificationService = verificationService;
        }

        public GetVerifiedData()
        {
            this.verificationService = new VerificationService();
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
        [LocalizedDisplayName(nameof(Resources.DocumentIdList))]
        [RequiredArgument]
        public InArgument<int[]> DocumentIdList { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.DataExtractionResult))]
        public OutArgument<List<DocumentExtractResponse>> VerifiedDataResult { get; set; }


        protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context)
        {
            var serviceUrl = ServiceUrl.Get(context);
            var token = Token.Get(context);
            var documentIds = DocumentIdList.Get(context);

            if (!documentIds.Any())
            {
                throw new ArgumentException("DocumentIdList cannot be empty");
            }

            var result = await this.verificationService.GetVerifiedDataAsync(documentIds, token, serviceUrl);

            return (asyncActivityContext) =>
            {
                VerifiedDataResult.Set(asyncActivityContext, result);
            };
        }
    }
}
