using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Translations.ValueFormatters;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Translations;
public class TranslationValueFormatter : ITranslationValueFormatter
{
    private readonly IServiceProvider _serviceProvider;
    private readonly LanguageService _languageService;
    private readonly Type _enumFormatter;

    private const string NullColorTMPro = "<#569cd6><b>null</b></color>";
    private const string NullColorUnity = "<color=#569cd6><b>null</b></color>";
    private const string NullNoColor = "null";

    private readonly ConcurrentDictionary<Type, object> _valueFormatters = new ConcurrentDictionary<Type, object>();
    public TranslationValueFormatter(IServiceProvider serviceProvider, IConfiguration systemConfig)
    {
        _enumFormatter = ContextualTypeResolver.ResolveType(systemConfig["translations:enum_formatter_type"], typeof(IEnumFormatter))
            ?? throw new InvalidOperationException("No enum formatter configured in 'enum_formatter_type'.");

        _serviceProvider = serviceProvider;
        _languageService = serviceProvider.GetRequiredService<LanguageService>();
    }

    // in order of priority. can either be a type or the object itself for singletons
    // types with one open generic argument are filled by the type being formatted
    private readonly object[] _valueFormatterTypes =
    {
        typeof(FormattableValueFormatter<>),
        new ToStringValueFormatter()
    };

    /// <inheritdoc />
    public string Format<T>(T? value, in ValueFormatParameters parameters)
    {
        string formattedValue = FormatIntl(value, in parameters);

        IArgumentAddon[] addons = parameters.Format.FormatAddons;
        for (int i = 0; i < addons.Length; ++i)
        {
            formattedValue = addons[i].ApplyAddon(formattedValue, in parameters);
        }

        return formattedValue;
    }

    /// <inheritdoc />
    public string Format(object? value, in ValueFormatParameters parameters, Type? formatType = null)
    {
        string formattedValue = FormatIntl(value, in parameters, formatType);

        IArgumentAddon[] addons = parameters.Format.FormatAddons;
        for (int i = 0; i < addons.Length; ++i)
        {
            formattedValue = addons[i].ApplyAddon(formattedValue, in parameters);
        }

        return formattedValue;
    }

    public string FormatEnum<TEnum>(TEnum value, LanguageInfo? language) where TEnum : unmanaged, Enum
    {
        return ((IEnumFormatter<TEnum>)GetValueFormatter<TEnum>()).GetValue(value, language ?? _languageService.GetDefaultLanguage());
    }

    public string FormatEnum(object value, Type enumType, LanguageInfo? language)
    {
        return ((IEnumFormatter)GetValueFormatter(enumType)).GetValue(value, language ?? _languageService.GetDefaultLanguage());
    }

    public string FormatEnumName<TEnum>(LanguageInfo? language) where TEnum : unmanaged, Enum
    {
        return ((IEnumFormatter<TEnum>)GetValueFormatter<TEnum>()).GetName(language ?? _languageService.GetDefaultLanguage());
    }

    public string FormatEnumName(Type enumType, LanguageInfo? language)
    {
        return ((IEnumFormatter)GetValueFormatter(enumType)).GetName(language ?? _languageService.GetDefaultLanguage());
    }

    private string FormatIntl<T>(T? value, in ValueFormatParameters parameters)
    {
        if (Equals(value, null))
        {
            return FormatNull(in parameters);
        }

        if (value is ITranslationArgument preDefined)
        {
            return preDefined.Translate(this, in parameters);
        }

        IValueFormatter<T> valueFormatter = GetValueFormatter<T>();

        return valueFormatter.Format(value, in parameters);
    }
    
    private string FormatIntl(object? value, in ValueFormatParameters parameters, Type? formatType)
    {
        if (Equals(value, null))
        {
            return FormatNull(in parameters);
        }

        if (value is ITranslationArgument preDefined)
        {
            return preDefined.Translate(this, in parameters);
        }

        formatType ??= value.GetType() ?? typeof(object);
        IValueFormatter valueFormatter = GetValueFormatter(formatType);

        return valueFormatter.Format(value, in parameters);
    }

    private IValueFormatter<T> GetValueFormatter<T>() => (IValueFormatter<T>)GetValueFormatter(typeof(T));
    private IValueFormatter GetValueFormatter(Type type)
    {
        return (IValueFormatter)_valueFormatters.GetOrAdd(type, type =>
        {
            if (type.IsEnum)
            {
                return ActivatorUtilities.CreateInstance(_serviceProvider, typeof(PropertiesEnumValueFormatter<>).MakeGenericType(type));
            }

            Type lookingForFormatterType = typeof(IValueFormatter<>).MakeGenericType(type);
            foreach (object valueFormatter in _valueFormatterTypes)
            {
                if (valueFormatter is not Type actualType)
                {
                    if (!lookingForFormatterType.IsInstanceOfType(valueFormatter))
                        continue;

                    return valueFormatter;
                }

                if (actualType.IsGenericTypeDefinition)
                {
                    Type[] genericArgs = actualType.GetGenericArguments();
                    if (genericArgs.Length != 1)
                        continue;

                    // this is 100x easier and cleaner than checking every single generic type constraint manually, sue me
                    try
                    {
                        actualType = actualType.MakeGenericType(type);
                    }
                    catch (ArgumentException)
                    {
                        continue;
                    }
                }

                if (!lookingForFormatterType.IsAssignableFrom(actualType))
                    continue;

                return ActivatorUtilities.CreateInstance(_serviceProvider, actualType);
            }

            return new ToStringValueFormatter();
        });
    }

    private static string FormatNull(in ValueFormatParameters parameters)
    {
        if ((parameters.Options & TranslationOptions.TranslateWithUnityRichText) != 0)
            return NullColorUnity;

        return (parameters.Options & TranslationOptions.NoRichText) != 0 ? NullNoColor : NullColorTMPro;
    }
}