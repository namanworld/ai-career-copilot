using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AiCareerCopilot.Api.Models;

namespace AiCareerCopilot.Api.Services;

public interface IGeminiClientService
{
    Task<T> GenerateStructuredOutputAsync<T>(string systemPrompt, string userPrompt);
    Task<string> GenerateChatResponseAsync(string systemPrompt, string userPrompt);
    Task<float[]> GetEmbeddingAsync(string text, string taskType = "RETRIEVAL_DOCUMENT");
}

public class GeminiClientService : IGeminiClientService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelName;
    private readonly string _embeddingModelName;

    public GeminiClientService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GEMINI_API_KEY"] 
            ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") 
            ?? throw new InvalidOperationException("GEMINI_API_KEY is not configured in appsettings.json or environment variables.");

        _modelName = configuration["GEMINI_MODEL"] 
            ?? Environment.GetEnvironmentVariable("GEMINI_MODEL") 
            ?? "models/gemini-3.6-flash";

        _embeddingModelName = configuration["EMBEDDING_MODEL"] 
            ?? Environment.GetEnvironmentVariable("EMBEDDING_MODEL") 
            ?? "models/gemini-embedding-001";
    }


    public async Task<T> GenerateStructuredOutputAsync<T>(string systemPrompt, string userPrompt)
    {
        var cleanModel = _modelName.Replace("models/", "");
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{cleanModel}:generateContent?key={_apiKey}";

        var requestBody = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = $"{systemPrompt}\nYou MUST respond ONLY with a single valid JSON object. Do not include markdown ticks or text outside the JSON." } }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = userPrompt } }
                }
            },
            generationConfig = new
            {
                response_mime_type = "application/json",
                temperature = 0.2
            }
        };

        var jsonPayload = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Gemini API error ({response.StatusCode}): {responseString}");
        }

        using var doc = JsonDocument.Parse(responseString);
        var candidates = doc.RootElement.GetProperty("candidates");
        if (candidates.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Gemini returned no candidates.");
        }

        var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Gemini returned empty text.");
        }

        // Clean any accidental markdown fence
        var cleanJson = Regex.Replace(text.Trim(), @"^```(?:json)?\s*", "", RegexOptions.IgnoreCase);
        cleanJson = Regex.Replace(cleanJson, @"\s*```$", "");

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<T>(cleanJson, options);
        if (result == null)
        {
            throw new InvalidOperationException("Failed to deserialize Gemini structured JSON output.");
        }

        return result;
    }

    public async Task<string> GenerateChatResponseAsync(string systemPrompt, string userPrompt)
    {
        var cleanModel = _modelName.Replace("models/", "");
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{cleanModel}:generateContent?key={_apiKey}";

        var requestBody = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = systemPrompt } }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = userPrompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.2
            }
        };

        var jsonPayload = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Gemini API error ({response.StatusCode}): {responseString}");
        }

        using var doc = JsonDocument.Parse(responseString);
        var candidates = doc.RootElement.GetProperty("candidates");
        if (candidates.GetArrayLength() == 0) return string.Empty;

        return candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? string.Empty;
    }

    public async Task<float[]> GetEmbeddingAsync(string text, string taskType = "RETRIEVAL_DOCUMENT")
    {
        var cleanModel = _embeddingModelName.Replace("models/", "");
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{cleanModel}:embedContent?key={_apiKey}";

        var requestBody = new
        {
            model = $"models/{cleanModel}",
            content = new
            {
                parts = new[] { new { text = text } }
            },
            taskType = taskType
        };

        var jsonPayload = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Gemini Embedding error ({response.StatusCode}): {responseString}");
        }

        using var doc = JsonDocument.Parse(responseString);
        var embeddingElement = doc.RootElement.GetProperty("embedding").GetProperty("values");

        var values = new List<float>();
        foreach (var item in embeddingElement.EnumerateArray())
        {
            values.Add(item.GetSingle());
        }

        return values.ToArray();
    }
}
