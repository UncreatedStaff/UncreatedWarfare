using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using UnityEngine;

namespace Uncreated.Warfare.Vehicles
{
    public class VehicleBay : JSONSaver<VehicleData>, IDisposable
    {
        public VehicleBay() : base(Data.VehicleStorage + "vehiclebay.json")
        {
            VehicleManager.onEnterVehicleRequested += OnVehicleEnterRequested;
            VehicleManager.onSwapSeatRequested += OnVehicleSwapSeatRequested;
            VehicleManager.onExitVehicleRequested += OnVehicleExitRequested;

            foreach (var data in ActiveObjects)
            {
                if (data.Items != null)
                {
                    foreach (var item in data.Items)
                    {
                        if (!Whitelister.IsWhitelisted(item, out _))
                            Whitelister.AddItem(item);
                    }
                }
            }
        }

        protected override string LoadDefaults() => "[]";
        public static void AddRequestableVehicle(InteractableVehicle vehicle)
        {
            VehicleData data = new VehicleData(vehicle.asset.GUID);
            data.SaveMetaData(vehicle);
            AddObjectToSave(data);
        }
        public static void RemoveRequestableVehicle(Guid vehicleID) => RemoveWhere(vd => vd.VehicleID == vehicleID);
        public static bool VehicleExists(Guid vehicleID, out VehicleData vehicleData)
        {
            for (int i = 0; i < ActiveObjects.Count; i++)
            {
                if (ActiveObjects[i].VehicleID == vehicleID)
                {
                    vehicleData = ActiveObjects[i];
                    return true;
                }
            }
            vehicleData = null;
            return false;
        }
        public static void IncrementRequestCount(Guid vehicleID, bool save)
        {
            for (int i = 0; i < ActiveObjects.Count; i++)
            {
                if (ActiveObjects[i].VehicleID == vehicleID)
                {
                    ActiveObjects[i].RequestCount++;
                    break;
                }
            }
            if (save) Save();
        }
        public static void SetItems(Guid vehicleID, Guid[] newItems) =>         UpdateObjectsWhere(vd => vd.VehicleID == vehicleID, vd => vd.Items = newItems);
        public static void AddCrewmanSeat(Guid vehicleID, byte newSeatIndex) => UpdateObjectsWhere(vd => vd.VehicleID == vehicleID, vd => vd.CrewSeats.Add(newSeatIndex));
        public static void RemoveCrewmanSeat(Guid vehicleID, byte seatIndex) => UpdateObjectsWhere(vd => vd.VehicleID == vehicleID, vd => vd.CrewSeats.Remove(seatIndex));
        /// <summary>Level must be loaded.</summary>
        public static InteractableVehicle SpawnLockedVehicle(Guid vehicleID, Vector3 position, Quaternion rotation, out uint instanceID)
        {
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
                                    { metadata = Convert.FromBase64String(k.metadata) };
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
        public static void ResupplyVehicleBarricades(InteractableVehicle vehicle, VehicleData vehicleData)
        {
            VehicleBarricadeRegion vehicleRegion = vehicle.FindRegionFromVehicleWithIndex(out ushort plant);
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
        public static void DeleteVehicle(InteractableVehicle vehicle)
        {
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
            for (byte seat = 0; seat < vehicle.passengers.Length; seat++)
            {
                if (seat == 0 && excludeDriver)
                    continue;

                var passenger = vehicle.passengers[seat];

                if (passenger.player == null)
                {
                    return true;
                }
            }
            return true;
        }
        public static bool TryGetFirstNonCrewSeat(InteractableVehicle vehicle, VehicleData data, out byte seat)
        {
            for (seat = 0; seat < vehicle.passengers.Length; seat++)
            {
                var passenger = vehicle.passengers[seat];

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
            for (seat = 0; seat < vehicle.passengers.Length; seat++)
            {
                var passenger = vehicle.passengers[seat];

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
            if (vehicle.lockedOwner == CSteamID.Nil || owner == null) return false;

            foreach (var passenger in vehicle.passengers)
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
            int count = 0;
            for (byte seat = 0; seat < vehicle.passengers.Length; seat++)
            {
                var passenger = vehicle.passengers[seat];

                if (data.CrewSeats.Contains(seat) && passenger.player != null)
                {
                    count++;
                }
            }
            return count;
        }
        private void OnVehicleExitRequested(Player player, InteractableVehicle vehicle, ref bool shouldAllow, ref Vector3 pendingLocation, ref float pendingYaw)
        {
            UCPlayer ucplayer = UCPlayer.FromPlayer(player);
            if (FOBManager.config.data.Buildables.Exists(e => e.type == EBuildableType.EMPLACEMENT && e.structureID == vehicle.asset.GUID)) return;
            if (pendingLocation.y - F.GetHeightAt2DPoint(pendingLocation.x, pendingLocation.z) > UCWarfare.Config.MaxVehicleHeightToLeave)
            {
                player.SendChat("vehicle_too_high");
                shouldAllow = false;
                return;
            }

            if (KitManager.KitExists(ucplayer.KitName, out Kit kit))
            {
                if (kit.Class == EClass.LAT || kit.Class == EClass.HAT)
                {
                    ucplayer.Player.equipment.dequip();
                }
            }
        }
        private void OnVehicleEnterRequested(Player nelsonplayer, InteractableVehicle vehicle, ref bool shouldAllow)
        {
            try
            {
                if (Data.Gamemode.State != EState.ACTIVE && Data.Gamemode.State != EState.STAGING)
                {
                    nelsonplayer.SendChat("gamemode is not active");
                    shouldAllow = false;
                    return;
                }
                if (vehicle == null || !vehicle.asset.canBeLocked)
                {
                    EventFunctions.OnEnterVehicle(nelsonplayer, vehicle, ref shouldAllow);
                    return;
                }
                UCPlayer player = UCPlayer.FromPlayer(nelsonplayer);
                if (player == null)
                {
                    EventFunctions.OnEnterVehicle(nelsonplayer, vehicle, ref shouldAllow);
                    return;
                }
                if (Data.Gamemode.State == EState.STAGING && Data.Is<IStagingPhase>(out _) && (!Data.Is(out IAttackDefense atk) || player.GetTeam() == atk.AttackingTeam))
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
            try
            {
                if (vehicle == null) return;
                if (!VehicleExists(vehicle.asset.GUID, out VehicleData vehicleData))
                    return;

                UCPlayer enterer = UCPlayer.FromPlayer(nelsonplayer);
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

                    UCPlayer owner = UCPlayer.FromCSteamID(vehicle.lockedOwner);

                    if (vehicleData.CrewSeats.Contains(toSeatIndex) && vehicleData.RequiredClass != EClass.NONE) // vehicle requires crewman or pilot
                    {
                        if (enterer.KitClass == vehicleData.RequiredClass)
                        {
                            if (toSeatIndex == 0) // if a crewman is trying to enter the driver's seat
                            {
                                bool canEnterDriverSeat = owner is null || 
                                    enterer == owner || 
                                    IsOwnerInVehicle(vehicle, owner) || 
                                    (owner is not null && owner.Squad != null && owner.Squad.Members.Contains(enterer) || 
                                    (owner.Position - vehicle.transform.position).sqrMagnitude > Math.Pow(200, 2)) ||
                                    (vehicleData.Type == EVehicleType.LOGISTICS && FOB.GetNearestFOB(vehicle.transform.position, EFOBRadius.FULL_WITH_BUNKER_CHECK, vehicle.lockedGroup.m_SteamID) != null);

                                if (!canEnterDriverSeat)
                                {
                                    if (owner.Squad == null)
                                        enterer.Message("vehicle_wait_for_owner", owner.CharacterName);
                                    else
                                        enterer.Message("vehicle_wait_for_owner_or_squad", owner.CharacterName, owner.Squad.Name);

                                    shouldAllow = false;
                                }
                            }
                            else // if the player is trying to switch to a gunner's seat
                            {
                                if (!F.IsInMain(vehicle.transform.position)) // if player is trying to switch to a gunner's seat outside of main
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
                            bool canEnterDriverSeat = owner is null || enterer == owner || IsOwnerInVehicle(vehicle, owner) || (owner is not null && owner.Squad != null && owner.Squad.Members.Contains(enterer));

                            if (!canEnterDriverSeat)
                            {
                                if (owner.Squad == null)
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

        public enum EVehicleProperty
        {
            TEAM,
            RESPAWNTIME,
            COST,
            LEVEL,
            TICKETS,
            BRANCH,
            COOLDOWN,
            CLASS,
            REARMCOST,
            REPAIRCOST
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
    public struct Delay
    {
        public static readonly Delay Nil = new Delay(EDelayType.NONE, float.NaN, null);
        public bool IsNil => value == float.NaN;
        public EDelayType type;
        public string gamemode;
        public float value;
        public Delay(EDelayType type, float value, string gamemode = null)
        {
            this.type = type;
            this.value = value;
            this.gamemode = gamemode;
        }
    }
    public class VehicleData
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
        public ushort Cost;
        [JsonSettable]
        public ushort UnlockLevel;
        [JsonSettable]
        public ushort TicketCost;
        [JsonSettable]
        public ushort Cooldown;
        [JsonSettable]
        public EBranch UnlockBranch;
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
        public Guid[] Items;
        public Delay[] Delays;
        public List<byte> CrewSeats;
        public MetaSave Metadata;
        public int RequestCount;
        public VehicleData(Guid vehicleID)
        {
            VehicleID = vehicleID;
            Team = 0;
            RespawnTime = 600;
            Cost = 0;
            UnlockLevel = 0;
            TicketCost = 0;
            Cooldown = 0;
            UnlockBranch = EBranch.DEFAULT;
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
            RearmCost = 3;
            Type = EVehicleType.NONE;
            RequiresSL = false;
            Items = new Guid[0];
            CrewSeats = new List<byte>();
            Metadata = null;
            RequestCount = 0;
            Delays = new Delay[0];
        }
        public VehicleData()
        {
            Name = "";
            VehicleID = Guid.Empty;
            Team = 0;
            RespawnTime = 600;
            Cost = 0;
            UnlockLevel = 0;
            TicketCost = 0;
            Cooldown = 0;
            UnlockBranch = EBranch.DEFAULT;
            Branch = EBranch.DEFAULT;
            RequiredClass = EClass.NONE;
            RearmCost = 3;
            Type = EVehicleType.NONE;
            RequiresSL = false;
            Items = new Guid[0];
            CrewSeats = new List<byte>();
            Metadata = null;
            RequestCount = 0;
            Delays = new Delay[0];
        }
        public void AddDelay(EDelayType type, float value, string gamemode = null)
        {
            if (type == EDelayType.NONE || value <= 0f) return;
            Delay del = new Delay(type, value, gamemode);
            Delay[] old = Delays;
            Delays = new Delay[old.Length];
            if (old.Length > 0)
            {
                Array.Copy(old, 0, Delays, 0, old.Length);
                old[old.Length - 1] = del;
            }
            else
            {
                old[0] = del;
            }
        }
        public bool HasDelayType(EDelayType type)
        {
            string gm = Data.Gamemode.Name;
            for (int i = 0; i < Delays.Length; i++)
            {
                ref Delay del = ref Delays[i];
                if (!gm.Equals(del.gamemode, StringComparison.OrdinalIgnoreCase)) continue;
                if (del.type == type) return true;
            }
            return false;
        }
        public bool IsDelayedType(EDelayType type)
        {
            string gm = Data.Gamemode.Name;
            float secondsSinceStart = Data.Gamemode.SecondsSinceStart;
            for (int i = 0; i < Delays.Length; i++)
            {
                ref Delay del = ref Delays[i];
                if (!gm.Equals(del.gamemode, StringComparison.OrdinalIgnoreCase)) continue;
                if (del.type == type)
                {
                    switch (type)
                    {
                        case EDelayType.NONE:
                            return false;
                        case EDelayType.TIME:
                            if (del.value > secondsSinceStart)
                            {
                                return true;
                            }
                            break;
                        case EDelayType.FLAG:
                            if (Data.Is(out IFlagTeamObjectiveGamemode fr) && Team != 0)
                            {
                                int i2 = GetHighestObjectiveIndex(Team, fr);
                                if ((Team == 1 && i2 > del.value) ||
                                    (Team == 2 && fr.Rotation.Count - i2 > del.value))
                                {
                                    return true;
                                }
                            }
                            break;
                        case EDelayType.FLAG_PERCENT:
                            if (Data.Is(out fr) && Team != 0)
                            {
                                int i2 = GetHighestObjectiveIndex(Team, fr);
                                if ((Team == 1 && i2 / (float)fr.Rotation.Count > del.value / 100f) ||
                                    (Team == 2 && (fr.Rotation.Count - i2) / (float)fr.Rotation.Count > del.value / 100f))
                                {
                                    return true;
                                }
                            }
                            break;
                        case EDelayType.OUT_OF_STAGING:
                            if (Data.Is(out IStagingPhase stg))
                            {
                                if (stg.State == EState.STAGING)
                                {
                                    return true;
                                }
                            }
                            break;
                    }
                }
            }
            return false;
        }
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
        public bool IsDelayed(out Delay delay)
        {
            delay = Delay.Nil;
            if (Delays == null || Delays.Length == 0) return false;
            string gm = Data.Gamemode.Name;
            float secondsSinceStart = Data.Gamemode.SecondsSinceStart;
            bool anyVal = false;
            for (int i = Delays.Length - 1; i >= 0; i--)
            {
                ref Delay del = ref Delays[i];
                bool isUni = string.IsNullOrEmpty(del.gamemode);
                if (isUni && anyVal) continue;
                if (!isUni && !gm.Equals(del.gamemode, StringComparison.OrdinalIgnoreCase)) continue;
                {
                    switch (del.type)
                    {
                        case EDelayType.NONE:
                            if (!isUni)
                            {
                                delay = Delay.Nil;
                                return false;
                            }
                            break;
                        case EDelayType.TIME:
                            if (del.value > secondsSinceStart)
                            {
                                delay = del;
                                if (!isUni) return true;
                                anyVal = true;
                            }
                            break;
                        case EDelayType.FLAG:
                            if (Data.Is(out IFlagTeamObjectiveGamemode fr))
                            {
                                if ((Team == 1 && fr.ObjectiveT1Index > del.value) || 
                                    (Team == 2 && fr.Rotation.Count - fr.ObjectiveT2Index > del.value))
                                {
                                    delay = del;
                                    if (!isUni) return true;
                                    anyVal = true;
                                }
                            }
                            break;
                        case EDelayType.FLAG_PERCENT:
                            if (Data.Is(out fr))
                            {
                                if ((Team == 1 && fr.ObjectiveT1Index / (float)fr.Rotation.Count > del.value / 100f) || 
                                    (Team == 2 && (fr.Rotation.Count - fr.ObjectiveT2Index) / (float)fr.Rotation.Count > del.value / 100f))
                                {
                                    delay = del;
                                    if (!isUni) return true;
                                    anyVal = true;
                                }
                            }
                            break;
                        case EDelayType.OUT_OF_STAGING:
                            if (Data.Is(out IStagingPhase stg))
                            {
                                if (stg.State == EState.STAGING)
                                {
                                    delay = del;
                                    if (!isUni) return true;
                                    anyVal = true;
                                }
                            }
                            break;
                    }
                }
            }
            if (!delay.IsNil)
                delay = Delay.Nil;
            return anyVal;
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
            List<VBarricade> barricades = null;
            List<KitItem> trunk = null;

            if (vehicle.trunkItems.items.Count > 0)
            {
                trunk = new List<KitItem>();

                foreach (var jar in vehicle.trunkItems.items)
                {
                    if (Assets.find(EAssetType.ITEM, jar.item.id) is ItemAsset asset)
                    {
                        trunk.Add(new KitItem(
                            asset.GUID,
                            jar.x,
                            jar.y,
                            jar.rot,
                            Convert.ToBase64String(jar.item.metadata),
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
    }

    public class MetaSave
    {
        public List<VBarricade> Barricades;
        public List<KitItem> TrunkItems;
        public MetaSave(List<VBarricade> barricades, List<KitItem> trunkItems)
        {
            Barricades = barricades;
            TrunkItems = trunkItems;
        }
    }

    public class VBarricade
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
    }

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
}
