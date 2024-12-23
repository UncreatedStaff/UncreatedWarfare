using Microsoft.Extensions.DependencyInjection;
using Org.BouncyCastle.Utilities;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util.Containers;
using static SDG.Provider.SteamGetInventoryResponse;
using static Uncreated.Warfare.FOBs.Deployment.Tweaks.ClaimToRearmTweaks;
using Item = SDG.Unturned.Item;

namespace Uncreated.Warfare.FOBs.Deployment.Tweaks;
internal class ClaimToRearmTweaks : IAsyncEventListener<ClaimBedRequested>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public ClaimToRearmTweaks(IServiceProvider serviceProvider, ILogger<ClaimToRearmTweaks> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
    }

    public async UniTask HandleEventAsync(ClaimBedRequested e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        FobManager? fobManager = serviceProvider.GetService<FobManager>();

        if (fobManager == null)
            return;

        SupplyCrate? ammoCrate = fobManager.Entities.OfType<SupplyCrate>().FirstOrDefault(s =>
            s.Type == SupplyType.Ammo &&
            !s.Buildable.IsDead &&
            s.Buildable.Equals(e.Buildable)
        ); // techdebt: is this an efficient way to do this?

        if (ammoCrate == null)
            return;

        ChatService chatService = serviceProvider.GetRequiredService<ChatService>();
        AmmoCommandTranslations translations = serviceProvider.GetRequiredService<TranslationInjection<AmmoCommandTranslations>>().Value;

        Kit? kit = null;
        if (e.Player.TryGetFromContainer(out KitPlayerComponent? kitComponenmt) && kitComponenmt.ActiveKitKey.HasValue)
        {
            kit = await serviceProvider.GetRequiredService<KitManager>().GetKit(kitComponenmt.ActiveKitKey.Value, token, KitManager.ItemsSet);
        }
        if (kit == null)
        {
            chatService.Send(e.Player, translations.AmmoNoKit);
            e.Cancel();
            return;
        }

        float rearmCost = GetRearmCost(e.Player.UnturnedPlayer.inventory, kit);
        if (rearmCost == 0)
        {
            chatService.Send(e.Player, translations.AmmoAlreadyFull);
            e.Cancel();
            return;
        }

        NearbySupplyCrates supplyCrate = NearbySupplyCrates.FromSingleCrate(ammoCrate, fobManager);
        if (rearmCost > supplyCrate.AmmoCount)
        {
            chatService.Send(e.Player, translations.AmmoOutOfStock, supplyCrate.AmmoCount, rearmCost);
            e.Cancel();
            return;
        }
        KitManager kitManager = serviceProvider.GetRequiredService<KitManager>();
        _ = kitManager.Requests.GiveKit(e.Player, kit, false, true);
        supplyCrate.SubstractSupplies(rearmCost, SupplyType.Ammo, SupplyChangeReason.ConsumeGeneral);

        chatService.Send(e.Player, translations.AmmoResuppliedKit, rearmCost, supplyCrate.AmmoCount);
        e.Cancel();
    }
    private float GetRearmCost(PlayerInventory inventory, Kit kit)
    {
        float totalRearmCost = 0;

        HashSet<Item> magazinesAlreadyCounted = new HashSet<Item>();
        foreach (ItemGunAsset gun in GetUniqueGunsInKit(kit))
        {
            int fullmags = CountFullMags(inventory, gun, magazinesAlreadyCounted);
            int requiredMags = CountMagsInKit(kit, gun);

            FirearmClass firearmClass = GetFirearmClass(gun);

            if (fullmags >= requiredMags)
            {
                _logger.LogDebug($"Weapon '{gun.FriendlyName}' ({firearmClass}) already has {fullmags}/{requiredMags} full mags, and does not need to be resupplied.");
                continue;
            }

            int magsThatNeedRefilling = requiredMags - fullmags;
            
            float magazinesRefillCost = GetMagazineCost(firearmClass) * magsThatNeedRefilling;

            _logger.LogDebug($"Weapon '{gun.FriendlyName}' ({firearmClass}) has {magsThatNeedRefilling} that need refilling ({fullmags}/{requiredMags} full mags). The resupply cost will be {magazinesRefillCost}.");

            totalRearmCost += magazinesRefillCost;
        }

        foreach (KeyValuePair<ItemAsset, int> count in GetEquipmentCountsInKit(kit))
        {
            ItemAsset equipmentAsset = count.Key;
            int requiredCount = count.Value;
            int countInInventory = CountItemsInInventory(inventory, equipmentAsset);

            if (countInInventory >= requiredCount)
            {
                _logger.LogDebug($"Equipment type '{equipmentAsset.FriendlyName}' already has {countInInventory}/{requiredCount} units, and does not need to be resupplied.");
                continue;
            }
            int numberToResupply = requiredCount - countInInventory;
            float equipmentResupplyCost = GetEquipmentCost(equipmentAsset) * numberToResupply;

            _logger.LogDebug($"Equipment type '{equipmentAsset.FriendlyName}' is missing {numberToResupply} units ({countInInventory}/{requiredCount} required). The resupply cost will be {equipmentResupplyCost}.");

            totalRearmCost += equipmentResupplyCost;
        }

        return totalRearmCost;
    }
    private List<ItemGunAsset> GetUniqueGunsInKit(Kit kit)
    {
        List<ItemGunAsset> guns = new List<ItemGunAsset>();
        foreach (var item in kit.ItemModels)
        {
            if (!item.Item.HasValue)
                continue;

            ItemAsset? asset = item.Item.Value.GetAsset<ItemAsset>();

            if (asset is not ItemGunAsset gunAsset)
                continue;

            if (guns.Contains(gunAsset)) // avoid having the same type of gun in the list, because it leads to duplicated ammo costs
                continue;

            guns.Add(gunAsset);
        }
        return guns;
    }
    private Dictionary<ItemAsset, int> GetEquipmentCountsInKit(Kit kit)
    {
        Dictionary<ItemAsset, int> equipment = new Dictionary<ItemAsset, int>();
        foreach (var item in kit.ItemModels)
        {
            if (!item.Item.HasValue)
                continue;

            ItemAsset? asset = item.Item.Value.GetAsset<ItemAsset>();
            if (asset == null)
                continue;

            if (GetEquipmentCost(asset) == 0)
                continue;

            if (!equipment.ContainsKey(asset))
                equipment[asset] = 0;

            equipment[asset]++;
        }
        return equipment;
    }
    private int CountFullMags(PlayerInventory inventory, ItemGunAsset gun, HashSet<Item> alreadyCounted)
    {
        int count = 0;
        foreach (var page in inventory.items)
        {
            if (page == null)
                continue;
            
            foreach (var itemJar in page.items)
            {
                if (itemJar == null)
                    continue;

                Item item = itemJar.item;

                if (alreadyCounted.Contains(item))
                    continue;

                ItemAsset asset = item.GetAsset();

                if (asset.GUID == gun.GUID && itemJar.item.state[10] == gun.ammoMax) // count the mag that's inside a matching gun as well
                {
                    count++;
                    alreadyCounted.Add(item);
                }
                else if (asset is ItemMagazineAsset magazine)
                {
                    if (!gun.magazineCalibers.Any(c => magazine.calibers.Contains(c)))
                        continue;

                    if (item.amount == magazine.amount)
                    {
                        count++;
                        alreadyCounted.Add(item);
                    }
                }
            }
        }
        return count;
    }
    private int CountMagsInKit(Kit kit, ItemGunAsset correspondingGun)
    {
        int count = 0;
        foreach (var item in kit.ItemModels)
        {
            if (!item.Item.HasValue)
                continue;

            ItemAsset? asset = item.Item.Value.GetAsset<ItemAsset>();
            if (asset == null)
                continue;

            if (asset.GUID == correspondingGun.GUID)
            {
                count++;
            }
            else if (asset is ItemMagazineAsset magazine && correspondingGun.magazineCalibers.Any(c => magazine.calibers.Contains(c)))
            {
                count++;
            }
            else
                continue;
        }
        return count;
    }
    private int CountItemsInInventory(PlayerInventory inventory, ItemAsset matchingItem)
    {
        int count = 0;
        foreach (var page in inventory.items)
        {
            if (page == null)
                continue;

            foreach (var itemJar in page.items)
            {
                if (itemJar == null)
                    continue;

                Item item = itemJar.item;
                ItemAsset asset = item.GetAsset();

                if (asset.GUID != matchingItem.GUID)
                    continue;

                count++;
            }
        }
        return count;
    }

    private float GetMagazineCost(FirearmClass firearmClass)
    {
        switch (firearmClass)
        {
            case FirearmClass.Pistol:
                return 0.1f;
            case FirearmClass.MachinePistol:
                return 0.15f;
            case FirearmClass.LargeMachinePistol:
                return 0.15f;
            case FirearmClass.MediumSidearm:
                return 0.15f;
            case FirearmClass.Shotgun:
                return 0.15f;
            case FirearmClass.SmallShotgun:
                return 0.1f;
            case FirearmClass.SubmachineGun:
                return 0.2f;
            case FirearmClass.Rifle:
                return 0.2f;
            case FirearmClass.BattleRifle:
                return 0.25f;
            case FirearmClass.LightMachineGun:
                return 0.5f;
            case FirearmClass.GeneralPurposeMachineGun:
                return 0.6f;
            case FirearmClass.DMR:
                return 0.3f;
            case FirearmClass.Sniper:
                return 0.3f;
            case FirearmClass.GrenadeLauncher:
                return 0.2f;
            case FirearmClass.LightAntiTank:
                return 1f;
            case FirearmClass.HeavyAntiTank:
                return 1f;
            default:
                return 0.2f;
        }
    }
    private float GetEquipmentCost(ItemAsset equipment)
    {
        switch (equipment)
        {
            case ItemThrowableAsset throwable:

                if (throwable.playerDamageMultiplier.damage > 0) // dangerous frag grenades
                    return 0.3f;
                return 0.1f; // could be a smoke grenade
            case ItemMedicalAsset medical:
                if (medical.health < 50)
                    return 0.1f;
                return 0.15f;
            case ItemTrapAsset trap:
                if (trap.vehicleDamage >= 500) // probably a powerful AT mine
                    return 0.4f;
                else if (trap.playerDamage >= 200) // probably a claymore or some anti-personnel IED
                    return 0.2f;
                return 0.15f;
            case ItemChargeAsset charge:
                if (charge.range2 < 9) // small explosive
                    return 0.2f;
                if (charge.range2 < 16) // medium explosive
                    return 0.4f;
                return 1; // beeg explosive
            case ItemBarricadeAsset barricade:
                float slotsTakeUp = barricade.size_x * barricade.size_y;

                if (slotsTakeUp < 8)
                    return 0.15f; // smol barricade

                return 0.35f; // medium or large barricade
            default:
                return 0;
        }
    }
    private FirearmClass GetFirearmClass(ItemGunAsset gun)
    {
        Asset? magazineAsset = Assets.find(EAssetType.ITEM, gun.getMagazineID());
        ItemMagazineAsset? magazine = magazineAsset is ItemMagazineAsset ? ((ItemMagazineAsset)magazineAsset) : null;

        if (magazine?.pellets > 1)
        {
            if (gun.size_x <= 3)
                return FirearmClass.SmallShotgun;
            else
                return FirearmClass.Shotgun;
        }
        else if (gun.size_x == 2)
        {
            if (gun.hasAuto)
                return FirearmClass.MachinePistol;
            else
                return FirearmClass.Pistol;
        }
        else if (gun.size_x == 3)
        {
            if (gun.hasAuto)
                return FirearmClass.LargeMachinePistol;
            else
                return FirearmClass.MediumSidearm;
        }
        else if (gun.size_x == 4)
        {
            if (gun.projectile != null && gun.vehicleDamage < 100)
                return FirearmClass.GrenadeLauncher;

            if (gun.playerDamageMultiplier.damage < 40)
                return FirearmClass.SubmachineGun;
            else if (gun.playerDamageMultiplier.damage < 60)
                return FirearmClass.Rifle;
            else
                return FirearmClass.BattleRifle;
        }
        else if (gun.size_x == 5)
        {
            if (gun.hasAuto)
            {
                if (gun.ammoMax < 45)
                    return FirearmClass.BattleRifle;
                else if (gun.playerDamageMultiplier.damage < 60)
                    return FirearmClass.LightMachineGun;
                else
                    return FirearmClass.GeneralPurposeMachineGun;
            }
            else
            {
                if (gun.projectile != null)
                {
                    if (gun.vehicleDamage < 500)
                        return FirearmClass.LightAntiTank;
                    else
                        return FirearmClass.HeavyAntiTank;
                }

                if (gun.playerDamageMultiplier.damage < 100)
                    return FirearmClass.DMR;
                else
                    return FirearmClass.Sniper;
            }
        }

        return FirearmClass.TooDifficultToClassify;
    }
    public enum FirearmClass
    {
        Pistol,
        MachinePistol,
        LargeMachinePistol,
        MediumSidearm,
        Shotgun,
        SmallShotgun,
        SubmachineGun,
        Rifle,
        BattleRifle,
        LightMachineGun,
        GeneralPurposeMachineGun,
        DMR,
        Sniper,
        GrenadeLauncher,
        LightAntiTank,
        HeavyAntiTank,
        TooDifficultToClassify
    }
}
