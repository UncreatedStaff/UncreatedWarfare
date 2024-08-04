using System;

namespace Uncreated.Warfare.Translations.Util;
internal static class TranslationPluralizations
{
    internal static Pluralization[] GetPluralizations(ref string value, ref string? imguiString)
    {
        imguiString ??= TranslationFormattingUtility.CreateIMGUIString(value);

        // todo

        return Array.Empty<Pluralization>();
    }
}

internal struct Pluralization
{
    public int StartIndex;
    public int IMGUIStartIndex;
    public int WordLength;
    public int Argument;
    public bool IsInverted;
    public Pluralization(int startIndex, int imguiStartIndex, int wordLength, int argument, bool isInverted)
    {
        StartIndex = startIndex;
        IMGUIStartIndex = imguiStartIndex;
        Argument = argument;
        WordLength = wordLength;
        IsInverted = isInverted;
    }
}