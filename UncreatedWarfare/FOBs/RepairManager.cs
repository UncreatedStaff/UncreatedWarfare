using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Levels;
using UnityEngine;

namespace Uncreated.Warfare.FOBs;

public static class RepairManager
{
    private static readonly List<RepairStation> stations = new List<RepairStation>();

    public static void OnBarricadePlaced(BarricadeDrop drop, BarricadeRegion region)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        BarricadeData data = drop.GetServersideData();

        if (Gamemode.Config.BarricadeRepairStation.MatchGuid(data.barricade.asset.GUID))
        {
            RegisterNewRepairStation(data, drop);
        }
    }
    public static void OnBarricadeDestroyed(BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Gamemode.Config.BarricadeRepairStation.MatchGuid(data.barricade.asset.GUID))
        {
            TryDeleteRepairStation(instanceID);
        }
    }

    public static void LoadRepairStations()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        stations.Clear();
        foreach (BarricadeDrop barricade in UCBarricadeManager.NonPlantedBarricades)
        {
            if (Gamemode.Config.BarricadeRepairStation.MatchGuid(barricade.asset.GUID) && !barricade.model.TryGetComponent(out RepairStationComponent _))
            {
                RepairStation station = new RepairStation(barricade.GetServersideData(), barricade);
                stations.Add(station);
                barricade.model.gameObject.AddComponent<RepairStationComponent>().Initialize(station);
            }
        }
    }
    public static void TryDeleteRepairStation(uint instanceID)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < stations.Count; i++)
        {
            if (stations[i].structure.instanceID == instanceID)
            {
                stations[i].IsActive = false;

                stations.RemoveAt(i);
                return;
            }
        }
    }
    public static void RegisterNewRepairStation(BarricadeData data, BarricadeDrop drop)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!stations.Exists(r => r.structure.instanceID == data.instanceID))
        {
            RepairStation station = new RepairStation(data, drop);
            station.drop.model.transform.gameObject.AddComponent<RepairStationComponent>().Initialize(station);

            stations.Add(station);
            if (UCWarfare.Config.Debug)
                foreach (RepairStation s in stations)
                    L.Log($"Repair station: Active: {s.IsActive}, Structure: {s.structure.instanceID}, Drop: {s.drop.instanceID}.", ConsoleColor.DarkGray);
        }
    }
}

public class RepairStation
{
    public SDG.Unturned.BarricadeData structure; // physical barricade structure of the rallypoint
    public BarricadeDrop drop;
    public InteractableStorage? storage;
    public Dictionary<uint, int> VehiclesRepairing;
    public bool IsActive;

    public RepairStation(SDG.Unturned.BarricadeData structure, BarricadeDrop drop)
    {
        this.structure = structure;
        this.drop = drop;
        storage = drop.interactable as InteractableStorage;
        VehiclesRepairing = new Dictionary<uint, int>();

        IsActive = true;
    }
    public void RepairVehicle(InteractableVehicle vehicle)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (vehicle.health >= vehicle.asset.health)
            return;

        ushort amount = 25;
        ushort newHealth = (ushort)(vehicle.health + amount);
        if (vehicle.health + amount >= vehicle.asset.health)
        {
            newHealth = vehicle.asset.health;
            if (vehicle.transform.TryGetComponent(out VehicleComponent c))
            {
                c.DamageTable.Clear();
            }
        }

        VehicleManager.sendVehicleHealth(vehicle, newHealth);
        if (Gamemode.Config.EffectRepair.ValidReference(out EffectAsset effect))
            F.TriggerEffectReliable(effect, EffectManager.SMALL, vehicle.transform.position);
        vehicle.updateVehicle();
    }

    public void RefuelVehicle(InteractableVehicle vehicle)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (vehicle.fuel >= vehicle.asset.fuel)
            return;

        ushort amount = 180;

        vehicle.askFillFuel(amount);

        if (Gamemode.Config.EffectRefuel.ValidReference(out EffectAsset effect))
            F.TriggerEffectReliable(effect, EffectManager.SMALL, vehicle.transform.position);
        vehicle.updateVehicle();
    }
}

public class RepairStationComponent : MonoBehaviour
{
    public RepairStation parent;
    int counter = 0;

    public void Initialize(RepairStation repairStation)
    {
        parent = repairStation;
        StartCoroutine(RepairStationLoop());
    }
    private IEnumerator<WaitForSeconds> RepairStationLoop()
    {
        while (parent.IsActive)
        {
#if DEBUG
            IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            List<InteractableVehicle> nearby = new List<InteractableVehicle>();
            VehicleManager.getVehiclesInRadius(parent.structure.point, (float)Math.Pow(25, 2), nearby);

            for (int i = 0; i < nearby.Count; i++)
            {
                if (nearby[i].lockedGroup.m_SteamID != parent.drop.GetServersideData().group)
                    continue;

                if (!(nearby[i].asset.engine == EEngine.PLANE || nearby[i].asset.engine == EEngine.HELICOPTER) && (parent.structure.point - nearby[i].transform.position).sqrMagnitude > Math.Pow(12, 2))
                    continue;

                if (nearby[i].health >= nearby[i].asset.health && nearby[i].fuel >= nearby[i].asset.fuel)
                {
                    if (parent.VehiclesRepairing.ContainsKey(nearby[i].instanceID))
                        parent.VehiclesRepairing.Remove(nearby[i].instanceID);
                }
                else
                {
                    if (parent.VehiclesRepairing.ContainsKey(nearby[i].instanceID))
                    {
                        int ticks = parent.VehiclesRepairing[nearby[i].instanceID];

                        if (ticks > 0)
                        {
                            if (nearby[i].health < nearby[i].asset.health)
                            {
                                parent.RepairVehicle(nearby[i]);
                                ticks--;
                            }
                            else if (counter == 0 && !nearby[i].isEngineOn)
                            {
                                parent.RefuelVehicle(nearby[i]);
                                ticks--;
                            }
                        }
                        if (ticks <= 0)
                        {
                            parent.VehiclesRepairing.Remove(nearby[i].instanceID);
                        }
                        else
                            parent.VehiclesRepairing[nearby[i].instanceID] = ticks;
                    }
                    else if (parent.structure.group == nearby[i].lockedGroup.m_SteamID)
                    {
                        FOB? fob = FOB.GetNearestFOB(parent.structure.point, EFobRadius.FULL_WITH_BUNKER_CHECK, parent.structure.group);
                        if (F.IsInMain(parent.structure.point) || (fob != null && fob.Build > 0))
                        {
                            parent.VehiclesRepairing.Add(nearby[i].instanceID, 9);
                            parent.RepairVehicle(nearby[i]);

                            if (fob != null)
                            {
                                fob.ReduceBuild(1);

                                UCPlayer? stationPlacer = UCPlayer.FromID(parent.structure.owner);
                                if (stationPlacer != null)
                                {
                                    if (stationPlacer.CSteamID != nearby[i].lockedOwner)
                                        Points.AwardXP(stationPlacer, XPReward.RepairVehicle);

                                    if (!(stationPlacer.Steam64 == fob.Creator || stationPlacer.Steam64 == fob.Placer))
                                        Points.TryAwardFOBCreatorXP(fob, XPReward.RepairVehicle, 0.5f);
                                }
                            }
                        }
                    }
                }
            }

            counter++;

            if (counter >= 3)
                counter = 0;
#if DEBUG
            profiler.Dispose();
#endif
            yield return new WaitForSeconds(1.5F);
        }
    }
}