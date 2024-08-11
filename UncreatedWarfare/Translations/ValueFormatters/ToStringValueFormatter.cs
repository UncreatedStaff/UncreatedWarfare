namespace Uncreated.Warfare.Translations.ValueFormatters;
public class ToStringValueFormatter : IValueFormatter<object>
{
    public string Format(object value, in ValueFormatParameters parameters) => value.ToString();
}
