#if DEBUG
// #define REFILL_TURRETS_DEBUG
#endif
using System;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.FOBs.SupplyCrates.Throwable.Vehicle;

public class ThrownVehicleCrate : ThrownSupplyCrate
{
    private static readonly Collider[] TempHitColliders = new Collider[4];
    private readonly EffectAsset _resupplyEffectAsset;
    private readonly FobManager? _fobManager;
    private readonly ZoneStore? _zoneStore;
    private readonly AmmoTranslations _translations;

    public ThrownVehicleCrate(GameObject throwable, WarfarePlayer thrower, ItemThrowableAsset thrownAsset, EffectAsset resupplyEffectAsset, FobManager? fobManager, ZoneStore? zoneStore, AmmoTranslations translations)
        : base(throwable, thrownAsset, thrower)
    {
        _resupplyEffectAsset = resupplyEffectAsset;
        _fobManager = fobManager;
        _zoneStore = zoneStore;
        _translations = translations;
        ThrownComponent thrownVehicleCrateComponent = throwable.gameObject.AddComponent<ThrownComponent>();
        thrownVehicleCrateComponent.OnThrowableDestroyed += OnThrowableDestroyed;
    }

    private void OnThrowableDestroyed()
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        // descending distance comparer
        IComparer<Component> comparer = new LookAtComparer<Component>(Throwable.transform.forward, x => x.transform.position - Throwable.transform.position, reverse: false);

        int results = Physics.OverlapSphereNonAlloc(Throwable.transform.position, 5f, TempHitColliders, 1 << LayerMasks.VEHICLE);
        Array.Sort<Collider>(TempHitColliders, 0, results, comparer);
        WarfareVehicle? warfareVehicle = null;
        for (int i = 0; i < results; i++)
        {
            Collider collider = TempHitColliders[i];
            WarfareVehicleComponent? warfareVehicleComponent = collider.GetComponentInParent<WarfareVehicleComponent>();
            if (warfareVehicleComponent != null)
            {
                warfareVehicle = warfareVehicleComponent.WarfareVehicle;
                break;
            }
        }

        if (warfareVehicle == null)
        {
            RespawnThrowableItem();
            Thrower.SendToast(new ToastMessage(ToastMessageStyle.Tip, _translations.ToastAmmoNotNearVehicle.Translate(Thrower)));
            return;
        }
        
        if (_fobManager != null && !(_zoneStore != null && _zoneStore.IsInMainBase(Throwable.transform.position)))
        {
            ResourceFob? nearestFob = _fobManager.FindNearestResourceFob(Thrower.Team, Throwable.transform.position);
            if (nearestFob == null)
            {
                RespawnThrowableItem();
                Thrower.SendToast(new ToastMessage(ToastMessageStyle.Tip, _translations.ToastAmmoNotNearFob.Translate(Thrower)));
                return;
            }
            
            int requiredAmmoCount = warfareVehicle.Info.Rearm.AmmoConsumed;
            if (nearestFob.AmmoCount < warfareVehicle.Info.Rearm.AmmoConsumed)
            {
                RespawnThrowableItem();
                Thrower.SendToast(new ToastMessage(ToastMessageStyle.Tip, _translations.ToastInsufficientAmmo.Translate(nearestFob.AmmoCount, requiredAmmoCount, Thrower)));
                return;
            }

            // note: this used to directly subtract from the FOB but led to mismatches between FOB ammo and crate ammo.
            // todo: is this comment^ still relevant?
            nearestFob.ChangeAmmo(-requiredAmmoCount, SupplyChangeReason.ConsumeRearmVehicle);
            Thrower.SendToast(new ToastMessage(ToastMessageStyle.Tip, _translations.ToastLoseAmmo.Translate(requiredAmmoCount, Thrower)));
        }
        
        DropSupplies(warfareVehicle);
        if (warfareVehicle.FlareEmitter != null)
            warfareVehicle.FlareEmitter.ReloadFlares();
    }

    private struct TurretRefillState
    {
        public ItemMagazineAsset? Magazine;
        public ItemGunAsset? Gun;

        /// <summary>
        /// Amount needed to add to fill up the magazine.
        /// </summary>
        public byte AmmoLeft;

        public bool PendingSendToPlayer;
    }

    private void DropSupplies(WarfareVehicle warfareVehicle)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        // collect some info about the current ammo in the turrets before refilling
        Passenger[] turrets = warfareVehicle.Vehicle.turrets;
        TurretRefillState[] turretInfo = new TurretRefillState[turrets.Length];
        for (int i = 0; i < turrets.Length; ++i)
        {
            Passenger turret = turrets[i];
            ref TurretRefillState state = ref turretInfo[i];

            state.Gun = Assets.find(EAssetType.ITEM, turret.turret.itemID) as ItemGunAsset;
            if (state.Gun == null || turret.state.Length < 18)
                continue;

            byte ammoCt = turret.state[GunStateIndices.AMMO];
            ushort magId = BitConverter.ToUInt16(turret.state, (int)AttachmentType.Magazine);
            state.Magazine = Assets.find(EAssetType.ITEM, magId) as ItemMagazineAsset;

            if (state.Magazine == null)
            {
                // no magazine loaded, choose default one
                state.Magazine = state.Gun.SelectDefaultMagazine();
                state.AmmoLeft = state.Magazine?.MaxAmountAsByte ?? state.Gun.countMax;
                continue;
            }

            byte maxAmount = state.Magazine.MaxAmountAsByte;
            if (ammoCt >= maxAmount)
            {
                // mag is already full
                continue;
            }

            state.AmmoLeft = (byte)(maxAmount - ammoCt);
        }

