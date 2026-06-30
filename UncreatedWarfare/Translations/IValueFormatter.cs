using System;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations.Storage;

namespace Uncreated.Warfare.Translations;

public interface IValueFormatter<in TFormattable> : IValueFormatter
{
    string Format(ITranslationValueFormatter formatter, TFormattable value, in ValueFormatParameters parameters);
}

public interface IValueFormatter
{
    string Format(ITranslationValueFormatter formatter, object value, in ValueFormatParameters parameters);
}

public interface IEnumFormatter<TEnum> : IEnumFormatter, IValueFormatter<TEnum> where TEnum : unmanaged, Enum
{
    IEnumTranslationStorage<TEnum>? Storage { get; }
    string GetValue(TEnum value, LanguageInfo language);
}

public interface IEnumFormatter : IValueFormatter
{
    string GetValue(object value, LanguageInfo language);
    void Visit<TVisitor>(ref TVisitor visitor) where TVisitor : IEnumFormatterVisitor;
}

public interface IEnumFormatterVisitor
{
    void Accept<TEnum>(IEnumFormatter<TEnum> formatter) where TEnum : unmanaged, Enum;
}