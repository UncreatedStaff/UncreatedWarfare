using System;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.UI;
public class ConventionalLeaderboardUI : UnturnedUI
{
    internal const string StatFormatTime = "%h\\:mm\\:ss";
    internal const string StatFormatFloat = "F0";
    internal const string StatFormatPrecisionFloat = "0.##";
    public readonly UnturnedLabel Title = new UnturnedLabel("TitleWinner");
    public readonly UnturnedLabel Gamemode = new UnturnedLabel("TitleGamemode");

    public readonly UnturnedLabel NextGameStartLabel = new UnturnedLabel("NextGameStartsIn");
    public readonly UnturnedLabel NextGameSeconds = new UnturnedLabel("NextGameSeconds");
    public readonly UnturnedLabel NextGameSecondsCircle = new UnturnedLabel("NextGameCircleForeground");

    public readonly UnturnedLabel Team1Header0 = new UnturnedLabel("1Kills");
    public readonly UnturnedLabel Team1Header1 = new UnturnedLabel("1Deaths");
    public readonly UnturnedLabel Team1Header2 = new UnturnedLabel("1XP");
    public readonly UnturnedLabel Team1Header3 = new UnturnedLabel("1OFP");
    public readonly UnturnedLabel Team1Header4 = new UnturnedLabel("1Caps");
    public readonly UnturnedLabel Team1Header5 = new UnturnedLabel("1Damage");

    public readonly UnturnedLabel Team2Header0 = new UnturnedLabel("2Kills");
    public readonly UnturnedLabel Team2Header1 = new UnturnedLabel("2Deaths");
    public readonly UnturnedLabel Team2Header2 = new UnturnedLabel("2XP");
    public readonly UnturnedLabel Team2Header3 = new UnturnedLabel("2OFP");
    public readonly UnturnedLabel Team2Header4 = new UnturnedLabel("2Caps");
    public readonly UnturnedLabel Team2Header5 = new UnturnedLabel("2Damage");
    
    public readonly UnturnedLabel Team1Name = new UnturnedLabel("1N0");
    public readonly UnturnedLabel Team1Kills = new UnturnedLabel("1K0");
    public readonly UnturnedLabel Team1Deaths = new UnturnedLabel("1D0");
    public readonly UnturnedLabel Team1XP = new UnturnedLabel("1X0");
    public readonly UnturnedLabel Team1Credits = new UnturnedLabel("1F0");
    public readonly UnturnedLabel Team1Captures = new UnturnedLabel("1C0");
    public readonly UnturnedLabel Team1Damage = new UnturnedLabel("1T0");

    public readonly UnturnedLabel Team2Name = new UnturnedLabel("2N0");
    public readonly UnturnedLabel Team2Kills = new UnturnedLabel("2K0");
    public readonly UnturnedLabel Team2Deaths = new UnturnedLabel("2D0");
    public readonly UnturnedLabel Team2XP = new UnturnedLabel("2X0");
    public readonly UnturnedLabel Team2Credits = new UnturnedLabel("2F0");
    public readonly UnturnedLabel Team2Captures = new UnturnedLabel("2C0");
    public readonly UnturnedLabel Team2Damage = new UnturnedLabel("2T0");

    public readonly UnturnedLabel[] Team1PlayerNames = UnturnedUIPatterns.CreateArray<UnturnedLabel>("1N{0}", 1, to: 14);
    public readonly UnturnedLabel[] Team1PlayerKills = UnturnedUIPatterns.CreateArray<UnturnedLabel>("1K{0}", 1, to: 14);
    public readonly UnturnedLabel[] Team1PlayerDeaths = UnturnedUIPatterns.CreateArray<UnturnedLabel>("1D{0}", 1, to: 14);
    public readonly UnturnedLabel[] Team1PlayerXP = UnturnedUIPatterns.CreateArray<UnturnedLabel>("1X{0}", 1, to: 14);
    public readonly UnturnedLabel[] Team1PlayerCredits = UnturnedUIPatterns.CreateArray<UnturnedLabel>("1F{0}", 1, to: 14);
    public readonly UnturnedLabel[] Team1PlayerCaptures = UnturnedUIPatterns.CreateArray<UnturnedLabel>("1C{0}", 1, to: 14);
    public readonly UnturnedLabel[] Team1PlayerDamage = UnturnedUIPatterns.CreateArray<UnturnedLabel>("1T{0}", 1, to: 14);
    public readonly UnturnedLabel[] Team1PlayerVCs = UnturnedUIPatterns.CreateArray<UnturnedLabel>("1VC{0}", 1, to: 14);

