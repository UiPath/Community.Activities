using DocAcquire.DataContracts;
using System.Threading.Tasks;

namespace DocAcquire
{
    public interface IDocumentExtractionService
    {
        Task<DocumentExtractResponse> ExtractAsync(AttachmentItem attachment, string token, string baseUrl);
    }
}