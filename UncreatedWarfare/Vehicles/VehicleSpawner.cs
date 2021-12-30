using Newtonsoft.Json;
using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
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
            TeamManager.OnPlayerEnteredMainBase += OnPlayerEnterMain;
            TeamManager.OnPlayerLeftMainBase += OnPlayerLeftMain;
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
                    L.LogDebug("Vehicle spawn was deregistered because the barricade was salvaged or destroyed.");
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
                    L.LogDebug("Vehicle spawn was deregistered because the structure was salvaged or destroyed.");
                    DeleteSpawn(data.instanceID, EStructType.STRUCTURE);
                }
            }
        }
        public static void RespawnAllVehicles()
        {
            L.Log("Respawning vehicles...", ConsoleColor.Magenta);
            for (int i = VehicleManager.vehicles.Count - 1; i >= 0; i--)
            {
                InteractableVehicle v = VehicleManager.vehicles[i];
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
            VehicleSpawn spawn = new VehicleSpawn(data.instanceID, vehicleID, EStructType.BARRICADE, new SerializableTransform(drop.model));
            spawn.Initialize();
            AddObjectToSave(spawn);
            StructureSaver.AddStructure(drop, data, out _);
            spawn.SpawnVehicle();
        }
        public static void CreateSpawn(StructureDrop drop, SDG.Unturned.StructureData data, Guid vehicleID)
        {
            VehicleSpawn spawn = new VehicleSpawn(data.instanceID, vehicleID, EStructType.STRUCTURE, new SerializableTransform(drop.model));
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
            TeamManager.OnPlayerEnteredMainBase -= OnPlayerEnterMain;
            TeamManager.OnPlayerLeftMainBase -= OnPlayerLeftMain;
        }
        internal static void OnPlayerLeaveVehicle(Player player, InteractableVehicle vehicle)
        {
#if false
            if (vehicle.TryGetComponent(out SpawnedVehicleComponent c))
            {
                c.StartIdleRespawnTimer();
            }
#endif
        }
        private static void OnPlayerEnterMain(SteamPlayer player, ulong team)
        {
            player.SendChat("entered_main", TeamManager.TranslateName(team, player, true));
            for (int i = 0; i < ActiveObjects.Count; i++)
            {
                VehicleSpawn spawn = ActiveObjects[i];
                if (spawn.LinkedSign != null && spawn.LinkedSign.SignDrop != null && (
                    team == 1 && TeamManager.Team1Main.IsInside(spawn.LinkedSign.SignDrop.model.transform.position) || 
                    team == 2 && TeamManager.Team2Main.IsInside(spawn.LinkedSign.SignDrop.model.transform.position)))
                    spawn.UpdateSign(player);
            }
        }
        private static void OnPlayerLeftMain(SteamPlayer player, ulong team)
        {
            player.SendChat("left_main", TeamManager.TranslateName(team, player, true));
        }
        public static void UpdateSigns(Guid vehicle)
        {
            for (int i = 0; i < ActiveObjects.Count; i++)
            {
                VehicleSpawn spawn = ActiveObjects[i];
                if (spawn.VehicleID == vehicle)
                    spawn.UpdateSign();
            }
        }
        public static void UpdateSigns(uint vehicle)
        {
            for (int i = 0; i < ActiveObjects.Count; i++)
            {
                VehicleSpawn spawn = ActiveObjects[i];
                if (spawn.VehicleInstanceID == vehicle)
                    spawn.UpdateSign();
            }
        }
    }
    public class VehicleSpawn
    {
        public uint SpawnPadInstanceID;
        public Guid VehicleID;
        public EStructType type;
        public SerializableTransform SpawnpadLocation;
        [JsonIgnore]
        public uint VehicleInstanceID { get; internal set; }
        [JsonIgnore]
        public BarricadeDrop BarricadeDrop { get; internal set; }
        [JsonIgnore]
        public SDG.Unturned.BarricadeData BarricadeData { get; internal set; }
        [JsonIgnore]
        public StructureDrop StructureDrop { get; internal set; }
        [JsonIgnore]
        public SDG.Unturned.StructureData StructureData { get; internal set; }
        [JsonIgnore]
        public bool IsActive;
        [JsonIgnore]
        public bool initialized = false;
        [JsonIgnore]
        public VehicleSign LinkedSign;
        public VehicleSpawn(uint spawnPadInstanceId, Guid vehicleID, EStructType type, SerializableTransform loc)
        {
            SpawnPadInstanceID = spawnPadInstanceId;
            VehicleID = vehicleID;
            VehicleInstanceID = 0;
            IsActive = true;
            this.type = type;
            initialized = false;
            SpawnpadLocation = loc;
        }
        public VehicleSpawn()
        {
            SpawnPadInstanceID = 0;
            VehicleID = Guid.Empty;
            VehicleInstanceID = 0;
            IsActive = true;
            type = EStructType.BARRICADE;
            initialized = false;
            SpawnpadLocation = default;
        }
        public void Initialize()
        {
            try
            {
                if (type == EStructType.BARRICADE)
                {
                    BarricadeData = UCBarricadeManager.GetBarricadeFromInstID(SpawnPadInstanceID, out BarricadeDrop drop);
                    BarricadeDrop = drop;
                    initialized = BarricadeData != null;
                    if (!initialized)
                    {
                        L.LogWarning("VEHICLE SPAWNER ERROR: corresponding BarricadeDrop could not be found, attempting to replace the barricade.");
                        if (!StructureSaver.StructureExists(SpawnPadInstanceID, EStructType.BARRICADE, out Structures.Structure structure))
                        {
                            if (SpawnpadLocation != default(SerializableTransform))
                                structure = StructureSaver.ActiveObjects.FirstOrDefault(x => x.transform == SpawnpadLocation && x.type == EStructType.BARRICADE);
                            if (structure == null)
                            {
                                L.LogError("VEHICLE SPAWNER ERROR: barricade save not found.");
                                initialized = false;
                                IsActive = false;
                                return;
                            }
                        }
                        if (!(Assets.find(structure.id) is ItemBarricadeAsset asset))
                        {
                            L.LogError("VEHICLE SPAWNER ERROR: barricade asset not found.");
                            initialized = false;
                            IsActive = false;
                            return;
                        }
                        Transform newBarricade = BarricadeManager.dropNonPlantedBarricade(
                            new Barricade(asset, asset.health, structure.Metadata),
                            structure.transform.position.Vector3, structure.transform.Rotation, structure.owner, structure.group);
                        if (newBarricade == null)
                        {
                            L.LogError("VEHICLE SPAWNER ERROR: barricade could not be spawned.");
                            initialized = false;
                            IsActive = false;
                            return;
                        }
                        BarricadeDrop newdrop = BarricadeManager.FindBarricadeByRootTransform(newBarricade);
                        if (newdrop != null)
                        {
                            BarricadeDrop = newdrop;
                            BarricadeData = newdrop.GetServersideData();
                            structure.instance_id = newdrop.instanceID;
                            SpawnPadInstanceID = newdrop.instanceID;
                            SpawnpadLocation = new SerializableTransform(newdrop.model);
                            VehicleSpawner.Save();
                            StructureSaver.Save();
                            initialized = true;
                        }
                        else
                        {
                            L.LogError("VEHICLE SPAWNER ERROR: spawned barricade could not be found.");
                            initialized = false;
                        }
                    }
                    else if (SpawnpadLocation != drop.model)
                    {
                        SpawnpadLocation = new SerializableTransform(drop.model);
                        VehicleSpawner.Save();
                    }
                    BarricadeDrop.model.transform.gameObject.AddComponent<VehicleSpawnComponent>().Initialize(this);
                }
                else if (type == EStructType.STRUCTURE)
                {
                    StructureDrop = UCBarricadeManager.GetStructureFromInstID(SpawnPadInstanceID);
                    initialized = StructureDrop != null;
                    if (!initialized)
                    {
                        L.LogWarning("VEHICLE SPAWNER ERROR: corresponding StructureDrop could not be found, attempting to replace the structure.");
                        if (!StructureSaver.StructureExists(SpawnPadInstanceID, EStructType.STRUCTURE, out Structures.Structure structure))
                        {
                            if (SpawnpadLocation != default(SerializableTransform))
                                structure = StructureSaver.ActiveObjects.FirstOrDefault(x => x.transform == SpawnpadLocation && x.type == EStructType.STRUCTURE);
                            if (structure == null)
                            {
                                L.LogError("VEHICLE SPAWNER ERROR: structure save not found.");
                                initialized = false;
                                IsActive = false;
                                return;
                            }
                        }
                        if (!(Assets.find(structure.id) is ItemStructureAsset asset))
                        {
                            L.LogError("VEHICLE SPAWNER ERROR: structure asset not found.");
                            initialized = false;
                            IsActive = false;
                            return;
                        }
                        if (!StructureManager.dropStructure(
                            new SDG.Unturned.Structure(asset, ushort.MaxValue),
                            structure.transform.position.Vector3, structure.transform.euler_angles.x, structure.transform.euler_angles.y,
                            structure.transform.euler_angles.z, structure.owner, structure.group))
                        {
                            L.LogWarning("VEHICLE SPAWNER ERROR: Structure could not be replaced");
                            initialized = false;
                        }
                        else if (Regions.tryGetCoordinate(structure.transform.position.Vector3, out byte x, out byte y))
                        {
                            StructureDrop newdrop = StructureManager.regions[x, y].drops.LastOrDefault();
                            if (newdrop == null)
                            {
                                L.LogWarning("VEHICLE SPAWNER ERROR: Spawned structure could not be found");
                                initialized = false;
                            }
                            else
                            {
                                L.Log((structure == null).ToString(), ConsoleColor.DarkGray);
                                StructureData = newdrop.GetServersideData();
                                StructureDrop = newdrop;
                                L.Log((StructureData == null).ToString(), ConsoleColor.DarkGray);
                                L.Log((StructureDrop == null).ToString(), ConsoleColor.DarkGray);
                                structure.instance_id = newdrop.instanceID;
                                SpawnPadInstanceID = newdrop.instanceID;
                                VehicleSpawner.Save();
                                StructureSaver.Save();
                                initialized = true;
                            }
                        }
                        else
                        {
                            L.LogWarning("VEHICLE SPAWNER ERROR: Unable to get region coordinates.");
                            initialized = false;
                        }
                    }
                    else
                        StructureData = StructureDrop.GetServersideData();
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
                L.LogError("Error initializing vehicle spawn: ");
                L.LogError(ex);
            }
        }
        public void SpawnVehicle()
        {
            try
            {
                if (!initialized)
                {
                    L.LogError($"VEHICLE SPAWNER ERROR: Tried to spawn vehicle without Initializing. {(Assets.find(VehicleID) is VehicleAsset va ? (va.vehicleName + " - " + VehicleID.ToString("N")) : VehicleID.ToString("N"))} spawn.");
                    return;
                }
                if (type == EStructType.BARRICADE)
                {
                    if (BarricadeData == default)
                        L.LogError($"VEHICLE SPAWNER ERROR: {(Assets.find(VehicleID) is VehicleAsset va ? (va.vehicleName + " - " + VehicleID.ToString("N")) : VehicleID.ToString("N"))} at spawn {BarricadeData.point} was unable to find BarricadeData.");
                    Quaternion rotation = new Quaternion
                    { eulerAngles = new Vector3((BarricadeData.angle_x * 2) + 90, BarricadeData.angle_y * 2, BarricadeData.angle_z * 2) };
                    InteractableVehicle veh = VehicleBay.SpawnLockedVehicle(VehicleID, new Vector3(BarricadeData.point.x, BarricadeData.point.y + VehicleSpawner.VEHICLE_HEIGHT_OFFSET, BarricadeData.point.z), rotation, out uint instanceID);
                    if (veh == null)
                    {
                        L.LogWarning("VEHICLE SPAWNER ERROR: " + (Assets.find(VehicleID) is VehicleAsset va ? (va.vehicleName + " - " + VehicleID.ToString("N")) : VehicleID.ToString("N")) + " returned null.");
                        return;
                    }
                    veh.gameObject.AddComponent<SpawnedVehicleComponent>().Initialize(veh, this);
                    LinkNewVehicle(instanceID);
                    UpdateSign();
                    if (UCWarfare.Config.Debug)
                        L.Log($"VEHICLE SPAWNER: spawned {(Assets.find(VehicleID) is VehicleAsset va ? (va.vehicleName + " - " + VehicleID.ToString("N")) : VehicleID.ToString("N"))} at spawn {BarricadeData.point}", ConsoleColor.DarkGray);
                }
                else if (type == EStructType.STRUCTURE)
                {
                    if (StructureData == default)
                        L.LogError($"VEHICLE SPAWNER ERROR: {(Assets.find(VehicleID) is VehicleAsset va ? (va.vehicleName + " - " + VehicleID.ToString("N")) : VehicleID.ToString("N"))} at spawn {StructureData.point} was unable to find StructureData.");
                    Quaternion rotation = new Quaternion
                    { eulerAngles = new Vector3((StructureData.angle_x * 2) + 90, StructureData.angle_y * 2, StructureData.angle_z * 2) };
                    InteractableVehicle veh = VehicleBay.SpawnLockedVehicle(VehicleID, new Vector3(StructureData.point.x, StructureData.point.y + VehicleSpawner.VEHICLE_HEIGHT_OFFSET, StructureData.point.z), rotation, out uint instanceID);
                    if (veh == null)
                    {
                        L.LogWarning("VEHICLE SPAWNER ERROR: " + (Assets.find(VehicleID) is VehicleAsset va ? (va.vehicleName + " - " + VehicleID.ToString("N")) : VehicleID.ToString("N")) + " returned null.");
                        return;
                    }
                    veh.gameObject.AddComponent<SpawnedVehicleComponent>().Initialize(veh, this);
                    LinkNewVehicle(instanceID);
                    UpdateSign();
                    if (UCWarfare.Config.Debug)
                        L.Log($"VEHICLE SPAWNER: spawned {(Assets.find(VehicleID) is VehicleAsset va ? (va.vehicleName + " - " + VehicleID.ToString("N")) : VehicleID.ToString("N"))} at spawn {StructureData.point}", ConsoleColor.DarkGray);
                }
            }
            catch (Exception ex)
            {
                L.LogError($"Error spawning vehicle {this.VehicleID} on vehicle bay: ");
                L.LogError(ex);
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
                L.LogWarning($"VEHICLE SPAWNER ERROR: could not start respawn timer, {(type == EStructType.BARRICADE ? "Barricade" : "Structure")}Drop was null");
                return;
            }
            if ((type == EStructType.BARRICADE && BarricadeDrop.model.transform.TryGetComponent(out VehicleSpawnComponent component)) ||
                (type == EStructType.STRUCTURE && StructureDrop.model.transform.TryGetComponent(out component)))
            {
                component.StartRespawnVehicleTimer();
            }
            else
            {
                L.LogWarning($"VEHICLE SPAWNER ERROR: could not start respawn timer, unable to get {nameof(VehicleSpawnComponent)} component from drop");
            }
        }
        public void CancelVehicleRespawnTimer()
        {
            if ((type == EStructType.BARRICADE && BarricadeDrop is null) || (type == EStructType.STRUCTURE && StructureDrop is null))
            {
                L.LogWarning($"VEHICLE SPAWNER ERROR: could not start respawn timer, {(type == EStructType.BARRICADE ? "Barricade" : "Structure")}Drop was null");
                return;
            }
            if ((type == EStructType.BARRICADE && BarricadeDrop.model.transform.TryGetComponent(out VehicleSpawnComponent component)) ||
                (type == EStructType.STRUCTURE && StructureDrop.model.transform.TryGetComponent(out component)))
            {
                component.CancelRespawnVehicleTimer();
            }
            else
            {
                L.LogWarning($"VEHICLE SPAWNER ERROR: could not start respawn timer, unable to get {nameof(VehicleSpawnComponent)} component from drop");
            }
        }

        public void UpdateSign(SteamPlayer player)
        {
            if (this.LinkedSign == null || this.LinkedSign.SignInteractable == null || this.LinkedSign.SignDrop == null) return;
            UpdateSignInternal(player, this);
        }
        private static IEnumerator<SteamPlayer> BasesToPlayer(IEnumerator<KeyValuePair<ulong, byte>> b, byte team)
        {
            while (b.MoveNext())
            {
                if (b.Current.Value == team)
                {
                    for (int i = 0; i < Provider.clients.Count; i++)
                    {
                        if (Provider.clients[i].playerID.steamID.m_SteamID == b.Current.Key)
                            yield return Provider.clients[i];
                    }
                }
            }
            b.Dispose();
        }
        public void UpdateSign()
        {
            L.LogDebug("Updating sign " + (Assets.find(VehicleID)?.name ?? VehicleID.ToString("N")));
            if (this.LinkedSign == null || this.LinkedSign.SignInteractable == null || this.LinkedSign.SignDrop == null) return;
            if (TeamManager.Team1Main.IsInside(LinkedSign.SignDrop.model.transform.position))
            {
                IEnumerator<SteamPlayer> t1Main = BasesToPlayer(TeamManager.PlayerBaseStatus.GetEnumerator(), 1);
                UpdateSignInternal(t1Main, this);
            }
            else if (TeamManager.Team2Main.IsInside(LinkedSign.SignDrop.model.transform.position))
            {
                IEnumerator<SteamPlayer> t2Main = BasesToPlayer(TeamManager.PlayerBaseStatus.GetEnumerator(), 2);
                UpdateSignInternal(t2Main, this);
            }
            else if (Regions.tryGetCoordinate(LinkedSign.SignDrop.model.transform.position, out byte x, out byte y))
            {
                IEnumerator<SteamPlayer> t2Main = F.EnumerateClients_Remote(x, y, BarricadeManager.BARRICADE_REGIONS).GetEnumerator();
                UpdateSignInternal(t2Main, this);
            }
            else
            {
                L.LogWarning($"Vehicle sign not in main bases or any region!");
            }
        }
        private static void UpdateSignInternal(IEnumerator<SteamPlayer> players, VehicleSpawn spawn)
        {
            if (!VehicleBay.VehicleExists(spawn.VehicleID, out VehicleData data))
                return;
            foreach (LanguageSet set in Translation.EnumerateLanguageSets(players))
            {
                L.Log("Updating sign for " + set.Language);
                string val = Translation.TranslateVBS(spawn, data, set.Language);
                NetId id = spawn.LinkedSign.SignInteractable.GetNetId();
                while (set.MoveNext())
                {
                    try
                    {
                        string val2 = string.Format(val, UCWarfare.GetColorHex(set.Next != null && set.Next.Rank.Level >= data.RequiredLevel ? "vbs_level_low_enough" : "vbs_level_too_high"));
                        Data.SendChangeText.Invoke(id, ENetReliability.Unreliable, set.Next.Player.channel.owner.transportConnection, val2);
                    }
                    catch (FormatException)
                    {
                        Data.SendChangeText.Invoke(id, ENetReliability.Unreliable, set.Next.Player.channel.owner.transportConnection, val);
                        L.LogError("Formatting error in send vbs!");
                    }
                }
            }
        }
        private void UpdateSignInternal(SteamPlayer player, VehicleSpawn spawn)
        {
            if (!VehicleBay.VehicleExists(spawn.VehicleID, out VehicleData data))
                return;
            if (!Data.Languages.TryGetValue(player.playerID.steamID.m_SteamID, out string lang))
                lang = JSONMethods.DefaultLanguage;
            string val = Translation.TranslateVBS(spawn, data, lang);
            try
            {
                UCPlayer pl = UCPlayer.FromSteamPlayer(player);
                string val2 = string.Format(val, UCWarfare.GetColorHex(pl != null && pl.Rank.Level >= data.RequiredLevel ? "vbs_level_low_enough" : "vbs_level_too_high"));
                Data.SendChangeText.Invoke(spawn.LinkedSign.SignInteractable.GetNetId(), ENetReliability.Unreliable, player.transportConnection, val2);
            }
            catch (FormatException)
            {
                Data.SendChangeText.Invoke(spawn.LinkedSign.SignInteractable.GetNetId(), ENetReliability.Unreliable, player.transportConnection, val);
                L.LogError("Formatting error in send vbs!");
            }
        }
    }
    public class VehicleSpawnComponent : MonoBehaviour
    {
        VehicleSpawn parent;
        Coroutine timer;
        public bool isRespawning = false;
        public float respawnTimeRemaining = 0f;
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
            isRespawning = false;
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
                isRespawning = true;
                respawnTimeRemaining = data.RespawnTime;
                while (respawnTimeRemaining > 0)
                {
                    yield return new WaitForSeconds(1f);
                    parent.UpdateSign();
                    respawnTimeRemaining--;
                }
                if (parent.IsActive)
                {
                    parent.SpawnVehicle();
                }
                isRespawning = false;
            }
        }
    }
    public class SpawnedVehicleComponent : MonoBehaviour
    {
        private Coroutine timer;
        private Coroutine xploop;
        private InteractableVehicle Owner;
        private VehicleSpawn spawn;
        private VehicleData data;
        public bool hasBeenRequested = false;
        public bool isIdle = false;
        public float idleSecondsRemaining = -1f;
        public float nextIdleSecond = 0f;
        private Vector3 lastLoc;
        private bool lastIdleState;

        public void Initialize(InteractableVehicle vehicle, VehicleSpawn spawn)
        {
            Owner = vehicle;
            this.spawn = spawn;
            if (VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData data))
            {
                this.data = data;
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
                StopCoroutine(timer);
            }
            isIdle = false;
        }
        private IEnumerator<WaitForSeconds> IdleRespawnVehicle(VehicleData data)
        {
            isIdle = true;
            idleSecondsRemaining = data.RespawnTime;
            while (idleSecondsRemaining > 0f)
            {
                yield return new WaitForSeconds(1f);
                idleSecondsRemaining--;
                if ((isIdle || idleSecondsRemaining % 4 == 0) && Owner.anySeatsOccupied || PlayerManager.IsPlayerNearby(Owner.lockedOwner.m_SteamID, 150, Owner.transform.position))
                {
                    isIdle = false;
                    idleSecondsRemaining = data.RespawnTime;
                    nextIdleSecond = data.RespawnTime - 10f;
                }
                else if (!isIdle && (idleSecondsRemaining <= nextIdleSecond || idleSecondsRemaining <= 1f))
                {
                    isIdle = true;
                    nextIdleSecond = 0f;
                }
                if (lastLoc != transform.position || lastIdleState != isIdle)
                {
                    spawn.UpdateSign();
                    lastLoc = transform.position;
                    lastIdleState = isIdle;
                }
                else if (isIdle)
                {
                    spawn.UpdateSign();
                }
            }
            spawn.SpawnVehicle();
            VehicleBay.DeleteVehicle(Owner);
            isIdle = false;
        }
    }
}
