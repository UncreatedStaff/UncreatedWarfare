using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Vehicles;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Uncreated.Warfare.Components;

public class VehicleComponent : MonoBehaviour
{
    public Guid LastItem;
    public bool LastItemIsVehicle;
    public InteractableVehicle Vehicle;
    public ulong Team { get => Vehicle.lockedGroup.m_SteamID; }
    public VehicleData Data;
    public bool isInVehiclebay { get; private set; }
    public EDamageOrigin LastDamageOrigin;
    public ulong LastInstigator;
    public Dictionary<ulong, Vector3> TransportTable { get; private set; }
    public Dictionary<ulong, double> UsageTable { get; private set; }
    private Dictionary<ulong, DateTime> TimeEnteredTable;
    private Dictionary<ulong, DateTime> TimeRewardedTable;
    public Dictionary<ulong, KeyValuePair<ushort, DateTime>> DamageTable;
    private float _quota;
    private float _requiredQuota;
    public float Quota { get => _quota; set => _quota = value; }
    public float RequiredQuota { get => _requiredQuota; set => _requiredQuota = value; }
    private bool IsResupplied;
    private Coroutine? quotaLoop;
    private Coroutine? autoSupplyLoop;
    public Coroutine? forceSupplyLoop { get; private set; }
    public bool IsArmor
    {
        get
        {
            if (Data is null) return false;

            return Data.Type == EVehicleType.APC ||
                Data.Type == EVehicleType.IFV ||
                Data.Type == EVehicleType.MBT;
        }
    }
    public bool IsLightVehlice
    {
        get
        {
            if (Data is null) return false;

            return Data.Type == EVehicleType.LOGISTICS ||
                Data.Type == EVehicleType.TRANSPORT ||
                Data.Type == EVehicleType.HUMVEE ||
                Data.Type == EVehicleType.SCOUT_CAR;
        }
    }
    public bool IsAircraft
    {
        get
        {
            if (Data is null) return false;

            return Data.Type == EVehicleType.HELI_TRANSPORT ||
                Data.Type == EVehicleType.HELI_ATTACK ||
                Data.Type == EVehicleType.JET;
        }
    }
    public bool IsEmplacement
    {
        get
        {
            if (Data is null) return false;

            return Data.Type == EVehicleType.EMPLACEMENT;
        }
    }
    public void Initialize(InteractableVehicle vehicle)
    {
        Vehicle = vehicle;
        TransportTable = new Dictionary<ulong, Vector3>();
        UsageTable = new Dictionary<ulong, double>();
        TimeEnteredTable = new Dictionary<ulong, DateTime>();
        TimeRewardedTable = new Dictionary<ulong, DateTime>();
        DamageTable = new Dictionary<ulong, KeyValuePair<ushort, DateTime>>();
        IsResupplied = true;

        _quota = 0;
        _requiredQuota = -1;

        if (VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData data))
        {
            Data = data;
            isInVehiclebay = true;
        }
        lastPos = this.transform.position;

        countermeasures = new List<Transform>();

