using System.Collections.Generic;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Util.Inventory;

public interface IItemDistributionService
{
    /// <summary>
    /// Clear the inventory of <paramref name="player"/>.
    /// </summary>
    /// <param name="state">Allows for intercepting clearing specific items.</param>
    /// <returns>The number of items and clothes removed from the player's inventory.</returns>
    int ClearInventory<TState>(WarfarePlayer player, TState state) where TState : IItemClearState;

    /// <summary>
    /// Clear the inventory of <paramref name="player"/>.
    /// </summary>
    /// <returns>The number of items and clothes removed from the player's inventory.</returns>
    int ClearInventory(WarfarePlayer player)
    {
        return ClearInventory(player, new DefaultItemDistributionState());
    }

    /// <summary>
    /// Equip a player with a set of items after clearing it.
    /// </summary>
    /// <param name="state">Allows for intercepting adding specific items and modifying their contents.</param>
    /// <returns>The number of items and clothes added to the player's inventory.</returns>
    int GiveItems<TState>(IEnumerable<IItem> items, WarfarePlayer player, TState state) where TState : IItemDistributionState;

    /// <summary>
    /// Equip a player with a set of items after clearing it.
    /// </summary>
    /// <returns>The number of items and clothes added to the player's inventory.</returns>
    int GiveItems(IEnumerable<IItem> items, WarfarePlayer player)
    {
        return GiveItems(items, player, new DefaultItemDistributionState());
    }

    protected struct DefaultItemDistributionState : IItemDistributionState, IItemClearState
    {
        public bool ClearClothes => true;
        public Kit? Kit => null;
        public Team RequestingTeam => Team.NoTeam;
        public bool Silent => false;
        public bool ShouldGrantItem(IClothingItem item, ref KitItemResolutionResult resolvedItem)
        {
            return true;
        }

        public bool ShouldGrantItem(IPageItem item, ref KitItemResolutionResult resolvedItem, ref byte x, ref byte y, ref Page page, ref byte rotation)
        {
            return true;
        }

        public void OnAddingPreviousItem(in KitItemResolutionResult result, byte x, byte y, byte rot, Page page, Item item)
        {
            
        }

        public void OnDroppingPreviousItem(in KitItemResolutionResult result, Vector3 dropNearbyPosition, Item item)
        {

        }

        public bool ShouldClearItem(ClothingType clothingType, ItemAsset clothing, byte[] state, byte quality)
        {
            return true;
        }

        public bool ShouldClearItem(ItemJar jar, Page page, ItemAsset item)
        {
            return true;
        }
    }
}

public interface IItemClearState
{
    bool ClearClothes { get; }
    bool Silent { get; }

    bool ShouldClearItem(ClothingType clothingType, ItemAsset clothing, byte[] state, byte quality);
    bool ShouldClearItem(ItemJar jar, Page page, ItemAsset item);
}

public interface IItemDistributionState
{
    Kit? Kit { get; }
    Team RequestingTeam { get; }
    bool Silent { get; }

    bool ShouldGrantItem(IClothingItem item, ref KitItemResolutionResult resolvedItem);
    bool ShouldGrantItem(IPageItem item, ref KitItemResolutionResult resolvedItem, ref byte x, ref byte y, ref Page page, ref byte rotation);
    void OnAddingPreviousItem(in KitItemResolutionResult result, byte x, byte y, byte rot, Page page, Item item);
    void OnDroppingPreviousItem(in KitItemResolutionResult result, Vector3 dropNearbyPosition, Item item);
}