using System;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Layouts.UI.Leaderboards;
public interface ILeaderboardUI
{
    bool IsActive { get; }

    void Open(LeaderboardSet[] sets, LeaderboardPhase leaderboardPhase);
    void OpenLate(WarfarePlayer player);
    void Close();
    void UpdateCountdown(TimeSpan timeLeft);
}