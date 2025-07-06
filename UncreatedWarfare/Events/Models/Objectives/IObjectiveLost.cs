using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Events.Models.Objectives;

/// <summary>
/// Event invoked when an objective is no longer owned by a team.
/// </summary>
public interface IObjectiveLost
{
    /// <summary>
    /// The team that lost the objective, if any.
    /// </summary>
    Team? Team { get; }

    /// <summary>
    /// The objective that was lost.
    /// </summary>
    IObjective Objective { get; }
}