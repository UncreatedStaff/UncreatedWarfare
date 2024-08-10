namespace Uncreated.Warfare.Translations;
public interface IValueFormatter<in TFormattable>
{
    string Format(TFormattable value, in ValueFormatParameters parameters);
}