    public readonly UnturnedLabel[] Team2PlayerNames = UnturnedUIPatterns.CreateArray<UnturnedLabel>("2N{0}", 1, to: 14);
    public readonly UnturnedLabel[] Team2PlayerKills = UnturnedUIPatterns.CreateArray<UnturnedLabel>("2K{0}", 1, to: 14);
    public readonly UnturnedLabel[] Team2PlayerDeaths = UnturnedUIPatterns.CreateArray<UnturnedLabel>("2D{0}", 1, to: 14);
    public readonly UnturnedLabel[] Team2PlayerXP = UnturnedUIPatterns.CreateArray<UnturnedLabel>("2X{0}", 1, to: 14);
    public readonly UnturnedLabel[] Team2PlayerCredits = UnturnedUIPatterns.CreateArray<UnturnedLabel>("2F{0}", 1, to: 14);
    public readonly UnturnedLabel[] Team2PlayerCaptures = UnturnedUIPatterns.CreateArray<UnturnedLabel>("2C{0}", 1, to: 14);
    public readonly UnturnedLabel[] Team2PlayerDamage = UnturnedUIPatterns.CreateArray<UnturnedLabel>("2T{0}", 1, to: 14);
    public readonly UnturnedLabel[] Team2PlayerVCs = UnturnedUIPatterns.CreateArray<UnturnedLabel>("2VC{0}", 1, to: 14);
    
    public readonly UnturnedLabel TeamStatsHeader = new UnturnedLabel("WarHeader");

    public readonly UnturnedLabel TeamDurationLabel = new UnturnedLabel("lblDuration");
    public readonly UnturnedLabel TeamT1CasualtiesLabel = new UnturnedLabel("lblCasualtiesT1");
    public readonly UnturnedLabel TeamT2CasualtiesLabel = new UnturnedLabel("lblCasualtiesT2");
    public readonly UnturnedLabel TeamFlagCapturesLabel = new UnturnedLabel("lblOwnerChangedCount");
    public readonly UnturnedLabel TeamT1AveragePlayersLabel = new UnturnedLabel("lblAveragePlayerCountT1");
    public readonly UnturnedLabel TeamT2AveragePlayersLabel = new UnturnedLabel("lblAveragePlayerCountT2");

    public readonly UnturnedLabel TeamDuration = new UnturnedLabel("DurationValue");
    public readonly UnturnedLabel TeamT1Casualties = new UnturnedLabel("CasualtiesValueT1");
    public readonly UnturnedLabel TeamT2Casualties = new UnturnedLabel("CasualtiesValueT2");
    public readonly UnturnedLabel TeamFlagCaptures = new UnturnedLabel("FlagCapturesValue");
    public readonly UnturnedLabel TeamT1AveragePlayers = new UnturnedLabel("AveragePlayerCountsT1Value");
    public readonly UnturnedLabel TeamT2AveragePlayers = new UnturnedLabel("AveragePlayerCountsT2Value");

    public readonly UnturnedLabel TeamT1FOBsPlacedLabel = new UnturnedLabel("lblFOBsPlacedT1");
    public readonly UnturnedLabel TeamT2FOBsPlacedLabel = new UnturnedLabel("lblFOBsPlacedT2");
    public readonly UnturnedLabel TeamT1FOBsDestroyedLabel = new UnturnedLabel("lblFOBsDestroyedT1");
    public readonly UnturnedLabel TeamT2FOBsDestroyedLabel = new UnturnedLabel("lblFOBsDestroyedT2");
    public readonly UnturnedLabel TeamTeamkillsLabel = new UnturnedLabel("lblTeamkillingCasualties");
    public readonly UnturnedLabel TeamLongestShotLabel = new UnturnedLabel("lblTopRankingOfficer");

    public readonly UnturnedLabel TeamT1FOBsPlaced = new UnturnedLabel("FOBsPlacedT1Value");
    public readonly UnturnedLabel TeamT2FOBsPlaced = new UnturnedLabel("FOBsPlacedT2Value");
    public readonly UnturnedLabel TeamT1FOBsDestroyed = new UnturnedLabel("FOBsDestroyedT1Value");
    public readonly UnturnedLabel TeamT2FOBsDestroyed = new UnturnedLabel("FOBsDestroyedT2Value");
    public readonly UnturnedLabel TeamTeamkills = new UnturnedLabel("TeamkillingCasualtiesValue");
    public readonly UnturnedLabel TeamLongestShot = new UnturnedLabel("TopRankingOfficerValue");
    
    public readonly UnturnedLabel PlayerStatsHeader = new UnturnedLabel("playerstats_header");

    public readonly UnturnedLabel[] PersonalStatsLabels = UnturnedUIPatterns.CreateArray<UnturnedLabel>("playerstats_{0}", 0, to: 11);
    public readonly UnturnedLabel[] PersonalStatsValues = UnturnedUIPatterns.CreateArray<UnturnedLabel>("playerstats_{0}_v", 0, to: 11);

    public ConventionalLeaderboardUI() : base(Gamemodes.Gamemode.Config.UIConventionalLeaderboard, reliable: false)
    {
        IsSendReliable = true;
    }

    public void UpdateTime(LanguageSet set, int secondsLeft)
    {
        int time = Mathf.RoundToInt(Gamemodes.Gamemode.Config.GeneralLeaderboardTime);
        string l1 = TimeSpan.FromSeconds(secondsLeft).ToString("m\\:ss", Localization.GetLocale(set.Language));
        string l2 = new string(Gamemodes.Gamemode.Config.UICircleFontCharacters[CTFUI.FromMax(Mathf.RoundToInt(time - secondsLeft), time)], 1);
        while (set.MoveNext())
        {
            NextGameSeconds.SetText(set.Next.Connection, l1);
            NextGameSecondsCircle.SetText(set.Next.Connection, l2);
        }
    }
}
