using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Gamemodes.UI;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes;
public abstract class ConventionalLeaderboard<Stats, StatTracker> : Leaderboard<Stats, StatTracker> where Stats : BasePlayerStats where StatTracker : BaseStatTracker<Stats>
{
    protected List<Stats>? statsT1;
    protected List<Stats>? statsT2;
    internal readonly ConventionalLeaderboardUI LeaderboardUI = new ConventionalLeaderboardUI();
    protected override UnturnedUI UI => LeaderboardUI;
    public override void UpdateLeaderboardTimer()
    {
        int sl = Mathf.RoundToInt(secondsLeft);
        foreach (LanguageSet set in Translation.EnumerateLanguageSets())
            LeaderboardUI.UpdateTime(set, sl);
    }
    public abstract void SendLeaderboard(LanguageSet set);
    public override void SendLeaderboard()
    {
        state1 = new bool[Math.Min(LeaderboardUI.Team1PlayerVCs.Length, statsT1!.Count - 1)];
        state2 = new bool[Math.Min(LeaderboardUI.Team2PlayerVCs.Length, statsT2!.Count - 1)];
        foreach (LanguageSet set in Translation.EnumerateLanguageSets())
        {
            while (set.MoveNext()) LeaderboardEx.ApplyLeaderboardModifiers(set.Next);
            set.Reset();
            SendLeaderboard(set);
        }
    }
    public override void OnPlayerJoined(UCPlayer player)
    {
        LanguageSet single = new LanguageSet(player);
        LeaderboardEx.ApplyLeaderboardModifiers(player);
        SendLeaderboard(single);
    }

    bool[]? state1;
    bool[]? state2;
    protected override void Update()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (state1 is null || state2 is null) return;
        float rt = Time.realtimeSinceStartup;
        int num = Math.Min(LeaderboardUI.Team1PlayerVCs.Length + 1, statsT1!.Count);
        for (int i = 1; i < num; i++)
        {
            UCPlayer? pl = statsT1[i].Player;
            if (state1[i - 1])
            {
                if (pl is null || !pl.IsTalking)
                    UpdateStateT1(false, i);
            }
            else if (pl is not null && pl.IsTalking)
                UpdateStateT1(true, i);
        }
        num = Math.Min(LeaderboardUI.Team2PlayerVCs.Length + 1, statsT2!.Count);
        for (int i = 1; i < num; i++)
        {
            UCPlayer? pl = statsT2[i].Player;
            if (state2[i - 1])
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
        state1![index] = newval;
        for (int i = 0; i < Provider.clients.Count; i++)
            LeaderboardUI.Team1PlayerVCs[index].SetVisibility(Provider.clients[i].transportConnection, newval);
    }
    private void UpdateStateT2(bool newval, int index)
    {
        --index;
        state2![index] = newval;
        for (int i = 0; i < Provider.clients.Count; i++)
            LeaderboardUI.Team2PlayerVCs[index].SetVisibility(Provider.clients[i].transportConnection, newval);
    }
}
