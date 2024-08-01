using System;
using System.Globalization;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Patterns;

namespace Uncreated.Warfare.Layouts.UI;

public class ConventionalLeaderboardUI : UnturnedUI
{
    internal const string StatFormatTime = "%h\\:mm\\:ss";
    internal const string StatFormatFloat = "F0";
    internal const string StatFormatPrecisionFloat = "0.##";
    public readonly UnturnedLabel Title = new UnturnedLabel("Titles/TitleWinner");
    public readonly UnturnedLabel Gamemode = new UnturnedLabel("Titles/TitleGamemode");

    public readonly UnturnedLabel NextGameStartLabel = new UnturnedLabel("Titles/NextGameStartsIn");
    public readonly UnturnedLabel NextGameSeconds = new UnturnedLabel("Titles/NextGameStartsIn/NextGameSeconds");
    public readonly UnturnedLabel NextGameSecondsCircle = new UnturnedLabel("Titles/NextGameStartsIn/NextGameCircleBackground/NextGameCircleForeground");

    public readonly UnturnedLabel Team1Header0 = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team1/1Kills");
    public readonly UnturnedLabel Team1Header1 = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team1/1Deaths");
    public readonly UnturnedLabel Team1Header2 = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team1/1XP");
    public readonly UnturnedLabel Team1Header3 = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team1/1OFP");
    public readonly UnturnedLabel Team1Header4 = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team1/1Caps");
    public readonly UnturnedLabel Team1Header5 = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team1/1Damage");

    public readonly UnturnedLabel Team2Header0 = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team2/2Kills");
    public readonly UnturnedLabel Team2Header1 = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team2/2Deaths");
    public readonly UnturnedLabel Team2Header2 = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team2/2XP");
    public readonly UnturnedLabel Team2Header3 = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team2/2OFP");
    public readonly UnturnedLabel Team2Header4 = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team2/2Caps");
    public readonly UnturnedLabel Team2Header5 = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team2/2Damage");
    
    public readonly UnturnedLabel Team1Name     = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team1/0/1N0");
    public readonly UnturnedLabel Team1Kills    = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team1/0/1K0");
    public readonly UnturnedLabel Team1Deaths   = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team1/0/1D0");
    public readonly UnturnedLabel Team1XP       = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team1/0/1X0");
    public readonly UnturnedLabel Team1Credits  = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team1/0/1F0");
    public readonly UnturnedLabel Team1Captures = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team1/0/1C0");
    public readonly UnturnedLabel Team1Damage   = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team1/0/1T0");

    public readonly UnturnedLabel Team2Name     = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team2/0/2N0");
    public readonly UnturnedLabel Team2Kills    = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team2/0/2K0");
    public readonly UnturnedLabel Team2Deaths   = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team2/0/2D0");
    public readonly UnturnedLabel Team2XP       = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team2/0/2X0");
    public readonly UnturnedLabel Team2Credits  = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team2/0/2F0");
    public readonly UnturnedLabel Team2Captures = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team2/0/2C0");
    public readonly UnturnedLabel Team2Damage   = new UnturnedLabel("DataEmpty/Scalar/Leaderboard/Team2/0/2T0");

    public readonly UnturnedLabel[] Team1PlayerNames    = ElementPatterns.CreateArray<UnturnedLabel>("DataEmpty/Scalar/Leaderboard/Team1/{0}/1N{0}",  1, to: 14);
    public readonly UnturnedLabel[] Team1PlayerKills    = ElementPatterns.CreateArray<UnturnedLabel>("DataEmpty/Scalar/Leaderboard/Team1/{0}/1K{0}",  1, to: 14);
    public readonly UnturnedLabel[] Team1PlayerDeaths   = ElementPatterns.CreateArray<UnturnedLabel>("DataEmpty/Scalar/Leaderboard/Team1/{0}/1D{0}",  1, to: 14);
    public readonly UnturnedLabel[] Team1PlayerXP       = ElementPatterns.CreateArray<UnturnedLabel>("DataEmpty/Scalar/Leaderboard/Team1/{0}/1X{0}",  1, to: 14);
    public readonly UnturnedLabel[] Team1PlayerCredits  = ElementPatterns.CreateArray<UnturnedLabel>("DataEmpty/Scalar/Leaderboard/Team1/{0}/1F{0}",  1, to: 14);
    public readonly UnturnedLabel[] Team1PlayerCaptures = ElementPatterns.CreateArray<UnturnedLabel>("DataEmpty/Scalar/Leaderboard/Team1/{0}/1C{0}",  1, to: 14);
    public readonly UnturnedLabel[] Team1PlayerDamage   = ElementPatterns.CreateArray<UnturnedLabel>("DataEmpty/Scalar/Leaderboard/Team1/{0}/1T{0}",  1, to: 14);
    public readonly UnturnedLabel[] Team1PlayerVCs      = ElementPatterns.CreateArray<UnturnedLabel>("DataEmpty/Scalar/Leaderboard/Team1/{0}/1VC{0}", 1, to: 14);