        if (IsArmor) vehicle.transform.gameObject.AddComponent<SpottedComponent>().Initialize(SpottedComponent.ESpotted.ARMOR);
        else if (IsLightVehlice) vehicle.transform.gameObject.AddComponent<SpottedComponent>().Initialize(SpottedComponent.ESpotted.LIGHT_VEHICLE);
        else if (IsAircraft) vehicle.transform.gameObject.AddComponent<SpottedComponent>().Initialize(SpottedComponent.ESpotted.AIRCRAFT);
        else if (IsEmplacement) vehicle.transform.gameObject.AddComponent<SpottedComponent>().Initialize(SpottedComponent.ESpotted.EMPLACEMENT);
    }
    private void OnDestroy()
    {
        RemoveCountermeasures();
    }
    internal void OnPlayerEnteredVehicle(EnterVehicle e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer player = e.Player;
        InteractableVehicle vehicle = e.Vehicle;
        
        byte toSeat = e.PassengerIndex;
        if (toSeat == 0)
        {
            LastDriver = player.Steam64;
            LastDriverTime = Time.realtimeSinceStartup;
            totalDistance = 0;
        }
        ActionLogger.Add(EActionLogType.ENTER_VEHICLE_SEAT, $"{vehicle.asset.vehicleName} / {vehicle.asset.id} / {vehicle.asset.GUID:N}, Owner: {vehicle.lockedOwner.m_SteamID}, " +
                                                         $"ID: ({vehicle.instanceID}) Seat move: >> " +
                                                         $"{toSeat.ToString(Warfare.Data.Locale)}", player.Steam64);

        if (VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData data))
        {
            bool isCrewSeat = data.CrewSeats.Contains(toSeat);

            if (!isCrewSeat)
            {
                if (!TransportTable.ContainsKey(player.Steam64))
                    TransportTable.Add(player.Steam64, player.Position);
                else
                    TransportTable[player.Steam64] = player.Position;
            }

            if (!TimeEnteredTable.ContainsKey(player.Steam64))
                TimeEnteredTable.Add(player.Steam64, DateTime.Now);
            else
                TimeEnteredTable[player.Steam64] = DateTime.Now;

            if (quotaLoop is null)
            {
                _requiredQuota = data.TicketCost * 0.5F;
                quotaLoop = StartCoroutine(QuotaLoop());
            }
        }
    }
    public void OnPlayerExitedVehicle(ExitVehicle e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (isInVehiclebay)
            EvaluateUsage(e.Player.Player.channel.owner);

        if (e.Player.KitClass == EClass.SQUADLEADER &&
            (Data.Type == EVehicleType.LOGISTICS || Data.Type == EVehicleType.HELI_TRANSPORT) &&
            !F.IsInMain(e.Player.Position) &&
            FOB.GetNearestFOB(e.Player.Position, EFOBRadius.FULL_WITH_BUNKER_CHECK, e.Player.GetTeam()) == null
            )
        {
            Tips.TryGiveTip(e.Player, ETip.PLACE_RADIO);
        }

        if (e.Vehicle.passengers[0] == null || e.Vehicle.passengers[0].player == null ||
            e.Vehicle.passengers[0].player.player.channel.owner.playerID.steamID.m_SteamID == e.Player.Steam64)
        {
            if (LastDriver == e.Player.Steam64)
                LastDriverDistance = totalDistance;
            return;
        }
        if (TransportTable.TryGetValue(e.Player.Steam64, out Vector3 original))
        {
            float distance = (e.Player.Position - original).magnitude;
            if (distance >= 200 && !(e.Player.KitClass == EClass.CREWMAN || e.Player.KitClass == EClass.PILOT))
            {
                bool isOnCooldown = false;
                if (TimeRewardedTable.TryGetValue(e.Player.Steam64, out DateTime time) && (DateTime.Now - time).TotalSeconds < 60)
                    isOnCooldown = true;

                if (!isOnCooldown)
                {
                    int amount = (int)(Math.Floor(distance / 100) * 2) + 5;

                    Player player = e.Vehicle.passengers[0].player.player;
                    Points.AwardXP(player, amount, T.XPToastTransportingPlayers.Translate(player.channel.owner.playerID.steamID.m_SteamID));

                    _quota += 0.5F;

                    if (!TimeRewardedTable.ContainsKey(e.Player.Steam64))
                        TimeRewardedTable.Add(e.Player.Steam64, DateTime.Now);
                    else
                        TimeRewardedTable[e.Player.Steam64] = DateTime.Now;
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
            totalDistance = 0;
        }

        if (isInVehiclebay)
        {
            EvaluateUsage(e.Player.SteamPlayer);

            if (!Data.CrewSeats.Contains(e.NewSeat))
            {
                if (!TransportTable.ContainsKey(e.Player.Steam64))
                    TransportTable.Add(e.Player.Steam64, e.Player.Position);
            }
            else
                TransportTable.Remove(e.Player.Steam64);
        }
    }
    public void EvaluateUsage(SteamPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        byte currentSeat = player.player.movement.getSeat();
        bool isCrewSeat = Data.CrewSeats.Contains(currentSeat);

        if (currentSeat == 0 || isCrewSeat)
        {
            ulong Steam64 = player.playerID.steamID.m_SteamID;

            if (TimeEnteredTable.TryGetValue(Steam64, out DateTime start))
            {
                double time = (DateTime.Now - start).TotalSeconds;
                if (!UsageTable.ContainsKey(Steam64))
                    UsageTable.Add(Steam64, time);
                else
                    UsageTable[Steam64] += time;
            }
        }
    }
    Coroutine? countermeasureRoutine;
    List<Transform> countermeasures;
    public void TrySpawnCountermeasures()
    {
        if (countermeasureRoutine != null)
            return;

        StartCoroutine(DropCountermeasures());

        for (byte seat = 0; seat < Vehicle.passengers.Length; seat++)
        {
            if (Vehicle.passengers[seat].player != null && Data.CrewSeats.Contains(seat))
                EffectManager.sendUIEffect(VehicleBay.Config.CountermeasureEffectID, (short)VehicleBay.Config.CountermeasureEffectID.Id, Vehicle.passengers[seat].player.transportConnection, true);
        }

        countermeasureRoutine = StartCoroutine(ReloadCountermeasures());
    }
    private IEnumerator<WaitForSeconds> DropCountermeasures()
    {
        if (Assets.find(VehicleBay.Config.CountermeasureGUID) is VehicleAsset countermeasureAsset)
        {
            int flareCount = 5;
            float angle = 0;
            for (int i = 0; i < flareCount; i++)
            {
                angle += i * (360 / flareCount) + Random.Range(-35f, 35f);

                InteractableVehicle? countermeasure = VehicleManager.spawnVehicleV2(countermeasureAsset.id, Vehicle.transform.TransformPoint(0, -4, 0), Vehicle.transform.rotation);

                countermeasure.transform.Rotate(Vector3.up, angle, Space.Self);

                Rigidbody? rigidbody = countermeasure.transform.GetComponent<Rigidbody>();
                rigidbody.velocity = Vehicle.transform.GetComponent<Rigidbody>().velocity;
                rigidbody.AddForce(countermeasure.transform.forward * 5, ForceMode.Impulse);

                countermeasures.Add(countermeasure.transform);
                HeatSeakingMissileComponent.ActiveCountermeasures.Add(countermeasure.transform);

                yield return new WaitForSeconds(0.25f);
            }
        }
        else
            L.LogDebug("     ERROR: Countermeasure asset not found");
    }
    private IEnumerator<WaitForSeconds> ReloadCountermeasures()
    {
        yield return new WaitForSeconds(15);

        RemoveCountermeasures();

        countermeasureRoutine = null;
    }
    private void RemoveCountermeasures()
    {
        foreach (Transform countermeasure in countermeasures)
        {
            HeatSeakingMissileComponent.ActiveCountermeasures.RemoveAll(t => t.GetInstanceID() == countermeasure.GetInstanceID());

            if (countermeasure.TryGetComponent(out InteractableVehicle vehicle))
            {
                VehicleManager.askVehicleDestroy(vehicle);
            }
        }
        countermeasures.Clear();
    }
    public void StartForceLoadSupplies(UCPlayer caller, ESupplyType type, int amount)
    {
        forceSupplyLoop = StartCoroutine(ForceSupplyLoopCoroutine(caller, type, amount));
    }
    public bool TryStartAutoLoadSupplies()
    {
        if (IsResupplied || autoSupplyLoop != null || Data.Metadata is null || Data.Metadata.TrunkItems is null || Vehicle.trunkItems is null)
            return false;

        autoSupplyLoop = StartCoroutine(AutoSupplyLoop());
        return true;

    }
    private IEnumerator<WaitForSeconds> ForceSupplyLoopCoroutine(UCPlayer caller, ESupplyType type, int amount)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ItemAsset? supplyAsset;

        if (type is ESupplyType.BUILD)
            TeamManager.GetFaction(Team).Build.ValidReference(out supplyAsset);
        else if (type is ESupplyType.AMMO)
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
        int existingCount = 0;
        int addedBackCount = 0;
        int addedNewCount = 0;
        int loaderBreak = 0;

        List<ItemJar> oldTrunkItems = new List<ItemJar>();
        for (int i = Vehicle.trunkItems.items.Count - 1; i >= 0; i--)
        {
            if (Vehicle.trunkItems.items[i].item.id == supplyAsset.id)
                existingCount++;

            oldTrunkItems.Add(Vehicle.trunkItems.items[i]);
            Vehicle.trunkItems.removeItem(Vehicle.trunkItems.getIndex(Vehicle.trunkItems.items[i].x, Vehicle.trunkItems.items[i].y));
            
        }

        bool shouldAddMoreItems = true;
        foreach (ItemJar item in oldTrunkItems)
        {
            if (item.item.id == supplyAsset.id)
            {
                Item newItem = new Item(item.item.id, true) { metadata = item.item.metadata };
                if (Vehicle.trunkItems.tryAddItem(newItem))
                {
                    addedBackCount++;
                    loaderBreak++;
                    if (loaderBreak >= 3)
                    {
                        loaderBreak = 0;
                        if (type is ESupplyType.BUILD)
                            EffectManager.sendEffect(25997, EffectManager.MEDIUM, Vehicle.transform.position);
                        else
                            EffectManager.sendEffect(25998, EffectManager.MEDIUM, Vehicle.transform.position);
                        yield return new WaitForSeconds(1);

                        while (!(Vehicle.speed >= -1 && Vehicle.speed <= 1))
                            yield return new WaitForSeconds(1);
                    }
                    if (addedBackCount >= amount)
                    {
                        shouldAddMoreItems = false;
                        break;
                    }
                }
            }
        }

        if (shouldAddMoreItems)
        {
            for (int i = 0; i < amount - existingCount; i++)
            {
                if (Vehicle.trunkItems.tryAddItem(new Item(supplyAsset.id, true)))
                {
                    addedNewCount++;
                    loaderBreak++;
                    if (loaderBreak >= 3)
                    {
                        loaderBreak = 0;
                        if (type is ESupplyType.BUILD)
                            EffectManager.sendEffect(25997, EffectManager.MEDIUM, Vehicle.transform.position);
                        else
                            EffectManager.sendEffect(25998, EffectManager.MEDIUM, Vehicle.transform.position);
                        yield return new WaitForSeconds(1);

                        while (!(Vehicle.speed >= -1 && Vehicle.speed <= 1))
                            yield return new WaitForSeconds(1);
                    }
                }
            }
        }
        foreach (ItemJar item in oldTrunkItems)
        {
            if (item.item.id != supplyAsset.id)
            {
                Vehicle.trunkItems.tryAddItem(item.item);
            }
        }

        caller.SendChat(type is ESupplyType.BUILD ? T.LoadCompleteBuild : T.LoadCompleteAmmo, addedBackCount + addedNewCount);

        forceSupplyLoop = null;
    }
    private IEnumerator<WaitForSeconds> AutoSupplyLoop()
    {
        TeamManager.GetFaction(Team).Build.ValidReference(out ItemAsset? build);
        TeamManager.GetFaction(Team).Ammo.ValidReference(out ItemAsset? ammo);

        UCPlayer? driver = UCPlayer.FromID(LastDriver);

        int loaderCount = 0;

        bool shouldMessagePlayer = false;

        if (Data.Metadata != null && Data.Metadata.TrunkItems != null)
        {
            List<KitItem> trunk = Data.Metadata.TrunkItems;
            for (int i = 0; i < trunk.Count; i++)
            {
                ItemAsset? asset;
                if (build is not null && trunk[i].id == build.GUID) asset = build;
                else if (ammo is not null && trunk[i].id == ammo.GUID) asset = ammo;
                else asset = Assets.find(trunk[i].id) as ItemAsset;

                if (asset is not null && Vehicle.trunkItems.checkSpaceEmpty(trunk[i].x, trunk[i].y, asset.size_x,
                        asset.size_y, trunk[i].rotation))
                {
                    Item item = new Item(asset.id, trunk[i].amount, 100, F.CloneBytes(trunk[i].metadata));
                    Vehicle.trunkItems.addItem(trunk[i].x, trunk[i].y, trunk[i].rotation, item);
                    loaderCount++;

                    if (loaderCount >= 3)
                    {
                        loaderCount = 0;
                        if (asset == build)
                            EffectManager.sendEffect(25997, EffectManager.MEDIUM, Vehicle.transform.position);
                        else
                            EffectManager.sendEffect(25998, EffectManager.MEDIUM, Vehicle.transform.position);

                        shouldMessagePlayer = true;

                        yield return new WaitForSeconds(1);
                        while (!(Vehicle.speed >= -1 && Vehicle.speed <= 1))
                            yield return new WaitForSeconds(1);
                    }
                }
            }
        }

        IsResupplied = true;
        autoSupplyLoop = null;

        if (shouldMessagePlayer && driver is not null && driver.IsOnline && F.IsInMain(driver.Position))
        {
            Tips.TryGiveTip(driver, ETip.LOGI_RESUPPLIED, Vehicle.asset.vehicleName);
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
            else if (IsResupplied)
            {
                IsResupplied = false;
            }

            tick++;
            if (tick >= 20)
            {
                _quota += 0.5F;
                tick = 0;
            }
        }
    }
    private Vector3 lastPos;
    private float totalDistance;
    public float TotalDistanceTravelled => totalDistance;
    private float lastCheck;
    public ulong LastDriver;
    public float LastDriverTime;
    public float LastDriverDistance;
    private void Update()
    {
        if (Time.time - lastCheck > 3f)
        {
            lastCheck = Time.time;
            if (Vehicle.passengers[0] == null || Vehicle.passengers[0].player == null) return;
            Vector3 pos = this.transform.position;
            if (pos == lastPos) return;
            float old = totalDistance;
            totalDistance += (lastPos - pos).magnitude;
            QuestManager.OnDistanceUpdated(LastDriver, totalDistance, totalDistance - old, this);
            lastPos = pos;
        }
    }
}
public enum ESupplyType
{
    BUILD,
    AMMO
}
