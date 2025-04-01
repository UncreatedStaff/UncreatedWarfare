namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Event listener args which fires after a player attempts to leave their current group from the vanilla Group menu.
/// </summary>
public class PlayerLeaveGroupRequested : CancellablePlayerEvent;