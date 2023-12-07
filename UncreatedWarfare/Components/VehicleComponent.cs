using JetBrains.Annotations;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Events.Vehicles;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Random = UnityEngine.Random;
using XPReward = Uncreated.Warfare.Levels.XPReward;

namespace Uncreated.Warfare.Components;

public class VehicleComponent : MonoBehaviour
{
    public static readonly VehicleHUD VehicleHUD = new VehicleHUD();

    private Zone? _noDropZone;
    private Zone? _noPickZone;
    private Zone? _safezoneZone;
    public Guid LastItem;
    public bool LastItemIsVehicle;
    public InteractableVehicle Vehicle;
    public SqlItem<VehicleData>? Data;
    public EDamageOrigin LastDamageOrigin;
    public ulong LastInstigator;
    private bool _isResupplied;
    private Coroutine? _quotaLoop;
    private Coroutine? _autoSupplyLoop;
    private Dictionary<ulong, DateTime> _timeEnteredTable;
    private Dictionary<ulong, DateTime> _timeRewardedTable;
    public Dictionary<ulong, KeyValuePair<ushort, DateTime>> DamageTable;
    public ulong PreviousOwner;
    public float TimeRequested { get; set; }
    public Stack<ulong> OwnerHistory { get; } = new Stack<ulong>(2);
    public ulong Team => Vehicle.lockedGroup.m_SteamID.GetTeam();
    public Dictionary<ulong, Vector3> TransportTable { get; private set; }
    public Dictionary<ulong, double> UsageTable { get; private set; }
    public float Quota { get; set; }
    public float RequiredQuota { get; set; }
    public Coroutine? ForceSupplyLoop { get; private set; }

    public Zone? SafezoneZone
    {
        get
        {
            if (Warfare.Data.Gamemode is null)
                return null;
            if (Time.time - _lastZoneCheck < Warfare.Data.Gamemode.EventLoopSpeed)
                CheckZones();
            return _safezoneZone;
        }
    }
    public Zone? NoDropZone
    {
        get
        {
            if (Warfare.Data.Gamemode is null)
                return null;
            if (Time.time - _lastZoneCheck < Warfare.Data.Gamemode.EventLoopSpeed)
                CheckZones();
            return _noDropZone;
        }
    }
    public Zone? NoPickZone
    {
        get
        {
            if (Warfare.Data.Gamemode is null)
                return null;
            if (Time.time - _lastZoneCheck < Warfare.Data.Gamemode.EventLoopSpeed)
                CheckZones();
            return _noPickZone;
        }
    }

    public bool IsGroundVehicle => Data?.Item != null && VehicleData.IsGroundVehicle(Data.Item.Type);
    public bool IsArmor => Data?.Item != null && VehicleData.IsArmor(Data.Item.Type);
    public bool IsLogistics => Data?.Item != null && VehicleData.IsLogistics(Data.Item.Type);
    public bool IsAircraft => Data?.Item != null && VehicleData.IsAircraft(Data.Item.Type);
    public bool IsAssaultAircraft => Data?.Item != null && VehicleData.IsAssaultAircraft(Data.Item.Type);
    public bool IsEmplacement => Data?.Item != null && VehicleData.IsEmplacement(Data.Item.Type);
    public bool IsInVehiclebay => Data?.Item != null;
    public bool CanTransport => Data?.Item != null && VehicleData.CanTransport(Data.Item, Vehicle);

