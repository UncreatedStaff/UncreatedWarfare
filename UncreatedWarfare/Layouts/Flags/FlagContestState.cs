using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Layouts.Flags;
public struct FlagContestState
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
    public static FlagContestState NoPlayers() => new FlagContestState { State = ContestState.NoPlayers };
    public static FlagContestState NotObjective() => new FlagContestState { State = ContestState.NotObjective };
    public static FlagContestState Contested() => new FlagContestState { State = ContestState.Contested };
    public static FlagContestState OneTeamIsLeading(Team team) => new FlagContestState { State = ContestState.OneTeamIsLeading, Leader = team };
}
