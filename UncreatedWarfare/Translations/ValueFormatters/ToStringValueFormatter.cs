using System;

namespace Uncreated.Warfare.Translations.ValueFormatters;
public class ToStringValueFormatter : IValueFormatter<object>
{
    public ReadOnlySpan<char> Format(object value, in ValueFormatParameters parameters) => value.ToString();
}
