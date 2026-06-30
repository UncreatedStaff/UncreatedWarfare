using System;
using System.Collections.Immutable;
using System.Linq;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Translations.Storage;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Translations.ValueFormatters;

/// <summary>
/// Translates enums by localizing each field.
/// </summary>
/// <typeparam name="TEnum">The enum type to translate.</typeparam>
public sealed class EnumValueFormatter<TEnum> : IEnumFormatter<TEnum>, IDisposable
    where TEnum : unmanaged, Enum
{
    private readonly LanguageService _languageService;
    private IEnumTranslationStorage<TEnum>? _storage;
    public IEnumTranslationStorage<TEnum>? Storage => _storage;

    private IReadOnlyDictionary<string, IReadOnlyDictionary<TEnum, string>>? _translations;

    public EnumValueFormatter(ITranslationService translationService)
    {
        _languageService = translationService.LanguageService;
        _storage = ReflectionUtility.GetTypeDescriptorAttribute<TranslatableAttribute>(typeof(TEnum)) != null
            ? translationService.Storage.CreateEnumStorage<TEnum>()
            : null;
    }

    private void HandleTranslationsUpdated(IReadOnlyDictionary<string, IReadOnlyDictionary<TEnum, string>> translations)
    {
        _translations = translations;
    }

    public string GetValue(object value, LanguageInfo language)
    {
        return GetValue((TEnum)value, language);
    }

    public string GetValue(TEnum value, LanguageInfo language)
    {
        CheckTranslations();

        if (_translations.TryGetValue(language.Code, out IReadOnlyDictionary<TEnum, string> table))
        {
            if (table.TryGetValue(value, out string translation))
            {
                return translation;
            }
        }

        if (!language.IsDefault && _translations.TryGetValue(_languageService.DefaultLanguageCode, out table))
        {
            return table.TryGetValue(value, out string translation) ? translation : value.ToString();
        }

        IReadOnlyDictionary<TEnum, string>? dict = _translations.Values.FirstOrDefault();
        return dict != null && dict.TryGetValue(value, out string t) ? t : EnumUtility.GetNameSafe(value);
    }

    public string Format(ITranslationValueFormatter formatter, object value, in ValueFormatParameters parameters)
    {
        return GetValue((TEnum)value, parameters.Language);
    }

    public string Format(ITranslationValueFormatter formatter, TEnum value, in ValueFormatParameters parameters)
    {
        return GetValue(value, parameters.Language);
    }

    [MemberNotNull(nameof(_translations))]
    private void CheckTranslations()
    {
        if (_translations != null)
        {
            return;
        }

        IEnumTranslationStorage<TEnum>? storage = _storage;

        lock (this)
        {
            if (_translations != null)
                return;

            if (storage == null)
            {
                _translations = ImmutableDictionary<string, IReadOnlyDictionary<TEnum, string>>.Empty;
                return;
            }

            storage.OnNeedsUpdating += HandleTranslationsUpdated;
            _translations = storage.Load();
            if (!_translations.TryGetValue(_languageService.DefaultLanguageCode, out IReadOnlyDictionary<TEnum, string> lang))
            {
                lang = ImmutableDictionary<TEnum, string>.Empty;
            }

            storage.Save(_languageService.DefaultLanguageCode, lang, null, options: WriteTranslationsOptions.WriteMissingValues);
        }
    }

    public void Dispose()
    {
        IEnumTranslationStorage<TEnum>? storage = Interlocked.Exchange(ref _storage, null);
        if (storage == null)
            return;

        storage.OnNeedsUpdating -= HandleTranslationsUpdated;

        if (storage is IDisposable disp)
            disp.Dispose();
    }

    void IEnumFormatter.Visit<TVisitor>(ref TVisitor visitor)
    {
        visitor.Accept(this);
    }
}