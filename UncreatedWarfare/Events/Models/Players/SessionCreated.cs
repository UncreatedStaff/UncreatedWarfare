using Uncreated.Warfare.Models.GameData;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Invoked when a session is created for a player.
/// </summary>
/// <remarks>Does not support async listeners.</remarks>
[EventModel(EventSynchronizationContext.Pure)]
public sealed class SessionCreated : PlayerEvent
{
    /// <summary>
    /// The newly-created session.
    /// </summary>
    public required SessionRecord Session { get; init; }

    /// <summary>
    /// The previous session.
    /// </summary>
    public SessionRecord? PreviousSession { get; init; }
}