    public readonly UnturnedLabel[] Team2PlayerNames    = ElementPatterns.CreateArray<UnturnedLabel>("DataEmpty/Scalar/Leaderboard/Team2/{0}/2N{0}",  1, to: 14);
    public readonly UnturnedLabel[] Team2PlayerKills    = ElementPatterns.CreateArray<UnturnedLabel>("DataEmpty/Scalar/Leaderboard/Team2/{0}/2K{0}",  1, to: 14);
    public readonly UnturnedLabel[] Team2PlayerDeaths   = ElementPatterns.CreateArray<UnturnedLabel>("DataEmpty/Scalar/Leaderboard/Team2/{0}/2D{0}",  1, to: 14);
    public readonly UnturnedLabel[] Team2PlayerXP       = ElementPatterns.CreateArray<UnturnedLabel>("DataEmpty/Scalar/Leaderboard/Team2/{0}/2X{0}",  1, to: 14);
    public readonly UnturnedLabel[] Team2PlayerCredits  = ElementPatterns.CreateArray<UnturnedLabel>("DataEmpty/Scalar/Leaderboard/Team2/{0}/2F{0}",  1, to: 14);
    public readonly UnturnedLabel[] Team2PlayerCaptures = ElementPatterns.CreateArray<UnturnedLabel>("DataEmpty/Scalar/Leaderboard/Team2/{0}/2C{0}",  1, to: 14);
    public readonly UnturnedLabel[] Team2PlayerDamage   = ElementPatterns.CreateArray<UnturnedLabel>("DataEmpty/Scalar/Leaderboard/Team2/{0}/2T{0}",  1, to: 14);
    public readonly UnturnedLabel[] Team2PlayerVCs      = ElementPatterns.CreateArray<UnturnedLabel>("DataEmpty/Scalar/Leaderboard/Team2/{0}/2VC{0}", 1, to: 14);

    public readonly UnturnedLabel TeamStatsHeader           = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/WarHeader");

    public readonly UnturnedLabel TeamDurationLabel         = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupLeft/ColumnLeft/lblDuration");
    public readonly UnturnedLabel TeamT1CasualtiesLabel     = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupLeft/ColumnLeft/lblCasualtiesT1");
    public readonly UnturnedLabel TeamT2CasualtiesLabel     = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupLeft/ColumnLeft/lblCasualtiesT2");
    public readonly UnturnedLabel TeamFlagCapturesLabel     = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupLeft/ColumnLeft/lblOwnerChangedCount");
    public readonly UnturnedLabel TeamT1AveragePlayersLabel = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupLeft/ColumnLeft/lblAveragePlayerCountT1");
    public readonly UnturnedLabel TeamT2AveragePlayersLabel = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupLeft/ColumnLeft/lblAveragePlayerCountT2");

    public readonly UnturnedLabel TeamDuration              = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupLeft/ColumnRight/DurationValue");
    public readonly UnturnedLabel TeamT1Casualties          = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupLeft/ColumnRight/CasualtiesValueT1");
    public readonly UnturnedLabel TeamT2Casualties          = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupLeft/ColumnRight/CasualtiesValueT2");
    public readonly UnturnedLabel TeamFlagCaptures          = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupLeft/ColumnRight/FlagCapturesValue");
    public readonly UnturnedLabel TeamT1AveragePlayers      = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupLeft/ColumnRight/AveragePlayerCountsT1Value");
    public readonly UnturnedLabel TeamT2AveragePlayers      = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupLeft/ColumnRight/AveragePlayerCountsT2Value");

