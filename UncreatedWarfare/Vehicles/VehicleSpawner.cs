using Newtonsoft.Json;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.XP;
using UnityEngine;

namespace Uncreated.Warfare.Vehicles
{
    public class VehicleSpawner : JSONSaver<VehicleSpawn>, IDisposable
    {
        public const float VEHICLE_HEIGHT_OFFSET = 2f;
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
        internal void OnBarricadeDestroyed(SDG.Unturned.BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant)
        {
            if (data.barricade.asset.GUID == Gamemode.Config.Barricades.VehicleBayGUID)
            {
                if (IsRegistered(data.instanceID, out _, EStructType.BARRICADE))
                {
                    if (UCWarfare.Config.Debug)
                        F.Log("Vehicle spawn was deregistered because the barricade was salvaged or destroyed.", ConsoleColor.DarkGray);
                    DeleteSpawn(data.instanceID, EStructType.BARRICADE);
                }
            }
        }
        internal void OnStructureDestroyed(SDG.Unturned.StructureData data, StructureDrop drop, uint instanceID)
        {
            if (data.structure.asset.GUID == Gamemode.Config.Barricades.VehicleBayGUID)
            {
                if (IsRegistered(data.instanceID, out _, EStructType.STRUCTURE))
                {
                    if (UCWarfare.Config.Debug)
                        F.Log("Vehicle spawn was deregistered because the structure was salvaged or destroyed.", ConsoleColor.DarkGray);
                    DeleteSpawn(data.instanceID, EStructType.STRUCTURE);
                }
            }
        }
        public static void RespawnAllVehicles()
        {
            F.Log("Respawning vehicles...", ConsoleColor.Magenta);
            foreach (InteractableVehicle v in VehicleManager.vehicles.ToList())
            {
                if (HasLinkedSpawn(v.instanceID, out _))
                {
                    if (v.TryGetComponent(out SpawnedVehicleComponent component))
                    {
                        component.StopIdleRespawnTimer();
                    }
                }

                VehicleBay.DeleteVehicle(v);
            }
            foreach (VehicleSpawn spawn in ActiveObjects)
            {
                spawn.CancelVehicleRespawnTimer();

                spawn.SpawnVehicle();
            }
        }
        public static void CreateSpawn(BarricadeDrop drop, SDG.Unturned.BarricadeData data, Guid vehicleID)
        {
            VehicleSpawn spawn = new VehicleSpawn(data.instanceID, vehicleID, EStructType.BARRICADE);
            spawn.Initialize();
            AddObjectToSave(spawn);
            StructureSaver.AddStructure(drop, data, out _);
            spawn.SpawnVehicle();
        }
        public static void CreateSpawn(StructureDrop drop, SDG.Unturned.StructureData data, Guid vehicleID)
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
        public static bool UnusedSpawnExists(Guid vehicleID, out VehicleSpawn spawn) =>
            ObjectExists(s =>
            {
                if (s.VehicleID == vehicleID && s.VehicleInstanceID != 0)
                {
                    var vehicle = VehicleManager.getVehicle(s.VehicleInstanceID);
                    return vehicle != null && !vehicle.isDead && !vehicle.isDrowned;
                }
                return false;
            }, out spawn);
        public static bool SpawnExists(uint bayInstanceID, EStructType type, out VehicleSpawn spawn) =>
            ObjectExists(s => s.SpawnPadInstanceID == bayInstanceID && s.type == type, out spawn);
        public static bool HasLinkedSpawn(uint vehicleInstanceID, out VehicleSpawn spawn) =>
            ObjectExists(s => s.VehicleInstanceID == vehicleInstanceID, out spawn);

