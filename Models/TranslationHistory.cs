namespace ClipTranslator.Models;

public class TranslationHistory
{
    private readonly List<TranslationResult> _items = new();
    private readonly int _maxItems;

    public TranslationHistory(int maxItems = 100)
    {
        _maxItems = maxItems;
    }

    public IReadOnlyList<TranslationResult> Items => _items.AsReadOnly();

    public void Add(TranslationResult result)
    {
        _items.Insert(0, result);
        if (_items.Count > _maxItems)
            _items.RemoveAt(_items.Count - 1);
    }

    public void Clear() => _items.Clear();
}
