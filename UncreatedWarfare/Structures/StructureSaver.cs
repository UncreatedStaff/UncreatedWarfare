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
                F.Log(structure.id);
                if(structure.Asset is ItemBarricadeAsset barricadeasset)
                {
                    Transform barricade = BarricadeManager.dropNonPlantedBarricade(new Barricade(structure.id, 100, structure.Metadata, barricadeasset), 
                        structure.transform.Position, structure.transform.Rotation, structure.owner, structure.group);
                    if (barricade == default)
                        F.LogError($"Failed to spawn barricade of id {structure.id}: \"{structure.Asset.itemName}\" at ({Math.Round(structure.transform.position.x, 2)}, " +
                            $"{Math.Round(structure.transform.position.y, 2)}, {Math.Round(structure.transform.position.z, 2)}).");
                } else if (structure.Asset is ItemStructureAsset structureasset)
                {
                    if(!StructureManager.dropStructure(new SDG.Unturned.Structure(structure.id, 100, structureasset),
                        structure.transform.Position, structure.transform.euler_angles.x, structure.transform.euler_angles.y, structure.transform.euler_angles.z, 
                        structure.owner, structure.group))
                    {
                        F.LogError($"Failed to spawn structure of id {structure.id}: \"{structure.Asset.itemName}\" at ({Math.Round(structure.transform.position.x, 2)}, " +
                            $"{Math.Round(structure.transform.position.y, 2)}, {Math.Round(structure.transform.position.z, 2)}).");
                    }
                }
            }
        }
        /// <param name="reason">0: success, 1: barricade not found, 2: vehicle inputted, 3: unknown error, 4: already exists</param>
        public static bool AddStructure(Interactable barricade, out Structure structureadded, out byte reason) => AddStructure(barricade.transform, out structureadded, out reason);
        /// <param name="reason">0: success, 1: barricade not found, 2: vehicle inputted, 3: unknown error, 4: already exists</param>
        public static bool AddStructure(Interactable2 structure, out Structure structureadded, out byte reason) => AddStructure(structure.transform, out structureadded, out reason);
        /// <param name="reason">0: success, 1: barricade not found, 2: vehicle inputted, 3: unknown error, 4: already exists</param>
        public static bool AddStructure(Transform structure, out Structure structureadded, out byte reason)
        {
            structureadded = default;
            reason = 0;
            if (structure == default) return false;
            if (!ObjectExists(x => x != null && x.transform.Position == structure.position && x.transform.Rotation == structure.rotation, out structureadded))
            {
                try
                {
                    structureadded = new Structure(structure);
                }
                catch (ArgumentException ex)
                {
                    switch (ex.Message)
                    {
                        case Structure.ARGUMENT_EXCEPTION_BARRICADE_NOT_FOUND:
                            F.Log($"Structure not found at ({Math.Round(structure.position.x, 2)}, " +
                                    $"{Math.Round(structure.position.y, 2)}, {Math.Round(structure.position.z, 2)}).");
                            reason = 1;
                            return false;
                        case Structure.ARGUMENT_EXCEPTION_VEHICLE_SAVED:
                            F.Log($"A vehicle was attempted to add to the structure list.");
                            reason = 2;
                            return false;
                    }
                }
                if (structureadded == default)
                {
                    reason = 3;
                    return false;
                }
                AddObjectToSave(structureadded);
                return true;
            }
            else reason = 4;
            return false;
        }
        public static void RemoveStructure(Structure structure) => RemoveWhere(x => structure != default && x != default && x.transform == structure.transform);
        private static bool StructureExists(Transform barricade, out Structure found) => ObjectExists(s => s.transform == barricade, out found);
        public static bool StructureExists(Interactable2 structure, out Structure found) => StructureExists(structure.transform, out found);
        public static bool StructureExists(Interactable barricade, out Structure found) => StructureExists(barricade.transform, out found);
        public static void SetOwner(Structure structure, ulong newOwner) => SetProperty(structure, nameof(structure.owner), newOwner, out _, out _, out _);
        public static void SetGroupOwner(Structure structure, ulong group) => SetProperty(structure, nameof(structure.group), group, out _, out _, out _);
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
        public string state;
        public SerializableTransform transform;
        [JsonSettable]
        public ulong owner;
        [JsonSettable]
        public ulong group;
        [JsonConstructor]
        public Structure(ushort id, string state, SerializableTransform transform, ulong owner, ulong group)
        {
            this.id = id;
            this.state = state;
            this.transform = transform;
            this.owner = owner;
            this.group = group;
        }
        public Structure(Transform transform)
        {
            if(transform.TryGetComponent(out Interactable interactable))
            {
                if(interactable is InteractableVehicle)
                    throw new ArgumentException(ARGUMENT_EXCEPTION_VEHICLE_SAVED, "transform");
                if (BarricadeManager.tryGetInfo(transform, out _, out _, out _, out ushort index, out BarricadeRegion region))
                {
                    BarricadeData data = region.barricades[index];
                    this.id = data.barricade.id;
                    this.state = Convert.ToBase64String(data.barricade.state);
                    this._metadata = data.barricade.state;
                    this.transform = new SerializableTransform(transform);
                    this.owner = data.owner;
                    this.group = data.group;
                }
                else
                {
                    throw new ArgumentException(ARGUMENT_EXCEPTION_BARRICADE_NOT_FOUND, "transform");
                }

            } else if (transform.TryGetComponent(out Interactable2 structure))
            {
                if (structure is Interactable2SalvageBarricade)
                {
                    if (BarricadeManager.tryGetInfo(structure.transform, out _, out _, out _, out ushort index, out BarricadeRegion region))
                    {
                        BarricadeData data = region.barricades[index];
                        this.id = data.barricade.id;
                        this.state = Convert.ToBase64String(data.barricade.state);
                        this._metadata = data.barricade.state;
                        this.transform = new SerializableTransform(transform);
                        this.owner = data.owner;
                        this.group = data.group;
                    }
                    else
                    {
                        throw new ArgumentException(ARGUMENT_EXCEPTION_BARRICADE_NOT_FOUND, "transform");
                    }
                }
                else if (structure is Interactable2SalvageStructure)
                {
                    if (StructureManager.tryGetInfo(structure.transform, out _, out _, out ushort index, out StructureRegion region))
                    {
                        StructureData data = region.structures[index];
                        this.id = data.structure.id;
                        this.state = Convert.ToBase64String(new byte[0]);
                        this._metadata = new byte[0];
                        this.transform = new SerializableTransform(structure.transform);
                        this.owner = data.owner;
                        this.group = data.group;
                    }
                    else
                    {
                        throw new ArgumentException(ARGUMENT_EXCEPTION_BARRICADE_NOT_FOUND, "transform");
                    }
                }
                else
                {
                    throw new ArgumentException(ARGUMENT_EXCEPTION_BARRICADE_NOT_FOUND, "transform");
                }
            } else
            {
                throw new ArgumentException(ARGUMENT_EXCEPTION_BARRICADE_NOT_FOUND, "transform");
            }
        }
        public Structure(Interactable barricade)
        {
            if(BarricadeManager.tryGetInfo(barricade.transform, out _, out _, out _, out ushort index, out BarricadeRegion region))
            {
                BarricadeData data = region.barricades[index];
                this.id = data.barricade.id;
                this.state = Convert.ToBase64String(data.barricade.state);
                this._metadata = data.barricade.state;
                this.transform = new SerializableTransform(barricade.transform);
                this.owner = data.owner;
                this.group = data.group;
            } else
            {
                throw new ArgumentException(ARGUMENT_EXCEPTION_BARRICADE_NOT_FOUND, "barricade");
            }
        }
        public Structure(Interactable2 structure)
        {
            if(structure is Interactable2SalvageBarricade barricade)
            {
                if (BarricadeManager.tryGetInfo(structure.transform, out _, out _, out _, out ushort index, out BarricadeRegion region))
                {
                    BarricadeData data = region.barricades[index];
                    this.id = data.barricade.id;
                    this.state = Convert.ToBase64String(data.barricade.state);
                    this._metadata = data.barricade.state;
                    this.transform = new SerializableTransform(structure.transform);
                    this.owner = data.owner;
                    this.group = data.group;
                }
                else
                {
                    throw new ArgumentException(ARGUMENT_EXCEPTION_BARRICADE_NOT_FOUND, "structure");
                }
            } else if (structure is Interactable2SalvageStructure)
            {
                if (StructureManager.tryGetInfo(structure.transform, out _, out _, out ushort index, out StructureRegion region))
                {
                    StructureData data = region.structures[index];
                    this.id = data.structure.id;
                    this.state = Convert.ToBase64String(new byte[0]);
                    this._metadata = new byte[0];
                    this.transform = new SerializableTransform(structure.transform);
                    this.owner = data.owner;
                    this.group = data.group;
                }
                else
                {
                    throw new ArgumentException(ARGUMENT_EXCEPTION_BARRICADE_NOT_FOUND, "structure");
                }
            }
            else
            {
                throw new ArgumentException(ARGUMENT_EXCEPTION_BARRICADE_NOT_FOUND, "structure");
            }
        }
    }
}
