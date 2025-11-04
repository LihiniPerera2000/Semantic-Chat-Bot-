//using System.Text.Json;

//namespace SemanticBotStar.Services
//{
//    public class EmbeddingService
//    {
//    }
//}


using System.Net.Http.Json;
using System.Text.Json;

namespace SemanticBotStar.Services;

public interface IEmbeddingService
{
    Task<float[]> GetEmbedding(string text);
}

public class HuggingFaceEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _client;
    private readonly string _hfKey;

    public HuggingFaceEmbeddingService()
    {
        _hfKey = Environment.GetEnvironmentVariable("HF_TOKEN")
                 ?? throw new InvalidOperationException("HF_TOKEN missing.");
        _client = new HttpClient();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _hfKey);
    }
    public async Task<float[]> GetEmbedding(string text)
    {
        var response = await _client.PostAsJsonAsync("https://router.huggingface.co/hf-inference/models/BAAI/bge-large-en-v1.5/pipeline/feature-extraction", new { inputs = text });
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Try to parse as flat array first
        try
        {
            var flat = JsonSerializer.Deserialize<List<float>>(content);
            if (flat != null && flat.Count > 0)
                return flat.ToArray();
        }
        catch { }

        // If not flat, try nested array
        try
        {
            var nested = JsonSerializer.Deserialize<List<List<float>>>(content);
            if (nested != null && nested.Count > 0)
                return nested[0].ToArray();
        }
        catch { }

        throw new Exception("Failed to parse embedding response: " + content);
    }

}
