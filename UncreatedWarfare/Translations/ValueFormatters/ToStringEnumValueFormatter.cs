using System;
using Uncreated.Warfare.Models.Localization;

namespace Uncreated.Warfare.Translations.ValueFormatters;

public class ToStringEnumValueFormatter<TEnum> : IEnumFormatter<TEnum> where TEnum : unmanaged, Enum
{
    /// <inheritdoc />
    public string Format(ITranslationValueFormatter formatter, TEnum value, in ValueFormatParameters parameters)
    {
        return value.ToString();
    }

    /// <inheritdoc />
    public string GetValue(TEnum value, LanguageInfo language)
    {
        return value.ToString();
    }

    /// <inheritdoc />
    public string Format(ITranslationValueFormatter formatter, object value, in ValueFormatParameters parameters)
    {
        return value.ToString();
    }

    /// <inheritdoc />
    public string GetValue(object value, LanguageInfo language)
    {
        return value.ToString();
    }

    /// <inheritdoc />
    public string GetName(LanguageInfo language)
    {
        return typeof(TEnum).Name;
    }
}
