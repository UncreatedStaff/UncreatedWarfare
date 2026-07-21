using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models.Fobs.Ammo;
using Uncreated.Warfare.Fobs.SupplyCrates;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.FOBs.SupplyCrates.Throwable.AmmoBags;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Containers;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Kits.Requests;

/// <summary>
/// Handles calculating the cost to re-arm a player.
/// </summary>
public class KitRearmService : BaseAlternateConfigurationFile // WARNING: not registered with all interfaces cause IConfiguration
{
    private readonly KitWeaponTextService? _kitWeaponTextService;
    private readonly ILogger<KitRearmService> _logger;
    private readonly IKitItemResolver _resolver;
    private readonly KitRequestService _kitRequestService;
    private readonly ChatService _chatService;
    private readonly AssetConfiguration _assetConfiguration;
    private readonly AmmoTranslations _translations;

    private readonly List<ItemGunAsset> _gunBuffer = new List<ItemGunAsset>();

    private float[] _magazineCosts;

    private float _equipmentFragThrowable;
    private float _equipmentOtherThrowable;
    private float _equipmentMedicalHigh;
    private float _equipmentMedicalLow;
    private float _equipmentVehicleTrap;
    private float _equipmentTrapHigh;
    private float _equipmentTrapLow;
    private float _equipmentBuildableSmall;
    private float _equipmentBuildableLarge;
    private float _equipmentChargeSmall;
    private float _equipmentChargeMedium;
    private float _equipmentChargeLarge;

    public KitRearmService(
        ILogger<KitRearmService> logger,
        IServiceProvider serviceProvider,
        IKitItemResolver resolver,
        KitRequestService kitRequestService,
        TranslationInjection<AmmoTranslations> translations,
        ChatService chatService,
        AssetConfiguration assetConfiguration,
        KitWeaponTextService? kitWeaponTextService = null
    ) : base(serviceProvider, "Kits/Rearm Costs.yml")
    {
        _kitWeaponTextService = kitWeaponTextService;
        _logger = logger;
        _resolver = resolver;
        _kitRequestService = kitRequestService;
        _chatService = chatService;
        _assetConfiguration = assetConfiguration;
        _translations = translations.Value;

        _magazineCosts ??= new float[(int)EnumUtility.GetMaximumValue<FirearmClass>() + 1];
    }

    protected override void HandleLoaded() => HandleChange();
    protected override void HandleChange()
    {
        IConfigurationSection magazines = GetSection("Magazines");

        float[] costs = new float[(int)EnumUtility.GetMaximumValue<FirearmClass>() + 1];
        for (int i = 0; i < costs.Length; ++i)
        {
            string name = EnumUtility.GetNameSafe((FirearmClass)i);
            float cost = magazines.GetValue(name, -1f);

            if (cost < 0)
            {
                _logger.LogWarning($"Missing firearm class in config: {(FirearmClass)i}.");
                continue;
            }

            costs[i] = cost;
        }

        _magazineCosts = costs;

        IConfigurationSection equipment = GetSection("Equipment");

        _equipmentFragThrowable = equipment.GetValue<float>("FragThrowable");
        _equipmentOtherThrowable = equipment.GetValue<float>("OtherThrowable");
        _equipmentMedicalHigh = equipment.GetValue<float>("MedicalHigh");
        _equipmentMedicalLow = equipment.GetValue<float>("MedicalLow");
        _equipmentVehicleTrap = equipment.GetValue<float>("VehicleTrap");
        _equipmentTrapHigh = equipment.GetValue<float>("TrapHigh");
        _equipmentTrapLow = equipment.GetValue<float>("TrapLow");
        _equipmentChargeSmall = equipment.GetValue<float>("ChargeSmall");
        _equipmentChargeMedium = equipment.GetValue<float>("ChargeMedium");
        _equipmentChargeLarge = equipment.GetValue<float>("ChargeLarge");
        _equipmentBuildableSmall = equipment.GetValue<float>("BuildableSmall");
        _equipmentBuildableLarge = equipment.GetValue<float>("BuildableLarge");
    }

