using System;

namespace Uncreated.Warfare.Translations;
public interface ITranslationValueFormatter
{
    string Format<T>(T? value, in ValueFormatParameters parameters);
    string Format(object? value, in ValueFormatParameters parameters, Type? formatType = null);
}