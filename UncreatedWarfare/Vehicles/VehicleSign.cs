using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
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
                    L.LogError("Failed to initialize a vbs sign.");
                    L.LogError(ex);
                }
            }
        }
        internal void OnBarricadeDestroyed(SDG.Unturned.BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant)
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
        public static IEnumerable<VehicleSign> GetLinkedSigns(VehicleSpawn spawn) => GetObjectsWhere(x => x.bay != null && x.bay.SpawnPadInstanceID == spawn.SpawnPadInstanceID && x.bay.type == spawn.type);
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
                        RequestSigns.SetSignTextSneaky(sign, "");
                        StructureSaver.RemoveStructure(ActiveObjects[i].save);
                        ActiveObjects.Remove(ActiveObjects[i]);
                        for (int i2 = 0; i2 < VehicleSpawner.ActiveObjects.Count; i2++)
                        {
                            if (VehicleSpawner.ActiveObjects[i2].LinkedSign == ActiveObjects[i])
                            {
                                VehicleSpawner.ActiveObjects[i2].LinkedSign = null;
                                VehicleSpawner.ActiveObjects[i2].UpdateSign();
                                if (Regions.tryGetCoordinate(sign.transform.position, out byte x, out byte y))
                                    F.InvokeSignUpdateForAll(sign, x, y, sign.text);
                            }
                        }
                        Save();
                        break;
                    }
                }
            }
        }
        public static void OnFlagCaptured()
        {
            for (int i = 0; i < VehicleSpawner.ActiveObjects.Count; i++)
            {
                VehicleSpawn spawn = VehicleSpawner.ActiveObjects[i];
                if (VehicleBay.VehicleExists(spawn.VehicleID, out VehicleData data) && (data.HasDelayType(EDelayType.FLAG) || data.HasDelayType(EDelayType.FLAG_PERCENT)))
                {
                    spawn.UpdateSign();
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
                VehicleSign n = AddObjectToSave(new VehicleSign(drop, sign, structure, spawn));
                spawn.LinkedSign = n;
               
                RequestSigns.SetSignTextSneaky(sign, n.placeholder_text);
                n.save.state = Convert.ToBase64String(drop.GetServersideData().barricade.state);
                n.save.ResetMetadata();
                StructureSaver.Save();
                spawn.UpdateSign();
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
        [JsonIgnore]
        public BarricadeDrop SignDrop;
        [JsonIgnore]
        public InteractableSign SignInteractable;
        public uint instance_id;
        public uint bay_instance_id;
        public EStructType bay_type;
        public SerializableTransform sign_transform;
        public SerializableTransform bay_transform;
        public string placeholder_text;
        public override string ToString() => $"Instance id: {instance_id}, bay: {bay_instance_id}, text: {placeholder_text}";
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
                BarricadeDrop drop = UCBarricadeManager.GetBarriadeBySerializedTransform(sign_transform);
                if (drop == null)
                {
                    L.LogWarning("Failed to link sign to the correct instance id.");
                }
                else if (!StructureSaver.StructureExists(drop.instanceID, EStructType.BARRICADE, out save))
                {
                    if (StructureSaver.AddStructure(drop, drop.GetServersideData(), out Structure structure))
                    {
                        save = structure;
                        structure.SpawnCheck();
                        this.instance_id = structure.instance_id;
                        SignDrop = drop;
                        SignInteractable = drop.interactable as InteractableSign;
                        RequestSigns.SetSignTextSneaky(SignInteractable, this.placeholder_text);
                    }
                    else
                    {
                        L.LogWarning("Failed to add sign to structure saver.");
                    }
                }
                else
                {
                    this.instance_id = drop.instanceID;
                    SignDrop = drop;
                    SignInteractable = drop.interactable as InteractableSign;
                    RequestSigns.SetSignTextSneaky(SignInteractable, this.placeholder_text);
                }
            }
            else
            {
                SignDrop = UCBarricadeManager.GetBarricadeFromInstID(save.instance_id);
                SignInteractable = SignDrop.interactable as InteractableSign;
            }
            if (SignDrop == null)
            {
                L.LogWarning("Unable to get drop of sign " + instance_id);
            }
            if (SignInteractable == null)
            {
                L.LogWarning("Unable to get interactable of sign " + instance_id);
            }
            if (!VehicleSpawner.IsRegistered(this.bay_instance_id, out bay, this.bay_type))
            {
                if (this.bay_type == EStructType.BARRICADE)
                {
                    BarricadeDrop drop = UCBarricadeManager.GetBarriadeBySerializedTransform(bay_transform);
                    if (drop == null)
                    {
                        L.LogWarning("Failed to link sign to the correct vehicle bay instance id.");
                    }
                    else if (!StructureSaver.StructureExists(drop.instanceID, EStructType.BARRICADE, out save))
                    {
                        L.LogWarning("Failed to find vehicle bay in structure saver.");
                    }
                    else if (VehicleSpawner.IsRegistered(drop.instanceID, out bay, this.bay_type))
                    {
                        this.instance_id = drop.instanceID;
                        this.sign_transform = new SerializableTransform(drop.model.transform);
                        bay.LinkedSign = this;
                        L.LogDebug("Linked sign " + instance_id + " to bay " + instance_id);
                        bay.UpdateSign();
                    }
                    else
                    {
                        L.LogWarning("Failed to find new vehicle bay in vehicle spawner.");
                    }
                }
                else
                {
                    StructureDrop drop = UCBarricadeManager.GetStructureBySerializedTransform(bay_transform);
                    if (drop == null)
                    {
                        L.LogWarning("Failed to link sign to the correct vehicle bay instance id.");
                    }
                    else if (!StructureSaver.StructureExists(drop.instanceID, EStructType.STRUCTURE, out save))
                    {
                        L.LogWarning("Failed to find vehicle bay in structure saver.");
                    }
                    else if (VehicleSpawner.IsRegistered(drop.instanceID, out bay, this.bay_type))
                    {
                        this.instance_id = drop.instanceID;
                        this.sign_transform = new SerializableTransform(drop.model.transform);
                        bay.LinkedSign = this;
                        L.LogDebug("Linked sign " + instance_id + " to bay " + instance_id);
                        bay.UpdateSign();
                    }
                    else
                    {
                        L.LogWarning("Failed to find new vehicle bay in vehicle spawner.");
                    }
                }
            }
            else
            {
                bay.LinkedSign = this;
                L.LogDebug("Sign " + instance_id + " was already linked to bay " + instance_id);
                bay.UpdateSign();
            }
        }
        public VehicleSign(BarricadeDrop drop, InteractableSign sign, Structure save, VehicleSpawn bay)
        {
            if (save == null || bay == null) throw new ArgumentNullException("save or bay", "Can not create a vehicle sign unless save and bay are defined.");
            this.save = save;
            this.bay = bay;
            this.instance_id = save.instance_id;
            this.bay_instance_id = bay.SpawnPadInstanceID;
            this.bay_type = bay.type;
            Asset asset = Assets.find(bay.VehicleID);
            this.placeholder_text = $"sign_vbs_" + (asset == null ? bay.VehicleID.ToString("N") : asset.id.ToString(Data.Locale));
            this.sign_transform = save.transform;
            this.SignInteractable = sign;
            this.SignDrop = drop;
            if (StructureSaver.StructureExists(bay.SpawnPadInstanceID, bay.type, out Structure s))
                this.bay_transform = s.transform;
            else if (bay.type == EStructType.BARRICADE)
            {
                SDG.Unturned.BarricadeData paddata = UCBarricadeManager.GetBarricadeFromInstID(bay.SpawnPadInstanceID, out BarricadeDrop paddrop);
                if (drop != default) this.bay_transform = new SerializableTransform(paddrop.model);
                StructureSaver.AddStructure(paddrop, paddata, out _);
            }
            else if (bay.type == EStructType.STRUCTURE)
            {
                SDG.Unturned.StructureData paddata = UCBarricadeManager.GetStructureFromInstID(bay.SpawnPadInstanceID, out StructureDrop paddrop);
                if (drop != default) this.bay_transform = new SerializableTransform(paddrop.model);
                StructureSaver.AddStructure(paddrop, paddata, out _);
            }
        }
    }
}
