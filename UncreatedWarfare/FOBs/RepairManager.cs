using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.FOBs
{
    public class RepairManager
    {
        private static List<RepairStation> stations = new List<RepairStation>();

        public static void OnBarricadePlaced(BarricadeRegion region, BarricadeData data, ref Transform location)
        {
            if (data.barricade.id == FOBManager.config.Data.RepairStationID)
            {
                RegisterNewRepairStation(data);
            }
        }
        public static void OnBarricadeDestroyed(BarricadeRegion region, BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant, ushort index)
        {
            if (data.barricade.id == FOBManager.config.Data.RepairStationID)
            {
                TryDeleteRepairStation(instanceID);
            }
        }

        public static void LoadRepairStations()
        {
            stations.Clear();
            var barricades = GetRepairStationBarricades();

            foreach (var barricade in barricades)
            {
                var station = new RepairStation(barricade, UCBarricadeManager.GetDropFromBarricadeData(barricade));
                stations.Add(station);
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
        public static void RegisterNewRepairStation(BarricadeData data)
        {
            if (!stations.Exists(r => r.structure.instanceID == data.instanceID))
            {
                var station = new RepairStation(data, UCBarricadeManager.GetDropFromBarricadeData(data));
                station.drop.model.transform.gameObject.AddComponent<RepairStationComponent>().Initialize(station);

                stations.Add(station);

                foreach (var s in stations)
                {
                    F.Log("Repair station: " + s.structure.instanceID);
                    F.Log("Repair station: " + s.drop.instanceID);
                    F.Log("Repair station: " + s.IsActive);
                }
            }
        }

        public static List<BarricadeData> GetRepairStationBarricades()
        {
            List<BarricadeRegion> barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>().ToList();
            List<BarricadeData> barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades).ToList();

            return barricadeDatas.Where(b => b.barricade.id == FOBManager.config.Data.RepairStationID).ToList();
        }
    }

    public class RepairStation
    {
        public BarricadeData structure; // physical barricade structure of the rallypoint
        public BarricadeDrop drop;
        public InteractableStorage storage;
        public Dictionary<uint, int> VehiclesRepairing;
        public bool IsActive;

        public RepairStation(BarricadeData structure, BarricadeDrop drop)
        {
            this.structure = structure;
            this.drop = drop;
            storage = drop.interactable as InteractableStorage;
            VehiclesRepairing = new Dictionary<uint, int>();

            IsActive = true;

            if (storage is null)
            {
                F.LogWarning("REPAIR STATION ERROR: Repair station was not a barricade with storage");
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
    }

    public class RepairStationComponent : MonoBehaviour
    {
        public RepairStation parent;

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
                    if (nearby[i].health >= nearby[i].asset.health)
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
                                parent.RepairVehicle(nearby[i]);
                                ticks--;
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

                            foreach (ItemJar jar in parent.storage.items.items)
                            {
                                if (TeamManager.IsTeam1(parent.structure.group) && jar.item.id == FOBManager.config.Data.Team1BuildID)
                                    build_count++;
                                else if (TeamManager.IsTeam2(parent.structure.group) && jar.item.id == FOBManager.config.Data.Team2BuildID)
                                    build_count++;
                            }

                            if (build_count > 0)
                            {
                                parent.VehiclesRepairing.Add(nearby[i].instanceID, 9);
                                parent.RepairVehicle(nearby[i]);

                                if (TeamManager.IsTeam1(parent.structure.group))
                                    UCBarricadeManager.RemoveSingleItemFromStorage(parent.storage, FOBManager.config.Data.Team1BuildID);
                                else if (TeamManager.IsTeam2(parent.structure.group))
                                    UCBarricadeManager.RemoveSingleItemFromStorage(parent.storage, FOBManager.config.Data.Team2BuildID);
                            }
                        }
                    }
                }

                yield return new WaitForSeconds(3);
            }
        }
    }
}
