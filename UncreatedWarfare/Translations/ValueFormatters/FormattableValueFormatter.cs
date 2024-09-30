using System;

namespace Uncreated.Warfare.Translations.ValueFormatters;
public class FormattableValueFormatter<TFormattable> : IValueFormatter<TFormattable> where TFormattable : IFormattable
{
    string IValueFormatter.Format(ITranslationValueFormatter formatter, object value, in ValueFormatParameters parameters)
    {
        return Format(formatter, (TFormattable)value, in parameters);
    }

    public string Format(ITranslationValueFormatter formatter, TFormattable value, in ValueFormatParameters parameters)
    {
        return value.ToString(parameters.Format.UseForToString ? parameters.Format.Format : null, parameters.Culture);
    }
}