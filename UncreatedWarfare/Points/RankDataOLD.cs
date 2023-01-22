using System;
using Uncreated.Warfare.Kits;
using UnityEngine;

namespace Uncreated.Warfare.Point;

[Obsolete("Unused", true)]
public class RankDataOLD
{
    public static readonly RankDataOLD Nil = new RankDataOLD(0, -1, Kits.Branch.Default, 0);
    public readonly ulong Steam64;
    public readonly Branch Branch;
    public int TotalXP;
    public int Level;
    public int RankTier;
    public int OfficerTier;
    public ulong OfficerTeam;
    public int CurrentXP;
    public int RequiredXP;
    public string Name;
    public string Abbreviation;
    public RankDataOLD(ulong steamID, int xp, Branch branch, ulong officerTeam)
    {
        Steam64 = steamID;
        Branch = branch;
        OfficerTeam = officerTeam;
        Update(xp);
    }

    public bool IsNil => TotalXP == -1;
    private const int A = 400;
    private const int D = 100;
    public void Update(int newXP)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        TotalXP = newXP;

        float x = D / 2f;
        float y = A - (3 * x);
        float z = 0 - x - y;
        Level = Mathf.RoundToInt(Mathf.Floor((x - A + Mathf.Sqrt(Mathf.Pow(A - x, 2f) + (2f * D * TotalXP))) / D));

        float n = Level + 1;
        CurrentXP = (int)(TotalXP - ((x * Math.Pow(n, 2)) + (y * n) + z));
        RequiredXP = A + Level * D;

        RankTier = GetRankTier(Level);

        CheckOfficerStatus();
        CheckNameAbbreviations();
    }
    public void CheckOfficerStatus()
    {
        if (OfficerStorage.IsOfficer(Steam64, OfficerTeam, out OfficerData officer))
        {
            OfficerTier = officer.OfficerTier;
        }
        else
        {
            OfficerTier = 0;
        }
    }
    public void CheckNameAbbreviations()
    {
        if (OfficerTier > 0)
        {
            Name = GetOfficerRankName(OfficerTier);
            Abbreviation = GetOfficerRankAbbreviation(OfficerTier);
        }
        else
        {
            Name = GetRankName(RankTier);
            Abbreviation = GetRankAbbreviation(RankTier);
        }
    }
    public static int GetRankTier(int level)
    {
        if (level < 1) return 0;
        else if (level < 3) return 1;
        else if (level < 5) return 2;
        else if (level < 7) return 3;
        else if (level < 9) return 4;
        else if (level < 11) return 5;
        else if (level < 14) return 6;
        else if (level < 17) return 7;
        else if (level < 20) return 8;
        else if (level < 25) return 9;
        else return 10;
    }
    public static string GetRankName(int rankTier)
    {
        return rankTier switch
        {
            0 => "Recruit",
            1 => "Private",
            2 => "Private 1st Class",
            3 => "Corporal",
            4 => "Specialist",
            5 => "Sergeant",
            6 => "Staff Sergeant",
            7 => "Sergeant 1st Class",
            8 => "Sergeant Major",
            9 => "Warrant Officer",
            10 => "Chief Warrant Officer",
            _ => "unknown",
        };
    }
    public static string GetRankAbbreviation(int rankTier)
    {
        return rankTier switch
        {
            0 => "Rec.",
            1 => "Pvt.",
            2 => "Pfc.",
            3 => "Cpl.",
            4 => "Spec.",
            5 => "Sgt.",
            6 => "Ssg.",
            7 => "Sfc.",
            8 => "S.M.",
            9 => "W.O.",
            10 => "C.W.O.",
            _ => "###",
        };
    }

    public static string GetOfficerRankName(int officerTier)
    {
        return officerTier switch
        {
            1 => "Captain",
            2 => "Major",
            3 => "Lieutenant",
            4 => "Colonel",
            5 => "General",
            _ => "unknown",
        };
    }
    public static string GetOfficerRankAbbreviation(int officerTier)
    {
        return officerTier switch
        {
            1 => "Cpt.",
            2 => "Maj.",
            3 => "Lt.",
            4 => "Col.",
            5 => "Gen.",
            _ => "###",
        };
    }
}
public readonly struct RankData : ITranslationArgument
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
    public RankData(int xp)
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
    public string Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags)
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
