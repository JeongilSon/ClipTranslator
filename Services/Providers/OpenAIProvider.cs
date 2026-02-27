using System.Net.Http.Json;
using System.Text.Json;
using ClipTranslator.Models;

namespace ClipTranslator.Services.Providers;

public class OpenAIProvider : ITranslationProvider
{
    public string Name => "OpenAI";

    private readonly HttpClient _httpClient;
    private readonly string _model;

    public OpenAIProvider(string apiKey, string model = "gpt-4o-mini")
    {
        _model = model;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.openai.com/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
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
            var systemPrompt = BuildSystemPrompt(targetLanguage);
            var request = new
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

            var response = await _httpClient.PostAsJsonAsync("v1/chat/completions", request, ct);
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

    private static string BuildSystemPrompt(string targetLanguage)
    {
        return $"""
            너는 전문 번역가야. 입력 텍스트의 언어를 자동으로 감지해.
            - 한국어가 입력되면 → {targetLanguage}로 번역
            - {targetLanguage}가 입력되면 → 한국어로 번역
            - 그 외 언어가 입력되면 → 한국어로 번역
            메신저 대화체에 맞는 자연스러운 말투로 번역해.
            번역문만 출력하고 다른 설명은 절대 붙이지 마.
            """;
    }
}
