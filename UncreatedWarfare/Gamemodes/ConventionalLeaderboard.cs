using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using SDG.NetTransport;
using Uncreated.Framework.UI;
using Uncreated.Players;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Gamemodes.UI;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes;
public abstract class ConventionalLeaderboard<TStats, TTracker> : Leaderboard<TStats, TTracker> where TStats : BasePlayerStats where TTracker : BaseStatTracker<TStats>
{
    protected List<TStats>? StatsTeam1;
    protected List<TStats>? StatsTeam2;
    bool[]? _vcStateT1;
    bool[]? _vcStateT2;
    internal static readonly ConventionalLeaderboardUI LeaderboardUI = new ConventionalLeaderboardUI();
    protected override UnturnedUI UI => LeaderboardUI;
    protected StatValue[] LeaderboardOverrides { get; set; } = Array.Empty<StatValue>();
    protected StatValue[] WarStatOverrides { get; set; } = Array.Empty<StatValue>();
    protected StatValue[] PlayerStatOverrides { get; set; } = Array.Empty<StatValue>();
    public override void UpdateLeaderboardTimer()
    {
        int sl = Mathf.RoundToInt(SecondsLeft);
        foreach (LanguageSet set in LanguageSet.All())
            LeaderboardUI.UpdateTime(set, sl);
    }
    public abstract void SendLeaderboard(in LanguageSet set);
    public override void SendLeaderboard()
    {
        _vcStateT1 = new bool[Math.Min(LeaderboardUI.Team1PlayerVCs.Length, StatsTeam1!.Count - 1)];
        _vcStateT2 = new bool[Math.Min(LeaderboardUI.Team2PlayerVCs.Length, StatsTeam2!.Count - 1)];
        foreach (LanguageSet set in LanguageSet.All())
        {
            while (set.MoveNext()) LeaderboardEx.ApplyLeaderboardModifiers(set.Next);
            set.Reset();
            try
            {
                SendLeaderboard(in set);
            }
            catch (Exception ex)
            {
                L.LogError("Error sending " + GetType().Name + " to all players.");
                L.LogError(ex);
            }
        }
    }
    public override void OnPlayerJoined(UCPlayer player)
    {
        LanguageSet single = new LanguageSet(player);
        LeaderboardEx.ApplyLeaderboardModifiers(player);
        try
        {
            SendLeaderboard(in single);
        }
        catch (Exception ex)
        {
            L.LogError("Error sending " + GetType().Name + " to " + player.Steam64.ToString(Data.AdminLocale) + ".");
            L.LogError(ex);
        }
    }

