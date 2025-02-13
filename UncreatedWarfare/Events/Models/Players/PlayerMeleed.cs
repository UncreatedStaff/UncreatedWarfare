namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Invoked after a player melee's.
/// </summary>
public class PlayerMeleed : PlayerEvent
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
