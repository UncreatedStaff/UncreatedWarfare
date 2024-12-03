using System;

namespace Uncreated.Warfare.Layouts.UI.Leaderboards;
public interface ILeaderboardUI
{
    bool IsActive { get; }

    void Open(LeaderboardSet[] sets);
    void Close();
    void UpdateCountdown(TimeSpan timeLeft);
}