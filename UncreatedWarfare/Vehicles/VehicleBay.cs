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
using Logger = Rocket.Core.Logging.Logger;

namespace Uncreated.Warfare.Vehicles
{
    public class VehicleBay : JSONSaver<VehicleData>, IDisposable
    {
        public VehicleBay()
            : base(Data.VehicleStorage + "vehiclebay.json")
        {
            VehicleManager.OnVehicleExploded += OnVehicleExploded;
            VehicleManager.onEnterVehicleRequested += OnVehicleEnterRequested;
            VehicleManager.onSwapSeatRequested += OnVehicleSwapSeatRequested;
            Patches.BarricadeDestroyedHandler += OnBarricadeDestroyed;
            Patches.StructureDestroyedHandler += OnStructureDestroyed;
        }
        public static void StartAllActive()
        {
            foreach(VehicleSpawn data in VehicleSpawner.ActiveObjects)
            {
                F.Log("Trying to spawn " + data.VehicleID);
                if (!TrySpawnNewVehicle(data))
                    F.Log("Couldn't start spawning " + data.VehicleID);
            }
        }
        public static void Start(VehicleSpawn spawn)
        {
            if (!TrySpawnNewVehicle(spawn))
                F.Log("Couldn't start spawning " + spawn.VehicleID);
        }
        private void OnStructureDestroyed(StructureRegion region, StructureData data, StructureDrop drop, uint instanceID)
        {
            if (data.structure.id == UCWarfare.Config.VehicleBaySettings.VehicleSpawnerID)
            {
                if (VehicleSpawner.SpawnExists(drop.model, out _))
                {
                    Logger.Log("Vehicle spawn was deregistered because it was salvaged or destroyed.");
                    VehicleSpawner.DeleteSpawn(drop.model);
                }
            }
        }

        private void OnBarricadeDestroyed(BarricadeRegion region, BarricadeData data, BarricadeDrop drop, uint instanceID)
        {
            if (data.barricade.id == UCWarfare.Config.VehicleBaySettings.VehicleSpawnerID)
            {
                if (VehicleSpawner.SpawnExists(drop.model, out _))
                {
                    Logger.Log("Vehicle spawn was deregistered because it was salvaged or destroyed.");
                    VehicleSpawner.DeleteSpawn(drop.model);
                }
            }
        }

        protected override string LoadDefaults() => "[]";
        public static void AddRequestableVehicle(InteractableVehicle vehicle) => AddObjectToSave(new VehicleData(vehicle.id));
        public static void RemoveRequestableVehicle(ushort vehicleID) => RemoveWhere(vd => vd.VehicleID == vehicleID);
        public static void RemoveAllVehicles() => RemoveAllObjectsFromSave();
        public static List<VehicleData> GetVehiclesWhere(Func<VehicleData, bool> predicate) => GetObjectsWhere(predicate);
        public static bool VehicleExists(ushort vehicleID, out VehicleData vehicleData)
        {
            bool result = ObjectExists(vd => vd.VehicleID == vehicleID, out var v);
            vehicleData = v;
            return result;
        }
        public static void SetProperty(ushort vehicleID, object property, object newValue, out bool propertyIsValid, out bool vehicleExists, out bool argIsValid)
        {
            propertyIsValid = false;
            vehicleExists = false;
            argIsValid = false;

            if (!IsPropertyValid<EVehicleProperty>(property, out var p))
            {
                return;
            }
            propertyIsValid = true;

            var vehicles = GetExistingObjects();
            foreach (var data in vehicles)
            {
                if (data.VehicleID == vehicleID)
                {
                    vehicleExists = true;

                    switch (p)
                    {
                        case EVehicleProperty.TEAM:
                            if (UInt64.TryParse(newValue.ToString(), out var team))
                            {
                                argIsValid = true;
                                data.Team = team;
                            } break;
                        case EVehicleProperty.RESPAWNTIME:
                            if (UInt16.TryParse(newValue.ToString(), out var time))
                            {
                                argIsValid = true;
                                data.RespawnTime = time;
                            }
                            break;
                        case EVehicleProperty.COST:
                            if (UInt16.TryParse(newValue.ToString(), out var cost))
                            {
                                argIsValid = true;
                                data.Cost = cost;
                            }
                            break;
                        case EVehicleProperty.LEVEL:
                            if (UInt16.TryParse(newValue.ToString(), out var level))
                            {
                                argIsValid = true;
                                data.RequiredLevel = level;
                            }
                            break;
                        case EVehicleProperty.TICKETS:
                            if (UInt16.TryParse(newValue.ToString(), out var tickets))
                            {
                                argIsValid = true;
                                data.TicketCost = tickets;
                            }
                            break;
                        case EVehicleProperty.COOLDOWN:
                            if (UInt16.TryParse(newValue.ToString(), out var cooldown))
                            {
                                argIsValid = true;
                                data.Cooldown = cooldown;
                            }
                            break;
                        case EVehicleProperty.BRANCH:
                            if (Enum.TryParse<EBranch>(newValue.ToString(), out var branch))
                            {
                                argIsValid = true;
                                data.RequiredBranch = branch;
                            }
                            break;
                        case EVehicleProperty.CLASS:
                            if (Enum.TryParse<Kit.EClass>(newValue.ToString(), out var kitclass))
                            {
                                argIsValid = true;
                                data.RequiredClass = kitclass;
                            }
                            break;
                        case EVehicleProperty.REARMCOST:
                            if (byte.TryParse(newValue.ToString(), out var rearmCost))
                            {
                                argIsValid = true;
                                data.RearmCost = rearmCost;
                            }
                            break;
                        case EVehicleProperty.REPAIRCOST:
                            if (byte.TryParse(newValue.ToString(), out var repairCost))
                            {
                                argIsValid = true;
                                data.RepairCost = repairCost;
                            }
                            break;
                    }
                    if (argIsValid)
                    {
                        OverwriteSavedList(vehicles);
                    }
                }
            }
        }

