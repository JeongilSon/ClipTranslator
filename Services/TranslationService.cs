using ClipTranslator.Models;
using ClipTranslator.Services.Providers;

namespace ClipTranslator.Services;

/// <summary>
/// Provider 관리 및 번역 요청을 통합 처리하는 서비스.
/// </summary>
public class TranslationService : IDisposable
{
    private ITranslationProvider? _currentProvider;
    private readonly TranslationHistory _history = new();
    private CancellationTokenSource? _cts;

    public TranslationHistory History => _history;

    /// <summary>
    /// 현재 설정에 맞는 Provider를 생성/교체한다.
    /// </summary>
    public void Configure(AppSettings settings)
    {
        _currentProvider = settings.SelectedProvider switch
        {
            ApiProviderType.Claude => new ClaudeProvider(settings.ClaudeApiKey, settings.ClaudeModel),
            ApiProviderType.OpenAI => new OpenAIProvider(settings.OpenAIApiKey, settings.OpenAIModel),
            ApiProviderType.Gemini => new GeminiProvider(settings.GeminiApiKey, settings.GeminiModel),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    /// <summary>
    /// 비동기로 번역을 수행한다. 이전 요청이 진행 중이면 취소한다.
    /// </summary>
    public async Task<TranslationResult> TranslateAsync(string text, string targetLanguage)
    {
        if (_currentProvider is null)
        {
            return new TranslationResult
            {
                OriginalText = text,
                ErrorMessage = "번역 Provider가 설정되지 않았습니다. 설정에서 API 키를 입력해 주세요."
            };
        }

        // 이전 요청 취소
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        try
        {
            var result = await _currentProvider.TranslateAsync(text, targetLanguage, _cts.Token);
            if (result.IsSuccess)
            {
                _history.Add(result);
            }
            return result;
        }
        catch (OperationCanceledException)
        {
            return new TranslationResult
            {
                OriginalText = text,
                ErrorMessage = "번역이 취소되었습니다."
            };
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}
