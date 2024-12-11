using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events;
partial class EventDispatcher
{
    /// <summary>
    /// Invoked by <see cref="ItemManager.onTakeItemRequested"/> when a player tries to pick up an item. Can be cancelled.
    /// </summary>
    private void ItemManagerOnTakeItemRequested(Player player, byte x, byte y, uint instanceId, byte toX, byte toY, byte toRotation, byte toPage, ItemData itemData, ref bool shouldAllow)
    {
        if (!shouldAllow)
            return;

        shouldAllow = false;

        ItemAsset? asset = itemData.item.GetAsset();
        if (asset == null)
        {
            return;
        }

        WarfarePlayer warfarePlayer = _playerService.GetOnlinePlayer(player);

        int index = ItemUtility.FindItem(instanceId, x, y).Index;
        if (index is < 0 or > ushort.MaxValue)
        {
            return;
        }

        ItemPickupRequested args = new ItemPickupRequested
        {
            Player = warfarePlayer,
            Asset = asset,
            DroppedItem = itemData,
            DestinationPage = (Page)toPage,
            DestinationX = toPage == byte.MaxValue ? byte.MaxValue : toX,
            DestinationY = toPage == byte.MaxValue ? byte.MaxValue : toY,
            DestinationRotation = toPage == byte.MaxValue ? (byte)0 : toRotation,
            DroppedItemCoord = new RegionCoord(x, y),
            DroppedItemIndex = (ushort)index,
            DroppedItemRegion = ItemManager.regions[x, y],
            Item = itemData.item
        };

        // DroppedItemTracker handles the ItemDestroyed invocation.
        EventContinuations.Dispatch(args, this, warfarePlayer.DisconnectToken, out shouldAllow, continuation: args =>
        {
            if (args.AutoFindFreeSpace
                    ? args.Inventory.tryAddItem(args.Item, true)
                    : args.Inventory.tryAddItem(args.Item, args.DestinationX, args.DestinationY, (byte)args.DestinationPage, args.DestinationRotation)
                )
            {
                ItemRegion region = args.DroppedItemRegion;
                bool success;
                if (region.items.Count > args.DroppedItemIndex && region.items[args.DroppedItemIndex] == args.DroppedItem)
                {
                    RegionCoord coord = args.DroppedItemCoord;
                    ItemUtility.DestroyDroppedItem(coord.x, coord.y, args.DroppedItemIndex, false, args.Player, true,
                        args.DestinationPage, args.DestinationX, args.DestinationY, args.DestinationRotation);
                    success = true;
                }
                else
                {
                    success = ItemUtility.DestroyDroppedItem(args.DroppedItem, despawned: false, args.Player, playTakeItemSound: true);
                }

                if (!success)
                {
                    args.PlayerObject.sendMessage(EPlayerMessage.SPACE);
                    return;
                }

                if (args.Equipment is { wasTryingToSelect: false, HasValidUseable: false })
                    args.Animator.sendGesture(EPlayerGesture.PICKUP, true);

                args.PlayerObject.sendStat(EPlayerStat.FOUND_ITEMS);
            }
            else
            {
                args.PlayerObject.sendMessage(EPlayerMessage.SPACE);
            }
        }, needsToContinue: _ => true);
    }
}