using System;

namespace Uncreated.Warfare.Translations;
public interface ITranslationValueFormatter
{
    ReadOnlySpan<char> Format<T>(T? value, in ValueFormatParameters parameters);
}