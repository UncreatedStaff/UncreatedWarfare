using Newtonsoft.Json;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Structures;
using UnityEngine;

namespace Uncreated.Warfare.Vehicles
{
    public class VehicleSpawner : JSONSaver<VehicleSpawn>, IDisposable
    {
        public const float VEHICLE_HEIGHT_OFFSET = 1f;
        public VehicleSpawner()
            : base(Data.VehicleStorage + "vehiclespawns.json")
        {
            VehicleManager.OnVehicleExploded += OnVehicleExploded;
        }
        protected override string LoadDefaults() => "[]";
        public void OnLevelLoaded()
        {
            LoadSpawns();
            RespawnAllVehicles();
        }
        private void LoadSpawns()
        {
            foreach (VehicleSpawn spawn in ActiveObjects)
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
        internal void OnBarricadeDestroyed(BarricadeRegion region, BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant, ushort index)
        {
            if (data.barricade.id == UCWarfare.Config.VehicleBaySettings.VehicleSpawnerID)
            {
                if (IsRegistered(data.instanceID, out _, EStructType.BARRICADE))
                {
                    if (UCWarfare.Config.Debug)
                        F.Log("Vehicle spawn was deregistered because the barricade was salvaged or destroyed.");
                    DeleteSpawn(data.instanceID, EStructType.BARRICADE);
                }
            }
        }
        internal void OnStructureDestroyed(StructureRegion region, StructureData data, StructureDrop drop, uint instanceID)
        {
            if (data.structure.id == UCWarfare.Config.VehicleBaySettings.VehicleSpawnerID)
            {
                if (IsRegistered(data.instanceID, out _, EStructType.STRUCTURE))
                {
                    if (UCWarfare.Config.Debug)
                        F.Log("Vehicle spawn was deregistered because the structure was salvaged or destroyed.");
                    DeleteSpawn(data.instanceID, EStructType.STRUCTURE);
                }
            }
        }
        public static void RespawnAllVehicles()
        {
            F.Log("Respawning vehicles...", ConsoleColor.Magenta);
            VehicleManager.askVehicleDestroyAll();
            ItemManager.askClearAllItems();
            foreach (VehicleSpawn spawn in ActiveObjects)
            {
                spawn.SpawnVehicle();
            }
        }
        public static void CreateSpawn(BarricadeDrop drop, BarricadeData data, ushort vehicleID)
        {
            VehicleSpawn spawn = new VehicleSpawn(data.instanceID, vehicleID, EStructType.BARRICADE);
            spawn.Initialize();
            AddObjectToSave(spawn);
            StructureSaver.AddStructure(drop, data, out _);
            spawn.SpawnVehicle();
        }
        public static void CreateSpawn(StructureDrop drop, StructureData data, ushort vehicleID)
        {
            VehicleSpawn spawn = new VehicleSpawn(data.instanceID, vehicleID, EStructType.STRUCTURE);
            spawn.Initialize();
            AddObjectToSave(spawn);
            StructureSaver.AddStructure(drop, data, out _);
            spawn.SpawnVehicle();
        }
        public static void DeleteSpawn(uint barricadeInstanceID, EStructType type)
        {
            VehicleSpawn spawn = GetObject(s => s.SpawnPadInstanceID == barricadeInstanceID && s.type == type);
            if (spawn != null)
            {
                spawn.IsActive = false;
                spawn.initialized = false;
            }
            RemoveWhere(s => s.SpawnPadInstanceID == barricadeInstanceID && s.type == type);
            StructureSaver.RemoveWhere(x => x.instance_id == barricadeInstanceID && x.type == type);
        }
        public static bool IsRegistered(uint barricadeInstanceID, out VehicleSpawn spawn, EStructType type)
        {
            if (type == EStructType.BARRICADE)
                return ObjectExists(s => barricadeInstanceID == s.SpawnPadInstanceID, out spawn);
            else if (type == EStructType.STRUCTURE)
                return ObjectExists(s => barricadeInstanceID == s.SpawnPadInstanceID, out spawn);
            else
            {
                spawn = null;
                return false;
            }
        }
        public static bool IsRegistered(SerializableTransform transform, out VehicleSpawn spawn, EStructType type)
        {
            if (type == EStructType.BARRICADE)
                return ObjectExists(s => transform == s.BarricadeDrop.model.transform, out spawn);
            else if (type == EStructType.STRUCTURE)
                return ObjectExists(s => transform == s.StructureDrop.model.transform, out spawn);
            else
            {
                spawn = null;
                return false;
            }
        }
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

        public void Dispose()
        {
            VehicleManager.OnVehicleExploded -= OnVehicleExploded;
        }
        internal static void OnPlayerLeaveVehicle(Player player, InteractableVehicle vehicle)
        {
            if (vehicle.TryGetComponent(out SpawnedVehicleComponent c))
                c.StartIdleVehicleTimer();
        }
    }
    public class VehicleSpawn
    {
        public uint SpawnPadInstanceID;
        public ushort VehicleID;
        public EStructType type;
        [JsonIgnore]
        public uint VehicleInstanceID { get; private set; }
        [JsonIgnore]
        public BarricadeDrop BarricadeDrop { get; private set; }
        [JsonIgnore]
        public BarricadeData BarricadeData { get; private set; }
        [JsonIgnore]
        public StructureDrop StructureDrop { get; private set; }
        [JsonIgnore]
        public StructureData StructureData { get; private set; }
        [JsonIgnore]
        public bool IsActive;
        [JsonIgnore]
        public bool initialized = false;
        public VehicleSpawn(uint spawnPadInstanceId, ushort vehicleID, EStructType type)
        {
            SpawnPadInstanceID = spawnPadInstanceId;
            VehicleID = vehicleID;
            VehicleInstanceID = 0;
            IsActive = true;
            this.type = type;
            initialized = false;
        }
        public void Initialize()
        {
            try
            {
                if (type == EStructType.BARRICADE)
                {
                    BarricadeData = F.GetBarricadeFromInstID(SpawnPadInstanceID, out BarricadeDrop drop);
                    BarricadeDrop = drop;
                    initialized = true;
                    if (BarricadeData is null)
                    {
                        F.LogWarning("VEHICLE SPAWNER ERROR: corresponding BarricadeDrop could not be found, attempting to replace the barricade.");
                        if (StructureSaver.StructureExists(SpawnPadInstanceID, EStructType.BARRICADE, out Structures.Structure structure))
                        {
                            Transform newBarricade = BarricadeManager.dropNonPlantedBarricade(
                                new Barricade(structure.id, ushort.MaxValue, structure.Metadata, structure.Asset as ItemBarricadeAsset),
                                structure.transform.position.Vector3, structure.transform.Rotation, structure.owner, structure.group
                                );
                            if (BarricadeManager.tryGetInfo(newBarricade, out _, out _, out _, out ushort index, out BarricadeRegion region))
                            {
                                if (region != default)
                                {
                                    BarricadeDrop = region.drops[index];
                                    BarricadeData = region.barricades[index];
                                    structure.instance_id = BarricadeDrop.instanceID;
                                    SpawnPadInstanceID = BarricadeDrop.instanceID;
                                    VehicleSpawner.Save();
                                    StructureSaver.Save();
                                    initialized = true;
                                }
                                else
                                {
                                    F.LogError("VEHICLE SPAWNER ERROR: spawned barricade could not be found.");
                                    initialized = false;
                                }
                            }
                            else
                            {
                                F.LogError("VEHICLE SPAWNER ERROR: Barricade could not be replaced");
                                initialized = false;
                            }
                        }
                        else
                        {
                            F.LogError("VEHICLE SPAWNER ERROR: corresponding BarricadeData could not be found");
                            initialized = false;
                        }
                    }
                    BarricadeDrop.model.transform.gameObject.AddComponent<VehicleSpawnComponent>().Initialize(this);
                }
                else if (type == EStructType.STRUCTURE)
                {
                    StructureData = F.GetStructureFromInstID(SpawnPadInstanceID, out StructureDrop drop);
                    StructureDrop = drop;
                    initialized = true;
                    if (StructureData is null)
                    {
                        F.LogWarning("VEHICLE SPAWNER ERROR: corresponding StructureDrop could not be found, attempting to replace the structure.");
                        if (StructureSaver.StructureExists(SpawnPadInstanceID, EStructType.STRUCTURE, out Structures.Structure structure))
                        {
                            if (!StructureManager.dropStructure(
                                new SDG.Unturned.Structure(structure.id, ushort.MaxValue, structure.Asset as ItemStructureAsset),
                                structure.transform.position.Vector3, structure.transform.euler_angles.x, structure.transform.euler_angles.y,
                                structure.transform.euler_angles.z, structure.owner, structure.group))
                            {
                                F.LogError("VEHICLE SPAWNER ERROR: Structure could not be replaced");
                            }
                            else
                            {
                                StructureData newdata = F.GetStructureFromTransform(structure.transform, out StructureDrop newdrop);
                                if (newdata == default || newdrop == default)
                                {
                                    F.LogError("VEHICLE SPAWNER ERROR: spawned structure could not be found");
                                    initialized = false;
                                }
                                else
                                {
                                    StructureData = newdata;
                                    StructureDrop = newdrop;
                                    structure.instance_id = newdata.instanceID;
                                    SpawnPadInstanceID = newdrop.instanceID;
                                    VehicleSpawner.Save();
                                    StructureSaver.Save();
                                    initialized = true;
                                }
                            }
                        }
                        else
                        {
                            F.LogError("VEHICLE SPAWNER ERROR: corresponding StructureData could not be found");
                            initialized = false;
                        }
                    }
                    StructureDrop.model.transform.gameObject.AddComponent<VehicleSpawnComponent>().Initialize(this);
                }
                IsActive = initialized;
            }
            catch (Exception ex)
            {
                F.LogError("VEHICLE SPAWNER ERROR: Initializing " + VehicleID.ToString(Data.Locale));
                F.LogError(ex);
            }
        }
        public void SpawnVehicle()
        {
            try
            {
                if (!initialized)
                {
                    F.LogError($"VEHICLE SPAWNER ERROR: Tried to spawn vehicle without Initializing. {UCAssetManager.FindVehicleAsset(VehicleID).vehicleName} spawn.");
                    return;
                }
                if (type == EStructType.BARRICADE)
                {
                    if (BarricadeData == default)
                    {
                        F.LogError($"VEHICLE SPAWNER ERROR: {UCAssetManager.FindVehicleAsset(VehicleID).vehicleName} - {VehicleID} at spawn {BarricadeData.point} was unable to find BarricadeData.");
                        return;
                    }
                    Quaternion rotation = new Quaternion
                    { eulerAngles = new Vector3((BarricadeData.angle_x * 2) + 90, BarricadeData.angle_y * 2, BarricadeData.angle_z * 2) };
                    InteractableVehicle veh = VehicleBay.SpawnLockedVehicle(VehicleID, new Vector3(BarricadeData.point.x, BarricadeData.point.y + VehicleSpawner.VEHICLE_HEIGHT_OFFSET, BarricadeData.point.z), rotation, out uint instanceID);
                    veh.gameObject.AddComponent<SpawnedVehicleComponent>().Owner = veh;
                    LinkNewVehicle(instanceID);
                    if (UCWarfare.Config.Debug)
                        F.Log($"VEHICLE SPAWNER: spawned {UCAssetManager.FindVehicleAsset(VehicleID).vehicleName} - {VehicleID} at spawn {BarricadeData.point}");
                }
                else if (type == EStructType.STRUCTURE)
                {
                    if (StructureData == default)
                    {
                        F.LogError($"VEHICLE SPAWNER ERROR: {UCAssetManager.FindVehicleAsset(VehicleID).vehicleName} - {VehicleID} at spawn {StructureData.point} was unable to find StructureData.");
                        return;
                    }
                    Quaternion rotation = new Quaternion
                    { eulerAngles = new Vector3((StructureData.angle_x * 2) + 90, StructureData.angle_y * 2, StructureData.angle_z * 2) };
                    InteractableVehicle veh = VehicleBay.SpawnLockedVehicle(VehicleID, new Vector3(StructureData.point.x, StructureData.point.y + VehicleSpawner.VEHICLE_HEIGHT_OFFSET, StructureData.point.z), rotation, out uint instanceID);
                    veh.gameObject.AddComponent<SpawnedVehicleComponent>().Owner = veh;
                    LinkNewVehicle(instanceID);
                    if (UCWarfare.Config.Debug)
                        F.Log($"VEHICLE SPAWNER: spawned {UCAssetManager.FindVehicleAsset(VehicleID).vehicleName} - {VehicleID} at spawn {StructureData.point}");
                }
            }
            catch (Exception ex)
            {
                F.LogError("VEHICLE SPAWNER ERROR: Spawning " + VehicleID);
                F.LogError(ex);
            }
            
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
            if ((type == EStructType.BARRICADE && BarricadeDrop is null) || (type == EStructType.STRUCTURE && StructureDrop is null))
            {
                F.LogWarning($"VEHICLE SPAWNER ERROR: could not start respawn timer, {(type == EStructType.BARRICADE ? "Barricade" : "Structure")}Drop was null");
                return;
            }
            if ((type == EStructType.BARRICADE && BarricadeDrop.model.transform.TryGetComponent(out VehicleSpawnComponent component)) ||
                (type == EStructType.STRUCTURE && StructureDrop.model.transform.TryGetComponent(out component)))
            {
                component.StartRespawnVehicleTimer();
            }
            else
            {
                F.LogWarning($"VEHICLE SPAWNER ERROR: could not start respawn timer, unable to get {nameof(VehicleSpawnComponent)} component from drop");
            }
        }
        public void CancelVehicleRespawnTimer()
        {
            if ((type == EStructType.BARRICADE && BarricadeDrop is null) || (type == EStructType.STRUCTURE && StructureDrop is null))
            {
                F.LogWarning($"VEHICLE SPAWNER ERROR: could not start respawn timer, {(type == EStructType.BARRICADE ? "Barricade" : "Structure")}Drop was null");
                return;
            }
            if ((type == EStructType.BARRICADE && BarricadeDrop.model.transform.TryGetComponent(out VehicleSpawnComponent component)) ||
                (type == EStructType.STRUCTURE && StructureDrop.model.transform.TryGetComponent(out component)))
            {
                component.CancelRespawnVehicleTimer();
            }
            else
            {
                F.LogWarning($"VEHICLE SPAWNER ERROR: could not start respawn timer, unable to get {nameof(VehicleSpawnComponent)} component from drop");
            }
        }
    }
    public class VehicleSpawnComponent : MonoBehaviour
    {
        VehicleSpawn parent;
        Coroutine timer;
        public void Initialize(VehicleSpawn parent)
        {
            this.parent = parent;
        }
        public void CancelRespawnVehicleTimer()
        {
            if (timer != null)
            {
                try
                {
                    StopCoroutine(timer);
                }
                catch { }
            }
        }
        public void StartRespawnVehicleTimer()
        {
            CancelRespawnVehicleTimer();
            timer = StartCoroutine(RespawnVehicleTimer());
        }
        private IEnumerator<WaitForSeconds> RespawnVehicleTimer()
        {
            if (VehicleBay.VehicleExists(parent.VehicleID, out VehicleData data))
            {
                parent.Unlink();
                yield return new WaitForSeconds(data.RespawnTime);
                if (parent.IsActive)
                {
                    parent.SpawnVehicle();
                }
            }
        }
    }
    public class SpawnedVehicleComponent : MonoBehaviour
    {
        Coroutine timer;
        public InteractableVehicle Owner;
        public void StartIdleVehicleTimer()
        {
            if (Owner == null) return;
            if (VehicleBay.VehicleExists(Owner.id, out VehicleData data))
            {
                CancelIdleVehicleTimer();
                timer = StartCoroutine(IdleRespawnVehicle(data));
            }
        }
        public void CancelIdleVehicleTimer()
        {
            if (timer != null)
            {
                try
                {
                    StopCoroutine(timer);
                }
                catch { }
            }
        }
        private IEnumerator<WaitForSeconds> IdleRespawnVehicle(VehicleData data)
        {
            yield return new WaitForSeconds(data.RespawnTime);
            if (!Owner.anySeatsOccupied)
            {
                VehicleManager.askVehicleDestroy(Owner);
                if (VehicleSpawner.HasLinkedSpawn(Owner.instanceID, out Vehicles.VehicleSpawn spawn))
                    spawn.SpawnVehicle();
            }
        }
    }
}
