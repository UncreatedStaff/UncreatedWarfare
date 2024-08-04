using System;

namespace Uncreated.Warfare.Translations.ValueFormatters;
public class FormattableValueFormatter<TFormattable> : IValueFormatter<TFormattable> where TFormattable : IFormattable
{
    public ReadOnlySpan<char> Format(TFormattable value, in ValueFormatParameters parameters)
    {
        return value.ToString(parameters.Format, parameters.Culture);
    }
}