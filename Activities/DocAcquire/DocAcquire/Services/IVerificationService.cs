using System.Collections.Generic;
using System.Threading.Tasks;
using DocAcquire.DataContracts;

namespace DocAcquire
{
    public interface IVerificationService
    {
        Task<List<DocumentExtractResponse>> GetVerifiedDataAsync(int[] documentIds, string token, string baseUrl);
    }
}