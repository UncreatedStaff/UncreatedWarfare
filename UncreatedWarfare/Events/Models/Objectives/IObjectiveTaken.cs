using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Events.Models.Objectives;

/// <summary>
/// Event invoked when an objective is captured or secured.
/// </summary>
public interface IObjectiveTaken
{
    /// <summary>
    /// The team the objective was taken by if it was taken.
    /// </summary>
    Team? Team { get; }

    /// <summary>
    /// The objective that was taken.
    /// </summary>
    /// <remarks>This may be destroyed in some cases.</remarks>
    IObjective Objective { get; }
}