using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Patches;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Players.Management;

/// <summary>
/// Tracks which players dropped specific items.
/// </summary>
public class DroppedItemTracker : IHostedService, IEventListener<PlayerLeft>
{
    private static bool _ignoreSpawningItemEvent;

    private readonly PlayerService _playerService;
    private readonly EventDispatcher2 _eventDispatcher;
    private readonly WarfareModule _module;
    private readonly Dictionary<uint, ulong> _itemDroppers = new Dictionary<uint, ulong>(128);
    private readonly PlayerDictionary<List<uint>> _droppedItems = new PlayerDictionary<List<uint>>(Provider.maxPlayers);
    private readonly Dictionary<Item, ulong> _itemsPendingDrop = new Dictionary<Item, ulong>(4);
    private readonly StaticGetter<uint>? _getNextInstanceId = Accessor.GenerateStaticGetter<ItemManager, uint>("instanceCount");

    public DroppedItemTracker(PlayerService playerService, EventDispatcher2 eventDispatcher, WarfareModule module)
    {
        _playerService = playerService;
        _eventDispatcher = eventDispatcher;
        _module = module;
    }

    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        ItemManager.onServerSpawningItemDrop += OnServerSpawningItemDrop;
        ItemUtility.OnItemDestroyed += OnItemDestroyed;