    protected override void Update()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (_vcStateT1 is null || _vcStateT2 is null) return;
        int num = Math.Min(LeaderboardUI.Team1PlayerVCs.Length + 1, StatsTeam1!.Count);
        for (int i = 1; i < num; i++)
        {
            UCPlayer? pl = StatsTeam1[i].Player;
            if (_vcStateT1[i - 1])
            {
                if (pl is null || !pl.IsTalking)
                    UpdateStateT1(false, i);
            }
            else if (pl is not null && pl.IsTalking)
                UpdateStateT1(true, i);
        }
        num = Math.Min(LeaderboardUI.Team2PlayerVCs.Length + 1, StatsTeam2!.Count);
        for (int i = 1; i < num; i++)
        {
            UCPlayer? pl = StatsTeam2[i].Player;
            if (_vcStateT2[i - 1])
            {
                if (pl is null || !pl.IsTalking)
                    UpdateStateT2(false, i);
            }
            else if (pl is not null && pl.IsTalking)
                UpdateStateT2(true, i);
        }
    }
    private void UpdateStateT1(bool newval, int index)
    {
        --index;
        _vcStateT1![index] = newval;
        for (int i = 0; i < Provider.clients.Count; i++)
            LeaderboardUI.Team1PlayerVCs[index].SetVisibility(Provider.clients[i].transportConnection, newval);
    }
    private void UpdateStateT2(bool newval, int index)
    {
        --index;
        _vcStateT2![index] = newval;
        for (int i = 0; i < Provider.clients.Count; i++)
            LeaderboardUI.Team2PlayerVCs[index].SetVisibility(Provider.clients[i].transportConnection, newval);
    }
    protected void SendLeaderboardPreset(LanguageSet set)
    {
        if (WarStatOverrides.Length < 11)
            throw new ArgumentException("WarStatOverrides must be at least 11 elements long.");
        if (PlayerStatOverrides.Length < 12)
            throw new ArgumentException("PlayerStatOverrides must be at least 12 elements long.");
        if (LeaderboardOverrides.Length < 6)
            throw new ArgumentException("LeaderboardOverrides must be at least 6 elements long.");
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        CultureInfo culture = set.CultureInfo;
        LanguageInfo lang = set.Language;
        int len = 47;
        if (StatsTeam1 is not null)
            len += Math.Min(StatsTeam1.Count, LeaderboardUI.Team1PlayerNames.Length + 1) * 7;
        if (StatsTeam2 is not null)
            len += Math.Min(StatsTeam2.Count, LeaderboardUI.Team2PlayerNames.Length + 1) * 7;
        string[] values = new string[len];
        int secondsLeft = Mathf.RoundToInt(Gamemode.Config.GeneralLeaderboardTime);

        values[0] = T.WinnerTitle.Translate(lang, TeamManager.GetFactionSafe(Winner)!);
        values[1] = !ShuttingDown || ShuttingDownMessage is null ?
            T.StartingSoon.Translate(lang) :
            T.NextGameShutdown.Translate(lang, ShuttingDownMessage);

        values[2] = TimeSpan.FromSeconds(secondsLeft).ToString("mm\\:ss", culture);
        values[3] = new string(Gamemode.Config.UICircleFontCharacters[0], 1);
        values[4] = T.WarstatsHeader.Translate(lang, TeamManager.GetFaction(1), TeamManager.GetFaction(2));

        values[5] = WarStatOverrides[0].Translate(lang, culture);
        values[6] = WarStatOverrides[1].Translate(lang, culture);
        values[7] = WarStatOverrides[2].Translate(lang, culture);
        values[8] = WarStatOverrides[3].Translate(lang, culture);
        values[9] = WarStatOverrides[4].Translate(lang, culture);
        values[10] = WarStatOverrides[5].Translate(lang, culture);
        values[11] = WarStatOverrides[6].Translate(lang, culture);
        values[12] = WarStatOverrides[7].Translate(lang, culture);
        values[13] = WarStatOverrides[8].Translate(lang, culture);
        values[14] = WarStatOverrides[9].Translate(lang, culture);
        values[15] = WarStatOverrides[10].Translate(lang, culture);                                 // longest shot
        values[16] = WarStatOverrides.Length > 11 ? WarStatOverrides[11].Translate(lang, culture) : T.CTFWarStats11.Translate(lang, culture);

        values[17] = PlayerStatOverrides[0].Translate(lang, culture);
        values[18] = PlayerStatOverrides[1].Translate(lang, culture);
        values[19] = PlayerStatOverrides[2].Translate(lang, culture);
        values[20] = PlayerStatOverrides[3].Translate(lang, culture);
        values[21] = PlayerStatOverrides[4].Translate(lang, culture);
        values[22] = PlayerStatOverrides[5].Translate(lang, culture);
        values[23] = PlayerStatOverrides[6].Translate(lang, culture);
        values[24] = PlayerStatOverrides[7].Translate(lang, culture);
        values[25] = PlayerStatOverrides[8].Translate(lang, culture);
        values[26] = PlayerStatOverrides[9].Translate(lang, culture);
        values[27] = PlayerStatOverrides[10].Translate(lang, culture);
        values[28] = PlayerStatOverrides[11].Translate(lang, culture);

        values[41] = LeaderboardOverrides[0].Translate(lang, culture);
        values[42] = LeaderboardOverrides[1].Translate(lang, culture);
        values[43] = LeaderboardOverrides[2].Translate(lang, culture);
        values[44] = LeaderboardOverrides[3].Translate(lang, culture);
        values[45] = LeaderboardOverrides[4].Translate(lang, culture);
        values[46] = LeaderboardOverrides[5].Translate(lang, culture);

        if (Tracker is not null)
        {
            values[29] = WarStatOverrides[0].GetValue(Tracker, null, culture);
            values[30] = WarStatOverrides[1].GetValue(Tracker, null, culture);
            values[31] = WarStatOverrides[2].GetValue(Tracker, null, culture);
            values[32] = WarStatOverrides[3].GetValue(Tracker, null, culture);
            values[33] = WarStatOverrides[4].GetValue(Tracker, null, culture);
            values[34] = WarStatOverrides[5].GetValue(Tracker, null, culture);
            values[35] = WarStatOverrides[6].GetValue(Tracker, null, culture);
            values[36] = WarStatOverrides[7].GetValue(Tracker, null, culture);
            values[37] = WarStatOverrides[8].GetValue(Tracker, null, culture);
            values[38] = WarStatOverrides[9].GetValue(Tracker, null, culture);
            values[39] = WarStatOverrides[10].GetValue(Tracker, null, culture);
            if (WarStatOverrides.Length > 11)
            {
                values[40] = WarStatOverrides[11].GetValue(Tracker, null, culture);
            }
            else
            {
                // longest shot
                values[40] = Tracker is not ILongestShotTracker { LongestShot.IsValue: true } ls ? LeaderboardEx.EmptyFieldPlaceholder :
                    T.LongestShot.Translate(lang, ls.LongestShot.Distance,
                        Assets.find<ItemAsset>(ls.LongestShot.Gun), ls.LongestShot.Name);
            }
        }
        else
        {
            for (int i = 29; i < 47; ++i)
                values[i] = LeaderboardEx.EmptyFieldPlaceholder;
        }

        int index = 46;
        if (StatsTeam1 is not null && StatsTeam1.Count > 0)
        {
            int num = Math.Min(StatsTeam1.Count, LeaderboardUI.Team1PlayerNames.Length + 1);
            for (int i = 0; i < num; ++i)
            {
                TStats stats = StatsTeam1[i];
                PlayerNames names = stats.Player == null
                    ? stats.cachedNames.HasValue ? stats.cachedNames.Value : new PlayerNames(stats.Steam64)
                    : stats.Player.Name;
                values[++index] = i == 0 ? TeamManager.TranslateShortName(1, lang, true).ToUpperInvariant() : names.CharacterName;
                values[++index] = LeaderboardOverrides[0].GetValue(null, stats, culture);
                values[++index] = LeaderboardOverrides[1].GetValue(null, stats, culture);
                values[++index] = LeaderboardOverrides[2].GetValue(null, stats, culture);
                values[++index] = LeaderboardOverrides[3].GetValue(null, stats, culture);
                values[++index] = LeaderboardOverrides[4].GetValue(null, stats, culture);
                values[++index] = LeaderboardOverrides[5].GetValue(null, stats, culture);
            }
        }

        if (StatsTeam2 is not null && StatsTeam2.Count > 0)
        {
            int num = Math.Min(StatsTeam2.Count, LeaderboardUI.Team2PlayerNames.Length + 1);
            for (int i = 0; i < num; ++i)
            {
                TStats stats = StatsTeam2[i];
                PlayerNames names = stats.Player == null
                    ? stats.cachedNames.HasValue ? stats.cachedNames.Value : new PlayerNames(stats.Steam64)
                    : stats.Player.Name;
                values[++index] = i == 0 ? TeamManager.TranslateShortName(2, lang, true).ToUpperInvariant() : names.CharacterName;
                values[++index] = LeaderboardOverrides[0].GetValue(null, stats, culture);
                values[++index] = LeaderboardOverrides[1].GetValue(null, stats, culture);
                values[++index] = LeaderboardOverrides[2].GetValue(null, stats, culture);
                values[++index] = LeaderboardOverrides[3].GetValue(null, stats, culture);
                values[++index] = LeaderboardOverrides[4].GetValue(null, stats, culture);
                values[++index] = LeaderboardOverrides[5].GetValue(null, stats, culture);
            }
        }

        while (set.MoveNext())
        {
            UCPlayer pl = set.Next;
            ulong team = pl.GetTeam();
            TStats? stats = team switch
            {
                1 => StatsTeam1?.Find(x => x.Steam64 == pl.Steam64),
                2 => StatsTeam2?.Find(x => x.Steam64 == pl.Steam64),
                _ => null
            };
            ITransportConnection c = pl.Connection;

            LeaderboardUI.SendToPlayer(c);

            LeaderboardUI.Title.SetText(c, values[0]);
            LeaderboardUI.Gamemode.SetText(c, Data.Gamemode?.DisplayName ?? string.Empty);

            LeaderboardUI.NextGameStartLabel.SetText(c, values[1]);
            LeaderboardUI.NextGameSeconds.SetText(c, values[2]);
            LeaderboardUI.NextGameSecondsCircle.SetText(c, values[3]);
            LeaderboardUI.TeamStatsHeader.SetText(c, values[4]);

            LeaderboardUI.TeamDurationLabel.SetText(c, values[5]);
            LeaderboardUI.TeamT1CasualtiesLabel.SetText(c, values[6]);
            LeaderboardUI.TeamT2CasualtiesLabel.SetText(c, values[7]);
            LeaderboardUI.TeamFlagCapturesLabel.SetText(c, values[8]);
            LeaderboardUI.TeamT1AveragePlayersLabel.SetText(c, values[9]);
            LeaderboardUI.TeamT2AveragePlayersLabel.SetText(c, values[10]);
            LeaderboardUI.TeamT1FOBsPlacedLabel.SetText(c, values[11]);
            LeaderboardUI.TeamT2FOBsPlacedLabel.SetText(c, values[12]);
            LeaderboardUI.TeamT1FOBsDestroyedLabel.SetText(c, values[13]);
            LeaderboardUI.TeamT2FOBsDestroyedLabel.SetText(c, values[14]);
            LeaderboardUI.TeamTeamkillsLabel.SetText(c, values[15]);
            LeaderboardUI.TeamLongestShotLabel.SetText(c, values[16]);

            for (int i = 0; i < 12; ++i)
                LeaderboardUI.PersonalStatsLabels[i].SetText(c, values[i + 17]);

            if (stats is not null)
            {
                LeaderboardUI.PlayerStatsHeader.SetText(c, T.PlayerstatsHeader.Translate(lang, pl, Tracker is not null ? Tracker.GetPresence(stats) : 0f));
                for (int i = 0; i < 12; ++i)
                    LeaderboardUI.PersonalStatsValues[i].SetText(c, PlayerStatOverrides[i].GetValue(null, stats, culture));
            }
            else
            {
                LeaderboardUI.PlayerStatsHeader.SetText(c, T.PlayerstatsHeader.Translate(lang, pl, 0f));
                for (int i = 0; i < 12; ++i)
                    LeaderboardUI.PersonalStatsValues[i].SetText(c, LeaderboardEx.EmptyFieldPlaceholder);
            }

            LeaderboardUI.TeamDuration.SetText(c, values[29]);
            LeaderboardUI.TeamT1Casualties.SetText(c, values[30]);
            LeaderboardUI.TeamT2Casualties.SetText(c, values[31]);
            LeaderboardUI.TeamFlagCaptures.SetText(c, values[32]);
            LeaderboardUI.TeamT1AveragePlayers.SetText(c, values[33]);
            LeaderboardUI.TeamT2AveragePlayers.SetText(c, values[34]);
            LeaderboardUI.TeamT1FOBsPlaced.SetText(c, values[35]);
            LeaderboardUI.TeamT2FOBsPlaced.SetText(c, values[36]);
            LeaderboardUI.TeamT1FOBsDestroyed.SetText(c, values[37]);
            LeaderboardUI.TeamT2FOBsDestroyed.SetText(c, values[38]);
            LeaderboardUI.TeamTeamkills.SetText(c, values[39]);
            LeaderboardUI.TeamLongestShot.SetText(c, values[40]);

            LeaderboardUI.Team1Header0.SetText(c, values[41]);
            LeaderboardUI.Team2Header0.SetText(c, values[41]);
            LeaderboardUI.Team1Header1.SetText(c, values[42]);
            LeaderboardUI.Team2Header1.SetText(c, values[42]);
            LeaderboardUI.Team1Header2.SetText(c, values[43]);
            LeaderboardUI.Team2Header2.SetText(c, values[43]);
            LeaderboardUI.Team1Header3.SetText(c, values[44]);
            LeaderboardUI.Team2Header3.SetText(c, values[44]);
            LeaderboardUI.Team1Header4.SetText(c, values[45]);
            LeaderboardUI.Team2Header4.SetText(c, values[45]);
            LeaderboardUI.Team1Header5.SetText(c, values[46]);
            LeaderboardUI.Team2Header5.SetText(c, values[46]);

            index = 46;
            if (StatsTeam1 is not null && StatsTeam1.Count > 0)
            {
                int num = Math.Min(StatsTeam1.Count, LeaderboardUI.Team1PlayerNames.Length + 1);
                for (int i = 0; i < num; ++i)
                {
                    if (i == 0)
                    {
                        LeaderboardUI.Team1Name.SetText(c, values[++index]);
                        LeaderboardUI.Team1Kills.SetText(c, values[++index]);
                        LeaderboardUI.Team1Deaths.SetText(c, values[++index]);
                        LeaderboardUI.Team1XP.SetText(c, values[++index]);
                        LeaderboardUI.Team1Credits.SetText(c, values[++index]);
                        LeaderboardUI.Team1Captures.SetText(c, values[++index]);
                        LeaderboardUI.Team1Damage.SetText(c, values[++index]);
                    }
                    else
                    {
                        int i3 = i - 1;
                        if (StatsTeam1[i].Steam64 == pl.Steam64)
                        {
                            LeaderboardUI.Team1PlayerNames[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            LeaderboardUI.Team1PlayerKills[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            LeaderboardUI.Team1PlayerDeaths[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            LeaderboardUI.Team1PlayerXP[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            LeaderboardUI.Team1PlayerCredits[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            LeaderboardUI.Team1PlayerCaptures[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            LeaderboardUI.Team1PlayerDamage[i3].SetText(c, values[++index].Colorize("dbffdc"));
                        }
                        else
                        {
                            LeaderboardUI.Team1PlayerNames[i3].SetText(c, values[++index]);
                            LeaderboardUI.Team1PlayerKills[i3].SetText(c, values[++index]);
                            LeaderboardUI.Team1PlayerDeaths[i3].SetText(c, values[++index]);
                            LeaderboardUI.Team1PlayerXP[i3].SetText(c, values[++index]);
                            LeaderboardUI.Team1PlayerCredits[i3].SetText(c, values[++index]);
                            LeaderboardUI.Team1PlayerCaptures[i3].SetText(c, values[++index]);
                            LeaderboardUI.Team1PlayerDamage[i3].SetText(c, values[++index]);
                        }
                        if (i != 0)
                            LeaderboardUI.Team1PlayerVCs[i3].SetVisibility(c, false);
                    }
                }
            }
            if (StatsTeam2 is not null && StatsTeam2.Count > 0)
            {
                int num = Math.Min(StatsTeam2.Count, LeaderboardUI.Team1PlayerNames.Length + 1);
                for (int i = 0; i < num; ++i)
                {
                    if (i == 0)
                    {
                        LeaderboardUI.Team2Name.SetText(c, values[++index]);
                        LeaderboardUI.Team2Kills.SetText(c, values[++index]);
                        LeaderboardUI.Team2Deaths.SetText(c, values[++index]);
                        LeaderboardUI.Team2XP.SetText(c, values[++index]);
                        LeaderboardUI.Team2Credits.SetText(c, values[++index]);
                        LeaderboardUI.Team2Captures.SetText(c, values[++index]);
                        LeaderboardUI.Team2Damage.SetText(c, values[++index]);
                    }
                    else
                    {
                        int i3 = i - 1;
                        if (StatsTeam2[i].Steam64 == pl.Steam64)
                        {
                            LeaderboardUI.Team2PlayerNames[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            LeaderboardUI.Team2PlayerKills[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            LeaderboardUI.Team2PlayerDeaths[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            LeaderboardUI.Team2PlayerXP[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            LeaderboardUI.Team2PlayerCredits[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            LeaderboardUI.Team2PlayerCaptures[i3].SetText(c, values[++index].Colorize("dbffdc"));
                            LeaderboardUI.Team2PlayerDamage[i3].SetText(c, values[++index].Colorize("dbffdc"));
                        }
                        else
                        {
                            LeaderboardUI.Team2PlayerNames[i3].SetText(c, values[++index]);
                            LeaderboardUI.Team2PlayerKills[i3].SetText(c, values[++index]);
                            LeaderboardUI.Team2PlayerDeaths[i3].SetText(c, values[++index]);
                            LeaderboardUI.Team2PlayerXP[i3].SetText(c, values[++index]);
                            LeaderboardUI.Team2PlayerCredits[i3].SetText(c, values[++index]);
                            LeaderboardUI.Team2PlayerCaptures[i3].SetText(c, values[++index]);
                            LeaderboardUI.Team2PlayerDamage[i3].SetText(c, values[++index]);
                        }
                        if (i != 0)
                            LeaderboardUI.Team2PlayerVCs[i3].SetVisibility(c, false);
                    }
                }
            }
        }
    }
    protected class StatValue
    {
        public Translation Label { get; }
        public Func<TTracker?, TStats?, IFormatProvider, string> GetValue { get; }
        public ulong Team { get; set; }
        public StatValue(Translation label, Func<TTracker?, TStats?, IFormatProvider, string> getValue, ulong team = 0)
        {
            Label = label;
            GetValue = getValue;
            Team = team;
        }

        public string Translate(LanguageInfo? language, CultureInfo? culture)
        {
            language ??= Localization.GetDefaultLanguage();
            culture ??= Localization.GetCultureInfo(language);
            if (Label is Translation<FactionInfo> teamTranslation && Team is 1 or 2)
            {
                return teamTranslation.Translate(language, culture, TeamManager.GetFaction(Team), null, 0ul);
            }

            return Label.Translate(language, culture);
        }
    }
}
