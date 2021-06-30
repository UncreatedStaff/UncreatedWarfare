using Newtonsoft.Json;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Structures;
using Structure = Uncreated.Warfare.Structures.Structure;

namespace Uncreated.Warfare.Vehicles
{
    /// <summary>Should load after VehicleSaver, VehicleBay, and StructureSaver</summary>
    public class VehicleSigns : JSONSaver<VehicleSign>
    {
        public VehicleSigns() : base(Data.VehicleStorage + "signs.json")
        { }
        public static void InitAllSigns()
        {
            for (int i = 0; i < ActiveObjects.Count; i++)
            {
                try
                {
                    ActiveObjects[i].InitVars();
                }
                catch (Exception ex)
                {
                    F.LogError("Failed to initialize a vbs sign.");
                    F.LogError(ex);
                }
            }
        }
        internal void OnBarricadeDestroyed(BarricadeRegion region, BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant, ushort index)
        {
            for (int i = 0; i < ActiveObjects.Count; i++)
            {
                if (ActiveObjects[i] != null && ActiveObjects[i].instance_id == instanceID)
                {
                    StructureSaver.RemoveStructure(ActiveObjects[i].save);
                    ActiveObjects.Remove(ActiveObjects[i]);
                    Save();
                    break;
                }
            }
        }
        public static List<VehicleSign> GetLinkedSigns(VehicleSpawn spawn) => GetObjectsWhere(x => x.bay.SpawnPadInstanceID == spawn.SpawnPadInstanceID && x.bay.type == spawn.type);
        protected override string LoadDefaults() => "[]";
        public static async Task UnlinkSign(InteractableSign sign)
        {
            if (BarricadeManager.tryGetInfo(sign.transform, out _, out _, out _, out ushort index, out BarricadeRegion region))
            {
                uint instid = region.barricades[index].instanceID;
                for (int i = 0; i < ActiveObjects.Count; i++)
                {
                    if (ActiveObjects[i] != null && ActiveObjects[i].instance_id == instid)
                    {
                        BarricadeManager.ServerSetSignText(sign, "");
                        await ActiveObjects[i].InvokeUpdate();
                        StructureSaver.RemoveStructure(ActiveObjects[i].save);
                        ActiveObjects.Remove(ActiveObjects[i]);
                        Save();
                        break;
                    }
                }
            }
        }
        public static bool SignExists(InteractableSign sign, out VehicleSign vbsign)
        {
            if (BarricadeManager.tryGetInfo(sign.transform, out _, out _, out ushort plant, out ushort index, out BarricadeRegion region))
            {
                if (plant < ushort.MaxValue)
                {
                    vbsign = default;
                    return false;
                }
                BarricadeData data = region.barricades[index];
                return ObjectExists(x => x != default && x.instance_id == data.instanceID, out vbsign);
            }
            vbsign = default;
            return false;
        }
        public static async Task<bool> LinkSign(InteractableSign sign, VehicleSpawn spawn)
        {
            if (BarricadeManager.tryGetInfo(sign.transform, out _, out _, out ushort plant, out ushort index, out BarricadeRegion region))
            {
                if (plant != ushort.MaxValue) throw new Exception("cant_link_to_planted");
                BarricadeData data = region.barricades[index];
                BarricadeDrop drop = region.drops[index];
                if (!StructureSaver.StructureExists(data.instanceID, EStructType.BARRICADE, out Structure structure))
                {
                    StructureSaver.AddStructure(drop, data, out structure);
                }
                VehicleSign n = AddObjectToSave(new VehicleSign(structure, spawn));
                BarricadeManager.ServerSetSignText(sign, n.placeholder_text);
                n.save.state = Convert.ToBase64String(data.barricade.state);
                n.save.ResetMetadata();
                StructureSaver.Save();
                await n.InvokeUpdate();
                return true;
            }
            return false;
        }
    }
    public class VehicleSign
    {
        [JsonIgnore]
        public Structure save;
        [JsonIgnore]
        public VehicleSpawn bay;
        public uint instance_id;
        public uint bay_instance_id;
        public ushort sign_id;
        public ushort bay_id;
        public EStructType bay_type;
        public SerializableTransform sign_transform;
        public SerializableTransform bay_transform;
        public string placeholder_text;
        [JsonConstructor]
        public VehicleSign(ushort sign_id, ushort bay_id, uint instance_id, uint bay_instance_id, SerializableTransform sign_transform, SerializableTransform bay_transform, string placeholder_text, EStructType bay_type)
        {
            this.sign_id = sign_id;
            this.bay_id = bay_id;
            this.instance_id = instance_id;
            this.bay_instance_id = bay_instance_id;
            this.placeholder_text = placeholder_text;
            this.bay_type = bay_type;
            this.sign_transform = sign_transform;
            this.bay_transform = bay_transform;
        }
        public void InitVars()
        {
            if (!StructureSaver.StructureExists(this.instance_id, EStructType.BARRICADE, out save))
            {
                BarricadeData data = F.GetBarricadeFromTransform(sign_transform, out BarricadeDrop drop);
                if (data == default || !StructureSaver.StructureExists(data.instanceID, EStructType.BARRICADE, out save))
                {
                    if (data != default)
                    {
                        if (StructureSaver.AddStructure(drop, data, out save))
                        {
                            save.SpawnCheck().GetAwaiter().GetResult();
                            this.instance_id = save.instance_id;
                        }
                    }
                    else
                    {
                        if (StructureSaver.AddUnspawnedStructure(sign_id, EStructType.BARRICADE, sign_transform, 0, Teams.TeamManager.AdminID, out save))
                        {
                            this.instance_id = save.instance_id;
                        } else
                        {
                            F.LogWarning("Failed to link sign to the correct instance id.");
                        }
                    }
                }
                else
                {
                    this.instance_id = data.instanceID;
                }
            }
            if (!VehicleSpawner.IsRegistered(this.bay_instance_id, out bay, this.bay_type))
            {
                BarricadeData data = F.GetBarricadeFromTransform(bay_transform, out BarricadeDrop drop);
                if (!StructureSaver.StructureExists(data.instanceID, EStructType.BARRICADE, out save))
                {
                    F.LogWarning("Failed to link sign to the correct instance id.");
                }
                else
                {
                    if (VehicleSpawner.IsRegistered(data.instanceID, out bay, this.bay_type))
                    {
                        this.instance_id = data.instanceID;
                    }
                    else
                    {
                        F.LogWarning("Failed to link sign to the correct instance id.");
                    }
                }
            }
        }
        public VehicleSign(Structure save, VehicleSpawn bay)
        {
            if (save == null || bay == null) throw new ArgumentNullException("save or bay", "Can not create a vehicle sign unless save and bay are defined.");
            this.save = save;
            this.bay = bay;
            this.instance_id = save.instance_id;
            this.bay_instance_id = bay.SpawnPadInstanceID;
            this.bay_type = bay.type;
            this.placeholder_text = $"sign_vbs_" + bay.VehicleID.ToString(Data.Locale);
            this.sign_transform = save.transform;
            this.sign_id = save.id;
            if (StructureSaver.StructureExists(bay.SpawnPadInstanceID, bay.type, out Structure s))
            {
                this.bay_id = s.id;
                this.bay_transform = s.transform;
            }
            else
            {
                if (bay.type == EStructType.BARRICADE)
                {
                    BarricadeData data = F.GetBarricadeFromInstID(bay.SpawnPadInstanceID, out BarricadeDrop drop);
                    if (drop != default)
                    {
                        this.bay_id = data.barricade.id;
                        this.bay_transform = new SerializableTransform(drop.model);
                    }
                }
                else if (bay.type == EStructType.STRUCTURE)
                {
                    StructureData data = F.GetStructureFromInstID(bay.SpawnPadInstanceID, out StructureDrop drop);
                    if (drop != default)
                    {
                        this.bay_id = data.structure.id;
                        this.bay_transform = new SerializableTransform(drop.model);
                    }
                }
            }
        }
        public void SpawnCheck() => save?.SpawnCheck();
        public async Task InvokeUpdate(SteamPlayer player)
        {
            F.GetBarricadeFromInstID(save.instance_id, out BarricadeDrop drop);
            if (drop != default && drop.model != default)
                if (BarricadeManager.tryGetInfo(drop.model.transform, out byte x, out byte y, out ushort plant, out ushort index, out _))
                    await F.InvokeSignUpdateFor(player, x, y, plant, index, placeholder_text);
        }
        public async Task InvokeUpdate()
        {
            F.GetBarricadeFromInstID(save.instance_id, out BarricadeDrop drop);
            if (drop != default && drop.model != default)
                if (BarricadeManager.tryGetInfo(drop.model.transform, out byte x, out byte y, out ushort plant, out ushort index, out _))
                    await F.InvokeSignUpdateForAllKits(x, y, plant, index, placeholder_text);
        }
    }
}
