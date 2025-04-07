using Uncreated.Warfare.Kits.Items;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Invoked when a player wants to equip a <see cref="Useable"/>.
/// </summary>
[EventModel(EventSynchronizationContext.PerPlayer, SynchronizedModelTags = [ "modify_inventory", "modify_useable" ])]
public class EquipUseableRequested : CancellablePlayerEvent
{
    public required ItemJar Item { get; init; }

    public required ItemAsset Asset { get; init; }

    public required Page Page { get; init; }
}