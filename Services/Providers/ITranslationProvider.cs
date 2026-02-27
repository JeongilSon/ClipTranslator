using ClipTranslator.Models;

namespace ClipTranslator.Services.Providers;

public interface ITranslationProvider
{
    string Name { get; }
    Task<TranslationResult> TranslateAsync(string text, string targetLanguage, CancellationToken ct = default);
}
