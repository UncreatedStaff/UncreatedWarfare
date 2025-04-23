using Uncreated.Warfare.Kits.Items;

namespace Uncreated.Warfare.Events.Models.Players;

[EventModel(EventSynchronizationContext.Pure)]
public class PlayerUseableEquipped : PlayerEvent
{
    public required Useable? Useable { get; init; }
    public required ItemAsset? Item { get; init; }
    public required ItemJar? DequippedItem { get; init; }
    public required Page DequippedItemPage { get; init; }
    public required InteractableVehicle? DequippedVehicle { get; init; }
    public required byte DequippedSeat { get; init; }
}