        return UniTask.CompletedTask;
    }

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        ItemManager.onServerSpawningItemDrop -= OnServerSpawningItemDrop;
        ItemUtility.OnItemDestroyed -= OnItemDestroyed;

        return UniTask.CompletedTask;
    }

    /// <summary>
    /// Destroy all items that were dropped by the given player.
    /// </summary>
    /// <returns>Number of items destroyed.</returns>
    public async UniTask<int> DestroyItemsDroppedByPlayerAsync(CSteamID player, bool despawned, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (!_droppedItems.TryGetValue(player, out List<uint>? instanceIds))
        {
            return 0;
        }

        uint[] underlying = instanceIds.GetUnderlyingArrayOrCopy();
        int ct = instanceIds.Count;

        int foundItems = 0;
        foreach (ItemInfo item in ItemUtility.EnumerateDroppedItems())
        {
            uint instanceId = item.Item.instanceID;

            bool found = false;
            for (int i = 0; i < ct; ++i)
            {
                if (underlying[i] != instanceId)
                    continue;

                found = true;
                break;
            }

            if (!found)
                continue;

            RegionCoord region = item.Coord;
            ItemUtility.RemoveDroppedItemUnsafe(region.x, region.y, item.Index, despawned, CSteamID.Nil, false);
            ++foundItems;
        }

        return foundItems;
    }

    [EventListener(Priority = int.MaxValue)]
    void IEventListener<PlayerLeft>.HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        _droppedItems.Remove(e.Player);
    }

    /// <summary>
    /// Get the Steam64 ID of the owner of the given <paramref name="item"/>.
    /// </summary>
    [Pure]
    public CSteamID GetOwner(ItemData item)
    {
        return GetOwner(item.instanceID);
    }

    /// <summary>
    /// Get the Steam64 ID of the owner of the given item instance ID.
    /// </summary>
    [Pure]
    public CSteamID GetOwner(uint itemInstanceId)
    {
        return _itemDroppers.TryGetValue(itemInstanceId, out ulong player) ? Unsafe.As<ulong, CSteamID>(ref player) : CSteamID.Nil;
    }

    /// <summary>
    /// Enumerate all <see cref="ItemData"/> that a player dropped.
    /// </summary>
    /// <remarks>Note that this is a bit slower than <see cref="EnumerateDroppedItemInstanceIds"/> so that should be used when possible.</remarks>
    [Pure]
    public IEnumerable<ItemData> EnumerateDroppedItems(CSteamID player)
    {
        return _droppedItems.TryGetValue(player, out List<uint>? items)
            ? items
                .Select(x => ItemUtility.FindItem(x).Item)
                .Where(x => x != null)
            : Enumerable.Empty<ItemData>();
    }

    /// <summary>
    /// Enumerate the instance IDs of all items that a player dropped.
    /// </summary>
    [Pure]
    public IEnumerable<uint> EnumerateDroppedItemInstanceIds(CSteamID player)
    {
        return _droppedItems.TryGetValue(player, out List<uint>? items) ? items : Enumerable.Empty<uint>();
    }

    private void OnItemDestroyed(in ItemInfo itemInfo, bool despawned, bool pickedUp, CSteamID pickUpPlayer)
    {
        _itemsPendingDrop.Remove(itemInfo.Item.item);
        if (!_itemDroppers.Remove(itemInfo.Item.instanceID, out ulong dropper64))
            return;

        if (_droppedItems.TryGetValue(dropper64, out List<uint>? items))
            items.Remove(itemInfo.Item.item.id);

        ItemDestroyed args = new ItemDestroyed
        {
            DroppedItem = itemInfo.Item,
            Item = itemInfo.Item.item,
            Despawned = despawned,
            PickedUp = pickedUp,
            DropPlayer = _playerService.GetOnlinePlayerOrNull(dropper64),
            DropPlayerId = Unsafe.As<ulong, CSteamID>(ref dropper64),
            PickUpPlayer = pickedUp ? _playerService.GetOnlinePlayerOrNull(pickUpPlayer) : null,
            PickUpPlayerId = pickedUp ? pickUpPlayer : CSteamID.Nil
        };

        _ = _eventDispatcher.DispatchEventAsync(args, CancellationToken.None);
    }

    private void OnServerSpawningItemDrop(Item item, ref Vector3 location, ref bool shouldallow)
    {
        if (_ignoreSpawningItemEvent)
            return;

        if (!shouldallow)
        {
            _itemsPendingDrop.Remove(item);
            return;
        }

        uint instanceId = _getNextInstanceId == null ? uint.MaxValue : _getNextInstanceId() + 1;
        _itemsPendingDrop.Remove(item, out ulong steam64Num);

        CSteamID steam64 = Unsafe.As<ulong, CSteamID>(ref steam64Num);

        ItemSpawning args = new ItemSpawning
        {
            InstanceId = instanceId,
            Position = location,
            Item = item,
            PlayerDroppedId = steam64,
            PlayerDropped = _playerService.GetOnlinePlayerOrNull(steam64),
            IsDroppedByPlayer = PlayerInventoryReceiveDropItem.LastIsDropped,
            IsWideSpread = PlayerInventoryReceiveDropItem.LastWideSpread,
            PlayDropEffect = PlayerInventoryReceiveDropItem.LastPlayEffect
        };

        CombinedTokenSources sources = default;
        CancellationToken token = _module.UnloadToken;
        if (args.PlayerDropped != null)
        {
            sources = token.CombineTokensIfNeeded(args.PlayerDropped.DisconnectToken);
        }

        try
        {
            EventContinuations.Dispatch(args, _eventDispatcher, token, out shouldallow, continuation: args =>
            {
                // ReSharper disable once AccessToDisposedClosure
                sources.Dispose();

                if (args.PlayerDropped is { IsOnline: false } || _getNextInstanceId == null || ItemManager.regions == null || !Regions.checkSafe(args.Position) || item.GetAsset() is not { isPro: false })
                    return;

                uint instanceId = _getNextInstanceId() + 1;
                SavePlayerInstigator(args.PlayerDroppedId, instanceId);

                _ignoreSpawningItemEvent = true;
                try
                {
                    ItemManager.dropItem(args.Item, args.Position, args.PlayDropEffect, args.IsDroppedByPlayer, args.IsWideSpread);
                }
                finally
                {
                    _ignoreSpawningItemEvent = false;
                }
            });
        }
        finally
        {
            if (shouldallow || args.IsActionCancelled)
            {
                sources.Dispose();
            }
        }

        if (!shouldallow)
            return;

        SavePlayerInstigator(steam64, instanceId);
    }

    private void SavePlayerInstigator(CSteamID steam64, uint instanceId)
    {
        if (steam64.GetEAccountType() != EAccountType.k_EAccountTypeIndividual || instanceId == uint.MaxValue)
            return;

        if (_droppedItems.TryGetValue(steam64, out List<uint>? instanceids))
        {
            instanceids.Add(instanceId);
        }
        else
        {
            _droppedItems.Add(steam64, [instanceId]);
        }

        _itemDroppers[instanceId] = steam64.m_SteamID;
    }

    // invoked by PlayerEventDispatcher
    internal void InvokeDropItemRequested(PlayerInventory inv, Item item, ref bool shouldAllow)
    {
        WarfarePlayer player = _playerService.GetOnlinePlayer(inv);

        Vector3 point = inv.transform.position + inv.transform.forward * 0.5f;

        ItemJar? foundJar = null;
        byte foundPage = byte.MaxValue;
        byte foundIndex = byte.MaxValue;
        for (byte page = 0; page < PlayerInventory.AREA; ++page)
        {
            Items pg = inv.items[page];
            int ct = pg.getItemCount();
            for (int i = 0; i < ct; ++i)
            {
                ItemJar jar = pg.getItem((byte)i);
                if (!ReferenceEquals(jar.item, item))
                    continue;

                foundJar = jar;
                foundPage = page;
                foundIndex = (byte)i;
                break;
            }

            if (foundJar != null)
                break;
        }

        ItemRegion? region = null;

        if (!Regions.tryGetCoordinate(point, out byte x, out byte y))
        {
            x = (byte)(Regions.WORLD_SIZE / 2);
            y = (byte)(Regions.WORLD_SIZE / 2);
        }
        else
        {
            region = ItemManager.regions?[x, y];
        }

        DropItemRequested args = new DropItemRequested
        {
            Player = player,
            Region = region,
            RegionPosition = new RegionCoord(x, y),
            Item = item,
            Page = (Page)foundPage,
            Index = foundIndex,
            X = foundJar?.x ?? 0,
            Y = foundJar?.y ?? 0,
            Rotation = foundJar?.rot ?? 0,
            Position = point
        };

        EventContinuations.Dispatch(args, _eventDispatcher, player.DisconnectToken, out shouldAllow, continuation: args =>
        {
            if (!args.Player.IsOnline)
                return;

            _itemsPendingDrop[item] = player.Steam64.m_SteamID;

            ItemManager.dropItem(args.Item, args.Position, true, true, false);
            args.Player.UnturnedPlayer.inventory.removeItem(args.X, args.Y);
            if ((int)args.Page <= PlayerInventory.SLOTS)
            {
                args.Player.UnturnedPlayer.equipment.sendSlot((byte)args.Page);
            }
        });

        if (shouldAllow)
        {
            _itemsPendingDrop[item] = player.Steam64.m_SteamID;
        }
    }
}