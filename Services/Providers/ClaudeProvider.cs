using System.Net.Http.Json;
using System.Text.Json;
using ClipTranslator.Models;

namespace ClipTranslator.Services.Providers;

public class ClaudeProvider : ITranslationProvider
{
    public string Name => "Claude";

    private static readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("https://api.anthropic.com/"),
        Timeout = TimeSpan.FromSeconds(30)
    };

    private readonly string _apiKey;
    private readonly string _model;

    public ClaudeProvider(string apiKey, string model = "claude-sonnet-4-5-20250929")
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
                max_tokens = 2048,
                system = systemPrompt,
                messages = new[]
                {
                    new { role = "user", content = text }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages");
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
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
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString();

            result.TranslatedText = content?.Trim() ?? string.Empty;
            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Claude API 호출 실패: {ex.Message}";
        }

        return result;
    }

    public void Dispose()
    {
        // static HttpClient이므로 인스턴스 해제 시 HttpClient를 dispose하지 않는다.
        GC.SuppressFinalize(this);
    }
}
