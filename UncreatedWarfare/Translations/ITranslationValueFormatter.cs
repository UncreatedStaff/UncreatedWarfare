namespace Uncreated.Warfare.Translations;
public interface ITranslationValueFormatter
{
    string Format<T>(T? value, in ValueFormatParameters parameters);
}