        public static void SetItems(ushort vehicleID, List<ushort> newItems) => UpdateObjectsWhere(vd => vd.VehicleID == vehicleID, vd => vd.Items = newItems);

        public static void AddCrewmanSeat(ushort vehicleID, byte newSeatIndex) => UpdateObjectsWhere(vd => vd.VehicleID == vehicleID, vd => vd.CrewSeats.Add(newSeatIndex));
        public static void RemoveCrewmanSeat(ushort vehicleID, byte seatIndex) => UpdateObjectsWhere(vd => vd.VehicleID == vehicleID, vd => vd.CrewSeats.Remove(seatIndex));

        public static void SpawnLockedVehicle(ushort vehicleID, Vector3 position, Quaternion rotation, out uint instanceID)
        {
            instanceID = 0;

            if (!Level.isLoaded)
                return;

            if (VehicleExists(vehicleID, out var vehicleData))
            {
                F.Log("Spawning vehicle");
                InteractableVehicle vehicle = VehicleManager.spawnVehicleV2(vehicleID, position, rotation);
                instanceID = vehicle.instanceID;

                if (vehicleData.Metadata != null)
                {
                    foreach (var vBarricade in vehicleData.Metadata.Barricades)
                    {
                        Barricade newBarricade = new Barricade(vBarricade.BarricadeID);
                        newBarricade.state = Convert.FromBase64String(vBarricade.State);

                        Quaternion quarternion = Quaternion.Euler(vBarricade.AngleX * 2, vBarricade.AngleY * 2, vBarricade.AngleZ * 2);

                        BarricadeManager.dropPlantedBarricade(vehicle.transform, newBarricade, new Vector3(vBarricade.PosX, vBarricade.PosY, vBarricade.PosZ), quarternion, vBarricade.OwnerID, vBarricade.GroupID);
                    }
                }

                if (vehicle.asset.canBeLocked)
                {
                    vehicle.tellLocked(CSteamID.Nil, CSteamID.Nil, true);

                    VehicleManager.ServerSetVehicleLock(vehicle, CSteamID.Nil, CSteamID.Nil, true);

                    vehicle.updateVehicle();
                    vehicle.updatePhysics();
                }
            }
        }

        public static bool TryRespawnVehicle(uint vehicleInstanceID)
        {
            if (VehicleSpawner.HasLinkedSpawn(vehicleInstanceID, out VehicleSpawn spawn))
            {
                if (spawn.Barricade != default(SerializableTransform))
                {
                    SpawnLockedVehicle(spawn.VehicleID, spawn.Barricade.Position, Quaternion.Euler(spawn.Barricade.euler_angles.x * 2,
                        spawn.Barricade.euler_angles.y * 2, spawn.Barricade.euler_angles.z * 2), out var newInstanceID);
                    VehicleSpawner.LinkVehicleToSpawn(newInstanceID, spawn.Barricade);
                    return true;
                }
                else return false;
            }
            return false;
        }

        public static bool TrySpawnNewVehicle(VehicleSpawn spawn)
        {
            if (VehicleSpawner.HasLinkedVehicle(spawn, out var vehicle))
            {
                if (!(vehicle.isDead || vehicle.isDrowned)) // if the vehicle is not dead or drowned
                    return false;
            }
            if (spawn.Barricade != default(SerializableTransform))
            {
                SpawnLockedVehicle(spawn.VehicleID, spawn.Barricade.Position + new Vector3(0, 5, 0), Quaternion.Euler(Vector3.up), out var newInstanceID);
                VehicleSpawner.LinkVehicleToSpawn(newInstanceID, spawn.Barricade);
                return true;
            }
            else return false;
        }
        private void OnVehicleExploded(InteractableVehicle vehicle)
        {
            UCWarfare.I.StartCoroutine(StartVehicleRespawnTimer(vehicle));
        }

