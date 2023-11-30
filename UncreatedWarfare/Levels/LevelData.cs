using System;
using System.Globalization;
using Uncreated.Warfare.Models.Localization;

namespace Uncreated.Warfare.Levels;

public readonly struct LevelData : ITranslationArgument
{
    public int TotalXP { get; }
    public int CurrentXP { get; }
    public int RequiredXP { get; }
    public int Level { get; }
    public string Name { get; }
    public string Abbreviation { get; }
    public string NextName { get; }
    public string NextAbbreviation { get; }
    public string ProgressBar { get; }
    public LevelData(int xp)
    {
        TotalXP = xp;
        Level = Points.GetLevel(xp);
        int startXP = Points.GetLevelXP(Level);
        CurrentXP = xp - startXP;
        RequiredXP = Points.GetNextLevelXP(Level) - startXP;
        Name = GetRankName(Level);
        Abbreviation = GetRankAbbreviation(Level);
        NextName = GetRankName(Level + 1);
        NextAbbreviation = GetRankAbbreviation(Level + 1);
        ProgressBar = UCWarfare.IsLoaded ? Points.GetProgressBar(CurrentXP, RequiredXP) : string.Empty;
    }
    public static string GetRankName(int level)
    {
        return level switch
        {
            0 => "Recruit",
            1 => "Private",
            2 => "Private 1st Class",
            3 => "Corporal",
            4 => "Specialist",
            5 => "Sergeant",
            6 => "Staff Sergeant",
            7 => "Sergeant Major",
            8 => "Warrant Officer",
            _ => "Level " + level.ToString(Data.LocalLocale),
        };
    }
    public static string GetRankAbbreviation(int level)
    {
        return level switch
        {
            0 => "Rec.",
            1 => "Pvt.",
            2 => "Pfc.",
            3 => "Cpl.",
            4 => "Spec.",
            5 => "Sgt.",
            6 => "Ssg.",
            7 => "S.M.",
            8 => "W.O.",
            _ => "L " + level.ToString(Data.LocalLocale),
        };
    }

    [FormatDisplay("Numeric")]
    public const string FormatNumeric = "x";
    [FormatDisplay("Abbreviation")]
    public const string FormatAbbreviation = "a";
    [FormatDisplay("Name")]
    public const string FormatName = "n";
    public string Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags)
    {
        if (format is not null)
        {
            if (format.Equals(FormatNumeric, StringComparison.Ordinal))
                return Level.ToString(Data.LocalLocale);
            if (format.Equals(FormatAbbreviation, StringComparison.Ordinal))
                return Abbreviation;
        }

        return Name;
    }
}
