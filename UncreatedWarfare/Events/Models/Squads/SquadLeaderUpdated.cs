using Uncreated.Warfare.Players;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Events.Models.Squads;

/// <summary>
/// Event listener args which fires after a player joines a <see cref="Squad"/>.
/// </summary>
[EventModel(EventSynchronizationContext.Global, SynchronizedModelTags = [ "squads" ])]
public class SquadLeaderUpdated : SquadUpdated
{
    /// <summary>
    /// The player that used to be the squad leader. This player may not be online.
    /// </summary>
    public required WarfarePlayer OldLeader { get; init; }

    /// <summary>
    /// The player that is now the squad leader. This player may not be online.
    /// </summary>
    public required WarfarePlayer NewLeader { get; init; }

    /// <summary>
    /// If the squad had the default 'Xxx's Squad' and was changed to correspond to the new leader.
    /// </summary>
    public required bool DidUpdateName { get; init; }
}