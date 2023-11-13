using Microsoft.EntityFrameworkCore;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.SQL;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players.Layouts;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits;
partial class KitManager
{
    public static async Task DownloadPlayersKitData(IEnumerable<UCPlayer> playerList, bool lockPurchaseSync,
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
            if (player.IsDownloadingKitData && !player.HasDownloadedKitData)
            {
                L.LogDebug("Spin-waiting for player kit data for " + player + "...");
                UCWarfare.SpinWaitUntil(() => player.HasDownloadedKitData, 2500);
                return;
            }
            
            player.IsDownloadingKitData = true;
        }

        List<KitHotkey>? bindingsToDelete = null;
        List<KitLayoutTransformation>? layoutsToDelete = null;


        if (lockPurchaseSync)
        {
            Task[] tasks = new Task[players.Length];
            for (int i = 0; i < players.Length; ++i)
            {
                UCPlayer pl = players[i];
                CancellationToken token2 = token;
                token2.CombineIfNeeded(pl.DisconnectToken);
                tasks[i] = pl.PurchaseSync.WaitAsync(token2);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        try
        {
            List<uint>?[] kitOutput = new List<uint>[players.Length];
            List<HotkeyBinding>?[] bindings = new List<HotkeyBinding>[players.Length];
            List<LayoutTransformation>?[] layouts = new List<LayoutTransformation>[players.Length];

            List<KitAccess> kitAccesses = await WarfareDatabases.Kits.KitAccess.Where(x => steam64Ids.Contains(x.Steam64)).ToListAsync(token).ConfigureAwait(false);
            for (int i = 0; i < kitAccesses.Count; ++i)
            {
                KitAccess access = kitAccesses[i];
                int index = Array.IndexOf(steam64Ids, access.Steam64);
                if (index != -1 && access.KitId > 0)
                {
                    (kitOutput[index] ??= new List<uint>(16)).Add(access.KitId);
                }
            }

            List<KitHotkey> kitHotkeys = await WarfareDatabases.Kits.KitHotkeys.Where(x => steam64Ids.Contains(x.Steam64)).ToListAsync(token).ConfigureAwait(false);
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
                    jar = new SpecificPageKitItem(PrimaryKey.NotAssigned, reference.Value, hotkey.X, hotkey.Y, 0, hotkey.Page, 1, Array.Empty<byte>());
                }
                else
                {
                    RedirectType? redir = hotkey.Redirect;
                    if (!redir.HasValue)
                    {
                        L.LogWarning("Failed to read redirect type and GUID from player " + hotkey.Steam64 + "'s hotkeys.");
                        del = true;
                        jar = new AssetRedirectPageKitItem(PrimaryKey.NotAssigned, hotkey.X, hotkey.Y, 0, hotkey.Page, RedirectType.None, null);
                    }
                    else
                    {
                        jar = new AssetRedirectPageKitItem(PrimaryKey.NotAssigned, hotkey.X, hotkey.Y, 0, hotkey.Page, redir.Value, null);
                    }
                }

                HotkeyBinding b = new HotkeyBinding(hotkey.KitId, hotkey.Slot, jar, hotkey);

                if (!del)
                    (bindings[index] ??= new List<HotkeyBinding>(4)).Add(b);
                else
                    (bindingsToDelete ??= new List<KitHotkey>(2)).Add(hotkey);
            }

            List<KitLayoutTransformation> kitLayouts = await WarfareDatabases.Kits.KitLayoutTransformations.Where(x => steam64Ids.Contains(x.Steam64)).ToListAsync(token).ConfigureAwait(false);
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

            await singleton.WaitAsync(token).ConfigureAwait(false);
            try
            {
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
                            Kit? kit = singleton.GetKit(l.Kit);
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
                            Kit? kit = singleton.GetKit(b.Kit);
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
                singleton.Release();
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
            WarfareDatabases.Kits.KitHotkeys.RemoveRange(bindingsToDelete);
        if (layoutsToDelete is { Count: > 0 })
            WarfareDatabases.Kits.KitLayoutTransformations.RemoveRange(layoutsToDelete);

        if (bindingsToDelete is { Count: > 0 } || layoutsToDelete is { Count: > 0 })
        {
            UCWarfare.RunTask(WarfareDatabases.Kits.SaveChangesAsync, token,
                ctx: "Delete invalid hotkeys and/or layout transformations for " + players.Length + " player(s).");
        }

        await UCWarfare.ToUpdate(token);
        Signs.UpdateKitSigns(null, null);
    }

    public static Task DownloadPlayerKitData(UCPlayer player, bool lockPurchaseSync, CancellationToken token = default) =>
        DownloadPlayersKitData(new UCPlayer[] { player }, lockPurchaseSync, token);

    private static async Task AddAccessRow(uint kit, ulong player, KitAccessType type, CancellationToken token = default)
    {
        await WarfareDatabases.Kits.KitAccess.AddAsync(new KitAccess
        {
            KitId = kit,
            Steam64 = player,
            AccessType = type,
            Timestamp = DateTimeOffset.UtcNow
        }, token).ConfigureAwait(false);
        await WarfareDatabases.Kits.SaveChangesAsync(token).ConfigureAwait(false);
    }
    /// <remarks>Thread Safe</remarks>
    public static Task<bool> HasAccess(uint kit, ulong player, CancellationToken token = default)
    {
        return kit == 0 ? Task.FromResult(false) : WarfareDatabases.Kits.KitAccess.AnyAsync(x => x.KitId == kit && x.Steam64 == player, token);
    }
    private static async Task<bool> AddAccessRow(string kit, ulong player, KitAccessType type, CancellationToken token = default)
    {
        uint pk = 0;
        if (UCWarfare.IsLoaded && GetSingletonQuick() is { } kitmanager)
        {
            Kit? kitFound = await kitmanager.FindKit(kit, token, true);
            if (kitFound is not null)
                pk = kitFound.PrimaryKey;
        }
        if (pk == 0)
        {
            Kit? kit2 = await WarfareDatabases.Kits.Kits.FirstOrDefaultAsync(x => x.InternalName == kit, token).ConfigureAwait(false);

            if (kit2 == null)
                return false;

            pk = kit2.PrimaryKey;
            if (pk == 0)
                return false;
        }

        await WarfareDatabases.Kits.KitAccess.AddAsync(new KitAccess
        {
            KitId = pk,
            Steam64 = player,
            AccessType = type,
            Timestamp = DateTimeOffset.UtcNow
        }, token).ConfigureAwait(false);

        await WarfareDatabases.Kits.SaveChangesAsync(token).ConfigureAwait(false);
        return true;
    }
    private static async Task<bool> RemoveAccessRow(uint kit, ulong player, CancellationToken token = default)
    {
        List<KitAccess> access = await WarfareDatabases.Kits.KitAccess.Where(x => x.KitId == kit && x.Steam64 == player)
            .ToListAsync(token).ConfigureAwait(false);

        if (access is not { Count: > 0 })
            return false;

        WarfareDatabases.Kits.KitAccess.RemoveRange(access);
        await WarfareDatabases.Kits.SaveChangesAsync(token).ConfigureAwait(false);
        return true;
    }
    private static async Task<bool> RemoveAccessRow(string kit, ulong player, CancellationToken token = default)
    {
        List<KitAccess> access = await WarfareDatabases.Kits.KitAccess.Where(x => x.Kit.InternalName == kit && x.Steam64 == player)
            .ToListAsync(token).ConfigureAwait(false);

        if (access is not { Count: > 0 })
            return false;

        WarfareDatabases.Kits.KitAccess.RemoveRange(access);
        await WarfareDatabases.Kits.SaveChangesAsync(token).ConfigureAwait(false);
        return true;
    }

    /// <remarks>Thread Safe</remarks>
    public async Task<bool> RemoveHotkey(uint kit, ulong player, byte slot, CancellationToken token = default)
    {
        if (!KitEx.ValidSlot(slot))
            throw new ArgumentException("Invalid slot number.", nameof(slot));
        List<KitHotkey> hotkeys = await WarfareDatabases.Kits.KitHotkeys.Where(x => x.Kit.PrimaryKey == kit && x.Steam64 == player && x.Slot == slot)
            .ToListAsync(token).ConfigureAwait(false);

        if (hotkeys is not { Count: > 0 })
            return false;

        WarfareDatabases.Kits.KitHotkeys.RemoveRange(hotkeys);
        await WarfareDatabases.Kits.SaveChangesAsync(token).ConfigureAwait(false);
        return true;
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<bool> RemoveHotkey(uint kit, ulong player, byte x, byte y, Page page, CancellationToken token = default)
    {
        List<KitHotkey> hotkeys = await WarfareDatabases.Kits.KitHotkeys.Where(h => h.Kit.PrimaryKey == kit && h.Steam64 == player && h.X == x && h.Y == y && h.Page == page)
            .ToListAsync(token).ConfigureAwait(false);

        if (hotkeys is not { Count: > 0 })
            return false;

        WarfareDatabases.Kits.KitHotkeys.RemoveRange(hotkeys);
        await WarfareDatabases.Kits.SaveChangesAsync(token).ConfigureAwait(false);
        return true;
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<KitHotkey> AddHotkey(uint kit, ulong player, byte slot, IPageKitItem item, CancellationToken token = default)
    {
        if (item is not ISpecificKitItem and not IAssetRedirectKitItem)
            throw new ArgumentException("Item must also implement IItem or IAssetRedirect.", nameof(item));
        if (!KitEx.ValidSlot(slot))
            throw new ArgumentException("Invalid slot number.", nameof(slot));

        byte x = item.X, y = item.Y;
        Page page = item.Page;
        List<KitHotkey> hotkeys = await WarfareDatabases.Kits.KitHotkeys.Where(h => h.Kit.PrimaryKey == kit && h.Steam64 == player && (h.X == x && h.Y == y && h.Page == page || h.Slot == slot))
            .ToListAsync(token).ConfigureAwait(false);

        if (hotkeys.Count > 0)
            WarfareDatabases.Kits.KitHotkeys.RemoveRange(hotkeys);

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
        WarfareDatabases.Kits.KitHotkeys.Add(kitHotkey);

        await WarfareDatabases.Kits.SaveChangesAsync(token).ConfigureAwait(false);
        return kitHotkey;
    }
    internal async Task SaveFavorites(UCPlayer player, IReadOnlyList<uint> favoriteKits, CancellationToken token = default)
    {
        token.CombineIfNeeded(UCWarfare.UnloadCancel);
        object[] args = new object[favoriteKits.Count + 1];
        args[0] = player.Steam64;

        ulong steam64 = player.Steam64;

        List<KitFavorite> list = await WarfareDatabases.Kits.KitFavorites.Where(x => x.Steam64 == steam64).ToListAsync(token);
        WarfareDatabases.Kits.KitFavorites.RemoveRange(list);
        WarfareDatabases.Kits.KitFavorites.AddRange(favoriteKits.Select(x => new KitFavorite
        {
            Steam64 = steam64,
            KitId = x
        }));

        await WarfareDatabases.Kits.SaveChangesAsync(token).ConfigureAwait(false);
        
        player.KitMenuData.FavoritesDirty = false;
    }
    public async Task ResetLayout(UCPlayer player, uint kit, bool lockPurchaseSync, CancellationToken token = default)
    {
        if (lockPurchaseSync)
            await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ulong steam64 = player.Steam64;
            player.LayoutTransformations?.RemoveAll(x => x.Kit == kit);
            List<KitLayoutTransformation> list = await WarfareDatabases.Kits.KitLayoutTransformations.Where(x => x.Steam64 == steam64 && x.KitId == kit).ToListAsync(token);
            WarfareDatabases.Kits.KitLayoutTransformations.RemoveRange(list);

            await WarfareDatabases.Kits.SaveChangesAsync(token).ConfigureAwait(false);
        }
        finally
        {
            if (lockPurchaseSync)
                player.PurchaseSync.Release();
        }
    }
    public async Task SaveLayout(UCPlayer player, Kit kit, bool lockPurchaseSync, bool lockKit, CancellationToken token = default)
    {
        await UCWarfare.ToUpdate(token);
        List<LayoutTransformation> active = GetLayoutTransformations(player, kit.PrimaryKey);
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
            if (lockKit)
                await WaitAsync(token).ConfigureAwait(false);
            try
            {
                IKitItem[] kitItems = kit.Items;
                for (int i = 0; i < active.Count; ++i)
                {
                    LayoutTransformation t = active[i];
                    IPageKitItem? original = (IPageKitItem?)kitItems.FirstOrDefault(x => x is IPageKitItem jar && jar.X == t.OldX && jar.Y == t.OldY && jar.Page == t.OldPage);
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
                        if (UCInventoryManager.IsOverlapping(jar.X, jar.Y, sizeX1, sizeY1, item.X, item.Y, item.SizeX, item.SizeY, jar.Rotation, item.Rotation))
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

                List<KitLayoutTransformation> existing = await WarfareDatabases.Kits.KitLayoutTransformations
                    .Where(x => x.Steam64 == steam64 && x.KitId == kitId).ToListAsync(token).ConfigureAwait(false);

                WarfareDatabases.Kits.KitLayoutTransformations.RemoveRange(existing);

                if (active.Count > 0)
                    await WarfareDatabases.Kits.KitLayoutTransformations.AddRangeAsync(active.Select(x => x.Model!), token);

                await WarfareDatabases.Kits.SaveChangesAsync(token).ConfigureAwait(false);
            }
            finally
            {
                if (lockKit)
                    Release();
            }
        }
        finally
        {
            if (lockPurchaseSync)
                player.PurchaseSync.Release();
        }
    }

    internal static async Task OnItemsChangedLayoutHandler(IKitItem[] oldItems, Kit kit, CancellationToken token = default)
    {
        await UCWarfare.ToUpdate(token);
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

        uint id = kit.PrimaryKey;
        List<KitLayoutTransformation> transformations =
            await WarfareDatabases.Kits.KitLayoutTransformations.Where(x => x.KitId == id).ToListAsync(token).ConfigureAwait(false);
        transformations.RemoveAll(x => !changed.Any(y => x.OldPage == y.Page && x.OldX == y.X && x.OldY == y.Y));
        WarfareDatabases.Kits.KitLayoutTransformations.RemoveRange(transformations);
        await WarfareDatabases.Kits.SaveChangesAsync(token);
    }
}
