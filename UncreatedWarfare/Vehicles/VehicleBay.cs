using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Quests;
using UnityEngine;

namespace Uncreated.Warfare.Vehicles
{
    public class VehicleBay : JSONSaver<VehicleData>, IDisposable
    {
        private static Config<VehicleBayConfig> _config;
        public static VehicleBayConfig Config { get => _config.data; }

        public VehicleBay() : base(Data.VehicleStorage + "vehiclebay.json", VehicleData.Write, VehicleData.Read)
        {

            _config = new Config<VehicleBayConfig>(Data.VehicleStorage, "config.json");

#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            VehicleManager.onEnterVehicleRequested += OnVehicleEnterRequested;
            VehicleManager.onSwapSeatRequested += OnVehicleSwapSeatRequested;
            VehicleManager.onExitVehicleRequested += OnVehicleExitRequested;

            if (Whitelister.ActiveObjects != null)
            {
                for (int i = 0; i < ActiveObjects.Count; i++)
                {
                    VehicleData data = ActiveObjects[i];
                    if (data.Items != null)
                    {
                        for (int j = 0; j < data.Items.Length; j++)
                        {
                            if (!Whitelister.IsWhitelisted(data.Items[j], out _))
                                Whitelister.AddItem(data.Items[j]);
                        }
                    }
                }
            }
            else
            {
                L.LogWarning("Failed to check all vehicle bay items for whitelisting, Whitelister hasn't been loaded yet.");
            }
        }
        public static void OnPlayerJoinedQuestHandling(UCPlayer player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            for (int i = 0; i < ActiveObjects.Count; i++)
            {
                if (ActiveObjects[i].UnlockRequirements != null)
                {
                    VehicleData data = ActiveObjects[i];
                    for (int j = 0; j < data.UnlockRequirements.Length; j++)
                    {
                        if (data.UnlockRequirements[j] is QuestUnlockRequirement req && req.UnlockPresets != null && req.UnlockPresets.Length > 0 && !req.CanAccess(player))
                        {
                            if (Assets.find(req.QuestID) is QuestAsset quest)
                            {
                                player.Player.quests.sendAddQuest(quest.id);
                            }
                            else
                            {
                                L.LogWarning("Unknown quest id " + req.QuestID + " in vehicle requirement for " + data.VehicleID.ToString("N"));
                            }
                            for (int r = 0; r < req.UnlockPresets.Length; r++)
                            {
                                BaseQuestTracker? tracker = QuestManager.CreateTracker(player, req.UnlockPresets[r]);
                                if (tracker == null)
                                {
                                    L.LogWarning("Failed to create tracker for vehicle " + data.VehicleID.ToString("N") + ", player " + player.Name.PlayerName);
                                }
                            }
                        }
                    }
                }
            }
        }
        protected override string LoadDefaults() => "[]";
        public static void AddRequestableVehicle(InteractableVehicle vehicle)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            VehicleData data = new VehicleData(vehicle.asset.GUID);
            data.SaveMetaData(vehicle);
            AddObjectToSave(data);
        }
        public static void RemoveRequestableVehicle(Guid vehicleID) => RemoveWhere(vd => vd.VehicleID == vehicleID);
        public static bool VehicleExists(Guid vehicleID, out VehicleData vehicleData)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            for (int i = 0; i < ActiveObjects.Count; i++)
            {
                if (ActiveObjects[i].VehicleID == vehicleID)
                {
                    vehicleData = ActiveObjects[i];
                    return true;
                }
            }
            vehicleData = null!;
            return false;
        }
        public static void SetItems(Guid vehicleID, Guid[] newItems) =>         UpdateObjectsWhere(vd => vd.VehicleID == vehicleID, vd => vd.Items = newItems);
        public static void AddCrewmanSeat(Guid vehicleID, byte newSeatIndex) => UpdateObjectsWhere(vd => vd.VehicleID == vehicleID, vd => vd.CrewSeats.Add(newSeatIndex));
        public static void RemoveCrewmanSeat(Guid vehicleID, byte seatIndex) => UpdateObjectsWhere(vd => vd.VehicleID == vehicleID, vd => vd.CrewSeats.Remove(seatIndex));
        /// <summary>Level must be loaded.</summary>
        public static InteractableVehicle? SpawnLockedVehicle(Guid vehicleID, Vector3 position, Quaternion rotation, out uint instanceID)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            try
            {
                instanceID = 0;
                if (VehicleExists(vehicleID, out VehicleData vehicleData))
                {
                    if (Assets.find(vehicleID) is not VehicleAsset asset)
                    {
                        L.LogError("SpawnLockedVehicle: Unable to find vehicle asset of " + vehicleID.ToString());
                        return null;
                    }
                    InteractableVehicle vehicle = VehicleManager.spawnVehicleV2(asset.id, position, rotation);
                    if (vehicle == null) return null;
                    instanceID = vehicle.instanceID;

                    if (vehicleData.Metadata != null)
                    {
                        if (vehicleData.Metadata.TrunkItems != null)
                        {
                            foreach (KitItem k in vehicleData.Metadata.TrunkItems)
                            {
                                if (Assets.find(k.id) is ItemAsset iasset)
                                {
                                    Item item = new Item(iasset.id, k.amount, 100)
                                    { metadata = k.metadata };
                                    if (!vehicle.trunkItems.tryAddItem(item))
                                            ItemManager.dropItem(item, vehicle.transform.position, false, true, true);
                                }
                            }
                        }

                        if (vehicleData.Metadata.Barricades != null)
                        {
                            foreach (VBarricade vb in vehicleData.Metadata.Barricades)
                            {
                                if (Assets.find(vb.BarricadeID) is not ItemBarricadeAsset basset)
                                {
                                    L.LogError("SpawnLockedVehicle: Unable to find barricade asset of " + vb.BarricadeID.ToString());
                                    continue;
                                }
                                Barricade barricade = new Barricade(basset, asset.health, Convert.FromBase64String(vb.State));
                                Quaternion quarternion = Quaternion.Euler(vb.AngleX * 2, vb.AngleY * 2, vb.AngleZ * 2);
                                BarricadeManager.dropPlantedBarricade(vehicle.transform, barricade, new Vector3(vb.PosX, vb.PosY, vb.PosZ), quarternion, vb.OwnerID, vb.GroupID);
                            }
                        }
                    }

                    if (vehicle.asset.canBeLocked)
                    {
                        vehicle.tellLocked(CSteamID.Nil, CSteamID.Nil, true);

                        VehicleManager.ServerSetVehicleLock(vehicle, CSteamID.Nil, CSteamID.Nil, true);

                        vehicle.updateVehicle();
                        vehicle.updatePhysics();
                    }
                    return vehicle;
                }
                else
                {
                    L.Log($"VEHICLE SPAWN ERROR: {(Assets.find(vehicleID) is VehicleAsset va ? va.vehicleName : vehicleID.ToString("N"))} has not been registered in the VehicleBay.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                L.LogError("Error spawning vehicle: ");
                L.LogError(ex);
                instanceID = 0;
                return null;
            }
        }
        internal static bool OnQuestCompleted(UCPlayer player, Guid presetKey) => false;

        public static void ResupplyVehicleBarricades(InteractableVehicle vehicle, VehicleData vehicleData)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            VehicleBarricadeRegion? vehicleRegion = vehicle.FindRegionFromVehicleWithIndex(out ushort plant);
            if (vehicleRegion != null)
            {
                if (plant < ushort.MaxValue)
                {
                    for (int i = vehicleRegion.drops.Count - 1; i >= 0; i--)
                    {
                        if (i >= 0)
                        {
                            if (vehicleRegion.drops[i].interactable is InteractableStorage store)
                                store.despawnWhenDestroyed = true;

                            BarricadeManager.destroyBarricade(vehicleRegion.drops[i], 0, 0, plant);
                        }
                    }
                }
                if (vehicleData.Metadata != null && vehicleData.Metadata.Barricades != null)
                {
                    foreach (VBarricade vb in vehicleData.Metadata.Barricades)
                    {
                        Barricade barricade;
                        if (Assets.find(vb.BarricadeID) is ItemBarricadeAsset asset)
                        {
                            barricade = new Barricade(asset, asset.health, Convert.FromBase64String(vb.State));
                        }
                        else
                        {
                            L.LogError("ResupplyVehicleBarricades: Unable to find barricade asset of " + vb.BarricadeID.ToString());
                            continue;
                        }
                        Quaternion quarternion = Quaternion.Euler(vb.AngleX * 2, vb.AngleY * 2, vb.AngleZ * 2);
                        BarricadeManager.dropPlantedBarricade(vehicle.transform, barricade, new Vector3(vb.PosX, vb.PosY, vb.PosZ), quarternion, vb.OwnerID, vb.GroupID);
                    }
                    EffectManager.sendEffect(30, EffectManager.SMALL, vehicle.transform.position);
                }
            }
        }
        public static void DeleteVehicle(InteractableVehicle vehicle)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            vehicle.forceRemoveAllPlayers();
            BarricadeRegion reg = BarricadeManager.getRegionFromVehicle(vehicle);
            if (reg != null)
            {
                for (int b = 0; b < reg.drops.Count; b++)
                {
                    if (reg.drops[b].interactable is InteractableStorage storage)
                    {
                        storage.despawnWhenDestroyed = true;
                    }
                }
            }
            vehicle.trunkItems?.clear();
            VehicleManager.askVehicleDestroy(vehicle);
        }
        public static void DeleteAllVehiclesFromWorld()
        {
            for (int i = 0; i < VehicleManager.vehicles.Count; i++)
            {
                DeleteVehicle(VehicleManager.vehicles[i]);
            }
        }
        public static bool IsVehicleFull(InteractableVehicle vehicle, bool excludeDriver = false)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            for (byte seat = 0; seat < vehicle.passengers.Length; seat++)
            {
                if (seat == 0 && excludeDriver)
                    continue;

                Passenger passenger = vehicle.passengers[seat];

                if (passenger.player == null)
                {
                    return true;
                }
            }
            return true;
        }
        public static bool TryGetFirstNonCrewSeat(InteractableVehicle vehicle, VehicleData data, out byte seat)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            for (seat = 0; seat < vehicle.passengers.Length; seat++)
            {
                Passenger passenger = vehicle.passengers[seat];

                if (passenger.player == null && !data.CrewSeats.Contains(seat))
                {
                    return true;
                }
            }
            seat = 0;
            return false;
        }
        public static bool TryGetFirstNonDriverSeat(InteractableVehicle vehicle, out byte seat)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            for (seat = 0; seat < vehicle.passengers.Length; seat++)
            {
                Passenger passenger = vehicle.passengers[seat];

                if (seat != 0 && passenger.player == null)
                {
                    return true;
                }
            }
            seat = 0;
            return false;
        }
        public static bool IsOwnerInVehicle(InteractableVehicle vehicle, UCPlayer owner)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (vehicle.lockedOwner == CSteamID.Nil || owner == null) return false;

            foreach (Passenger passenger in vehicle.passengers)
            {
                if (passenger.player != null && owner.CSteamID == passenger.player.playerID.steamID)
                {
                    return true;
                }
            }
            return false;
        }
        public static int CountCrewmen(InteractableVehicle vehicle, VehicleData data)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            int count = 0;
            for (byte seat = 0; seat < vehicle.passengers.Length; seat++)
            {
                Passenger passenger = vehicle.passengers[seat];

                if (data.CrewSeats.Contains(seat) && passenger.player != null)
                {
                    count++;
                }
            }
            return count;
        }
        private void OnVehicleExitRequested(Player player, InteractableVehicle vehicle, ref bool shouldAllow, ref Vector3 pendingLocation, ref float pendingYaw)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            UCPlayer? ucplayer = UCPlayer.FromPlayer(player);
            if (ucplayer == null || FOBManager.config.data.Buildables.Exists(e => e.type == EBuildableType.EMPLACEMENT && e.structureID == vehicle.asset.GUID)) return;
            if (!ucplayer.OnDuty() && pendingLocation.y - F.GetHeightAt2DPoint(pendingLocation.x, pendingLocation.z) > UCWarfare.Config.MaxVehicleHeightToLeave)
            {
                player.SendChat("vehicle_too_high");
                shouldAllow = false;
                return;
            }
            if (ucplayer != null && KitManager.KitExists(ucplayer.KitName, out Kit kit))
            {
                if (kit.Class == EClass.LAT || kit.Class == EClass.HAT)
                {
                    ucplayer.Player.equipment.dequip();
                }
            }
        }
        private void OnVehicleEnterRequested(Player nelsonplayer, InteractableVehicle vehicle, ref bool shouldAllow)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (vehicle == null)
            {
                L.LogError("Vehicle not specified");
                return;
            }
            try
            {
                if (Data.Gamemode.State != EState.ACTIVE && Data.Gamemode.State != EState.STAGING)
                {
                    nelsonplayer.SendChat("gamemode is not active");
                    shouldAllow = false;
                    return;
                }
                if (!vehicle.asset.canBeLocked)
                {
                    EventFunctions.OnEnterVehicle(nelsonplayer, vehicle, ref shouldAllow);
                    return;
                }
                UCPlayer? player = UCPlayer.FromPlayer(nelsonplayer);
                if (player == null)
                {
                    EventFunctions.OnEnterVehicle(nelsonplayer, vehicle, ref shouldAllow);
                    return;
                }
                if (!player.OnDuty() && Data.Gamemode.State == EState.STAGING && Data.Is<IStagingPhase>(out _) && (!Data.Is(out IAttackDefense atk) || player.GetTeam() == atk.AttackingTeam))
                {
                    player.SendChat("vehicle_staging");
                    shouldAllow = false;
                    return;
                }
                if (Data.Is(out IRevives r) && r.ReviveManager.DownedPlayers.ContainsKey(nelsonplayer.channel.owner.playerID.steamID.m_SteamID))
                {
                    shouldAllow = false;
                    return;
                }

                if (!KitManager.HasKit(player, out Kit kit))
                {
                    player.SendChat("vehicle_no_kit");
                    shouldAllow = false;
                    return;
                }
                
                EventFunctions.OnEnterVehicle(nelsonplayer, vehicle, ref shouldAllow);
            }
            catch (Exception ex)
            {
                L.LogError("Error in OnVehicleEnterRequested: ");
                L.LogError(ex);
                if (shouldAllow)
                    EventFunctions.OnEnterVehicle(nelsonplayer, vehicle, ref shouldAllow);
            }
        }
        private void OnVehicleSwapSeatRequested(Player nelsonplayer, InteractableVehicle vehicle, ref bool shouldAllow, byte fromSeatIndex, ref byte toSeatIndex)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            try
            {
                if (vehicle == null) return;
                if (!VehicleExists(vehicle.asset.GUID, out VehicleData vehicleData))
                    return;

                UCPlayer? enterer = UCPlayer.FromPlayer(nelsonplayer);
                if (enterer == null)
                    return;

                if (vehicleData.Type == EVehicleType.EMPLACEMENT && toSeatIndex == 0)
                {
                    shouldAllow = false;
                }
                else
                {
                    if (!KitManager.HasKit(enterer, out Kit kit))
                    {
                        enterer.SendChat("vehicle_no_kit");
                        shouldAllow = false;
                        return;
                    }

                    UCPlayer? owner = UCPlayer.FromCSteamID(vehicle.lockedOwner);

                    if (vehicleData.CrewSeats.Contains(toSeatIndex) && vehicleData.RequiredClass != EClass.NONE) // vehicle requires crewman or pilot
                    {
                        if (enterer.KitClass == vehicleData.RequiredClass || enterer.OnDuty())
                        {
                            if (toSeatIndex == 0) // if a crewman is trying to enter the driver's seat
                            {
                                bool canEnterDriverSeat = owner == null || 
                                    enterer == owner || 
                                    enterer.OnDuty() ||
                                    IsOwnerInVehicle(vehicle, owner) || 
                                    (owner != null && owner.Squad != null && owner.Squad.Members.Contains(enterer) || 
                                    (owner!.Position - vehicle.transform.position).sqrMagnitude > Math.Pow(200, 2)) ||
                                    (vehicleData.Type == EVehicleType.LOGISTICS && FOB.GetNearestFOB(vehicle.transform.position, EFOBRadius.FULL_WITH_BUNKER_CHECK, vehicle.lockedGroup.m_SteamID) != null);

                                if (!canEnterDriverSeat)
                                {
                                    if (owner!.Squad == null)
                                        enterer.Message("vehicle_wait_for_owner", owner.CharacterName);
                                    else
                                        enterer.Message("vehicle_wait_for_owner_or_squad", owner.CharacterName, owner.Squad.Name);

                                    shouldAllow = false;
                                }
                            }
                            else // if the player is trying to switch to a gunner's seat
                            {
                                if (!(F.IsInMain(vehicle.transform.position) || enterer.OnDuty())) // if player is trying to switch to a gunner's seat outside of main
                                {
                                    if (vehicle.passengers[0].player is null) // if they have no driver
                                    {
                                        enterer.Message("vehicle_need_driver");
                                        shouldAllow = false;
                                    }
                                    else if (enterer.CSteamID == vehicle.passengers[0].player.playerID.steamID) // if they are the driver
                                    {
                                        enterer.Message("vehicle_cannot_abandon_driver");
                                        shouldAllow = false;
                                    }
                                }
                            }
                        }
                        else
                        {
                            enterer.Message("vehicle_not_valid_kit", vehicleData.RequiredClass.ToString().ToUpper());
                            shouldAllow = false;
                        }
                    }
                    else
                    {
                        if (toSeatIndex == 0)
                        {
                            bool canEnterDriverSeat = owner is null || enterer == owner || enterer.OnDuty() || IsOwnerInVehicle(vehicle, owner) || (owner is not null && owner.Squad != null && owner.Squad.Members.Contains(enterer));

                            if (!canEnterDriverSeat)
                            {
                                if (owner!.Squad == null)
                                    enterer.Message("vehicle_wait_for_owner", owner.CharacterName);
                                else
                                    enterer.Message("vehicle_wait_for_owner_or_squad", owner.CharacterName, owner.Squad.Name);

                                shouldAllow = false;
                            }
                        }
                    }
                }

                EventFunctions.OnVehicleSwapSeatRequested(nelsonplayer, vehicle, ref shouldAllow, fromSeatIndex, ref toSeatIndex);
            }
            catch (Exception ex)
            {
                L.LogError("Error in OnVehicleSeatChanged: ");
                L.LogError(ex);
            }
        }

        public void Dispose()
        {
            VehicleManager.onEnterVehicleRequested -= OnVehicleEnterRequested;
            VehicleManager.onSwapSeatRequested -= OnVehicleSwapSeatRequested;
            VehicleManager.onExitVehicleRequested -= OnVehicleExitRequested;
        }
    }
    public enum EDelayType
    {
        NONE            = 0,
        TIME            = 1,
        /// <summary><see cref="VehicleData.Team"/> must be set.</summary>
        FLAG            = 2,
        /// <summary><see cref="VehicleData.Team"/> must be set.</summary>
        FLAG_PERCENT = 3,
        OUT_OF_STAGING  = 4
    }
    public struct Delay : IJsonReadWrite
    {
        public static readonly Delay Nil = new Delay(EDelayType.NONE, float.NaN, null);
        [JsonIgnore]
        public bool IsNil => value == float.NaN;
        public EDelayType type;
        public string? gamemode;
        public float value;
        public Delay(EDelayType type, float value, string? gamemode = null)
        {
            this.type = type;
            this.value = value;
            this.gamemode = gamemode;
        }

        public override string ToString() =>
            $"{type} Delay, {(string.IsNullOrEmpty(gamemode) ? "any" : gamemode)} " +
            $"gamemode{(type == EDelayType.NONE || type == EDelayType.OUT_OF_STAGING ? string.Empty : $" Value: {value}")}";
        public void WriteJson(Utf8JsonWriter writer)
        {
            writer.WriteNumber(nameof(type), (int)type);
            writer.WriteString(nameof(gamemode), gamemode);
            writer.WriteNumber(nameof(value), value);
        }
        public void ReadJson(ref Utf8JsonReader reader)
        {
            while (reader.TokenType == JsonTokenType.PropertyName || (reader.Read() && reader.TokenType == JsonTokenType.PropertyName))
            {
                string? prop = reader.GetString();
                if (reader.Read() && prop != null)
                {
                    switch (prop)
                    {
                        case nameof(type):
                            if (reader.TryGetInt32(out int i))
                                type = (EDelayType)i;
                            break;
                        case nameof(gamemode):
                            if (reader.TokenType == JsonTokenType.Null) gamemode = null;
                            else gamemode = reader.GetString();
                            break;
                        case nameof(value):
                            reader.TryGetSingle(out value);
                            break;
                    }
                }
            }
        }
    }
    public class VehicleData : IJsonReadWrite
    {
        [JsonSettable]
        public string Name;
        [JsonSettable]
        public Guid VehicleID;
        [JsonSettable]
        public ulong Team;
        [JsonSettable]
        public ushort RespawnTime;
        [JsonSettable]
        public ushort TicketCost;
        [JsonSettable]
        public ushort CreditCost;
        [JsonSettable]
        public ushort Cooldown;
        [JsonSettable]
        public EBranch Branch;
        [JsonSettable]
        public EClass RequiredClass;
        [JsonSettable]
        public byte RearmCost;
        [JsonSettable]
        public EVehicleType Type;
        [JsonSettable]
        public bool RequiresSL;
        [JsonSettable]
        public ushort UnlockLevel;
        public BaseUnlockRequirement[] UnlockRequirements;
        public Guid[] Items;
        public Delay[] Delays;
        public List<byte> CrewSeats;
        public MetaSave? Metadata;
        public VehicleData(Guid vehicleID)
        {
            VehicleID = vehicleID;
            Team = 0;
            RespawnTime = 600;
            TicketCost = 0;
            CreditCost = 0;
            Cooldown = 0;
            if (Assets.find(vehicleID) is VehicleAsset va)
            {
                Name = va.name;
                if (va.engine == EEngine.PLANE || va.engine == EEngine.HELICOPTER || va.engine == EEngine.BLIMP)
                    Branch = EBranch.AIRFORCE;
                else if (va.engine == EEngine.BOAT)
                    Branch = (EBranch)5; // navy
                else
                    Branch = EBranch.DEFAULT;
            }
            else Branch = EBranch.DEFAULT;
            RequiredClass = EClass.NONE;
            UnlockRequirements = new BaseUnlockRequirement[0];
            RearmCost = 3;
            Type = EVehicleType.NONE;
            RequiresSL = false;
            UnlockLevel = 0;
            Items = new Guid[0];
            CrewSeats = new List<byte>();
            Metadata = null;
            Delays = new Delay[0];
        }
        public VehicleData()
        {
            Name = "";
            VehicleID = Guid.Empty;
            Team = 0;
            UnlockRequirements = new BaseUnlockRequirement[0];
            RespawnTime = 600;
            TicketCost = 0;
            CreditCost = 0;
            Cooldown = 0;
            Branch = EBranch.DEFAULT;
            RequiredClass = EClass.NONE;
            RearmCost = 3;
            Type = EVehicleType.NONE;
            RequiresSL = false;
            UnlockLevel = 0;
            Items = new Guid[0];
            CrewSeats = new List<byte>();
            Metadata = null;
            Delays = new Delay[0];
        }
        public void AddDelay(EDelayType type, float value, string? gamemode = null)
        {
            int index = -1;
            for (int i = 0; i < Delays.Length; i++)
            {
                ref Delay del = ref Delays[i];
                if (del.type == type && del.value == value && (del.gamemode == gamemode || (string.IsNullOrEmpty(del.gamemode) && string.IsNullOrEmpty(gamemode))))
                {
                    index = i;
                    break;
                }
            }
            if (index == -1)
            {
                Delay del = new Delay(type, value, gamemode);
                Delay[] old = Delays;
                Delays = new Delay[old.Length + 1];
                if (old.Length > 0)
                {
                    Array.Copy(old, 0, Delays, 0, old.Length);
                    Delays[Delays.Length - 1] = del;
                }
                else
                {
                    Delays[0] = del;
                }
            }
        }
        public bool RemoveDelay(EDelayType type, float value, string? gamemode = null)
        {
            if (Delays.Length == 0) return false;
            int index = -1;
            for (int i = 0; i < Delays.Length; i++)
            {
                ref Delay del = ref Delays[i];
                if (del.type == type && del.value == value && (del.gamemode == gamemode || (string.IsNullOrEmpty(del.gamemode) && string.IsNullOrEmpty(gamemode))))
                {
                    index = i;
                    break;
                }
            }
            if (index == -1) return false;
            Delay[] old = Delays;
            Delays = new Delay[old.Length - 1];
            if (old.Length == 1) return true;
            if (index != 0)
                Array.Copy(old, 0, Delays, 0, index);
            Array.Copy(old, index + 1, Delays, index, old.Length - index - 1);
            return true;
        }
        public bool HasDelayType(EDelayType type)
        {
            string gm = Data.Gamemode.Name;
            for (int i = 0; i < Delays.Length; i++)
            {
                ref Delay del = ref Delays[i];
                if (!string.IsNullOrEmpty(del.gamemode) && !gm.Equals(del.gamemode, StringComparison.OrdinalIgnoreCase)) continue;
                if (del.type == type) return true;
            }
            return false;
        }
        public bool IsDelayedType(EDelayType type)
        {
            string gm = Data.Gamemode.Name;
            for (int i = 0; i < Delays.Length; i++)
            {
                ref Delay del = ref Delays[i];
                if (!string.IsNullOrEmpty(del.gamemode))
                {
                    string gamemode = del.gamemode!;
                    bool blacklist = false;
                    if (gamemode[0] == '!')
                    {
                        blacklist = true;
                        gamemode = gamemode.Substring(1);
                    }

                    if (gm.Equals(gamemode, StringComparison.OrdinalIgnoreCase))
                    {
                        if (blacklist) continue;
                    }
                    else if (!blacklist) continue;
                }
                if (del.type == type)
                {
                    switch (type)
                    {
                        case EDelayType.NONE:
                            return false;
                        case EDelayType.TIME:
                            if (TimeDelayed(ref del))
                                return true;
                            break;
                        case EDelayType.FLAG:
                            if (FlagDelayed(ref del))
                                return true;
                            break;
                        case EDelayType.FLAG_PERCENT:
                            if (FlagPercentDelayed(ref del))
                                return true;
                            break;
                        case EDelayType.OUT_OF_STAGING:
                            if (StagingDelayed(ref del))
                                return true;
                            break;
                    }
                }
            }
            return false;
        }
        public bool IsDelayed(out Delay delay)
        {
            delay = Delay.Nil;
            string gm = Data.Gamemode.Name;
            if (Delays == null || Delays.Length == 0) return false;
            bool anyVal = false;
            bool isNoneYet = false;
            for (int i = Delays.Length - 1; i >= 0; i--)
            {
                ref Delay del = ref Delays[i];
                bool universal = string.IsNullOrEmpty(del.gamemode);
                if (!universal)
                {
                    string gamemode = del.gamemode!; // !TeamCTF
                    bool blacklist = false;
                    if (gamemode[0] == '!') // true
                    {
                        blacklist = true;
                        gamemode = gamemode.Substring(1); // TeamCTF
                    }

                    if (gm.Equals(gamemode, StringComparison.OrdinalIgnoreCase)) // false
                    {
                        if (blacklist) continue;
                    }
                    else if (!blacklist) continue; // false
                    universal = true;
                }
                if (universal && anyVal) continue;
                switch (del.type)
                {
                    case EDelayType.NONE:
                        if (!universal)
                        {
                            delay = del;
                            isNoneYet = true;
                        }
                        break;
                    case EDelayType.TIME:
                        if ((!universal || !isNoneYet) && TimeDelayed(ref del))
                        {
                            delay = del;
                            if (!universal) return true;
                            anyVal = true;
                        }
                        break;
                    case EDelayType.FLAG:
                        if ((!universal || !isNoneYet) && FlagDelayed(ref del))
                        {
                            delay = del;
                            if (!universal) return true;
                            anyVal = true;
                        }
                        break;
                    case EDelayType.FLAG_PERCENT:
                        if ((!universal || !isNoneYet) && FlagPercentDelayed(ref del))
                        {
                            delay = del;
                            if (!universal) return true;
                            anyVal = true;
                        }
                        break;
                    case EDelayType.OUT_OF_STAGING:
                        if ((!universal || !isNoneYet) && StagingDelayed(ref del))
                        {
                            delay = del;
                            if (!universal) return true;
                            anyVal = true;
                        }
                        break;
                }
            }
            return anyVal;
        }
        private bool TimeDelayed(ref Delay delay) => delay.value > Data.Gamemode.SecondsSinceStart;
        private bool FlagDelayed(ref Delay delay) => FlagDelayed(ref delay, false);
        private bool FlagPercentDelayed(ref Delay delay) => FlagDelayed(ref delay, true);
        private bool FlagDelayed(ref Delay delay, bool percent)
        {
            if (Data.Is(out Invasion inv))
            {
                int ct = percent ? Mathf.RoundToInt(inv.Rotation.Count * delay.value / 100f) : Mathf.RoundToInt(delay.value);
                if (Team == 1)
                {
                    if (inv.AttackingTeam == 1)
                        return inv.ObjectiveT1Index < ct;
                    else
                        return inv.Rotation.Count - inv.ObjectiveT2Index - 1 < ct;
                }
                else if (Team == 2)
                {
                    if (inv.AttackingTeam == 2)
                        return inv.Rotation.Count - inv.ObjectiveT2Index - 1 < ct;
                    else
                        return inv.ObjectiveT1Index < ct;
                }
                return false;
            }
            else if (Data.Is(out IFlagTeamObjectiveGamemode fr))
            {
                int ct = percent ? Mathf.RoundToInt(fr.Rotation.Count * delay.value / 100f) : Mathf.RoundToInt(delay.value);
                int i2 = GetHighestObjectiveIndex(Team, fr);
                return (Team == 1 && i2 < ct) ||
                       (Team == 2 && fr.Rotation.Count - i2 - 1 < ct);
            }
            else if (Data.Is(out Insurgency ins))
            {
                int ct = percent ? Mathf.RoundToInt(ins.Caches.Count * delay.value / 100f) : Mathf.RoundToInt(delay.value);
                return ins.Caches != null && ins.CachesDestroyed < ct;
            }
            return false;
        }
        private bool StagingDelayed(ref Delay delay) => Data.Is(out IStagingPhase sp) && sp.State == EState.STAGING;
        private int GetHighestObjectiveIndex(ulong team, IFlagTeamObjectiveGamemode gm)
        {
            if (team == 1)
            {
                for (int i = 0; i < gm.Rotation.Count; i++)
                {
                    if (!gm.Rotation[i].HasBeenCapturedT1)
                        return i;
                }
                return 0;
            }
            else if (team == 2)
            {
                for (int i = gm.Rotation.Count - 1; i >= 0; i--)
                {
                    if (!gm.Rotation[i].HasBeenCapturedT2)
                        return i;
                }
                return gm.Rotation.Count - 1;
            }
            return -1;
        }
        public IEnumerable<VehicleSpawn> EnumerateSpawns()
        {
            for (int i = 0; i < VehicleSpawner.ActiveObjects.Count; i++)
            {
                if (VehicleSpawner.ActiveObjects[i].VehicleID == VehicleID)
                    yield return VehicleSpawner.ActiveObjects[i];
            }
        }
        public List<VehicleSpawn> GetSpawners()
        {
            List<VehicleSpawn> rtn = new List<VehicleSpawn>();
            for (int i = 0; i < VehicleSpawner.ActiveObjects.Count; i++)
            {
                if (VehicleSpawner.ActiveObjects[i].VehicleID == VehicleID)
                    rtn.Add(VehicleSpawner.ActiveObjects[i]);
            }
            return rtn;
        }
        public void SaveMetaData(InteractableVehicle vehicle)
        {
            List<VBarricade>? barricades = null;
            List<KitItem>? trunk = null;

            if (vehicle.trunkItems.items.Count > 0)
            {
                trunk = new List<KitItem>();

                foreach (ItemJar jar in vehicle.trunkItems.items)
                {
                    if (Assets.find(EAssetType.ITEM, jar.item.id) is ItemAsset asset)
                    {
                        trunk.Add(new KitItem(
                            asset.GUID,
                            jar.x,
                            jar.y,
                            jar.rot,
                            jar.item.metadata,
                            jar.item.amount,
                            0
                        ));
                    }
                }
            }

            VehicleBarricadeRegion vehicleRegion = BarricadeManager.findRegionFromVehicle(vehicle);
            if (vehicleRegion != null)
            {
                barricades = new List<VBarricade>();
                for (int i = 0; i < vehicleRegion.drops.Count; i++)
                {
                    SDG.Unturned.BarricadeData bdata = vehicleRegion.drops[i].GetServersideData();
                    barricades.Add(new VBarricade(bdata.barricade.asset.GUID, bdata.barricade.asset.health, 0, Teams.TeamManager.AdminID, bdata.point.x, bdata.point.y,
                        bdata.point.z, bdata.angle_x, bdata.angle_y, bdata.angle_z, Convert.ToBase64String(bdata.barricade.state)));
                }
            }

            if (barricades is not null || trunk is not null)
                Metadata = new MetaSave(barricades, trunk);
        }
        public static void Write(VehicleData data, Utf8JsonWriter writer) => data.WriteJson(writer);
        public static VehicleData Read(ref Utf8JsonReader reader)
        {
            VehicleData data = new VehicleData();
            data.ReadJson(ref reader);
            return data;
        }
        public void WriteJson(Utf8JsonWriter writer)
        {
            writer.WriteString(nameof(Name), Name);
            writer.WriteString(nameof(VehicleID), VehicleID);
            writer.WriteNumber(nameof(Team), Team);
            writer.WriteNumber(nameof(RespawnTime), RespawnTime);
            writer.WriteNumber(nameof(TicketCost), TicketCost);
            writer.WriteNumber(nameof(Cooldown), Cooldown);
            writer.WriteNumber(nameof(Branch), (int)Branch);
            writer.WriteNumber(nameof(RequiredClass), (int)RequiredClass);
            writer.WriteNumber(nameof(RearmCost), RearmCost);
            writer.WriteNumber(nameof(Type), (int)Type);
            writer.WriteBoolean(nameof(RequiresSL), RequiresSL);
            writer.WriteNumber(nameof(CreditCost), CreditCost);
            writer.WriteNumber(nameof(UnlockLevel), UnlockLevel);

            writer.WritePropertyName(nameof(UnlockRequirements));
            writer.WriteStartArray();
            for (int i = 0; i < UnlockRequirements.Length; i++)
            {
                writer.WriteStartObject();
                BaseUnlockRequirement.Write(writer, UnlockRequirements[i]);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WritePropertyName(nameof(Items));
            writer.WriteStartArray();
            for (int i = 0; i < Items.Length; i++)
                writer.WriteStringValue(Items[i]);
            writer.WriteEndArray();

            writer.WritePropertyName(nameof(Delays));
            writer.WriteStartArray();
            for (int i = 0; i < Delays.Length; i++)
            {
                writer.WriteStartObject();
                ref Delay del = ref Delays[i];
                del.WriteJson(writer);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WritePropertyName(nameof(CrewSeats));
            writer.WriteStartArray();
            for (int i = 0; i < CrewSeats.Count; i++)
                writer.WriteNumberValue(CrewSeats[i]);
            writer.WriteEndArray();

            writer.WritePropertyName(nameof(Metadata));
            if (Metadata == null) writer.WriteNullValue();
            else
            {
                writer.WriteStartObject();
                Metadata.WriteJson(writer);
                writer.WriteEndObject();
            }
        }
        public void ReadJson(ref Utf8JsonReader reader)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) return;
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string? prop = reader.GetString();
                    if (reader.Read() && prop != null)
                    {
                        switch (prop)
                        {
                            case nameof(Name):
                                Name = reader.GetString()!;
                                break;
                            case nameof(VehicleID):
                                VehicleID = reader.GetGuid();
                                break;
                            case nameof(Team):
                                Team = reader.GetUInt64();
                                break;
                            case nameof(RespawnTime):
                                RespawnTime = reader.GetUInt16();
                                break;
                            case nameof(TicketCost):
                                TicketCost = reader.GetUInt16();
                                break;
                            case nameof(CreditCost):
                                CreditCost = reader.GetUInt16();
                                break;
                            case nameof(Cooldown):
                                Cooldown = reader.GetUInt16();
                                break;
                            case nameof(Branch):
                                Branch = (EBranch)reader.GetByte();
                                break;
                            case nameof(RequiredClass):
                                RequiredClass = (EClass)reader.GetByte();
                                break;
                            case nameof(RearmCost):
                                RearmCost = reader.GetByte();
                                break;
                            case nameof(Type):
                                Type = (EVehicleType)reader.GetByte();
                                break;
                            case nameof(RequiresSL):
                                RequiresSL = reader.TokenType == JsonTokenType.True;
                                break;
                            case nameof(UnlockLevel):
                                UnlockLevel = reader.GetUInt16();
                                break;
                            case nameof(UnlockRequirements):
                                if (reader.TokenType == JsonTokenType.StartArray)
                                {
                                    List<BaseUnlockRequirement> reqs = new List<BaseUnlockRequirement>(2);
                                    while (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                                    {
                                        BaseUnlockRequirement? bur = BaseUnlockRequirement.Read(ref reader);
                                        while (reader.TokenType != JsonTokenType.EndObject) if (!reader.Read()) break;
                                        if (bur != null) reqs.Add(bur);
                                    }
                                    UnlockRequirements = reqs.ToArray();
                                    while (reader.TokenType != JsonTokenType.EndArray) if (!reader.Read()) break;
                                }
                                break;
                            case nameof(Items):
                                if (reader.TokenType == JsonTokenType.StartArray)
                                {
                                    List<Guid> items = new List<Guid>(16);
                                    while (reader.Read() && reader.TokenType == JsonTokenType.String)
                                    {
                                        if (reader.TryGetGuid(out Guid guid))
                                            items.Add(guid);
                                    }
                                    Items = items.ToArray();
                                    while (reader.TokenType != JsonTokenType.EndArray) if (!reader.Read()) break;
                                }
                                break;
                            case nameof(Delays):
                                if (reader.TokenType == JsonTokenType.StartArray)
                                {
                                    List<Delay> delays = new List<Delay>(1);
                                    while (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                                    {
                                        Delay delay = new Delay();
                                        delay.ReadJson(ref reader);
                                        while (reader.TokenType != JsonTokenType.EndObject) if (!reader.Read()) break;
                                        delays.Add(delay);
                                    }
                                    Delays = delays.ToArray();
                                    while (reader.TokenType != JsonTokenType.EndArray) if (!reader.Read()) break;
                                }
                                break;
                            case nameof(CrewSeats):
                                if (reader.TokenType == JsonTokenType.StartArray)
                                {
                                    CrewSeats = new List<byte>(0);
                                    while (reader.Read() && reader.TokenType == JsonTokenType.Number)
                                        CrewSeats.Add(reader.GetByte());
                                    while (reader.TokenType != JsonTokenType.EndArray) if (!reader.Read()) break;
                                }
                                break;
                            case nameof(Metadata):
                                if (reader.TokenType == JsonTokenType.Null)
                                    Metadata = null;
                                else if (reader.TokenType == JsonTokenType.StartObject)
                                {
                                    Metadata = new MetaSave(null, null);
                                    Metadata.ReadJson(ref reader);
                                }
                                break;
                        }
                    }
                }
            }
        }
        public string GetCostLine(UCPlayer ucplayer)
        {
            if (UnlockRequirements == null || UnlockRequirements.Length == 0)
                return string.Empty;
            else
            {
                for (int i = 0; i < UnlockRequirements.Length; i++)
                {
                    BaseUnlockRequirement req = UnlockRequirements[i];
                    if (req.CanAccess(ucplayer))
                        continue;
                    return req.GetSignText(ucplayer);
                }
            }
            return string.Empty;
        }
    }

    public class MetaSave : IJsonReadWrite
    {
        public List<VBarricade>? Barricades;
        public List<KitItem>? TrunkItems;
        public MetaSave(List<VBarricade>? barricades, List<KitItem>? trunkItems)
        {
            Barricades = barricades;
            TrunkItems = trunkItems;
        }
        public void ReadJson(ref Utf8JsonReader reader)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) return;
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string prop = reader.GetString()!;
                    if (reader.Read())
                    {
                        switch (prop)
                        {
                            case nameof(TrunkItems):
                                if (reader.TokenType == JsonTokenType.StartArray)
                                {
                                    TrunkItems = new List<KitItem>(0);
                                    while (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                                    {
                                        KitItem item = new KitItem();
                                        item.ReadJson(ref reader);
                                        while (reader.TokenType != JsonTokenType.EndObject) if (!reader.Read()) break;
                                        TrunkItems.Add(item);
                                    }
                                    while (reader.TokenType != JsonTokenType.EndArray) if (!reader.Read()) break;
                                }
                                else if (reader.TokenType == JsonTokenType.Null)
                                    TrunkItems = null;
                                break;
                            case nameof(Barricades):
                                if (reader.TokenType == JsonTokenType.StartArray)
                                {
                                    Barricades = new List<VBarricade>(0);
                                    while (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                                    {
                                        VBarricade barricade = new VBarricade();
                                        barricade.ReadJson(ref reader);
                                        while (reader.TokenType != JsonTokenType.EndObject) if (!reader.Read()) break;
                                        Barricades.Add(barricade);
                                    }
                                    while (reader.TokenType != JsonTokenType.EndArray) if (!reader.Read()) break;
                                }
                                else if (reader.TokenType == JsonTokenType.Null)
                                    Barricades = null;
                                break;
                        }
                    }
                }
            }
        }
        public void WriteJson(Utf8JsonWriter writer)
        {
            writer.WritePropertyName(nameof(TrunkItems));
            if (TrunkItems != null)
            {
                writer.WriteStartArray();
                for (int i = 0; i < TrunkItems.Count; i++)
                {
                    writer.WriteStartObject();
                    TrunkItems[i].WriteJson(writer);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            else
                writer.WriteNullValue();

            writer.WritePropertyName(nameof(Barricades));
            if (Barricades != null)
            {
                writer.WriteStartArray();
                for (int i = 0; i < Barricades.Count; i++)
                {
                    writer.WriteStartObject();
                    Barricades[i].WriteJson(writer);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            else
                writer.WriteNullValue();
        }
    }

    public class VBarricade : IJsonReadWrite
    {
        public Guid BarricadeID;
        public ushort Health;
        public ulong OwnerID;
        public ulong GroupID;
        public float PosX;
        public float PosY;
        public float PosZ;
        public float AngleX;
        public float AngleY;
        public float AngleZ;
        public string State;
        internal VBarricade() { }
        public VBarricade(Guid barricadeID, ushort health, ulong ownerID, ulong groupID, float posX, float posY, float posZ, float angleX, float angleY, float angleZ, string state)
        {
            BarricadeID = barricadeID;
            Health = health;
            OwnerID = ownerID;
            GroupID = groupID;
            PosX = posX;
            PosY = posY;
            PosZ = posZ;
            AngleX = angleX;
            AngleY = angleY;
            AngleZ = angleZ;
            State = state;
        }
        public void WriteJson(Utf8JsonWriter writer)
        {
            writer.WriteString(nameof(BarricadeID), BarricadeID);
            writer.WriteNumber(nameof(Health), Health);
            writer.WriteNumber(nameof(OwnerID), OwnerID);
            writer.WriteNumber(nameof(GroupID), GroupID);
            writer.WriteNumber(nameof(PosX), PosX);
            writer.WriteNumber(nameof(PosY), PosY);
            writer.WriteNumber(nameof(PosZ), PosZ);
            writer.WriteNumber(nameof(AngleX), AngleX);
            writer.WriteNumber(nameof(AngleY), AngleY);
            writer.WriteNumber(nameof(AngleZ), AngleZ);
            writer.WriteString(nameof(State), State);
        }
        public void ReadJson(ref Utf8JsonReader reader)
        {
            while (reader.TokenType == JsonTokenType.PropertyName || (reader.Read() && reader.TokenType == JsonTokenType.PropertyName))
            {
                string? prop = reader.GetString();
                if (reader.Read() && prop != null)
                {
                    switch (prop)
                    {
                        case nameof(BarricadeID):
                            reader.TryGetGuid(out BarricadeID);
                            break;
                        case nameof(Health):
                            reader.TryGetUInt16(out Health);
                            break;
                        case nameof(OwnerID):
                            reader.TryGetUInt64(out OwnerID);
                            break;
                        case nameof(GroupID):
                            reader.TryGetUInt64(out GroupID);
                            break;
                        case nameof(PosX):
                            reader.TryGetSingle(out PosX);
                            break;
                        case nameof(PosY):
                            reader.TryGetSingle(out PosY);
                            break;
                        case nameof(PosZ):
                            reader.TryGetSingle(out PosZ);
                            break;
                        case nameof(AngleX):
                            reader.TryGetSingle(out AngleX);
                            break;
                        case nameof(AngleY):
                            reader.TryGetSingle(out AngleY);
                            break;
                        case nameof(AngleZ):
                            reader.TryGetSingle(out AngleZ);
                            break;
                        case nameof(State):
                            State = reader.GetString() ?? string.Empty;
                            break;
                    }
                }
            }
        }
    }

    [Translatable]
    public enum EVehicleType
    {
        NONE,
        HUMVEE,
        TRANSPORT,
        SCOUT_CAR,
        LOGISTICS,
        APC,
        IFV,
        MBT,
        HELI_TRANSPORT,
        HELI_ATTACK,
        JET,
        EMPLACEMENT,
    }

    public class VehicleBayConfig : ConfigData
    {
        public ushort MissileWarningID;
        public ushort MissileWarningDriverID;
        public ushort CountermeasureEffectID;
        public Guid CountermeasureGUID;
        public Guid[] TOWMissileWeapons;
        public Guid[] GroundAAWeapons;
        public Guid[] AirAAWeapons;

        public override void SetDefaults()
        {
            MissileWarningID = 26033;
            MissileWarningDriverID = 26034;
            CountermeasureEffectID = 26035;
            CountermeasureGUID = new Guid("16dbd4e5928e498783675529ca53fc61");
            TOWMissileWeapons = new Guid[]
            {
                new Guid("f8978efc872e415bb41086fdee10d7ad"), // TOW
                new Guid("d900a6de5bb344f887855ce351e3bb41"), // Kornet
                new Guid("9ed685df6df34527b104ab227465489d"), // M2 Bradley TOW
                new Guid("d6312ceb00ad4530bdb07735bc02f070"), // BMP-2 Konkurs
            };
            GroundAAWeapons = new Guid[]
            {
                new Guid("58b18a3fa1104ca58a7bdebef3ab6b29"), // stinger
                new Guid("5ae39e59d299415d8c4d08b233206302"), // igla

            };
            AirAAWeapons = new Guid[]
            {
                new Guid("661a347f5e56406e85510a1b427bc4d6"), // F-15 AA
                new Guid("ad70852b3d31401b9001a13d64a13f78"), // Su-34 AA

            };
        }
    }
}
