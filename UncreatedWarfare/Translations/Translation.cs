using System;
using System.Collections.Generic;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations.Collections;

namespace Uncreated.Warfare.Translations;
public class Translation : IDisposable
{
    public TranslationValue Original { get; }
    public string Key { get; private set; }
    public TranslationData Data { get; private set; }
    public TranslationCollection Collection { get; private set; } = null!;
    public SharedTranslationDictionary Table { get; private set; } = null!;
    public bool IsInitialized { get; private set; }
    public TranslationOptions Options { get; }
    public virtual int ArgumentCount => 0;
    public Translation(string defaultValue, LanguageService languageService, TranslationOptions options = default)
    {
        Key = string.Empty;
        Options = options;
        Original = new TranslationValue(
            new LanguageInfo
            {
                // todo get from provider
                Code = TranslationService.DefaultLanguage
            },
            defaultValue, this);
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
        TranslationData data)
    {
        Key = key;
        Data = data;

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