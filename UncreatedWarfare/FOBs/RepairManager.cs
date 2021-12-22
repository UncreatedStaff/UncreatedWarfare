using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.FOBs
{
    public static class RepairManager
    {
        private static readonly List<RepairStation> stations = new List<RepairStation>();

        public static void OnBarricadePlaced(BarricadeDrop drop, BarricadeRegion region)
        {
            SDG.Unturned.BarricadeData data = drop.GetServersideData();

            if (data.barricade.asset.GUID == Gamemode.Config.Barricades.RepairStationGUID)
            {
                RegisterNewRepairStation(data, drop);
            }
        }
        public static void OnBarricadeDestroyed(SDG.Unturned.BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant)
        {
            if (data.barricade.asset.GUID == Gamemode.Config.Barricades.RepairStationGUID)
            {
                TryDeleteRepairStation(instanceID);
            }
        }

        public static void LoadRepairStations()
        {
            stations.Clear();
            List<RBarricade> barricades = GetRepairStationBarricades();

            foreach (RBarricade barricade in barricades)
            {
                RepairStation station = new RepairStation(barricade.data, UCBarricadeManager.GetDropFromBarricadeData(barricade.data));
                stations.Add(station);
                barricade.drop.model.gameObject.AddComponent<RepairStationComponent>().Initialize(station);
            }
        }
        public static void TryDeleteRepairStation(uint instanceID)
        {
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
        public static void RegisterNewRepairStation(SDG.Unturned.BarricadeData data, BarricadeDrop drop)
        {
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

        public static List<RBarricade> GetRepairStationBarricades()
        {
            List<RBarricade> barricades = new List<RBarricade>();
            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    if (region == default) continue;
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        if (region.drops[i].asset.GUID == Gamemode.Config.Barricades.RepairStationGUID)
                        {
                            barricades.Add(new RBarricade(region.drops[i].GetServersideData(), region.drops[i]));
                        }
                    }
                }
            }
            return barricades;
        }
        public struct RBarricade
        {
            public SDG.Unturned.BarricadeData data;
            public BarricadeDrop drop;
            public RBarricade(SDG.Unturned.BarricadeData data, BarricadeDrop drop)
            {
                this.data = data;
                this.drop = drop;
            }
        }
    }

    public class RepairStation
    {
        public SDG.Unturned.BarricadeData structure; // physical barricade structure of the rallypoint
        public BarricadeDrop drop;
        public InteractableStorage storage;
        public Dictionary<uint, int> VehiclesRepairing;
        public bool IsActive;

        public RepairStation(SDG.Unturned.BarricadeData structure, BarricadeDrop drop)
        {
            this.structure = structure;
            this.drop = drop;
            storage = drop.interactable as InteractableStorage;
            VehiclesRepairing = new Dictionary<uint, int>();

            IsActive = true;

            if (storage is null)
            {
                L.LogWarning("REPAIR STATION ERROR: Repair station was not a barricade with storage");
                IsActive = false;
            }
        }
        public void RepairVehicle(InteractableVehicle vehicle)
        {
            if (vehicle.health >= vehicle.asset.health)
                return;

            ushort amount = 25;
            ushort newHealth = (ushort)(vehicle.health + amount);
            if (vehicle.health + amount >= vehicle.asset.health)
            {
                newHealth = vehicle.asset.health;
            }

            VehicleManager.sendVehicleHealth(vehicle, newHealth);
            EffectManager.sendEffect(27, EffectManager.SMALL, vehicle.transform.position);
            vehicle.updateVehicle();
        }

        public void RefuelVehicle(InteractableVehicle vehicle)
        {
            if (vehicle.fuel >= vehicle.asset.fuel)
                return;

            ushort amount = 180;

            vehicle.askFillFuel(amount);

            EffectManager.sendEffect(38316, EffectManager.SMALL, vehicle.transform.position);
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
            StartCoroutine(RepaitStationLoop());
        }
        private IEnumerator<WaitForSeconds> RepaitStationLoop()
        {
            while (parent.IsActive)
            {
                List<InteractableVehicle> nearby = new List<InteractableVehicle>();
                VehicleManager.getVehiclesInRadius(parent.structure.point, (float)Math.Pow(20, 2), nearby);

                for (int i = 0; i < nearby.Count; i++)
                {
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
                            int build_count = 0;

                            ulong team = parent.structure.group.GetTeam();
                            foreach (ItemJar jar in parent.storage.items.items)
                            {
                                if (!(Assets.find(EAssetType.ITEM, jar.item.id) is ItemAsset asset)) continue;
                                if (team == 1 && asset.GUID == Gamemode.Config.Items.T1Build)
                                {
                                    build_count++;
                                    break;
                                }
                                else if (team == 2 && asset.GUID == Gamemode.Config.Items.T2Build)
                                {
                                    build_count++;
                                    break;
                                }
                            }

                            if (build_count > 0)
                            {
                                parent.VehiclesRepairing.Add(nearby[i].instanceID, 9);
                                parent.RepairVehicle(nearby[i]);

                                if (team == 1)
                                    UCBarricadeManager.RemoveSingleItemFromStorage(parent.storage, Gamemode.Config.Items.T1Build);
                                else if (team == 2)
                                    UCBarricadeManager.RemoveSingleItemFromStorage(parent.storage, Gamemode.Config.Items.T2Build);
                            }
                        }
                    }
                }

                counter++;

                if (counter >= 3)
                    counter = 0;

                yield return new WaitForSeconds(1.5F);
            }
        }
    }
}
