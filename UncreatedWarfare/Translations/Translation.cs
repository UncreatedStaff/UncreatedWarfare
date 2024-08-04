using System;
using System.Collections.Generic;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Translations;
public class Translation : IDisposable
{
    private readonly string _defaultText;
    public TranslationValue Original { get; private set; }
    public string Key { get; private set; }
    public TranslationData Data { get; private set; }
    public TranslationCollection Collection { get; private set; } = null!;
    public SharedTranslationDictionary Table { get; private set; } = null!;
    public bool IsInitialized { get; private set; }
    public TranslationOptions Options { get; }
    public virtual int ArgumentCount => 0;
    public Translation(string defaultValue, TranslationOptions options = default)
    {
        _defaultText = defaultValue;
        Key = string.Empty;
        Options = options;
    }
    public TranslationValue GetValueForLanguage(LanguageInfo? language)
    {
        AssertInitialized();

        string langCode = language?.Code ?? TranslationService.DefaultLanguage;

        if (Table.TryGetValue(langCode, out TranslationValue value))
            return value;

        if (language is not { FallbackTranslationLanguageCode: { } fallbackLangCode }
            || !Table.TryGetValue(fallbackLangCode, out value))
        {
            return Original;
        }
        
        return value;
    }
    internal virtual void Initialize(
        string key,
        IDictionary<TranslationLanguageKey, TranslationValue> underlyingTable,
        TranslationCollection collection,
        LanguageService languageService,
        TranslationData data)
    {
        Key = key;
        Data = data;

        Original = new TranslationValue(languageService.GetDefaultLanguage(), _defaultText, this);
        Collection = collection;
        Table = new SharedTranslationDictionary(this, underlyingTable);
        IsInitialized = true;
        Table[TranslationService.DefaultLanguage] = Original;
    }
    protected internal void AssertInitialized()
    {
        if (!IsInitialized)
            throw new InvalidOperationException("This translation has not been initialized.");
    }
    internal void UpdateValue(string value, LanguageInfo language)
    {
        AssertInitialized();
        Table.AddOrUpdate(new TranslationValue(language, value, this));
    }
    public void Dispose()
    {
        Table.Clear();
    }
}

public class SignTranslation : Translation
{
    public string SignId { get; }
    public SignTranslation(string signId, string defaultValue) : base(defaultValue, TranslationOptions.TMProSign)
    {
        SignId = signId;
    }
}