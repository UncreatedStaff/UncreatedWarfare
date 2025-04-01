using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Layouts.Flags;
public struct FlagContestState
{
    public ContestState State;
    public Team? Winner;
    public enum ContestState
    {
        NoPlayers,
        NotObjective,
        Contested,
        OneTeamIsLeading
    }
    public static FlagContestState NoPlayers() => new FlagContestState { State = ContestState.NoPlayers };
    public static FlagContestState NotObjective(Team team) => new FlagContestState { State = ContestState.NotObjective, Winner = team };
    public static FlagContestState Contested() => new FlagContestState { State = ContestState.Contested };
    public static FlagContestState OneTeamIsLeading(Team team) => new FlagContestState { State = ContestState.OneTeamIsLeading, Winner = team };

    public readonly bool Equals(in FlagContestState other)
    {
        return other.State == State && other.Winner == Winner;
    }

    public override string ToString()
    {
        return Winner == null ? EnumUtility.GetNameSafe(State) : $"{EnumUtility.GetNameSafe(State)} - {Winner}";
    }
}
