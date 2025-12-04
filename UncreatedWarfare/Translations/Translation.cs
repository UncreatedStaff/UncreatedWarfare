using System;
using System.ComponentModel;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Translations.Util;

namespace Uncreated.Warfare.Translations;

/// <summary>
/// Base class for all translations. Also represents a translation with no arguments.
/// </summary>
/// <remarks>Signs should use <see cref="SignTranslation"/> instead.</remarks>
public class Translation : IDisposable
{
    private readonly string _defaultText;

    /// <summary>
    /// If the default language is using the same value as the original translation from code.
    /// </summary>
    public bool HasDefaultValue { get; private set; }

    /// <summary>
    /// The original value specified in code.
    /// </summary>
    public TranslationValue Original { get; private set; } = null!;

    /// <summary>
    /// The translation key, usually equal to the field/property name.
    /// </summary>
    public string Key { get; private set; }

    /// <summary>
    /// Information about the translation specified in the <see cref="TranslationDataAttribute"/>.
    /// </summary>
    public TranslationData Data { get; private set; }

    /// <summary>
    /// The collection this translation is a member of.
    /// </summary>
    public TranslationCollection Collection { get; private set; } = null!;

    /// <summary>
    /// Translation value table storing different language implementations for this translation.
    /// </summary>
    public SharedTranslationDictionary Table { get; private set; } = null!;

    /// <summary>
    /// Whether or not the translation has been loaded from it's storage.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Formatting options when translating this translation.
    /// </summary>
    public TranslationOptions Options { get; }

    /// <summary>
    /// The number of {n} arguments in this translation.
    /// </summary>
    public virtual int ArgumentCount => 0;
    
    /// <summary>
    /// Linked implementation of <see cref="ITranslationService"/>.
    /// </summary>
    public ITranslationService TranslationService { get; private set; } = null!;

    /// <summary>
    /// Linked instance of <see cref="Uncreated.Warfare.Translations.Languages.LanguageService"/>.
    /// </summary>
    public LanguageService LanguageService { get; private set; } = null!;

    public Translation(string defaultValue, TranslationOptions options = default)
    {
        _defaultText = defaultValue;
        Key = string.Empty;
        Options = options;
        HasDefaultValue = true;
    }

    // for tests
    internal Translation(string defaultValue, TranslationCollection collection, ITranslationService translationService, TranslationOptions options = default)
    {
        _defaultText = defaultValue;
        Key = string.Empty;
        Options = options;
        Collection = collection;
        TranslationService = translationService;
        HasDefaultValue = true;
    }

    /// <summary>
    /// Get the value-set for a given language from the table. Defaults to the default language if <see langword="null"/>.
    /// </summary>
    public TranslationValue GetValueForLanguage(LanguageInfo? language)
    {
        AssertInitialized();

        string langCode = language?.Code ?? LanguageService.DefaultCultureCode;

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
        ITranslationService translationService,
        TranslationData data)
    {
        Key = key;
        Data = data;

        LanguageService = languageService;
        TranslationService = translationService;
        Original = new TranslationValue(languageService.GetDefaultLanguage(), _defaultText, this);
        Collection = collection;
        Table = new SharedTranslationDictionary(this, underlyingTable);
        HasDefaultValue = Table.TryGetValue(languageService.DefaultLanguageCode, out TranslationValue value)
                          && string.Equals(value.RawValue, _defaultText, StringComparison.Ordinal);
        IsInitialized = true;
        Table[languageService.DefaultCultureCode] = Original;
    }

    /// <summary>
    /// Returns the format of a given argument index.
    /// </summary>
    public virtual ArgumentFormat GetArgumentFormat(int index)
    {
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <summary>
    /// Overridden in generic translation classes to cast the arguments without reflection.
    /// </summary>
    /// <remarks><paramref name="formattingParameters"/> have already been verified to be the correct type by this point.</remarks>
    protected virtual string UnsafeTranslateIntl(in TranslationArguments arguments, object?[] formattingParameters)
    {
        return arguments.ValueSet.GetValueString(arguments.UseIMGUI, arguments.UseUncoloredTranslation, (arguments.Options & TranslationOptions.ForTerminal) != 0);
    }

    /// <summary>
    /// Checks if the translation has a value specifically for <paramref name="language"/>.
    /// </summary>
    public bool HasLanguage(LanguageInfo? language)
    {
        string langCode = language?.Code ?? LanguageService.DefaultCultureCode;

        return Table.ContainsKey(langCode);
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

    /// <summary>
    /// Translate using an object[] instead of type-safe generics.
    /// </summary>
    /// <exception cref="ArgumentException">One of the values wasn't the right type.</exception>
    public string UnsafeTranslate(in TranslationArguments arguments, object?[] formatting)
    {
        Type[] genericArguments = GetType().GetGenericArguments();
        if (genericArguments.Length == 0)
        {
            return arguments.ValueSet.GetValueString(arguments.UseIMGUI, arguments.UseUncoloredTranslation, (arguments.Options & TranslationOptions.ForTerminal) != 0);
        }

        // resize formatting to correct length
        if (formatting == null)
            formatting = new object[genericArguments.Length];
        else if (genericArguments.Length > formatting.Length)
            Array.Resize(ref formatting, genericArguments.Length);
        
        // convert arguments
        for (int i = 0; i < genericArguments.Length; ++i)
        {
            object? v = formatting[i];
            Type expectedType = genericArguments[i];
            if (v == null)
            {
                if (expectedType.IsValueType)
                {
                    throw new ArgumentException($"Formatting argument at index {i} is null and its generic type is a value type!", $"{nameof(formatting)}[{i}]");
                }

                continue;
            }

            Type suppliedType = v.GetType();
            if (expectedType.IsAssignableFrom(suppliedType))
                continue;

            if (expectedType == typeof(string))
            {
                ArgumentFormat argFmt = GetArgumentFormat(i);
                ValueFormatParameters parameters = new ValueFormatParameters(-1, in arguments, in argFmt, null, 0);
                formatting[i] = TranslationService.ValueFormatter.Format(genericArguments[i], in parameters);
            }

            try
            {
                formatting[i] = Convert.ChangeType(v, expectedType);
            }
            catch (Exception ex)
            {
                try
                {
                    TypeConverter fromSupplied = TypeDescriptor.GetConverter(suppliedType);
                    if (fromSupplied.CanConvertTo(expectedType))
                    {
                        formatting[i] = fromSupplied.ConvertTo(v, expectedType);
                        continue;
                    }
                }
                catch (NotSupportedException) { }

                try
                {
                    TypeConverter toSupplied = TypeDescriptor.GetConverter(expectedType);
                    if (toSupplied.CanConvertFrom(suppliedType))
                    {
                        formatting[i] = toSupplied.ConvertFrom(v);
                        continue;
                    }
                }
                catch (NotSupportedException) { }

                throw new ArgumentException($"Formatting argument at index {i} is not a type compatable with it's generic type!", $"{nameof(formatting)}[{i}]", ex);
            }
        }

        return UnsafeTranslateIntl(in arguments, formatting);
    }

    void IDisposable.Dispose()
    {
        Table.Clear();
    }
}

/// <summary>
/// A translation that can be referenced by placed signs.
/// </summary>
public class SignTranslation : Translation
{
    /// <summary>
    /// Unique ID which is typed into the sign text (sign_[SignId]) and will be replaced by the value of this translation when being sent to clients.
    /// </summary>
    public string SignId { get; }
    public SignTranslation(string signId, string defaultValue) : base(defaultValue, TranslationOptions.TMProSign)
    {
        SignId = signId;
    }
}