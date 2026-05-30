using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace JobCandidates.Services
{
    public class OpenAiEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string Model = "text-embedding-3-small";

        public OpenAiEmbeddingService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["OpenAI:ApiKey"]
                ?? throw new InvalidOperationException("OpenAI:ApiKey is missing.");
        }

        public async Task<double[]> GetEmbeddingAsync(string text)
        {
            if (text.Length > 4000)
                text = text[..4000];

            var requestBody = new
            {
                input = text,
                model = Model
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);

            return doc.RootElement
                .GetProperty("data")[0]
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(e => e.GetDouble())
                .ToArray();
        }
    }
}