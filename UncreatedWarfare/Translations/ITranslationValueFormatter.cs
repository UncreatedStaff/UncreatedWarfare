using System;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Translations;
public interface ITranslationValueFormatter
{
    LanguageService LanguageService { get; }
    ITranslationService TranslationService { get; }
    string Format<T>(T? value, in ValueFormatParameters parameters);
    string Format(object? value, in ValueFormatParameters parameters, Type? formatType = null);

    string FormatEnum<TEnum>(TEnum value, LanguageInfo? language) where TEnum : unmanaged, Enum;
    string FormatEnum(object value, Type enumType, LanguageInfo? language);
    string FormatEnumName<TEnum>(LanguageInfo? language) where TEnum : unmanaged, Enum;
    string FormatEnumName(Type enumType, LanguageInfo? language);

    string Colorize(ReadOnlySpan<char> text, Color32 color, TranslationOptions options);
}