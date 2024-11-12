using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.ItemTracking;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits;

public class KitDistribution(KitManager manager, IServiceProvider serviceProvider)
{
    private readonly IPlayerService _playerService = serviceProvider.GetRequiredService<IPlayerService>();
    private readonly TipService _tipService = serviceProvider.GetRequiredService<TipService>();
    private readonly TipTranslations _tipTranslations = serviceProvider.GetRequiredService<TranslationInjection<TipTranslations>>().Value;
    private readonly AssetRedirectService _assetRedirectService = serviceProvider.GetRequiredService<AssetRedirectService>();
    private readonly IFactionDataStore _factionDataStore = serviceProvider.GetRequiredService<IFactionDataStore>();
    public KitManager Manager { get; } = manager;

    /// <summary>
    /// Dequip the given kit from all players and give them their default kits instead.
    /// </summary>
    /// <remarks>Thread Safe</remarks>
    public async Task DequipKit(Kit kit, bool manual, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        List<KeyValuePair<Team, Kit?>> defaultCache = new List<KeyValuePair<Team, Kit?>>(2);

        List<Task> dequipTasks = new List<Task>();
        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            uint? activeKit = player.Component<KitPlayerComponent>().ActiveKitKey;
            if (!activeKit.HasValue || activeKit.Value != kit.PrimaryKey)
                continue;

            dequipTasks.Add(Task.Run(async () =>
            {
                Kit? defaultKit = null;

                Team team = player.Team;

                if (team.IsValid)
                {
                    KeyValuePair<Team, Kit?> cacheEntry = defaultCache.Find(x => x.Key == team);
                    defaultKit = cacheEntry.Value;
                    if (cacheEntry.Key == null)
                    {
                        defaultKit = await Manager.GetDefaultKit(team, token, x => KitManager.RequestableSet(x, false));
                        defaultCache.Add(new KeyValuePair<Team, Kit?>(team, defaultKit));
                    }
                }

                await Manager.Requests.GiveKit(player, defaultKit == kit ? null : defaultKit, manual, false, token);
            }, token));
        }