    public void Initialize(InteractableVehicle vehicle)
    {
        Vehicle = vehicle;
        TransportTable = new Dictionary<ulong, Vector3>();
        UsageTable = new Dictionary<ulong, double>();
        _timeEnteredTable = new Dictionary<ulong, DateTime>();
        _timeRewardedTable = new Dictionary<ulong, DateTime>();
        DamageTable = new Dictionary<ulong, KeyValuePair<ushort, DateTime>>();
        _isResupplied = true;
        if (vehicle.lockedOwner.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
            OwnerHistory.Push(vehicle.lockedOwner.m_SteamID);

        Quota = 0;
        RequiredQuota = -1;

        VehicleBay? bay = VehicleBay.GetSingletonQuick();
        if (bay != null)
        {
            Data = bay.GetDataProxySync(Vehicle.asset.GUID);
            if (Data?.Item != null)
                vehicle.transform.gameObject.AddComponent<SpottedComponent>().Initialize(Data.Item.Type, vehicle);
        }
        _lastPosInterval = transform.position;

        foreach (var passenger in Vehicle.turrets)
        {
            if (VehicleBay.Config.GroundAAWeapons.HasID(passenger.turret.itemID))
                passenger.turretAim.gameObject.AddComponent<HeatSeekingController>().Initialize(700, 1500, Gamemode.Config.EffectLockOn1, 0.7f, 14.6f);
            if (VehicleBay.Config.AirAAWeapons.HasID(passenger.turret.itemID))
                passenger.turretAim.gameObject.AddComponent<HeatSeekingController>().Initialize(600, Gamemode.Config.EffectLockOn2, 1, 11);
        }

        ReloadCountermeasures();
    }
    public static void TryAddOwnerToHistory(InteractableVehicle vehicle, ulong steam64)
    {
        if (vehicle.TryGetComponent(out VehicleComponent comp))
        {
            if (comp.OwnerHistory.Count > 0)
                comp.PreviousOwner = comp.OwnerHistory.Peek();
            else
                comp.TimeRequested = Time.realtimeSinceStartup;
            comp.OwnerHistory.Push(steam64);
        }
    }
    public bool IsType(VehicleType type) => Data?.Item != null && Data.Item.Type == type;
    private void CheckZones()
    {
        _lastZoneCheck = Time.time;
        if (Warfare.Data.Singletons.TryGetSingleton(out ZoneList zoneList))
        {
            zoneList.WriteWait();
            try
            {
                _safezoneZone = null;
                _noDropZone = null;
                _noPickZone = null;
                for (int i = 0; i < zoneList.Items.Count; ++i)
                {
                    Zone? zone = zoneList.Items[i]?.Item;
                    if (zone is null || zone.Data.Flags == ZoneFlags.None || !zone.IsInside(transform.position))
                        continue;
                    if ((zone.Data.Flags & ZoneFlags.Safezone) != 0)
                    {
                        if (_safezoneZone is null || _safezoneZone.BoundsArea > zone.BoundsArea)
                        {
                            _safezoneZone = zone;
                            L.LogDebug($"Vehicle {Vehicle.asset.name} in sz {_safezoneZone.Name}.");
                        }
                    }
                    if ((zone.Data.Flags & ZoneFlags.NoDropItems) != 0)
                    {
                        if (_noDropZone is null || _noDropZone.BoundsArea > zone.BoundsArea)
                        {
                            _noDropZone = zone;
                            L.LogDebug($"Vehicle {Vehicle.asset.name} in ndz {_noDropZone.Name}.");
                        }
                    }
                    if ((zone.Data.Flags & ZoneFlags.NoPickItems) != 0)
                    {
                        if (_noPickZone is null || _noPickZone.BoundsArea > zone.BoundsArea)
                        {
                            _noPickZone = zone;
                            L.LogDebug($"Vehicle {Vehicle.asset.name} in npz {_noPickZone.Name}.");
                        }
                    }
                }
            }
            finally
            {
                zoneList.WriteRelease();
            }
        }
    }
    private bool TryEnsureDataUpdated()
    {
        if (Data?.Item == null)
        {
            if (Warfare.Data.Singletons.TryGetSingleton(out VehicleBay bay))
            {
                Data = bay.GetDataProxySync(Vehicle.asset.GUID);
                return Data?.Item != null;
            }
            return false;
        }
        return true;
    }
    private void ShowHUD(UCPlayer player, byte seat)
    {
        if (Data?.Item == null)
            return;

        if (!IsAircraft)
            return;

        if (!Data.Item.CrewSeats.ArrayContains(seat))
            return;

        VehicleHUD.SendToPlayer(player.Connection);

        VehicleHUD.MissileWarning.SetVisibility(player.Connection, false);
        VehicleHUD.MissileWarningDriver.SetVisibility(player.Connection, false);
        VehicleHUD.FlareCount.SetVisibility(player.Connection, seat == 0);

        if (seat == 0)
            VehicleHUD.FlareCount.SetText(player.Connection, "FLARES: " + _totalFlaresLeft);
    }
    private void UpdateHUDFlares()
    {
        if (!IsAircraft)
            return;

        var driver = Vehicle.passengers[0].player;
        if (driver != null)
            VehicleHUD.FlareCount.SetText(driver.transportConnection, "FLARES: " + _totalFlaresLeft);
    }
    private void HideHUD(UCPlayer player)
    {
        VehicleHUD.ClearFromPlayer(player.Connection);
    }
    internal void OnPlayerEnteredVehicle(EnterVehicle e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer player = e.Player;

        byte toSeat = e.PassengerIndex;
        if (toSeat == 0)
        {
            LastDriver = player.Steam64;
            LastDriverTime = Time.realtimeSinceStartup;
            _totalDistance = 0;
        }
        if (TryEnsureDataUpdated())
        {
            if (!Data!.Item!.IsCrewSeat(toSeat))
            {
                TransportTable[player.Steam64] = player.Position;
            }

            _timeEnteredTable[player.Steam64] = DateTime.UtcNow;

            if (_quotaLoop is null)
            {
                RequiredQuota = Data.Item.TicketCost * 0.5f;
                _quotaLoop = StartCoroutine(QuotaLoop());
            }
        }
        ShowHUD(player, toSeat);
    }
    public void OnPlayerExitedVehicle(ExitVehicle e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (IsInVehiclebay)
            EvaluateUsage(e.Player.Player.channel.owner);

        HideHUD(e.Player);
        
        if (e.Player.KitClass == Class.Squadleader &&
            (Data?.Item != null && VehicleData.IsLogistics(Data.Item.Type)) &&
            !F.IsInMain(e.Player.Position) &&
            Warfare.Data.Singletons.GetSingleton<FOBManager>()?.FindNearestFOB<FOB>(e.Player.Position, e.Player.GetTeam()) == null
            )
        {
            Tips.TryGiveTip(e.Player, 300, T.TipPlaceRadio);
        }

        if (e.Vehicle.passengers.Length > 0 && e.Vehicle.passengers[0] == null || e.Vehicle.passengers[0].player == null ||
            e.Vehicle.passengers[0].player.player.channel.owner.playerID.steamID.m_SteamID == e.Player.Steam64)
        {
            if (LastDriver == e.Player.Steam64)
                LastDriverDistance = _totalDistance;
            return;
        }
        if (TransportTable.TryGetValue(e.Player.Steam64, out Vector3 original))
        {
            float distance = (e.Player.Position - original).magnitude;
            if (distance >= 200 && e.Player.KitClass is not Class.Crewman and not Class.Pilot)
            {
                if (!(_timeRewardedTable.TryGetValue(e.Player.Steam64, out DateTime time) && (DateTime.UtcNow - time).TotalSeconds < 60))
                {
                    int amount = (int)(Math.Floor(distance / 100) * 2) + 5;

                    Player player = e.Vehicle.passengers[0].player.player;
                    if (UCPlayer.FromPlayer(player) is { } pl)
                        Points.AwardXP(pl, XPReward.TransportingPlayer, T.XPToastTransportingPlayers, amount);

                    Quota += 0.5F;

                    _timeRewardedTable[e.Player.Steam64] = DateTime.UtcNow;
                }
            }
            TransportTable.Remove(e.Player.Steam64);
        }
    }
    public void OnPlayerSwapSeatRequested(VehicleSwapSeat e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (e.NewSeat == 0)
        {
            // new driver
            LastDriver = e.Player.Steam64;
            LastDriverTime = Time.realtimeSinceStartup;
            _totalDistance = 0;
        }

        if (IsInVehiclebay)
        {
            EvaluateUsage(e.Player.SteamPlayer);

            if (!Data!.Item!.CrewSeats.Contains(e.NewSeat))
                TransportTable.TryAdd(e.Player.Steam64, e.Player.Position);
            else
                TransportTable.Remove(e.Player.Steam64);
        }

        ShowHUD(e.Player, e.NewSeat);
    }
    public void EvaluateUsage(SteamPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        byte currentSeat = player.player.movement.getSeat();
        bool isCrewSeat = Data?.Item != null && Data.Item.CrewSeats.ArrayContains(currentSeat);

        if (currentSeat == 0 || isCrewSeat)
        {
            ulong s64 = player.playerID.steamID.m_SteamID;

            if (_timeEnteredTable.TryGetValue(s64, out DateTime start))
            {
                double time = (DateTime.UtcNow - start).TotalSeconds;
                if (!UsageTable.ContainsKey(s64))
                    UsageTable.Add(s64, time);
                else
                    UsageTable[s64] += time;
            }
        }
    }
    private int _totalFlaresLeft;
    public void ReloadCountermeasures()
    {
        if (Data?.Item is { Type: var type })
        {
            _totalFlaresLeft = type switch
            {
                VehicleType.AttackHeli => StartingFlaresAttackHeli,
                VehicleType.TransportAir => StartingFlaresTransportHeli,
                VehicleType.Jet => StartingFlaresJet,
                _ => _totalFlaresLeft
            };
        }

        UpdateHUDFlares();
    }
    public void ReceiveMissileWarning()
    {
        if (_warningRoutine is not null)
            StopCoroutine(_warningRoutine);

        _warningRoutine = StartCoroutine(WarningRoutine());
    }
    private Coroutine _warningRoutine;
    private IEnumerator<WaitForSeconds> WarningRoutine()
    {
        ToggleMissileWarning(true);
        yield return new WaitForSeconds(1);
        ToggleMissileWarning(false);
    }
    private void ToggleMissileWarning(bool enabled)
    {
        if (Data is null)
            return;
        ListSqlConfig<VehicleData>? manager = Data.Manager;
        if (manager is null)
            return;
        manager.WriteWait();
        try
        {
            VehicleData? data = Data.Item;
            if (data is null)
                return;
            for (byte i = 0; i < Vehicle.passengers.Length; i++)
            {
                Passenger passenger = Vehicle.passengers[i];
                if (passenger?.player != null && data.IsCrewSeat(i))
                {
                    VehicleHUD.MissileWarning.SetVisibility(passenger.player.transportConnection, enabled);
                    if (i == 0)
                        VehicleHUD.MissileWarningDriver.SetVisibility(passenger.player.transportConnection, enabled);
                }
            }
        }
        finally
        {
            manager.WriteRelease();
        }

    }
    public void StartForceLoadSupplies(UCPlayer caller, SupplyType type, int amount)
    {
        ForceSupplyLoop = StartCoroutine(ForceSupplyLoopCoroutine(caller, type, amount));
    }
    public bool TryStartAutoLoadSupplies()
    {
        if (_isResupplied || _autoSupplyLoop != null || Data?.Item is null || Data.Item.Metadata is null || Data.Item.Metadata.TrunkItems is null || Vehicle.trunkItems is null)
            return false;

        _autoSupplyLoop = StartCoroutine(AutoSupplyLoop());
        return true;
    }
    private IEnumerator<WaitForSeconds> ForceSupplyLoopCoroutine(UCPlayer caller, SupplyType type, int amount)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ItemAsset? supplyAsset;

        if (type is SupplyType.Build)
            TeamManager.GetFaction(Team).Build.ValidReference(out supplyAsset);
        else if (type is SupplyType.Ammo)
            TeamManager.GetFaction(Team).Ammo.ValidReference(out supplyAsset);
        else
        {
            caller.SendChat(T.UnknownError);
            yield break;
        }
        if (supplyAsset == null)
        {
            caller.SendChat(T.UnknownError);
            yield break;
        }
        int addedNewCount = 0;
        int loaderBreak = 0;
        for (int i = 0; i < amount; i++)
        {
            if (!TeamManager.IsInAnyMain(Vehicle.transform.position))
                break;
            if (!Vehicle.trunkItems.tryAddItem(new Item(supplyAsset.id, true)))
                break;

            addedNewCount++;
            loaderBreak++;
            if (loaderBreak >= 3)
            {
                loaderBreak = 0;
                F.TryTriggerSupplyEffect(type, Vehicle.transform.position);
                yield return new WaitForSeconds(1);

                while (!(Vehicle.speed >= -1 && Vehicle.speed <= 1))
                    yield return new WaitForSeconds(1);
            }
        }

        caller.SendChat(type is SupplyType.Build ? T.LoadCompleteBuild : T.LoadCompleteAmmo, addedNewCount);

        ForceSupplyLoop = null;
    }
    private IEnumerator<WaitForSeconds> AutoSupplyLoop()
    {
        TeamManager.GetFaction(Team).Build.ValidReference(out ItemAsset? build);
        TeamManager.GetFaction(Team).Ammo.ValidReference(out ItemAsset? ammo);

        UCPlayer? driver = UCPlayer.FromID(LastDriver);

        int loaderCount = 0;

        bool shouldMessagePlayer = false;
        if (Data?.Item == null)
            yield break;
        if (Data.Item.Metadata?.TrunkItems != null)
        {
            List<ISpecificPageKitItem> trunk = Data.Item.Metadata.TrunkItems;
            for (int i = 0; i < trunk.Count; i++)
            {
                ItemAsset? asset;
                ISpecificPageKitItem trunkItem = trunk[i];
                if (build is not null && trunkItem.Item.Guid == build.GUID) asset = build;
                else if (ammo is not null && trunkItem.Item.Guid == ammo.GUID) asset = ammo;
                else asset = trunkItem.Item.GetAsset<ItemAsset>();

                if (asset is not null && Vehicle.trunkItems.checkSpaceEmpty(trunkItem.X, trunkItem.Y, asset.size_x, asset.size_y, trunkItem.Rotation) &&
                    TeamManager.IsInMain(Vehicle.lockedGroup.m_SteamID.GetTeam(), Vehicle.transform.position))
                {
                    Item item = new Item(asset.id, trunkItem.Amount, 100, Util.CloneBytes(trunkItem.State));
                    Vehicle.trunkItems.addItem(trunkItem.X, trunkItem.Y, trunkItem.Rotation, item);
                    loaderCount++;

                    if (loaderCount >= 3)
                    {
                        loaderCount = 0;
                        F.TryTriggerSupplyEffect(asset == build ? SupplyType.Build : SupplyType.Ammo, Vehicle.transform.position);
                        shouldMessagePlayer = true;

                        yield return new WaitForSeconds(1);
                        while (Vehicle.speed is < -1 or > 1)
                            yield return new WaitForSeconds(1);
                    }
                }
            }
        }

        _isResupplied = true;
        _autoSupplyLoop = null;

        if (shouldMessagePlayer && driver is not null && driver.IsOnline && F.IsInMain(driver.Position) && Data?.Item != null)
        {
            Tips.TryGiveTip(driver, 120, T.TipLogisticsVehicleResupplied, Data.Item.Type);
        }
    }
    private IEnumerator<WaitForSeconds> QuotaLoop()
    {
        int tick = 0;

        while (true)
        {
            yield return new WaitForSeconds(3);
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (F.IsInMain(Vehicle.transform.position))
            {
                //var ammoCrate = UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.Barricades.AmmoCrateGUID, 30, Vehicle.transform.position, true).FirstOrDefault();
                if (Vehicle.speed >= -1 && Vehicle.speed <= 1)
                {
                    TryStartAutoLoadSupplies();
                }
            }
            else if (_isResupplied)
            {
                _isResupplied = false;
            }

            tick++;
            if (tick >= 20)
            {
                Quota += 0.5F;
                tick = 0;
            }
        }
    }
    private Vector3 _lastPosInterval;
    private float _totalDistance;
    public float TotalDistanceTravelled => _totalDistance;
    private float _lastCheck;
    private float _lastZoneCheck;
    public ulong LastDriver;
    public float LastDriverTime;
    public float LastDriverDistance;

