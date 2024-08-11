using System;
using System.Collections.Generic;
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
    [ThreadStatic]
    private static List<string>? _pluralBuffer;

    private readonly string _defaultText;
    public TranslationValue Original { get; private set; }
    public string Key { get; private set; }
    public TranslationData Data { get; private set; }
    public TranslationCollection Collection { get; private set; } = null!;
    public SharedTranslationDictionary Table { get; private set; } = null!;
    public bool IsInitialized { get; private set; }
    public TranslationOptions Options { get; }
    public virtual int ArgumentCount => 0;

    public ITranslationService TranslationService { get; private set; } = null!;
    public LanguageService LanguageService { get; private set; } = null!;

    public Translation(string defaultValue, TranslationOptions options = default)
    {
        _defaultText = defaultValue;
        Key = string.Empty;
        Options = options;
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
        return arguments.ValueSet.GetValueString(arguments.UseIMGUI, arguments.UseUncoloredTranslation);
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
    /// Applies all pluralizers using the given language.
    /// </summary>
    protected ReadOnlySpan<char> ApplyPluralizers(scoped in TranslationArguments args, ArgumentSpan[] pluralizers, int argumentOffset, int argCt, Func<int, object?> accessor)
    {
        Span<int> indices = stackalloc int[pluralizers.Length];
        int ct = 0;
        int first = -1;
        for (int i = 0; i < pluralizers.Length; ++i)
        {
            ref ArgumentSpan argSpan = ref pluralizers[i];
            if (argSpan.Argument >= argCt)
            {
                indices[i] = -1;
                continue;
            }

            object? argValue = accessor(argSpan.Argument);
            if (argValue is IConvertible conv && !TranslationPluralizations.IsOne(conv))
            {
                if (first == -1)
                    first = i;
                ++ct;
                continue;
            }

            indices[i] = -1;
        }

        if (ct == 0)
            return args.PreformattedValue;

        // todo disable 1 override for test
        if (ct == 1)
        {
            ref ArgumentSpan span = ref pluralizers[first];
            ReadOnlySpan<char> word = args.PreformattedValue.Slice(span.StartIndex + argumentOffset, span.Length);
            string pluralWord = TranslationPluralizations.Pluralize(word, args.Language);
            return TranslationArgumentModifiers.ReplaceModifiers(args.PreformattedValue, pluralWord, indices, pluralizers, argumentOffset);
        }

        List<string> pluralBuffer = _pluralBuffer ??= new List<string>(ct);

        if (pluralBuffer.Capacity < ct)
            pluralBuffer.Capacity = ct;

        try
        {
            int spanCt = pluralizers.Length;
            int totalSize = 0;
            for (int i = 0; i < spanCt; ++i)
            {
                ref ArgumentSpan span = ref pluralizers[i];
                ref int index = ref indices[i];
                if (index < 0)
                    continue;

                ReadOnlySpan<char> word = args.PreformattedValue.Slice(span.StartIndex + argumentOffset, span.Length);
                string pluralWord = TranslationPluralizations.Pluralize(word, args.Language);
                index = totalSize;
                totalSize += pluralWord.Length;
                pluralBuffer.Add(pluralWord);
            }

            Span<char> pluralWordsBuffer = stackalloc char[totalSize];
            int bufferIndex = 0;
            for (int i = 0; i < pluralBuffer.Count; ++i)
            {
                string pluralWord = pluralBuffer[i];
                pluralWord.AsSpan().CopyTo(pluralWordsBuffer[bufferIndex..]);
                bufferIndex += pluralWord.Length;
            }

            return TranslationArgumentModifiers.ReplaceModifiers(args.PreformattedValue, pluralWordsBuffer, indices, pluralizers, argumentOffset);
        }
        finally
        {
            pluralBuffer.Clear();
        }
    }

    /// <summary>
    /// Translate using an object[] instead of type-safe generics.
    /// </summary>
    /// <exception cref="ArgumentException">One of the values wasn't the right type.</exception>
    public string TranslateUnsafe(in TranslationArguments arguments, object?[] formatting)
    {
        Type[] genericArguments = GetType().GetGenericArguments();
        if (genericArguments.Length == 0)
        {
            return arguments.ValueSet.GetValueString(arguments.UseIMGUI, arguments.UseUncoloredTranslation);
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

public class SignTranslation : Translation
{
    public string SignId { get; }
    public SignTranslation(string signId, string defaultValue) : base(defaultValue, TranslationOptions.TMProSign)
    {
        SignId = signId;
    }
}