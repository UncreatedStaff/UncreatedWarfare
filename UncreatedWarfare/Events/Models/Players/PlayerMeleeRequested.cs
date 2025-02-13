namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Handles when a player melee swings, just before the hit is registered by the server.
/// </summary>
public class PlayerMeleeRequested : CancellablePlayerEvent
{
    /// <summary>
    /// The asset of the melee weapon that the player swung with.
    /// </summary>
    public required ItemMeleeAsset Asset { get; init; }

    /// <summary>
    /// The <see cref="SDG.Unturned.InputInfo"/> associated with the swing.
    /// </summary>
    public required InputInfo InputInfo { get; init; }
}
