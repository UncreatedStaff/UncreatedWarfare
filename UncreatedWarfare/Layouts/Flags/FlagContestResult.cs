using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Layouts.Flags;
public struct FlagContestResult
{
    public ContestState State;
    public Team? Leader;
    public enum ContestState
    {
        NoPlayers,
        NotObjective,
        Contested,
        OneTeamIsLeading
    }
    public static FlagContestResult NoPlayers() => new FlagContestResult { State = ContestState.NoPlayers };
    public static FlagContestResult NotObjective() => new FlagContestResult { State = ContestState.NotObjective };
    public static FlagContestResult Contested() => new FlagContestResult { State = ContestState.Contested };
    public static FlagContestResult OneTeamIsLeading(Team team) => new FlagContestResult { State = ContestState.OneTeamIsLeading, Leader = team };
}