    public async Task<RearmResult> RearmAsync(WarfarePlayer player, IAmmoStorage ammoStorage, CancellationToken token = default)
    {
        // just in case they somehow got a preview kit out of main
        await _kitRequestService.RevertPreviewAsync(player, token);

        Kit? kit = null;
        if (player.TryGetFromContainer(out KitPlayerComponent? kitComponent) && kitComponent.HasKit)
        {
            kit = await kitComponent.GetActiveKitAsync(KitInclude.Giveable, token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
        }

        if (kit == null)
        {
            _chatService.Send(player, _translations.AmmoNoKit);
            return new RearmResult(RearmResultType.NoKit, 0f, ammoStorage.AmmoCount);
        }

        float rearmCost = GetRearmCost(player, kit);
        if (rearmCost == 0)
        {
            _chatService.Send(player, _translations.AmmoAlreadyFull);
            return new RearmResult(RearmResultType.AlreadyFull, 0f, ammoStorage.AmmoCount);
        }

        Task task = _kitRequestService.RestockKitAsync(player, resupplyAmmoBags: ammoStorage is not PlacedAmmoBagComponent, token);

        ApplyRearm(player, rearmCost, ammoStorage, kit, token);

        await task.ConfigureAwait(false);

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new PlayerRearmedKit
        {
            Player = player,
            AmmoConsumed = rearmCost,
            AmmoStorage = ammoStorage,
            Kit = kit
        }, CancellationToken.None);

        return new RearmResult(RearmResultType.Rearmed, rearmCost, ammoStorage.AmmoCount);
    }

    public void ApplyRearm(WarfarePlayer player, float rearmCost, IAmmoStorage ammoStorage, Kit kit, CancellationToken token = default)
    {
        ammoStorage.SubtractAmmo(rearmCost);

        player.SendToast(new ToastMessage(ToastMessageStyle.Tip, _translations.ToastLoseAmmo.Translate(rearmCost, player)));

        float ammoLeft = ammoStorage.AmmoCount;
        if (float.IsFinite(ammoLeft))
        {
            _chatService.Send(player, _translations.AmmoResuppliedKit, rearmCost, ammoStorage.AmmoCount);
        }
        else
        {
            _chatService.Send(player, _translations.AmmoResuppliedKitInfinite);
        }

        EffectUtility.TriggerEffect(
            _assetConfiguration.GetAssetLink<EffectAsset>("Effects:Resupply").GetAssetOrFail(),
            EffectManager.SMALL,
            player.Position,
            true
        );
    }

    public struct RearmResult
    {
        public readonly RearmResultType Result;
        public readonly float AmmoConsumed;
        public readonly float AmmoLeft;

        public RearmResult(RearmResultType result, float ammoConsumed, float ammoLeft)
        {
            Result = result;
            AmmoConsumed = ammoConsumed;
            AmmoLeft = ammoLeft;
        }
    }

    public enum RearmResultType
    {
        Rearmed,
        AlreadyFull,
        NoKit
    }

    /// <summary>
    /// Gets the ammo supply cost of refilling one magazine of the given <paramref name="firearmType"/>.
    /// </summary>
    public virtual float GetMagazineCost(FirearmClass firearmType)
    {
        float[] costs = _magazineCosts;
        int index = (int)firearmType;

        if (index < 0 || index >= costs.Length)
            return costs[(int)FirearmClass.TooDifficultToClassify];

        return costs[index];
    }

    /// <inheritdoc cref="GetRearmCost(WarfarePlayer, Kit)"/>
    public virtual float GetRearmCost(WarfarePlayer player)
    {
        Kit? kit = player.Component<KitPlayerComponent>().GetActiveEffectiveKit()?.CachedKit;
        if (kit == null)
        {
            return 0;
        }

        return GetRearmCost(player, kit);
    }

    /// <summary>
    /// Gets the full cost to rearm the given player.
    /// </summary>
    public virtual float GetRearmCost(WarfarePlayer player, Kit kit)
    {
        GameThread.AssertCurrent();

        float totalRearmCost = 0;


        PlayerInventory inventory = player.UnturnedPlayer.inventory;

        HashSet<Item> magazinesAlreadyCounted = new HashSet<Item>();
        GatherUniqueGunsInKit(kit);
        foreach (ItemGunAsset gun in _gunBuffer)
        {
            int fullmags = CountFullMags(inventory, gun, magazinesAlreadyCounted);
            int requiredMags = CountMagsInKit(kit, gun);

            FirearmClass firearmClass = ItemUtility.GetFirearmClass(gun);

            if (fullmags >= requiredMags)
            {
                continue;
            }

            int magsThatNeedRefilling = requiredMags - fullmags;

            float magazinesRefillCost = GetMagazineCost(firearmClass) * magsThatNeedRefilling;

            totalRearmCost += magazinesRefillCost;
        }

        _gunBuffer.Clear();

        foreach (KeyValuePair<ItemAsset, int> count in GetEquipmentCountsInKit(kit, player.Team))
        {
            ItemAsset equipmentAsset = count.Key;
            int requiredCount = count.Value;

            int countInInventory = CountItemsInInventory(inventory, equipmentAsset);

            if (countInInventory >= requiredCount)
            {
                continue;
            }
            int numberToResupply = requiredCount - countInInventory;
            float equipmentResupplyCost = GetEquipmentCost(equipmentAsset) * numberToResupply;

            totalRearmCost += equipmentResupplyCost;
        }

        return totalRearmCost;
    }

