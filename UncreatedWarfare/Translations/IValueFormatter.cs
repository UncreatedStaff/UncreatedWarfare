namespace Uncreated.Warfare.Translations;

public interface IValueFormatter<in TFormattable> : IValueFormatter
{
    string Format(TFormattable value, in ValueFormatParameters parameters);
}
public interface IValueFormatter
{
    string Format(object value, in ValueFormatParameters parameters);
}