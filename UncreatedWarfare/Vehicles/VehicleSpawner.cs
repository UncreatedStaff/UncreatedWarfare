using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Vehicles
{
    public class VehicleSpawner : JSONSaver<VehicleSpawn>
    {
        public VehicleSpawner()
            : base(Data.VehicleStorage + "vehiclespawns.json")
        {
            
        }
        protected override string LoadDefaults() => "[]";
        public static void CreateSpawn(ushort vehicleID, uint barricadeInstanceID) => AddObjectToSave(new VehicleSpawn(vehicleID, barricadeInstanceID));
        public static void DeleteSpawn(uint barricadeInstanceID) => RemoveWhere(s => s.BarricadeInstanceID == barricadeInstanceID);
        public static List<VehicleSpawn> GetAllSpawns() => GetExistingObjects();
        public static bool SpawnExists(uint barricadeInstanceID, out VehicleSpawn spawn) => 
            ObjectExists(s => s.BarricadeInstanceID == barricadeInstanceID, out spawn);
        public static bool VehicleHasSpawn(ushort vehicleID, uint barricadeInstanceID, out VehicleSpawn spawn) => 
            ObjectExists(s => s.VehicleID == vehicleID && s.BarricadeInstanceID == barricadeInstanceID, out spawn);
        public static void LinkVehicleToSpawn(uint vehicleInstanceID, uint barricadeInstanceID) => 
            UpdateObjectsWhere(s => s.BarricadeInstanceID == barricadeInstanceID, s => s.VehicleInstanceID = vehicleInstanceID);
        public static bool HasLinkedSpawn(uint vehicleInstanceID, out VehicleSpawn spawn) =>
            ObjectExists(s => s.VehicleInstanceID == vehicleInstanceID, out spawn);
        public static bool HasLinkedVehicle(VehicleSpawn spawn, out InteractableVehicle vehicle)
        {
            vehicle = VehicleManager.getVehicle(spawn.VehicleInstanceID);
            return vehicle != null;
        }
    }
    public class VehicleSpawn
    {
        public ushort VehicleID;
        [JsonSettable]
        public uint BarricadeInstanceID;
        [JsonSettable]
        public uint VehicleInstanceID;

        public VehicleSpawn(ushort vehicleID, uint instanceID)
        {
            VehicleID = vehicleID;
            BarricadeInstanceID = instanceID;
            VehicleInstanceID = 0;
        }
    }
}
