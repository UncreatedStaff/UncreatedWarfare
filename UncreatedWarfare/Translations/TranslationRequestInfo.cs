using System;
using System.Globalization;
using Uncreated.Warfare.Models.Localization;

namespace Uncreated.Warfare.Translations;
public struct TranslationRequestInfo
{
    public readonly CultureInfo Culture;
    public readonly LanguageInfo Language;
    public readonly bool IMGUI;
    public readonly bool ForChat;
    public Color ChatColor;
    public readonly ReadOnlySpan<char> GetProperValue(TranslationValue value)
    {
        if (IMGUI)
        {
            return ForChat ? value.ColorStrippedIMGUIValue : value.IMGUIValue;
        }

        return ForChat ? value.ColorStrippedValue : value.Value;
    }

    public TranslationRequestInfo(CultureInfo culture, LanguageInfo language, bool imgui, bool forChat)
    {
        Culture = culture;
        Language = language;
        IMGUI = imgui;
        ForChat = forChat;
        ChatColor = Color.white;
    }
}