using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.ItemTracking;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Inventory;
using Z.EntityFramework.Plus;

namespace Uncreated.Warfare.Kits;

public class KitLayoutService
{
    private readonly ILogger<KitLayoutService> _logger;
    private readonly DroppedItemTracker _droppedItemTracker;
    private readonly IKitsDbContext _dbContext;
    private readonly IPlayerService? _playerService;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public KitLayoutService(ILogger<KitLayoutService> logger, DroppedItemTracker droppedItemTracker, IKitsDbContext dbContext, IPlayerService? playerService = null)
    {
        _logger = logger;
        _droppedItemTracker = droppedItemTracker;
        _dbContext = dbContext;
        _playerService = playerService;
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
    }

    /// <summary>
    /// Remove all item transformations for the given <paramref name="player"/> for a kit.
    /// </summary>
    /// <returns>If any rows were removed.</returns>
    public async Task<bool> ResetLayoutAsync(CSteamID player, uint kitPrimaryKey, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ulong s64 = player.m_SteamID;
            return await _dbContext.KitLayoutTransformations
                .Where(x => x.Steam64 == s64 && x.KitId == kitPrimaryKey)
                .AsNoTracking()
                .DeleteAsync(token)
                .ConfigureAwait(false) > 0;
        }
        finally
        {
            _dbContext.ChangeTracker.Clear();
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Save the player's current inventory as the layout for their equipped kit.
    /// </summary>
    public async Task<bool> SaveLayoutAsync(WarfarePlayer player, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        uint? kit = player.Component<KitPlayerComponent>().ActiveKitKey;
        if (!kit.HasValue)
            return false;

        List<KitLayoutTransformation> transformations = new List<KitLayoutTransformation>();
        foreach (ItemTransformation transformation in player.Component<ItemTrackingPlayerComponent>().ItemTransformations)
        {
            ItemJar? item = player.GetItemAt(transformation.NewPage, transformation.NewX, transformation.NewY);
            if (item == null)
                continue;

            transformations.Add(new KitLayoutTransformation
            {
                KitId = kit.Value,
                Steam64 = player.Steam64.m_SteamID,
                NewPage = transformation.NewPage,
                NewX = transformation.NewX,
                NewY = transformation.NewY,
                NewRotation = item.rot,
                OldPage = transformation.OldPage,
                OldX = transformation.OldX,
                OldY = transformation.OldY
            });
        }

        if (transformations.Count == 0)
        {
            await ResetLayoutAsync(player.Steam64, kit.Value, token).ConfigureAwait(false);
            return true;
        }

        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            _dbContext.AddRange(transformations);
            
            await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        }
        finally
        {
            _dbContext.ChangeTracker.Clear();
            _semaphore.Release();
        }

        return true;
    }

