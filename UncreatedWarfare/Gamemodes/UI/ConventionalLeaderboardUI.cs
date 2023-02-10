using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Framework.UI;
using Uncreated.Players;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.Hardpoint;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.UI;
public class ConventionalLeaderboardUI : UnturnedUI
{
    internal const string StatFormatTime = "h\\:mm\\:ss";
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

    public readonly UnturnedLabel[] Team1PlayerNames = UnturnedLabel.GetPattern("1N{0}", 14);
    public readonly UnturnedLabel[] Team1PlayerKills = UnturnedLabel.GetPattern("1K{0}", 14);
    public readonly UnturnedLabel[] Team1PlayerDeaths = UnturnedLabel.GetPattern("1D{0}", 14);
    public readonly UnturnedLabel[] Team1PlayerXP = UnturnedLabel.GetPattern("1X{0}", 14);
    public readonly UnturnedLabel[] Team1PlayerCredits = UnturnedLabel.GetPattern("1F{0}", 14);
    public readonly UnturnedLabel[] Team1PlayerCaptures = UnturnedLabel.GetPattern("1C{0}", 14);
    public readonly UnturnedLabel[] Team1PlayerDamage = UnturnedLabel.GetPattern("1T{0}", 14);
    public readonly UnturnedLabel[] Team1PlayerVCs = UnturnedLabel.GetPattern("1VC{0}", 14);

    public readonly UnturnedLabel[] Team2PlayerNames = UnturnedLabel.GetPattern("2N{0}", 14);
    public readonly UnturnedLabel[] Team2PlayerKills = UnturnedLabel.GetPattern("2K{0}", 14);
    public readonly UnturnedLabel[] Team2PlayerDeaths = UnturnedLabel.GetPattern("2D{0}", 14);
    public readonly UnturnedLabel[] Team2PlayerXP = UnturnedLabel.GetPattern("2X{0}", 14);
    public readonly UnturnedLabel[] Team2PlayerCredits = UnturnedLabel.GetPattern("2F{0}", 14);
    public readonly UnturnedLabel[] Team2PlayerCaptures = UnturnedLabel.GetPattern("2C{0}", 14);
    public readonly UnturnedLabel[] Team2PlayerDamage = UnturnedLabel.GetPattern("2T{0}", 14);
    public readonly UnturnedLabel[] Team2PlayerVCs = UnturnedLabel.GetPattern("2VC{0}", 14);

    #region War Stats
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
    #endregion

    #region Personal Stats
    public readonly UnturnedLabel PlayerStatsHeader = new UnturnedLabel("playerstats_header");

    public readonly UnturnedLabel PersonalStats0Label = new UnturnedLabel("playerstats_0");
    public readonly UnturnedLabel PersonalStats1Label = new UnturnedLabel("playerstats_1");
    public readonly UnturnedLabel PersonalStats2Label = new UnturnedLabel("playerstats_2");
    public readonly UnturnedLabel PersonalStats3Label = new UnturnedLabel("playerstats_3");
    public readonly UnturnedLabel PersonalStats4Label = new UnturnedLabel("playerstats_4");
    public readonly UnturnedLabel PersonalStats5Label = new UnturnedLabel("playerstats_5");

    public readonly UnturnedLabel PersonalStats0 = new UnturnedLabel("playerstats_0_v");
    public readonly UnturnedLabel PersonalStats1 = new UnturnedLabel("playerstats_1_v");
    public readonly UnturnedLabel PersonalStats2 = new UnturnedLabel("playerstats_2_v");
    public readonly UnturnedLabel PersonalStats3 = new UnturnedLabel("playerstats_3_v");
    public readonly UnturnedLabel PersonalStats4 = new UnturnedLabel("playerstats_4_v");
    public readonly UnturnedLabel PersonalStats5 = new UnturnedLabel("playerstats_5_v");

    public readonly UnturnedLabel PersonalStats6Label = new UnturnedLabel("playerstats_6");
    public readonly UnturnedLabel PersonalStats7Label = new UnturnedLabel("playerstats_7");
    public readonly UnturnedLabel PersonalStats8Label = new UnturnedLabel("playerstats_8");
    public readonly UnturnedLabel PersonalStats9Label = new UnturnedLabel("playerstats_9");
    public readonly UnturnedLabel PersonalStats10Label = new UnturnedLabel("playerstats_10");
    public readonly UnturnedLabel PersonalStats11Label = new UnturnedLabel("playerstats_11");

    public readonly UnturnedLabel PersonalStats6 = new UnturnedLabel("playerstats_6_v");
    public readonly UnturnedLabel PersonalStats7 = new UnturnedLabel("playerstats_7_v");
    public readonly UnturnedLabel PersonalStats8 = new UnturnedLabel("playerstats_8_v");
    public readonly UnturnedLabel PersonalStats9 = new UnturnedLabel("playerstats_9_v");
    public readonly UnturnedLabel PersonalStats10 = new UnturnedLabel("playerstats_10_v");
    public readonly UnturnedLabel PersonalStats11 = new UnturnedLabel("playerstats_11_v");
    #endregion

    public ConventionalLeaderboardUI() : base(12007, Gamemodes.Gamemode.Config.UIConventionalLeaderboard, true, false) { }
    public void SendCTFLeaderboard<TStats, TStatTracker>(LanguageSet set, in LongestShot info, List<TStats>? t1Stats, List<TStats>? t2Stats, TStatTracker tracker, string? shutdownReason, ulong winner) where TStats : BaseCTFStats where TStatTracker : BaseCTFTracker<TStats>
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        IFormatProvider locale = Localization.GetLocale(set.Language);
        FactionInfo t1 = TeamManager.GetFaction(1), t2 = TeamManager.GetFaction(2);
        string lang = set.Language;
        int len = 47;
        if (t1Stats is not null)
            len += Math.Min(t1Stats.Count, Team1PlayerNames.Length + 1) * 7;
        if (t2Stats is not null)
            len += Math.Min(t2Stats.Count, Team2PlayerNames.Length + 1) * 7;
        string[] values = new string[len];
        int secondsLeft = Mathf.RoundToInt(Gamemodes.Gamemode.Config.GeneralLeaderboardTime);

        values[0] = T.WinnerTitle.Translate(lang, TeamManager.GetFactionSafe(winner)!);
        values[1] = shutdownReason is null ?
            T.StartingSoon.Translate(lang) :
            T.NextGameShutdown.Translate(lang, shutdownReason);

