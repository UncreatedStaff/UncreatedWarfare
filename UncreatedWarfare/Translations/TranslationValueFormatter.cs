using StackCleaner;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Translations.ValueFormatters;

namespace Uncreated.Warfare.Translations;

public class TranslationValueFormatter : ITranslationValueFormatter, IDisposable
{
    private const string NullNoColor = "null";
    private const string NullColorUnity = "<color=#569cd6><b>null</b></color>";
    private const string NullColorTMPro = "<#569cd6><b>null</b></color>";
    private const string NullANSI = "\e[94mnull\e[39m";
    private const string NullExtendedANSI = "\e[38;2;86;156;214mnull\e[39m";

    private readonly Lazy<LanguageService> _languageService;
    private readonly Lazy<ITranslationService> _translationService;

    private readonly ConcurrentDictionary<Type, object> _valueFormatters = new ConcurrentDictionary<Type, object>();

    [field: MaybeNull]
    public LanguageService LanguageService => field ??= _languageService.Value;

    [field: MaybeNull]
    public ITranslationService TranslationService => field ??= _translationService.Value;

    public TranslationValueFormatter(
        Lazy<LanguageService> languageService,
        Lazy<ITranslationService> translationService)
    {
        _languageService = languageService;
        _translationService = translationService;
    }

    // In order of priority. can either be a type or the object itself for singletons.
    // Types with one open generic argument are filled by the type being formatted.
    private readonly object[] _valueFormatterTypes =
    {
        new ColorValueFormatter(),
        new AssetValueFormatter(),
        new ReflectionMemberFormatter(),
        typeof(FormattableValueFormatter<>),
        new ToStringValueFormatter()
    };

    public string Format<T>(T? value, in ValueFormatParameters parameters)
    {
        string formattedValue = FormatIntl(value, in parameters);

        IArgumentAddon[]? addons = parameters.Format.FormatAddons;
        TypedReference typeRef = __makeref(value);
        if (addons != null)
        {
            for (int i = 0; i < addons.Length; ++i)
            {
                formattedValue = addons[i].ApplyAddon(this, formattedValue, typeRef, in parameters);
            }
        }

        return formattedValue;
    }

    public string Format(object? value, in ValueFormatParameters parameters, Type? formatType = null)
    {
        string formattedValue = FormatIntl(value, in parameters, formatType);

        IArgumentAddon[]? addons = parameters.Format.FormatAddons;
        TypedReference typeRef = __makeref(value);

        if (addons != null)
        {
            for (int i = 0; i < addons.Length; ++i)
            {
                formattedValue = addons[i].ApplyAddon(this, formattedValue, typeRef, in parameters);
            }
        }

        return formattedValue;
    }

    public string FormatEnum<TEnum>(TEnum value, LanguageInfo? language) where TEnum : unmanaged, Enum
    {
        return ((IEnumFormatter<TEnum>)GetValueFormatter<TEnum>()).GetValue(value, language ?? LanguageService.GetDefaultLanguage());
    }

    public string FormatEnum(object value, Type enumType, LanguageInfo? language)
    {
        return ((IEnumFormatter)GetValueFormatter(enumType)).GetValue(value, language ?? LanguageService.GetDefaultLanguage());
    }

    private string FormatIntl<T>(T? value, in ValueFormatParameters parameters)
    {
        if (!typeof(T).IsValueType)
        {
            object? valBox = value;
            if (Equals(valBox, null) || valBox == DBNull.Value)
            {
                return FormatNull(in parameters);
            }
        }

        if (value is ITranslationArgument preDefined)
        {
            return preDefined.Translate(this, in parameters);
        }

        IValueFormatter valueFormatter = GetValueFormatter<T>();

        if (valueFormatter is IValueFormatter<T> v)
            return v.Format(this, value!, in parameters);

        return valueFormatter.Format(this, value!, in parameters);
    }
    
    private string FormatIntl(object? value, in ValueFormatParameters parameters, Type? formatType)
    {
        if (Equals(value, null) || value == DBNull.Value)
        {
            return FormatNull(in parameters);
        }

        if (value is ITranslationArgument preDefined)
        {
            return preDefined.Translate(this, in parameters);
        }

        formatType ??= value.GetType() ?? typeof(object);
        IValueFormatter valueFormatter = GetValueFormatter(formatType);

        return valueFormatter.Format(this, value, in parameters);
    }

    private IValueFormatter GetValueFormatter<T>()
    {
        return GetValueFormatter(typeof(T));
    }

    public IValueFormatter GetValueFormatter(Type type)
    {
        return (IValueFormatter)_valueFormatters.GetOrAdd(type, static (type, vf) =>
        {
            if (type.IsEnum)
            {
                Type formatterType = typeof(EnumValueFormatter<>).MakeGenericType(type);
                IEnumFormatter formatter = (IEnumFormatter)vf.CreateInstance(formatterType);
                return formatter;
            }

            Type lookingForFormatterType = typeof(IValueFormatter<>).MakeGenericType(type);
            foreach (object valueFormatter in vf._valueFormatterTypes)
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

                object formatter = vf.CreateInstance(actualType);
                return formatter;
            }

            return new ToStringValueFormatter();
        }, this);
    }

    private object CreateInstance(Type type)
    {
        ConstructorInfo? ctor1 = type.GetConstructor([ typeof(ITranslationService) ]);
        if (ctor1 != null)
        {
            return ctor1.Invoke([ TranslationService ]);
        }

        return Activator.CreateInstance(type);
    }

    public string Colorize(ReadOnlySpan<char> text, Color32 color, TranslationOptions options)
    {
        return TranslationFormattingUtility.Colorize(text, color, options, TranslationService.TerminalColoring);
    }

    public string Colorize(string text, Color32 color, TranslationOptions options)
    {
        return TranslationFormattingUtility.Colorize(text, color, options, TranslationService.TerminalColoring);
    }

    private string FormatNull(in ValueFormatParameters parameters)
    {
        if ((parameters.Options & TranslationOptions.NoRichText) != 0)
        {
            return NullNoColor;
        }

        if ((parameters.Options & TranslationOptions.TranslateWithTerminalRichText) != 0)
        {
            return TranslationService.TerminalColoring switch
            {
                StackColorFormatType.ExtendedANSIColor => NullExtendedANSI,
                StackColorFormatType.ANSIColor => NullANSI,
                _ => NullNoColor
            };
        }

        return (parameters.Options & TranslationOptions.TranslateWithUnityRichText) != 0 ? NullColorUnity : NullColorTMPro;
    }

    public void Dispose()
    {
        do
        {
            foreach (Type type in _valueFormatters.Keys)
            {
                if (_valueFormatters.TryRemove(type, out object val) && val is IDisposable disp)
                {
                    disp.Dispose();
                }
            }
        } while (_valueFormatters.Count > 0);
    }
}