        private void OnVehicleEnterRequested(Player nelsonplayer, InteractableVehicle vehicle, ref bool shouldAllow)
        {
            UnturnedPlayer player = UnturnedPlayer.FromPlayer(nelsonplayer);

            // TODO: if vehicle is an emplacement, return

            bool isOwnerOnline = Provider.clients.Exists(sp => sp.playerID.steamID == vehicle.lockedOwner);
            bool isOwnerInVehicle = false;
            float OwnerDistanceFromVehicle = 0;

            if (!isOwnerOnline)
                return;

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
                OwnerDistanceFromVehicle = (UnturnedPlayer.FromCSteamID(vehicle.lockedOwner).Position - vehicle.transform.position).magnitude;

            if (isOwnerOnline && vehicle.isLocked && !(vehicle.lockedOwner == player.CSteamID || vehicle.lockedOwner == CSteamID.Nil) && !isOwnerInVehicle && OwnerDistanceFromVehicle <= 150)
            {
                // "Wait for the owner of this vehicle to get in before swapping seats."
                shouldAllow = false;
                return;
            }

            if (!VehicleExists(vehicle.id, out var vehicleData))
                return;

            if (vehicleData.RequiredClass == Kit.EClass.NONE)
                return;
            
            if (!KitManager.HasKit(player, out var kit))
            {
                // "You must get a kit before you can enter vehicles."
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
                // "You need a {kitname} kit in order to man this vehicle. Wait for its crew to get in first if you just want to ride as passenger.";
                shouldAllow = false;
                return;
            }
        }

        private void OnVehicleSwapSeatRequested(Player nelsonplayer, InteractableVehicle vehicle, ref bool shouldAllow, byte fromSeatIndex, ref byte toSeatIndex)
        {
            UnturnedPlayer player = UnturnedPlayer.FromPlayer(nelsonplayer);

            // TODO: if vehicle is an emplacement, return

            bool isOwnerOnline = Provider.clients.Exists(sp => sp.playerID.steamID == vehicle.lockedOwner);
            bool isOwnerInVehicle = false;
            float OwnerDistanceFromVehicle = 0;

            if (isOwnerOnline)
            {
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
                    OwnerDistanceFromVehicle = (UnturnedPlayer.FromCSteamID(vehicle.lockedOwner).Position - vehicle.transform.position).magnitude;
                }
            }

            if (isOwnerOnline && vehicle.isLocked && !(vehicle.lockedOwner == player.CSteamID || vehicle.lockedOwner == CSteamID.Nil) && !isOwnerInVehicle && OwnerDistanceFromVehicle <= 150)
            {
                // "Wait for the owner of this vehicle to get in before swapping seats."
                shouldAllow = false;
                return;
            }

            if (!VehicleExists(vehicle.id, out var vehicleData))
                return;

            if (vehicleData.RequiredClass == Kit.EClass.NONE)
                return;

            if (!KitManager.HasKit(player, out var kit))
            {
                // "How did you even get in here without a kit?"
                shouldAllow = false;
                return;
            }

            if (vehicleData.CrewSeats.Contains(toSeatIndex) && kit.Class != vehicleData.RequiredClass)
            {
                // "You need a {kitname} kit in order to man this vehicle."
                shouldAllow = false;
                return;
            }

            bool isThereAnotherCrewman = false;
            foreach (Passenger passenger in vehicle.passengers)
            {
                if (passenger.player == null)
                    continue;
                if (passenger.player.playerID.steamID == player.CSteamID)
                    continue;
                if (KitManager.HasKit(passenger.player.playerID.steamID, out var pKit) && pKit.Class == vehicleData.RequiredClass)
                {
                    isThereAnotherCrewman = true;
                    break;
                }
            }

            if (!isThereAnotherCrewman && vehicleData.CrewSeats.Contains(toSeatIndex) && toSeatIndex != 0)
            {
                // "You must have ONE OTHER {kitname} in this vehicle before you can enter the gunner's seat."
                shouldAllow = false;
                return;
            }
        }
        public void FirstSpawn()
        {
            var allspawns = VehicleSpawner.GetAllSpawns();
            foreach (var spawn in allspawns)
                TrySpawnNewVehicle(spawn);
        }

        private IEnumerator<WaitForSeconds> StartVehicleRespawnTimer(InteractableVehicle vehicle)
        {
            if (!VehicleExists(vehicle.id, out var vehicleData))
                yield break;

            yield return new WaitForSeconds(vehicleData.RespawnTime);

            if (UCWarfare.I.State != PluginState.Loaded)
                yield break;

            TryRespawnVehicle(vehicle.instanceID);
        }

        public void Dispose()
        {
            VehicleManager.OnVehicleExploded -= OnVehicleExploded;
            VehicleManager.onEnterVehicleRequested -= OnVehicleEnterRequested;
            VehicleManager.onSwapSeatRequested -= OnVehicleSwapSeatRequested;
            Patches.BarricadeDestroyedHandler -= OnBarricadeDestroyed;
            Patches.StructureDestroyedHandler -= OnStructureDestroyed;
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
            Items = new List<ushort>() { 1440, 277 };
            CrewSeats = new List<byte>();
            Metadata = null;
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
}