#if REFILL_TURRETS_DEBUG
        // debugging dump
        ILogger logger = WarfareModule.Singleton.ServiceProvider.Resolve<ILogger<ThrownVehicleCrate>>();
        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace($"Vehicle: {warfareVehicle.Asset.name}");
            logger.LogTrace($"Turrets: {turrets.Length}");
            for (int i = 0; i < turrets.Length; ++i)
            {
                Passenger turret = turrets[i];
                ref TurretRefillState state = ref turretInfo[i];

                logger.LogTrace($"-Turret {i}-: {turret.turret.itemID} (seat {turret.turret.seatIndex})");
                logger.LogTrace($"Gun       : {state.Gun?.name ?? "null"}");
                logger.LogTrace($"Magazine  : {state.Magazine?.name ?? "null"}");
                logger.LogTrace($"Ammo Left : {state.AmmoLeft}");
            }
        }
#endif

        foreach (IAssetLink<ItemAsset> itemAsset in warfareVehicle.Info.Rearm.Items)
        {
            ItemAsset? asset = itemAsset.GetAsset();
            if (asset == null)
                continue;

            byte amount = asset.MaxAmountAsByte;
            for (int i = 0; i < turrets.Length; ++i)
            {
                ref TurretRefillState state = ref turretInfo[i];
                if (state.AmmoLeft == 0)
                {
                    // already filled the turret
                    continue;
                }

                if (state.Magazine == null || state.Magazine.GUID != asset.GUID)
                    continue;

                // rearm magazine instead of dropping the magazine
                byte amountAvailable = Math.Min(state.AmmoLeft, amount);
                amount -= amountAvailable;
                state.AmmoLeft -= amountAvailable;
                state.PendingSendToPlayer |= UpdateTurretMagazineState(warfareVehicle, i, state.Magazine, state.AmmoLeft);
#if REFILL_TURRETS_DEBUG
                logger.LogTrace($"Used {amountAvailable} ammo to refill turret {i} from missing {state.AmmoLeft + amountAvailable} ammo to missing {state.AmmoLeft} ammo. Left to drop: {amount}.");
#endif
            }

            if (amount <= 0)
                continue;
            
#if REFILL_TURRETS_DEBUG
            logger.LogTrace($"Dropping item {asset.name} with amount: {amount}.");
#endif
            // if we didn't use the entire mag drop the item
            ItemManager.dropItem(new Item(asset, EItemOrigin.CRAFT) { amount = amount }, Throwable.transform.position, false, true, true);
        }

        // state could be updated multiple times for one turret, so just send them all at the end
        for (int i = 0; i < turrets.Length; ++i)
        {
            if (turretInfo[i].PendingSendToPlayer)
            {
                turrets[i].player.player.equipment.sendUpdateState();
            }
        }
        
        // spawn a nice effect
        EffectManager.triggerEffect(new TriggerEffectParameters(_resupplyEffectAsset)
        {
            position = Throwable.transform.position,
            relevantDistance = 70,
            reliable = true
        });
    }

    /// <returns>Needs sent to player?</returns>
    private static bool UpdateTurretMagazineState(WarfareVehicle warfareVehicle, int turretIndex, ItemMagazineAsset magazine, byte amountFromFull)
    {
        byte full = magazine.MaxAmountAsByte;
        byte amount = (byte)(full - Math.Min(full, amountFromFull));

        InteractableVehicle vehicle = warfareVehicle.Vehicle;
        Passenger turret = vehicle.turrets[turretIndex];

        BitConverter.TryWriteBytes(
            turret.state.AsSpan((int)AttachmentType.Magazine),
            magazine.id
        );
        turret.state[GunStateIndices.MAGAZINE_QUALITY] = 100;
        turret.state[GunStateIndices.AMMO] = amount;

        return turret.player != null;
    }
}