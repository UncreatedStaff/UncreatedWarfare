using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Layouts;

/// <summary>
/// An objective, such as a flag or cache.
/// </summary>
public interface IObjective : ITransformObject
{
    /// <summary>
    /// The display-name of this objective.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The team that owns this objective.
    /// </summary>
    Team Owner { get; }

    /// <summary>
    /// If this objective is currently active in the game (instead of waiting to be activated or something like a cache that hasn't spawned yet).
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Whether or not an opposing team knows about this objective.
    /// </summary>
    bool IsDiscovered(Team team);
}