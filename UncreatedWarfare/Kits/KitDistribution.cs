using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Encoding;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Layouts;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits;

public class KitDistribution(KitManager manager)
{
    public KitManager Manager { get; } = manager;

    /// <remarks>Thread Safe</remarks>
    public async Task DequipKit(Kit kit, bool manual, CancellationToken token = default)
    {
        Kit? t1def = null;
        Kit? t2def = null;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            uint? activeKit = pl.ActiveKit;
            if (!activeKit.HasValue || activeKit.Value != kit.PrimaryKey)
                continue;

            ulong team = pl.GetTeam();
            if (team == 1 && (t1def ??= await Manager.GetDefaultKit(1ul, token, x => KitManager.RequestableSet(x, false))) != null)
                await Manager.Requests.GiveKit(pl, t1def == kit ? null : t1def, manual, false, token);
            else if (team == 2 && (t2def ??= await Manager.GetDefaultKit(2ul, token)) != null)
                await Manager.Requests.GiveKit(pl, t2def == kit ? null : t2def, manual, false, token);
            else
                await Manager.Requests.GiveKit(pl, null, manual, false, token);
        }
    }
    /// <remarks>Thread Safe</remarks>
    public async Task DequipKit(UCPlayer player, bool manual, CancellationToken token = default)
    {
        ulong team = player.GetTeam();
        Kit? dkit = await Manager.GetDefaultKit(team, token, x => KitManager.RequestableSet(x, false));
        if (dkit != null)
            await Manager.Requests.GiveKit(player, dkit, manual, true, token);
        else
            await Manager.Requests.GiveKit(player, null, manual, false, token);
    }
    /// <remarks>Thread Safe</remarks>
    public Task DequipKit(UCPlayer player, bool manual, Kit kit, CancellationToken token = default)
    {
        uint? activeKit = player.ActiveKit;
        if (activeKit is not null && activeKit == kit.PrimaryKey)
        {
            return DequipKit(player, manual, token);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Add the items to a player's inventory.
    /// </summary>
    public void DistributeKitItems(UCPlayer player, Kit? kit, bool clearInventory = true, bool sendActionTip = true, bool ignoreAmmobags = false)
    {
        ThreadUtil.assertIsGameThread();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (clearInventory)
            UCInventoryManager.ClearInventory(player, !Data.UseFastKits);

        player.ItemTransformations.Clear();
        player.ItemDropTransformations.Clear();
        player.Player.equipment.dequip();
        if (kit == null)
        {
            UCInventoryManager.UpdateSlots(player);
            return;
        }

        LayoutTransformation[] layout = player.LayoutTransformations == null || !player.HasDownloadedKitData || kit.PrimaryKey == 0
            ? Array.Empty<LayoutTransformation>()
            : player.LayoutTransformations.Where(x => x.Kit == kit.PrimaryKey).ToArray();

        FactionInfo? faction = player.Faction;

        IKitItem[] items = kit.Items;

        if (Data.UseFastKits)
        {
            NetId id = player.Player.clothing.GetNetId();
            int flag = 0;
            bool hasPlayedEffect = false;
            for (int i = 0; i < items.Length; ++i)
            {
                IKitItem item = items[i];
                if (item is not IClothingKitItem clothingJar)
                    continue;

                ItemAsset? asset = item.GetItem(kit, faction, out _, out byte[] state);
                if (asset == null || asset.type != clothingJar.Type.GetItemType())
                {
                    ReportItemError(kit, item, asset);
                    continue;
                }

                if ((flag & (1 << (int)clothingJar.Type)) == 0) // to prevent duplicates
                {
                    flag |= (byte)(1 << (int)clothingJar.Type);
                    ClientInstanceMethod<Guid, byte, byte[], bool>? inv =
                        clothingJar.Type switch
                        {
                            ClothingType.Shirt => Data.SendWearShirt,
                            ClothingType.Pants => Data.SendWearPants,
                            ClothingType.Hat => Data.SendWearHat,
                            ClothingType.Backpack => Data.SendWearBackpack,
                            ClothingType.Vest => Data.SendWearVest,
                            ClothingType.Mask => Data.SendWearMask,
                            ClothingType.Glasses => Data.SendWearGlasses,
                            _ => null
                        };

                    if (inv == null)
                        continue;

                    inv.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), asset.GUID, 100, state, !hasPlayedEffect);
                    hasPlayedEffect = true;
                }
                else
                {
                    L.LogWarning("Duplicate " + clothingJar.Type + " defined for " + kit.InternalName + ", " + item + ".");
                }
            }

            byte[] blank = Array.Empty<byte>();
            for (int i = 0; i < 7; ++i)
            {
                if (((flag >> i) & 1) == 1)
                    continue;

                ((ClothingType)i switch
                {
                    ClothingType.Shirt => Data.SendWearShirt,
                    ClothingType.Pants => Data.SendWearPants,
                    ClothingType.Hat => Data.SendWearHat,
                    ClothingType.Backpack => Data.SendWearBackpack,
                    ClothingType.Vest => Data.SendWearVest,
                    ClothingType.Mask => Data.SendWearMask,
                    ClothingType.Glasses => Data.SendWearGlasses,
                    _ => null
                })?.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
            }

            SDG.Unturned.Items[] p = player.Player.inventory.items;

            bool ohi = Data.GetOwnerHasInventory(player.Player.inventory);
            if (ohi)
                Data.SetOwnerHasInventory(player.Player.inventory, false);

            List<(Item, IPageKitItem)>? toAddLater = null;
            for (int i = 0; i < items.Length; ++i)
            {
                IKitItem item = items[i];

                if (item is not IPageKitItem jar)
                    continue;

                ItemAsset? asset = item.GetItem(kit, faction, out byte amt, out byte[] state);

                // Dootpressor
                if (item is not IAssetRedirectKitItem
                    && asset is ItemGunAsset
                    && !UCWarfare.Config.DisableAprilFools
                    && HolidayUtil.isHolidayActive(ENPCHoliday.APRIL_FOOLS)
                    && Gamemode.Config.ItemAprilFoolsBarrel.ValidReference(out ItemBarrelAsset barrel))
                {
                    unsafe
                    {
                        fixed (byte* ptr = state)
                        {
                            UnsafeBitConverter.GetBytes(ptr, barrel.id, (int)AttachmentType.Barrel);
                            ptr[(int)AttachmentType.Barrel / 2 + 13] = 100;
                        }
                    }
                }

                // ignore ammo bag if enabled
                if (asset != null && ignoreAmmobags && Gamemode.Config.BarricadeAmmoBag.MatchGuid(asset.GUID))
                {
                    L.LogDebug("[GIVE KIT] Skipping ammo bag: " + jar + ".");
                    continue;
                }

                bool layoutAffected = false;
                byte giveX = jar.X;
                byte giveY = jar.Y;
                byte giveRot = jar.Rotation;
                Page givePage = jar.Page;

                // find layout override
                for (int j = 0; j < layout.Length; ++j)
                {
                    ref LayoutTransformation l = ref layout[j];
                    if (l.OldPage != givePage || l.OldX != giveX || l.OldY != giveY)
                        continue;

                    layoutAffected = true;
                    givePage = l.NewPage;
                    giveX = l.NewX;
                    giveY = l.NewY;
                    giveRot = l.NewRotation;
                    L.LogDebug("[GIVE KIT] Found layout for item " + item + " (to: " + givePage + ", (" + giveX + ", " + giveY + ") rot: " + giveRot + ".)");
                    break;
                }

                // checks for overlapping items and retries overlapping layout-affected items
                retry:
                if ((int)givePage < PlayerInventory.PAGES - 2 && asset != null)
                {
                    SDG.Unturned.Items page = p[(int)givePage];
                    Item itm = new Item(asset.id, amt, 100, state);
                    // ensures multiple items are not put in the slots (causing the ghost gun issue)
                    if (givePage is Page.Primary or Page.Secondary)
                    {
                        if (page.getItemCount() > 0)
                        {
                            L.LogWarning("[GIVE KIT] Duplicate " + givePage.ToString().ToLowerInvariant() + " defined for " + kit.InternalName + ", " + item + ".");
                            L.Log("[GIVE KIT] Removing " + (page.items[0].GetAsset().itemName) + " in place of duplicate.");
                            (toAddLater ??= new List<(Item, IPageKitItem)>(2)).Add((page.items[0].item, jar));
                            page.removeItem(0);
                        }

                        giveX = 0;
                        giveY = 0;
                        giveRot = 0;
                    }
                    else if (UCInventoryManager.IsOutOfBounds(page, giveX, giveY, asset.size_x, asset.size_y, giveRot))
                    {
                        // if an item is out of range of it's container with a layout override, remove it and try again
                        if (layoutAffected)
                        {
                            L.LogDebug("[GIVE KIT] Out of bounds layout item in " + givePage + " defined for " + kit.InternalName + ", " + item + ".");
                            L.LogDebug("[GIVE KIT] Retrying at original position.");
                            layoutAffected = false;
                            giveX = jar.X;
                            giveY = jar.Y;
                            giveRot = jar.Rotation;
                            givePage = jar.Page;
                            goto retry;
                        }
                        L.LogWarning("[GIVE KIT] Out of bounds item in " + givePage + " defined for " + kit.InternalName + ", " + item + ".");
                        (toAddLater ??= new List<(Item, IPageKitItem)>(2)).Add((itm, jar));
                    }

                    int ic2 = page.getItemCount();
                    for (int j = 0; j < ic2; ++j)
                    {
                        ItemJar? jar2 = page.getItem((byte)j);
                        if (jar2 != null && UCInventoryManager.IsOverlapping(giveX, giveY, asset.size_x, asset.size_y, jar2.x, jar2.y, jar2.size_x, jar2.size_y, giveRot, jar2.rot))
                        {
                            // if an overlap is detected with a layout override, remove it and try again
                            if (layoutAffected)
                            {
                                L.LogDebug("[GIVE KIT] Overlapping layout item in " + givePage + " defined for " + kit.InternalName + ", " + item + ".");
                                L.LogDebug("[GIVE KIT] Retrying at original position.");
                                layoutAffected = false;
                                giveX = jar.X;
                                giveY = jar.Y;
                                giveRot = jar.Rotation;
                                givePage = jar.Page;
                                goto retry;
                            }
                            L.LogWarning("[GIVE KIT] Overlapping item in " + givePage + " defined for " + kit.InternalName + ", " + item + ".");
                            L.Log("[GIVE KIT] Removing " + (jar2.GetAsset().itemName) + " (" + jar2.x + ", " + jar2.y + " @ " + jar2.rot + "), in place of duplicate.");
                            page.removeItem((byte)j--);
                            (toAddLater ??= new List<(Item, IPageKitItem)>(2)).Add((jar2.item, jar));
                        }
                    }

                    if (layoutAffected)
                    {
                        player.ItemTransformations.Add(new ItemTransformation(jar.Page, givePage, jar.X, jar.Y, giveX, giveY, itm));
                    }
                    page.addItem(giveX, giveY, giveRot, itm);
                }
                // if a clothing item asset redirect is missing it's likely a kit being requested on a faction without those clothes.
                else if (item is not (IAssetRedirectKitItem and IClothingKitItem))
                    ReportItemError(kit, item, asset);
            }

            // try to add removed items later
            if (toAddLater is { Count: > 0 })
            {
                for (int i = 0; i < toAddLater.Count; ++i)
                {
                    (Item item, IPageKitItem jar) = toAddLater[i];
                    if (!player.Player.inventory.tryAddItemAuto(item, false, false, false, !hasPlayedEffect))
                    {
                        ItemManager.dropItem(item, player.Position, !hasPlayedEffect, true, false);
                        player.ItemDropTransformations.Add(new ItemDropTransformation(jar.Page, jar.X, jar.Y, item));
                    }
                    else
                    {
                        for (int pageIndex = 0; pageIndex < PlayerInventory.STORAGE; ++pageIndex)
                        {
                            SDG.Unturned.Items page = player.Player.inventory.items[pageIndex];
                            int c = page.getItemCount();
                            for (int index = 0; index < c; ++index)
                            {
                                ItemJar jar2 = page.getItem((byte)index);
                                if (jar2.item != item)
                                    continue;

                                player.ItemTransformations.Add(new ItemTransformation(jar.Page, (Page)pageIndex, jar.X, jar.Y, jar2.x, jar2.y, item));
                                goto exit;
                            }
                        }
                    }
                    exit:

                    if (!hasPlayedEffect)
                        hasPlayedEffect = true;
                }
            }
            if (ohi)
                Data.SetOwnerHasInventory(player.Player.inventory, true);
            UCInventoryManager.SendPages(player);
        }
        else
        {
            foreach (IKitItem item in items)
            {
                if (item is IClothingKitItem clothing)
                {
                    ItemAsset? asset = item.GetItem(kit, faction, out byte amt, out byte[] state);
                    if (asset is null)
                    {
                        ReportItemError(kit, item, null);
                        return;
                    }
                    if (clothing.Type == ClothingType.Shirt)
                    {
                        if (asset is ItemShirtAsset shirt)
                            player.Player.clothing.askWearShirt(shirt, 100, state, true);
                        else goto e;
                    }
                    else if (clothing.Type == ClothingType.Pants)
                    {
                        if (asset is ItemPantsAsset pants)
                            player.Player.clothing.askWearPants(pants, 100, state, true);
                        else goto e;
                    }
                    else if (clothing.Type == ClothingType.Vest)
                    {
                        if (asset is ItemVestAsset vest)
                            player.Player.clothing.askWearVest(vest, 100, state, true);
                        else goto e;
                    }
                    else if (clothing.Type == ClothingType.Hat)
                    {
                        if (asset is ItemHatAsset hat)
                            player.Player.clothing.askWearHat(hat, 100, state, true);
                        else goto e;
                    }
                    else if (clothing.Type == ClothingType.Mask)
                    {
                        if (asset is ItemMaskAsset mask)
                            player.Player.clothing.askWearMask(mask, 100, state, true);
                        else goto e;
                    }
                    else if (clothing.Type == ClothingType.Backpack)
                    {
                        if (asset is ItemBackpackAsset backpack)
                            player.Player.clothing.askWearBackpack(backpack, 100, state, true);
                        else goto e;
                    }
                    else if (clothing.Type == ClothingType.Glasses)
                    {
                        if (asset is ItemGlassesAsset glasses)
                            player.Player.clothing.askWearGlasses(glasses, 100, state, true);
                        else goto e;
                    }
                    else
                        goto e;

                    continue;
                    e:
                    ReportItemError(kit, item, asset);
                    Item uitem = new Item(asset.id, amt, 100, state);
                    if (!player.Player.inventory.tryAddItem(uitem, true))
                    {
                        ItemManager.dropItem(uitem, player.Position, false, true, true);
                    }
                }
            }

            foreach (IKitItem item in items)
            {
                if (item is IClothingKitItem)
                    continue;

                ItemAsset? asset = item.GetItem(kit, faction, out byte amt, out byte[] state);
                if (asset is null)
                {
                    ReportItemError(kit, item, null);
                    return;
                }

                Item uitem = new Item(asset.id, amt, 100, state);
                if ((item is not IPageKitItem jar || !player.Player.inventory.tryAddItem(uitem, jar.X, jar.Y, (byte)jar.Page, jar.Rotation))
                    && !player.Player.inventory.tryAddItem(uitem, true))
                {
                    ItemManager.dropItem(uitem, player.Position, false, true, true);
                }
            }
        }

        UCInventoryManager.UpdateSlots(player);

        // send action menu tip
        if (kit.Class != Class.Unarmed && sendActionTip)
        {
            if (player.IsSquadLeader())
                Tips.TryGiveTip(player, 1200, T.TipActionMenuSl);
            else
                Tips.TryGiveTip(player, 3600, T.TipActionMenu);
        }

        // equip primary or secondary
        if (player.Player.inventory.getItemCount((byte)Page.Primary) > 0)
            player.Player.equipment.ServerEquip((byte)Page.Primary, 0, 0);
        else if (player.Player.inventory.getItemCount((byte)Page.Secondary) > 0)
            player.Player.equipment.ServerEquip((byte)Page.Secondary, 0, 0);
    }

    private static void ReportItemError(Kit kit, IKitItem item, ItemAsset? asset)
    {
        if (asset == null)
        {
            L.LogWarning("Unknown item in kit \"" + kit.InternalName + "\": {" +
                         item switch
                         {
                             ISpecificKitItem i2 => i2.Item.ToString(),
                             _ => item.ToString()
                         } + "}.", method: "GIVE KIT");
        }
        else if (item is IClothingKitItem clothing)
        {
            L.LogWarning("Invalid " + clothing.Type.ToString().ToLowerInvariant() +
                         " in kit \"" + kit.InternalName + "\" for item " + asset.itemName +
                         " {" + asset.GUID.ToString("N") + "}.", method: "GIVE KIT");
        }
        else
        {
            L.LogWarning("Invalid item" +
                         " in kit \"" + kit.InternalName + "\" for item " + asset.itemName +
                         " {" + asset.GUID.ToString("N") + "}.", method: "GIVE KIT");
        }
    }
}
