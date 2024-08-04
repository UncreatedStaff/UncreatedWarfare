using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Translations.Storage;

namespace Uncreated.Warfare.Translations;

public abstract class TranslationCollection
{
#nullable disable
    private ILogger _logger;
    private int _isInitialized;
    private Dictionary<string, Translation> _translations;
    private Dictionary<TranslationLanguageKey, TranslationValue> _valueTable;
    private ICachableLanguageDataStore _languageDataStore;

    public abstract ITranslationStorage Storage { get; }
    public ITranslationService TranslationService { get; private set; }
    public LanguageService LanguageService { get; private set; }
    public ITranslationValueFormatter ValueFormatter { get; private set; }
    public IReadOnlyDictionary<string, Translation> Translations { get; private set; }
#nullable restore

    /// <summary>
    /// Initialize if it hasn't already.
    /// </summary>
    /// <remarks>I have it set up like this to make thread-safety easier for service injections.</remarks>
    internal bool TryInitialize(ITranslationService translationService, IServiceProvider serviceProvider)
    {
        if (Interlocked.Exchange(ref _isInitialized, 1) != 0)
            return false;

        Initialize(translationService, serviceProvider);
        return true;
    }

    protected virtual void Initialize(ITranslationService translationService, IServiceProvider serviceProvider)
    {
        _translations = new Dictionary<string, Translation>(32);
        _valueTable = new Dictionary<TranslationLanguageKey, TranslationValue>(64, TranslationLanguageKey.EqualityComparer);

        LanguageService = serviceProvider.GetRequiredService<LanguageService>();
        TranslationService = translationService;
        Translations = new ReadOnlyDictionary<string, Translation>(_translations);

        _languageDataStore = serviceProvider.GetRequiredService<ICachableLanguageDataStore>();
        ValueFormatter = serviceProvider.GetRequiredService<ITranslationValueFormatter>();

        // get logger for parent collection type
        _logger = (ILogger)serviceProvider
            .GetRequiredService(typeof(ILogger<>)
                .MakeGenericType(GetType())
            );

        List<Translation> translations = FindTranslationsInMembers();

        // each field and property in the collection's type.
        foreach (Translation translation in translations)
        {
            _translations.Add(translation.Key, translation);
        }

        Reload();
    }

    public void Reload()
    {
        IReadOnlyDictionary<TranslationLanguageKey, string> translationData = Storage.Load();

        foreach (KeyValuePair<TranslationLanguageKey, string> translation in translationData)
        {
            if (!_translations.TryGetValue(translation.Key.TranslationKey, out Translation translationMember))
            {
                _logger.LogWarning("Unknown translation in collection {0}.", Accessor.Formatter.Format(GetType()));
                continue;
            }

            LanguageInfo? language = _languageDataStore.GetInfoCached(translation.Key.LanguageCode);

            if (language is null)
            {
                _logger.LogWarning("Unknown language {0} in collection {1}.", translation.Key.LanguageCode, Accessor.Formatter.Format(GetType()));
            }

            translationMember.UpdateValue(translation.Value, language ?? new LanguageInfo(translation.Key.LanguageCode));
        }
    }

    private List<Translation> FindTranslationsInMembers()
    {
        Type type = GetType();
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        List<Translation> discoveredTranslations = new List<Translation>();

        // each field and property in the collection's type.
        foreach (MemberInfo member in fields
                     .Concat<MemberInfo>(properties)
                     .Where(member => typeof(Translation).IsAssignableFrom(member.GetMemberType())))
        {
            if (member.IsIgnored())
                continue;

            Translation? translation = member switch
            {
                FieldInfo field => (Translation?)field.GetValue(this),
                PropertyInfo property => (Translation?)property.GetValue(this),
                _ => null
            };

            TranslationDataAttribute? data = member.GetAttributeSafe<TranslationDataAttribute>();
            string key = data?.Key ?? member.Name;

            if (translation == null)
            {
                _logger.LogError($"Translation '{key}' in collection '{type.Name}' is null.");
                continue;
            }

            translation.Initialize(key,
                // storing this as readonly so it's obvious it shouldn't be modified outside the translation class
                _valueTable,
                this,
                new TranslationData(
                    data?.Description,
                    data?.Parameters,
                    data == null || data.IsPriorityTranslation)
            );

            discoveredTranslations.Add(translation);
        }

        return discoveredTranslations;
    }

    public override string ToString() => Storage.ToString();
}
