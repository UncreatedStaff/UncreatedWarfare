using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Kits.Cosmetics;

internal class PlayerOptionCosmeticItemProvider : ICosmeticItemProvider
{
    bool ICosmeticItemProvider.IsEnabled => true;

    public ItemClothingAsset? DecideVisibleAsset(WarfarePlayer player, WarfarePlayer onPlayer, in ClothingItem slot, Kit kit)
    {
        return slot.Asset;
    }

    public bool ShouldKitCosmeticsBeInstanced(Kit kit)
    {
        return kit.Type != KitType.Public;
    }
}
