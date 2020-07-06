using DocAcquire.DataContracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DocAcquire
{
    internal interface IDocumentUploadService
    {
        Task<FileUploadResponse> UploadAsync(AttachmentItem attachment, string token, string baseUrl);
    }
}