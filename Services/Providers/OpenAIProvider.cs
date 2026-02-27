using System.Net.Http.Json;
using System.Text.Json;
using ClipTranslator.Models;

namespace ClipTranslator.Services.Providers;

public class OpenAIProvider : ITranslationProvider
{
    public string Name => "OpenAI";

    private static readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("https://api.openai.com/"),
        Timeout = TimeSpan.FromSeconds(30)
    };

    private readonly string _apiKey;
    private readonly string _model;

    public OpenAIProvider(string apiKey, string model = "gpt-4o-mini")
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
                model = _model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = text }
                },
                max_tokens = 2048,
                temperature = 0.3
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions");
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Content = JsonContent.Create(requestBody);

            var response = await _httpClient.SendAsync(request, ct);
            var json = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                result.ErrorMessage = $"API 오류 ({response.StatusCode}): {json}";
                return result;
            }

            using var doc = JsonDocument.Parse(json);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            result.TranslatedText = content?.Trim() ?? string.Empty;
            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"OpenAI API 호출 실패: {ex.Message}";
        }

        return result;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
