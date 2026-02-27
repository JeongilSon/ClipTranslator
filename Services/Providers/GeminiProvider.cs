using System.Net.Http.Json;
using System.Text.Json;
using ClipTranslator.Models;

namespace ClipTranslator.Services.Providers;

public class GeminiProvider : ITranslationProvider
{
    public string Name => "Gemini";

    private static readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("https://generativelanguage.googleapis.com/"),
        Timeout = TimeSpan.FromSeconds(30)
    };

    private readonly string _apiKey;
    private readonly string _model;

    public GeminiProvider(string apiKey, string model = "gemini-2.0-flash")
    {
        _apiKey = apiKey;
        _model = model;
    }

    public async Task<TranslationResult> TranslateAsync(string text, string targetLanguage, CancellationToken ct = default)
    {
        var result = new TranslationResult
        {
            OriginalText = text,
            ProviderName = Name,
            TargetLanguage = targetLanguage
        };

        try
        {
            var systemPrompt = PromptBuilder.BuildTranslationPrompt(targetLanguage);
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
                        parts = new[] { new { text } }
                    }
                },
                generationConfig = new
                {
                    maxOutputTokens = 2048,
                    temperature = 0.3
                }
            };

            var url = $"v1beta/models/{_model}:generateContent?key={_apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, requestBody, ct);
            var json = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                result.ErrorMessage = $"API 오류 ({response.StatusCode}): {json}";
                return result;
            }

            using var doc = JsonDocument.Parse(json);
            var content = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            result.TranslatedText = content?.Trim() ?? string.Empty;
            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Gemini API 호출 실패: {ex.Message}";
        }

        return result;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