        public void Dispose()
        {
            VehicleManager.OnVehicleExploded -= OnVehicleExploded;
        }
        internal static void OnPlayerLeaveVehicle(Player player, InteractableVehicle vehicle)
        {
            if (vehicle.TryGetComponent(out SpawnedVehicleComponent c))
            {
                c.StartIdleRespawnTimer();
            }
        }
    }
    public class VehicleSpawn
    {
        public uint SpawnPadInstanceID;
        public Guid VehicleID;
        public EStructType type;
        [JsonIgnore]
        public uint VehicleInstanceID { get; private set; }
        [JsonIgnore]
        public BarricadeDrop BarricadeDrop { get; private set; }
        [JsonIgnore]
        public SDG.Unturned.BarricadeData BarricadeData { get; private set; }
        [JsonIgnore]
        public StructureDrop StructureDrop { get; private set; }
        [JsonIgnore]
        public SDG.Unturned.StructureData StructureData { get; private set; }
        [JsonIgnore]
        public bool IsActive;
        [JsonIgnore]
        public bool initialized = false;
        public VehicleSpawn(uint spawnPadInstanceId, Guid vehicleID, EStructType type)
        {
            SpawnPadInstanceID = spawnPadInstanceId;
            VehicleID = vehicleID;
            VehicleInstanceID = 0;
            IsActive = true;
            this.type = type;
            initialized = false;
        }
        public VehicleSpawn()
        {
            SpawnPadInstanceID = 0;
            VehicleID = Guid.Empty;
            VehicleInstanceID = 0;
            IsActive = true;
            type = EStructType.BARRICADE;
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
                    initialized = BarricadeData != null;
                    if (!initialized)
                    {
                        F.LogWarning("VEHICLE SPAWNER ERROR: corresponding BarricadeDrop could not be found, attempting to replace the barricade.");
                        if (StructureSaver.StructureExists(SpawnPadInstanceID, EStructType.BARRICADE, out Structures.Structure structure))
                        {
                            if (!(Assets.find(structure.id) is ItemBarricadeAsset asset))
                            {
                                F.LogError("VEHICLE SPAWNER ERROR: barricade asset not found.");
                                initialized = false;
                                IsActive = false;
                                return;
                            }
                            Transform newBarricade = BarricadeManager.dropNonPlantedBarricade(
                                new Barricade(asset, ushort.MaxValue, structure.Metadata),
                                structure.transform.position.Vector3, structure.transform.Rotation, structure.owner, structure.group
                                );
                            BarricadeDrop newdrop = BarricadeManager.FindBarricadeByRootTransform(newBarricade);
                            if (newdrop != null)
                            {
                                BarricadeDrop = newdrop;
                                BarricadeData = newdrop.GetServersideData();
                                structure.instance_id = newdrop.instanceID;
                                SpawnPadInstanceID = newdrop.instanceID;
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
                            F.LogError("VEHICLE SPAWNER ERROR: corresponding BarricadeData could not be found");
                            initialized = false;
                        }
                    }
                    BarricadeDrop.model.transform.gameObject.AddComponent<VehicleSpawnComponent>().Initialize(this);
                }
                else if (type == EStructType.STRUCTURE)
                {
                    StructureDrop = F.GetStructureFromInstID(SpawnPadInstanceID);
                    StructureData = StructureDrop.GetServersideData();
                    initialized = StructureDrop != null;
                    if (!initialized)
                    {
                        F.LogWarning("VEHICLE SPAWNER ERROR: corresponding StructureDrop could not be found, attempting to replace the structure.");
                        if (StructureSaver.StructureExists(SpawnPadInstanceID, EStructType.STRUCTURE, out Structures.Structure structure))
                        {
                            if (!(Assets.find(structure.id) is ItemStructureAsset asset))
                            {
                                F.LogError("VEHICLE SPAWNER ERROR: structure asset not found.");
                                initialized = false;
                                IsActive = false;
                                return;
                            }
                            if (!StructureManager.dropStructure(
                                new SDG.Unturned.Structure(asset, ushort.MaxValue),
                                structure.transform.position.Vector3, structure.transform.euler_angles.x, structure.transform.euler_angles.y,
                                structure.transform.euler_angles.z, structure.owner, structure.group))
                            {
                                F.LogWarning("VEHICLE SPAWNER ERROR: Structure could not be replaced");
                                initialized = false;
                            }
                            else
                            {
                                if (Regions.tryGetCoordinate(structure.transform.position.Vector3, out byte x, out byte y))
                                {
                                    StructureDrop newdrop = StructureManager.regions[x, y].drops.LastOrDefault();
                                    if (newdrop == null)
                                    {
                                        F.LogWarning("VEHICLE SPAWNER ERROR: Spawned structure could not be found");
                                        initialized = false;
                                    }
                                    else
                                    {
                                        F.Log((structure == null).ToString(), ConsoleColor.DarkGray);
                                        StructureData = newdrop.GetServersideData();
                                        StructureDrop = newdrop;
                                        F.Log((StructureData == null).ToString(), ConsoleColor.DarkGray);
                                        F.Log((StructureDrop == null).ToString(), ConsoleColor.DarkGray);
                                        structure.instance_id = newdrop.instanceID;
                                        SpawnPadInstanceID = newdrop.instanceID;
                                        VehicleSpawner.Save();
                                        StructureSaver.Save();
                                        initialized = true;
                                    }
                                }
                                else
                                {
                                    F.LogWarning("VEHICLE SPAWNER ERROR: Unable to get region coordinates.");
                                    initialized = false;
                                }
                            }
                        }
                        else
                        {
                            F.LogError("VEHICLE SPAWNER ERROR: Corresponding StructureData could not be found");
                            initialized = false;
                        }
                    }
                    if (initialized)
                    {
                        StructureDrop.model.transform.gameObject.AddComponent<VehicleSpawnComponent>().Initialize(this);
                    }
                }
                IsActive = initialized;
            }
            catch (Exception ex)
            {
                initialized = false;
                F.LogError("Error initializing vehicle spawn: ");
                F.LogError(ex);
            }
        }
        public void SpawnVehicle()
        {
            try
            {
                if (!initialized)
                {
                    F.LogError($"VEHICLE SPAWNER ERROR: Tried to spawn vehicle without Initializing. {(Assets.find(VehicleID) is VehicleAsset va ? (va.vehicleName + " - " + VehicleID.ToString("N")) : VehicleID.ToString("N"))} spawn.");
                    return;
                }
                if (type == EStructType.BARRICADE)
                {
                    if (BarricadeData == default)
                        F.LogError($"VEHICLE SPAWNER ERROR: {(Assets.find(VehicleID) is VehicleAsset va ? (va.vehicleName + " - " + VehicleID.ToString("N")) : VehicleID.ToString("N"))} at spawn {BarricadeData.point} was unable to find BarricadeData.");
                    Quaternion rotation = new Quaternion
                    { eulerAngles = new Vector3((BarricadeData.angle_x * 2) + 90, BarricadeData.angle_y * 2, BarricadeData.angle_z * 2) };
                    InteractableVehicle veh = VehicleBay.SpawnLockedVehicle(VehicleID, new Vector3(BarricadeData.point.x, BarricadeData.point.y + VehicleSpawner.VEHICLE_HEIGHT_OFFSET, BarricadeData.point.z), rotation, out uint instanceID);
                    if (veh == null)
                    {
                        F.LogWarning("VEHICLE SPAWNER ERROR: " + (Assets.find(VehicleID) is VehicleAsset va ? (va.vehicleName + " - " + VehicleID.ToString("N")) : VehicleID.ToString("N")) + " returned null.");
                        return;
                    }
                    veh.gameObject.AddComponent<SpawnedVehicleComponent>().Initialize(veh);
                    LinkNewVehicle(instanceID);
                    if (UCWarfare.Config.Debug)
                        F.Log($"VEHICLE SPAWNER: spawned {(Assets.find(VehicleID) is VehicleAsset va ? (va.vehicleName + " - " + VehicleID.ToString("N")) : VehicleID.ToString("N"))} at spawn {BarricadeData.point}", ConsoleColor.DarkGray);
                }
                else if (type == EStructType.STRUCTURE)
                {
                    if (StructureData == default)
                        F.LogError($"VEHICLE SPAWNER ERROR: {(Assets.find(VehicleID) is VehicleAsset va ? (va.vehicleName + " - " + VehicleID.ToString("N")) : VehicleID.ToString("N"))} at spawn {StructureData.point} was unable to find StructureData.");
                    Quaternion rotation = new Quaternion
                    { eulerAngles = new Vector3((StructureData.angle_x * 2) + 90, StructureData.angle_y * 2, StructureData.angle_z * 2) };
                    InteractableVehicle veh = VehicleBay.SpawnLockedVehicle(VehicleID, new Vector3(StructureData.point.x, StructureData.point.y + VehicleSpawner.VEHICLE_HEIGHT_OFFSET, StructureData.point.z), rotation, out uint instanceID);
                    if (veh == null)
                    {
                        F.LogWarning("VEHICLE SPAWNER ERROR: " + (Assets.find(VehicleID) is VehicleAsset va ? (va.vehicleName + " - " + VehicleID.ToString("N")) : VehicleID.ToString("N")) + " returned null.");
                        return;
                    }
                    veh.gameObject.AddComponent<SpawnedVehicleComponent>().Initialize(veh);
                    LinkNewVehicle(instanceID);
                    if (UCWarfare.Config.Debug)
                        F.Log($"VEHICLE SPAWNER: spawned {(Assets.find(VehicleID) is VehicleAsset va ? (va.vehicleName + " - " + VehicleID.ToString("N")) : VehicleID.ToString("N"))} at spawn {StructureData.point}", ConsoleColor.DarkGray);
                }
            }
            catch (Exception ex)
            {
                F.LogError($"Error spawning vehicle {this.VehicleID} on vehicle bay: ");
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
        private Coroutine timer;
        private Coroutine xploop;
        private InteractableVehicle Owner;
        private VehicleData data;

        public void Initialize(InteractableVehicle vehicle)
        {
            Owner = vehicle;

            if (VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData data))
            {
                this.data = data;
                StartXPLoop();
            }
        }
        public void StartIdleRespawnTimer()
        {
            if (Owner == null) return;
            if (data != null)
            {
                StopIdleRespawnTimer();
                timer = StartCoroutine(IdleRespawnVehicle(data));
            }
        }
        public void StopIdleRespawnTimer()
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
        public void StartXPLoop()
        {
            if (Owner == null) return;
            if (data != null)
            {
                xploop = StartCoroutine(XPLoop());
            }
        }
        private IEnumerator<WaitForSeconds> IdleRespawnVehicle(VehicleData data)
        {
            yield return new WaitForSeconds(data.RespawnTime);
            if (!Owner.anySeatsOccupied)
            {
                while (PlayerManager.IsPlayerNearby(Owner.lockedOwner.m_SteamID, 150, Owner.transform.position))
                {
                    yield return new WaitForSeconds(60);
                    if (Owner.anySeatsOccupied)
                    {
                        yield break;
                    }
                }
                VehicleBay.DeleteVehicle(Owner);
                if (VehicleSpawner.HasLinkedSpawn(Owner.instanceID, out VehicleSpawn spawn))
                    spawn.SpawnVehicle();
            }
        }
        private IEnumerator<WaitForSeconds> XPLoop()
        {
            if (data is null) yield break;

            while (!Owner.isDead)
            {
                int count = 0;
                //       not count driver
                for (int i = 1; i < Owner.passengers.Length; i++)
                {
                    if (!data.CrewSeats.Exists(x => x == i) && Owner.passengers[i] != null && Owner.passengers[i].player != null) count++;
                }
                if (Owner.passengers.Length > 0)
                {
                    if (Owner.passengers[0] != null && Owner.passengers[0].player != null && count > 1 && Owner.speed > 0)
                    {
                        UCPlayer player = UCPlayer.FromSteamPlayer(Owner.passengers[0].player);
                        if (player != null)
                        {
                            //if (player.Squad != null)
                            //    await OfficerManager.AddOfficerPoints(player.Player, OfficerManager.config.Data.TransportPlayerPoints * (count - 2), F.Translate("ofp_transporting_players", player.Steam64));
                            //else
                            XPManager.AddXP(player.Player, XPManager.config.Data.TransportPlayerXP * (count - 1), F.Translate("xp_transporting_players", player.Steam64));
                        }
                    }
                }
                yield return new WaitForSeconds(XPManager.config.Data.TimeBetweenXpAndOfpAwardForTransport);
            }
        }
    }
}
