using System;

namespace Uncreated.Warfare.Translations;
public interface IValueFormatter<in TFormattable>
{
    ReadOnlySpan<char> Format(TFormattable value, in ValueFormatParameters parameters);
}