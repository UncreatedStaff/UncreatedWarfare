namespace Uncreated.Warfare.Translations;
public class TranslationInjection<T> where T : TranslationCollection, new()
{
    public T Value { get; }
    
    public TranslationInjection(ITranslationService service)
    {
        Value = service.Get<T>();
    }

    public TranslationInjection(T value)
    {
        Value = value;
    }
}