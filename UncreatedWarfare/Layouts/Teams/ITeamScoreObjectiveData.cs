using Uncreated.Warfare.Layouts.Phases.Flags;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Layouts.Teams;

/// <summary>
/// Zone data that keeps track of a team's score in relation to other teams.
/// </summary>
public interface ITeamScoreObjectiveData : IObjectiveData
{
    TeamScoreTable Score { get; }
}