    public readonly UnturnedLabel TeamT1FOBsPlacedLabel     = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupRight/ColumnLeft/lblFOBsPlacedT1");
    public readonly UnturnedLabel TeamT2FOBsPlacedLabel     = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupRight/ColumnLeft/lblFOBsPlacedT2");
    public readonly UnturnedLabel TeamT1FOBsDestroyedLabel  = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupRight/ColumnLeft/lblFOBsDestroyedT1");
    public readonly UnturnedLabel TeamT2FOBsDestroyedLabel  = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupRight/ColumnLeft/lblFOBsDestroyedT2");
    public readonly UnturnedLabel TeamTeamkillsLabel        = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupRight/ColumnLeft/lblTeamkillingCasualties");
    public readonly UnturnedLabel TeamLongestShotLabel      = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupRight/ColumnLeft/lblTopRankingOfficer");

    public readonly UnturnedLabel TeamT1FOBsPlaced          = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupRight/ColumnRight/FOBsPlacedT1Value");
    public readonly UnturnedLabel TeamT2FOBsPlaced          = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupRight/ColumnRight/FOBsPlacedT2Value");
    public readonly UnturnedLabel TeamT1FOBsDestroyed       = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupRight/ColumnRight/FOBsDestroyedT1Value");
    public readonly UnturnedLabel TeamT2FOBsDestroyed       = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupRight/ColumnRight/FOBsDestroyedT2Value");
    public readonly UnturnedLabel TeamTeamkills             = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupRight/ColumnRight/TeamkillingCasualtiesValue");
    public readonly UnturnedLabel TeamLongestShot           = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/WarAnalytics/ColumnGroupRight/ColumnRight/TopRankingOfficerValue");
    
    public readonly UnturnedLabel PlayerStatsHeader = new UnturnedLabel("DataEmpty/Scalar/BottomContainer/Stats/playerstats_header");

    public readonly UnturnedLabel[] PersonalStatsLabels = ElementPatterns.CreateArray(index => new UnturnedLabel(
        index <= 5
            ? ("DataEmpty/Scalar/BottomContainer/Stats/ColumnGroupLeft/ColumnLeft/playerstats_"  + index.ToString(CultureInfo.InvariantCulture))
            : ("DataEmpty/Scalar/BottomContainer/Stats/ColumnGroupRight/ColumnLeft/playerstats_" + index.ToString(CultureInfo.InvariantCulture))
    ), 0, to: 11);
    public readonly UnturnedLabel[] PersonalStatsValues = ElementPatterns.CreateArray(index => new UnturnedLabel(
        index <= 5
            ? ("DataEmpty/Scalar/BottomContainer/Stats/ColumnGroupLeft/ColumnLeft/playerstats_"  + index.ToString(CultureInfo.InvariantCulture) + "_v")
            : ("DataEmpty/Scalar/BottomContainer/Stats/ColumnGroupRight/ColumnLeft/playerstats_" + index.ToString(CultureInfo.InvariantCulture) + "_v")
    ), 0, to: 11);

    public ConventionalLeaderboardUI() : base(Gamemodes.Gamemode.Config.UIConventionalLeaderboard.GetId(), reliable: false)
    {
        IsSendReliable = true;
    }

    public void UpdateTime(LanguageSet set, int secondsLeft)
    {
        int time = Mathf.RoundToInt(Gamemodes.Gamemode.Config.GeneralLeaderboardTime);
        string l1 = TimeSpan.FromSeconds(secondsLeft).ToString("m\\:ss", set.CultureInfo);
        string l2 = new string(Gamemodes.Gamemode.Config.UICircleFontCharacters[CTFUI.FromMax(Mathf.RoundToInt(time - secondsLeft), time)], 1);
        while (set.MoveNext())
        {
            NextGameSeconds.SetText(set.Next.Connection, l1);
            NextGameSecondsCircle.SetText(set.Next.Connection, l2);
        }
    }
}
