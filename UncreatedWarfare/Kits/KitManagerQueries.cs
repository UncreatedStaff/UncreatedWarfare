using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.ItemTracking;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits;

partial class KitManager
{
    /// <remarks>Thread Safe</remarks>
    public async Task<bool> HasAccess(uint kit, CSteamID player, CancellationToken token = default)
    {
        await using ILifetimeScope scope = _lifetimeScope.BeginLifetimeScope();
        await using IKitsDbContext dbContext = scope.Resolve<IKitsDbContext>();

        return await HasAccess(dbContext, kit, player, token);
    }

    /// <remarks>Thread Safe</remarks>
    public async Task<bool> HasAccess(IKitsDbContext dbContext, uint kit, CSteamID player, CancellationToken token = default)
    {
        ulong s64 = player.m_SteamID;
        return kit != 0 && await dbContext.KitAccess.AnyAsync(x => x.KitId == kit && x.Steam64 == s64, token);
    }

    internal async Task<bool> AddAccessRow(uint kit, CSteamID player, KitAccessType type, CancellationToken token = default)
    {
        await using ILifetimeScope scope = _lifetimeScope.BeginLifetimeScope();
        await using IKitsDbContext dbContext = scope.Resolve<IKitsDbContext>();

        ulong s64 = player.m_SteamID;
        if (await dbContext.KitAccess.FirstOrDefaultAsync(kitAccess => kitAccess.Steam64 == s64 && kitAccess.KitId == kit, token) is { } access)
        {
            if (access.AccessType == type)
                return false;

            access.AccessType = type;
            dbContext.Update(access);
        }
        else
        {
            dbContext.Add(new KitAccess
            {
                KitId = kit,
                Steam64 = s64,
                AccessType = type,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        try
        {
            await dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }
    internal async Task<bool> RemoveAccessRow(uint kit, CSteamID player, CancellationToken token = default)
    {
        await using ILifetimeScope scope = _lifetimeScope.BeginLifetimeScope();
        await using IKitsDbContext dbContext = scope.Resolve<IKitsDbContext>();

        ulong s64 = player.m_SteamID;
        List<KitAccess> access = await dbContext.KitAccess
            .Where(x => x.KitId == kit && x.Steam64 == s64)
            .ToListAsync(token)
            .ConfigureAwait(false);

        if (access is not { Count: > 0 })
            return false;

        dbContext.KitAccess.RemoveRange(access);
        try
        {
            await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    /// <remarks>Thread Safe</remarks>
    public async Task<bool> RemoveHotkey(uint kit, CSteamID player, byte slot, CancellationToken token = default)
    {
        if (!KitEx.ValidSlot(slot))
            throw new ArgumentException("Invalid slot number.", nameof(slot));

        await using ILifetimeScope scope = _lifetimeScope.BeginLifetimeScope();
        await using IKitsDbContext dbContext = scope.Resolve<IKitsDbContext>();

        ulong s64 = player.m_SteamID;
        List<KitHotkey> hotkeys = await dbContext.KitHotkeys
            .Where(x => x.KitId == kit && x.Steam64 == s64 && x.Slot == slot)
            .ToListAsync(token)
            .ConfigureAwait(false);

        if (hotkeys is not { Count: > 0 })
            return false;

        dbContext.KitHotkeys.RemoveRange(hotkeys);
        await dbContext.SaveChangesAsync(token).ConfigureAwait(false);

        WarfarePlayer? onlinePlayer = _playerService.GetOnlinePlayerOrNullThreadSafe(player);
        if (onlinePlayer != null)
        {
            _ = Task.Run(async () =>
            {
                await onlinePlayer.PurchaseSync.WaitAsync(onlinePlayer.DisconnectToken);
                try
                {
                    await UniTask.SwitchToMainThread(onlinePlayer.DisconnectToken);
                    if (!onlinePlayer.IsOnline)
                        return;
                    onlinePlayer.Component<HotkeyPlayerComponent>().HotkeyBindings?.RemoveAll(x => x.Slot == slot && x.Kit == kit);
                }
                finally
                {
                    onlinePlayer.PurchaseSync.Release();
                }
            });
        }
        return true;
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<bool> RemoveHotkey(uint kit, ulong player, byte x, byte y, Page page, CancellationToken token = default)
    {
        await using ILifetimeScope scope = _lifetimeScope.BeginLifetimeScope();
        await using IKitsDbContext dbContext = scope.Resolve<IKitsDbContext>();

        List<KitHotkey> hotkeys = await dbContext.KitHotkeys.Where(h => h.KitId == kit && h.Steam64 == player && h.X == x && h.Y == y && h.Page == page)
            .ToListAsync(token).ConfigureAwait(false);

        if (hotkeys is not { Count: > 0 })
            return false;

        dbContext.KitHotkeys.RemoveRange(hotkeys);
        await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        WarfarePlayer? onlinePlayer = _playerService.GetOnlinePlayerOrNull(player);
        if (onlinePlayer != null)
        {
            onlinePlayer.Component<HotkeyPlayerComponent>().HotkeyBindings?.RemoveAll(x => x.Kit == kit && hotkeys.Any(y => y.Slot == x.Slot));
        }
        return true;
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<KitHotkey> AddHotkey(uint kit, ulong player, byte slot, IPageKitItem item, CancellationToken token = default)
    {
        if (item is not ISpecificKitItem and not IAssetRedirectKitItem)
            throw new ArgumentException("Item must also implement IItem or IAssetRedirect.", nameof(item));
        if (!KitEx.ValidSlot(slot))
            throw new ArgumentException("Invalid slot number.", nameof(slot));

        await using ILifetimeScope scope = _lifetimeScope.BeginLifetimeScope();
        await using IKitsDbContext dbContext = scope.Resolve<IKitsDbContext>();

        byte x = item.X, y = item.Y;
        Page page = item.Page;
        List<KitHotkey> hotkeys = await dbContext.KitHotkeys
            .Where(h => h.Kit.PrimaryKey == kit && h.Steam64 == player && (h.X == x && h.Y == y && h.Page == page || h.Slot == slot))
            .ToListAsync(token).ConfigureAwait(false);

        if (hotkeys.Count > 0)
            dbContext.KitHotkeys.RemoveRange(hotkeys);

        KitHotkey kitHotkey = new KitHotkey
        {
            Steam64 = player,
            KitId = kit,
            Item = item is ISpecificKitItem item2 ? item2.Item : null,
            Redirect = item is IAssetRedirectKitItem redir ? redir.RedirectType : null,
            X = x,
            Y = y,
            Page = page,
            Slot = slot
        };
        dbContext.KitHotkeys.Add(kitHotkey);

        await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        return kitHotkey;
    }
    internal async Task SaveFavorites(WarfarePlayer player, IReadOnlyList<uint> favoriteKits, CancellationToken token = default)
    {
        // using CombinedTokenSources tokens = token.CombineTokensIfNeeded(UCWarfare.UnloadCancel);

        await using ILifetimeScope scope = _lifetimeScope.BeginLifetimeScope();
        await using IKitsDbContext dbContext = scope.Resolve<IKitsDbContext>();

        ulong steam64 = player.Steam64.m_SteamID;

        List<KitFavorite> list = await dbContext.KitFavorites.Where(x => x.Steam64 == steam64).ToListAsync(token);
        dbContext.KitFavorites.RemoveRange(list);
        dbContext.KitFavorites.AddRange(favoriteKits.Select(x => new KitFavorite
        {
            Steam64 = steam64,
            KitId = x
        }));

        await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        
        // todo player.KitMenuData.FavoritesDirty = false;
    }
    public async Task ResetLayout(WarfarePlayer player, uint kit, bool lockPurchaseSync, CancellationToken token = default)
    {
        await using ILifetimeScope scope = _lifetimeScope.BeginLifetimeScope();
        await using IKitsDbContext dbContext = scope.Resolve<IKitsDbContext>();

        if (lockPurchaseSync)
            await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ulong steam64 = player.Steam64.m_SteamID;

            player.Component<ItemTrackingPlayerComponent>().LayoutTransformations?.RemoveAll(x => x.Kit == kit);

            List<KitLayoutTransformation> list = await dbContext.KitLayoutTransformations
                .Where(x => x.Steam64 == steam64 && x.KitId == kit)
                .ToListAsync(token).ConfigureAwait(false);

            dbContext.KitLayoutTransformations.RemoveRange(list);

            await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        }
        finally
        {
            if (lockPurchaseSync)
                player.PurchaseSync.Release();
        }
    }
    public async Task SaveLayout(WarfarePlayer player, Kit kit, bool lockPurchaseSync, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);
        List<ItemLayoutTransformationData> active = Layouts.GetLayoutTransformations(player, kit.PrimaryKey);
        List<(Page Page, Item Item, byte X, byte Y, byte Rotation, byte SizeX, byte SizeY)> inventory = new List<(Page, Item, byte, byte, byte, byte, byte)>(24);
        for (int pageIndex = 0; pageIndex < PlayerInventory.STORAGE; ++pageIndex)
        {
            SDG.Unturned.Items page = player.UnturnedPlayer.inventory.items[pageIndex];
            int c = page.getItemCount();
            for (int index = 0; index < c; ++index)
            {
                ItemJar jar = page.getItem((byte)index);
                if (jar.item == null)
                    continue;
                inventory.Add(((Page)pageIndex, jar.item, jar.x, jar.y, jar.rot, jar.size_x, jar.size_y));
            }
        }

        // ensure validity of 'active', remove non-kit items, try to add missing items
        List<IPageKitItem> items = new List<IPageKitItem>(active.Count);
        if (lockPurchaseSync)
            await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            IKitItem[] kitItems = kit.Items;
            for (int i = 0; i < active.Count; ++i)
            {
                ItemLayoutTransformationData t = active[i];
                IPageKitItem? original = (IPageKitItem?)kitItems
                    .FirstOrDefault(x => x is IPageKitItem jar && jar.X == t.OldX && jar.Y == t.OldY && jar.Page == t.OldPage);
                if (original == null)
                {
                    // item is not a part of the kit
                    active.RemoveAtFast(i--);
                    continue;
                }
                if (items.Contains(original))
                {
                    _logger.LogWarning("Duplicate layout transformation for item {0}, skipping.", original);
                    active.RemoveAtFast(i--);
                    continue;
                }
                if (original.X == t.NewX && original.Y == t.NewY && original.Page == t.NewPage && original.Rotation == t.NewRotation)
                {
                    _logger.LogWarning("Identity layout transformation for item {0}, skipping.", original);
                    active.RemoveAtFast(i--);
                    continue;
                }
                items.Add(original);
                _logger.LogDebug("Found active: {0} -> {1}, ({2} -> {3}, {4} -> {5}) new rot: {6}.", t.OldPage, t.NewPage, t.OldX, t.NewX, t.OldY, t.NewY, t.NewRotation);
            }

            // check for missing items
            ulong steam64 = player.Steam64.m_SteamID;
            foreach (IPageKitItem jar in kitItems.OfType<IPageKitItem>().Where(x => !items.Contains(x)))
            {
                _logger.LogDebug("Missing item {0}, trying to fit somewhere.", jar);
                byte sizeX1, sizeY1;
                ItemAsset? asset;
                if (jar is ISpecificKitItem itemSpec)
                {
                    if (itemSpec.Item.TryGetAsset(out ItemAsset ia))
                    {
                        sizeX1 = ia.size_x;
                        sizeY1 = ia.size_y;
                        asset = ia;
                    }
                    else continue;
                }
                else if (jar.GetItem(kit, player.Team, out _, out _, _assetRedirectService, _factionDataStore) is { } ia)
                {
                    sizeX1 = ia.size_x;
                    sizeY1 = ia.size_y;
                    asset = ia;
                }
                else continue;
                (Page Page, Item Item, byte X, byte Y, byte Rotation, byte SizeX, byte SizeY)? colliding = null;
                foreach ((Page Page, Item Item, byte X, byte Y, byte Rotation, byte SizeX, byte SizeY) item in inventory)
                {
                    if (item.Page != jar.Page) continue;
                    if (ItemUtility.IsOverlapping(jar.X, jar.Y, sizeX1, sizeY1, item.X, item.Y, item.SizeX, item.SizeY, jar.Rotation, item.Rotation))
                    {
                        _logger.LogDebug("Found colliding item: {0}, ({1}, {2}).", item.Page, item.X, item.Y);
                        colliding = item;
                        break;
                    }
                }

                if (!colliding.HasValue || colliding.Value.X == jar.X && colliding.Value.Y == jar.Y && colliding.Value.Rotation == jar.Rotation
                    && colliding.Value.Item.GetAsset() is { } ia2 && ia2.GUID == asset.GUID)
                {
                    _logger.LogConditional("Found no collisions (or the collision was the same item).");
                    continue;
                }
                byte origx, origy;
                Page origPage;
                ItemTrackingPlayerComponent comp = player.Component<ItemTrackingPlayerComponent>();
                ItemTransformation t = comp.ItemTransformations.FirstOrDefault(x => x.Item == colliding.Value.Item);
                if (t.Item == null)
                {
                    ItemDropTransformation t2 = comp.ItemDropTransformations.FirstOrDefault(x => x.Item == colliding.Value.Item);
                    if (t.Item == null)
                    {
                        _logger.LogDebug("Unable to find transformations for original item blocking " + jar + ".");
                        continue;
                    }
                    origx = t2.OldX;
                    origy = t2.OldY;
                    origPage = t2.OldPage;
                }
                else
                {
                    origx = t.OldX;
                    origy = t.OldY;
                    origPage = t.OldPage;
                }
                IPageKitItem? orig = (IPageKitItem?)kitItems.FirstOrDefault(x => x is IPageKitItem jar && jar.X == origx && jar.Y == origy && jar.Page == origPage);
                if (orig == null)
                {
                    _logger.LogDebug("Unable to find original item blocking {0}.", jar);
                    continue;
                }
                if (colliding.Value.SizeX == sizeX1 && colliding.Value.SizeY == sizeY1)
                {
                    active.Add(new ItemLayoutTransformationData(jar.Page, orig.Page, jar.X, jar.Y, orig.X, orig.Y, orig.Rotation, kit.PrimaryKey, new KitLayoutTransformation
                    {
                        Steam64 = steam64,
                        Kit = kit,
                        OldX = jar.X,
                        OldY = jar.Y,
                        OldPage = jar.Page,
                        NewX = orig.X,
                        NewY = orig.Y,
                        NewPage = orig.Page,
                        NewRotation = orig.Rotation
                    }));
                    _logger.LogConditional("Moved item to original position of other item: {0} -> {1}.", jar, orig);
                }
                else if (colliding.Value.SizeX == sizeY1 && colliding.Value.SizeY == sizeX1)
                {
                    active.Add(new ItemLayoutTransformationData(jar.Page, orig.Page, jar.X, jar.Y, orig.X, orig.Y, (byte)(orig.Rotation + 1 % 4), kit.PrimaryKey, new KitLayoutTransformation
                    {
                        Steam64 = steam64,
                        Kit = kit,
                        OldX = jar.X,
                        OldY = jar.Y,
                        OldPage = jar.Page,
                        NewX = orig.X,
                        NewY = orig.Y,
                        NewPage = orig.Page,
                        NewRotation = (byte)(orig.Rotation + 1 % 4)
                    }));
                    _logger.LogConditional("Moved item to original position of other item (rotated 90 degrees): {0} -> {1}.", jar, orig);
                }
            }

            uint kitId = kit.PrimaryKey;

            await using ILifetimeScope scope = _lifetimeScope.BeginLifetimeScope();
            await using IKitsDbContext dbContext = scope.Resolve<IKitsDbContext>();

            List<KitLayoutTransformation> existing = await dbContext.KitLayoutTransformations
                .Where(x => x.Steam64 == steam64 && x.KitId == kitId).ToListAsync(token).ConfigureAwait(false);

            dbContext.KitLayoutTransformations.RemoveRange(existing);

            if (active.Count > 0)
                await dbContext.KitLayoutTransformations.AddRangeAsync(active.Select(x => x.Model), token).ConfigureAwait(false);

            await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        }
        finally
        {
            if (lockPurchaseSync)
                player.PurchaseSync.Release();
        }
    }

    internal async Task OnItemsChangedLayoutHandler(IKitItem[] oldItems, Kit kit, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);
        if (kit.PrimaryKey == 0)
            return;
        IKitItem[] newItems = kit.Items;
        List<IPageKitItem> changed = new List<IPageKitItem>(newItems.Length / 2);
        for (int i = 0; i < oldItems.Length; ++i)
        {
            IKitItem old = oldItems[i];

            if (old is IPageKitItem jar)
            {
                for (int k = 0; k < newItems.Length; ++k)
                {
                    if (old.Equals(newItems[k]))
                        goto c;
                }
                changed.Add(jar);
            }
            c: ;
        }

        if (changed.Count == 0)
            return;

        await using ILifetimeScope scope = _lifetimeScope.BeginLifetimeScope();
        await using IKitsDbContext dbContext = scope.Resolve<IKitsDbContext>();

        uint id = kit.PrimaryKey;

        List<KitLayoutTransformation> transformations = await dbContext.KitLayoutTransformations
            .Where(x => x.KitId == id)
            .ToListAsync(token).ConfigureAwait(false);

        transformations.RemoveAll(x => !changed.Any(y => x.OldPage == y.Page && x.OldX == y.X && x.OldY == y.Y));
        dbContext.KitLayoutTransformations.RemoveRange(transformations);
        await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
    }
}