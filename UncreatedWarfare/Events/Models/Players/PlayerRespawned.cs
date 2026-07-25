namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Invoked after a player respawns from being dead.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class PlayerRespawned : PlayerEvent
{
    /// <summary>
    /// The location the player respawned at, including the <c>0.5m</c> vertical offset.
    /// </summary>
    public required Vector3 RespawnPosition { get; init; }

    /// <summary>
    /// The rotation the player respawned at.
    /// </summary>
    public required Quaternion RespawnRotation { get; init; }

    /// <summary>
    /// The yaw of the rotation the player respawned at.
    /// </summary>
    public required float RespawnAngle { get; init; }

    /// <summary>
    /// Whether or not the player tried to respawn at their bed.
    /// </summary>
    public required bool AtHome { get; init; }
}