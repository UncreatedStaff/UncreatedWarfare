namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Event listener args which handles <see cref="Provider.onBattlEyeKick"/>.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class BattlEyeKicked : PlayerEvent
{
    /// <summary>
    /// The raw reason from BattlEye describing why the player was kicked.
    /// </summary>
    public required string KickReason { get; init; }

    /// <summary>
    /// ID of the global ban on the player, if that was the reason for the kick.
    /// </summary>
    public string? GlobalBanId { get; init; }
}
