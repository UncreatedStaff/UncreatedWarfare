using System.Globalization;

namespace Uncreated.Warfare.Translations;
public readonly ref struct ValueFormatParameters
{
    public readonly int Argument;
    public readonly CultureInfo Culture;
    public readonly TranslationOptions Options;
    public readonly string? Format;
    public ValueFormatParameters(int argument, CultureInfo culture, TranslationOptions options, string? format)
    {
        Argument = argument;
        Culture = culture;
        Options = options;
        Format = format;
    }
}
