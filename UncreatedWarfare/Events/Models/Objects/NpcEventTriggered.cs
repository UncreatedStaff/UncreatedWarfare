using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Objects;

/// <summary>
/// Invoked by <see cref="NPCEventManager.onEvent"/>.
/// </summary>
public class NpcEventTriggered : ConsumableEvent
{
    /// <summary>
    /// Unique ID stored in the .dat file.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The player that invoked the event, if any.
    /// </summary>
    public required WarfarePlayer? Player { get; init; }
}