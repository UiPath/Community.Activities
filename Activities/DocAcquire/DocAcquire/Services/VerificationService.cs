using DocAcquire.DataContracts;
using DocAcquire.Helpers;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DocAcquire
{
    public class VerificationService : IVerificationService
    {
        public async Task<List<DocumentExtractResponse>> GetVerifiedDataAsync(int[] documentIds, string token, string baseUrl)
        {
            var apiUrl = "api/External/Documents/Verified";

            var jsonString = JsonConvert.SerializeObject(documentIds);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            var httpClient = CustomHttpClient.GetInstance(baseUrl);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + token);

            var result = await httpClient.PostAsync(apiUrl, content);
            result.EnsureSuccessStatusCode();
            return await result.Content.ReadAsAsync<List<DocumentExtractResponse>>();
        }
    }
}