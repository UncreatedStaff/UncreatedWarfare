using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Kits.Items;

public interface IKitItemResolver
{
    KitItemResolutionResult ResolveKitItem(IItem item, Kit? kit, Team requestingTeam);

    bool ContainsItem(Kit kitWithItems, IAssetLink<ItemAsset> asset, Team requestingTeam);

    int CountItems(Kit kitWithItems, IAssetLink<ItemAsset> asset, Team requestingTeam);
}

public struct KitItemResolutionResult
{
    public ItemAsset? Asset { get; set; }
    public byte[] State { get; set; }
    public byte Amount { get; set; }
    public byte Quality { get; set; }

    public KitItemResolutionResult(ItemAsset? asset, byte[] state, byte amount, byte quality)
    {
        Asset = asset;
        State = state;
        Amount = amount;
        Quality = quality;
    }
}