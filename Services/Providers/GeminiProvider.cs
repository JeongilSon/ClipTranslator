using System.Net.Http.Json;
using System.Text.Json;
using ClipTranslator.Models;

namespace ClipTranslator.Services.Providers;

public class GeminiProvider : ITranslationProvider
{
    public string Name => "Gemini";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiProvider(string apiKey, string model = "gemini-2.0-flash")
    {
        _apiKey = apiKey;
        _model = model;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
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
            var response = await _httpClient.PostAsJsonAsync(url, request, ct);
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
