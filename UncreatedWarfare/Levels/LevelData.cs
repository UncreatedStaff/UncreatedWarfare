using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Translations.ValueFormatters;

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
        //Level = Points.GetLevel(xp);
        //int startXP = Points.GetLevelXP(Level);
        //CurrentXP = xp - startXP;
        //RequiredXP = Points.GetNextLevelXP(Level) - startXP;
        //Name = GetRankName(Level);
        //Abbreviation = GetRankAbbreviation(Level);
        //NextName = GetRankName(Level + 1);
        //NextAbbreviation = GetRankAbbreviation(Level + 1);
        //ProgressBar = Points.GetProgressBar(CurrentXP, RequiredXP);
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


    public static readonly SpecialFormat FormatNumeric = new SpecialFormat("Numeric", "x");

    public static readonly SpecialFormat FormatAbbreviation = new SpecialFormat("Abbreviation", "a");

    public static readonly SpecialFormat FormatName = new SpecialFormat("Name", "n");
    public string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        if (FormatNumeric.Match(in parameters))
            return Level.ToString(Data.LocalLocale);

        if (FormatAbbreviation.Match(in parameters))
            return Abbreviation;

        return Name;
    }
}
