using Newtonsoft.Json;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Uncreated.Warfare.Vehicles
{
    public class VehicleSpawner : JSONSaver<VehicleSpawn>
    {
        public VehicleSpawner()
            : base(Data.VehicleStorage + "vehiclespawns.json")
        {
            VehicleManager.OnVehicleExploded += OnVehicleExploded;
            Patches.BarricadeDestroyedHandler += OnBarricadeDestroyed;
            Level.onLevelLoaded += OnLevelLoaded;
        }
        protected override string LoadDefaults() => "[]";
        private void OnLevelLoaded(int level)
        {
            LoadSpawns();
            RespawnAllVehicles();
        }
        private void LoadSpawns()
        {
            F.Log("Loading vehicle spawns...", ConsoleColor.DarkCyan);
            foreach (var spawn in Spawns)
            {
                spawn.Initialize();
            }
        }
        private void OnVehicleExploded(InteractableVehicle vehicle)
        {
            if (HasLinkedSpawn(vehicle.instanceID, out var spawn) && spawn.IsActive)
            {
                spawn.StartVehicleRespawnTimer();
            }
        }
        private void OnBarricadeDestroyed(BarricadeRegion region, BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant, ushort index)
        {
            if (data.barricade.id == UCWarfare.Config.VehicleBaySettings.VehicleSpawnerID)
            {
                if (IsRegistered(data.instanceID, out _))
                {
                    F.Log("Vehicle spawn was deregistered because it was salvaged or destroyed.");
                    DeleteSpawn(data.instanceID);
                }
            }
        }
        public static void RespawnAllVehicles()
        {
            F.Log("Respawning vehicles...", ConsoleColor.DarkCyan);
            foreach (var spawn in Spawns)
            {
                if (spawn.HasLinkedVehicle(out var vehicle))
                {
                    VehicleBay.DeleteVehicle(vehicle);
                }
                spawn.SpawnVehicle();
            }
        }
        public static void CreateSpawn(uint barricadeInstanceID, ushort vehicleID)
        {
            VehicleSpawn spawn = new VehicleSpawn(barricadeInstanceID, vehicleID);
            spawn.Initialize();
            AddObjectToSave(spawn);
            Structures.StructureSaver.AddStructure(spawn.BarricadeDrop.model.transform, out _, out _);
            spawn.SpawnVehicle();
        }
        public static void DeleteSpawn(uint barricadeInstanceID)
        {
            var spawn = GetObject(s => s.BarricadeInstanceID == barricadeInstanceID);
            if (spawn != null)
            {
                spawn.IsActive = false;
            }
            RemoveWhere(s => s.BarricadeInstanceID == barricadeInstanceID);
            Structures.StructureSaver.RemoveWhere(x => x.transform.instanceID == barricadeInstanceID);
        }
        public static List<VehicleSpawn> Spawns
        {
            get { return GetExistingObjects(); }
        }
        public static bool IsRegistered(uint barricadeInstanceID, out VehicleSpawn spawn) => 
            ObjectExists(s => s.BarricadeInstanceID == barricadeInstanceID, out spawn);
        public static bool UnusedSpawnExists(ushort vehicleID, out VehicleSpawn spawn) =>
            ObjectExists(s => {
                if (s.VehicleID == vehicleID && s.VehicleInstanceID != 0)
                {
                    var vehicle = VehicleManager.getVehicle(s.VehicleInstanceID);
                    return vehicle != null && !vehicle.isDead && !vehicle.isDrowned;
                }
                return false;
            }, out spawn);
        public static bool HasLinkedSpawn(uint vehicleInstanceID, out VehicleSpawn spawn) =>
            ObjectExists(s => s.VehicleInstanceID == vehicleInstanceID, out spawn);
    }
    public class VehicleSpawn
    {
        public uint BarricadeInstanceID;
        public ushort VehicleID;
        [JsonIgnore]
        public uint VehicleInstanceID { get; private set; }
        [JsonIgnore]
        public BarricadeDrop BarricadeDrop { get; private set; }
        [JsonIgnore]
        public BarricadeData BarricadeData { get; private set; }
        [JsonIgnore]
        public bool IsActive;
        public VehicleSpawn(uint baricadeInstanceID, ushort vehicleID)
        {
            BarricadeInstanceID = baricadeInstanceID;
            VehicleID = vehicleID;
            VehicleInstanceID = 0;
            IsActive = true;
        }
        public void Initialize()
        {
            List<BarricadeRegion> barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>().ToList();
            List<BarricadeData> barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades).ToList();
            List<BarricadeDrop> barricadeDrops = barricadeRegions.SelectMany(brd => brd.drops).ToList();

            BarricadeData= barricadeDatas.Where(b => BarricadeInstanceID == b.instanceID).FirstOrDefault();
            BarricadeDrop = barricadeDrops.Where(b => BarricadeInstanceID == b.instanceID).FirstOrDefault();
            if (BarricadeData is null)
            {
                F.Log("VEHICLE SPAWNER ERROR: corresponding BarricadeDrop could not be found");
                return;
            }
            if (BarricadeDrop is null)
            {
                F.Log("VEHICLE SPAWNER ERROR: corresponding BarricadeData could not be found");
                return;
            }

            BarricadeDrop.model.transform.gameObject.AddComponent<VehicleSpawnComponent>().Initialize(this);

            IsActive = true;
        }
        public void SpawnVehicle()
        {
            var rotation = new Quaternion();
            rotation.eulerAngles = new Vector3(BarricadeData.angle_x + 90, BarricadeData.angle_y, BarricadeData.angle_z);
            VehicleBay.SpawnLockedVehicle(VehicleID, new Vector3(BarricadeData.point.x, BarricadeData.point.y + 5, BarricadeData.point.z), rotation, out var instanceID);
            LinkNewVehicle(instanceID);
            F.Log($"VEHICLE SPAWNER: spawned {UCAssetManager.FindVehicleAsset(VehicleID).vehicleName} - {VehicleID} at spawn {BarricadeData.point}");
        }
        public bool HasLinkedVehicle(out InteractableVehicle vehicle)
        {
            if (VehicleInstanceID == 0)
            {
                vehicle = null;
                return false;
            }
            vehicle = VehicleManager.getVehicle(VehicleInstanceID);
            return vehicle != null && !vehicle.isDead && !vehicle.isDrowned;
        }
        public void LinkNewVehicle(uint vehicleInstanceID)
        {
            VehicleInstanceID = vehicleInstanceID;
        }
        public void Unlink()
        {
            VehicleInstanceID = 0;
        }
        public void StartVehicleRespawnTimer()
        {
            if (BarricadeDrop is null)
            {
                F.Log($"VEHICLE SPAWNER ERROR: could not start respawn timer, BarricadeDrop was null");
                return;
            }

            if (BarricadeDrop.model.transform.TryGetComponent<VehicleSpawnComponent>(out var component))
            {
                component.StartRespawnVehicleTimer();
            }
            else
            {
                F.Log($"VEHICLE SPAWNER ERROR: could not start respawn timer, unable to get MonoBehavior component from drop");
            }
        }
    }
    public class VehicleSpawnComponent : MonoBehaviour
    {
        VehicleSpawn parent;

        public void Initialize(VehicleSpawn parent)
        {
            this.parent = parent;
            F.Log("VEHICLE SPAWNER: VehicleSpawn script has been initialized.");
        }
        public void StartRespawnVehicleTimer()
        {
            StartCoroutine(RespawnVehicleTimer());
        }
        private IEnumerator<WaitForSeconds> RespawnVehicleTimer()
        {
            if (VehicleBay.VehicleExists(parent.VehicleID, out var data))
            {
                F.Log($"VEHICLE SPAWNER: starting respawn timer - {data.RespawnTime} seconds");
                parent.Unlink();
                yield return new WaitForSeconds(data.RespawnTime);
                if (parent.IsActive)
                {
                    F.Log($"VEHICLE SPAWNER: respawn timer complete, respawning vehicle...");
                    parent.SpawnVehicle();
                }
            }
        }
    }
}
