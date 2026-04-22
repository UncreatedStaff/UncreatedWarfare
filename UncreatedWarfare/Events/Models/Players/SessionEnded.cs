using Uncreated.Warfare.Models.GameData;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Invoked when a session is finalized for a player.
/// </summary>
/// <remarks>Does not support async listeners.</remarks>
[EventModel(EventSynchronizationContext.Pure)]
public sealed class SessionEnded : PlayerEvent
{
    /// <summary>
    /// The ending session session.
    /// </summary>
    public required SessionRecord Session { get; init; }

    /// <summary>
    /// Whether or not this session was pruned due to being insignificant (very short with no events).
    /// </summary>
    public required bool WasPruned { get; init; }

    /// <summary>
    /// The next session.
    /// </summary>
    public SessionRecord? NextSession { get; init; }
}