    private void GatherUniqueGunsInKit(Kit kit)
    {
        _gunBuffer.Clear();
        foreach (IKitItem item in kit.Items)
        {
            if (item is not IConcreteItem concrete)
                continue;

            if (!concrete.Item.TryGetAsset(out ItemAsset? asset) || asset is not ItemGunAsset gunAsset)
                continue;

            if (_gunBuffer.Contains(gunAsset)) // avoid having the same type of gun in the list, because it leads to duplicated ammo costs
                continue;

            // the kit weapon filter blacklists guns that aren't real guns like the laser designator
            if (_kitWeaponTextService != null && _kitWeaponTextService.IsBlacklisted(concrete.Item))
                continue;

            _gunBuffer.Add(gunAsset);
        }
    }

    private Dictionary<ItemAsset, int> GetEquipmentCountsInKit(Kit kit, Team team)
    {
        Dictionary<ItemAsset, int> equipment = new Dictionary<ItemAsset, int>();
        foreach (IKitItem item in kit.Items)
        {
            KitItemResolutionResult result = _resolver.ResolveKitItem(item, kit, team);

            if (result.Asset == null)
                continue;

            if (GetEquipmentCost(result.Asset) == 0)
                continue;

            if (!equipment.TryAdd(result.Asset, 1))
                equipment[result.Asset]++;
        }
        return equipment;
    }

    private static int CountFullMags(PlayerInventory inventory, ItemGunAsset gun, HashSet<Item> alreadyCounted)
    {
        int count = 0;
        for (int p = 0; p < PlayerInventory.STORAGE; ++p)
        {
            SDG.Unturned.Items page = inventory.items[p];
            foreach (ItemJar itemJar in page.items)
            {
                Item item = itemJar.item;

                if (alreadyCounted.Contains(item))
                    continue;

                ItemAsset asset = item.GetAsset();

                if (asset.GUID == gun.GUID) // count the mag that's inside a matching gun as well
                {
                    ushort magazineId = BitConverter.ToUInt16(itemJar.item.state, GunStateIndices.MAGAZINE_ID);
                    if (Assets.find(EAssetType.ITEM, magazineId) is ItemMagazineAsset magazineType && itemJar.item.state[GunStateIndices.AMMO] == magazineType.MaxAmount)
                    {
                        count++;
                        alreadyCounted.Add(item);
                    }
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

    private static int CountMagsInKit(Kit kit, ItemGunAsset correspondingGun)
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

    private static int CountItemsInInventory(PlayerInventory inventory, ItemAsset matchingItem)
    {
        int count = 0;
        for (int p = 0; p < PlayerInventory.STORAGE; ++p)
        {
            SDG.Unturned.Items page = inventory.items[p];
            foreach (ItemJar itemJar in page.items)
            {
                // todo: replace with GUID whenever nelson makes that happen
                if (itemJar.item.id == matchingItem.id)
                    count++;
            }
        }
        return count;
    }

    private float GetEquipmentCost(ItemAsset equipment)
    {
        switch (equipment)
        {
            case ItemThrowableAsset throwable:
                if (throwable.playerDamageMultiplier.damage > 0) // dangerous frag grenades
                    return _equipmentFragThrowable;
                return _equipmentOtherThrowable; // could be a smoke grenade

            case ItemMedicalAsset medical:
                if (medical.health < 50)
                    return _equipmentMedicalLow;
                return _equipmentMedicalHigh;

            case ItemTrapAsset trap:
                if (trap.vehicleDamage >= 500) // probably a powerful AT mine
                    return _equipmentVehicleTrap;
                if (trap.playerDamage >= 200) // probably a claymore or some anti-personnel IED
                    return _equipmentTrapHigh;
                return _equipmentTrapLow;

            case ItemChargeAsset charge:
                if (charge.range2 < 9) // small explosive
                    return _equipmentChargeSmall;
                if (charge.range2 < 16) // medium explosive
                    return _equipmentChargeMedium;
                return _equipmentChargeLarge; // beeg explosive

            case ItemPlaceableAsset buildable:
                float slotsTakeUp = buildable.size_x * buildable.size_y;

                if (slotsTakeUp < 8)
                    return _equipmentBuildableSmall; // smol barricade

                return _equipmentBuildableLarge; // medium or large barricade
            default:
                return 0;
        }
    }

}