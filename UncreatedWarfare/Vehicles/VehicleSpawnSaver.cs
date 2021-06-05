using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Vehicles
{
    public class VehicleSpawnSaver : JSONSaver<VehicleSpawn>
    {
        public VehicleSpawnSaver()
            : base(Data.VehicleStorage + "vehiclespawns.json")
        {

        }
        protected override string LoadDefaults() => "[]";
        public static void CreateSpawn(ushort vehicleID, uint barricadeInstanceID) => AddObjectToSave(new VehicleSpawn(vehicleID, barricadeInstanceID));
        public static void DeleteSpawn(uint barricadeInstanceID)
        {
            var spawns = GetObjectsWhere(s => s.BarricadeInstanceID == barricadeInstanceID);
            spawns.RemoveAll(s => s.BarricadeInstanceID == barricadeInstanceID);
            OverwriteSavedList(spawns);
        }
        public static List<VehicleSpawn> GetAllSpawns() => GetExistingObjects();
        public static bool SpawnExists(uint barricadeInstanceID, out VehicleSpawn spawn)
        {
            bool result = ObjectExists(s => s.BarricadeInstanceID == barricadeInstanceID, out var t);
            spawn = t;
            return result;
        }
        public static bool VehicleHasSpawn(ushort vehicleID, uint barricadeInstanceID, out VehicleSpawn spawn)
        {
            bool result = ObjectExists(s => s.VehicleID == vehicleID && s.BarricadeInstanceID == barricadeInstanceID, out var t);
            spawn = t;
            return result;
        }
        public static void LinkVehicleToSpawn(uint vehicleInstanceID, uint barricadeInstanceID)
        {
            var spawns = GetExistingObjects();
            foreach (var spawn in spawns)
            {
                if (spawn.BarricadeInstanceID == barricadeInstanceID)
                {
                    spawn.VehicleInstanceID = vehicleInstanceID;
                    OverwriteSavedList(spawns);
                    return;
                }
            }
        }

        public static bool HasLinkedSpawn(uint vehicleInstanceID, out VehicleSpawn spawn)
        {
            bool result = ObjectExists(s => s.VehicleInstanceID == vehicleInstanceID, out var t);
            spawn = t;
            return result;
        }

        public static bool HasLinkedVehicle(VehicleSpawn spawn, out InteractableVehicle vehicle)
        {
            var v = VehicleManager.getVehicle(spawn.VehicleID);
            vehicle = v;
            return v != null;
        }

    }

    public class VehicleSpawn
    {
        public ushort VehicleID;
        public uint BarricadeInstanceID;
        public uint VehicleInstanceID;

        public VehicleSpawn(ushort vehicleID, uint instanceID)
        {
            VehicleID = vehicleID;
            BarricadeInstanceID = instanceID;
            VehicleInstanceID = 0;
        }
    }
}
