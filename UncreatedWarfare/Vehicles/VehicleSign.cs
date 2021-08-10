using Newtonsoft.Json;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;
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
        internal void OnBarricadeDestroyed(BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant)
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
        public static void UnlinkSign(InteractableSign sign)
        {
            BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(sign.transform);
            if (drop != null)
            {
                for (int i = 0; i < ActiveObjects.Count; i++)
                {
                    if (ActiveObjects[i] != null && ActiveObjects[i].instance_id == drop.instanceID)
                    {
                        BarricadeManager.ServerSetSignText(sign, "");
                        ActiveObjects[i].InvokeUpdate();
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
            BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(sign.transform);
            if (drop != null)
            {
                return ObjectExists(x => x != default && x.instance_id == drop.instanceID, out vbsign);
            }
            vbsign = default;
            return false;
        }
        public static bool LinkSign(InteractableSign sign, VehicleSpawn spawn)
        {
            BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(sign.transform);
            if (drop != null)
            {
                //if (plant != ushort.MaxValue) throw new Exception("cant_link_to_planted");
                if (!StructureSaver.StructureExists(drop.instanceID, EStructType.BARRICADE, out Structure structure))
                {
                    StructureSaver.AddStructure(drop, drop.GetServersideData(), out structure);
                }
                VehicleSign n = AddObjectToSave(new VehicleSign(structure, spawn));
                BarricadeManager.ServerSetSignText(sign, n.placeholder_text);
                n.save.state = Convert.ToBase64String(drop.GetServersideData().barricade.state);
                n.save.ResetMetadata();
                StructureSaver.Save();
                n.InvokeUpdate();
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
        public EStructType bay_type;
        public SerializableTransform sign_transform;
        public SerializableTransform bay_transform;
        public string placeholder_text;
        [JsonConstructor]
        public VehicleSign(uint instance_id, uint bay_instance_id, SerializableTransform sign_transform, SerializableTransform bay_transform, string placeholder_text, EStructType bay_type)
        {
            this.instance_id = instance_id;
            this.sign_transform = sign_transform;
            this.bay_transform = bay_transform;
            this.bay_instance_id = bay_instance_id;
            this.placeholder_text = placeholder_text;
            this.bay_type = bay_type;
        }
        public VehicleSign()
        {
            this.instance_id = 0;
            this.bay_instance_id = 0;
            this.placeholder_text = string.Empty;
            this.bay_type = EStructType.BARRICADE;
            this.sign_transform = SerializableTransform.Zero;
            this.bay_transform = SerializableTransform.Zero;
        }
        public void InitVars()
        {
            if (!StructureSaver.StructureExists(this.instance_id, EStructType.BARRICADE, out save))
            {
                BarricadeDrop drop = F.GetBarriadeBySerializedTransform(bay_transform);
                if (!StructureSaver.StructureExists(drop.instanceID, EStructType.BARRICADE, out save))
                {
                    if (drop != default)
                    {
                        if (StructureSaver.AddStructure(drop, drop.GetServersideData(), out Structure structure))
                        {
                            save = structure;
                            this.instance_id = structure.instance_id;
                            structure.SpawnCheck();
                        }
                    }
                    else
                    {
                        F.LogWarning("Failed to link sign to the correct instance id.");
                    }
                }
                else
                {
                    this.instance_id = drop.instanceID;
                }
            }
            if (!VehicleSpawner.IsRegistered(this.bay_instance_id, out bay, this.bay_type))
            {
                BarricadeDrop drop = F.GetBarriadeBySerializedTransform(bay_transform);
                if (!StructureSaver.StructureExists(drop.instanceID, EStructType.BARRICADE, out save))
                {
                    F.LogWarning("Failed to link sign to the correct instance id.");
                }
                else
                {
                    if (VehicleSpawner.IsRegistered(drop.instanceID, out bay, this.bay_type))
                    {
                        this.instance_id = drop.instanceID;
                        this.sign_transform = new SerializableTransform(drop.model.transform);
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
            if (StructureSaver.StructureExists(bay.SpawnPadInstanceID, bay.type, out Structure s))
                this.bay_transform = s.transform;
            else
            {
                if (bay.type == EStructType.BARRICADE)
                {
                    F.GetBarricadeFromInstID(bay.SpawnPadInstanceID, out BarricadeDrop drop);
                    if (drop != default) this.bay_transform = new SerializableTransform(drop.model);
                } else if (bay.type == EStructType.STRUCTURE)
                {
                    F.GetStructureFromInstID(bay.SpawnPadInstanceID, out StructureDrop drop);
                    if (drop != default) this.bay_transform = new SerializableTransform(drop.model);
                }
            }
        }
        public void SpawnCheck()
        {
            if (save == null)
            {
                F.LogWarning("Save was null in VehicleSign.");
            } else
            {
                save?.SpawnCheck();
                BarricadeDrop drop = F.GetBarricadeFromInstID(save.instance_id);
                if (drop != null && save.exists && drop.interactable is InteractableSign sign)
                {
                    if (sign.text != placeholder_text)
                    {
                        RequestSigns.SetSignTextSneaky(sign, placeholder_text);
                        sign.updateText(placeholder_text);
                    }
                }
            }
        }

        public void InvokeUpdate(SteamPlayer player)
        {
            F.GetBarricadeFromInstID(save.instance_id, out BarricadeDrop drop);
            if (drop != default && drop.model != default)
                if (drop.model.TryGetComponent(out InteractableSign sign) && Regions.tryGetCoordinate(sign.transform.position, out byte x, out byte y))
                    F.InvokeSignUpdateFor(player, sign, placeholder_text);
        }
        public void InvokeUpdate()
        {
            F.GetBarricadeFromInstID(save.instance_id, out BarricadeDrop drop);
            if (drop != default && drop.model != default)
                if (drop.model.TryGetComponent(out InteractableSign sign) && Regions.tryGetCoordinate(sign.transform.position, out byte x, out byte y))
                    F.InvokeSignUpdateForAllKits(sign, x, y, placeholder_text);
        }
    }
}
