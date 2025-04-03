using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Fobs.Ammo;
using Uncreated.Warfare.Events.Models.Zones;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Fobs.SupplyCrates;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.FOBs.SupplyCrates.Throwable.AmmoBags;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Containers;
using Uncreated.Warfare.Util.Inventory;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.FOBs.Deployment.Tweaks;
public class ClaimToRearmTweaks : // todo: move this class out of this namespace
    IAsyncEventListener<ClaimBedRequested>,
    IAsyncEventListener<PlayerEnteredZone>,
    IAsyncEventListener<PlayerExitedZone>
{
    private readonly KitRequestService _kitRequestService;
    private readonly FobManager _fobManager;
    private readonly KitWeaponTextService? _kitWeaponTextService;
    private readonly ChatService _chatService;
    private readonly AmmoTranslations _translations;
    private readonly ILogger _logger;
    private readonly ZoneStore? _zoneStore;
    private readonly AssetConfiguration _assetConfiguration;


    public ClaimToRearmTweaks(IServiceProvider serviceProvider, ILogger<ClaimToRearmTweaks> logger)
    {
        _assetConfiguration = serviceProvider.GetRequiredService<AssetConfiguration>();
        _fobManager = serviceProvider.GetRequiredService<FobManager>();
        _kitRequestService = serviceProvider.GetRequiredService<KitRequestService>();
        _kitWeaponTextService = serviceProvider.GetService<KitWeaponTextService>();
        _chatService = serviceProvider.GetRequiredService<ChatService>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<AmmoTranslations>>().Value;
        _zoneStore = serviceProvider.GetService<ZoneStore>();
        _logger = logger;
    }

    [EventListener(RequireActiveLayout = true)]
    public async UniTask HandleEventAsync(ClaimBedRequested e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        IAmmoStorage? ammoStorage = null;
        if (e.Buildable.Model.TryGetComponent(out PlacedAmmoBagComponent ammoBag))
        {
            ammoStorage = ammoBag;
        }
        else
        {
            SupplyCrate? ammoCrate = _fobManager.Entities.OfType<SupplyCrate>().FirstOrDefault(s =>
                s.Type == SupplyType.Ammo &&
                !s.Buildable.IsDead &&
                s.Buildable.Equals(e.Buildable)
            );
            
            if (ammoCrate != null)
                ammoStorage = AmmoSupplyCrate.FromSupplyCrate(ammoCrate, _fobManager);
        }
        
        if (ammoStorage == null)
            return;

        if (e.Player.Team.GroupId != e.Buildable.Group)
        {
            _chatService.Send(e.Player, _translations.AmmoWrongTeam);
            e.Cancel();
            return;
        }

        Kit? kit = null;
        if (e.Player.TryGetFromContainer(out KitPlayerComponent? kitComponent) && kitComponent.ActiveKitKey.HasValue)
        {
            kit = await kitComponent.GetActiveKitAsync(KitInclude.Giveable, token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
        }

        if (kit == null)
        {
            _chatService.Send(e.Player, _translations.AmmoNoKit);
            e.Cancel();
            return;
        }

        float rearmCost = GetRearmCost(e.Player.UnturnedPlayer.inventory, kit);
        if (rearmCost == 0)
        {
            _chatService.Send(e.Player, _translations.AmmoAlreadyFull);
            e.Cancel();
            return;
        }

        // if (rearmCost > ammoStorage.AmmoCount)
        // {
        //     _chatService.Send(e.Player, _translations.AmmoInsufficient, ammoStorage.AmmoCount, rearmCost);
        //     e.Cancel();
        //     return;
        // }

        Task task = _kitRequestService.RestockKitAsync(e.Player, resupplyAmmoBags: ammoStorage is not PlacedAmmoBagComponent, token);

        ammoStorage.SubtractAmmo(rearmCost);

        e.Player.SendToast(new ToastMessage(ToastMessageStyle.Tip, _translations.ToastLoseAmmo.Translate(rearmCost, e.Player)));
        _chatService.Send(e.Player, _translations.AmmoResuppliedKit, rearmCost, ammoStorage.AmmoCount);
        
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new PlayerRearmedKit()
        {
            Player = e.Player,
            AmmoConsumed = rearmCost,
            AmmoStorage = ammoStorage,
            Kit = kit
        }, CancellationToken.None);
        
        EffectUtility.TriggerEffect(
            _assetConfiguration.GetAssetLink<EffectAsset>("Effects:Resupply").GetAssetOrFail(),
            EffectManager.SMALL,
            e.Player.Position,
            true
        );
        
        e.Cancel();

        await task.ConfigureAwait(false);
    }

    private float GetRearmCost(PlayerInventory inventory, Kit kit)
    {
        float totalRearmCost = 0;

        HashSet<Item> magazinesAlreadyCounted = new HashSet<Item>();
        foreach (ItemGunAsset gun in GetUniqueGunsInKit(kit))
        {
            int fullmags = CountFullMags(inventory, gun, magazinesAlreadyCounted);
            int requiredMags = CountMagsInKit(kit, gun);

            FirearmClass firearmClass = ItemUtility.GetFirearmClass(gun);

            if (fullmags >= requiredMags)
            {
                //_logger.LogDebug($"Weapon '{gun.FriendlyName}' ({firearmClass}) already has {fullmags}/{requiredMags} full mags, and does not need to be resupplied.");
                continue;
            }

            int magsThatNeedRefilling = requiredMags - fullmags;
            
            float magazinesRefillCost = GetMagazineCost(firearmClass) * magsThatNeedRefilling;

            //_logger.LogDebug($"Weapon '{gun.FriendlyName}' ({firearmClass}) has {magsThatNeedRefilling} that need refilling ({fullmags}/{requiredMags} full mags). The resupply cost will be {magazinesRefillCost}.");

            totalRearmCost += magazinesRefillCost;
        }

        foreach (KeyValuePair<ItemAsset, int> count in GetEquipmentCountsInKit(kit))
        {
            ItemAsset equipmentAsset = count.Key;
            int requiredCount = count.Value;

            int countInInventory = CountItemsInInventory(inventory, equipmentAsset);

            if (countInInventory >= requiredCount)
            {
                //_logger.LogDebug($"Equipment type '{equipmentAsset.FriendlyName}' already has {countInInventory}/{requiredCount} units, and does not need to be resupplied.");
                continue;
            }
            int numberToResupply = requiredCount - countInInventory;
            float equipmentResupplyCost = GetEquipmentCost(equipmentAsset) * numberToResupply;

            //_logger.LogDebug($"Equipment type '{equipmentAsset.FriendlyName}' is missing {numberToResupply} units ({countInInventory}/{requiredCount} required). The resupply cost will be {equipmentResupplyCost}.");

            totalRearmCost += equipmentResupplyCost;
        }

        return totalRearmCost;
    }

    private List<ItemGunAsset> GetUniqueGunsInKit(Kit kit)
    {
        List<ItemGunAsset> guns = new List<ItemGunAsset>();
        foreach (IKitItem item in kit.Items)
        {
            if (item is not IConcreteItem concrete)
                continue;

            if (!concrete.Item.TryGetAsset(out ItemAsset? asset) || asset is not ItemGunAsset gunAsset)
                continue;

            if (guns.Contains(gunAsset)) // avoid having the same type of gun in the list, because it leads to duplicated ammo costs
                continue;

            // the kit weapon filter blacklists guns that aren't real guns like the laser designator
            if (_kitWeaponTextService != null && _kitWeaponTextService.IsBlacklisted(concrete.Item))
                continue;

            guns.Add(gunAsset);
        }
        return guns;
    }

    private Dictionary<ItemAsset, int> GetEquipmentCountsInKit(Kit kit)
    {
        Dictionary<ItemAsset, int> equipment = new Dictionary<ItemAsset, int>();
        foreach (IKitItem item in kit.Items)
        {
            if (item is not IConcreteItem concrete)
                continue;

            if (!concrete.Item.TryGetAsset(out ItemAsset? asset))
                continue;

            if (GetEquipmentCost(asset) == 0)
                continue;

            if (!equipment.TryAdd(asset, 1))
                equipment[asset]++;
        }
        return equipment;
    }

    private int CountFullMags(PlayerInventory inventory, ItemGunAsset gun, HashSet<Item> alreadyCounted)
    {
        int count = 0;
        for (int p = 0; p < PlayerInventory.STORAGE; ++p)
        {
            Items page = inventory.items[p];
            foreach (ItemJar itemJar in page.items)
            {
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
        foreach (IKitItem item in kit.Items)
        {
            if (item is not IConcreteItem concrete)
                continue;

            if (!concrete.Item.TryGetAsset(out ItemAsset? asset))
                continue;

            if (asset.GUID == correspondingGun.GUID)
            {
                count++;
            }
            else if (asset is ItemMagazineAsset magazine && correspondingGun.magazineCalibers.Any(c => magazine.calibers.Contains(c)))
            {
                count++;
            }
        }
        return count;
    }

    private int CountItemsInInventory(PlayerInventory inventory, ItemAsset matchingItem)
    {
        int count = 0;
        for (int p = 0; p < PlayerInventory.STORAGE; ++p)
        {
            Items page = inventory.items[p];
            foreach (ItemJar itemJar in page.items)
            {
                // todo: replace with GUID whenever nelson makes that happen
                if (itemJar.item.id == matchingItem.id)
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
    
    [EventListener(Priority = -1)]
    public async UniTask HandleEventAsync(PlayerEnteredZone e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        if (e.Zone.Type is not ZoneType.MainBase)
            return;

        if (_zoneStore == null || _zoneStore.IsInWarRoom(e.Player))
            return;

        await _kitRequestService.RestockKitAsync(e.Player, true, token);
    }

    [EventListener(Priority = -1)]
    public async UniTask HandleEventAsync(PlayerExitedZone e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        if (e.Zone.Type is not ZoneType.WarRoom)
            return;

        if (_zoneStore == null)
            return;

        if (!_zoneStore.IsInMainBase(e.Player) || _zoneStore.IsInWarRoom(e.Player))
            return;

        Kit? kit = await e.Player.Component<KitPlayerComponent>().GetActiveKitAsync(KitInclude.Giveable, token);
        if (kit is { Class: > Class.Unarmed })
            await _kitRequestService.RestockKitAsync(e.Player, true, token);
    }
}
