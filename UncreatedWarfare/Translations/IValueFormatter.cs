using System;
using Uncreated.Warfare.Models.Localization;

namespace Uncreated.Warfare.Translations;

public interface IValueFormatter<in TFormattable> : IValueFormatter
{
    string Format(ITranslationValueFormatter formatter, TFormattable value, in ValueFormatParameters parameters);
}

public interface IValueFormatter
{
    string Format(ITranslationValueFormatter formatter, object value, in ValueFormatParameters parameters);
}

public interface IEnumFormatter<in TEnum> : IEnumFormatter, IValueFormatter<TEnum> where TEnum : unmanaged, Enum
{
    string GetValue(TEnum value, LanguageInfo language);
}

public interface IEnumFormatter : IValueFormatter
{
    string GetValue(object value, LanguageInfo language);
    string GetName(LanguageInfo language);
}