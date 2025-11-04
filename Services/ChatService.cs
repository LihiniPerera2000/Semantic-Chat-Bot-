//using System.Text;
//using System.Text.Json;
//using System.Text.Json.Serialization;

//namespace SemanticBotStar.Services
//{
//    public class ChatService
//    {
//    }
//}

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RSemanticBotStar.Services;

public interface IChatService
{
    Task<string> QueryLLM(string prompt);
}

public class ChatService : IChatService
{
    private readonly ILogger<ChatService> _logger;
    private readonly HttpClient _client;
    private readonly string _hfKey;

    public ChatService(ILogger<ChatService> logger)
    {
        _logger = logger;
        _hfKey = Environment.GetEnvironmentVariable("HF_TOKEN")
                 ?? throw new InvalidOperationException("HF_TOKEN not found in environment.");

        _client = new HttpClient();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _hfKey);
    }

    public async Task<string> QueryLLM(string prompt)
    {
        var payload = new
        {
            model = "MiniMaxAI/MiniMax-M2:novita",
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var res = await _client.PostAsync(
            "https://router.huggingface.co/v1/chat/completions",
            new StringContent(json, Encoding.UTF8, "application/json")
        );

        _logger.LogInformation("Payload: {Json}", json);
        var body = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
        {
            _logger.LogError("HF Chat Error ({StatusCode}): {Body}", res.StatusCode, body);
            return $"Error: {res.StatusCode}";
        }

        using var doc = JsonDocument.Parse(body);
        var choices = doc.RootElement.GetProperty("choices");
        if (choices.GetArrayLength() == 0)
            return "No choices returned from model.";

        var message = choices[0].GetProperty("message");

        var content = message.TryGetProperty("content", out var c)
            ? c.GetString()
            : "(no content)";

        var reasoning = message.TryGetProperty("reasoning_content", out var r)
            ? r.GetString()
            : null;

        if (!string.IsNullOrEmpty(reasoning))
            _logger.LogInformation("💭 Reasoning: {Snippet}...", reasoning[..Math.Min(150, reasoning.Length)]);

        _logger.LogInformation(content);

        return content ?? "No answer.";
    }
}