    [SuppressMessage(Warfare.Data.SuppressCategory, Warfare.Data.SuppressID)]
    [UsedImplicitly]
    private void Update()
    {
        float time = Time.time;
        if (time - _lastCheck > 3f)
        {
            _lastCheck = time;
            if (Vehicle.passengers[0]?.player == null) return;
            Vector3 pos = transform.position;
            if (pos == _lastPosInterval) return;
            float old = _totalDistance;
            _totalDistance += (_lastPosInterval - pos).magnitude;
            QuestManager.OnDistanceUpdated(LastDriver, _totalDistance, _totalDistance - old, this);
            _lastPosInterval = pos;
        }
    }
    private float _timeLastFlare;
    private float _timeLastFlareDrop;
    private int _flareBurst;
    const int StartingFlaresAttackHeli = 30;
    const int StartingFlaresTransportHeli = 50;
    const int StartingFlaresJet = 30;
    public const int FlareBurstCount = 10;
    public const int FlareCooldown = 11;
    public void TryDropFlares()
    {
        if (Time.time - _timeLastFlareDrop < FlareCooldown || _totalFlaresLeft < 0)
            return;

        _flareBurst = FlareBurstCount;
        _timeLastFlareDrop = Time.time;

        byte[] crewseats = Data?.Item == null ? Array.Empty<byte>() : Data.Item.CrewSeats;
        for (byte seat = 0; seat < Vehicle.passengers.Length; seat++)
        {
            if (Vehicle.passengers[seat].player != null && crewseats.ArrayContains(seat) && VehicleBay.Config.CountermeasureEffectID.HasValue)
                EffectManager.sendUIEffect(VehicleBay.Config.CountermeasureEffectID.Value, (short)VehicleBay.Config.CountermeasureEffectID.Value.Id, Vehicle.passengers[seat].player.transportConnection, true);
        }
    }

    [UsedImplicitly]
    private void FixedUpdate()
    {
        if (_totalFlaresLeft > 0 && _flareBurst > 0 && Time.time - _timeLastFlare > 0.2f)
        {
            _timeLastFlare = Time.time;

            InteractableVehicle? countermeasureVehicle = VehicleManager.spawnVehicleV2(VehicleBay.Config.CountermeasureGUID.Value.Id, Vehicle.transform.TransformPoint(0, -4, 0), Vehicle.transform.rotation);

            float sideforce = Random.Range(20, 30);

            if (_flareBurst % 2 == 0) sideforce = -sideforce;

            Rigidbody? rigidbody = countermeasureVehicle.transform.GetComponent<Rigidbody>();
            Vector3 velocity = Vehicle.transform.forward * Vehicle.speed * 0.9f - Vehicle.transform.up * 15 + Vehicle.transform.right * sideforce;
            rigidbody.velocity = velocity;

            var countermeasure = countermeasureVehicle.gameObject.AddComponent<Countermeasure>();

            Countermeasure.ActiveCountermeasures.Add(countermeasure);

            _totalFlaresLeft--;
            _flareBurst--;
            UpdateHUDFlares();
        }
    }
}
public enum SupplyType
{
    Build,
    Ammo
}
