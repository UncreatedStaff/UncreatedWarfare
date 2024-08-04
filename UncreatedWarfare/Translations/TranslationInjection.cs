namespace Uncreated.Warfare.Translations;
public readonly struct TranslationInjection<T> where T : TranslationCollection, new()
{
    public T Value { get; }
    public TranslationInjection(ITranslationService service)
    {
        Value = service.Get<T>();
    }
}