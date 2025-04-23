using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Events.Models.Squads;

/// <summary>
/// Base event for all <see cref="Warfare.Squads.Squad"/> updates.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public abstract class SquadUpdated
{
    /// <summary>
    /// The squad that the player joined.
    /// </summary>
    public required Squad Squad { get; init; }
}