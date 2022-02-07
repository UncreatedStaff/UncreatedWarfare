using SDG.Unturned;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace Uncreated.Warfare.Structures
{
    public class StructureSaver : JSONSaver<Structure>
    {
        public StructureSaver() : base(Data.StructureStorage + "structures.json", Structure.WriteStructure, Structure.ReadStructure) { }
        protected override string LoadDefaults() => "[]";
        public static void DropAllStructures()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            foreach (Structure structure in ActiveObjects)
            {
                structure.SpawnCheck();
                if (!structure.exists)
                    L.LogError($"Structure {structure?.Asset?.itemName ?? structure.id.ToString("N")} ({structure.instance_id}) failed to spawn.");
            }
        }
        public static bool AddStructure(StructureDrop drop, SDG.Unturned.StructureData data, out Structure structureadded)
        {
            if (data == default || drop == default)
            {
                structureadded = default;
                return false;
            }
            if (!ObjectExists(s => s != null && s.instance_id == drop.instanceID, out Structure structure))
            {
                structureadded = AddObjectToSave(new Structure(drop, data));
                return structureadded != default;
            }
            else
            {
                structureadded = default;
                return false;
            }
        }
        public static bool AddStructure(BarricadeDrop drop, SDG.Unturned.BarricadeData data, out Structure structureadded)
        {
            if (data == default || drop == default)
            {
                structureadded = default;
                return false;
            }
            if (!ObjectExists(s => s != null && s.instance_id == drop.instanceID, out Structure structure))
            {
                structureadded = AddObjectToSave(new Structure(drop, data));
                return structureadded != default;
            }
            else
            {
                structureadded = default;
                return false;
            }
        }
        public static void RemoveStructure(Structure structure) => RemoveWhere(x => structure != default && x != default && x.instance_id == structure.instance_id);
        public static bool StructureExists(uint instance_id, EStructType type, out Structure found) => ObjectExists(s => s.instance_id == instance_id && s.type == type, out found);
        public static void SetOwner(Structure structure, ulong newOwner) => SetProperty(structure, nameof(structure.owner), newOwner, out _, out _, out _);
        public static void SetGroupOwner(Structure structure, ulong group) => SetProperty(structure, nameof(structure.group), group, out _, out _, out _);
    }
    public enum EStructType : byte
    {
        STRUCTURE = 1,
        BARRICADE = 2
    }

    public class Structure : IJsonReadWrite
    {
        public const string ARGUMENT_EXCEPTION_VEHICLE_SAVED = "ERROR_VEHICLE_SAVED";
        public const string ARGUMENT_EXCEPTION_BARRICADE_NOT_FOUND = "ERROR_BARRICADE_NOT_FOUND";
        public Guid id;
        [JsonIgnore]
        public ItemAsset Asset
        {
            get
            {
                if (_asset != default) return _asset;
                if (Assets.find(id) is ItemAsset asset)
                {
                    _asset = asset;
                    return asset;
                }
                return default;
            }
        }
        [JsonIgnore]
        private ItemAsset _asset;
        [JsonIgnore]
        public byte[] Metadata
        {
            get
            {
                if (_metadata != default) return _metadata;
                if (state == default) return new byte[0];
                _metadata = Convert.FromBase64String(state);
                return _metadata;
            }
        }
        [JsonIgnore]
        private byte[] _metadata;
        internal void ResetMetadata()
        {
            _metadata = Convert.FromBase64String(state);
        }
        public string state;
        public SerializableTransform transform;
        public uint instance_id;
        public EStructType type;
        [JsonSettable]
        public ulong owner;
        [JsonSettable]
        public ulong group;
        [JsonIgnore]
        public bool exists;
        [JsonConstructor]
        public Structure(Guid id, string state, SerializableTransform transform, uint instance_id, ulong owner, ulong group, EStructType type)
        {
            this.id = id;
            this.state = state;
            this.type = type;
            this.instance_id = instance_id;
            if (type == EStructType.BARRICADE)
            {
                UCBarricadeManager.GetBarricadeFromInstID(instance_id, out BarricadeDrop drop);
                if (drop == default)
                {
                    this.transform = transform;
                    exists = false;
                }
                else
                {
                    this.transform = new SerializableTransform(drop.model.transform);
                    exists = true;
                }
            }
            else if (type == EStructType.STRUCTURE)
            {
                UCBarricadeManager.GetStructureFromInstID(instance_id, out StructureDrop drop);
                if (drop == default)
                {
                    this.transform = transform;
                    exists = false;
                }
                else
                {
                    this.transform = new SerializableTransform(drop.model.transform);
                    exists = true;
                }
            }
            else exists = false;
            this.owner = owner;
            this.group = group;
        }
        public Structure()
        {
            this.id = Guid.Empty;
            this.state = string.Empty;
            this.type = EStructType.BARRICADE;
            this.instance_id = 0;
            this.owner = 0;
            this.group = 0;
            this.exists = false;
        }
        /// <summary>Spawns the structure if it is not already placed.</summary>
        public void SpawnCheck()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (type == EStructType.BARRICADE)
            {
                SDG.Unturned.BarricadeData data = UCBarricadeManager.GetBarricadeFromInstID(instance_id, out BarricadeDrop bdrop);
                if (data == default)
                {
                    if (Asset is not ItemBarricadeAsset asset)
                    {
                        L.LogError("Failed to find barricade asset in Structure Saver");
                        exists = false;
                        return;
                    }
                    if (transform != default(SerializableTransform))
                    {
                        bdrop = UCBarricadeManager.GetBarriadeBySerializedTransform(transform);
                        if (bdrop != null && bdrop.asset.GUID == id)
                        {
                            if (bdrop.interactable is InteractableSign sign && Regions.tryGetCoordinate(bdrop.model.position, out byte x, out byte y))
                            {
                                F.InvokeSignUpdateForAll(sign, x, y, sign.text);
                            }
                            if (Vehicles.VehicleSpawner.SpawnExists(instance_id, EStructType.BARRICADE, out Vehicles.VehicleSpawn vbspawn))
                            {
                                vbspawn.SpawnPadInstanceID = bdrop.instanceID;
                                Vehicles.VehicleSpawner.Save();
                            }
                            instance_id = bdrop.instanceID;
                            exists = true;
                            float h = bdrop.GetServersideData().barricade.health;
                            if (h < asset.health)
                                BarricadeManager.repair(bdrop.model, asset.health - h, 1f);
                            StructureSaver.Save();
                            L.Log("Found barricade by location", ConsoleColor.DarkGray);
                            return;
                        }
                    }
                    Transform newBarricade = BarricadeManager.dropNonPlantedBarricade(
                        new Barricade(asset, asset.health, Metadata),
                        transform.position.Vector3, transform.Rotation, owner, group
                        );
                    if (newBarricade == null)
                    {
                        exists = false;
                        return;
                    }
                    bdrop = BarricadeManager.FindBarricadeByRootTransform(newBarricade);
                    if (bdrop != null)
                    {
                        if (newBarricade.TryGetComponent(out InteractableSign sign) && Regions.tryGetCoordinate(newBarricade.position, out byte x, out byte y))
                        {
                            F.InvokeSignUpdateForAll(sign, x, y, sign.text);
                        }
                        if (Vehicles.VehicleSpawner.SpawnExists(instance_id, EStructType.BARRICADE, out Vehicles.VehicleSpawn vbspawn))
                        {
                            vbspawn.SpawnPadInstanceID = bdrop.instanceID;
                            Vehicles.VehicleSpawner.Save();
                        }
                        instance_id = bdrop.instanceID;
                        exists = true;
                        StructureSaver.Save();
                    }
                    else
                    {
                        exists = false;
                    }
                }
                else
                {
                    SerializableTransform n = new SerializableTransform(bdrop.model);
                    if (transform != n)
                    {
                        transform = n;
                        StructureSaver.Save();
                    }
                    exists = true;
                    float h = bdrop.GetServersideData().barricade.health;
                    if (h < bdrop.asset.health)
                        BarricadeManager.repair(bdrop.model, bdrop.asset.health - h, 1f);
                }
            }
            else if (type == EStructType.STRUCTURE)
            {
                SDG.Unturned.StructureData data = UCBarricadeManager.GetStructureFromInstID(instance_id, out StructureDrop sdrop);
                if (data == default)
                {
                    if (Asset is not ItemStructureAsset asset)
                    {
                        L.LogError("Failed to find structure asset asset in Structure Saver");
                        exists = false;
                        return;
                    }
                    if (transform != default(SerializableTransform))
                    {
                        sdrop = UCBarricadeManager.GetStructureBySerializedTransform(transform);
                        if (sdrop != null && sdrop.asset.GUID == id)
                        {
                            if (Vehicles.VehicleSpawner.SpawnExists(instance_id, EStructType.STRUCTURE, out Vehicles.VehicleSpawn vbspawn))
                            {
                                vbspawn.SpawnPadInstanceID = sdrop.instanceID;
                                Vehicles.VehicleSpawner.Save();
                            }
                            instance_id = sdrop.instanceID;
                            exists = true;
                            float h = sdrop.GetServersideData().structure.health;
                            if (h < asset.health)
                                BarricadeManager.repair(sdrop.model, asset.health - h, 1f);
                            StructureSaver.Save();
                            L.Log("Found structure by location", ConsoleColor.DarkGray);
                            return;
                        }
                    }
                    if (!StructureManager.dropStructure(
                        new SDG.Unturned.Structure(asset, asset.health),
                        transform.position.Vector3, transform.euler_angles.x, transform.euler_angles.y,
                        transform.euler_angles.z, owner, group))
                    {
                        L.LogWarning("Error in StructureSaver SpawnCheck(): Structure could not be placed, unknown error.");
                        exists = false;
                    }
                    else
                    {
                        if (Regions.tryGetCoordinate(transform.position.Vector3, out byte x, out byte y))
                        {
                            sdrop = StructureManager.regions[x, y].drops.LastOrDefault(nd => nd.model.position == transform.position.Vector3);
                            if (sdrop == null)
                            {
                                L.LogWarning("Error in StructureSaver SpawnCheck(): Spawned structure could be placed but was not able to locate a structure at that position.");
                                exists = false;
                            }
                            else
                            {
                                L.Log("Respawned structure", ConsoleColor.DarkGray);
                                if (Vehicles.VehicleSpawner.SpawnExists(instance_id, EStructType.STRUCTURE, out Vehicles.VehicleSpawn vbspawn))
                                {
                                    vbspawn.SpawnPadInstanceID = sdrop.instanceID;
                                    Vehicles.VehicleSpawner.Save();
                                }
                                instance_id = sdrop.instanceID;
                                StructureSaver.Save();
                                exists = true;
                            }
                        }
                        else
                        {
                            exists = false;
                        }
                    }
                }
                else
                {
                    SerializableTransform n = new SerializableTransform(sdrop.model);
                    if (transform != n)
                    {
                        transform = n;
                        StructureSaver.Save();
                    }
                    exists = true;
                    float h = sdrop.GetServersideData().structure.health;
                    if (h < sdrop.asset.health)
                        BarricadeManager.repair(sdrop.model, sdrop.asset.health - h, 1f);
                }
            }
        }
        public static void WriteStructure(Structure structure, Utf8JsonWriter writer)
        {
            structure.WriteJson(writer);
        }
        public static Structure ReadStructure(ref Utf8JsonReader reader)
        {
            Structure structure = new Structure();
            structure.ReadJson(ref reader);
            return structure;
        }

        public void WriteJson(Utf8JsonWriter writer)
        {
            writer.WriteProperty(nameof(id), id);
            writer.WriteProperty(nameof(state), state);
            writer.WriteProperty(nameof(transform), transform);
            writer.WriteProperty(nameof(instance_id), instance_id);
            writer.WriteProperty(nameof(type), (byte)type);
        }
        public void ReadJson(ref Utf8JsonReader reader)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string prop = reader.GetString();
                    if (reader.Read())
                    {
                        switch (prop)
                        {
                            case nameof(id):
                                id = reader.GetGuid();
                                break;
                            case nameof(state):
                                state = reader.GetString();
                                break;
                            case nameof(transform):
                                if (reader.TokenType == JsonTokenType.StartObject)
                                    transform.ReadJson(ref reader);
                                break;
                            case nameof(instance_id):
                                instance_id = reader.GetUInt32();
                                break;
                            case nameof(type):
                                type = (EStructType)reader.GetByte();
                                break;
                        }
                    }
                }
                else if (reader.TokenType == JsonTokenType.EndObject)
                    break;
            }

            if (type == EStructType.BARRICADE)
            {
                UCBarricadeManager.GetBarricadeFromInstID(instance_id, out BarricadeDrop drop);
                if (drop == default)
                {
                    exists = false;
                }
                else
                {
                    this.transform = new SerializableTransform(drop.model.transform);
                    exists = true;
                }
            }
            else if (type == EStructType.STRUCTURE)
            {
                UCBarricadeManager.GetStructureFromInstID(instance_id, out StructureDrop drop);
                if (drop == default)
                {
                    exists = false;
                }
                else
                {
                    this.transform = new SerializableTransform(drop.model.transform);
                    exists = true;
                }
            }
            else exists = false;
        }

        public Structure(StructureDrop drop, SDG.Unturned.StructureData data)
        {
            this.id = data.structure.asset.GUID;
            this._metadata = new byte[0];
            this.state = Convert.ToBase64String(_metadata);
            this.transform = new SerializableTransform(drop.model.transform);
            this.owner = data.owner;
            this.group = data.group;
            this.instance_id = data.instanceID;
            this.type = EStructType.STRUCTURE;
        }
        public Structure(BarricadeDrop drop, SDG.Unturned.BarricadeData data)
        {
            this.id = data.barricade.asset.GUID;
            this._metadata = data.barricade.state;
            this.state = Convert.ToBase64String(_metadata);
            this.transform = new SerializableTransform(drop.model.transform);
            this.owner = data.owner;
            this.group = data.group;
            this.instance_id = data.instanceID;
            this.type = EStructType.BARRICADE;
        }
    }
}
