using System;
using System.Globalization;

namespace Uncreated.Warfare.Translations.Languages;
public static class Languages
{
    public const string EnglishUS = "en-us";
    public const string Russian = "ru-ru";
    public const string Spanish = "es-es";
    public const string German = "de-de";
    public const string Arabic = "ar-sa";
    public const string French = "fr-fr";
    public const string Polish = "pl-pl";
    public const string PortugueseBrazil = "pt-br";
    public const string PortuguesePortugal = "pt-br";
    public const string Filipino = "fl-ph";
    public const string Norwegian = "nb-no";
    public const string Romanian = "ro-ro";
    public const string Dutch = "nl-nl";
    public const string Swedish = "sv-se";
    public const string ChineseSimplified = "zh-cn";
    public const string ChineseTraditional = "zh-tw";
    public static readonly CultureInfo CultureEnglishUS = new CultureInfo("en-US");
    public static readonly CultureInfo CultureRussian = new CultureInfo("ru-RU");
    public static readonly CultureInfo CultureSpanish = new CultureInfo("es-ES");
    public static readonly CultureInfo CultureGerman = new CultureInfo("de-DE");
    public static readonly CultureInfo CultureArabic = new CultureInfo("ar-SA");
    public static readonly CultureInfo CultureFrench = new CultureInfo("fr-FR");
    public static readonly CultureInfo CulturePolish = new CultureInfo("pl-PL");
    public static readonly CultureInfo CulturePortugueseBrazil = new CultureInfo("pt-BR");
    public static readonly CultureInfo CulturePortuguesePortugal = new CultureInfo("pt-BR");
    public static readonly CultureInfo CultureFilipino = new CultureInfo("fil-PH");
    public static readonly CultureInfo CultureNorwegian = new CultureInfo("nb-NO");
    public static readonly CultureInfo CultureRomanian = new CultureInfo("ro-RO");
    public static readonly CultureInfo CultureDutch = new CultureInfo("nl-NL");
    public static readonly CultureInfo CultureChinese = new CultureInfo("zh-CN");
    public static readonly CultureInfo CultureSwedish = new CultureInfo("sv-SE");

    public static CultureInfo GetCultureInfo(string? language)
    {
        if (language is not null)
        {
            if (language.Equals(EnglishUS, StringComparison.Ordinal))
                return CultureEnglishUS;
            if (language.Equals(Russian, StringComparison.Ordinal))
                return CultureRussian;
            if (language.Equals(Spanish, StringComparison.Ordinal))
                return CultureSpanish;
            if (language.Equals(German, StringComparison.Ordinal))
                return CultureGerman;
            if (language.Equals(Arabic, StringComparison.Ordinal))
                return CultureArabic;
            if (language.Equals(French, StringComparison.Ordinal))
                return CultureFrench;
            if (language.Equals(Polish, StringComparison.Ordinal))
                return CulturePolish;
            if (language.Equals(PortugueseBrazil, StringComparison.Ordinal))
                return CulturePortugueseBrazil;
            if (language.Equals(PortuguesePortugal, StringComparison.Ordinal))
                return CulturePortuguesePortugal;
            if (language.Equals(Norwegian, StringComparison.Ordinal))
                return CultureNorwegian;
            if (language.Equals(Romanian, StringComparison.Ordinal))
                return CultureRomanian;
            if (language.Equals(Dutch, StringComparison.Ordinal))
                return CultureDutch;
            if (language.Equals(Swedish, StringComparison.Ordinal))
                return CultureSwedish;
            if (language.Equals(ChineseSimplified, StringComparison.Ordinal) ||
                language.Equals(ChineseTraditional, StringComparison.Ordinal))
                return CultureChinese;
        }

        return Data.LocalLocale;
    }
}
