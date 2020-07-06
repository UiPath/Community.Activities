using DocAcquire.DataContracts;
using DocAcquire.Helpers;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DocAcquire
{
    public class DocumentExtractionService : IDocumentExtractionService
    {
        public async Task<DocumentExtractResponse> ExtractAsync(AttachmentItem attachment, string token, string baseUrl)
        {
            var apiUrl = "api/documentextraction/External/extract";
            var requestContent = new MultipartFormDataContent();
            var imageContent = new ByteArrayContent(attachment.Content);
            requestContent.Add(imageContent, "files", attachment.Name);
            requestContent.Add(new StringContent(attachment.Name), "\"name\"");

            var httpClient = CustomHttpClient.GetInstance(baseUrl);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("enctype", "multipart/form-data");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + token);

            var result = await httpClient.PostAsync(apiUrl, requestContent);
            result.EnsureSuccessStatusCode();
            return await result.Content.ReadAsAsync<DocumentExtractResponse>();
        }
    }
}