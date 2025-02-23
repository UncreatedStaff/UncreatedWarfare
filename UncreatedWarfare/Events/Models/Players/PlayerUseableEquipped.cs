using Uncreated.Warfare.Kits.Items;

namespace Uncreated.Warfare.Events.Models.Players;

public class PlayerUseableEquipped : PlayerEvent
{
    public required Useable? Useable { get; init; }
    public required ItemAsset? Item { get; init; }
    public required ItemJar? DequippedItem { get; init; }
    public required Page DequippedItemPage { get; init; }
}