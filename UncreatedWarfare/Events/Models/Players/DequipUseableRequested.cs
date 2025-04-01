using Uncreated.Warfare.Kits.Items;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Invoked when a player wants to unequip a <see cref="Useable"/>.
/// </summary>
[EventModel(SynchronizationContext = EventSynchronizationContext.PerPlayer, SynchronizedModelTags = [ "modify_inventory", "modify_useable" ])]
public class DequipUseableRequested : CancellablePlayerEvent
{
    public required ItemJar EquppedItem { get; init; }

    public required ItemAsset EquippedAsset { get; init; }

    public required Page EquippedPage { get; init; }
}