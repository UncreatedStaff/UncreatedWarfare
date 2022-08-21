using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Structures;

namespace Uncreated.Warfare.Vehicles;

[SingletonDependency(typeof(VehicleSpawner))]
[SingletonDependency(typeof(VehicleBay))]
[SingletonDependency(typeof(StructureSaver))]
public class VehicleSigns : ListSingleton<VehicleSign>, ILevelStartListener
{
    public VehicleSigns() : base("vehiclesigns", Path.Combine(Data.Paths.VehicleStorage, "signs.json")) { }
    private static VehicleSigns Singleton;
    public static bool Loaded => Singleton.IsLoaded<VehicleSigns, VehicleSign>();
    public override void Load()
    {
        Singleton = this;
    }
    public override void Unload()
    {
        Singleton = null!;
    }
    public void OnLevelReady()
    {
        InitAllSigns();
    }
    public void InitAllSigns()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < Count; i++)
        {
            try
            {
                this[i].InitVars();
            }
            catch (Exception ex)
            {
                L.LogError("Failed to initialize a vbs sign.");
                L.LogError(ex);
            }
        }
    }
    internal void OnBarricadeDestroyed(BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < Count; i++)
        {
            VehicleSign vs = this[i];
            if (vs is not null && vs.instance_id == instanceID)
            {
                StructureSaver.RemoveSave(vs.save);
                Remove(vs);
                Save();
                break;
            }
        }
    }
    public static IEnumerable<VehicleSign> GetLinkedSigns(VehicleSpawn spawn)
    {
        Singleton.AssertLoaded<VehicleSigns, VehicleSign>();
        return Singleton.GetObjectsWhere(x => x.bay != null && x.bay.SpawnPadInstanceID == spawn.SpawnPadInstanceID && x.bay.type == spawn.type);
    }

    protected override string LoadDefaults() => "[]";
    public static void SaveSingleton()
    {
        Singleton.AssertLoaded<VehicleSigns, VehicleSign>();
        Singleton.Save();
    }
    public static void UnlinkSign(InteractableSign sign)
    {
        Singleton.AssertLoaded<VehicleSigns, VehicleSign>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(sign.transform);
        if (drop != null)
        {
            for (int i = 0; i < Singleton.Count; i++)
            {
                if (Singleton[i] != null && Singleton[i].instance_id == drop.instanceID)
                {
                    RequestSigns.SetSignTextSneaky(sign, string.Empty);
                    VehicleSign vs = Singleton[i];
                    StructureSaver.RemoveSave(vs.save);
                    Singleton.Remove(Singleton[i]);
                    if (VehicleSpawner.Loaded)
                    {
                        foreach (VehicleSpawn spawn in VehicleSpawner.Spawners)
                        {
                            if (spawn.LinkedSign == Singleton[i])
                            {
                                spawn.LinkedSign = null;
                                spawn.UpdateSign();
                                Signs.BroadcastSignUpdate(drop);
                            }
                        }
                    }
                    Singleton.Save();
                    break;
                }
            }
        }
    }
    public static void OnFlagCaptured()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (VehicleSpawner.Loaded)
        {
            foreach (VehicleSpawn spawn in VehicleSpawner.Spawners)
            {
                if (VehicleBay.VehicleExists(spawn.VehicleID, out VehicleData data) && (data.HasDelayType(EDelayType.FLAG) || data.HasDelayType(EDelayType.FLAG_PERCENT)))
                {
                    spawn.UpdateSign();
                }
            }
        }
    }
    public static bool SignExists(InteractableSign sign, out VehicleSign vbsign)
    {
        Singleton.AssertLoaded<VehicleSigns, VehicleSign>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(sign.transform);
        if (drop != null)
        {
            return Singleton.ObjectExists(x => x != default && x.instance_id == drop.instanceID, out vbsign) && vbsign.bay != null;
        }
        vbsign = default!;
        return false;
    }
    public static bool LinkSign(InteractableSign sign, VehicleSpawn spawn)
    {
        Singleton.AssertLoaded<VehicleSigns, VehicleSign>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(sign.transform);
        if (drop != null)
        {
            if (!StructureSaver.SaveExists(drop, out SavedStructure structure))
                StructureSaver.AddBarricade(drop, out structure);

            VehicleSign n = Singleton.AddObjectToSave(new VehicleSign(drop, sign, structure, spawn));
            spawn.LinkedSign = n;
            
            n.save.Metadata = RequestSigns.SetSignTextSneaky(sign, n.placeholder_text);
            StructureSaver.SaveSingleton();
            spawn.UpdateSign();
            return true;
        }
        return false;
    }

    internal static void TimeSync()
    {
        if (VehicleSpawner.Loaded)
        {
            for (int i = 0; i < VehicleSpawner.Singleton.Count; ++i)
            {
                VehicleSpawn spawn = VehicleSpawner.Singleton[i];
                if (spawn.Component != null)
                    spawn.Component.TimeSync();
            }
        }
    }
}
[JsonSerializable(typeof(VehicleSign))]
public class VehicleSign
{
    [JsonIgnore]
    public SavedStructure save;
    [JsonIgnore]
    public VehicleSpawn bay;
    [JsonIgnore]
    public BarricadeDrop? SignDrop;
    [JsonIgnore]
    public InteractableSign? SignInteractable;
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
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!StructureSaver.SaveExists(this.instance_id, EStructType.BARRICADE, out save))
        {
            BarricadeDrop? drop = UCBarricadeManager.GetBarriadeBySerializedTransform(sign_transform);
            if (drop == null)
            {
                L.LogWarning("Failed to link sign to the correct instance id.");
            }
            else if (!StructureSaver.SaveExists(drop, out save))
            {
                if (StructureSaver.AddBarricade(drop, out SavedStructure structure))
                {
                    save = structure;
                    this.instance_id = structure.InstanceID;
                    SignDrop = drop;
                    SignInteractable = drop.interactable as InteractableSign;
                    if (SignInteractable != null)
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
                if (SignInteractable != null)
                    RequestSigns.SetSignTextSneaky(SignInteractable, this.placeholder_text);
            }
        }
        else
        {
            SignDrop = UCBarricadeManager.GetBarricadeFromInstID(save.InstanceID);
            if (SignDrop != null)
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
                BarricadeDrop? drop = UCBarricadeManager.GetBarriadeBySerializedTransform(bay_transform);
                if (drop == null)
                {
                    L.LogWarning("Failed to link sign to the correct vehicle bay instance id.");
                }
                else if (!StructureSaver.SaveExists(drop, out save))
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
                StructureDrop? drop = UCBarricadeManager.GetStructureBySerializedTransform(bay_transform);
                if (drop == null)
                {
                    L.LogWarning("Failed to link sign to the correct vehicle bay instance id.");
                }
                else if (!StructureSaver.SaveExists(drop, out save))
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
    public VehicleSign(BarricadeDrop drop, InteractableSign sign, SavedStructure save, VehicleSpawn bay)
    {
        if (save == null || bay == null) throw new ArgumentNullException("save or bay", "Can not create a vehicle sign unless save and bay are defined.");
        this.save = save;
        this.bay = bay;
        this.instance_id = save.InstanceID;
        this.bay_instance_id = bay.SpawnPadInstanceID;
        this.bay_type = bay.type;
        Asset? asset = Assets.find(bay.VehicleID);
        this.placeholder_text = $"sign_vbs_" + (asset == null ? bay.VehicleID.ToString("N") : asset.id.ToString(Data.Locale));
        this.sign_transform = new SerializableTransform(save.Position, save.Rotation);
        this.SignInteractable = sign;
        this.SignDrop = drop;
        if (StructureSaver.SaveExists(bay.SpawnPadInstanceID, bay.type, out SavedStructure s))
            this.bay_transform = new SerializableTransform(s.Position, s.Rotation);
        else if (bay.type == EStructType.BARRICADE)
        {
            BarricadeData? paddata = UCBarricadeManager.GetBarricadeFromInstID(bay.SpawnPadInstanceID, out BarricadeDrop? paddrop);
            if (paddata != null)
            {
                if (drop != default) this.bay_transform = new SerializableTransform(paddrop!.model);
                StructureSaver.AddBarricade(paddrop!, out _);
            }
        }
        else if (bay.type == EStructType.STRUCTURE)
        {
            StructureData? paddata = UCBarricadeManager.GetStructureFromInstID(bay.SpawnPadInstanceID, out StructureDrop? paddrop);
            if (paddata != null)
            {
                if (drop != default) this.bay_transform = new SerializableTransform(paddrop!.model);
                StructureSaver.AddStructure(paddrop!, out _);
            }
        }
    }
}
