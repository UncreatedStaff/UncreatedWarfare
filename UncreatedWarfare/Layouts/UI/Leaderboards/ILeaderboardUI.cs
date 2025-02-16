using System;
using Uncreated.Warfare.Layouts.Phases;

namespace Uncreated.Warfare.Layouts.UI.Leaderboards;
public interface ILeaderboardUI
{
    bool IsActive { get; }

    void Open(LeaderboardSet[] sets, LeaderboardPhase leaderboardPhase);
    void Close();
    void UpdateCountdown(TimeSpan timeLeft);
}