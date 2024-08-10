using Uncreated.Warfare.Logging;

namespace Uncreated.Warfare.Translations.Util;
internal readonly struct ArgumentSpan
{
    public readonly int Argument;
    public readonly int StartIndex;
    public readonly int Length;
    public readonly bool Inverted;
    public ArgumentSpan(int argument, int startIndex, int length, bool inverted)
    {
        Argument = argument;
        StartIndex = startIndex;
        Length = length;
        Inverted = inverted;
    }
    public void Pluralize(in TranslationHelper helper, ref string value, ref int offset)
    {
        int index = StartIndex + offset;
        if (index >= value.Length)
            return;
        int len = Length;
        if (len > value.Length - index)
            len = value.Length - index;
        L.LogDebug($"Argument: {Argument}, index {index} for {len} char. Inverted: {Inverted}");
        string plural = Translation.Pluralize(helper.Language, helper.Culture, value.Substring(index, len), helper.Flags | TranslationFlags.Plural);
        L.LogDebug($"Value: {plural} ({value.Substring(index, len)})");
        offset += plural.Length - len;
        value = value.Substring(0, index) + plural + value.Substring(index + len);
    }
}