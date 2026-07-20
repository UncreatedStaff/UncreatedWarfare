using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Kits.Cosmetics;

/// <summary>
/// Handles deciding whether players can see cosmetics or not, and makes sure that the correct players are seeing the correct sets of armor.
/// </summary>
/// <remarks>Layout-scoped.</remarks>
public class CosmeticInstancer
{
    private readonly ICosmeticItemProvider _itemProvider;

    private bool[] _isInstancedTable =
    [
        false, // Shirt
        false, // Pants
        false,
        false,
        true,
        false,
        true
    ];

    public bool IsEnabled => _itemProvider.IsEnabled;

    public CosmeticInstancer(ICosmeticItemProvider itemProvider)
    {
        _itemProvider = itemProvider;
    }

    /// <summary>
    /// Determines whether or not to apply clothing instancing to the given player.
    /// </summary>
    public bool ShouldInstance(ClothingType type, WarfarePlayer player)
    {
        if (player.IsOnDuty)
            return false;

        Kit? kit = player.Component<KitPlayerComponent>().ActiveKit?.CachedKit;
        if (kit == null || !_itemProvider.ShouldKitCosmeticsBeInstanced(kit))
            return false;

        return true;
    }
}