        values[2] = TimeSpan.FromSeconds(secondsLeft).ToString("mm\\:ss", locale);
        values[3] = new string(Gamemodes.Gamemode.Config.UICircleFontCharacters[0], 1);
        values[4] = T.WarstatsHeader.Translate(lang, TeamManager.GetFaction(1), TeamManager.GetFaction(2));

        values[5] = T.CTFWarStats0.Translate(lang);
        values[6] = T.CTFWarStats1.Translate(lang, t1);
        values[7] = T.CTFWarStats2.Translate(lang, t2);
        values[8] = T.CTFWarStats3.Translate(lang);
        values[9] = T.CTFWarStats4.Translate(lang, t1);
        values[10] = T.CTFWarStats5.Translate(lang, t2);
        values[11] = T.CTFWarStats6.Translate(lang, t1);
        values[12] = T.CTFWarStats7.Translate(lang, t2);
        values[13] = T.CTFWarStats8.Translate(lang, t1);
        values[14] = T.CTFWarStats9.Translate(lang, t2);
        values[15] = T.CTFWarStats10.Translate(lang);
        values[16] = T.CTFWarStats11.Translate(lang);

        values[17] = T.CTFPlayerStats0.Translate(lang);
        values[18] = T.CTFPlayerStats1.Translate(lang);
        values[19] = T.CTFPlayerStats2.Translate(lang);
        values[20] = T.CTFPlayerStats3.Translate(lang);
        values[21] = T.CTFPlayerStats4.Translate(lang);
        values[22] = T.CTFPlayerStats5.Translate(lang);
        values[23] = T.CTFPlayerStats6.Translate(lang);
        values[24] = T.CTFPlayerStats7.Translate(lang);
        values[25] = T.CTFPlayerStats8.Translate(lang);
        values[26] = T.CTFPlayerStats9.Translate(lang);
        values[27] = T.CTFPlayerStats10.Translate(lang);
        values[28] = T.CTFPlayerStats11.Translate(lang);

        values[41] = T.CTFHeader0.Translate(lang);
        values[42] = T.CTFHeader1.Translate(lang);
        values[43] = T.CTFHeader2.Translate(lang);
        values[44] = T.CTFHeader3.Translate(lang);
        values[45] = T.CTFHeader4.Translate(lang);
        values[46] = T.CTFHeader5.Translate(lang);

        if (tracker is not null)
        {
            values[29] = tracker.Duration.ToString(StatFormatTime, locale);
            values[30] = tracker.casualtiesT1.ToString(locale);
            values[31] = tracker.casualtiesT2.ToString(locale);
            values[32] = tracker.flagOwnerChanges.ToString(locale);
            values[33] = tracker.AverageTeam1Size.ToString(StatFormatFloat, locale);
            values[34] = tracker.AverageTeam2Size.ToString(StatFormatFloat, locale);
            values[35] = tracker.fobsPlacedT1.ToString(locale);
            values[36] = tracker.fobsPlacedT2.ToString(locale);
            values[37] = tracker.fobsDestroyedT1.ToString(locale);
            values[38] = tracker.fobsDestroyedT2.ToString(locale);
            values[39] = (tracker.teamkillsT1 + tracker.teamkillsT2).ToString(locale);
            values[40] = !info.IsValue ? LeaderboardEx.EmptyFieldNamePlaceholder :
                T.LongestShot.Translate(lang, info.Distance,
                    Assets.find<ItemAsset>(info.Gun),
                    info.Name);
        }
        else
        {
            for (int i = 29; i < 47; ++i)
                values[i] = LeaderboardEx.EmptyFieldPlaceholder;
        }

        int index = 46;
        if (t1Stats is not null && t1Stats.Count > 0)
        {
            int num = Math.Min(t1Stats.Count, Team1PlayerNames.Length + 1);
            for (int i = 0; i < num; ++i)
            {
                TStats stats = t1Stats[i];
                PlayerNames names = stats.Player == null
                    ? stats.cachedNames.HasValue ? stats.cachedNames.Value : new PlayerNames(stats.Steam64)
                    : stats.Player.Name;
                values[++index] = i == 0 ?
                    TeamManager.TranslateShortName(1, lang, true).ToUpperInvariant() : names.CharacterName;
                values[++index] = stats.kills.ToString(locale);
                values[++index] = stats.deaths.ToString(locale);
                values[++index] = stats.XPGained.ToString(locale);
                values[++index] = stats.Credits.ToString(locale);
                values[++index] = stats.Captures.ToString(locale);
                values[++index] = stats.DamageDone.ToString(locale);
            }
        }

        if (t2Stats is not null && t2Stats.Count > 0)
        {
            int num = Math.Min(t2Stats.Count, Team2PlayerNames.Length + 1);
            for (int i = 0; i < num; ++i)
            {
                TStats stats = t2Stats[i];
                PlayerNames names = stats.Player == null
                    ? stats.cachedNames.HasValue ? stats.cachedNames.Value : new PlayerNames(stats.Steam64)
                    : stats.Player.Name;
                values[++index] = i == 0 ?
                    TeamManager.TranslateShortName(2, lang, true).ToUpperInvariant() : names.CharacterName;
                values[++index] = stats.kills.ToString(locale);
                values[++index] = stats.deaths.ToString(locale);
                values[++index] = stats.XPGained.ToString(locale);
                values[++index] = stats.Credits.ToString(locale);
                values[++index] = stats.Captures.ToString(locale);
                values[++index] = stats.DamageDone.ToString(locale);
            }
        }

