namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Invoked when a player goes on or off duty.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class PlayerDutyStatusChanged : PlayerEvent
{
    /// <summary>
    /// Whether or not the player is now on duty.
    /// </summary>
    public required bool IsOnDuty { get; init; }
}