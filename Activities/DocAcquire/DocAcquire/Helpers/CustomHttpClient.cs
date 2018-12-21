using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace DocAcquire.Helpers
{
    public class CustomHttpClient
    {
        private static HttpClient instance;

        private static HttpClientHandler GetClientHandler()
        {
            return new HttpClientHandler
            {
                UseProxy = false,
                UseDefaultCredentials = true
            };
        }

        public static HttpClient GetInstance(string baseUrl)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentNullException("baseUrl");
            }
            
            if (instance == null)
            {
                var clientHandler = GetClientHandler();
                instance = new HttpClient(clientHandler)
                {
                    BaseAddress = new Uri(baseUrl)
                };
                instance.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
            return instance;
        }
    }
}
