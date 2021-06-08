using Newtonsoft.Json;
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
        public static void CreateSpawn(ushort vehicleID, UnityEngine.Transform barricade)
        {
            VehicleSpawn spawn = new VehicleSpawn(vehicleID, new SerializableTransform(barricade));
            AddObjectToSave(spawn);
            Structures.StructureSaver.AddStructure(barricade, out _, out _);
            VehicleBay.Start(spawn);
        }
        public static void DeleteSpawn(UnityEngine.Transform barricade)
        {
            RemoveWhere(s => s.Barricade == barricade);
            Structures.StructureSaver.RemoveWhere(x => x.transform == barricade);
        }
        public static List<VehicleSpawn> GetAllSpawns() => GetExistingObjects();
        public static bool SpawnExists(SerializableTransform barricade, out VehicleSpawn spawn) => 
            ObjectExists(s => s.Barricade == barricade, out spawn);
        public static bool SpawnExists(UnityEngine.Transform barricade, out VehicleSpawn spawn) => 
            ObjectExists(s => s.Barricade == barricade, out spawn);
        public static bool VehicleHasSpawn(ushort vehicleID, SerializableTransform barricade, out VehicleSpawn spawn) => 
            ObjectExists(s => s.VehicleID == vehicleID && s.Barricade == barricade, out spawn);
        public static void LinkVehicleToSpawn(uint vehicleInstanceID, SerializableTransform barricade) => 
            UpdateObjectsWhere(s => s.Barricade == barricade, s => s.VehicleInstanceID = vehicleInstanceID, false);
        public static bool HasLinkedSpawn(uint vehicleInstanceID, out VehicleSpawn spawn) =>
            ObjectExists(s => s.VehicleInstanceID == vehicleInstanceID, out spawn);
        public static bool HasLinkedVehicle(VehicleSpawn spawn, out InteractableVehicle vehicle)
        {
            if (spawn.VehicleInstanceID == 0)
            {
                vehicle = null;
                return false;
            }
            vehicle = VehicleManager.getVehicle(spawn.VehicleInstanceID);
            return vehicle != null;
        }
    }
    public class VehicleSpawn
    {
        public ushort VehicleID;
        public SerializableTransform Barricade;
        [JsonSettable]
        [JsonIgnore]
        public uint VehicleInstanceID;
        public VehicleSpawn(ushort VehicleID, SerializableTransform Barricade)
        {
            this.VehicleID = VehicleID;
            this.Barricade = Barricade;
            this.VehicleInstanceID = 0;
        }
    }
}