    /// <summary>
    /// Undoes the player's inventory by moving items back to where the kit has them layed out.
    /// </summary>
    public void TryReverseLayoutTransformations(WarfarePlayer player, Kit kitWithItems)
    {
        GameThread.AssertCurrent();

        IKitItem[] items = kitWithItems.Items;

        PlayerInventory inventory = player.UnturnedPlayer.inventory;
        ItemTrackingPlayerComponent itemTracking = player.Component<ItemTrackingPlayerComponent>();

        List<Item> removedItems = new List<Item>();

        List<IPageItem> pageItems = items.OfType<IPageItem>().ToList();
        List<LocatedItem> locatedItems = new List<LocatedItem>(pageItems.Count);

        for (int i = 0; i < pageItems.Count; ++i)
        {
            IPageItem item = pageItems[i];
            if (!itemTracking.TryGetCurrentItemPosition(item.Page, item.X, item.Y, out Page page, out byte x, out byte y, out bool isDropped, out Item? vanillaItem))
            {
                _logger.LogConditional($"{kitWithItems.Id}::{((IKitItem)item).PrimaryKey,-8} Not found in current inventory.");
                pageItems.RemoveAt(i);
                --i;
                continue;
            }

            if (isDropped)
            {
                _logger.LogConditional($"{kitWithItems.Id}::{((IKitItem)item).PrimaryKey,-8} Item is dropped.");
                pageItems.RemoveAt(i);
                --i;
                continue;
            }

            ItemJar? current = player.GetItemAt(page, x, y, out byte index);
            if (current == null)
                continue;

            LocatedItem location = default;
            location.Item = vanillaItem;
            location.Page = page;
            location.Jar = current;
            location.Index = index;

            locatedItems.Add(location);
        }

        int totalItemCount = 0;
        for (int pg = 0; pg < PlayerInventory.STORAGE; ++pg)
        {
            totalItemCount += inventory.items[pg].getItemCount();
        }

        int ct = 0;
        do
        {
            for (int i = 0; i < pageItems.Count; ++i)
            {
                IPageItem item = pageItems[i];
                LocatedItem location = locatedItems[i];

                Page page = location.Page;
                byte x = location.Jar.x, y = location.Jar.y, rot = location.Jar.rot;

                if (page == item.Page && x == item.X && y == item.Y)
                {
                    if (item.Rotation != rot)
                    {
                        // rotation is only wrong
                        inventory.ReceiveDragItem((byte)page, x, y, (byte)page, x, y, item.Rotation);
                    }

                    // already at the right position
                    continue;
                }

                SDG.Unturned.Items sourcePage = inventory.items[(int)location.Page];
                SDG.Unturned.Items destinationPage = inventory.items[(int)item.Page];

                if (ItemUtility.IsOutOfBounds(destinationPage, item.X, item.Y, location.Jar.size_x, location.Jar.size_y, item.Rotation))
                {
                    _logger.LogConditional($"{kitWithItems.Id}::{((IKitItem)item).PrimaryKey,-8} Item is out of bounds.");
                    continue;
                }

                // try swapping the two items
                ItemJar? swappable = inventory.GetItemAt(item.Page, item.X, item.Y, out byte swappableIndex);
                if (swappable != null && ItemUtility.CanPerformSwap(inventory, swappable, item.Page, location.Jar, page))
                {
                    sourcePage.removeItem(location.Index);
                    destinationPage.removeItem(swappableIndex);

                    destinationPage.addItem(item.X, item.Y, item.Rotation, location.Item);
                    sourcePage.addItem(x, y, rot, swappable.item);

                    location.Page = item.Page;
                    location.Index = (byte)(destinationPage.getItemCount() - 1);
                    location.Jar = destinationPage.getItem(location.Index)!;
                    locatedItems[i] = location;

                    // update moved items
                    for (int j = i + 1; j < locatedItems.Count; ++j)
                    {
                        LocatedItem otherLocation = locatedItems[j];
                        if (otherLocation.Jar != swappable)
                            continue;

                        otherLocation.Page = page;
                        otherLocation.Index = (byte)(sourcePage.getItemCount() - 1);
                        otherLocation.Jar = sourcePage.getItem(otherLocation.Index)!;
                        locatedItems[j] = otherLocation;
                        break;
                    }

                    // update item tracking
                    RemoveItemFromTracking(location.Item, itemTracking);
                    AddTransformation(itemTracking, item.Page, page, item.X, item.Y, x, y, swappable.item);

                    continue;
                }

                // remove blocking items
                int itemCt = destinationPage.getItemCount();
                for (int k = itemCt - 1; k >= 0; --k)
                {
                    ItemJar? itemJar = destinationPage.getItem((byte)k);

                    if (itemJar == location.Jar
                        || !ItemUtility.IsOverlapping(itemJar.x, itemJar.y, itemJar.size_x, itemJar.size_y, item.X, item.Y,
                            location.Jar.size_x, location.Jar.size_y, itemJar.rot, location.Jar.rot))
                    {
                        continue;
                    }

                    removedItems.Add(itemJar.item);
                    destinationPage.removeItem((byte)k);
                    if (!locatedItems.Exists(x => x.Jar == itemJar))
                    {
                        locatedItems.Add(new LocatedItem
                        {
                            Jar = itemJar,
                            Item = itemJar.item,
                            Page = item.Page
                        });
                    }
                }

                sourcePage.removeItem(location.Index);

                destinationPage.addItem(item.X, item.Y, item.Rotation, location.Item);

                location.Page = item.Page;
                location.Index = (byte)(destinationPage.getItemCount() - 1);
                location.Jar = destinationPage.getItem(location.Index)!;
                locatedItems[i] = location;
            }

            bool hasPlayedEffect = false;

            // re-add removed items
            foreach (Item removedItem in removedItems)
            {
                int locationIndex = locatedItems.FindLastIndex(l => l.Item == removedItem);
                if (locationIndex == -1)
                {
                    AddOrDropItem(inventory, removedItem, out _, out _, out _, ref hasPlayedEffect);
                    continue;
                }

                LocatedItem location = locatedItems[locationIndex];
                AddOrDropItem(inventory, removedItem, out Page page, out byte index, out bool isDropped, ref hasPlayedEffect);
                if (isDropped)
                {
                    AddDropTransformation(itemTracking, location.Page, location.Jar.x, location.Jar.y, removedItem);
                }
                else
                {
                    ItemJar? item = inventory.getItem((byte)page, index);
                    if (item != null)
                        AddTransformation(itemTracking, location.Page, page, location.Jar.x, location.Jar.y, item.x, item.y, removedItem);
                }
            }

            ++ct;
            if (ct >= totalItemCount)
            {
                _logger.LogWarning($"{kitWithItems.Id}           Hit maximum reset iteration: {ct}.");
                break;
            }

            removedItems.Clear();
        }
        while (removedItems.Count > 0);
    }