        while (set.MoveNext())
        {
            UCPlayer pl = set.Next;
            ulong team = pl.GetTeam();
            TStats? stats = team switch
            {
                1 => t1Stats?.Find(x => x.Steam64 == pl.Steam64),
                2 => t2Stats?.Find(x => x.Steam64 == pl.Steam64),
                _ => null
            };
            ITransportConnection c = pl.Connection;

            SendToPlayer(c);

            Title.SetText(c, values[0]);
            if (Data.Gamemode is not null)
                Gamemode.SetText(c, Data.Gamemode.DisplayName);
            else
                Gamemode.SetText(c, string.Empty);

            NextGameStartLabel.SetText(c, values[1]);
            NextGameSeconds.SetText(c, values[2]);
            NextGameSecondsCircle.SetText(c, values[3]);
            TeamStatsHeader.SetText(c, values[4]);

            TeamDurationLabel.SetText(c, values[5]);
            TeamT1CasualtiesLabel.SetText(c, values[6]);
            TeamT2CasualtiesLabel.SetText(c, values[7]);
            TeamFlagCapturesLabel.SetText(c, values[8]);
            TeamT1AveragePlayersLabel.SetText(c, values[9]);
            TeamT2AveragePlayersLabel.SetText(c, values[10]);
            TeamT1FOBsPlacedLabel.SetText(c, values[11]);
            TeamT2FOBsPlacedLabel.SetText(c, values[12]);
            TeamT1FOBsDestroyedLabel.SetText(c, values[13]);
            TeamT2FOBsDestroyedLabel.SetText(c, values[14]);
            TeamTeamkillsLabel.SetText(c, values[15]);
            TeamLongestShotLabel.SetText(c, values[16]);

            PersonalStats0Label.SetText(c, values[17]);
            PersonalStats1Label.SetText(c, values[18]);
            PersonalStats2Label.SetText(c, values[19]);
            PersonalStats3Label.SetText(c, values[20]);
            PersonalStats4Label.SetText(c, values[21]);
            PersonalStats5Label.SetText(c, values[22]);
            PersonalStats6Label.SetText(c, values[23]);
            PersonalStats7Label.SetText(c, values[24]);
            PersonalStats8Label.SetText(c, values[25]);
            PersonalStats9Label.SetText(c, values[26]);
            PersonalStats10Label.SetText(c, values[27]);
            PersonalStats11Label.SetText(c, values[28]);

            if (stats is not null)
            {
                PlayerStatsHeader.SetText(c, T.PlayerstatsHeader.Translate(lang, pl, tracker is not null ? tracker.GetPresence(stats) : 0f));
                PersonalStats0.SetText(c, stats.Kills.ToString(locale));
                PersonalStats1.SetText(c, stats.Deaths.ToString(locale));
                PersonalStats2.SetText(c, stats.KDR.ToString(StatFormatPrecisionFloat, locale));
                PersonalStats3.SetText(c, stats.KillsOnPoint.ToString(locale));
                PersonalStats4.SetText(c, TimeSpan.FromSeconds(stats.timedeployed).ToString(StatFormatTime, locale));
                PersonalStats5.SetText(c, stats.XPGained.ToString(locale));
                PersonalStats6.SetText(c, TimeSpan.FromSeconds(stats.timeonpoint).ToString(StatFormatTime, locale));
                PersonalStats7.SetText(c, stats.Captures.ToString(locale));
                PersonalStats8.SetText(c, stats.DamageDone.ToString(locale));
                PersonalStats9.SetText(c, stats.Teamkills.ToString(locale));
                PersonalStats10.SetText(c, stats.FOBsDestroyed.ToString(locale));
                PersonalStats11.SetText(c, stats.Credits.ToString(locale));
            }
            else
            {
                PlayerStatsHeader.SetText(c, T.PlayerstatsHeader.Translate(lang, pl, 0f));
                PersonalStats0.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats1.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats2.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats3.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats4.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats5.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats6.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats7.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats8.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats9.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats10.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats11.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
            }

            TeamDuration.SetText(c, values[29]);
            TeamT1Casualties.SetText(c, values[30]);
            TeamT2Casualties.SetText(c, values[31]);
            TeamFlagCaptures.SetText(c, values[32]);
            TeamT1AveragePlayers.SetText(c, values[33]);
            TeamT2AveragePlayers.SetText(c, values[34]);
            TeamT1FOBsPlaced.SetText(c, values[35]);
            TeamT2FOBsPlaced.SetText(c, values[36]);
            TeamT1FOBsDestroyed.SetText(c, values[37]);
            TeamT2FOBsDestroyed.SetText(c, values[38]);
            TeamTeamkills.SetText(c, values[39]);
            TeamLongestShot.SetText(c, values[40]);

            Team1Header0.SetText(c, values[41]);
            Team2Header0.SetText(c, values[41]);
            Team1Header1.SetText(c, values[42]);
            Team2Header1.SetText(c, values[42]);
            Team1Header2.SetText(c, values[43]);
            Team2Header2.SetText(c, values[43]);
            Team1Header3.SetText(c, values[44]);
            Team2Header3.SetText(c, values[44]);
            Team1Header4.SetText(c, values[45]);
            Team2Header4.SetText(c, values[45]);
            Team1Header5.SetText(c, values[46]);
            Team2Header5.SetText(c, values[46]);

            index = 46;
            if (t1Stats is not null && t1Stats.Count > 0)
            {
                int num = Math.Min(t1Stats.Count, Team1PlayerNames.Length + 1);
                for (int i = 0; i < num; ++i)
                {
                    if (i == 0)
                    {
                        Team1Name.SetText(c, values[++index]);
                        Team1Kills.SetText(c, values[++index]);
                        Team1Deaths.SetText(c, values[++index]);
                        Team1XP.SetText(c, values[++index]);
                        Team1Credits.SetText(c, values[++index]);
                        Team1Captures.SetText(c, values[++index]);
                        Team1Damage.SetText(c, values[++index]);
                    }
                    else
                    {
                        int i3 = i - 1;
                        if (t1Stats[i].Steam64 == pl.Steam64)
                        {
                            Team1PlayerNames[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerKills[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerDeaths[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerXP[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerCredits[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerCaptures[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerDamage[i3].SetText(c, values[++index].Colorize("dbffdc"));
                        }
                        else
                        {
                            Team1PlayerNames[i3].SetText(c, values[++index]);
                            Team1PlayerKills[i3].SetText(c, values[++index]);
                            Team1PlayerDeaths[i3].SetText(c, values[++index]);
                            Team1PlayerXP[i3].SetText(c, values[++index]);
                            Team1PlayerCredits[i3].SetText(c, values[++index]);
                            Team1PlayerCaptures[i3].SetText(c, values[++index]);
                            Team1PlayerDamage[i3].SetText(c, values[++index]);
                        }
                        if (i != 0)
                            Team1PlayerVCs[i3].SetVisibility(c, false);
                    }
                }
            }
            if (t2Stats is not null && t2Stats.Count > 0)
            {
                int num = Math.Min(t2Stats.Count, Team1PlayerNames.Length + 1);
                for (int i = 0; i < num; ++i)
                {
                    if (i == 0)
                    {
                        Team2Name.SetText(c, values[++index]);
                        Team2Kills.SetText(c, values[++index]);
                        Team2Deaths.SetText(c, values[++index]);
                        Team2XP.SetText(c, values[++index]);
                        Team2Credits.SetText(c, values[++index]);
                        Team2Captures.SetText(c, values[++index]);
                        Team2Damage.SetText(c, values[++index]);
                    }
                    else
                    {
                        int i3 = i - 1;
                        if (t2Stats[i].Steam64 == pl.Steam64)
                        {
                            Team2PlayerNames[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerKills[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerDeaths[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerXP[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerCredits[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerCaptures[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerDamage[i3].SetText(c, values[++index].Colorize("dbffdc"));
                        }
                        else
                        {
                            Team2PlayerNames[i3].SetText(c, values[++index]);
                            Team2PlayerKills[i3].SetText(c, values[++index]);
                            Team2PlayerDeaths[i3].SetText(c, values[++index]);
                            Team2PlayerXP[i3].SetText(c, values[++index]);
                            Team2PlayerCredits[i3].SetText(c, values[++index]);
                            Team2PlayerCaptures[i3].SetText(c, values[++index]);
                            Team2PlayerDamage[i3].SetText(c, values[++index]);
                        }
                        if (i != 0)
                            Team2PlayerVCs[i3].SetVisibility(c, false);
                    }
                }
            }
        }
    }
    public void SendInsurgencyLeaderboard(LanguageSet set, in LongestShot info, List<InsurgencyPlayerStats>? t1Stats, List<InsurgencyPlayerStats>? t2Stats, InsurgencyTracker tracker, string? shutdownReason, ulong winner)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        IFormatProvider locale = Localization.GetLocale(set.Language);
        FactionInfo t1 = TeamManager.GetFaction(1), t2 = TeamManager.GetFaction(2);
        string lang = set.Language;
        int len = 47;
        if (t1Stats is not null)
            len += Math.Min(t1Stats.Count, Team1PlayerNames.Length + 1) * 7;
        if (t2Stats is not null)
            len += Math.Min(t2Stats.Count, Team2PlayerNames.Length + 1) * 7;
        string[] values = new string[len];
        int secondsLeft = Mathf.RoundToInt(Gamemodes.Gamemode.Config.GeneralLeaderboardTime);

        values[0] = T.WinnerTitle.Translate(lang, TeamManager.GetFactionSafe(winner)!);
        values[1] = shutdownReason is null ?
            T.StartingSoon.Translate(lang) :
            T.NextGameShutdown.Translate(lang, shutdownReason);

        values[2] = TimeSpan.FromSeconds(secondsLeft).ToString("mm\\:ss", locale);
        values[3] = new string(Gamemodes.Gamemode.Config.UICircleFontCharacters[0], 1);
        values[4] = T.WarstatsHeader.Translate(lang, TeamManager.GetFaction(1), TeamManager.GetFaction(2));

        values[5] = T.InsurgencyWarStats0.Translate(lang);
        values[6] = T.InsurgencyWarStats1.Translate(lang, t1);
        values[7] = T.InsurgencyWarStats2.Translate(lang, t2);
        values[8] = T.InsurgencyWarStats3.Translate(lang);
        values[9] = T.InsurgencyWarStats4.Translate(lang, t1);
        values[10] = T.InsurgencyWarStats5.Translate(lang, t2);
        values[11] = T.InsurgencyWarStats6.Translate(lang, t1);
        values[12] = T.InsurgencyWarStats7.Translate(lang, t2);
        values[13] = T.InsurgencyWarStats8.Translate(lang, t1);
        values[14] = T.InsurgencyWarStats9.Translate(lang, t2);
        values[15] = T.InsurgencyWarStats10.Translate(lang);
        values[16] = T.InsurgencyWarStats11.Translate(lang);

        values[17] = T.InsurgencyPlayerStats0.Translate(lang);
        values[18] = T.InsurgencyPlayerStats1.Translate(lang);
        values[19] = T.InsurgencyPlayerStats2.Translate(lang);
        values[20] = T.InsurgencyPlayerStats3.Translate(lang);
        values[21] = T.InsurgencyPlayerStats4.Translate(lang);
        values[22] = T.InsurgencyPlayerStats5.Translate(lang);
        values[23] = T.InsurgencyPlayerStats6.Translate(lang);
        values[24] = T.InsurgencyPlayerStats7.Translate(lang);
        values[25] = T.InsurgencyPlayerStats8.Translate(lang);
        values[26] = T.InsurgencyPlayerStats9.Translate(lang);
        values[27] = T.InsurgencyPlayerStats10.Translate(lang);
        values[28] = T.InsurgencyPlayerStats11.Translate(lang);

        values[41] = T.InsurgencyHeader0.Translate(lang);
        values[42] = T.InsurgencyHeader1.Translate(lang);
        values[43] = T.InsurgencyHeader2.Translate(lang);
        values[44] = T.InsurgencyHeader3.Translate(lang);
        values[45] = T.InsurgencyHeader4.Translate(lang);
        values[46] = T.InsurgencyHeader5.Translate(lang);

        if (tracker is not null)
        {
            values[29] = tracker.Duration.ToString(StatFormatTime, locale);
            values[30] = tracker.casualtiesT1.ToString(locale);
            values[31] = tracker.casualtiesT2.ToString(locale);
            values[32] = tracker.intelligenceGathered.ToString(locale);
            values[33] = tracker.AverageTeam1Size.ToString(StatFormatFloat, locale);
            values[34] = tracker.AverageTeam2Size.ToString(StatFormatFloat, locale);
            values[35] = tracker.fobsPlacedT1.ToString(locale);
            values[36] = tracker.fobsPlacedT2.ToString(locale);
            values[37] = tracker.fobsDestroyedT1.ToString(locale);
            values[38] = tracker.fobsDestroyedT2.ToString(locale);
            values[39] = (tracker.teamkillsT1 + tracker.teamkillsT2).ToString(locale);
            ulong pl = info.Player;
            InsurgencyPlayerStats? s;
            values[40] = !info.IsValue ? LeaderboardEx.EmptyFieldNamePlaceholder :
                T.LongestShot.Translate(lang, info.Distance,
                    Assets.find<ItemAsset>(info.Gun),
                    UCPlayer.FromID(info.Player) as IPlayer ?? ((s = (info.Team == 1ul ? t1Stats : t2Stats)?.Find(x => x.Steam64 == pl)) == null ? new PlayerNames(pl) : (s.Player == null
                        ? s.cachedNames.HasValue ? s.cachedNames.Value : new PlayerNames(pl)
                        : s.Player.Name)));
        }
        else
        {
            for (int i = 29; i < 47; ++i)
                values[i] = LeaderboardEx.EmptyFieldPlaceholder;
        }

        int index = 46;
        if (t1Stats is not null && t1Stats.Count > 0)
        {
            int num = Math.Min(t1Stats.Count, Team1PlayerNames.Length + 1);
            for (int i = 0; i < num; ++i)
            {
                InsurgencyPlayerStats stats = t1Stats[i];
                PlayerNames names = stats.Player == null
                    ? stats.cachedNames.HasValue ? stats.cachedNames.Value : new PlayerNames(stats.Steam64)
                    : stats.Player.Name;
                values[++index] = i == 0 ?
                    TeamManager.TranslateShortName(1, lang, true).ToUpperInvariant() : names.CharacterName;
                values[++index] = stats.kills.ToString(locale);
                values[++index] = stats.deaths.ToString(locale);
                values[++index] = stats.XPGained.ToString(locale);
                values[++index] = stats.Credits.ToString(locale);
                values[++index] = stats.KDR.ToString(locale);
                values[++index] = stats.DamageDone.ToString(locale);
            }
        }

        if (t2Stats is not null && t2Stats.Count > 0)
        {
            int num = Math.Min(t2Stats.Count, Team2PlayerNames.Length + 1);
            for (int i = 0; i < num; ++i)
            {
                InsurgencyPlayerStats stats = t2Stats[i];
                PlayerNames names = stats.Player == null
                    ? stats.cachedNames.HasValue ? stats.cachedNames.Value : new PlayerNames(stats.Steam64)
                    : stats.Player.Name;
                values[++index] = i == 0 ?
                    TeamManager.TranslateShortName(2, lang, true).ToUpperInvariant() : names.CharacterName;
                values[++index] = stats.kills.ToString(locale);
                values[++index] = stats.deaths.ToString(locale);
                values[++index] = stats.XPGained.ToString(locale);
                values[++index] = stats.Credits.ToString(locale);
                values[++index] = stats.KDR.ToString(locale);
                values[++index] = stats.DamageDone.ToString(locale);
            }
        }

        while (set.MoveNext())
        {
            UCPlayer pl = set.Next;
            ulong team = pl.GetTeam();
            InsurgencyPlayerStats? stats = team switch
            {
                1 => t1Stats?.Find(x => x.Steam64 == pl.Steam64),
                2 => t2Stats?.Find(x => x.Steam64 == pl.Steam64),
                _ => null
            };
            ITransportConnection c = pl.Connection;

            SendToPlayer(c);

            Title.SetText(c, values[0]);
            Gamemode.SetText(c, Data.Gamemode is not null ? Data.Gamemode.DisplayName : string.Empty);

            NextGameStartLabel.SetText(c, values[1]);
            NextGameSeconds.SetText(c, values[2]);
            NextGameSecondsCircle.SetText(c, values[3]);
            TeamStatsHeader.SetText(c, values[4]);

            TeamDurationLabel.SetText(c, values[5]);
            TeamT1CasualtiesLabel.SetText(c, values[6]);
            TeamT2CasualtiesLabel.SetText(c, values[7]);
            TeamFlagCapturesLabel.SetText(c, values[8]);
            TeamT1AveragePlayersLabel.SetText(c, values[9]);
            TeamT2AveragePlayersLabel.SetText(c, values[10]);
            TeamT1FOBsPlacedLabel.SetText(c, values[11]);
            TeamT2FOBsPlacedLabel.SetText(c, values[12]);
            TeamT1FOBsDestroyedLabel.SetText(c, values[13]);
            TeamT2FOBsDestroyedLabel.SetText(c, values[14]);
            TeamTeamkillsLabel.SetText(c, values[15]);
            TeamLongestShotLabel.SetText(c, values[16]);

            PersonalStats0Label.SetText(c, values[17]);
            PersonalStats1Label.SetText(c, values[18]);
            PersonalStats2Label.SetText(c, values[19]);
            PersonalStats3Label.SetText(c, values[20]);
            PersonalStats4Label.SetText(c, values[21]);
            PersonalStats5Label.SetText(c, values[22]);
            PersonalStats6Label.SetText(c, values[23]);
            PersonalStats7Label.SetText(c, values[24]);
            PersonalStats8Label.SetText(c, values[25]);
            PersonalStats9Label.SetText(c, values[26]);
            PersonalStats10Label.SetText(c, values[27]);
            PersonalStats11Label.SetText(c, values[28]);

            if (stats is not null)
            {
                PlayerStatsHeader.SetText(c, T.PlayerstatsHeader.Translate(lang, pl, tracker is not null ? tracker.GetPresence(stats) : 0f));
                PersonalStats0.SetText(c, stats.Kills.ToString(locale));
                PersonalStats1.SetText(c, stats.Deaths.ToString(locale));
                PersonalStats2.SetText(c, stats.DamageDone.ToString(StatFormatFloat, locale));
                if (Data.Gamemode is IAttackDefense iad)
                    PersonalStats3.SetText(c, (team == iad.AttackingTeam ? stats.KillsAttack : stats.KillsDefense).ToString(locale));
                else
                    PersonalStats3.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats4.SetText(c, TimeSpan.FromSeconds(stats.timedeployed).ToString(StatFormatTime, locale));
                PersonalStats5.SetText(c, stats.XPGained.ToString(locale));
                PersonalStats6.SetText(c, stats._intelligencePointsCollected.ToString(locale));
                PersonalStats7.SetText(c, LeaderboardEx.EmptyFieldPlaceholder /* todo */);
                PersonalStats8.SetText(c, stats._cachesDestroyed.ToString(locale));
                PersonalStats9.SetText(c, stats.Teamkills.ToString(locale));
                PersonalStats10.SetText(c, stats.FOBsDestroyed.ToString(locale));
                PersonalStats11.SetText(c, stats.Credits.ToString(locale));
            }
            else
            {
                PlayerStatsHeader.SetText(c, T.PlayerstatsHeader.Translate(lang, pl, 0f));
                PersonalStats0.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats1.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats2.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats3.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats4.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats5.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats6.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats7.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats8.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats9.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats10.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats11.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
            }

            TeamDuration.SetText(c, values[29]);
            TeamT1Casualties.SetText(c, values[30]);
            TeamT2Casualties.SetText(c, values[31]);
            TeamFlagCaptures.SetText(c, values[32]);
            TeamT1AveragePlayers.SetText(c, values[33]);
            TeamT2AveragePlayers.SetText(c, values[34]);
            TeamT1FOBsPlaced.SetText(c, values[35]);
            TeamT2FOBsPlaced.SetText(c, values[36]);
            TeamT1FOBsDestroyed.SetText(c, values[37]);
            TeamT2FOBsDestroyed.SetText(c, values[38]);
            TeamTeamkills.SetText(c, values[39]);
            TeamLongestShot.SetText(c, values[40]);


            Team1Header0.SetText(c, values[41]);
            Team2Header0.SetText(c, values[41]);
            Team1Header1.SetText(c, values[42]);
            Team2Header1.SetText(c, values[42]);
            Team1Header2.SetText(c, values[43]);
            Team2Header2.SetText(c, values[43]);
            Team1Header3.SetText(c, values[44]);
            Team2Header3.SetText(c, values[44]);
            Team1Header4.SetText(c, values[45]);
            Team2Header4.SetText(c, values[45]);
            Team1Header5.SetText(c, values[46]);
            Team2Header5.SetText(c, values[46]);

            index = 46;
            if (t1Stats is not null && t1Stats.Count > 0)
            {
                int num = Math.Min(t1Stats.Count, Team1PlayerNames.Length + 1);
                for (int i = 0; i < num; ++i)
                {
                    if (i == 0)
                    {
                        Team1Name.SetText(c, values[++index]);
                        Team1Kills.SetText(c, values[++index]);
                        Team1Deaths.SetText(c, values[++index]);
                        Team1XP.SetText(c, values[++index]);
                        Team1Credits.SetText(c, values[++index]);
                        Team1Captures.SetText(c, values[++index]);
                        Team1Damage.SetText(c, values[++index]);
                    }
                    else
                    {
                        int i3 = i - 1;
                        if (t1Stats[i].Steam64 == pl.Steam64)
                        {
                            Team1PlayerNames[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerKills[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerDeaths[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerXP[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerCredits[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerCaptures[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerDamage[i3].SetText(c, values[++index].Colorize("dbffdc"));
                        }
                        else
                        {
                            Team1PlayerNames[i3].SetText(c, values[++index]);
                            Team1PlayerKills[i3].SetText(c, values[++index]);
                            Team1PlayerDeaths[i3].SetText(c, values[++index]);
                            Team1PlayerXP[i3].SetText(c, values[++index]);
                            Team1PlayerCredits[i3].SetText(c, values[++index]);
                            Team1PlayerCaptures[i3].SetText(c, values[++index]);
                            Team1PlayerDamage[i3].SetText(c, values[++index]);
                        }
                        if (i != 0)
                            Team1PlayerVCs[i3].SetVisibility(c, false);
                    }
                }
            }
            if (t2Stats is not null && t2Stats.Count > 0)
            {
                int num = Math.Min(t2Stats.Count, Team1PlayerNames.Length + 1);
                for (int i = 0; i < num; ++i)
                {
                    if (i == 0)
                    {
                        Team2Name.SetText(c, values[++index]);
                        Team2Kills.SetText(c, values[++index]);
                        Team2Deaths.SetText(c, values[++index]);
                        Team2XP.SetText(c, values[++index]);
                        Team2Credits.SetText(c, values[++index]);
                        Team2Captures.SetText(c, values[++index]);
                        Team2Damage.SetText(c, values[++index]);
                    }
                    else
                    {
                        int i3 = i - 1;
                        if (t2Stats[i].Steam64 == pl.Steam64)
                        {
                            Team2PlayerNames[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerKills[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerDeaths[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerXP[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerCredits[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerCaptures[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerDamage[i3].SetText(c, values[++index].Colorize("dbffdc"));
                        }
                        else
                        {
                            Team2PlayerNames[i3].SetText(c, values[++index]);
                            Team2PlayerKills[i3].SetText(c, values[++index]);
                            Team2PlayerDeaths[i3].SetText(c, values[++index]);
                            Team2PlayerXP[i3].SetText(c, values[++index]);
                            Team2PlayerCredits[i3].SetText(c, values[++index]);
                            Team2PlayerCaptures[i3].SetText(c, values[++index]);
                            Team2PlayerDamage[i3].SetText(c, values[++index]);
                        }
                        if (i != 0)
                            Team2PlayerVCs[i3].SetVisibility(c, false);
                    }
                }
            }
        }
    }
    public void SendConquestLeaderboard(LanguageSet set, in LongestShot info, List<ConquestStats>? t1Stats, List<ConquestStats>? t2Stats, ConquestStatTracker tracker, string? shutdownReason, ulong winner)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        IFormatProvider locale = Localization.GetLocale(set.Language);
        FactionInfo t1 = TeamManager.GetFaction(1), t2 = TeamManager.GetFaction(2);
        string lang = set.Language;
        int len = 47;
        if (t1Stats is not null)
            len += Math.Min(t1Stats.Count, Team1PlayerNames.Length + 1) * 7;
        if (t2Stats is not null)
            len += Math.Min(t2Stats.Count, Team2PlayerNames.Length + 1) * 7;
        string[] values = new string[len];
        int secondsLeft = Mathf.RoundToInt(Gamemodes.Gamemode.Config.GeneralLeaderboardTime);

        values[0] = T.WinnerTitle.Translate(lang, TeamManager.GetFactionSafe(winner)!);
        values[1] = shutdownReason is null ?
            T.StartingSoon.Translate(lang) :
            T.NextGameShutdown.Translate(lang, shutdownReason);

        values[2] = TimeSpan.FromSeconds(secondsLeft).ToString("m\\:ss", locale);
        values[3] = new string(Gamemodes.Gamemode.Config.UICircleFontCharacters[0], 1);
        values[4] = T.WarstatsHeader.Translate(lang, TeamManager.GetFaction(1), TeamManager.GetFaction(2));

        values[5] = T.ConquestWarStats0.Translate(lang);
        values[6] = T.ConquestWarStats1.Translate(lang, t1);
        values[7] = T.ConquestWarStats2.Translate(lang, t2);
        values[8] = T.ConquestWarStats3.Translate(lang);
        values[9] = T.ConquestWarStats4.Translate(lang, t1);
        values[10] = T.ConquestWarStats5.Translate(lang, t2);
        values[11] = T.ConquestWarStats6.Translate(lang, t1);
        values[12] = T.ConquestWarStats7.Translate(lang, t2);
        values[13] = T.ConquestWarStats8.Translate(lang, t1);
        values[14] = T.ConquestWarStats9.Translate(lang, t2);
        values[15] = T.ConquestWarStats10.Translate(lang);
        values[16] = T.ConquestWarStats11.Translate(lang);

        values[17] = T.ConquestPlayerStats0.Translate(lang);
        values[18] = T.ConquestPlayerStats1.Translate(lang);
        values[19] = T.ConquestPlayerStats2.Translate(lang);
        values[20] = T.ConquestPlayerStats3.Translate(lang);
        values[21] = T.ConquestPlayerStats4.Translate(lang);
        values[22] = T.ConquestPlayerStats5.Translate(lang);
        values[23] = T.ConquestPlayerStats6.Translate(lang);
        values[24] = T.ConquestPlayerStats7.Translate(lang);
        values[25] = T.ConquestPlayerStats8.Translate(lang);
        values[26] = T.ConquestPlayerStats9.Translate(lang);
        values[27] = T.ConquestPlayerStats10.Translate(lang);
        values[28] = T.ConquestPlayerStats11.Translate(lang);

        values[41] = T.ConquestHeader0.Translate(lang);
        values[42] = T.ConquestHeader1.Translate(lang);
        values[43] = T.ConquestHeader2.Translate(lang);
        values[44] = T.ConquestHeader3.Translate(lang);
        values[45] = T.ConquestHeader4.Translate(lang);
        values[46] = T.ConquestHeader5.Translate(lang);


        if (tracker is not null)
        {
            values[29] = tracker.Duration.ToString(StatFormatTime, locale);
            values[30] = tracker.casualtiesT1.ToString(locale);
            values[31] = tracker.casualtiesT2.ToString(locale);
            values[32] = tracker.flagOwnerChanges.ToString(locale);
            values[33] = tracker.AverageTeam1Size.ToString(StatFormatFloat, locale);
            values[34] = tracker.AverageTeam2Size.ToString(StatFormatFloat, locale);
            values[35] = tracker.fobsPlacedT1.ToString(locale);
            values[36] = tracker.fobsPlacedT2.ToString(locale);
            values[37] = tracker.fobsDestroyedT1.ToString(locale);
            values[38] = tracker.fobsDestroyedT2.ToString(locale);
            values[39] = (tracker.teamkillsT1 + tracker.teamkillsT2).ToString(locale);
            ulong pl = info.Player;
            ConquestStats? s;
            values[40] = !info.IsValue ? LeaderboardEx.EmptyFieldNamePlaceholder :
                T.LongestShot.Translate(lang, info.Distance,
                    Assets.find<ItemAsset>(info.Gun),
                    UCPlayer.FromID(info.Player) as IPlayer ?? ((s = (info.Team == 1ul ? t1Stats : t2Stats)?.Find(x => x.Steam64 == pl)) == null ? new PlayerNames(pl) : (s.Player == null
                        ? s.cachedNames.HasValue ? s.cachedNames.Value : new PlayerNames(pl)
                        : s.Player.Name)));
        }
        else
        {
            for (int i = 29; i < 47; ++i)
                values[i] = LeaderboardEx.EmptyFieldPlaceholder;
        }

        int index = 46;
        if (t1Stats is not null && t1Stats.Count > 0)
        {
            int num = Math.Min(t1Stats.Count, Team1PlayerNames.Length + 1);
            for (int i = 0; i < num; ++i)
            {
                ConquestStats stats = t1Stats[i];
                PlayerNames names = stats.Player == null
                    ? stats.cachedNames.HasValue ? stats.cachedNames.Value : new PlayerNames(stats.Steam64)
                    : stats.Player.Name;
                values[++index] = i == 0 ?
                    TeamManager.TranslateShortName(1, lang, true).ToUpperInvariant() : names.CharacterName;
                values[++index] = stats.kills.ToString(locale);
                values[++index] = stats.deaths.ToString(locale);
                values[++index] = stats.XPGained.ToString(locale);
                values[++index] = stats.Credits.ToString(locale);
                values[++index] = stats.KDR.ToString(locale);
                values[++index] = stats.DamageDone.ToString(locale);
            }
        }

        if (t2Stats is not null && t2Stats.Count > 0)
        {
            int num = Math.Min(t2Stats.Count, Team2PlayerNames.Length + 1);
            for (int i = 0; i < num; ++i)
            {
                ConquestStats stats = t2Stats[i];
                PlayerNames names = stats.Player == null
                    ? stats.cachedNames.HasValue ? stats.cachedNames.Value : new PlayerNames(stats.Steam64)
                    : stats.Player.Name;
                values[++index] = i == 0 ?
                    TeamManager.TranslateShortName(2, lang, true).ToUpperInvariant() : names.CharacterName;
                values[++index] = stats.kills.ToString(locale);
                values[++index] = stats.deaths.ToString(locale);
                values[++index] = stats.XPGained.ToString(locale);
                values[++index] = stats.Credits.ToString(locale);
                values[++index] = stats.KDR.ToString(locale);
                values[++index] = stats.DamageDone.ToString(locale);
            }
        }

        while (set.MoveNext())
        {
            UCPlayer pl = set.Next;
            ulong team = pl.GetTeam();
            ConquestStats? stats = team switch
            {
                1 => t1Stats?.Find(x => x.Steam64 == pl.Steam64),
                2 => t2Stats?.Find(x => x.Steam64 == pl.Steam64),
                _ => null
            };
            ITransportConnection c = pl.Connection;

            SendToPlayer(c);

            Title.SetText(c, values[0]);
            Gamemode.SetText(c, Data.Gamemode is not null ? Data.Gamemode.DisplayName : string.Empty);

            NextGameStartLabel.SetText(c, values[1]);
            NextGameSeconds.SetText(c, values[2]);
            NextGameSecondsCircle.SetText(c, values[3]);
            TeamStatsHeader.SetText(c, values[4]);

            TeamDurationLabel.SetText(c, values[5]);
            TeamT1CasualtiesLabel.SetText(c, values[6]);
            TeamT2CasualtiesLabel.SetText(c, values[7]);
            TeamFlagCapturesLabel.SetText(c, values[8]);
            TeamT1AveragePlayersLabel.SetText(c, values[9]);
            TeamT2AveragePlayersLabel.SetText(c, values[10]);
            TeamT1FOBsPlacedLabel.SetText(c, values[11]);
            TeamT2FOBsPlacedLabel.SetText(c, values[12]);
            TeamT1FOBsDestroyedLabel.SetText(c, values[13]);
            TeamT2FOBsDestroyedLabel.SetText(c, values[14]);
            TeamTeamkillsLabel.SetText(c, values[15]);
            TeamLongestShotLabel.SetText(c, values[16]);

            PersonalStats0Label.SetText(c, values[17]);
            PersonalStats1Label.SetText(c, values[18]);
            PersonalStats2Label.SetText(c, values[19]);
            PersonalStats3Label.SetText(c, values[20]);
            PersonalStats4Label.SetText(c, values[21]);
            PersonalStats5Label.SetText(c, values[22]);
            PersonalStats6Label.SetText(c, values[23]);
            PersonalStats7Label.SetText(c, values[24]);
            PersonalStats8Label.SetText(c, values[25]);
            PersonalStats9Label.SetText(c, values[26]);
            PersonalStats10Label.SetText(c, values[27]);
            PersonalStats11Label.SetText(c, values[28]);

            if (stats is not null)
            {
                PlayerStatsHeader.SetText(c, T.PlayerstatsHeader.Translate(lang, pl, tracker is not null ? tracker.GetPresence(stats) : 0f));
                PersonalStats0.SetText(c, stats.Kills.ToString(locale));
                PersonalStats1.SetText(c, stats.Deaths.ToString(locale));
                PersonalStats2.SetText(c, stats.DamageDone.ToString(StatFormatFloat, locale));
                PersonalStats3.SetText(c, stats.KillsOnPoint.ToString(locale));
                PersonalStats4.SetText(c, TimeSpan.FromSeconds(stats.timedeployed).ToString(StatFormatTime, locale));
                PersonalStats5.SetText(c, stats.XPGained.ToString(locale));
                PersonalStats6.SetText(c, TimeSpan.FromSeconds(stats.timedeployed).ToString(StatFormatTime, locale));
                PersonalStats7.SetText(c, stats.Captures.ToString(locale));
                PersonalStats8.SetText(c, TimeSpan.FromSeconds(stats.timeonpoint).ToString(StatFormatTime, locale));
                PersonalStats9.SetText(c, stats.Teamkills.ToString(locale));
                PersonalStats10.SetText(c, stats.FOBsDestroyed.ToString(locale));
                PersonalStats11.SetText(c, stats.Credits.ToString(locale));
            }
            else
            {
                PlayerStatsHeader.SetText(c, T.PlayerstatsHeader.Translate(lang, pl, 0f));
                PersonalStats0.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats1.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats2.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats3.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats4.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats5.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats6.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats7.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats8.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats9.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats10.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
                PersonalStats11.SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
            }

            TeamDuration.SetText(c, values[29]);
            TeamT1Casualties.SetText(c, values[30]);
            TeamT2Casualties.SetText(c, values[31]);
            TeamFlagCaptures.SetText(c, values[32]);
            TeamT1AveragePlayers.SetText(c, values[33]);
            TeamT2AveragePlayers.SetText(c, values[34]);
            TeamT1FOBsPlaced.SetText(c, values[35]);
            TeamT2FOBsPlaced.SetText(c, values[36]);
            TeamT1FOBsDestroyed.SetText(c, values[37]);
            TeamT2FOBsDestroyed.SetText(c, values[38]);
            TeamTeamkills.SetText(c, values[39]);
            TeamLongestShot.SetText(c, values[40]);


            Team1Header0.SetText(c, values[41]);
            Team2Header0.SetText(c, values[41]);
            Team1Header1.SetText(c, values[42]);
            Team2Header1.SetText(c, values[42]);
            Team1Header2.SetText(c, values[43]);
            Team2Header2.SetText(c, values[43]);
            Team1Header3.SetText(c, values[44]);
            Team2Header3.SetText(c, values[44]);
            Team1Header4.SetText(c, values[45]);
            Team2Header4.SetText(c, values[45]);
            Team1Header5.SetText(c, values[46]);
            Team2Header5.SetText(c, values[46]);

            index = 46;
            if (t1Stats is not null && t1Stats.Count > 0)
            {
                int num = Math.Min(t1Stats.Count, Team1PlayerNames.Length + 1);
                for (int i = 0; i < num; ++i)
                {
                    if (i == 0)
                    {
                        Team1Name.SetText(c, values[++index]);
                        Team1Kills.SetText(c, values[++index]);
                        Team1Deaths.SetText(c, values[++index]);
                        Team1XP.SetText(c, values[++index]);
                        Team1Credits.SetText(c, values[++index]);
                        Team1Captures.SetText(c, values[++index]);
                        Team1Damage.SetText(c, values[++index]);
                    }
                    else
                    {
                        int i3 = i - 1;
                        if (t1Stats[i].Steam64 == pl.Steam64)
                        {
                            Team1PlayerNames[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerKills[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerDeaths[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerXP[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerCredits[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerCaptures[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team1PlayerDamage[i3].SetText(c, values[++index].Colorize("dbffdc"));
                        }
                        else
                        {
                            Team1PlayerNames[i3].SetText(c, values[++index]);
                            Team1PlayerKills[i3].SetText(c, values[++index]);
                            Team1PlayerDeaths[i3].SetText(c, values[++index]);
                            Team1PlayerXP[i3].SetText(c, values[++index]);
                            Team1PlayerCredits[i3].SetText(c, values[++index]);
                            Team1PlayerCaptures[i3].SetText(c, values[++index]);
                            Team1PlayerDamage[i3].SetText(c, values[++index]);
                        }
                        if (i != 0)
                            Team1PlayerVCs[i3].SetVisibility(c, false);
                    }
                }
            }
            if (t2Stats is not null && t2Stats.Count > 0)
            {
                int num = Math.Min(t2Stats.Count, Team1PlayerNames.Length + 1);
                for (int i = 0; i < num; ++i)
                {
                    if (i == 0)
                    {
                        Team2Name.SetText(c, values[++index]);
                        Team2Kills.SetText(c, values[++index]);
                        Team2Deaths.SetText(c, values[++index]);
                        Team2XP.SetText(c, values[++index]);
                        Team2Credits.SetText(c, values[++index]);
                        Team2Captures.SetText(c, values[++index]);
                        Team2Damage.SetText(c, values[++index]);
                    }
                    else
                    {
                        int i3 = i - 1;
                        if (t2Stats[i].Steam64 == pl.Steam64)
                        {
                            Team2PlayerNames[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerKills[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerDeaths[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerXP[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerCredits[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerCaptures[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            Team2PlayerDamage[i3].SetText(c, values[++index].Colorize("dbffdc"));
                        }
                        else
                        {
                            Team2PlayerNames[i3].SetText(c, values[++index]);
                            Team2PlayerKills[i3].SetText(c, values[++index]);
                            Team2PlayerDeaths[i3].SetText(c, values[++index]);
                            Team2PlayerXP[i3].SetText(c, values[++index]);
                            Team2PlayerCredits[i3].SetText(c, values[++index]);
                            Team2PlayerCaptures[i3].SetText(c, values[++index]);
                            Team2PlayerDamage[i3].SetText(c, values[++index]);
                        }
                        if (i != 0)
                            Team2PlayerVCs[i3].SetVisibility(c, false);
                    }
                }
            }
        }
    }
    public void SendHardpointLeaderboard(LanguageSet set, in LongestShot info, List<HardpointPlayerStats>? t1Stats, List<HardpointPlayerStats>? t2Stats, HardpointTracker tracker, string? shutdownReason, ulong winner)
    {
        throw new NotImplementedException(); // todo
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
