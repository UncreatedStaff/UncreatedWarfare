using System;

namespace Uncreated.Warfare.Translations.ValueFormatters;
public class FormattableValueFormatter<TFormattable> : IValueFormatter<TFormattable> where TFormattable : IFormattable
{
    string IValueFormatter.Format(object value, in ValueFormatParameters parameters) => Format((TFormattable)value, in parameters);
    public string Format(TFormattable value, in ValueFormatParameters parameters)
    {
        return value.ToString(parameters.Format.Format, parameters.Culture);
    }
}