        await Task.WhenAll(dequipTasks);
    }

    /// <summary>
    /// Dequip <paramref name="player"/>'s kit and give them their default kit instead.
    /// </summary>
    /// <remarks>Thread Safe</remarks>
    public async Task DequipKit(WarfarePlayer player, bool manual, CancellationToken token = default)
    {
        Kit? defaultKit = await Manager.GetDefaultKit(player.Team, token, x => KitManager.RequestableSet(x, false));
        await Manager.Requests.GiveKit(player, defaultKit, manual, false, token);
    }

    /// <summary>
    /// Dequip <paramref name="player"/>'s kit if it's <paramref name="kit"/> and give them their default kit instead.
    /// </summary>
    /// <remarks>Thread Safe</remarks>
    public Task DequipKit(WarfarePlayer player, bool manual, Kit kit, CancellationToken token = default)
    {
        uint? activeKit = player.Component<KitPlayerComponent>().ActiveKitKey;
        if (activeKit is not null && activeKit == kit.PrimaryKey)
        {
            return DequipKit(player, manual, token);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Add the items to a player's inventory.
    /// </summary>
    public void DistributeKitItems(WarfarePlayer player, Kit? kit, ILogger logger, bool clearInventory = true, bool sendActionTip = true, bool ignoreAmmobags = false)
    {
        GameThread.AssertCurrent();
        if (clearInventory)
            ItemUtility.ClearInventory(player, !ItemUtility.SupportsFastKits);

        ItemTrackingPlayerComponent itemTracker = player.Component<ItemTrackingPlayerComponent>();
        itemTracker.Reset();

        player.UnturnedPlayer.equipment.dequip();
        if (kit == null)
        {
            ItemUtility.UpdateSlots(player);
            return;
        }

        PlayerInventory inventory = player.UnturnedPlayer.inventory;

        ItemLayoutTransformationData[] layout = itemTracker.LayoutTransformations == null || kit.PrimaryKey == 0
            ? Array.Empty<ItemLayoutTransformationData>()
            : itemTracker.LayoutTransformations.Where(x => x.Kit == kit.PrimaryKey).ToArray();

        Team team = player.Team;

        IKitItem[] items = kit.Items;

        if (ItemUtility.SupportsFastKits)
        {
            int flag = 0;
            bool hasPlayedEffect = false;
            for (int i = 0; i < items.Length; ++i)
            {
                IKitItem item = items[i];
                if (item is not IClothingKitItem clothingJar)
                    continue;

                ItemAsset? asset = item.GetItem(kit, team, out _, out byte[] state, _assetRedirectService, _factionDataStore);
                if (asset == null || asset.type != clothingJar.Type.GetItemType())
                {
                    ReportItemError(kit, item, logger, asset);
                    continue;
                }

                if ((flag & (1 << (int)clothingJar.Type)) == 0) // to prevent duplicates
                {
                    flag |= (byte)(1 << (int)clothingJar.Type);
                    ItemUtility.SendWearClothing(player.UnturnedPlayer, asset, clothingJar.Type, 100, state, !hasPlayedEffect);
                    hasPlayedEffect = true;
                }
                else
                {
                    logger.LogWarning("Duplicate {0} defined for {1}, {2}.", clothingJar.Type, kit.InternalName, item);
                }
            }

            byte[] blank = Array.Empty<byte>();
            for (int i = 0; i < 7; ++i)
            {
                if (((flag >> i) & 1) == 1)
                    continue;

                ItemUtility.SendWearClothing(player.UnturnedPlayer, null, (ClothingType)i, 100, blank, false);
            }

            SDG.Unturned.Items[] p = inventory.items;

            ItemUtility.IsolateInventory(player.UnturnedPlayer, out bool oldValue);
            try
            {
                List<(Item, IPageKitItem)>? toAddLater = null;
                for (int i = 0; i < items.Length; ++i)
                {
                    IKitItem item = items[i];

                    if (item is not IPageKitItem jar)
                        continue;

                    ItemAsset? asset = item.GetItem(kit, team, out byte amt, out byte[] state, _assetRedirectService, _factionDataStore);

                    // todo // Dootpressor
                    // if (item is not IAssetRedirectKitItem
                    //     && asset is ItemGunAsset
                    //     && !UCWarfare.Config.DisableAprilFools
                    //     && HolidayUtil.isHolidayActive(ENPCHoliday.APRIL_FOOLS)
                    //     && Gamemode.Config.ItemAprilFoolsBarrel.TryGetAsset(out ItemBarrelAsset? barrel))
                    // {
                    //     BitConverter.TryWriteBytes(state.AsSpan((int)AttachmentType.Barrel), barrel.id);
                    //     state[(int)AttachmentType.Barrel / 2 + 13] = 100;
                    // }

                    // todo ignore ammo bag if enabled
                    // if (asset != null && ignoreAmmobags && Gamemode.Config.BarricadeAmmoBag.MatchGuid(asset.GUID))
                    // {
                    //     L.LogDebug("[GIVE KIT] Skipping ammo bag: " + jar + ".");
                    //     continue;
                    // }

                    bool layoutAffected = false;
                    byte giveX = jar.X;
                    byte giveY = jar.Y;
                    byte giveRot = jar.Rotation;
                    Page givePage = jar.Page;

                    // find layout override
                    for (int j = 0; j < layout.Length; ++j)
                    {
                        ref ItemLayoutTransformationData l = ref layout[j];
                        if (l.OldPage != givePage || l.OldX != giveX || l.OldY != giveY)
                            continue;

                        layoutAffected = true;
                        givePage = l.NewPage;
                        giveX = l.NewX;
                        giveY = l.NewY;
                        giveRot = l.NewRotation;
                        logger.LogDebug("[GIVE KIT] Found layout for item {0} (to: {1}, ({2}, {3}) rot: {4}.)", item, givePage, giveX, giveY, giveRot);
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
                                logger.LogWarning("[GIVE KIT] Duplicate {0} defined for {1}, {2}.", givePage.ToString().ToLowerInvariant(), kit.InternalName, item);
                                logger.LogInformation("[GIVE KIT] Removing {0} in place of duplicate.", (page.items[0].GetAsset().itemName));
                                (toAddLater ??= new List<(Item, IPageKitItem)>(2)).Add((page.items[0].item, jar));
                                page.removeItem(0);
                            }

                            giveX = 0;
                            giveY = 0;
                            giveRot = 0;
                        }
                        else if (ItemUtility.IsOutOfBounds(page, giveX, giveY, asset.size_x, asset.size_y, giveRot))
                        {
                            // if an item is out of range of it's container with a layout override, remove it and try again
                            if (layoutAffected)
                            {
                                logger.LogDebug("[GIVE KIT] Out of bounds layout item in {0} defined for {1}, {2}.", givePage, kit.InternalName, item);
                                logger.LogDebug("[GIVE KIT] Retrying at original position.");
                                layoutAffected = false;
                                giveX = jar.X;
                                giveY = jar.Y;
                                giveRot = jar.Rotation;
                                givePage = jar.Page;
                                goto retry;
                            }
                            logger.LogWarning("[GIVE KIT] Out of bounds item in {0} defined for {1}, {2}.", givePage, kit.InternalName, item);
                            (toAddLater ??= new List<(Item, IPageKitItem)>(2)).Add((itm, jar));
                        }

                        int ic2 = page.getItemCount();
                        for (int j = 0; j < ic2; ++j)
                        {
                            ItemJar? jar2 = page.getItem((byte)j);
                            if (jar2 != null && ItemUtility.IsOverlapping(giveX, giveY, asset.size_x, asset.size_y, jar2.x, jar2.y, jar2.size_x, jar2.size_y, giveRot, jar2.rot))
                            {
                                // if an overlap is detected with a layout override, remove it and try again
                                if (layoutAffected)
                                {
                                    logger.LogDebug("[GIVE KIT] Overlapping layout item in {0} defined for {1}, {2}.", givePage, kit.InternalName, item);
                                    logger.LogDebug("[GIVE KIT] Retrying at original position.");
                                    layoutAffected = false;
                                    giveX = jar.X;
                                    giveY = jar.Y;
                                    giveRot = jar.Rotation;
                                    givePage = jar.Page;
                                    goto retry;
                                }
                                logger.LogWarning("[GIVE KIT] Overlapping item in {0} defined for {1}, {2}.", givePage, kit.InternalName, item);
                                logger.LogInformation("[GIVE KIT] Removing {0} ({1}, {2} @ {3}), in place of duplicate.", jar2.GetAsset().itemName, jar2.x, jar2.y, jar2.rot);
                                page.removeItem((byte)j--);
                                (toAddLater ??= new List<(Item, IPageKitItem)>(2)).Add((jar2.item, jar));
                            }
                        }

                        if (layoutAffected)
                        {
                            itemTracker.ItemTransformations.Add(new ItemTransformation(jar.Page, givePage, jar.X, jar.Y, giveX, giveY, itm));
                        }
                        page.addItem(giveX, giveY, giveRot, itm);
                    }
                    // if a clothing item asset redirect is missing it's likely a kit being requested on a faction without those clothes.
                    else if (item is not (IAssetRedirectKitItem and IClothingKitItem))
                        ReportItemError(kit, item, logger, asset);
                }

                // try to add removed items later
                if (toAddLater is { Count: > 0 })
                {
                    for (int i = 0; i < toAddLater.Count; ++i)
                    {
                        (Item item, IPageKitItem jar) = toAddLater[i];
                        if (!inventory.tryAddItemAuto(item, false, false, false, !hasPlayedEffect))
                        {
                            ItemManager.dropItem(item, player.Position, !hasPlayedEffect, true, false);
                            itemTracker.ItemDropTransformations.Add(new ItemDropTransformation(jar.Page, jar.X, jar.Y, item));
                        }
                        else
                        {
                            for (int pageIndex = 0; pageIndex < PlayerInventory.STORAGE; ++pageIndex)
                            {
                                SDG.Unturned.Items page = inventory.items[pageIndex];
                                int c = page.getItemCount();
                                for (int index = 0; index < c; ++index)
                                {
                                    ItemJar jar2 = page.getItem((byte)index);
                                    if (jar2.item != item)
                                        continue;

                                    itemTracker.ItemTransformations.Add(new ItemTransformation(jar.Page, (Page)pageIndex, jar.X, jar.Y, jar2.x, jar2.y, item));
                                    goto exit;
                                }
                            }
                        }
                        exit:

                        if (!hasPlayedEffect)
                            hasPlayedEffect = true;
                    }
                }
            }
            finally
            {
                ItemUtility.UndoIsolateInventory(player.UnturnedPlayer, oldValue);
            }

            ItemUtility.SendPages(player);
        }
        else
        {
            foreach (IKitItem item in items)
            {
                if (item is not IClothingKitItem clothing)
                    continue;

                ItemAsset? asset = item.GetItem(kit, team, out byte amt, out byte[] state, _assetRedirectService, _factionDataStore);
                if (asset == null)
                {
                    ReportItemError(kit, item, logger, null);
                    return;
                }

                switch (clothing.Type)
                {
                    case ClothingType.Shirt when asset is ItemShirtAsset shirt:
                        player.UnturnedPlayer.clothing.askWearShirt(shirt, 100, state, true);
                        continue;

                    case ClothingType.Pants when asset is ItemPantsAsset pants:
                        player.UnturnedPlayer.clothing.askWearPants(pants, 100, state, true);
                        continue;

                    case ClothingType.Vest when asset is ItemVestAsset vest:
                        player.UnturnedPlayer.clothing.askWearVest(vest, 100, state, true);
                        continue;

                    case ClothingType.Hat when asset is ItemHatAsset hat:
                        player.UnturnedPlayer.clothing.askWearHat(hat, 100, state, true);
                        continue;

                    case ClothingType.Mask when asset is ItemMaskAsset mask:
                        player.UnturnedPlayer.clothing.askWearMask(mask, 100, state, true);
                        continue;

                    case ClothingType.Backpack when asset is ItemBackpackAsset backpack:
                        player.UnturnedPlayer.clothing.askWearBackpack(backpack, 100, state, true);
                        continue;

                    case ClothingType.Glasses when asset is ItemGlassesAsset glasses:
                        player.UnturnedPlayer.clothing.askWearGlasses(glasses, 100, state, true);
                        continue;
                }

                ReportItemError(kit, item, logger, asset);
                Item uitem = new Item(asset, EItemOrigin.WORLD) { amount = amt, state = state, quality = 100 };
                if (!inventory.tryAddItem(uitem, true))
                {
                    ItemManager.dropItem(uitem, player.Position, false, true, true);
                }
            }

            foreach (IKitItem item in items)
            {
                if (item is IClothingKitItem)
                    continue;

                ItemAsset? asset = item.GetItem(kit, team, out byte amt, out byte[] state, _assetRedirectService, _factionDataStore);
                if (asset is null)
                {
                    ReportItemError(kit, item, logger, null);
                    return;
                }

                Item uitem = new Item(asset, EItemOrigin.WORLD) { amount = amt, state = state, quality = 100 };
                if ((item is not IPageKitItem jar || !inventory.tryAddItem(uitem, jar.X, jar.Y, (byte)jar.Page, jar.Rotation)) && !inventory.tryAddItem(uitem, true))
                {
                    ItemManager.dropItem(uitem, player.Position, false, true, true);
                }
            }
        }

        ItemUtility.UpdateSlots(player);

        // send action menu tip
        if (kit.Class != Class.Unarmed && sendActionTip)
        {
            if (player.IsSquadLeader())
                _tipService.TryGiveTip(player, 1200, _tipTranslations.ActionMenuSquadLeader);
            else
                _tipService.TryGiveTip(player, 3600, _tipTranslations.ActionMenu);
        }

        // equip primary or secondary
        if (inventory.getItemCount((byte)Page.Primary) > 0)
            player.UnturnedPlayer.equipment.ServerEquip((byte)Page.Primary, 0, 0);
        else if (inventory.getItemCount((byte)Page.Secondary) > 0)
            player.UnturnedPlayer.equipment.ServerEquip((byte)Page.Secondary, 0, 0);
    }

    private static void ReportItemError(Kit kit, IKitItem item, ILogger logger, ItemAsset? asset)
    {
        if (asset == null)
        {
            logger.LogWarning("[GIVE KIT] Unknown item in kit \"{0}\": {{{1}}}.", kit.InternalName, item switch
            {
                ISpecificKitItem i2 => i2.Item.ToString(),
                _ => item.ToString()
            });
        }
        else if (item is IClothingKitItem clothing)
        {
            logger.LogWarning("[GIVE KIT] Invalid {0} in kit \"{1}\" for item {2} {{{3}}}.", clothing.Type.ToString().ToLowerInvariant(), kit.InternalName, asset.itemName, asset.GUID.ToString("N"));
        }
        else
        {
            logger.LogWarning("[GIVE KIT] Invalid item" + " in kit \"{0}\" for item {1} {{{2}}}.", kit.InternalName, asset.itemName, asset.GUID.ToString("N"));
        }
    }
}
