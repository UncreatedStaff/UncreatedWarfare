using Cysharp.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players.Layouts;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits;

partial class KitManager
{
    public async Task DownloadPlayersKitData(IEnumerable<UCPlayer> playerList, bool lockPurchaseSync,
        CancellationToken token = default)
    {
        UCPlayer[] players = playerList.AsArrayFast(true);
        if (players.Length == 0)
            return;
        ulong[] steam64Ids = new ulong[players.Length];
        for (int i = 0; i < players.Length; ++i)
        {
            steam64Ids[i] = players[i].Steam64;
            UCPlayer player = players[i];
            if (player is { IsDownloadingKitData: true, HasDownloadedKitData: false })
            {
                L.LogDebug("Spin-waiting for player kit data for " + player + "...");
                // todo
                return;
            }
            
            player.IsDownloadingKitData = true;
        }

        List<KitHotkey>? bindingsToDelete = null;
        List<KitLayoutTransformation>? layoutsToDelete = null;

        await using IKitsDbContext dbContext = new WarfareDbContext();
        await UniTask.SwitchToMainThread(token);

        if (lockPurchaseSync)
        {
            Task[] tasks = new Task[players.Length];
            CombinedTokenSources[] tknSources = new CombinedTokenSources[players.Length];
            for (int i = 0; i < players.Length; ++i)
            {
                UCPlayer pl = players[i];
                CancellationToken token2 = token;
                tknSources[i] = token2.CombineTokensIfNeeded(pl.DisconnectToken);
                tasks[i] = pl.PurchaseSync.WaitAsync(token2);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            for (int i = 0; i < tknSources.Length; ++i)
                tknSources[i].Dispose();
        }

        await UniTask.SwitchToMainThread(token);

        try
        {
            List<uint>?[] kitOutput = new List<uint>[players.Length];
            List<HotkeyBinding>?[] bindings = new List<HotkeyBinding>[players.Length];
            List<LayoutTransformation>?[] layouts = new List<LayoutTransformation>[players.Length];

            List<KitAccess> kitAccesses = await dbContext.KitAccess.Where(x => steam64Ids.Contains(x.Steam64)).ToListAsync(token).ConfigureAwait(false);
            for (int i = 0; i < kitAccesses.Count; ++i)
            {
                KitAccess access = kitAccesses[i];
                int index = Array.IndexOf(steam64Ids, access.Steam64);
                if (index != -1 && access.KitId > 0)
                {
                    (kitOutput[index] ??= new List<uint>(16)).Add(access.KitId);
                }
            }
            
            List<KitHotkey> kitHotkeys = await dbContext.KitHotkeys
                .Include(x => x.Kit)
                .ThenInclude(x => x.ItemModels)
                .Where(x => steam64Ids.Contains(x.Steam64)).ToListAsync(token).ConfigureAwait(false);

            for (int i = 0; i < kitHotkeys.Count; ++i)
            {
                KitHotkey hotkey = kitHotkeys[i];
                int index = Array.IndexOf(steam64Ids, hotkey.Steam64);
                if (index == -1 || hotkey.KitId == 0)
                    continue;

                bool del = false;
                if (!KitEx.ValidSlot(hotkey.Slot))
                {
                    L.LogWarning("Invalid kit slot (" + hotkey.Slot + ") in player " + hotkey.Steam64 + "'s hotkeys.");
                    del = true;
                }

                IPageKitItem jar;

                UnturnedAssetReference? reference = hotkey.Item;
                if (reference.HasValue)
                {
                    jar = new SpecificPageKitItem(0u, reference.Value, hotkey.X, hotkey.Y, 0, hotkey.Page, 1, Array.Empty<byte>());
                }
                else
                {
                    RedirectType? redir = hotkey.Redirect;
                    if (!redir.HasValue)
                    {
                        L.LogWarning("Failed to read redirect type and GUID from player " + hotkey.Steam64 + "'s hotkeys.");
                        del = true;
                        jar = new AssetRedirectPageKitItem(0u, hotkey.X, hotkey.Y, 0, hotkey.Page, RedirectType.None, null);
                    }
                    else
                    {
                        jar = new AssetRedirectPageKitItem(0u, hotkey.X, hotkey.Y, 0, hotkey.Page, redir.Value, null);
                    }
                }

                HotkeyBinding b = new HotkeyBinding(hotkey.KitId, hotkey.Slot, jar, hotkey);

                if (!del)
                    (bindings[index] ??= new List<HotkeyBinding>(4)).Add(b);
                else
                    (bindingsToDelete ??= new List<KitHotkey>(2)).Add(hotkey);
            }

            List<KitLayoutTransformation> kitLayouts = await dbContext.KitLayoutTransformations
                .Include(x => x.Kit)
                .ThenInclude(x => x.ItemModels)
                .Where(x => steam64Ids.Contains(x.Steam64)).ToListAsync(token).ConfigureAwait(false);

            for (int i = 0; i < kitLayouts.Count; ++i)
            {
                KitLayoutTransformation layout = kitLayouts[i];
                int index = Array.IndexOf(steam64Ids, layout.Steam64);
                if (index == -1 || layout.KitId == 0)
                    continue;

                LayoutTransformation l = new LayoutTransformation(layout.OldPage, layout.NewPage, layout.OldX, layout.OldY,
                    layout.NewX, layout.NewY, layout.NewRotation, layout.KitId, layout);

                (layouts[index] ??= new List<LayoutTransformation>(16)).Add(l);
            }

            KitManager? singleton = GetSingletonQuick();
            if (singleton == null)
                throw new SingletonUnloadedException(typeof(KitManager));

            for (int i = 0; i < players.Length; ++i)
                players[i].AccessibleKits = kitOutput[i] ?? new List<uint>(0);

            for (int p = 0; p < players.Length; ++p)
            {
                UCPlayer player = players[p];
                if (!player.IsOnline)
                    continue;

                List<LayoutTransformation>? layouts2 = layouts[p];
                if (layouts2 is { Count: > 0 })
                {
                    for (int i = 0; i < layouts2.Count; ++i)
                    {
                        LayoutTransformation l = layouts2[i];
                        Kit? kit = l.Model.Kit;
                        if (kit is null)
                        {
                            L.LogWarning("Kit for " + player + "'s layout transformation (" + l.Kit + ") not found.");
                            goto del;
                        }

                        // find matching item
                        IKitItem? item = kit.Items?.FirstOrDefault(x =>
                            x is IPageKitItem jar && jar.Page == l.OldPage && jar.X == l.OldX && jar.Y == l.OldY);
                        if (item == null)
                        {
                            L.LogWarning(player + "'s layout transformation for kit " + l.Kit +
                                         " has an invalid item position: " + l.OldPage + ", (" + l.OldX + ", " +
                                         l.OldY + ").");
                            goto del;
                        }

                        continue;
                    del:
                        (layoutsToDelete ??= new List<KitLayoutTransformation>()).Add(l.Model);
                        layouts2.RemoveAtFast(i);
                        --i;
                    }
                }

                List<HotkeyBinding>? bindings2 = bindings[p];
                if (bindings2 is { Count: > 0 })
                {
                    for (int i = 0; i < bindings2.Count; ++i)
                    {
                        HotkeyBinding b = bindings2[i];
                        Kit? kit = b.Model.Kit;
                        if (kit is null)
                        {
                            L.LogWarning("Kit for " + player + "'s hotkey (" + b.Kit + ") not found.");
                            goto del;
                        }

                        // find matching item
                        IPageKitItem? item = (IPageKitItem?)kit.Items?.FirstOrDefault(x =>
                            x is IPageKitItem jar && jar.Page == b.Item.Page && jar.X == b.Item.X &&
                            jar.Y == b.Item.Y &&
                            (jar is ISpecificKitItem i1 && b.Item is ISpecificKitItem i2 && i1.Item == i2.Item ||
                             jar is IAssetRedirectKitItem r1 &&
                             b.Item is IAssetRedirectKitItem r2 && r1.RedirectType == r2.RedirectType));
                        if (item == null)
                        {
                            L.LogWarning(player + "'s hotkey for kit " + b.Kit + " has an invalid item position: " +
                                         b.Item + ".");
                            goto del;
                        }

                        b.Item = item;
                        bindings2[i] = b;

                        continue;
                    del:
                        (bindingsToDelete ??= new List<KitHotkey>()).Add(b.Model);
                        bindings2.RemoveAtFast(i);
                        --i;
                    }
                }

                player.AccessibleKits = kitOutput[p];
                player.HotkeyBindings = bindings2;
                player.LayoutTransformations = layouts2;
            }
        }
        finally
        {
            for (int i = 0; i < players.Length; ++i)
            {
                UCPlayer player = players[i];
                player.HasDownloadedKitData = true;
                player.IsDownloadingKitData = false;
                if (lockPurchaseSync)
                    player.PurchaseSync.Release();
            }
        }

        if (bindingsToDelete is { Count: > 0 })
            dbContext.KitHotkeys.RemoveRange(bindingsToDelete);
        if (layoutsToDelete is { Count: > 0 })
            dbContext.KitLayoutTransformations.RemoveRange(layoutsToDelete);


        if (bindingsToDelete is { Count: > 0 } || layoutsToDelete is { Count: > 0 })
        {
            await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        }

        await UniTask.SwitchToMainThread(token);
        Signs.UpdateSigns();
    }

    public Task DownloadPlayerKitData(UCPlayer player, bool lockPurchaseSync, CancellationToken token = default) =>
        DownloadPlayersKitData(new UCPlayer[] { player }, lockPurchaseSync, token);

    /// <remarks>Thread Safe</remarks>
    public async Task<bool> HasAccess(uint kit, ulong player, CancellationToken token = default)
    {
        await using IKitsDbContext dbContext = new WarfareDbContext();

        return kit != 0 && await dbContext.KitAccess.AnyAsync(x => x.KitId == kit && x.Steam64 == player, token);
    }
    internal async Task<bool> AddAccessRow(uint kit, ulong player, KitAccessType type, CancellationToken token = default)
    {
        await using IKitsDbContext dbContext = new WarfareDbContext();

        if (await dbContext.KitAccess.FirstOrDefaultAsync(kitAccess => kitAccess.Steam64 == player && kitAccess.KitId == kit, token) is { } access)
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
                Steam64 = player,
                AccessType = type,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

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
    internal async Task<bool> RemoveAccessRow(uint kit, ulong player, CancellationToken token = default)
    {
        await using IKitsDbContext dbContext = new WarfareDbContext();

        List<KitAccess> access = await dbContext.KitAccess
            .Where(x => x.KitId == kit && x.Steam64 == player)
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
    public async Task<bool> RemoveHotkey(uint kit, ulong player, byte slot, CancellationToken token = default)
    {
        if (!KitEx.ValidSlot(slot))
            throw new ArgumentException("Invalid slot number.", nameof(slot));

        await using IKitsDbContext dbContext = new WarfareDbContext();

        List<KitHotkey> hotkeys = await dbContext.KitHotkeys.Where(x => x.KitId == kit && x.Steam64 == player && x.Slot == slot)
            .ToListAsync(token).ConfigureAwait(false);

        if (hotkeys is not { Count: > 0 })
            return false;

        dbContext.KitHotkeys.RemoveRange(hotkeys);
        await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        if (UCWarfare.IsLoaded && UCPlayer.FromID(player) is { IsOnline: true } ucPlayer)
        {
            ucPlayer.HotkeyBindings?.RemoveAll(x => x.Slot == slot && x.Kit == kit);
        }
        return true;
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<bool> RemoveHotkey(uint kit, ulong player, byte x, byte y, Page page, CancellationToken token = default)
    {
        await using IKitsDbContext dbContext = new WarfareDbContext();

        List<KitHotkey> hotkeys = await dbContext.KitHotkeys.Where(h => h.KitId == kit && h.Steam64 == player && h.X == x && h.Y == y && h.Page == page)
            .ToListAsync(token).ConfigureAwait(false);

        if (hotkeys is not { Count: > 0 })
            return false;

        dbContext.KitHotkeys.RemoveRange(hotkeys);
        await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        if (UCWarfare.IsLoaded && UCPlayer.FromID(player) is { IsOnline: true } ucPlayer)
        {
            ucPlayer.HotkeyBindings?.RemoveAll(x => x.Kit == kit && hotkeys.Any(y => y.Slot == x.Slot));
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

        await using IKitsDbContext dbContext = new WarfareDbContext();

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
    internal async Task SaveFavorites(UCPlayer player, IReadOnlyList<uint> favoriteKits, CancellationToken token = default)
    {
        using CombinedTokenSources tokens = token.CombineTokensIfNeeded(UCWarfare.UnloadCancel);

        await using IKitsDbContext dbContext = new WarfareDbContext();

        ulong steam64 = player.Steam64;

        List<KitFavorite> list = await dbContext.KitFavorites.Where(x => x.Steam64 == steam64).ToListAsync(token);
        dbContext.KitFavorites.RemoveRange(list);
        dbContext.KitFavorites.AddRange(favoriteKits.Select(x => new KitFavorite
        {
            Steam64 = steam64,
            KitId = x
        }));

        await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        
        player.KitMenuData.FavoritesDirty = false;
    }
    public async Task ResetLayout(UCPlayer player, uint kit, bool lockPurchaseSync, CancellationToken token = default)
    {
        await using IKitsDbContext dbContext = new WarfareDbContext();

        if (lockPurchaseSync)
            await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ulong steam64 = player.Steam64;

            player.LayoutTransformations?.RemoveAll(x => x.Kit == kit);

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
    public async Task SaveLayout(UCPlayer player, Kit kit, bool lockPurchaseSync, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);
        List<LayoutTransformation> active = Layouts.GetLayoutTransformations(player, kit.PrimaryKey);
        List<(Page Page, Item Item, byte X, byte Y, byte Rotation, byte SizeX, byte SizeY)> inventory = new List<(Page, Item, byte, byte, byte, byte, byte)>(24);
        for (int pageIndex = 0; pageIndex < PlayerInventory.STORAGE; ++pageIndex)
        {
            SDG.Unturned.Items page = player.Player.inventory.items[pageIndex];
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
                LayoutTransformation t = active[i];
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
                    L.LogWarning("Duplicate layout transformation for item " + original + ", skipping.");
                    active.RemoveAtFast(i--);
                    continue;
                }
                if (original.X == t.NewX && original.Y == t.NewY && original.Page == t.NewPage && original.Rotation == t.NewRotation)
                {
                    L.LogWarning("Identity layout transformation for item " + original + ", skipping.");
                    active.RemoveAtFast(i--);
                    continue;
                }
                items.Add(original);
                L.LogDebug($"Found active: {t.OldPage} -> {t.NewPage}, ({t.OldX} -> {t.NewX}, {t.OldY} -> {t.NewY}) new rot: {t.NewRotation}.");
            }

            // check for missing items
            ulong steam64 = player.Steam64;
            foreach (IPageKitItem jar in kitItems.OfType<IPageKitItem>().Where(x => !items.Contains(x)))
            {
                L.LogDebug("Missing item " + jar + ", trying to fit somewhere.");
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
                else if (jar.GetItem(kit, TeamManager.GetFactionSafe(player.GetTeam()), out _, out _) is { } ia)
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
                        L.LogDebug("Found colliding item: " + item.Page + ", (" + item.X + ", " + item.Y + ").");
                        colliding = item;
                        break;
                    }
                }

                if (!colliding.HasValue || colliding.Value.X == jar.X && colliding.Value.Y == jar.Y && colliding.Value.Rotation == jar.Rotation
                    && colliding.Value.Item.GetAsset() is { } ia2 && ia2.GUID == asset.GUID)
                {
                    L.LogDebug("Found no collisions (or the collision was the same item).");
                    continue;
                }
                byte origx, origy;
                Page origPage;
                ItemTransformation t = player.ItemTransformations.FirstOrDefault(x => x.Item == colliding.Value.Item);
                if (t.Item == null)
                {
                    ItemDropTransformation t2 = player.ItemDropTransformations.FirstOrDefault(x => x.Item == colliding.Value.Item);
                    if (t.Item == null)
                    {
                        L.LogDebug("Unable to find transformations for original item blocking " + jar + ".");
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
                    L.LogDebug("Unable to find original item blocking " + jar + ".");
                    continue;
                }
                if (colliding.Value.SizeX == sizeX1 && colliding.Value.SizeY == sizeY1)
                {
                    active.Add(new LayoutTransformation(jar.Page, orig.Page, jar.X, jar.Y, orig.X, orig.Y, orig.Rotation, kit.PrimaryKey, new KitLayoutTransformation
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
                    L.LogDebug("Moved item to original position of other item: " + jar + " -> " + orig + ".");
                }
                else if (colliding.Value.SizeX == sizeY1 && colliding.Value.SizeY == sizeX1)
                {
                    active.Add(new LayoutTransformation(jar.Page, orig.Page, jar.X, jar.Y, orig.X, orig.Y, (byte)(orig.Rotation + 1 % 4), kit.PrimaryKey, new KitLayoutTransformation
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
                    L.LogDebug("Moved item to original position of other item (rotated 90 degrees): " + jar + " -> " + orig + ".");
                }
            }

            uint kitId = kit.PrimaryKey;

            await using IKitsDbContext dbContext = new WarfareDbContext();

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

        await using IKitsDbContext dbContext = new WarfareDbContext();

        uint id = kit.PrimaryKey;

        List<KitLayoutTransformation> transformations = await dbContext.KitLayoutTransformations
            .Where(x => x.KitId == id)
            .ToListAsync(token).ConfigureAwait(false);

        transformations.RemoveAll(x => !changed.Any(y => x.OldPage == y.Page && x.OldX == y.X && x.OldY == y.Y));
        dbContext.KitLayoutTransformations.RemoveRange(transformations);
        await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
    }
}