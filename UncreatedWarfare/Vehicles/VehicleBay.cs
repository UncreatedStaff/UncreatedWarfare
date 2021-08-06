using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Kits;
using UnityEngine;

namespace Uncreated.Warfare.Vehicles
{
    public class VehicleBay : JSONSaver<VehicleData>, IDisposable
    {
        public VehicleBay()
            : base(Data.VehicleStorage + "vehiclebay.json")
        {
            VehicleManager.onEnterVehicleRequested += OnVehicleEnterRequested;
            VehicleManager.onSwapSeatRequested += OnVehicleSwapSeatRequested;
        }

        protected override string LoadDefaults() => "[]";
        public static void AddRequestableVehicle(InteractableVehicle vehicle)
        {
            VehicleData data = new VehicleData(vehicle.id);
            data.SaveMetaData(vehicle);
            AddObjectToSave(data);
        }
        public static void RemoveRequestableVehicle(ushort vehicleID) => RemoveWhere(vd => vd.VehicleID == vehicleID);
        public static bool VehicleExists(ushort vehicleID, out VehicleData vehicleData)
        {
            bool result = ObjectExists(vd => vd.VehicleID == vehicleID, out var v);
            vehicleData = v;
            return result;
        }
        public static void SetItems(ushort vehicleID, List<ushort> newItems) => UpdateObjectsWhere(vd => vd.VehicleID == vehicleID, vd => vd.Items = newItems);
        public static void AddCrewmanSeat(ushort vehicleID, byte newSeatIndex) => UpdateObjectsWhere(vd => vd.VehicleID == vehicleID, vd => vd.CrewSeats.Add(newSeatIndex));
        public static void RemoveCrewmanSeat(ushort vehicleID, byte seatIndex) => UpdateObjectsWhere(vd => vd.VehicleID == vehicleID, vd => vd.CrewSeats.Remove(seatIndex));
        /// <summary>Level must be loaded.</summary>
        public static InteractableVehicle SpawnLockedVehicle(ushort vehicleID, Vector3 position, Quaternion rotation, out uint instanceID)
        {
            try
            {
                instanceID = 0;
                if (VehicleBay.VehicleExists(vehicleID, out VehicleData vehicleData))
                {
                    InteractableVehicle vehicle = VehicleManager.spawnVehicleV2(vehicleID, position, rotation);
                    if (vehicle == null) return null;
                    instanceID = vehicle.instanceID;

                    if (vehicleData.Metadata != null)
                    {
                        foreach (VBarricade vb in vehicleData.Metadata.Barricades)
                        {
                            Barricade barricade;
                            if (Assets.find(EAssetType.ITEM, vb.BarricadeID) is ItemBarricadeAsset asset)
                            {
                                barricade = new Barricade(vb.BarricadeID, asset.health, Convert.FromBase64String(vb.State), asset);
                            }
                            else
                            {
                                barricade = new Barricade(vb.BarricadeID)
                                { state = Convert.FromBase64String(vb.State) };
                            }
                            Quaternion quarternion = Quaternion.Euler(vb.AngleX * 2, vb.AngleY * 2, vb.AngleZ * 2);
                            BarricadeManager.dropPlantedBarricade(vehicle.transform, barricade, new Vector3(vb.PosX, vb.PosY, vb.PosZ), quarternion, vb.OwnerID, vb.GroupID);
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
                    F.Log($"VEHICLE SPAWN ERROR: {UCAssetManager.FindVehicleAsset(vehicleID).vehicleName} has not been registered in the VehicleBay.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                F.LogError("Error spawning vehicle: ");
                F.LogError(ex);
                instanceID = 0;
                return null;
            }
        }
        public static void ResupplyVehicleBarricades(InteractableVehicle vehicle, VehicleData vehicleData)
        {
            VehicleBarricadeRegion vehicleRegion = vehicle.FindRegionFromVehicleWithIndex(out int index);
            if (index > -1)
            {
                ushort plant = unchecked((ushort)index);
                for (int i = vehicleRegion.drops.Count - 1; i >= 0; i--)
                {
                    if (i >= 0)
                    {
                        if (vehicleRegion.drops[i].interactable is InteractableStorage store)
                            store.despawnWhenDestroyed = true;

                        BarricadeManager.destroyBarricade(vehicleRegion, 0, 0, plant, unchecked((ushort)i));
                    }
                }
            }
            foreach (VBarricade vb in vehicleData.Metadata.Barricades)
            {
                Barricade barricade;
                if (Assets.find(EAssetType.ITEM, vb.BarricadeID) is ItemBarricadeAsset asset)
                {
                    barricade = new Barricade(vb.BarricadeID, asset.health, Convert.FromBase64String(vb.State), asset);
                } else
                {
                    barricade = new Barricade(vb.BarricadeID)
                    { state = Convert.FromBase64String(vb.State) };
                }
                Quaternion quarternion = Quaternion.Euler(vb.AngleX * 2, vb.AngleY * 2, vb.AngleZ * 2);
                BarricadeManager.dropPlantedBarricade(vehicle.transform, barricade, new Vector3(vb.PosX, vb.PosY, vb.PosZ), quarternion, vb.OwnerID, vb.GroupID);
            }
            EffectManager.sendEffect(30, EffectManager.SMALL, vehicle.transform.position);
        }
        public static void DeleteVehicle(InteractableVehicle vehicle)
        {
            VehicleBarricadeRegion vehicleRegion = BarricadeManager.findRegionFromVehicle(vehicle);

            for (int i = vehicleRegion.drops.Count - 1; i >= 0; i--)
            {
                if (i >= 0)
                {
                    if (vehicleRegion.drops[i].interactable is InteractableStorage store)
                        store.despawnWhenDestroyed = true;
                }
            }
            VehicleManager.askVehicleDestroy(vehicle);
        }
        public static void DeleteAllVehiclesFromWorld()
        {
            for (int i = 0; i < VehicleManager.vehicles.Count; i++)
            {
                DeleteVehicle(VehicleManager.vehicles[i]);
            }
        }

        private void OnVehicleEnterRequested(Player nelsonplayer, InteractableVehicle vehicle, ref bool shouldAllow)
        {
            if (vehicle == null) return;
            UCPlayer player = UCPlayer.FromPlayer(nelsonplayer);
            if (Data.ReviveManager.DownedPlayers.ContainsKey(nelsonplayer.channel.owner.playerID.steamID.m_SteamID))
            {
                shouldAllow = false;
                return;
            }

            UCPlayer owner = UCPlayer.FromCSteamID(vehicle.lockedOwner);

            bool isOwnerOnline = owner != null;
            Players.FPlayerName ownernames = isOwnerOnline ? F.GetPlayerOriginalNames(owner.SteamPlayer) : Players.FPlayerName.Nil;
            bool IsInOwnerSquad = isOwnerOnline && owner.Squad != null && owner.Squad.Members.Contains(player);
            bool isOwnerInVehicle = false;
            float OwnerDistanceFromVehicle = 0;

            if (!isOwnerOnline)
            {
                EventFunctions.OnEnterVehicle(nelsonplayer, vehicle, ref shouldAllow);
                return;
            }

            foreach (Passenger passenger in vehicle.passengers)
            {
                if (passenger.player != null)
                {
                    if (passenger.player.playerID.steamID == vehicle.lockedOwner)
                    {
                        isOwnerInVehicle = true;
                        break;
                    }
                }
            }

            if (!isOwnerInVehicle)
                OwnerDistanceFromVehicle = (owner.Position - vehicle.transform.position).sqrMagnitude;

            if (isOwnerOnline && !IsInOwnerSquad && vehicle.isLocked && !(vehicle.lockedOwner == player.CSteamID || vehicle.lockedOwner == CSteamID.Nil) && !isOwnerInVehicle && OwnerDistanceFromVehicle <= Math.Pow(150, 2))
            {
                if (owner.Squad != null)
                    player.SendChat("vehicle_owner_not_in_vehicle_squad", F.ColorizeName(ownernames.PlayerName, owner.GetTeam()), owner.Squad.Name);
                else
                    player.SendChat("vehicle_owner_not_in_vehicle", F.ColorizeName(ownernames.PlayerName, owner.GetTeam()));

                shouldAllow = false;
                return;
            }

            if (!VehicleExists(vehicle.id, out var vehicleData) || vehicleData.RequiredClass == Kit.EClass.NONE)
            {
                EventFunctions.OnEnterVehicle(nelsonplayer, vehicle, ref shouldAllow);
                return;
            }

            if (!KitManager.HasKit(player, out var kit))
            {
                player.SendChat("vehicle_no_kit");
                shouldAllow = false;
                return;
            }

            bool HasCrewman = true;

            foreach (byte i in vehicleData.CrewSeats)
            {
                if (vehicle.passengers[i].player == null)
                    HasCrewman = false;
            }

            if (vehicleData.RequiredClass != kit.Class && !HasCrewman)
            {
                player.SendChat("vehicle_need_another_person_with_kit", vehicleData.RequiredClass.ToString().ToUpper());
                shouldAllow = false;
                return;
            }
        }
        private void OnVehicleEnterRequestedNew(Player nelsonplayer, InteractableVehicle vehicle, ref bool shouldAllow)
        {
            if (vehicle == null) return;
            UCPlayer player = UCPlayer.FromPlayer(nelsonplayer);
            if (Data.ReviveManager.DownedPlayers.ContainsKey(nelsonplayer.channel.owner.playerID.steamID.m_SteamID))
            {
                shouldAllow = false;
                return;
            }

            if (!KitManager.HasKit(player, out var kit))
            {
                player.SendChat("vehicle_no_kit");
                shouldAllow = false;
                return;
            }

            if (!VehicleExists(vehicle.id, out var vehicleData))
            {
                EventFunctions.OnEnterVehicle(nelsonplayer, vehicle, ref shouldAllow);
                return;
            }

            UCPlayer owner = UCPlayer.FromCSteamID(vehicle.lockedOwner);

            

            bool IsPlayerOwner = vehicle.lockedOwner == player.CSteamID || vehicle.lockedOwner == CSteamID.Nil;

            if (!IsPlayerOwner)
            {
                bool isOwnerOnline = owner != null;

                if (isOwnerOnline)
                {
                    bool IsInOwnerSquad = owner.Squad != null && owner.Squad.Members.Contains(player);

                    if (!IsInOwnerSquad)
                    {
                        bool isOwnerInVehicle = false;

                        foreach (Passenger passenger in vehicle.passengers)
                        {
                            if (passenger.player != null)
                            {
                                if (passenger.player.playerID.steamID == vehicle.lockedOwner)
                                {
                                    isOwnerInVehicle = true;
                                    break;
                                }
                            }
                        }

                        if (!isOwnerInVehicle)
                        {
                            float OwnerDistanceFromVehicle = (owner.Position - vehicle.transform.position).sqrMagnitude;

                            if (vehicle.isLocked && OwnerDistanceFromVehicle < Math.Pow(200, 2))
                            {
                                Players.FPlayerName ownernames = F.GetPlayerOriginalNames(owner.SteamPlayer);

                                if (owner.Squad != null)
                                    player.SendChat("vehicle_owner_not_in_vehicle_squad", F.ColorizeName(ownernames.PlayerName, owner.GetTeam()), owner.Squad.Name);
                                else
                                    player.SendChat("vehicle_owner_not_in_vehicle", F.ColorizeName(ownernames.PlayerName, owner.GetTeam()));

                                shouldAllow = false;
                                return;
                            }
                        }
                    }   
                }
            }

            if (vehicleData.RequiredClass != Kit.EClass.NONE && vehicleData.CrewSeats.Count != 0) // if the vehicle requires a CREWMAN kit
            {
                if (kit.Class != vehicleData.RequiredClass)
                {
                    bool IsThereACrewman = false;

                    foreach (Passenger passenger in vehicle.passengers)
                    {
                        if (passenger == null || passenger.player == null)
                            continue;

                        if (UCPlayer.FromSteamPlayer(passenger.player)?.KitClass == vehicleData.RequiredClass)
                        {
                            IsThereACrewman = true;
                            break;
                        }
                    }

                    if (!IsThereACrewman)
                    {
                        player.SendChat("vehicle_need_another_person_with_kit", vehicleData.RequiredClass.ToString().ToUpper());
                        shouldAllow = false;
                        return;
                    }
                }
            }

            EventFunctions.OnEnterVehicle(nelsonplayer, vehicle, ref shouldAllow);
        }
        private void OnVehicleSwapSeatRequested(Player nelsonplayer, InteractableVehicle vehicle, ref bool shouldAllow, byte fromSeatIndex, ref byte toSeatIndex)
        {
            if (!VehicleExists(vehicle.id, out var vehicleData))
                return;

            UCPlayer player = UCPlayer.FromPlayer(nelsonplayer);

            if (!KitManager.HasKit(player, out var kit))
            {
                player.SendChat("vehicle_no_kit");
                shouldAllow = false;
                return;
            }

            UCPlayer owner = UCPlayer.FromCSteamID(vehicle.lockedOwner);

            bool isOwnerOnline = owner != default;

            bool IsInOwnerSquad = owner.Squad != null && owner.Squad.Members.Contains(player);

            if (!IsInOwnerSquad)
            {
                bool isOwnerInVehicle = false;

                foreach (Passenger passenger in vehicle.passengers)
                {
                    if (passenger.player != null)
                    {
                        if (passenger.player.playerID.steamID == vehicle.lockedOwner)
                        {
                            isOwnerInVehicle = true;
                            break;
                        }
                    }
                }

                if (!isOwnerInVehicle)
                {
                    float OwnerDistanceFromVehicle = (owner.Position - vehicle.transform.position).sqrMagnitude;

                    if (vehicle.isLocked && OwnerDistanceFromVehicle < Math.Pow(200, 2))
                    {
                        Players.FPlayerName ownernames = F.GetPlayerOriginalNames(owner.SteamPlayer);

                        if (owner.Squad != null)
                            player.SendChat("vehicle_owner_not_in_vehicle_squad", F.ColorizeName(ownernames.PlayerName, owner.GetTeam()), owner.Squad.Name);
                        else
                            player.SendChat("vehicle_owner_not_in_vehicle", F.ColorizeName(ownernames.PlayerName, owner.GetTeam()));

                        shouldAllow = false;
                        return;
                    }
                }
            }

            if (vehicleData.RequiredClass != Kit.EClass.NONE && vehicleData.CrewSeats.Count != 0) // if the vehicle requires a CREWMAN kit
            {
                if (toSeatIndex != 0 && vehicleData.CrewSeats.Contains(toSeatIndex))
                {
                    if (kit.Class == vehicleData.RequiredClass)
                    {
                        bool isThereAnotherCrewman = false;
                        foreach (Passenger passenger in vehicle.passengers)
                        {
                            if (passenger == null || passenger.player == null || passenger.player.playerID.steamID == player.CSteamID)
                                continue;

                            if (UCPlayer.FromSteamPlayer(passenger.player)?.KitClass == vehicleData.RequiredClass)
                            {
                                isThereAnotherCrewman = true;
                                break;
                            }
                        }

                        if (!isThereAnotherCrewman)
                        {
                            player.SendChat("vehicle_need_another_person_with_kit", vehicleData.RequiredClass.ToString().ToUpper());
                            shouldAllow = false;
                            return;
                        }
                    }
                    else
                    {
                        player.SendChat("vehicle_not_valid_kit", vehicleData.RequiredClass.ToString().ToUpper());
                        shouldAllow = false;
                        return;
                    }
                }
            }
        }

        public void Dispose()
        {
            VehicleManager.onEnterVehicleRequested -= OnVehicleEnterRequested;
            VehicleManager.onSwapSeatRequested -= OnVehicleSwapSeatRequested;
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

    public class VehicleData
    {
        [JsonSettable]
        public ushort VehicleID;
        [JsonSettable]
        public ulong Team;
        [JsonSettable]
        public ushort RespawnTime;
        [JsonSettable]
        public ushort Cost;
        [JsonSettable]
        public ushort RequiredLevel;
        [JsonSettable]
        public ushort TicketCost;
        [JsonSettable]
        public ushort Cooldown;
        [JsonSettable]
        public EBranch RequiredBranch;
        [JsonSettable]
        public Kit.EClass RequiredClass;
        [JsonSettable]
        public byte RearmCost;
        [JsonSettable]
        public byte RepairCost;
        [JsonSettable]
        public EVehicleType Type;
        [JsonSettable]
        public bool RequiresSL;
        public List<ushort> Items;
        public List<byte> CrewSeats;
        public MetaSave Metadata;
        public VehicleData(ushort vehicleID)
        {
            VehicleID = vehicleID;
            Team = 0;
            RespawnTime = 600;
            Cost = 0;
            RequiredLevel = 0;
            TicketCost = 0;
            Cooldown = 0;
            RequiredBranch = EBranch.DEFAULT;
            RequiredClass = Kit.EClass.NONE;
            RearmCost = 3;
            RepairCost = 3;
            Type = EVehicleType.NONE;
            RequiresSL = false;
            Items = new List<ushort>() { 28, 277 };
            CrewSeats = new List<byte>();
            Metadata = null;
        }
        public VehicleData()
        {
            VehicleID = 0;
            Team = 0;
            RespawnTime = 600;
            Cost = 0;
            RequiredLevel = 0;
            TicketCost = 0;
            Cooldown = 0;
            RequiredBranch = EBranch.DEFAULT;
            RequiredClass = Kit.EClass.NONE;
            RearmCost = 3;
            RepairCost = 3;
            Type = EVehicleType.NONE;
            RequiresSL = false;
            Items = new List<ushort>() { 28, 277 };
            CrewSeats = new List<byte>();
            Metadata = null;
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
            VehicleBarricadeRegion vehicleRegion = BarricadeManager.findRegionFromVehicle(vehicle);
            if (vehicleRegion != null)
            {
                List<VBarricade> barricades = new List<VBarricade>();
                for (int i = 0; i < vehicleRegion.drops.Count; i++)
                {
                    BarricadeData bdata = vehicleRegion.barricades[i];
                    barricades.Add(new VBarricade(bdata.barricade.id, bdata.barricade.asset.health, 0, Teams.TeamManager.AdminID, bdata.point.x, bdata.point.y,
                        bdata.point.z, bdata.angle_x, bdata.angle_y, bdata.angle_z, Convert.ToBase64String(bdata.barricade.state)));
                }
                if (barricades.Count > 0) Metadata = new MetaSave(vehicle.id, barricades);
            }
        }
    }

    public class MetaSave
    {
        public ushort VehicleID;
        public List<VBarricade> Barricades;

        public MetaSave(ushort vehicleID, List<VBarricade> barricades)
        {
            VehicleID = vehicleID;
            Barricades = barricades;
        }
    }

    public class VBarricade
    {
        public ushort BarricadeID;
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

        public VBarricade(ushort barricadeID, ushort health, ulong ownerID, ulong groupID, float posX, float posY, float posZ, float angleX, float angleY, float angleZ, string state)
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
