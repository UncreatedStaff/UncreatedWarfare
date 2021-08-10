using Newtonsoft.Json;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Uncreated.Warfare.Structures
{
    public class StructureSaver : JSONSaver<Structure>
    {
        public StructureSaver() : base(Data.StructureStorage + "structures.json") { }
        protected override string LoadDefaults() => "[]";
        public static void DropAllStructures()
        {
            foreach (Structure structure in ActiveObjects)
            {
                structure.SpawnCheck();
                if (!structure.exists)
                    F.LogError($"Structure {structure.Asset.itemName} ({structure.instance_id}) failed to spawn.");
            }
        }
        public static bool AddStructure(StructureDrop drop, StructureData data, out Structure structureadded)
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
            } else
            {
                structureadded = default;
                return false;
            }
        }
        public static bool AddStructure(BarricadeDrop drop, BarricadeData data, out Structure structureadded)
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

    public class Structure
    {
        public const string ARGUMENT_EXCEPTION_VEHICLE_SAVED = "ERROR_VEHICLE_SAVED";
        public const string ARGUMENT_EXCEPTION_BARRICADE_NOT_FOUND = "ERROR_BARRICADE_NOT_FOUND";
        public ushort id;
        [JsonIgnore]
        public ItemAsset Asset
        {
            get
            {
                if (_asset != default) return _asset;
                if (Assets.find(EAssetType.ITEM, id) is ItemAsset asset)
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
            _metadata = default;
        }
        public ushort health;
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
        public Structure(ushort id, ushort health, string state, SerializableTransform transform, uint instance_id, ulong owner, ulong group, EStructType type)
        {
            this.id = id;
            this.health = health;
            this.state = state;
            this.type = type;
            this.instance_id = instance_id;
            if (type == EStructType.BARRICADE)
            {
                 F.GetBarricadeFromInstID(instance_id, out BarricadeDrop drop);
                if (drop == default)
                {
                    this.transform = transform;
                    exists = false;
                } else
                {
                    this.transform = new SerializableTransform(drop.model.transform);
                    exists = true;
                }
            } 
            else if (type == EStructType.STRUCTURE)
            {
                F.GetStructureFromInstID(instance_id, out StructureDrop drop);
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
            this.id = 0;
            this.health = 100;
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
            if (type == EStructType.BARRICADE)
            {
                BarricadeData data = F.GetBarricadeFromInstID(instance_id, out _);
                if (data == default)
                {
                    ItemBarricadeAsset asset = Asset as ItemBarricadeAsset;
                    Transform newBarricade = BarricadeManager.dropNonPlantedBarricade(
                        new Barricade(id, asset.health, Metadata, asset),
                        transform.position.Vector3, transform.Rotation, owner, group
                        );
                    if (newBarricade == null)
                    {
                        exists = false;
                        return;
                    }
                    BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(newBarricade);
                    if (drop != null)
                    {
                        if (newBarricade.TryGetComponent(out InteractableSign sign) && Regions.tryGetCoordinate(newBarricade.position, out byte x, out byte y))
                        {
                            F.InvokeSignUpdateForAll(sign, x, y, sign.text);
                        }
                        if (Vehicles.VehicleSpawner.SpawnExists(instance_id, EStructType.BARRICADE, out Vehicles.VehicleSpawn vbspawn))
                        {
                            vbspawn.SpawnPadInstanceID = drop.instanceID;
                            Vehicles.VehicleSpawner.Save();
                        }
                        instance_id = drop.instanceID;
                        exists = true;
                        StructureSaver.Save();
                    }
                    else
                    {
                        exists = false;
                    }
                }
                else exists = true;
            }
            else if (type == EStructType.STRUCTURE)
            {
                StructureData data = F.GetStructureFromInstID(instance_id, out _);
                if (data == default)
                {
                    ItemStructureAsset asset = Asset as ItemStructureAsset;
                    if (!StructureManager.dropStructure(
                        new SDG.Unturned.Structure(id, asset.health, asset),
                        transform.position.Vector3, transform.euler_angles.x, transform.euler_angles.y,
                        transform.euler_angles.z, owner, group))
                    {
                        F.LogWarning("Error in StructureSaver SpawnCheck(): Structure could not be placed, unknown error.");
                        exists = false;
                    }
                    else
                    {
                        if (Regions.tryGetCoordinate(transform.position.Vector3, out byte x, out byte y))
                        {
                            StructureDrop newdrop = StructureManager.regions[x, y].drops.LastOrDefault(nd => nd.model.position == transform.position.Vector3);
                            if (newdrop == null)
                            {
                                F.LogWarning("Error in StructureSaver SpawnCheck(): Spawned structure could be placed but was not able to locate a structure at that position.");
                                exists = false;
                            } else
                            {
                                F.Log("Respawned structure", ConsoleColor.DarkGray);
                                if (Vehicles.VehicleSpawner.SpawnExists(instance_id, EStructType.STRUCTURE, out Vehicles.VehicleSpawn vbspawn))
                                {
                                    vbspawn.SpawnPadInstanceID = newdrop.instanceID;
                                    Vehicles.VehicleSpawner.Save();
                                }
                                instance_id = newdrop.instanceID;
                                StructureSaver.Save();
                                exists = true;
                            }
                        } else
                        {
                            exists = false;
                        }
                    }
                } else exists = true;
            }
        }
        public Structure(StructureDrop drop, StructureData data)
        {
            this.id = data.structure.id;
            this._metadata = new byte[0];
            this.state = Convert.ToBase64String(_metadata);
            this.transform = new SerializableTransform(drop.model.transform);
            this.owner = data.owner;
            this.group = data.group;
            this.instance_id = data.instanceID;
            this.type = EStructType.STRUCTURE;
        }
        public Structure(BarricadeDrop drop, BarricadeData data)
        {
            this.id = data.barricade.id;
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
