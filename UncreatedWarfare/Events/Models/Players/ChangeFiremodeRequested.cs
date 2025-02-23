namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Invoked when a player tries to change their firemode on their held gun.
/// </summary>
[EventModel(SynchronizationContext = EventSynchronizationContext.PerPlayer, SynchronizedModelTags = [ "modify_inventory", "modify_useable" ])]
public class ChangeFiremodeRequested : CancellablePlayerEvent
{
    /// <summary>
    /// The asset of the item who's firemode is being changed.
    /// </summary>
    public required ItemGunAsset Asset { get; init; }

    /// <summary>
    /// The item who's firemode is being changed.
    /// </summary>
    public required ItemJar Item { get; init; }

    /// <summary>
    /// The gun who's firemode is being changed.
    /// </summary>
    public required UseableGun Useable { get; init; }

    /// <summary>
    /// The new firemode.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public required EFiremode Firemode { get; set; }

    /// <summary>
    /// The current firemode.
    /// </summary>
    public required EFiremode CurrentFiremode { get; init; }
}