    private void AddOrDropItem(PlayerInventory inventory, Item item, out Page page, out byte index, out bool isDropped, ref bool hasPlayedEffect)
    {
        isDropped = false;
        for (int pg = PlayerInventory.SLOTS; pg < PlayerInventory.STORAGE; ++pg)
        {
            SDG.Unturned.Items invPage = inventory.items[pg];
            if (!invPage.tryAddItem(item, true))
                continue;

            page = (Page)pg;
            index = (byte)(invPage.getItemCount() - 1);
            return;
        }

        for (int pg = 0; pg < PlayerInventory.SLOTS; ++pg)
        {
            if (!inventory.items[pg].tryAddItem(item, true))
                continue;

            page = (Page)pg;
            index = 0;
            return;
        }

        _droppedItemTracker.SetNextDroppedItemInstigator(item, inventory.channel.owner.playerID.steamID.m_SteamID);
        ItemManager.dropItem(item, inventory.player.transform.position, !hasPlayedEffect, true, true);
        hasPlayedEffect = true;
        isDropped = true;
        page = (Page)byte.MaxValue;
        index = byte.MaxValue;
    }

    private static void AddTransformation(ItemTrackingPlayerComponent itemTracking, Page oldPage, Page newPage, byte oldX, byte oldY, byte newX, byte newY, Item item)
    {
        bool foundTransformation = false;
        for (int t = 0; t < itemTracking.ItemTransformations.Count; ++t)
        {
            ItemTransformation transformation = itemTracking.ItemTransformations[t];
            if (transformation.Item != item)
                continue;

            if (transformation.OldPage != newPage || transformation.OldX != newX || transformation.OldY != newY)
                itemTracking.ItemTransformations[t] = new ItemTransformation(transformation.OldPage, newPage, transformation.OldX, transformation.OldY, newX, newY, item);
            else
                itemTracking.ItemTransformations.RemoveAt(t);
            foundTransformation = true;
            break;
        }

        if (foundTransformation)
            return;

        for (int t = 0; t < itemTracking.ItemDropTransformations.Count; ++t)
        {
            ItemDropTransformation transformation = itemTracking.ItemDropTransformations[t];
            if (transformation.Item != item)
                continue;

            itemTracking.ItemDropTransformations.RemoveAt(t);
            if (transformation.OldPage != newPage || transformation.OldX != newX || transformation.OldY != newY)
                itemTracking.ItemTransformations.Add(new ItemTransformation(transformation.OldPage, newPage, transformation.OldX, transformation.OldY, newX, newY, item));
            foundTransformation = true;
            break;
        }

        if (!foundTransformation && (oldPage != newPage || oldX != newX || oldY != newY))
            itemTracking.ItemTransformations.Add(new ItemTransformation(oldPage, newPage, oldX, oldY, newX, newY, item));
    }

    private static void AddDropTransformation(ItemTrackingPlayerComponent itemTracking, Page oldPage, byte oldX, byte oldY, Item item)
    {
        bool foundTransformation = false;
        for (int t = 0; t < itemTracking.ItemTransformations.Count; ++t)
        {
            ItemTransformation transformation = itemTracking.ItemTransformations[t];
            if (transformation.Item != item)
                continue;

            itemTracking.ItemTransformations.RemoveAt(t);
            itemTracking.ItemDropTransformations.Add(new ItemDropTransformation(transformation.OldPage, transformation.OldX, transformation.OldY, item));
            foundTransformation = true;
            break;
        }

        if (foundTransformation)
            return;

        for (int t = 0; t < itemTracking.ItemDropTransformations.Count; ++t)
        {
            ItemDropTransformation transformation = itemTracking.ItemDropTransformations[t];
            if (transformation.Item == item)
                return;
        }

        if (!foundTransformation)
            itemTracking.ItemDropTransformations.Add(new ItemDropTransformation(oldPage, oldX, oldY, item));
    }

    private static void RemoveItemFromTracking(Item item, ItemTrackingPlayerComponent itemTracking)
    {
        itemTracking.ItemTransformations.RemoveAll(x => x.Item == item);
        itemTracking.ItemDropTransformations.RemoveAll(x => x.Item == item);
    }

    private struct LocatedItem
    {
        public Item Item;
        public ItemJar Jar;
        public byte Index;
        public Page Page;
    }
}
