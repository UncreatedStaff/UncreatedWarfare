using Uncreated.Warfare.Players;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Events.Models.Squads;

/// <summary>
/// Event listener args which fires after a player joines a <see cref="Squad"/>.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class SquadLockUpdated : SquadUpdated
{
    /// <summary>
    /// If the squad is now locked. The old state is just the opposite of this.
    /// </summary>
    public required bool NewLockState { get; init; }

    /// <summary>
    /// The player who locked/unlocked the squad, if any.
    /// </summary>
    public required WarfarePlayer? Instigator { get; init; }
}